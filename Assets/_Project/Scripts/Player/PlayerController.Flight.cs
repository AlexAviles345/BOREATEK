using Boreatek.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Boreatek.Player
{
    public partial class PlayerController
    {
        private BoreatekBushPlane _pilotingPlane;
        private Vector3 _flightCameraBoom;
        private float _flightLookYaw, _flightLookPitch;
        internal bool DiedInPlaneExplosion { get; private set; }

        internal void TryMountPlane(BoreatekBushPlane plane, bool resuming = false)
        {
            if ((!_gameplayEnabled && !resuming) || _pilotingPlane != null || _mountedSled != null || IsShopMenuOpen ||
                !plane.Board(this, resuming)) return;
            _pilotingPlane = plane;
            _flightLookYaw = _flightLookPitch = 0f;
            PrepareVehicleSeat();
            UpdateFlightSeat();
            _currentFocusedInteractable = plane;
            UpdateCameraPositionImmediate();
            PublishStatus("W acelera. Pulsa ↓ para levantar el morro cuando la avioneta tome velocidad.");
        }

        private void UpdateFlight(float dt, bool acceptInput)
        {
            Keyboard keys = Keyboard.current;
            Gamepad pad = Gamepad.current;
            float power = 0f, pitch = 0f, steer = 0f;
            bool brake = false;
            if (acceptInput)
            {
                if (keys != null)
                {
                    power = KeyAxis(keys.wKey.isPressed, keys.sKey.isPressed);
                    pitch = KeyAxis(keys.downArrowKey.isPressed, keys.upArrowKey.isPressed);
                    steer = KeyAxis(keys.dKey.isPressed, keys.aKey.isPressed);
                    brake = keys.spaceKey.isPressed;
                    if (keys.rKey.wasPressedThisFrame) _pilotingPlane.Refill(_inventoryAndShop);
                }
                if (pad != null)
                {
                    Vector2 stick = pad.leftStick.ReadValue();
                    power += pad.rightTrigger.ReadValue() - pad.leftTrigger.ReadValue();
                    pitch -= stick.y;
                    steer += stick.x;
                    brake |= pad.buttonSouth.isPressed;
                    if (pad.buttonNorth.wasPressedThisFrame) _pilotingPlane.Refill(_inventoryAndShop);
                }
                if (keys?.eKey.wasPressedThisFrame == true || pad?.buttonWest.wasPressedThisFrame == true)
                {
                    if (!_pilotingPlane.CanExit) PublishStatus("Aterriza y frena antes de bajar de la avioneta.");
                    else if (!LeavePlane()) PublishStatus("No hay espacio junto a la cabina para bajar.");
                    if (_pilotingPlane == null) return;
                }
                if (_isCursorLocked)
                {
                    Vector2 look = Mouse.current != null ? Mouse.current.delta.ReadValue() * _mouseSensitivity : Vector2.zero;
                    if (pad != null) look += pad.rightStick.ReadValue() * (112f * dt);
                    _flightLookYaw = Mathf.Clamp(_flightLookYaw + look.x, -112f, 112f);
                    _flightLookPitch = Mathf.Clamp(_flightLookPitch - look.y, -43f, 49f);
                }
            }
            _pilotingPlane.Fly(Mathf.Clamp(power, -1f, 1f), Mathf.Clamp(pitch, -1f, 1f), Mathf.Clamp(steer, -1f, 1f), brake, dt);
            if (_pilotingPlane == null) return;
            UpdateFlightSeat();
            _currentFocusedInteractable = _pilotingPlane;
            _aimHit = default;
            UpdateCameraObstruction(dt);
        }

        private static float KeyAxis(bool positive, bool negative) => (positive ? 1f : 0f) - (negative ? 1f : 0f);

        private void UpdateFlightSeat()
        {
            Transform seat = _pilotingPlane.Seat;
            transform.SetPositionAndRotation(seat.position, seat.rotation);
            _playerYaw = seat.eulerAngles.y;
            _elfPose?.SitInVehicle();
        }

        private bool LeavePlane(bool force = false)
        {
            BoreatekBushPlane plane = _pilotingPlane;
            Vector3 landing = plane.Seat.position;
            if (!force && !plane.FindExit(out landing)) return false;
            _pilotingPlane = null;
            _currentFocusedInteractable = null;
            plane.Release();
            transform.SetPositionAndRotation(landing, Quaternion.Euler(0f, _playerYaw, 0f));
            _characterController.enabled = true;
            if (_cameraPivot != null) _cameraPivot.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
            _elfPose?.Reset();
            UpdateCameraPositionImmediate();
            return true;
        }

        internal void HandlePlaneExplosion(BoreatekBushPlane plane, Vector3 center)
        {
            if (_pilotingPlane != plane) return;
            DiedInPlaneExplosion = true;
            LeavePlane(force: true);
            _temperatureSystem.ApplyCombatDamage(_temperatureSystem.MaxHealth, center);
        }

        private bool UpdateFlightCamera(Transform camera, float dt)
        {
            Transform hull = _pilotingPlane.transform;
            Quaternion look = Quaternion.Euler(_flightLookPitch, _flightLookYaw, 0f);
            if (!InFirstPerson)
            {
                bool front = CurrentPerspective == Perspective.Front;
                bool shoulder = CurrentPerspective == Perspective.Shoulder;
                Vector3 anchor = hull.position + Vector3.up * 1.74f;
                float yaw = front ? Mathf.Clamp(_flightLookYaw, -58f, 58f) : _flightLookYaw;
                Quaternion orbit = Quaternion.Euler(0f, _playerYaw + yaw, 0f);
                Vector3 offset = front ? new Vector3(0f, 1.65f, 8.5f) :
                    shoulder ? new Vector3(2.6f, 0.88f, -5.9f) : new Vector3(0f, 2.1f, -8.6f);
                offset.y = Mathf.Max(0.58f, offset.y + _flightLookPitch * 0.062f);
                Vector3 desired = orbit * offset;
                Vector3 clear = ClampViewOffset(anchor, desired, 0.27f);
                if (clear.sqrMagnitude < 9f)
                {
                    Vector3 raised = ClampViewOffset(anchor, desired + Vector3.up * 1.65f, 0.27f);
                    if (raised.sqrMagnitude > clear.sqrMagnitude) clear = raised;
                }
                Vector3 boom = dt <= 0f || _flightCameraBoom == Vector3.zero ? clear :
                    Vector3.Lerp(_flightCameraBoom, clear, 1f - Mathf.Exp(-11f * dt));
                _flightCameraBoom = ClampViewOffset(anchor, boom, 0.27f);
                if (_flightCameraBoom.sqrMagnitude >= 4.8f)
                {
                    Vector3 target = anchor + orbit * new Vector3(shoulder ? 0.85f : 0f, -0.14f, front ? -0.23f : 1.55f);
                    camera.SetPositionAndRotation(anchor + _flightCameraBoom, Quaternion.LookRotation(target - anchor - _flightCameraBoom));
                    _pilotingPlane.SetCockpitView(false);
                    return true;
                }
            }
            camera.SetPositionAndRotation(hull.TransformPoint(new Vector3(0f, 1.85f, 0.01f)), hull.rotation * look);
            _pilotingPlane.SetCockpitView(true);
            return false;
        }
    }
}
