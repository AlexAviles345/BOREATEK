using UnityEngine;

namespace Boreatek.Player
{
    public partial class PlayerController
    {
        private enum Perspective { Legacy, FirstPerson, Behind, Front }
        private Perspective _perspective;

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
                _ => Perspective.FirstPerson
            };
            UpdateCameraPositionImmediate();
            PublishStatus(_perspective switch
            {
                Perspective.Behind => "Cámara en tercera persona desde atrás.",
                Perspective.Front => "Cámara en tercera persona de frente.",
                _ => "Cámara en primera persona."
            });
        }

        private void UpdateCameraPositionImmediate()
        {
            RefreshPickaxeView();
            if (_playerCamera != null && _cameraPivot != null)
            {
                _playerCamera.transform.SetParent(_cameraPivot, false);
                _playerCamera.transform.localRotation = CurrentPerspective == Perspective.Front
                    ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            }
            UpdateCameraObstruction();
        }

        private void UpdateCameraObstruction()
        {
            bool showHead = !InFirstPerson;
            if (_playerCamera != null && _cameraPivot != null)
            {
                Vector3 offset = _firstPersonCameraOffset;
                if (!InFirstPerson)
                {
                    offset = CurrentPerspective == Perspective.Front
                        ? new Vector3(0f, 0.08f, -_thirdPersonCameraOffset.z) : _thirdPersonCameraOffset;
                    float distance = offset.magnitude;
                    if (distance > 0.01f)
                    {
                        Vector3 direction = _cameraPivot.TransformDirection(offset / distance);
                        if (Physics.SphereCast(_cameraPivot.position, 0.19f, direction, out RaycastHit hit, distance,
                                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                            hit.collider != _characterController)
                        {
                            float clearance = Mathf.Max(0.05f, hit.distance - 0.07f);
                            offset *= clearance / distance;
                            showHead = clearance > 0.75f;
                        }
                    }
                }
                _playerCamera.transform.localPosition = offset;
            }
            if (_thirdPersonHead != null) _thirdPersonHead.enabled = showHead;
            RefreshHeadlampLens();
        }

        private Transform AimFrame => CurrentPerspective == Perspective.Front && _cameraPivot != null
            ? _cameraPivot : _playerCamera != null ? _playerCamera.transform : transform;

        private float CameraAimReach(Transform frame)
        {
            if (CurrentPerspective != Perspective.Behind || _cameraPivot == null) return _miningReachDistance;
            return _miningReachDistance + Mathf.Max(0f, Vector3.Dot(_cameraPivot.position - frame.position, frame.forward));
        }

        private void RestorePerspective(SaveState state)
        {
            _useThirdPersonView = state.thirdPerson;
            _perspective = state.cameraPerspective >= (int)Perspective.FirstPerson &&
                state.cameraPerspective <= (int)Perspective.Front
                ? (Perspective)state.cameraPerspective : Perspective.Legacy;
        }
    }
}
