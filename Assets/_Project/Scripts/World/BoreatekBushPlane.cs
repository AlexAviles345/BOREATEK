using System;
using Boreatek.Audio;
using Boreatek.Core;
using Boreatek.Economy;
using Boreatek.Player;
using UnityEngine;

namespace Boreatek.World
{
    internal sealed partial class BoreatekBushPlane : MonoBehaviour, IInteractable
    {
        internal const float TankCapacity = 24f;
        internal const int CanLiters = 8;
        internal const int CanPrice = 10;
        private const float RotationSpeed = 16.5f;
        private const float TakeoffClearance = 1.65f;
        private readonly RaycastHit[] _hits = new RaycastHit[32];
        private readonly Vector3[] _exitOffsets =
        {
            new(1.5f, 0f, -1.15f), new(-1.5f, 0f, -1.15f), new(2f, 0f, -1.4f), new(-2f, 0f, -1.4f),
            new(1.5f, 0f, 1.2f), new(-1.5f, 0f, 1.2f)
        };

        private Transform _seat, _propeller, _elevator, _rudder, _leftAileron, _rightAileron;
        private Renderer _glazing;
        private AudioSource _engine, _wind;
        private PlayerController _pilot;
        private Vector3 _parkingPosition;
        private float _parkingHeading;
        private float _fuel = 9f;
        private float _speed, _verticalSpeed, _throttle, _heading, _pitch, _bank, _altitude, _rpm;
        private float _takeoffAssist;
        private bool _airborne;

        internal Transform Seat => _seat;
        internal bool CanExit => !_airborne && _speed < 0.85f && _collisionDrift.sqrMagnitude < 0.85f * 0.85f;
        internal bool PilotAboard => _pilot != null;

        [Serializable]
        internal struct SaveState
        {
            public bool valid, airborne, piloting, emergencyDescent;
            public Vector3 position;
            public float fuel, heading, pitch, bank, speed, climb, throttle, takeoffAssist;
            public bool hasHullState, wrecked;
            public float hull, burnSeconds;
            public Vector3 collisionDrift;
        }

        internal void Initialize(Transform seat, Renderer glazing, Transform propeller, Transform elevator,
            Transform rudder, Transform leftAileron, Transform rightAileron)
        {
            _seat = seat;
            _glazing = glazing;
            _propeller = propeller;
            _elevator = elevator;
            _rudder = rudder;
            _leftAileron = leftAileron;
            _rightAileron = rightAileron;
            _parkingPosition = transform.position;
            _parkingHeading = _heading = transform.eulerAngles.y;
            _engine = CreateLoop(propeller.gameObject, wind: false);
            _wind = CreateLoop(seat.gameObject, wind: true);
            InitializeDamageEffects();
        }

        public string GetInteractionPrompt()
        {
            if (_wrecked) return "Avioneta siniestrada · Solicita un reemplazo en el surtidor";
            string fire = Burning ? (_airborne ? $"¡Fuego! Aterriza · {_burnSeconds:0.0} s"
                : $"¡Fuego! Evacúa al detenerte · {_burnSeconds:0.0} s") : null;
            if (_pilot == null) return fire ?? "[E] Pilotar la avioneta";
            return (fire == null ? "" : fire + "\n") +
                $"Casco {_hull:0}%  ·  Gasolina {_fuel:0.0} L\n{_speed * 3.6f:0} km/h  ·  Altitud {_altitude:0} m";
        }

        public bool CanInteract(GameObject interactor) => _pilot == null && !_wrecked && !Burning && !_airborne && interactor != null &&
            (interactor.transform.position - _seat.position).sqrMagnitude <= 4.6f * 4.6f;

        public void Interact(GameObject interactor)
        {
            if (CanInteract(interactor) && interactor.TryGetComponent(out PlayerController player))
                player.TryMountPlane(this);
        }

        internal bool Board(PlayerController player, bool resuming)
        {
            if (_pilot != null || _wrecked || (!resuming && !CanInteract(player.gameObject))) return false;
            _pilot = player;
            return true;
        }

