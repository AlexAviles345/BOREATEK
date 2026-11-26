using UnityEngine;

namespace Boreatek.Player
{
    public partial class PlayerController
    {
        private enum Perspective { Legacy, FirstPerson, Behind, Front, Shoulder }
        private static readonly Vector3 ShoulderOffset = new Vector3(0.70f, 0.11f, -1.94f);
        private readonly RaycastHit[] _viewObstructions = new RaycastHit[24];
        private Perspective _perspective;
        private float _standardFieldOfView = 60f;
        private Renderer[] _bodyRenderers = System.Array.Empty<Renderer>();
        private Vector3 _sledCameraBoom;

        private Perspective CurrentPerspective => _perspective == Perspective.Legacy
            ? (_useThirdPersonView ? Perspective.Behind : Perspective.FirstPerson)
            : _perspective;
        private bool InFirstPerson => CurrentPerspective == Perspective.FirstPerson;
        internal bool HasAimReticle => CurrentPerspective != Perspective.Front;

        private void CyclePerspective()
        {
            _perspective = CurrentPerspective switch
            {
                Perspective.FirstPerson => Perspective.Behind,
                Perspective.Behind => Perspective.Front,
                Perspective.Front => Perspective.Shoulder,
                _ => Perspective.FirstPerson
            };
            UpdateCameraPositionImmediate();
            PublishStatus(_perspective switch
            {
                Perspective.Behind => "Cámara en tercera persona desde atrás.",
                Perspective.Front => "Cámara en tercera persona de frente.",
                Perspective.Shoulder => "Cámara sobre el hombro derecho.",
                _ => "Cámara en primera persona."
            });
        }

        private void UpdateCameraPositionImmediate()
        {
            RefreshPickaxeView();
            if (_playerCamera != null && _cameraPivot != null)
            {
                _playerCamera.transform.SetParent(_cameraPivot, false);
                _playerCamera.fieldOfView = CurrentPerspective == Perspective.Shoulder
                    ? _standardFieldOfView * 0.88f : _standardFieldOfView;
            }
            if (_mountedSled == null) SetCameraBodyVisible(!InFirstPerson);
            _sledCameraBoom = Vector3.zero;
            UpdateCameraObstruction();
            RefreshHeldEquipment();
        }

        private void UpdateCameraObstruction(float dt = 0f)
        {
            bool showHead = !InFirstPerson;
            if (_playerCamera != null && _cameraPivot != null)
            {
                Transform camera = _playerCamera.transform;
                if (_mountedSled != null)
                {
                    showHead = UpdateMountedCamera(camera, dt);
                    SetCameraBodyVisible(showHead);
                }
                else switch (CurrentPerspective)
                {
                    case Perspective.FirstPerson:
                        camera.SetLocalPositionAndRotation(_firstPersonCameraOffset, Quaternion.identity);
                        break;
                    default:
                    {
                        bool front = CurrentPerspective == Perspective.Front;
                        Vector3 offset = CurrentPerspective switch
                        {
                            Perspective.Front => new Vector3(0f, 0.08f, -_thirdPersonCameraOffset.z),
                            Perspective.Shoulder => ShoulderOffset,
                            _ => _thirdPersonCameraOffset
                        };
                        Vector3 displacement = _cameraPivot.TransformDirection(offset);
                        float clear = ViewClearance(_cameraPivot.position, displacement, 0.16f);
                        camera.SetLocalPositionAndRotation(offset.normalized * clear,
                            front ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity);
                        showHead = clear > 0.57f;
                        break;
                    }
                }
            }
            if (_thirdPersonHead != null) _thirdPersonHead.enabled = showHead;
            RefreshHeadlampLens();
        }

