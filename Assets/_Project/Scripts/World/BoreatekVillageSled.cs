using Boreatek.Audio;
using Boreatek.Core;
using Boreatek.Player;
using UnityEngine;

namespace Boreatek.World
{
    internal sealed class BoreatekVillageSled : MonoBehaviour, IInteractable
    {
        private const float AnimalRadius = 0.24f;
        private const float AnimalClearance = 0.07f;
        private const float WalkableNormalY = 0.64f;
        private CharacterController _body;
        private Transform _seat;
        private SledAnimal[] _team;
        private PlayerController _rider;
        private string _id, _label;
        private float _topSpeed, _steering, _speed, _groundSpeed, _turnSpeed, _gravity, _bellDistance;
        private readonly RaycastHit[] _hits = new RaycastHit[24];
        private static readonly Vector3[] LandingDirections =
        {
            Vector3.right, Vector3.left, Vector3.back, new Vector3(0.71f, 0f, -0.71f), new Vector3(-0.71f, 0f, -0.71f)
        };
        internal Transform Seat => _seat;
        internal bool Moving => _groundSpeed > 1.1f;

        [System.Serializable]
        internal struct SaveState
        {
            public string id;
            public Vector3 position;
            public float yaw;
        }

        internal void Initialize(string id, string label, CharacterController body, Transform seat, SledAnimal[] animals,
            float topSpeed, float steering)
        {
            _id = id;
            _label = label;
            _body = body;
            _seat = seat;
            _team = animals;
            _topSpeed = topSpeed;
            _steering = steering;
        }

        public string GetInteractionPrompt() => _rider == null ? $"[E] Montar · {_label}" : _label;

        public bool CanInteract(GameObject interactor) => _rider == null &&
            interactor != null && (interactor.transform.position - transform.position).sqrMagnitude < 3.6f * 3.6f;

        public void Interact(GameObject interactor)
        {
            if (CanInteract(interactor) && interactor.TryGetComponent(out PlayerController player))
                player.TryMountSled(this);
        }

        internal bool AttachRider(PlayerController player)
        {
            if (_rider != null || _body == null || !CanInteract(player.gameObject)) return false;
            _rider = player;
            Physics.IgnoreCollision(_body, player.GetComponent<CharacterController>(), true);
            return true;
        }

        internal void ReleaseRider()
        {
            if (_rider != null && _rider.TryGetComponent(out CharacterController riderBody))
                Physics.IgnoreCollision(_body, riderBody, false);
            _rider = null;
            _speed = _groundSpeed = _turnSpeed = 0f;
            _gravity = 0f;
        }

        internal void Drive(float throttle, float steering, bool brake, float dt)
        {
            float target = brake ? 0f : throttle * (throttle < 0f ? 3.8f : _topSpeed);
            _speed = Mathf.MoveTowards(_speed, target, (brake ? 14f : Mathf.Abs(target) > Mathf.Abs(_speed) ? 5.4f : 3.1f) * dt);
            float directionSign = Mathf.Sign(Mathf.Abs(_speed) > 0.15f ? _speed : throttle);
            float turn = steering * _steering * Mathf.Lerp(0.53f, 1f, Mathf.Abs(_speed) / 5f) * directionSign * dt;
            _turnSpeed = Mathf.Abs(SteerTeam(turn)) * (2.65f * Mathf.Deg2Rad / Mathf.Max(dt, 0.001f));
            Vector3 requested = transform.forward * (_speed * dt);
            Vector3 travel = SlideTeam(requested);
            if (_body.isGrounded && _gravity < 0f) _gravity = -2f;
            _gravity = Mathf.Max(-16f, _gravity - 19f * dt);
            Vector3 before = transform.position;
            _body.Move(travel + Vector3.up * (_gravity * dt));
            float actual = Vector3.ProjectOnPlane(transform.position - before, Vector3.up).magnitude;
            _groundSpeed = actual / Mathf.Max(dt, 0.001f);
            if (actual < 0.001f || actual + 0.006f < travel.magnitude) _speed = Mathf.Sign(_speed) * _groundSpeed;
            _bellDistance += actual;
            if (_bellDistance >= 5.5f)
            {
                _bellDistance %= 5.5f;
                BoreatekSoundscape.Play(SoundCue.SledBell, transform.position, 0.34f);
            }
        }