        internal void Release()
        {
            _pilot = null;
            _throttle = 0f;
            if (!_airborne)
            {
                _speed = _verticalSpeed = 0f;
                _collisionDrift = Vector3.zero;
            }
            SetCockpitView(false);
        }

        internal void SetCockpitView(bool inside) => _glazing.forceRenderingOff = inside || _wrecked;

        internal void Refill(InventoryAndShop inventory)
        {
            if (!CanExit || Burning || _wrecked)
            {
                _pilot.PublishStatus("Detén la avioneta en tierra para repostar.");
                return;
            }
            if (_fuel >= TankCapacity - 0.05f)
            {
                _pilot.PublishStatus("El tanque ya está lleno.");
                return;
            }
            if (!inventory.TryUseAviationCanister())
            {
                _pilot.PublishStatus("Compra bidones de gasolina en el surtidor de la estación aérea.");
                return;
            }
            float added = Mathf.Min(CanLiters, TankCapacity - _fuel);
            _fuel += added;
            BoreatekSoundscape.Play(SoundCue.PlaneRefuel, transform.position, 0.72f);
            _pilot.PublishStatus($"Repostaste {added:0.0} L de gasolina polar.");
        }

        internal void Fly(float powerChange, float nose, float turn, bool brake, float dt)
        {
            if (dt <= 0f) return;
            int steps = Mathf.CeilToInt(dt / 0.025f);
            float step = dt / steps;
            for (int i = 0; i < steps && !_wrecked && _pilot != null; i++)
                AdvanceFlight(powerChange, nose, turn, brake, step);
        }

        private void AdvanceFlight(float powerChange, float nose, float turn, bool brake, float dt)
        {
            UpdateImpactInertia(dt, brake);
            _throttle = brake && !_airborne ? 0f : Mathf.Clamp01(_throttle + powerChange * 0.70f * dt);
            if (Burning || _fuel <= 0f) _throttle = 0f;
            if (_throttle > 0.02f)
            {
                _fuel = Mathf.Max(0f, _fuel - (0.016f + 0.096f * _throttle) * dt);
                if (_fuel == 0f) _pilot?.PublishStatus("El tanque se vació. Baja el morro suavemente para planear.");
            }
            if (_fuel <= 0f) _throttle = 0f;

            _pitch = Mathf.MoveTowards(_pitch, nose * 22f, 26f * dt);
            _bank = Mathf.MoveTowards(_bank, _airborne ? turn * 38f : 0f, 48f * dt);
            float headRate = (_airborne ? _bank * 0.54f : turn * Mathf.Lerp(11f, 36f, _speed / 12f)) + _collisionYaw;
            _heading = Mathf.Repeat(_heading + headRate * dt, 360f);
            float engineEfficiency = Mathf.Lerp(0.5f, 1f, Mathf.InverseLerp(25f, 80f, _hull));
            float targetSpeed = _throttle * (_airborne ? 35f : 29f) * engineEfficiency;
            if (_airborne) targetSpeed = Mathf.Max(targetSpeed, 8.7f) - _pitch * 0.38f;
            _speed = Mathf.MoveTowards(_speed, Mathf.Max(0f, targetSpeed), (brake && !_airborne ? 13f :
                _airborne ? 3.35f : targetSpeed > _speed ? 7.4f : 2.6f) * dt);
            if (!_airborne && _speed >= RotationSpeed && _pitch >= 6f && _throttle >= 0.45f)
            {
                _airborne = true;
                _takeoffAssist = 0.92f;
                _verticalSpeed = 2.25f;
            }
            if (_airborne)
            {
                float lift = Mathf.InverseLerp(11f, 22f, _speed) * Mathf.Cos(_bank * Mathf.Deg2Rad);
                float climb = Mathf.Sin(_pitch * Mathf.Deg2Rad) * _speed * lift - Mathf.Lerp(6.6f, 0.48f, lift);
                if (Burning) climb = Mathf.Min(climb, -5.1f);
                if (_takeoffAssist > 0f) climb = Mathf.Max(climb, 2.45f);
                _verticalSpeed = Mathf.MoveTowards(_verticalSpeed, climb, 7.1f * dt);
            }

            Quaternion heading = Quaternion.Euler(0f, _heading, 0f);
            Vector3 travel = (heading * Vector3.forward * _speed + _collisionDrift) * dt;
            Vector3 next = transform.position + travel;
            if (_airborne) next.y = Mathf.Min(148f, next.y + _verticalSpeed * dt);
            bool hasGround = FindGround(next, out RaycastHit ground);
            bool touchingDown = _airborne && hasGround && next.y <= ground.point.y + 0.03f;
            Vector3 sweep = _airborne && !touchingDown ? next - transform.position : travel;
            if (TrySweepHull(sweep, hasGround ? ground.collider : null, out HullContact collision))
            {
                DeflectFromContact(collision, sweep, dt);
                return;
            }
            if (!_airborne)
            {
                if (hasGround && (ground.normal.y < 0.72f || ground.point.y > transform.position.y + 0.44f))
                {
                    HitRise(ground, travel, dt);
                    return;
                }
                if (!hasGround && _speed < 3.5f)
                {
                    StopTaxi();
                    return;
                }
                if (hasGround) next.y = ground.point.y + 0.025f;
                else
                {
                    _airborne = true;
                    _verticalSpeed = -1.1f;
                }
            }
            else if (touchingDown)
            {
                Land(next, ground);
                return;
            }

            _altitude = hasGround ? Mathf.Max(0f, next.y - ground.point.y) : Mathf.Max(0f, next.y);
            _takeoffAssist = Mathf.Max(0f, _takeoffAssist - dt);
            transform.SetPositionAndRotation(next, Attitude());
        }

