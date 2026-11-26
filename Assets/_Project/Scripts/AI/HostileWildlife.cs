using Boreatek.Audio;
using Boreatek.Player;
using Boreatek.World;
using UnityEngine;

namespace Boreatek.AI
{
    internal sealed class HostileWildlife : MonoBehaviour, IEnemyDamageable
    {
        internal enum Species { SnowWolf, FrostMoth }
        private enum Activity { Roam, Hunt, Windup, Strike, Recover, Stagger, Defeated }

        private readonly RaycastHit[] _query = new RaycastHit[12];
        private CharacterController _body;
        private PlayerController _target;
        private TemperatureSystem _vitals;
        private Species _species;
        private Activity _activity;
        private Transform _rig, _head, _tail;
        private Transform[] _joints, _feet;
        private Vector3 _home, _patrolTarget, _strikeDirection, _recoil;
        private float _phase, _stride, _fall, _timer, _time, _nextPatrol, _strikeHeight, _avoid, _health;
        private float _circle = 1f;
        private int _patrolIndex;
        private bool _struck;
        private Vector3 _rigRest;

        private bool Flies => _species == Species.FrostMoth;
        internal float MaxHealth => Flies ? 48f : 70f;
        internal float CurrentHealth => _health;
        private float WarningTime => Flies ? 0.76f : 0.57f;
        private float AttackTime => Flies ? 0.51f : 0.42f;
        private string Label => Flies ? "Polilla de escarcha" : "Lobo nival";

        internal void Initialize(Species species, PlayerController target, CharacterController body,
            Transform rig, Transform head, Transform tail, Transform[] joints, Transform[] feet)
        {
            _species = species;
            _target = target;
            _vitals = target != null ? target.GetComponent<TemperatureSystem>() : null;
            _body = body;
            _rig = rig;
            _rigRest = rig.localPosition;
            _head = head;
            _tail = tail;
            _joints = joints;
            _feet = feet;
            _home = transform.position;
            _health = MaxHealth;
            _time = Mathf.Repeat(_home.x * 0.19f + _home.z * 0.38f, 8f);
            _circle = _time < 4f ? -1f : 1f;
            _patrolTarget = _home;
        }

        internal static bool InSanctuary(Vector3 position) => position.y > -1.5f &&
            (BoreatekWorldBuilder.IsVillageGround(position.x, position.z) ||
                BoreatekWorldBuilder.IsAirfieldClearance(position.x, position.z));

        private void Update()
        {
            if (_body == null || Time.deltaTime <= 0f) return;
            if (_activity == Activity.Defeated)
            {
                _timer += Time.deltaTime;
                Animate();
                if (_timer >= 1.10f) gameObject.SetActive(false);
                return;
            }
            if (_target == null || (_target.transform.position - transform.position).sqrMagnitude > 48f * 48f) return;
            Step(Time.deltaTime);
        }

        private void Step(float dt)
        {
            _time += dt;
            _timer += dt;
            Vector3 target = _target.transform.position;
            Vector3 delta = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
            float distance = delta.magnitude;
            Vector3 toward = distance > 0.01f ? delta / distance : transform.forward;
            bool chase = _vitals.CurrentHealth > 0f && !InSanctuary(target) &&
                Mathf.Abs(target.y - transform.position.y) < (Flies ? 2.8f : 1.85f) &&
                (target - _home).sqrMagnitude < 23f * 23f && distance <= (Flies ? 14f : 20f);
            Vector3 velocity = Vector3.zero;
            float hover = _home.y + Mathf.Sin(_time * 3.9f) * 0.13f;
            switch (_activity)
            {
                case Activity.Stagger:
                    velocity = _recoil;
                    _recoil = Vector3.MoveTowards(_recoil, Vector3.zero, dt * 14f);
                    if (_timer >= 0.33f) Enter(Activity.Recover);
                    break;
                case Activity.Windup:
                    Face(toward, dt);
                    if (Flies) hover += Mathf.SmoothStep(0f, 0.84f, _timer / WarningTime);
                    if (!chase || !ClearToTarget(target)) Enter(Activity.Roam);
                    else if (_timer >= WarningTime)
                    {
                        _strikeDirection = transform.forward;
                        _strikeHeight = target.y + 0.42f;
                        _struck = false;
                        Enter(Activity.Strike);
                        BoreatekSoundscape.Play(Flies ? SoundCue.MothDive : SoundCue.WolfLunge, transform.position, 0.76f);
                    }
                    break;
                case Activity.Strike:
                    velocity = _strikeDirection * ((Flies ? 7.9f : 8.7f) * Mathf.Lerp(1f, 0.30f, _timer / AttackTime));
                    if (Flies) hover = _strikeHeight;
                    if (chase && !_struck && _timer > 0.055f && distance < (Flies ? 1.22f : 1.36f) &&
                        Mathf.Abs(target.y + 0.42f - transform.position.y) < (Flies ? 0.90f : 1.07f) &&
                        Vector3.Dot(_strikeDirection, toward) > 0.02f && ClearToTarget(target))
                    {
                        _struck = true;
                        float damage = Flies ? 9f : 17f;
                        _vitals.ApplyCombatDamage(damage, transform.position);
                        _target.PublishStatus($"{Label} te golpeó. −{damage:0} HP.");
                    }
                    if (_timer >= AttackTime || !chase) Enter(Activity.Recover);
                    break;
                case Activity.Recover:
                    if (Flies && chase)
                    {
                        velocity = (Vector3.Cross(Vector3.up, toward) * _circle - toward * 0.4f) * 2.0f;
                        Face(velocity.normalized, dt);
                    }
                    if (_timer >= (Flies ? 1.45f : 1.10f)) Enter(chase ? Activity.Hunt : Activity.Roam);
                    break;
                default:
                    if (!chase || (_activity == Activity.Roam && !ClearToTarget(target)))
                    {
                        if (_activity != Activity.Roam) Enter(Activity.Roam);
                        velocity = Patrol(dt);
                    }
                    else
                    {
                        if (_activity != Activity.Hunt) Enter(Activity.Hunt);
                        if (distance <= (Flies ? 3.65f : 2.18f) && ClearToTarget(target))
                        {
                            Face(toward, dt);
                            Enter(Activity.Windup);
                            BoreatekSoundscape.Play(Flies ? SoundCue.MothWarning : SoundCue.WolfWarning, transform.position, 0.70f);
                        }
                        else
                        {
                            Vector3 move = Flies ? (toward + Vector3.Cross(Vector3.up, toward) * (_circle * 0.49f)).normalized : toward;
                            velocity = Steer(move, Flies ? 3.75f : 4.35f, dt);
                            if (Flies) hover = Mathf.Clamp(target.y + 1.02f, _home.y - 0.70f, _home.y + 1.35f);
                        }
                    }
                    break;
            }
            Move(velocity, hover, dt);
            Animate();
        }

