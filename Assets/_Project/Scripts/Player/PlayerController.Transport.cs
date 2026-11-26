using Boreatek.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Boreatek.Player
{
    public partial class PlayerController
    {
        private BoreatekVillageSled _mountedSled;
        private float _sledLookYaw, _sledLookPitch;

        internal void TryMountSled(BoreatekVillageSled sled)
        {
            if (!_gameplayEnabled || _mountedSled != null || _isShopMenuOpen || !sled.AttachRider(this)) return;
            _mountedSled = sled;
            _sledLookYaw = _sledLookPitch = 0f;
            _characterController.enabled = false;
            _verticalVelocity = _knockbackVelocity = Vector3.zero;
            CancelPickaxeSwing();
            CancelHeatPlacement();
            _snowFootprints?.ResetBoots();
            UpdateSeatPose();
            UpdateCameraPositionImmediate();
            PublishStatus("WASD para conducir. Puedes girar en reposo. Espacio para frenar y E para bajar.");
        }

        private void UpdateSled(float dt, bool acceptInput)
        {
            var keys = Keyboard.current;
            var pad = Gamepad.current;
            if (acceptInput && _isCursorLocked && _cameraPivot != null)
            {
                Vector2 look = Mouse.current != null ? Mouse.current.delta.ReadValue() * _mouseSensitivity : Vector2.zero;
                if (pad != null) look += pad.rightStick.ReadValue() * (118f * dt);
                _sledLookYaw = Mathf.Clamp(_sledLookYaw + look.x, -95f, 95f);
                _sledLookPitch = Mathf.Clamp(_sledLookPitch - look.y, -52f, 55f);
            }
            if (acceptInput && (keys?.eKey.wasPressedThisFrame == true || pad?.buttonWest.wasPressedThisFrame == true))
            {
                if (_mountedSled.Moving) PublishStatus("Frena el trineo antes de bajar.");
                else if (!LeaveSled()) PublishStatus("No hay suelo despejado para bajar.");
                if (_mountedSled == null) return;
            }
            Vector2 input = acceptInput && pad != null ? pad.leftStick.ReadValue() : Vector2.zero;
            if (acceptInput && keys != null)
            {
                input.x += (keys.dKey.isPressed ? 1f : 0f) - (keys.aKey.isPressed ? 1f : 0f);
                input.y += (keys.wKey.isPressed ? 1f : 0f) - (keys.sKey.isPressed ? 1f : 0f);
            }
            input = Vector2.ClampMagnitude(input, 1f);
            bool brake = !acceptInput || keys?.spaceKey.isPressed == true || pad?.buttonSouth.isPressed == true;
            _mountedSled.Drive(input.y, input.x, brake, dt);
            UpdateSeatPose();
            _currentFocusedInteractable = _mountedSled;
            _aimHit = default;
            UpdateCameraObstruction(dt);
        }

        private void UpdateSeatPose()
        {
            Transform seat = _mountedSled.Seat;
            float heading = seat.eulerAngles.y;
            transform.SetPositionAndRotation(seat.position, Quaternion.Euler(0f, heading, 0f));
            _playerYaw = heading;
            _elfPose?.SitOnSled();
        }

        private bool LeaveSled(bool force = false)
        {
            if (_mountedSled == null) return false;
            if (!_mountedSled.FindLanding(out Vector3 landing) && !force) return false;
            BoreatekVillageSled sled = _mountedSled;
            _mountedSled = null;
            transform.position = landing;
            _characterController.enabled = true;
            sled.ReleaseRider();
            if (_cameraPivot != null) _cameraPivot.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
            _elfPose?.Reset();
            UpdateCameraPositionImmediate();
            return true;
        }

        private Vector3 SavePosition()
        {
            if (_mountedSled == null) return transform.position;
            return _mountedSled.FindLanding(out Vector3 landing) ? landing : new Vector3(0f, 1.2f, -5.5f);
        }
    }
}
