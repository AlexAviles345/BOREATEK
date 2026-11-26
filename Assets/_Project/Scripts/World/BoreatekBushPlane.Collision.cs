using Boreatek.Audio;
using Boreatek.Player;
using UnityEngine;

namespace Boreatek.World
{
    internal sealed partial class BoreatekBushPlane
    {
        private const float CollisionMargin = 0.055f;
        private const float SmokeThreshold = 70f;
        private const float FireThreshold = 24f;
        private readonly Collider[] _blastTargets = new Collider[24];
        private Vector3 _collisionDrift;
        private float _collisionYaw, _collisionRoll, _impactCooldown;
        private float _hull = 100f;
        private float _burnSeconds;
        private Collider _lastObstacle;
        private bool _wrecked;

        internal bool IsWrecked => _wrecked;
        private bool Burning => _burnSeconds > 0f;

        private readonly struct HullContact
        {
            internal readonly RaycastHit Hit;
            internal readonly bool Wing;

            internal HullContact(RaycastHit hit, bool wing)
            {
                Hit = hit;
                Wing = wing;
            }
        }

        private void UpdateImpactInertia(float dt, bool brake)
        {
            float drag = _airborne ? 3.2f : brake ? 16f : 10f;
            _collisionDrift = Vector3.MoveTowards(_collisionDrift, Vector3.zero, drag * dt);
            _collisionYaw = Mathf.MoveTowards(_collisionYaw, 0f, dt * 120f);
            _collisionRoll = Mathf.MoveTowards(_collisionRoll, 0f, dt * 45f);
            _impactCooldown = Mathf.Max(0f, _impactCooldown - dt);
        }

        private bool TrySweepHull(Vector3 travel, Collider support, out HullContact contact)
        {
            contact = default;
            float distance = travel.magnitude;
            if (distance < 0.001f) return false;
            Vector3 direction = travel / distance;
            Quaternion orientation = _takeoffAssist > 0f && _altitude < TakeoffClearance
                ? Quaternion.Euler(0f, _heading, 0f) : transform.rotation;
            SweepPart(new Vector3(0f, 1.20f, 0.23f), new Vector3(0.54f, 0.69f, 2.89f),
                orientation, direction, distance, support, wing: false, ref contact);
            SweepPart(new Vector3(0f, 2.25f, 0.30f), new Vector3(3.95f, 0.16f, 0.70f),
                orientation, direction, distance, support, wing: true, ref contact);
            return contact.Hit.collider != null;
        }

        private void SweepPart(Vector3 center, Vector3 size, Quaternion orientation, Vector3 direction,
            float distance, Collider support, bool wing, ref HullContact contact)
        {
            int count = Physics.BoxCastNonAlloc(transform.position + orientation * center, size, direction, _hits,
                orientation, distance + CollisionMargin, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];
                Collider obstacle = hit.collider;
                if (obstacle.transform.IsChildOf(transform) || (_pilot != null && obstacle.transform.IsChildOf(_pilot.transform)))
                    continue;
                if (hit.normal.y > 0.72f && hit.point.y < transform.position.y + 0.36f) continue;
                if (obstacle == support && hit.distance <= 0.001f && _takeoffAssist > 0f && direction.y >= 0f) continue;
                if (Vector3.Dot(direction, hit.normal) > -0.09f ||
                    (contact.Hit.collider != null && hit.distance >= contact.Hit.distance)) continue;
                contact = new HullContact(hit, wing);
            }
        }

        private void DeflectFromContact(HullContact collision, Vector3 travel, float dt)
        {
            RaycastHit hit = collision.Hit;
            Vector3 velocity = travel / dt;
            float speedIntoObstacle = Mathf.Max(0f, -Vector3.Dot(velocity, hit.normal));
            Vector3 contactPoint = hit.distance > 0.001f ? hit.point : transform.position + Vector3.up * 1.14f;
            float allowed = Mathf.Clamp(hit.distance - CollisionMargin, 0f, travel.magnitude);
            transform.position += travel.normalized * allowed;
            ApplyDeflection(hit.normal, contactPoint, velocity, speedIntoObstacle);

            if (hit.collider == _lastObstacle && _impactCooldown > 0f) return;
            _lastObstacle = hit.collider;
            _impactCooldown = 0.40f;
            float damage = Mathf.Max(0f, speedIntoObstacle - 2.8f) * (collision.Wing ? 2.3f : 3.65f);
            bool catastrophic = speedIntoObstacle >= (collision.Wing ? 29f : 24f);
            ReportImpact(contactPoint, damage, speedIntoObstacle, catastrophic, landing: false);
        }

