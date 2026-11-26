using Boreatek.Audio;
using Boreatek.Core;
using Boreatek.Player;
using UnityEngine;

namespace Boreatek.World
{
    internal sealed class BoreatekVillageSled : MonoBehaviour, IInteractable
    {
        private CharacterController _body;
        private Transform _seat;
        private SledAnimal[] _team;
        private PlayerController _rider;
        private string _id, _label;
        private float _topSpeed, _steering, _speed, _gravity, _bellDistance;
        private readonly RaycastHit[] _hits = new RaycastHit[12];
        private static readonly Vector3[] LandingOffsets = { Vector3.right * 1.38f, Vector3.left * 1.38f, Vector3.back * 1.35f };
        internal Transform Seat => _seat;
        internal bool Moving => Mathf.Abs(_speed) > 1.1f;

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

        public string GetInteractionPrompt() => _rider == null ? $"[E] Montar {_label}" :
            "[W/S] Avanzar o retroceder · [A/D] Girar · [Espacio] Frenar · [E] Bajar";

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
            _speed = 0f;
            _gravity = 0f;
        }

        internal void Drive(float throttle, float steering, bool brake, float dt)
        {
            float target = brake ? 0f : throttle * (throttle < 0f ? 2.5f : _topSpeed);
            _speed = Mathf.MoveTowards(_speed, target, (brake ? 14f : Mathf.Abs(target) > Mathf.Abs(_speed) ? 5.4f : 3.1f) * dt);
            if (Mathf.Abs(_speed) > 0.14f || (!brake && Mathf.Abs(throttle) > 0.2f))
            {
                float turn = steering * _steering * Mathf.Lerp(0.34f, 1f, Mathf.Abs(_speed) / 5f);
                float directionSign = Mathf.Sign(Mathf.Abs(_speed) > 0.005f ? _speed : throttle);
                transform.Rotate(0f, turn * directionSign * dt, 0f, Space.World);
            }
            Vector3 direction = transform.forward * Mathf.Sign(_speed);
            float travel = Mathf.Abs(_speed) * dt;
            if (_speed > 0f && travel > 0.001f)
            {
                foreach (SledAnimal animal in _team)
                {
                    Vector3 origin = animal.transform.position + Vector3.up * 0.46f;
                    int count = Physics.SphereCastNonAlloc(origin, 0.26f, direction, _hits, travel + 0.42f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < count; i++)
                    {
                        RaycastHit hit = _hits[i];
                        if (hit.collider.transform.IsChildOf(transform) || hit.normal.y >= 0.63f) continue;
                        travel = Mathf.Min(travel, Mathf.Max(0f, hit.distance - 0.27f));
                    }
                }
            }
            if (_body.isGrounded && _gravity < 0f) _gravity = -2f;
            _gravity = Mathf.Max(-16f, _gravity - 19f * dt);
            Vector3 before = transform.position;
            _body.Move(direction * travel + Vector3.up * (_gravity * dt));
            float actual = Vector3.ProjectOnPlane(transform.position - before, Vector3.up).magnitude;
            if (actual + 0.025f < Mathf.Abs(_speed) * dt) _speed = Mathf.Sign(_speed) * actual / Mathf.Max(dt, 0.001f);
            _bellDistance += actual;
            if (_bellDistance >= 5.5f)
            {
                _bellDistance %= 5.5f;
                BoreatekSoundscape.Play(SoundCue.SledBell, transform.position, 0.34f);
            }
        }

        internal bool FindLanding(out Vector3 landing)
        {
            foreach (Vector3 offset in LandingOffsets)
            {
                Vector3 probe = transform.TransformPoint(offset) + Vector3.up * 0.92f;
                if (!Physics.Raycast(probe, Vector3.down, out RaycastHit floor, 2.6f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || floor.normal.y < 0.65f) continue;
                landing = floor.point + Vector3.up * 0.06f;
                if (!Physics.CheckCapsule(landing + Vector3.up * 0.27f, landing + Vector3.up * 0.91f, 0.25f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return true;
            }
            landing = transform.position + Vector3.back * 1.2f;
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
            foreach (SledAnimal animal in _team) animal.Animate(Mathf.Abs(_speed), Time.deltaTime);
        }
    }
}
