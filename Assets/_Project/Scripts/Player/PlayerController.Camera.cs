using UnityEngine;

namespace Boreatek.Player
{
    public partial class PlayerController
    {
        private enum Perspective { Legacy, FirstPerson, Behind, Front, Shoulder }
        private static readonly Vector3 ShoulderOffset = new Vector3(0.70f, 0.11f, -1.94f);
        private static readonly Vector3 FrontOffset = new Vector3(0f, 0.22f, 2.60f);
        private static readonly float[] FrontFallbackAngles = { -42f, 42f, -68f, 68f };
        private readonly RaycastHit[] _viewObstructions = new RaycastHit[12];
        private Perspective _perspective;
        private float _standardFieldOfView = 60f;

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
            UpdateCameraObstruction();
        }

        private void UpdateCameraObstruction()
        {
            bool showHead = !InFirstPerson;
            if (_playerCamera != null && _cameraPivot != null)
            {
                Transform camera = _playerCamera.transform;
                switch (CurrentPerspective)
                {
                    case Perspective.FirstPerson:
                        camera.SetLocalPositionAndRotation(_firstPersonCameraOffset, Quaternion.identity);
                        break;
                    case Perspective.Front:
                        showHead = PositionFrontCamera(camera);
                        break;
                    default:
                    {
                        Vector3 offset = CurrentPerspective == Perspective.Shoulder ? ShoulderOffset : _thirdPersonCameraOffset;
                        Vector3 displacement = _cameraPivot.TransformDirection(offset);
                        float clear = ViewClearance(_cameraPivot.position, displacement, 0.16f);
                        camera.SetLocalPositionAndRotation(offset.normalized * clear, Quaternion.identity);
                        showHead = clear > 0.57f;
                        break;
                    }
                }
            }
            if (_thirdPersonHead != null) _thirdPersonHead.enabled = showHead;
            RefreshHeadlampLens();
        }

        private bool PositionFrontCamera(Transform camera)
        {
            // The front portrait follows character yaw. Looking up or down only steers the gameplay aim.
            Vector3 target = transform.position + Vector3.up * 0.70f;
            Vector3 offset = transform.TransformDirection(FrontOffset);
            float clear = ViewClearance(target, offset, 0.16f);
            if (clear < 1.45f)
            {
                foreach (float angle in FrontFallbackAngles)
                {
                    Vector3 candidate = Quaternion.AngleAxis(angle, Vector3.up) * transform.TransformDirection(FrontOffset);
                    float distance = ViewClearance(target, candidate, 0.16f);
                    if (distance <= clear + 0.14f) continue;
                    offset = candidate;
                    clear = distance;
                }
            }
            Vector3 direction = offset.normalized;
            camera.SetPositionAndRotation(target + direction * clear, Quaternion.LookRotation(-direction, Vector3.up));
            return clear > 0.51f;
        }

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
                if (hit.collider.transform.IsChildOf(transform)) continue;
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