        private void ApplyDeflection(Vector3 normal, Vector3 point, Vector3 velocity, float severity)
        {
            Vector3 tangent = Vector3.ProjectOnPlane(velocity, normal);
            Vector3 push = tangent * Mathf.Lerp(0.93f, 0.46f, severity / 26f) + normal * Mathf.Clamp(severity * 0.18f, 0.12f, 5.2f);
            Vector3 forward = Quaternion.Euler(0f, _heading, 0f) * Vector3.forward;
            Vector3 horizontal = Vector3.ProjectOnPlane(push, Vector3.up);
            _speed = Mathf.Max(0f, Vector3.Dot(horizontal, forward));
            _collisionDrift = Vector3.ClampMagnitude(horizontal - forward * _speed, 16f);
            if (_airborne)
            {
                _verticalSpeed = Mathf.Clamp(push.y, -15f, 7f);
                if (severity > 7.5f) _takeoffAssist = 0f;
            }
            if (severity > 8f) _throttle *= 0.32f;
            float side = Vector3.Dot(Vector3.Cross(Vector3.up, forward), normal);
            if (Mathf.Abs(side) < 0.13f)
            {
                float lever = Vector3.Dot(Vector3.Cross(Vector3.up, forward), transform.position - point);
                side = Mathf.Abs(lever) < 0.10f ? 0.24f : Mathf.Sign(lever) * 0.42f;
            }
            _collisionYaw = Mathf.Clamp(_collisionYaw + side * (9f + severity * 3.1f), -89f, 89f);
            _collisionRoll = Mathf.Clamp(_collisionRoll + side * Mathf.Min(24f, severity * 1.15f), -34f, 34f);
            transform.rotation = Attitude();
        }

        private void HitRise(RaycastHit ground, Vector3 travel, float dt)
        {
            if (travel.sqrMagnitude < 0.000001f)
            {
                StopTaxi();
                return;
            }
            Vector3 normal = Vector3.ProjectOnPlane(ground.normal, Vector3.up);
            if (normal.sqrMagnitude < 0.025f || Vector3.Dot(normal, travel) >= 0f) normal = -travel.normalized;
            normal.Normalize();
            float severity = Mathf.Max(0f, -Vector3.Dot(travel / dt, normal));
            ApplyDeflection(normal, ground.point, travel / dt, severity);
            if (ground.collider == _lastObstacle && _impactCooldown > 0f) return;
            _lastObstacle = ground.collider;
            _impactCooldown = 0.40f;
            ReportImpact(ground.point, Mathf.Max(0f, severity - 4f) * 3f, severity, severity >= 26f, landing: false);
        }

        private void Land(Vector3 next, RaycastHit ground)
        {
            float descent = Mathf.Max(0f, -_verticalSpeed);
            float sideways = Mathf.Max(0f, Mathf.Abs(_bank) - 18f);
            float incline = Mathf.Max(0f, 0.80f - ground.normal.y);
            float noseDown = Mathf.Max(0f, -_pitch - 12f);
            float damage = Mathf.Max(0f, descent - 4.2f) * 5.1f + sideways * 1.4f +
                incline * _speed * 4.1f + noseDown * 1.6f;
            bool catastrophic = (descent >= 11.8f && (_speed >= 21f || noseDown > 3f)) || damage >= 94f;

            _airborne = false;
            _takeoffAssist = 0f;
            _throttle = Mathf.Min(_throttle, 0.24f);
            _verticalSpeed = 0f;
            _altitude = 0f;
            next.y = ground.point.y + 0.025f;
            if (damage >= 3f)
            {
                float side = Mathf.Abs(_bank) > 0.1f ? Mathf.Sign(_bank) : 1f;
                _speed *= Mathf.Lerp(0.77f, 0.22f, damage / 75f);
                _throttle = 0f;
                _collisionRoll = side * Mathf.Clamp(damage * 0.29f, 1f, 24f);
                _collisionYaw = side * Mathf.Clamp(damage * 0.62f, 3f, 53f);
            }
            transform.SetPositionAndRotation(next, Attitude());
            if (damage >= 3f)
                ReportImpact(ground.point, damage, descent + sideways * 0.32f, catastrophic, landing: true);
            else
            {
                BoreatekSoundscape.Play(SoundCue.PlaneTouchdown, next, 0.82f);
                if (!Burning) _pilot?.PublishStatus("En tierra. Frena con Espacio y pulsa E para bajar.");
            }
        }

