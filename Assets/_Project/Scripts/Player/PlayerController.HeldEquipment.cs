using Boreatek.Core;
using UnityEngine;

namespace Boreatek.Player
{
    public partial class PlayerController
    {
        private Transform[] _heldModels;
        private Transform _equippedModel, _firstPersonGlove;
        private ElfPose.Gesture _heldGesture;
        private float _heldGestureTime;

        internal void ConfigureHeldEquipment(Transform[] models, Transform glove)
        {
            _heldModels = models;
            _firstPersonGlove = glove;
            RefreshHeldEquipment();
        }

        private void RefreshHeldEquipment(float dt = 0f)
        {
            bool present = _characterController != null && _characterController.enabled;
            bool pickaxe = present && SelectedEquipment == EquipmentSlot.Pickaxe;
            if (_pickaxeVisualRoot != null && _pickaxeVisualRoot.gameObject.activeSelf != pickaxe)
                _pickaxeVisualRoot.gameObject.SetActive(pickaxe);

            int slot = (int)SelectedEquipment;
            bool canShow = present && !pickaxe && _heldModels != null && slot < _heldModels.Length &&
                (_heldGestureTime > 0f || (_inventoryAndShop != null && _inventoryAndShop.GetEquipmentStock(SelectedEquipment) > 0));
            Transform next = canShow ? _heldModels[slot] : null;
            if (next != _equippedModel)
            {
                if (_equippedModel != null) _equippedModel.gameObject.SetActive(false);
                _equippedModel = next;
                if (next != null) next.gameObject.SetActive(true);
            }

            bool viewModel = InFirstPerson && _playerCamera != null && _cameraPivot != null;
            bool showGlove = viewModel && (pickaxe || next != null);
            if (_firstPersonGlove != null && _firstPersonGlove.gameObject.activeSelf != showGlove)
                _firstPersonGlove.gameObject.SetActive(showGlove);
            if (next != null) PoseHeldModel(next, viewModel);
            if (showGlove && _firstPersonGlove != null && pickaxe && _pickaxeVisualRoot != null)
            {
                _firstPersonGlove.localPosition = _pickaxeVisualRoot.localPosition;
                _firstPersonGlove.localRotation = Quaternion.Euler(2f, -11f, 6f);
                _firstPersonGlove.localScale = Vector3.one * Mathf.Clamp(_pickaxeVisualRoot.localScale.x / FirstPersonPickaxeScale, 0.31f, 1f);
            }
            _heldGestureTime = Mathf.Max(0f, _heldGestureTime - dt);
        }

        private void PoseHeldModel(Transform item, bool firstPerson)
        {
            Transform mount = firstPerson ? _cameraPivot : _pickaxeGripParent;
            if (mount == null) return;
            if (item.parent != mount) item.SetParent(mount, false);
            if (!firstPerson)
            {
                item.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(6f, -12f, -10f));
                item.localScale = Vector3.one;
                return;
            }

            Vector3 grip = new Vector3(0.335f, -0.299f, 0.60f);
            Vector3 angles = new Vector3(0f, -15f, -9f);
            float pace = Mathf.Min(1f, _characterController.velocity.magnitude * 0.19f);
            grip.y += Mathf.Sin(Time.time * 11.2f) * 0.017f * pace;
            float action = Mathf.Sin(Mathf.PI * _heldGestureTime / ElfPose.GestureDuration);
            switch (_heldGesture)
            {
                case ElfPose.Gesture.Consume:
                    grip += new Vector3(-0.14f, 0.135f, -0.125f) * action;
                    angles += new Vector3(-28f, -6f, -24f) * action;
                    break;
                case ElfPose.Gesture.Place:
                    grip += new Vector3(-0.09f, -0.16f, 0.16f) * action;
                    angles.x += 27f * action;
                    break;
                case ElfPose.Gesture.Throw:
                    grip += new Vector3(0.08f, 0.15f, 0.10f) * action;
                    angles.z -= 32f * action;
                    break;
                case ElfPose.Gesture.Reach:
                    grip.z += 0.12f * action;
                    break;
            }
            if (_swordStrike)
            {
                PickaxeStroke slash = MeleePose;
                grip += new Vector3(0.092f, 0.11f, -0.08f) * slash.Raise +
                    new Vector3(-0.27f, 0.062f, 0.14f) * slash.Contact +
                    new Vector3(-0.31f, -0.055f, 0.055f) * slash.Follow;
                angles += new Vector3(-13f, 31f, -57f) * slash.Raise +
                    new Vector3(19f, -35f, 68f) * slash.Contact + new Vector3(28f, -38f, 103f) * slash.Follow;
            }

            var camera = _playerCamera.transform;
            Vector3 path = camera.TransformDirection(grip + Vector3.forward * 0.13f);
            float clearance = Mathf.Clamp(ViewClearance(camera.position, path, 0.09f) / path.magnitude, 0.32f, 1f);
            Vector3 position = _firstPersonCameraOffset + grip * clearance;
            Quaternion rotation = Quaternion.Euler(angles);
            item.SetLocalPositionAndRotation(position, rotation);
            item.localScale = Vector3.one * (0.84f * clearance);
            if (_firstPersonGlove != null)
            {
                _firstPersonGlove.SetLocalPositionAndRotation(position, rotation);
                _firstPersonGlove.localScale = Vector3.one * clearance;
            }
        }
    }
}