        private Quaternion Attitude()
        {
            float clearance = Mathf.InverseLerp(0.05f, TakeoffClearance, _altitude);
            return Quaternion.Euler(_airborne ? -_pitch * clearance : 0f, _heading,
                (_airborne ? -_bank * clearance : 0f) + _collisionRoll);
        }

        private void StopTaxi()
        {
            if (_speed > 1.5f) _pilot?.PublishStatus("Paso obstruido. Gira hacia terreno despejado.");
            _speed = _throttle = 0f;
            _collisionDrift = Vector3.zero;
            transform.rotation = Attitude();
        }

        private void ReturnToStation()
        {
            _airborne = false;
            _speed = _pitch = _bank = _throttle = _verticalSpeed = _altitude = 0f;
            _takeoffAssist = 0f;
            _collisionDrift = Vector3.zero;
            _collisionYaw = _collisionRoll = 0f;
            _heading = _parkingHeading;
            transform.SetPositionAndRotation(_parkingPosition, Quaternion.Euler(0f, _heading, 0f));
        }

        private bool FindGround(Vector3 position, out RaycastHit ground)
        {
            Vector3 probe = position;
            probe.y = Mathf.Max(position.y, transform.position.y) + 1f;
            int count = Physics.RaycastNonAlloc(probe, Vector3.down, _hits, _airborne ? 260f : 3f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            ground = default;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];
                if (hit.collider.transform.IsChildOf(transform) || (_pilot != null && hit.collider.transform.IsChildOf(_pilot.transform)))
                    continue;
                if (ground.collider == null || hit.distance < ground.distance) ground = hit;
            }
            return ground.collider != null;
        }