        private void ReportImpact(Vector3 point, float damage, float severity, bool violent, bool landing)
        {
            if (severity >= 1.35f || damage >= 3f)
            {
                BoreatekSoundscape.Play(damage >= 18f ? SoundCue.PlaneImpact : SoundCue.PlaneScrape, point,
                    Mathf.Lerp(0.36f, 0.94f, Mathf.Max(severity / 20f, damage / 54f)));
                EmitImpactSparks(point, Mathf.RoundToInt(Mathf.Lerp(5f, 24f, Mathf.Max(severity / 20f, damage / 54f))));
            }
            if (damage < 1.25f && !violent) return;
            _hull = Mathf.Max(0f, _hull - damage);
            if (violent) _hull = Mathf.Min(_hull, 10f);
            if (_hull <= FireThreshold)
            {
                float fuse = violent ? 0.84f : 4.5f;
                _burnSeconds = Burning ? Mathf.Min(fuse, _burnSeconds) : fuse;
                _throttle = 0f;
                _pilot?.PublishStatus("¡Motor en llamas! Sal de la avioneta en cuanto se detenga.");
            }
            else
            {
                string impact = landing ? "Aterrizaje brusco" : "Golpe en la avioneta";
                _pilot?.PublishStatus($"{impact}. Estado {_hull:0}%.");
            }
            float injury = Mathf.Clamp((severity - 8f) * 1.15f, 0f, 22f);
            if (injury >= 1f && _pilot != null && _pilot.TryGetComponent(out TemperatureSystem vitals))
                vitals.ApplyCombatDamage(injury, point);
        }

        private void Update() => AdvanceDamage(Time.deltaTime);

        private void AdvanceDamage(float dt)
        {
            if (dt <= 0f) return;
            if (Burning)
            {
                _burnSeconds -= dt;
                if (_burnSeconds <= 0f) Explode();
            }
            if (_pilot == null && _airborne) FallUnoccupied(dt);
        }

        private void FallUnoccupied(float dt)
        {
            Vector3 next = transform.position;
            _verticalSpeed = Mathf.MoveTowards(_verticalSpeed, -20f, 13f * dt);
            next.y += _verticalSpeed * dt;
            if (FindGround(next, out RaycastHit ground) && next.y <= ground.point.y + 0.025f)
            {
                next.y = ground.point.y + 0.025f;
                _airborne = false;
                _verticalSpeed = 0f;
                _collisionDrift = Vector3.zero;
                BoreatekSoundscape.Play(SoundCue.PlaneImpact, next, 0.69f);
            }
            transform.SetPositionAndRotation(next, Quaternion.Euler(_wrecked ? 7f : 0f, _heading, _wrecked ? 11f : 0f));
        }

        private void Explode()
        {
            if (_wrecked) return;
            _wrecked = true;
            _hull = _burnSeconds = _fuel = _speed = _throttle = _rpm = 0f;
            Vector3 center = transform.TransformPoint(new Vector3(0f, 1.06f, 1.25f));
            SetWreckVisuals();
            EmitExplosion(center);
            BoreatekSoundscape.Play(SoundCue.PlaneExplosion, _pilot != null ? (Vector3?)null : center, 0.96f);
            if (_pilot != null)
            {
                _pilot.HandlePlaneExplosion(this, center);
                return;
            }
            int count = Physics.OverlapSphereNonAlloc(center, 5.2f, _blastTargets,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (!_blastTargets[i].TryGetComponent(out PlayerController player) ||
                    !player.TryGetComponent(out TemperatureSystem vitals)) continue;
                float distance = Vector3.Distance(center, player.transform.position + Vector3.up * 0.60f);
                float harm = 72f * (1f - Mathf.InverseLerp(1.1f, 5.2f, distance));
                vitals.ApplyCombatDamage(harm, center);
                break;
            }
        }

        internal bool NeedsServiceAt(Vector3 station) => _wrecked ||
            (_hull < 99.9f && !Burning && _pilot == null && CanExit && (station - transform.position).sqrMagnitude <= 22f * 22f);

        internal void Service()
        {
            if (_wrecked)
            {
                ReturnToStation();
                _fuel = 9f;
            }
            _wrecked = false;
            _burnSeconds = 0f;
            _hull = 100f;
            _lastObstacle = null;
            _impactCooldown = 0f;
            SetWreckVisuals();
            ClearCollisionEffects();
        }

        private void RestoreHull(SaveState save)
        {
            _hull = save.hasHullState ? Bound(save.hull, 0f, 100f) : 100f;
            _wrecked = save.hasHullState && save.wrecked;
            _burnSeconds = save.hasHullState && !_wrecked ? Bound(save.burnSeconds, 0f, 4.5f) : 0f;
            Vector3 savedDrift = save.collisionDrift;
            _collisionDrift = !_wrecked && float.IsFinite(savedDrift.x) && float.IsFinite(savedDrift.z)
                ? Vector3.ClampMagnitude(new Vector3(savedDrift.x, 0f, savedDrift.z), 16f) : Vector3.zero;
            _collisionYaw = _collisionRoll = _impactCooldown = 0f;
            _lastObstacle = null;
            SetWreckVisuals();
        }
    }
}