        private bool UpdateMountedCamera(Transform camera, float dt)
        {
            const float cameraRadius = 0.20f;
            Vector3 anchor = transform.position + Vector3.up * 0.93f;
            Quaternion heading = Quaternion.Euler(0f, _playerYaw + _sledLookYaw, 0f);
            if (!InFirstPerson)
            {
                bool front = CurrentPerspective == Perspective.Front;
                bool shoulder = CurrentPerspective == Perspective.Shoulder;
                if (front) heading = Quaternion.Euler(0f, _playerYaw + Mathf.Clamp(_sledLookYaw, -50f, 50f), 0f);
                Vector3 rest = front ? new Vector3(0f, 0.4f, 4.05f) :
                    shoulder ? new Vector3(0.81f, 0.42f, -2.35f) : new Vector3(0f, 0.64f, -3.75f);
                float lift = Mathf.Sin(Mathf.Clamp(_sledLookPitch, -14f, 22f) * Mathf.Deg2Rad) * Mathf.Abs(rest.z);
                rest.y = Mathf.Max(0.18f, rest.y + lift);
                Vector3 desired = heading * rest;
                Vector3 candidate = ClampViewOffset(anchor, desired, cameraRadius);
                if (candidate.sqrMagnitude < 1.85f * 1.85f)
                {
                    // Try keeping the rider in frame when the usual boom is against a corner.
                    Vector3 right = heading * Vector3.right * (shoulder ? 0.75f : 0.95f);
                    Vector3 raised = Vector3.up * 0.52f;
                    Vector3 alternate = ClampViewOffset(anchor, desired + right + raised, cameraRadius);
                    if (alternate.sqrMagnitude > candidate.sqrMagnitude + 0.08f) candidate = alternate;
                    alternate = ClampViewOffset(anchor, desired - right + raised, cameraRadius);
                    if (alternate.sqrMagnitude > candidate.sqrMagnitude + 0.08f) candidate = alternate;
                }
                Vector3 boom = dt <= 0f || _sledCameraBoom == Vector3.zero ? candidate :
                    Vector3.Lerp(_sledCameraBoom, candidate, 1f - Mathf.Exp(-13f * dt));
                _sledCameraBoom = ClampViewOffset(anchor, boom, cameraRadius);
                if (_sledCameraBoom.sqrMagnitude > 0.82f * 0.82f)
                {
                    Vector3 target = anchor + heading * (front ? new Vector3(0f, -0.13f, -0.12f) :
                        new Vector3(shoulder ? 0.78f : 0f, 0.06f, shoulder ? 2.45f : 1.55f));
                    camera.SetPositionAndRotation(anchor + _sledCameraBoom,
                        Quaternion.LookRotation(target - anchor - _sledCameraBoom));
                    return true;
                }
            }

            Vector3 eye = ClampViewOffset(anchor, heading * Vector3.forward * 0.17f, 0.11f);
            camera.SetPositionAndRotation(anchor + eye, heading * Quaternion.Euler(_sledLookPitch, 0f, 0f));
            return false;
        }

        private void SetCameraBodyVisible(bool visible)
        {
            foreach (Renderer renderer in _bodyRenderers)
                if (renderer != null) renderer.forceRenderingOff = !visible;
        }

        private Vector3 ClampViewOffset(Vector3 origin, Vector3 displacement, float radius) =>
            displacement.normalized * ViewClearance(origin, displacement, radius);

        private float ViewClearance(Vector3 origin, Vector3 displacement, float radius)
        {
            float distance = displacement.magnitude;
            if (distance < 0.001f) return 0f;
            int count = Physics.SphereCastNonAlloc(origin, radius, displacement / distance, _viewObstructions, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float nearest = distance;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _viewObstructions[i];
                Transform obstacle = hit.collider.transform;
                if (obstacle.IsChildOf(transform) || (_mountedSled != null && obstacle.IsChildOf(_mountedSled.transform))) continue;
                nearest = Mathf.Min(nearest, Mathf.Max(0.04f, hit.distance - 0.065f));
            }
            return nearest;
        }

        private Transform AimFrame => CurrentPerspective == Perspective.Front && _cameraPivot != null
            ? _cameraPivot : _playerCamera != null ? _playerCamera.transform : transform;

        private float CameraAimReach(Transform frame)
        {
            if (InFirstPerson || CurrentPerspective == Perspective.Front || _cameraPivot == null) return _miningReachDistance;
            return _miningReachDistance + Mathf.Max(0f, Vector3.Dot(_cameraPivot.position - frame.position, frame.forward));
        }

        private void RestorePerspective(SaveState state)
        {
            _useThirdPersonView = state.thirdPerson;
            _perspective = state.cameraPerspective >= (int)Perspective.FirstPerson &&
                state.cameraPerspective <= (int)Perspective.Shoulder
                ? (Perspective)state.cameraPerspective : Perspective.Legacy;
        }
    }
}