        internal bool FindExit(out Vector3 landing)
        {
            foreach (Vector3 offset in _exitOffsets)
            {
                if (!FindGround(transform.TransformPoint(offset), out RaycastHit ground) || ground.normal.y < 0.72f) continue;
                landing = ground.point + Vector3.up * 0.045f;
                if (!Physics.CheckCapsule(landing + Vector3.up * 0.30f, landing + Vector3.up * 0.94f, 0.29f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return true;
            }
            landing = _parkingPosition + Vector3.back * 2.45f;
            return false;
        }

        internal SaveState CaptureSave() => new SaveState
        {
            valid = true, position = transform.position, fuel = _fuel, heading = _heading, pitch = _pitch, bank = _bank,
            speed = _speed, throttle = _throttle, climb = _verticalSpeed, airborne = _airborne, piloting = _pilot != null,
            takeoffAssist = _takeoffAssist, hasHullState = true, hull = _hull, wrecked = _wrecked,
            burnSeconds = _burnSeconds, collisionDrift = _collisionDrift
        };

        internal void RestoreSave(SaveState save)
        {
            if (!save.valid || !float.IsFinite(save.position.x) || !float.IsFinite(save.position.y) ||
                !float.IsFinite(save.position.z) || !float.IsFinite(save.heading)) return;
            _heading = Mathf.Repeat(save.heading, 360f);
            _fuel = float.IsFinite(save.fuel) ? Mathf.Clamp(save.fuel, 0f, TankCapacity) : 9f;
            RestoreHull(save);
            _airborne = save.airborne;
            _takeoffAssist = _airborne && !save.emergencyDescent ? Bound(save.takeoffAssist, 0f, 0.92f) : 0f;
            _pitch = _airborne ? Bound(save.pitch, -22f, 22f) : 0f;
            _bank = _airborne ? Bound(save.bank, -38f, 38f) : 0f;
            _speed = save.piloting ? Bound(save.speed, 0f, 45f) : 0f;
            _throttle = save.piloting ? Bound(save.throttle, 0f, 1f) : 0f;
            _verticalSpeed = _airborne ? Bound(save.climb, -23f, 23f) : 0f;
            if (_airborne && save.emergencyDescent) _verticalSpeed = Mathf.Min(-2.5f, _verticalSpeed);
            _altitude = _airborne ? Mathf.Max(0f, save.position.y -
                (FindGround(save.position, out RaycastHit ground) ? ground.point.y : 0f)) : 0f;
            transform.SetPositionAndRotation(save.position, Attitude());
        }

        private static float Bound(float value, float min, float max) => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : 0f;

        private static AudioSource CreateLoop(GameObject sourceObject, bool wind)
        {
            var source = sourceObject.AddComponent<AudioSource>();
            source.clip = BoreatekSoundBank.MakePlaneLoop(wind);
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = wind ? 0.20f : 0.85f;
            source.minDistance = 3f;
            source.maxDistance = 45f;
            source.dopplerLevel = 0.15f;
            source.volume = 0f;
            return source;
        }

        private void LateUpdate()
        {
            if (_engine == null) return;
            float dt = Time.deltaTime;
            float rpm = !Burning && !_wrecked && _throttle > 0.02f && _fuel > 0f
                ? (0.34f + _throttle * 0.66f) * (0.7f + _hull * 0.003f) : 0f;
            _rpm = Mathf.MoveTowards(_rpm, rpm, dt * 1.35f);
            float air = _airborne ? Mathf.InverseLerp(8f, 35f, _speed) : 0f;
            _propeller.Rotate(0f, 0f, Mathf.Max(_rpm * 3700f, air * 320f) * dt, Space.Self);
            _elevator.localRotation = Quaternion.Euler(_pitch * 0.68f, 0f, 0f);
            _rudder.localRotation = Quaternion.Euler(0f, _bank * 0.31f, 0f);
            _leftAileron.localRotation = Quaternion.Euler(_bank * 0.38f, 0f, 0f);
            _rightAileron.localRotation = Quaternion.Euler(-_bank * 0.38f, 0f, 0f);
            MixLoop(_engine, dt > 0f ? _rpm * 0.54f : 0f, 0.67f + _rpm * 0.67f);
            MixLoop(_wind, dt > 0f && _pilot != null ? air * 0.29f : 0f, 0.84f + air * 0.38f);
            UpdateDamageEffects();
        }

        private static void MixLoop(AudioSource source, float volume, float pitch)
        {
            source.pitch = pitch;
            source.volume = Mathf.MoveTowards(source.volume, volume, Time.unscaledDeltaTime * 1.6f);
            if (!source.isPlaying && source.volume > 0.005f) source.Play();
            if (source.isPlaying && source.volume <= 0.001f) source.Stop();
        }

        private void OnDestroy()
        {
            if (_engine != null) Destroy(_engine.clip);
            if (_wind != null) Destroy(_wind.clip);
            if (_damageMaterial != null) Destroy(_damageMaterial);
        }
    }
}