        private Vector3 Patrol(float dt)
        {
            if (_time >= _nextPatrol)
            {
                float angle = _time * 0.91f + _patrolIndex++ * 2.39996f;
                float radius = Flies ? 3.8f : 6.7f;
                _patrolTarget = _home + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                _nextPatrol = _time + 4.3f;
            }
            Vector3 path = Vector3.ProjectOnPlane(_patrolTarget - transform.position, Vector3.up);
            if (Vector3.Distance(transform.position, _home) > 12f)
                path = Vector3.ProjectOnPlane(_home - transform.position, Vector3.up);
            return path.sqrMagnitude > 0.45f ? Steer(path.normalized, Flies ? 1.85f : 1.55f, dt) : Vector3.zero;
        }

        private Vector3 Steer(Vector3 direction, float speed, float dt)
        {
            if (_avoid > 0f)
            {
                _avoid -= dt;
                direction = Quaternion.Euler(0f, 61f * _circle, 0f) * direction;
            }
            Face(direction, dt);
            return direction * speed;
        }

        private void Move(Vector3 velocity, float hover, float dt)
        {
            Vector3 position = transform.position;
            Vector3 shift = velocity * dt;
            Vector3 next = position + shift;
            if (InSanctuary(next) || (next - _home).sqrMagnitude > 26f * 26f) shift = Vector3.zero;
            if (!Flies && shift.sqrMagnitude > 0.0001f && !HasFooting(next))
            {
                shift = Vector3.zero;
                _avoid = 0.48f;
            }
            if (Flies) shift.y = Mathf.Clamp((hover - position.y) * 4.6f, -4.4f, 3.4f) * dt;
            else
            {
                _fall = _body.isGrounded ? -1.4f : Mathf.Max(-18f, _fall - 19f * dt);
                shift.y = _fall * dt;
            }
            CollisionFlags collision = _body.Move(shift);
            float travel = Vector3.ProjectOnPlane(transform.position - position, Vector3.up).magnitude;
            if ((collision & CollisionFlags.Sides) != 0 && travel < shift.magnitude * 0.52f) _avoid = 0.58f;
            _stride = Mathf.MoveTowards(_stride, Mathf.Clamp01(travel / Mathf.Max(0.01f, dt) * 0.30f), dt * 6f);
            _phase += dt * (Flies ? 22f + _stride * 13f : 2f + _stride * 12f);
        }

        private bool HasFooting(Vector3 position)
        {
            int count = Physics.RaycastNonAlloc(position + Vector3.up * 0.52f, Vector3.down, _query, 1.45f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = _query[i];
                if (hit.normal.y >= 0.59f && !hit.collider.transform.IsChildOf(transform) &&
                    hit.collider.GetComponentInParent<IEnemyDamageable>() == null &&
                    hit.collider.GetComponentInParent<PlayerController>() == null) return true;
            }
            return false;
        }