        private float SteerTeam(float turn)
        {
            if (Mathf.Abs(turn) < 0.001f) return 0f;
            Quaternion rotation = Quaternion.Euler(0f, turn, 0f);
            float fraction = 1f;
            foreach (SledAnimal animal in _team)
            {
                Vector3 center = animal.CollisionCenter;
                Vector3 lever = center - transform.position;
                Vector3 swing = rotation * lever - lever;
                if (AnimalContact(center, animal.transform.position.y, swing, out RaycastHit hit))
                    fraction = Mathf.Min(fraction, Mathf.Clamp01((hit.distance - AnimalClearance) / swing.magnitude));
            }
            transform.Rotate(0f, turn * fraction, 0f, Space.World);
            return turn * fraction;
        }

        private Vector3 SlideTeam(Vector3 travel)
        {
            if (!TeamContact(Vector3.zero, travel, out RaycastHit hit)) return travel;
            Vector3 forward = travel.normalized * Mathf.Max(0f, hit.distance - AnimalClearance);
            Vector3 wall = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;
            Vector3 slide = Vector3.ProjectOnPlane(travel - forward, wall);
            if (TeamContact(forward, slide, out RaycastHit sideHit))
                slide = slide.normalized * Mathf.Max(0f, sideHit.distance - AnimalClearance);
            return forward + slide;
        }

        private bool TeamContact(Vector3 offset, Vector3 travel, out RaycastHit nearest)
        {
            nearest = default;
            foreach (SledAnimal animal in _team)
            {
                if (AnimalContact(animal.CollisionCenter + offset, animal.transform.position.y, travel, out RaycastHit hit) &&
                    (nearest.collider == null || hit.distance < nearest.distance)) nearest = hit;
            }
            return nearest.collider != null;
        }

        private bool AnimalContact(Vector3 origin, float footY, Vector3 travel, out RaycastHit nearest)
        {
            nearest = default;
            float distance = travel.magnitude;
            if (distance < 0.001f) return false;
            Vector3 direction = travel / distance;
            int count = Physics.SphereCastNonAlloc(origin, AnimalRadius, direction, _hits, distance + AnimalClearance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.distance <= 0.001f)
                {
                    // A sweep that starts overlapping reports no surface point and a fictitious opposing normal.
                    Vector3 behind = origin - direction * (AnimalRadius + AnimalClearance);
                    if (!hit.collider.Raycast(new Ray(behind, direction), out hit, AnimalRadius * 2f + AnimalClearance)) continue;
                    hit.distance = 0f;
                }
                if (hit.normal.y >= WalkableNormalY ||
                    Vector3.Dot(hit.normal, direction) >= -0.08f || IsShallowStep(hit, footY, direction)) continue;
                if (nearest.collider == null || hit.distance < nearest.distance) nearest = hit;
            }
            return nearest.collider != null;
        }

        private bool IsShallowStep(RaycastHit hit, float footY, Vector3 direction)
        {
            float maxStep = _body.stepOffset - 0.045f;
            if (hit.point.y > footY + maxStep) return false;
            Vector3 probe = hit.point + direction * 0.13f;
            probe.y = footY + maxStep + 0.045f;
            return Physics.Raycast(probe, Vector3.down, out RaycastHit top, maxStep + 0.16f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                top.collider == hit.collider && top.normal.y >= WalkableNormalY && top.point.y <= footY + maxStep;
        }

        internal bool FindLanding(out Vector3 landing)
        {
            for (float distance = 1.35f; distance <= 2.36f; distance += 0.5f)
            {
                foreach (Vector3 side in LandingDirections)
                {
                    Vector3 probe = transform.TransformPoint(side * distance) + Vector3.up * 0.98f;
                    if (!Physics.Raycast(probe, Vector3.down, out RaycastHit floor, 2.8f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || floor.normal.y < WalkableNormalY) continue;
                    landing = floor.point + Vector3.up * 0.06f;
                    if (!Physics.CheckCapsule(landing + Vector3.up * 0.29f, landing + Vector3.up * 0.94f, 0.28f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return true;
                }
            }
            landing = transform.position - transform.forward * 1.2f;
            return false;
        }

        internal SaveState CaptureSave() => new SaveState { id = _id, position = transform.position, yaw = transform.eulerAngles.y };

        internal void RestoreSave(SaveState state)
        {
            if (state.id != _id || !float.IsFinite(state.position.x) || !float.IsFinite(state.position.y) ||
                !float.IsFinite(state.position.z) || !float.IsFinite(state.yaw)) return;
            _body.enabled = false;
            transform.SetPositionAndRotation(state.position, Quaternion.Euler(0f, state.yaw, 0f));
            _body.enabled = true;
        }

        private void LateUpdate()
        {
            if (_team == null || Time.deltaTime <= 0f) return;
            float strideSpeed = Mathf.Max(_groundSpeed, _turnSpeed) * Mathf.Sign(_speed);
            foreach (SledAnimal animal in _team) animal.Animate(strideSpeed, Time.deltaTime);
        }
    }
}
