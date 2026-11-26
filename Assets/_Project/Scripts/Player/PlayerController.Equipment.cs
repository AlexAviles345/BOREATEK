using Boreatek.Audio;
using Boreatek.Core;
using Boreatek.Economy;
using Boreatek.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Boreatek.Player
{
    public partial class PlayerController
    {
        private const float HeatPlacementReach = 3.8f;
        private const float HeatPlacementDelay = 0.21f;
        private RaycastHit _pendingHeatTarget;
        private EquipmentSlot _pendingHeatTool;
        private float _heatPlaceIn, _pendingHeatYaw;
        private LineRenderer _placementGuide;
        private Material _placementGuideMaterial;
        internal EquipmentSlot SelectedEquipment { get; private set; }

        internal void SelectEquipment(EquipmentSlot equipment)
        {
            if (SelectedEquipment == equipment) return;
            SelectedEquipment = equipment;
            CancelPickaxeSwing();
            _heldGestureTime = 0f;
            RefreshHeldEquipment();
        }

        private void HandleEquipmentSelection()
        {
            var mouse = Mouse.current;
            float scroll = mouse != null ? mouse.scroll.ReadValue().y : 0f;
            var pad = Gamepad.current;
            var keyboard = Keyboard.current;
            const int slots = (int)EquipmentSlot.Sword + 1;
            int change = scroll > 0.01f || pad?.leftShoulder.wasPressedThisFrame == true ? -1
                : scroll < -0.01f || pad?.rightShoulder.wasPressedThisFrame == true ? 1 : 0;
            if (change != 0)
            {
                SelectEquipment((EquipmentSlot)(((int)SelectedEquipment + change + slots) % slots));
                BoreatekSoundscape.Play(SoundCue.Click, strength: 0.26f);
            }
            if (keyboard == null) return;
            for (int i = 0; i < slots; i++)
            {
                if (!keyboard[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) continue;
                SelectEquipment((EquipmentSlot)i);
                break;
            }
        }

        internal static bool IsConsumable(EquipmentSlot slot) =>
            slot is EquipmentSlot.Chocolate or EquipmentSlot.Cookie or EquipmentSlot.Peppermint;

        internal bool ConsumeEquipment(EquipmentSlot slot)
        {
            if (_inventoryAndShop == null || !IsConsumable(slot)) return false;
            int stock = _inventoryAndShop.GetEquipmentStock(slot);
            switch (slot)
            {
                case EquipmentSlot.Chocolate: _inventoryAndShop.TryUseHotChocolateThermos(); break;
                case EquipmentSlot.Cookie: _inventoryAndShop.TryUseGingerbreadCookie(); break;
                case EquipmentSlot.Peppermint: _inventoryAndShop.TryUsePeppermintCane(); break;
            }
            if (_inventoryAndShop.GetEquipmentStock(slot) >= stock) return false;
            if (SelectedEquipment == slot) PlayInteraction(ElfPose.Gesture.Consume, AimFrame.forward);
            return true;
        }

        private void UseEquipment(EquipmentSlot slot)
        {
            if (_inventoryAndShop == null || _heatPlaceIn > 0f) return;
            if (slot == EquipmentSlot.Sword)
            {
                if (CanSwingPickaxe) PerformSwordSwing();
                return;
            }
            if (IsConsumable(slot))
            {
                ConsumeEquipment(slot);
                return;
            }
            if (slot is EquipmentSlot.Garland or EquipmentSlot.Beacon)
            {
                StartHeatPlacement(slot);
                return;
            }
            int stock = _inventoryAndShop.GetEquipmentStock(slot);
            if (slot == EquipmentSlot.Firecracker)
            {
                Vector3 landing = _aimHit.collider != null && Vector3.Distance(transform.position, _aimHit.point) < 3.3f
                    ? _aimHit.point : transform.position + transform.forward * 2.5f;
                _inventoryAndShop.TryDetonateFestiveFirecracker(landing);
                if (stock > 0) PlayInteraction(ElfPose.Gesture.Throw, AimFrame.forward);
            }
            else if (slot == EquipmentSlot.Extractor)
            {
                _inventoryAndShop.TryDeployMechanicalElfExtractor(transform.position);
                if (_inventoryAndShop.GetEquipmentStock(slot) < stock) PlayInteraction(ElfPose.Gesture.Place, AimFrame.forward);
            }
        }

        private bool ValidHeatTarget(RaycastHit target, EquipmentSlot slot, float yaw)
        {
            Collider support = target.collider;
            return support != null && support.enabled && support.gameObject.activeInHierarchy &&
                support.GetComponentInParent<IInteractable>() == null &&
                support.GetComponentInParent<FoundryWorker>() == null &&
                support.GetComponentInParent<AI.DefectiveToyAI>() == null &&
                (transform.position - target.point).sqrMagnitude <= HeatPlacementReach * HeatPlacementReach &&
                InventoryAndShop.CanAnchorHeatTool(target.point, target.normal, slot == EquipmentSlot.Beacon, yaw);
        }

        private void StartHeatPlacement(EquipmentSlot slot)
        {
            if (_inventoryAndShop.GetEquipmentStock(slot) <= 0)
            {
                PublishStatus("La mochila no tiene unidades de este objeto.");
                return;
            }
            float yaw = transform.eulerAngles.y;
            if (!ValidHeatTarget(_aimHit, slot, yaw))
            {
                PublishStatus("Apunta a un suelo firme, despejado y cercano para colocar el objeto.");
                return;
            }
            _pendingHeatTarget = _aimHit;
            _pendingHeatTool = slot;
            _pendingHeatYaw = yaw;
            _heatPlaceIn = HeatPlacementDelay;
            PlayInteraction(ElfPose.Gesture.Place, (_aimHit.point - AimFrame.position).normalized);
        }

        private void AdvanceHeatPlacement(float dt)
        {
            if (_heatPlaceIn <= 0f) return;
            _heatPlaceIn -= dt;
            if (_heatPlaceIn > 0f) return;
            if (!ValidHeatTarget(_pendingHeatTarget, _pendingHeatTool, _pendingHeatYaw) ||
                !_inventoryAndShop.TryPlaceHeatTool(_pendingHeatTool, _pendingHeatTarget.point,
                    _pendingHeatTarget.normal, _pendingHeatYaw))
                PublishStatus("Ya no hay espacio seguro para colocarlo.");
            _pendingHeatTarget = default;
        }

        private void UpdatePlacementGuide(bool canUseWorld)
        {
            bool beacon = SelectedEquipment == EquipmentSlot.Beacon;
            bool visible = canUseWorld && _heatPlaceIn <= 0f && (beacon || SelectedEquipment == EquipmentSlot.Garland) &&
                _inventoryAndShop != null && _inventoryAndShop.GetEquipmentStock(SelectedEquipment) > 0 &&
                ValidHeatTarget(_aimHit, SelectedEquipment, transform.eulerAngles.y);
            if (visible && _placementGuide == null) BuildPlacementGuide();
            if (_placementGuide == null) return;
            _placementGuide.enabled = visible;
            if (!visible) return;
            _placementGuide.transform.SetPositionAndRotation(_aimHit.point + _aimHit.normal * 0.025f,
                Quaternion.FromToRotation(Vector3.up, _aimHit.normal));
            _placementGuide.transform.localScale = Vector3.one * (beacon ? 0.45f : 0.54f);
        }

        private void BuildPlacementGuide()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) return;
            var guide = new GameObject("Placement_Footprint");
            guide.transform.SetParent(transform, false);
            _placementGuideMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
            _placementGuide = guide.AddComponent<LineRenderer>();
            _placementGuide.sharedMaterial = _placementGuideMaterial;
            _placementGuide.useWorldSpace = false;
            _placementGuide.loop = true;
            _placementGuide.startColor = _placementGuide.endColor = new Color(0.28f, 0.89f, 1f, 0.90f);
            _placementGuide.widthMultiplier = 0.024f;
            _placementGuide.shadowCastingMode = ShadowCastingMode.Off;
            _placementGuide.receiveShadows = false;
            _placementGuide.positionCount = 28;
            for (int i = 0; i < _placementGuide.positionCount; i++)
            {
                float angle = 2f * Mathf.PI * i / _placementGuide.positionCount;
                _placementGuide.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
            }
        }

        private void CancelHeatPlacement()
        {
            _heatPlaceIn = 0f;
            _pendingHeatTarget = default;
            if (_placementGuide != null) _placementGuide.enabled = false;
        }

        private void OnDestroy()
        {
            if (_placementGuideMaterial != null) Destroy(_placementGuideMaterial);
        }
    }
}