        private bool ClearToTarget(Vector3 target)
        {
            Vector3 origin = transform.position + Vector3.up * (Flies ? 0.12f : 0.54f);
            Vector3 ray = target + Vector3.up * 0.58f - origin;
            int count = Physics.RaycastNonAlloc(origin, ray.normalized, _query, ray.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Transform obstacle = _query[i].collider.transform;
                if (!obstacle.IsChildOf(transform) && !obstacle.IsChildOf(_target.transform)) return false;
            }
            return true;
        }

        private void Face(Vector3 heading, float dt)
        {
            if (heading.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(heading), 430f * dt);
        }

        private void Enter(Activity activity)
        {
            _activity = activity;
            _timer = 0f;
        }

        private void Animate()
        {
            float warned = _activity == Activity.Windup ? Mathf.SmoothStep(0f, 1f, _timer / WarningTime) : 0f;
            float lunged = _activity == Activity.Strike ? Mathf.Sin(Mathf.PI * Mathf.Clamp01(_timer / AttackTime)) : 0f;
            float flinched = _activity == Activity.Stagger ? Mathf.Sin(Mathf.PI * Mathf.Clamp01(_timer / 0.33f)) : 0f;
            float collapsed = _activity == Activity.Defeated ? Mathf.SmoothStep(0f, 1f, _timer / 0.69f) : 0f;
            if (Flies)
            {
                float flap = Mathf.Sin(_phase) * Mathf.Lerp(42f, 63f, warned) * (1f - collapsed) + collapsed * 55f;
                _joints[0].localRotation = Quaternion.Euler(0f, 0f, flap);
                _joints[1].localRotation = Quaternion.Euler(0f, 0f, -flap);
                _rig.localPosition = _rigRest + Vector3.down * (collapsed * 0.50f);
                _rig.localRotation = Quaternion.Euler(18f * lunged - 12f * warned, 0f, collapsed * 104f + 21f * flinched);
                _head.localRotation = Quaternion.Euler(7f * Mathf.Sin(_time * 4f), 0f, 0f);
                return;
            }
            float bob = Mathf.Abs(Mathf.Sin(_phase)) * _stride * 0.060f;
            _rig.localPosition = _rigRest + Vector3.up * (bob - warned * 0.115f + lunged * 0.090f - collapsed * 0.23f);
            _rig.localRotation = Quaternion.Euler(12f * warned - 22f * lunged, 0f, collapsed * 83f + 18f * flinched);
            _head.localRotation = Quaternion.Euler(-20f * warned + 17f * lunged, Mathf.Sin(_time * 1.75f) * 10f * (1f - _stride), 0f);
            _tail.localRotation = Quaternion.Euler(-15f - warned * 19f, 18f * Mathf.Sin(_time * (2.3f + 3.5f * _stride)), 0f);
            for (int i = 0; i < _joints.Length; i++)
            {
                float lag = i == 0 || i == 3 ? 0f : Mathf.PI;
                float reach = (i < 2 ? -37f : 34f) * lunged;
                _joints[i].localRotation = Quaternion.Euler(Mathf.Sin(_phase + lag) * _stride * 39f + reach - warned * 13f, 0f, 0f);
                _feet[i].localRotation = Quaternion.Euler(Mathf.Max(0f, Mathf.Cos(_phase + lag)) * _stride * 30f + warned * 18f, 0f, 0f);
            }
        }

        public void TakePickaxeDamage(float damage, int pickaxeTier, PlayerController attacker) =>
            Hit(damage, attacker != null ? attacker.transform.position : transform.position + transform.forward, attacker);

        public void TakeSwordDamage(PlayerController attacker) =>
            Hit(45f, attacker != null ? attacker.transform.position : transform.position + transform.forward, attacker);

        public void TakeBlastDamage(float damage, Vector3 center, PlayerController attacker) => Hit(damage, center, attacker);

        private void Hit(float amount, Vector3 center, PlayerController attacker)
        {
            if (_health <= 0f || amount <= 0f) return;
            _health = Mathf.Max(0f, _health - amount);
            if (_health > 0f)
            {
                Vector3 away = Vector3.ProjectOnPlane(transform.position - center, Vector3.up);
                _recoil = (away.sqrMagnitude > 0.01f ? away.normalized : -transform.forward) * 3.7f;
                Enter(Activity.Stagger);
                BoreatekSoundscape.Play(Flies ? SoundCue.MothWarning : SoundCue.WolfLunge, transform.position, 0.53f);
                attacker?.PublishStatus($"{Label} ({Mathf.CeilToInt(_health)}/{MaxHealth:0} HP).");
                return;
            }
            Enter(Activity.Defeated);
            _body.enabled = false;
            BoreatekSoundscape.Play(Flies ? SoundCue.MothFade : SoundCue.WolfFall, transform.position, 0.72f);
            attacker?.PublishStatus($"{Label} fuera de combate.");
        }

        internal void RestoreHealth(float health)
        {
            _health = Mathf.Clamp(health, 0f, MaxHealth);
            _body.enabled = _health > 0f;
            gameObject.SetActive(_health > 0f);
            Enter(_health > 0f ? Activity.Roam : Activity.Defeated);
        }
    }
}
