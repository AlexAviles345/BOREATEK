using UnityEngine;

namespace Boreatek.Player
{
    public partial class PlayerController
    {
        private enum Perspective { Legacy, FirstPerson, Behind, Front, Shoulder }
        private static readonly Vector3 ShoulderOffset = new Vector3(0.70f, 0.11f, -1.94f);
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
