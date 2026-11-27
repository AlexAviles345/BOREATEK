using Boreatek.Core;
using Boreatek.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Boreatek.UI
{
    public partial class BoreatekHUDAndShopUI
    {
        private const string OnFootControls = "Rueda  Elegir  ·  Clic  Usar  ·  [B] Mochila  ·  [E] Interactuar  ·  [F5] Cámara  ·  [Esc] Pausa";
        private BoreatekUiGlyph _crosshair;
        private RectTransform _contextCard, _miningGauge, _vehicleControlsPlate;
        private CanvasGroup _toast;
        private Text _contextText, _vehicleHelp, _toastText, _controls, _miningValue;
        private Image _miningFill;
        private IInteractable _shownVehicle, _shownFocus;
        private float _helpUntil, _toastUntil;
        private bool _padHints;

        private void BuildContextHud(RectTransform root, RectTransform vitals, RectTransform campaign)
        {
            _crosshair = AddHudGlyph(root, "CenterCrosshair", BoreatekGlyph.Aim,
                new Vector2(0.5f, 0.5f), Vector2.zero, 22f, ColTextLight);
            RectTransform notice = CreateHudNote(vitals, "StatusToast", ColBrassRim, out _toastText);
            _toast = notice.gameObject.AddComponent<CanvasGroup>();
            _toast.blocksRaycasts = _toast.interactable = false;
            notice.gameObject.SetActive(false);

            _contextCard = CreateHudNote(campaign, "ContextCard", ColIceCyan, out _contextText);
            _vehicleHelp = CreateLabel(_contextCard, "VehicleHelp", "", Vector2.zero, Vector2.zero,
                16, FontStyle.Normal, ColTextLight, TextAnchor.UpperLeft);
            _vehicleHelp.gameObject.SetActive(false);
            _miningGauge = (RectTransform)CreateUIObject("DestructionProgress", _contextCard).transform;
            _miningGauge.gameObject.AddComponent<LayoutElement>().preferredHeight = 35f;
            _miningFill = CreateHudGauge(_miningGauge, "Damage", "Destrucción", 0f,
                campaign.rect.width - 62f, ColWarmAmber, out _miningValue);
            _miningGauge.gameObject.SetActive(false);
            _contextCard.gameObject.SetActive(false);
        }

        private RectTransform CreateHudNote(RectTransform parent, string name, Color accent, out Text label)
        {
            // Stretch to the width of the existing HUD column and grow down as the text wraps.
            var panel = CreatePanelCard(name, parent, Vector2.zero, Vector2.right,
                new Vector2(0.5f, 1f), new Vector2(0f, -8f), Vector2.zero, new Color32(16, 30, 41, 218));
            panel.GetComponent<Image>().raycastTarget = false;
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 12, 8, 8);
            layout.spacing = 5f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var stripe = CreateAccentLine(panel, "Accent", Vector2.zero, Vector2.zero, accent);
            stripe.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var rect = stripe.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.up;
            rect.offsetMin = new Vector2(2f, 5f);
            rect.offsetMax = new Vector2(5f, -5f);
            label = CreateLabel(panel, "Message", "", Vector2.zero, Vector2.zero, 17, FontStyle.Bold,
                ColTextLight, TextAnchor.UpperLeft);
            return panel;
        }

        private void ShowStatusToast(string message)
        {
            _toastText.text = RemoveMessageGlyphPrefix(message);
            _toastUntil = Time.time + Mathf.Clamp(1.8f + _toastText.text.Length * 0.043f, 2.8f, 8f);
            UpdateStatusToast();
        }

        private void UpdateStatusToast()
        {
            float remaining = _toastUntil - Time.time;
            bool visible = remaining > 0f && _toastText.text.Length > 0 && !_player.IsShopMenuOpen;
            _toast.gameObject.SetActive(visible);
            if (visible) _toast.alpha = Mathf.Clamp01(remaining / 0.45f);
        }

        private void UpdateContextHud(bool refresh)
        {
            IInteractable vehicle = _player.CurrentVehicle;
            IInteractable focus = _player.IsShopMenuOpen ? null : vehicle ?? _player.CurrentFocusedInteractable;
            bool changedVehicle = vehicle != _shownVehicle;
            if (changedVehicle)
            {
                _shownVehicle = vehicle;
                _helpUntil = Time.time + 7f;
                _controls.fontSize = vehicle == null ? 14 : 16;
                _vehicleControlsPlate.gameObject.SetActive(vehicle != null);
            }

            bool showHelp = vehicle != null && UpdateVehicleHelp(vehicle is BoreatekBushPlane);
            _vehicleHelp.gameObject.SetActive(showHelp);
            if (vehicle == null && changedVehicle) _controls.text = OnFootControls;
            if (refresh || changedVehicle || focus != _shownFocus)
            {
                _shownFocus = focus;
                _contextText.text = RemoveMessageGlyphPrefix(focus?.GetInteractionPrompt());
                _contextText.color = focus is BoreatekBushPlane plane && plane.Burning ? ColWarmAmber : ColTextLight;
                UpdateMiningGauge(focus as HarvestableNode);
            }

            bool showContext = _contextText.text.Length > 0 && (vehicle is not BoreatekVillageSled || showHelp);
            _contextCard.gameObject.SetActive(showContext);
            _crosshair.enabled = !_player.IsShopMenuOpen && vehicle == null && _player.HasAimReticle;
            _crosshair.color = showContext ? ColIceCyan : ColTextLight;
        }

        private bool UpdateVehicleHelp(bool plane)
        {
            Keyboard keys = Keyboard.current;
            Gamepad pad = Gamepad.current;
            if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > 0.08f || pad.rightTrigger.isPressed ||
                pad.leftTrigger.isPressed || pad.buttonWest.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame))
                _padHints = true;
            if (keys?.anyKey.wasPressedThisFrame == true || keys?.f1Key.wasPressedThisFrame == true) _padHints = false;

            bool visible = Time.time < _helpUntil;
            if (keys?.f1Key.wasPressedThisFrame == true || pad?.selectButton.wasPressedThisFrame == true)
            {
                visible = !visible;
                _helpUntil = visible ? float.PositiveInfinity : 0f;
            }

            string shortcut = _padHints ? "[Select] Controles  ·  [Botón oeste] Bajar"
                : "[F1] Controles  ·  [E] Bajar  ·  [F5] Cámara  ·  [Esc] Pausa";
            if (_controls.text != shortcut)
            {
                _controls.text = shortcut;
                _vehicleControlsPlate.sizeDelta = new Vector2(Mathf.Ceil(_controls.preferredWidth) + 20f, 28f);
            }
            if (visible)
            {
                _vehicleHelp.text = plane
                    ? (_padHints ? "Gatillos Motor · Stick izq. Dirección\nTira del stick para despegar\nBotón sur Frenar · Norte Repostar"
                        : "W/S Motor · A/D Girar\n↓ Elevar morro · ↑ Bajar morro\nEspacio Frenar · R Repostar")
                    : (_padHints ? "Stick izq. para avanzar, retroceder y girar\nBotón sur para frenar"
                        : "W/S Avanzar o retroceder\nA/D Girar · Espacio Frenar");
            }
            return visible;
        }

        private void UpdateMiningGauge(HarvestableNode node)
        {
            float progress = node != null && !node.IsDepleted && !node.IsFrozenLocked ? node.DestructionProgress : 0f;
            _miningGauge.gameObject.SetActive(progress > 0f);
            if (progress <= 0f) return;
            _miningFill.fillAmount = progress;
            _miningValue.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        }
    }
}
