using System.Collections.Generic;
using Boreatek.Core;
using Boreatek.Economy;
using Boreatek.Player;
using Boreatek.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Boreatek.UI
{
    /// <summary>
    /// Interfaz gráfica estilizada y responsiva en uGUI (Canvas 1920x1080 con CanvasScaler):
    /// - Tarjeta superior izquierda: Barras animadas de Temperatura Corporal (°T), Salud (HP) y Presión del Tanque-Mochila.
    /// - Tarjeta superior derecha: Reloj de Campaña de 20 Horas (Ciclo de 20m), Clima en español, Nivel de Equipo y Radar.
    /// - Barra de herramientas, mochila y avisos de juego.
    /// - Retícula central minimalista + Tarjeta flotante contextual al apuntar a un nodo, cofre congelado o tolva.
    /// - Viñeta perimetral de escarcha cuando la temperatura cae de 35 °T.
    /// - Tienda del comerciante con tarjetas y filtros para la compra de equipo.
    /// </summary>
    public partial class BoreatekHUDAndShopUI : MonoBehaviour
    {
        private PlayerController _player;
        private TemperatureSystem _tempSystem;
        private InventoryAndShop _inventoryShop;

        private Canvas _hudCanvas;
        private Sprite _roundedCardSprite;
        private Sprite _flatBarSprite;
        private Font _uiFont;

        // Referencias de barras y textos del Panel Izquierdo (Telemetría)
        private Image _tempBarFill;
        private Text _tempValueText;
        private Text _tempBadgeText;
        private Image _tempBadgeBg;

        private Image _healthBarFill;
        private Text _healthValueText;

        private Image _backpackBarFill;
        private Text _backpackValueText;
        private Text _coinsValueText;
        private Text _coresValueText;
        private Text _biomeNameText;

        // Referencias del Panel Derecho (Campaña 20h, Clima y Equipo)
        private Image _cycleBarFill;
        private Text _cycleTitleText;
        private Text _weatherBadgeText;
        private Image _weatherBadgeBg;
        private Text _equipmentSummaryText;
        private Text _radarStatusText;

        // Referencias de Retícula, Interacción, Banner de Estado y Escarcha
        private GameObject _interactionCardRoot;
        private BoreatekUiGlyph _crosshair;
        private Text _interactionPromptText;
        private RectTransform _destructionCard;
        private Image _destructionFill;
        private Text _destructionValue;
        private Text _statusToastText;
        private RawImage _frostVignetteOverlay;
        private RawImage _damageVignetteOverlay;
        private Texture2D _frostVignetteTexture;
        private string _lastToastMessage;
        private float _hideToastAt;
        private float _nextRadarRefresh;
        private float _nextTelemetryRefresh;

        private readonly List<Text> _hotbarCountTexts = new List<Text>();
        private readonly List<GameObject> _hotbarContents = new List<GameObject>();
        private readonly List<BoreatekUiButton> _hotbarSlots = new List<BoreatekUiButton>();
        private readonly int[] _shownHotbarStock = new int[EquipmentDisplay.Length];
        private int _shownEquipmentSlot = -1;

        // Paleta cromática oficial Boreatek UI
        private static readonly Color ColPanelDark = new Color(0.05f, 0.10f, 0.16f, 0.90f);
        private static readonly Color ColCardInner = new Color(0.09f, 0.16f, 0.24f, 0.94f);
        private static readonly Color ColBrassGold = new Color(0.85f, 0.64f, 0.30f, 1.0f);
        private static readonly Color ColWarmAmber = new Color(1.00f, 0.62f, 0.11f, 1.0f);
        private static readonly Color ColIceCyan = new Color(0.35f, 0.75f, 0.96f, 1.0f);
        private static readonly Color ColCandyRed = new Color(0.90f, 0.22f, 0.27f, 1.0f);
        private static readonly Color ColTextLight = new Color(0.93f, 0.96f, 1.00f, 1.0f);
        private static readonly Color ColTextMuted = new Color(0.66f, 0.76f, 0.86f, 1.0f);

        private void Awake()
        {
            EnsureEventSystem();
            EnsureCanvasReady();
        }

        public void BindPlayer(PlayerController player)
        {
            _player = player;
            _lastToastMessage = null;
            _nextTelemetryRefresh = 0f;
            _nextRadarRefresh = 0f;
            if (_player != null)
            {
                _tempSystem = _player.GetComponent<TemperatureSystem>();
                _inventoryShop = _player.GetComponent<InventoryAndShop>();
            }

            EnsureCanvasReady();
        }

        private void EnsureCanvasReady()
        {
            if (_uiFont == null)
            {
                _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_uiFont == null)
                {
                    _uiFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 16);
                }
            }

            if (_roundedCardSprite == null)
            {
                GenerateProceduralUISprites();
            }

            if (_hudCanvas == null || !_hudCanvas.gameObject.activeInHierarchy || _tempBarFill == null)
            {
                BuildResponsiveCanvasHierarchy();
            }
        }

        private void Update()
        {
            if (_player == null)
            {
                PlayerController found = FindAnyObjectByType<PlayerController>();
                if (found != null)
                {
                    BindPlayer(found);
                }
                return;
            }

            EnsureCanvasReady();
            if (!_hudCanvas.enabled)
            {
                return;
            }

            if (Time.unscaledTime >= _nextTelemetryRefresh)
            {
                _nextTelemetryRefresh = Time.unscaledTime + 0.10f;
                UpdateVitalAndThermalTelemetry();
                UpdateClockWeatherAndEquipment();
            }
            UpdateHotbarAndToast();
            UpdateCrosshairInteractionPrompt();
            UpdateFrostWarningVignette();
            UpdateDamageFeedback();
            UpdateShopModalVisibility();
            UpdateMinimap();
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject esObj = new GameObject("Boreatek_EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>
        /// Genera sprites 9-slice con bordes redondeados y marco de latón en memoria para no depender de PNGs externos.
        /// </summary>
        private void GenerateProceduralUISprites()
        {
            _roundedCardSprite = CreateRoundedRectSprite(64, 64, radius: 10, borderThickness: 2);
            _flatBarSprite = CreateRoundedRectSprite(32, 32, radius: 6, borderThickness: 0);
        }

        private static Sprite CreateRoundedRectSprite(int width, int height, int radius, int borderThickness)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = 0f;
                    float dy = 0f;

                    if (x < radius) dx = radius - x;
                    else if (x >= width - radius) dx = x - (width - radius - 1);

                    if (y < radius) dy = radius - y;
                    else if (y >= height - radius) dy = y - (height - radius - 1);

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > radius + 0.5f)
                    {
                        pixels[y * width + x] = Color.clear;
                    }
                    else
                    {
                        float alpha = Mathf.Clamp01(radius + 0.5f - dist);
                        bool isBorder = borderThickness > 0 &&
                                        (x < borderThickness || x >= width - borderThickness ||
                                         y < borderThickness || y >= height - borderThickness ||
                                         dist >= radius - borderThickness);

                        Color c = isBorder ? new Color(1f, 1f, 1f, alpha) : new Color(0.88f, 0.88f, 0.88f, alpha);
                        pixels[y * width + x] = c;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            Vector4 border = new Vector4(radius + 2, radius + 2, radius + 2, radius + 2);
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        private void BuildResponsiveCanvasHierarchy()
        {
            GameObject oldCanvas = GameObject.Find("Boreatek_HUD_Canvas");
            if (oldCanvas != null)
            {
                oldCanvas.name = "Boreatek_HUD_Canvas_Disposing";
                oldCanvas.SetActive(false);
                if (Application.isPlaying) Destroy(oldCanvas);
                else DestroyImmediate(oldCanvas);
            }

            GameObject canvasObj = new GameObject("Boreatek_HUD_Canvas");
            _hudCanvas = canvasObj.AddComponent<Canvas>();
            _hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _hudCanvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            ConfigureHudScaler(scaler);

            canvasObj.AddComponent<GraphicRaycaster>();

            RectTransform rootRect = canvasObj.GetComponent<RectTransform>();

            BuildFrostVignetteOverlay(rootRect);
            BuildTopLeftVitalPanel(rootRect);
            BuildTopRightCampaignAndEquipmentPanel(rootRect);
            BuildCenterCrosshairAndInteractionCard(rootRect);
            BuildBottomHotbarAndToastBanner(rootRect);
            BuildWorkshopShopModal(rootRect);
        }

        private void BuildFrostVignetteOverlay(RectTransform parent)
        {
            GameObject frostObj = CreateUIObject("FrostWarning_Overlay", parent);
            StretchFull(frostObj.GetComponent<RectTransform>());
            _frostVignetteOverlay = frostObj.AddComponent<RawImage>();
            if (_frostVignetteTexture == null)
            {
                _frostVignetteTexture = CreateFrostVignetteTexture();
            }
            _frostVignetteOverlay.texture = _frostVignetteTexture;
            _frostVignetteOverlay.color = new Color(0.47f, 0.80f, 1f, 0f);
            _frostVignetteOverlay.raycastTarget = false;

            GameObject damage = CreateUIObject("CombatDamage_Overlay", parent);
            StretchFull(damage.GetComponent<RectTransform>());
            _damageVignetteOverlay = damage.AddComponent<RawImage>();
            _damageVignetteOverlay.texture = _frostVignetteTexture;
            _damageVignetteOverlay.color = new Color(0.99f, 0.075f, 0.10f, 0f);
            _damageVignetteOverlay.raycastTarget = false;
        }

        private void UpdateDamageFeedback()
        {
            if (_tempSystem == null) return;
            float elapsed = Mathf.Clamp01((Time.time - _tempSystem.CombatHitAt) / 0.48f);
            float flash = (1f - elapsed) * (1f - elapsed);
            var tint = _damageVignetteOverlay.color;
            tint.a = flash * Mathf.Lerp(0.47f, 0.83f, _tempSystem.CombatHitStrength);
            _damageVignetteOverlay.color = tint;
            _healthBarFill.color = Color.Lerp(ColCandyRed, new Color(1f, 0.70f, 0.56f), flash);
        }

        private void BuildTopLeftVitalPanel(RectTransform parent)
        {
            var topLeft = new Vector2(0f, 1f);
            RectTransform panel = CreatePanelCard("Panel_TopLeft_Telemetry", parent, topLeft, topLeft, topLeft,
                new Vector2(24f, -24f), new Vector2(368f, 244f), ColDeepSteel);
            ApplyMetalPanel(panel, ColDeepSteel, ColSteelRim);
            panel.GetComponent<Image>().raycastTarget = false;
            AddHudHeading(panel, "Estado del traje", 184f);

            RectTransform thermal = CreatePanelCard("ThermalStateBadge", panel, topLeft, topLeft, topLeft,
                new Vector2(210f, -17f), new Vector2(140f, 26f), ColBlueSteel);
            _tempBadgeBg = thermal.GetComponent<Image>();
            _tempBadgeBg.raycastTarget = false;
            _tempBadgeText = CreateLabel(thermal, "BadgeTxt", "", Vector2.zero, new Vector2(140f, 26f),
                14, FontStyle.Bold, ColTextLight, TextAnchor.MiddleCenter);
            StretchFull(_tempBadgeText.rectTransform);

            _tempBarFill = CreateHudGauge(panel, "Temperature", "Temperatura", 55f, 332f, ColWarmAmber, out _tempValueText);
            _healthBarFill = CreateHudGauge(panel, "Health", "Salud", 98f, 332f, ColCandyRed, out _healthValueText);
            _backpackBarFill = CreateHudGauge(panel, "Backpack", "Mochila", 141f, 332f, ColBrassGold, out _backpackValueText);

            CreateAccentLine(panel, "EconomyRule", new Vector2(18f, -184f), new Vector2(332f, 1f), ColSteelRim);
            _coinsValueText = CreateLabel(panel, "CoinsTxt", "", new Vector2(18f, -190f), new Vector2(186f, 23f),
                15, FontStyle.Bold, ColBrassRim, TextAnchor.MiddleLeft);
            _coresValueText = CreateLabel(panel, "CoresTxt", "", new Vector2(205f, -190f), new Vector2(145f, 23f),
                15, FontStyle.Bold, ColTextLight, TextAnchor.MiddleRight);
            _biomeNameText = CreateLabel(panel, "BiomeTxt", "", new Vector2(18f, -216f), new Vector2(332f, 20f),
                15, FontStyle.Normal, ColIceCyan, TextAnchor.MiddleLeft);
            _biomeNameText.resizeTextForBestFit = true;
            _biomeNameText.resizeTextMinSize = 12;
            _biomeNameText.resizeTextMaxSize = 15;
            _biomeNameText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void BuildTopRightCampaignAndEquipmentPanel(RectTransform parent)
        {
            var topRight = Vector2.one;
            var topLeft = new Vector2(0f, 1f);
            RectTransform panel = CreatePanelCard("Panel_TopRight_Campaign", parent, topRight, topRight, topRight,
                new Vector2(-24f, -24f), new Vector2(348f, 227f), ColDeepSteel);
            ApplyMetalPanel(panel, ColDeepSteel, ColSteelRim);
            panel.GetComponent<Image>().raycastTarget = false;
            AddHudHeading(panel, "Campaña", 312f);

            _cycleBarFill = CreateHudGauge(panel, "Cycle", "Ciclo", 55f, 312f, ColIceCyan, out _cycleTitleText);
            RectTransform weather = CreatePanelCard("WeatherBadge", panel, topLeft, topLeft, topLeft,
                new Vector2(18f, -99f), new Vector2(312f, 29f), ColBlueSteel);
            _weatherBadgeBg = weather.GetComponent<Image>();
            _weatherBadgeBg.raycastTarget = false;
            _weatherBadgeText = CreateLabel(weather, "WeatherTxt", "", Vector2.zero, new Vector2(312f, 29f),
                15, FontStyle.Bold, ColTextLight, TextAnchor.MiddleCenter);
            StretchFull(_weatherBadgeText.rectTransform, 4f);

            _equipmentSummaryText = CreateLabel(panel, "EquipSummaryTxt", "", new Vector2(18f, -136f),
                new Vector2(312f, 62f), 15, FontStyle.Normal, ColTextLight, TextAnchor.UpperLeft);
            _radarStatusText = CreateLabel(panel, "RadarTxt", "[F] Faro  ·  [M] Mapa", new Vector2(18f, -202f),
                new Vector2(312f, 18f), 14, FontStyle.Bold, ColIceCyan, TextAnchor.MiddleLeft);
        }

        private void AddHudHeading(RectTransform panel, string title, float ruleWidth)
        {
            CreateLabel(panel, "Title", title, new Vector2(18f, -14f), new Vector2(ruleWidth, 29f),
                20, FontStyle.Bold, ColTextLight, TextAnchor.MiddleLeft);
            CreateAccentLine(panel, "BrassHeaderRule", new Vector2(18f, -46f), new Vector2(36f, 2f), ColBrassRim);
            CreateAccentLine(panel, "SteelHeaderRule", new Vector2(59f, -46f),
                new Vector2(Mathf.Max(0f, ruleWidth - 41f), 1f), ColSteelRim);
        }

        private void BuildCenterCrosshairAndInteractionCard(RectTransform parent)
        {
            var center = new Vector2(0.5f, 0.5f);
            _crosshair = AddHudGlyph(parent, "CenterCrosshair", BoreatekGlyph.Aim, center, Vector2.zero, 22f, ColTextLight);

            RectTransform prompt = CreatePanelCard("InteractionPromptCard", parent, center, center, center,
                new Vector2(0f, -50f), new Vector2(590f, 50f), ColDeepSteel);
            ApplyMetalPanel(prompt, ColDeepSteel, ColSteelRim);
            prompt.GetComponent<Image>().raycastTarget = false;
            _interactionCardRoot = prompt.gameObject;
            _interactionPromptText = CreateLabel(prompt, "PromptTxt", "", Vector2.zero, new Vector2(566f, 44f),
                16, FontStyle.Bold, ColTextLight, TextAnchor.MiddleCenter);
            StretchFull(_interactionPromptText.rectTransform, 6f);
            _interactionPromptText.verticalOverflow = VerticalWrapMode.Truncate;
            _interactionCardRoot.SetActive(false);

            _destructionCard = CreatePanelCard("DestructionProgress", parent, center, center, center,
                new Vector2(0f, -98f), new Vector2(262f, 42f), ColDeepSteel);
            ApplyMetalPanel(_destructionCard, ColDeepSteel, ColSteelRim);
            _destructionCard.GetComponent<Image>().raycastTarget = false;
            _destructionFill = CreateHudGauge(_destructionCard, "Damage", "Destrucción", 4f, 236f,
                ColWarmAmber, out _destructionValue);
            _destructionCard.gameObject.SetActive(false);
        }

        private void BuildBottomHotbarAndToastBanner(RectTransform parent)
        {
            var bottom = new Vector2(0.5f, 0f);
            RectTransform toast = CreatePanelCard("StatusToastBanner", parent, bottom, bottom, bottom,
                new Vector2(0f, 143f), new Vector2(820f, 48f), ColDeepSteel);
            ApplyMetalPanel(toast, ColDeepSteel, ColSteelRim);
            toast.GetComponent<Image>().raycastTarget = false;
            _statusToastText = CreateLabel(toast, "ToastTxt", "", Vector2.zero, new Vector2(790f, 44f),
                16, FontStyle.Bold, ColTextLight, TextAnchor.MiddleCenter);
            StretchFull(_statusToastText.rectTransform, 6f);
            _statusToastText.verticalOverflow = VerticalWrapMode.Truncate;
            toast.gameObject.SetActive(false);

            var topLeft = new Vector2(0f, 1f);
            RectTransform bar = CreatePanelCard("BottomHotbarContainer", parent, bottom, bottom, bottom,
                new Vector2(0f, 38f), new Vector2(920f, 94f), ColDeepSteel);
            ApplyMetalPanel(bar, ColDeepSteel, ColSteelRim);
            bar.GetComponent<Image>().raycastTarget = false;

            _hotbarCountTexts.Clear();
            _hotbarContents.Clear();
            _hotbarSlots.Clear();
            _shownEquipmentSlot = -1;

            for (int i = 0; i < EquipmentDisplay.Length; i++)
            {
                var entry = EquipmentDisplay[i];
                var equipment = (EquipmentSlot)i;
                RectTransform slot = CreatePanelCard($"HotbarSlot_{equipment}", bar, topLeft, topLeft, topLeft,
                    new Vector2(11f + 100f * i, -9f), new Vector2(96f, 76f), ColBlueSteel);
                var button = AttachMetalButton(slot, ColBlueSteel, ColBrassRim);
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                button.onClick.AddListener(() => _player?.SelectEquipment(equipment));
                var contents = (RectTransform)CreateUIObject("Contents", slot).transform;
                StretchFull(contents);
                CreateLabel(contents, "KeyTxt", (i + 1).ToString(), new Vector2(9f, -7f), new Vector2(22f, 22f),
                    14, FontStyle.Bold, ColIceCyan, TextAnchor.MiddleLeft);
                Text count = CreateLabel(contents, "CountTxt", "", new Vector2(59f, -7f), new Vector2(29f, 22f),
                    15, FontStyle.Bold, ColTextLight, TextAnchor.MiddleRight);
                AddHudGlyph(contents, "ItemGlyph", entry.glyph, new Vector2(0.5f, 1f),
                    new Vector2(0f, -28f), 29f, entry.accent);
                CreateLabel(contents, "ItemNameTxt", entry.label, new Vector2(4f, -51f), new Vector2(88f, 21f),
                    14, FontStyle.Bold, ColTextLight, TextAnchor.MiddleCenter);

                _hotbarCountTexts.Add(count);
                _hotbarContents.Add(contents.gameObject);
                _hotbarSlots.Add(button);
                _shownHotbarStock[i] = -1;
                SetHotbarStock(i, _inventoryShop?.GetEquipmentStock(equipment) ?? 0);
            }

            Text controls = CreateLabel(parent, "ControlsTxt",
                "Rueda  Elegir  ·  Clic  Usar  ·  [B] Mochila  ·  [E] Interactuar  ·  [F5] Cámara  ·  [Esc] Pausa",
                Vector2.zero, new Vector2(960f, 23f), 14, FontStyle.Bold, ColTextLight, TextAnchor.MiddleCenter);
            RectTransform footer = controls.rectTransform;
            footer.anchorMin = footer.anchorMax = footer.pivot = bottom;
            footer.anchoredPosition = new Vector2(0f, 9f);
        }

        private static BoreatekUiGlyph AddHudGlyph(RectTransform parent, string name, BoreatekGlyph glyph,
            Vector2 anchor, Vector2 position, float size, Color tint)
        {
            var icon = CreateUIObject(name, parent).AddComponent<BoreatekUiGlyph>();
            RectTransform rect = icon.rectTransform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = Vector2.one * size;
            icon.SetGlyph(glyph);
            icon.color = tint;
            icon.raycastTarget = false;
            return icon;
        }

        private static Texture2D CreateFrostVignetteTexture()
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y) / (size - 1f) * 2f - Vector2.one;
                    float edge = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y));
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 1f, edge));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255f * alpha));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void UpdateVitalAndThermalTelemetry()
        {
            if (_tempSystem != null)
            {
                float temperature = _tempSystem.CurrentTemperature;
                _tempBarFill.fillAmount = _tempSystem.NormalizedTemperature;
                _tempBarFill.color = Color.Lerp(ColIceCyan, ColWarmAmber, _tempSystem.NormalizedTemperature);
                _tempValueText.text = $"{temperature:0} / {_tempSystem.MaxTemperature:0} °T";

                if (_tempSystem.IsInsideHeatBubble)
                {
                    _tempBadgeText.text = "A resguardo";
                    _tempBadgeBg.color = new Color32(28, 82, 72, 255);
                }
                else if (_tempSystem.IsHotChocolateActive)
                {
                    _tempBadgeText.text = $"Calor  {_tempSystem.HotChocolateRemainingSeconds:0}s";
                    _tempBadgeBg.color = new Color32(107, 61, 29, 255);
                }
                else if (_tempSystem.ShelterGraceRemaining > 0f)
                {
                    _tempBadgeText.text = $"Aislado  {Mathf.CeilToInt(_tempSystem.ShelterGraceRemaining)}s";
                    _tempBadgeBg.color = ColBlueSteel;
                }
                else
                {
                    bool critical = temperature < 30f;
                    _tempBadgeText.text = critical ? "Frío crítico" : "A la intemperie";
                    _tempBadgeBg.color = critical ? new Color32(128, 42, 52, 255) : ColBlueSteel;
                }

                _healthBarFill.fillAmount = _tempSystem.NormalizedHealth;
                _healthValueText.text = $"{_tempSystem.CurrentHealth:0} / {_tempSystem.MaxHealth:0} HP";
                _biomeNameText.text = _tempSystem.ActiveHeatSourceName;
            }

            if (_inventoryShop != null)
            {
                int count = _inventoryShop.CurrentBackpackCount;
                int slots = _inventoryShop.MaxBackpackSlots;
                bool full = count >= slots;
                _backpackBarFill.fillAmount = slots > 0 ? (float)count / slots : 0f;
                _backpackBarFill.color = full ? ColCandyRed : ColBrassGold;
                _backpackValueText.text = full ? $"{count} / {slots}  ·  Llena" : $"{count} / {slots}";
                _coinsValueText.text = $"{_inventoryShop.GingerbreadCoins} monedas";
                _coresValueText.text = $"Forja  {_inventoryShop.LuminousCoresDeliveredToForge}/{_inventoryShop.LuminousCoresRequiredForChristmasEve}";
            }
        }

        private void UpdateClockWeatherAndEquipment()
        {
            DayNightCycleManager clock = DayNightCycleManager.Instance;
            if (clock != null)
            {
                _cycleBarFill.fillAmount = clock.NormalizedCycleTime;
                _cycleBarFill.color = clock.IsNightPhase ? ColIceCyan : ColBrassGold;
                int minutes = Mathf.CeilToInt((1f - clock.NormalizedCycleTime) * 20f);
                _cycleTitleText.text = $"{clock.CurrentCycleNumber}/{DayNightCycleManager.CAMPAIGN_TOTAL_CYCLES}  ·  {minutes} min";
                _weatherBadgeText.text = GetSpanishWeatherDescription(clock.CurrentWeather, clock.CurrentWeatherCoolingMultiplier);
                _weatherBadgeBg.color = clock.IsBlindBlizzardActive ? new Color32(38, 76, 113, 255) : ColBlueSteel;
            }

            int coatTier = _tempSystem != null ? _tempSystem.ThermalCoatTier : 1;
            int coatPct = _tempSystem != null ? Mathf.RoundToInt(_tempSystem.CoatCoolingReduction * 100f) : 0;
            string headlampState = _player.IsHeadlampEnabled ? "Encendido" : "Apagado";
            _equipmentSummaryText.text =
                $"Pico  Nv. {_player.CurrentPickaxeTier} · Daño {_player.PickaxeDamage}\n" +
                $"Abrigo  Nv. {coatTier} · −{coatPct}% frío\n" +
                $"Faro  {headlampState}";

            if (_inventoryShop == null || !_inventoryShop.HasGiftRadarUnlocked)
            {
                _radarStatusText.text = "[F] Faro  ·  [M] Mapa";
            }
            else if (Time.unscaledTime >= _nextRadarRefresh)
            {
                _nextRadarRefresh = Time.unscaledTime + 0.5f;
                float distance = FindNearestFrozenChestDistance();
                _radarStatusText.text = distance < 999f ? $"Radar  ·  Cofre a {distance:0} m" : "Radar  ·  Sin cofres cercanos";
            }
        }

        private void UpdateHotbarAndToast()
        {
            string message = _player.LastStatusMessage;
            if (message != _lastToastMessage)
            {
                _lastToastMessage = message;
                _statusToastText.text = RemoveMessageGlyphPrefix(message);
                _hideToastAt = Time.time + 5f;
            }

            bool showToast = Time.time < _hideToastAt && !string.IsNullOrEmpty(_statusToastText.text);
            GameObject toast = _statusToastText.transform.parent.gameObject;
            if (toast.activeSelf != showToast)
            {
                toast.SetActive(showToast);
            }

            if (_inventoryShop == null)
            {
                return;
            }

            for (int i = 0; i < _hotbarSlots.Count; i++)
                SetHotbarStock(i, _inventoryShop.GetEquipmentStock((EquipmentSlot)i));
            int selected = (int)_player.SelectedEquipment;
            if (selected == _shownEquipmentSlot || selected >= _hotbarSlots.Count) return;
            if (_shownEquipmentSlot >= 0) _hotbarSlots[_shownEquipmentSlot].SetLatched(false);
            _hotbarSlots[selected].SetLatched(true);
            _shownEquipmentSlot = selected;
        }

        private void SetHotbarStock(int index, int amount)
        {
            if (_shownHotbarStock[index] == amount)
            {
                return;
            }

            _shownHotbarStock[index] = amount;
            _hotbarContents[index].SetActive(amount > 0);
            _hotbarCountTexts[index].text = amount <= 0 || (EquipmentSlot)index is EquipmentSlot.Pickaxe or EquipmentSlot.Sword
                ? "" : amount.ToString();
        }

        private static string RemoveMessageGlyphPrefix(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            int index = 0;
            while (index < text.Length)
            {
                char c = text[index];
                var category = char.GetUnicodeCategory(c);
                if (!char.IsWhiteSpace(c) && category != System.Globalization.UnicodeCategory.Surrogate &&
                    category != System.Globalization.UnicodeCategory.OtherSymbol &&
                    category != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    break;
                }
                index++;
            }
            return index == 0 ? text : text.Substring(index);
        }

        private void UpdateCrosshairInteractionPrompt()
        {
            if (_crosshair != null) _crosshair.enabled = !_player.IsShopMenuOpen && _player.HasAimReticle;
            UpdateDestructionProgress(_player.IsShopMenuOpen ? null : _player.CurrentFocusedInteractable as HarvestableNode);
            if (_interactionCardRoot == null)
            {
                return;
            }

            if (_player.IsShopMenuOpen)
            {
                _interactionCardRoot.SetActive(false);
                return;
            }

            IInteractable focused = _player.CurrentFocusedInteractable;
            if (focused != null)
            {
                string prompt = focused.GetInteractionPrompt();
                if (!string.IsNullOrEmpty(prompt))
                {
                    _interactionCardRoot.SetActive(true);
                    _interactionPromptText.text = RemoveMessageGlyphPrefix(prompt);
                    return;
                }
            }

            _interactionCardRoot.SetActive(false);
        }

        private void UpdateDestructionProgress(HarvestableNode node)
        {
            if (_destructionCard == null) return;
            float progress = node != null && !node.IsDepleted && !node.IsFrozenLocked ? node.DestructionProgress : 0f;
            _destructionCard.gameObject.SetActive(progress > 0f);
            if (progress <= 0f) return;
            _destructionFill.fillAmount = progress;
            _destructionValue.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        }

        private void UpdateFrostWarningVignette()
        {
            if (_frostVignetteOverlay == null || _tempSystem == null)
            {
                return;
            }

            float targetAlpha = _tempSystem.CurrentTemperature < 35f
                ? Mathf.Lerp(0.38f, 0f, _tempSystem.CurrentTemperature / 35f)
                : 0f;

            Color c = _frostVignetteOverlay.color;
            c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * 5f);
            _frostVignetteOverlay.color = c;
        }

        private static string GetSpanishWeatherDescription(WeatherState state, float mult)
        {
            switch (state)
            {
                case WeatherState.ClearArcticDay:
                    return $"Día ártico  ·  Frío ×{mult:0.0}";
                case WeatherState.PolarNight:
                    return $"Noche polar  ·  Frío ×{mult:0.0}";
                case WeatherState.BlindBlizzard:
                    return $"Ventisca ciega  ·  Frío ×{mult:0.0}";
                case WeatherState.ChristmasEveOvercharge:
                    return $"Gran Nochebuena  ·  Frío ×{mult:0.0}";
                default:
                    return "Clima Polar";
            }
        }

        private float FindNearestFrozenChestDistance()
        {
            HarvestableNode[] nodes = FindObjectsByType<HarvestableNode>();
            float best = 9999f;
            Vector3 p = _player.transform.position;

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] != null && !nodes[i].IsDepleted && nodes[i].IsFrozenLocked)
                {
                    float d = Vector3.Distance(p, nodes[i].transform.position);
                    if (d < best)
                    {
                        best = d;
                    }
                }
            }

            return best;
        }

        // =====================================================================
        // Helpers de Construcción de Componentes uGUI
        // =====================================================================

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private RectTransform CreatePanelCard(
            string name,
            RectTransform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 size,
            Color color)
        {
            GameObject obj = CreateUIObject(name, parent);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.sprite = _roundedCardSprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            return rt;
        }

        private Image CreateHudGauge(RectTransform parent, string name, string label, float y,
            float width, Color fillColor, out Text valueLabel)
        {
            CreateLabel(parent, name + "_Label", label, new Vector2(18f, -y), new Vector2(123f, 20f),
                15, FontStyle.Bold, ColTextLight, TextAnchor.MiddleLeft);
            valueLabel = CreateLabel(parent, name + "_Value", "", new Vector2(143f, -y), new Vector2(width - 125f, 20f),
                15, FontStyle.Bold, ColTextLight, TextAnchor.MiddleRight);

            var topLeft = new Vector2(0f, 1f);
            RectTransform track = CreatePanelCard(name + "_Track", parent, topLeft, topLeft, topLeft,
                new Vector2(18f, -y - 24f), new Vector2(width, 9f), new Color32(35, 56, 73, 255));
            Image trackImage = track.GetComponent<Image>();
            trackImage.sprite = _flatBarSprite;
            trackImage.raycastTarget = false;
            var fill = CreateUIObject(name + "_Fill", track).AddComponent<Image>();
            StretchFull(fill.rectTransform, 1f);
            fill.sprite = _flatBarSprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.color = fillColor;
            fill.raycastTarget = false;
            return fill;
        }

        private Text CreateLabel(
            RectTransform parent,
            string name,
            string content,
            Vector2 anchoredPos,
            Vector2 size,
            int fontSize,
            FontStyle style,
            Color color,
            TextAnchor alignment)
        {
            GameObject obj = CreateUIObject(name, parent);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Text txt = obj.AddComponent<Text>();
            txt.font = _uiFont;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = alignment;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;

            Shadow shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(1f, -1f);

            return txt;
        }

        private BoreatekUiButton CreateStylizedButton(
            RectTransform parent,
            string name,
            string label,
            Vector2 anchoredPos,
            Vector2 size,
            Color bgColor,
            UnityEngine.Events.UnityAction onClick)
        {
            return CreateStylizedButton(parent, name, label, anchoredPos, size, bgColor, onClick, out _);
        }

        private BoreatekUiButton CreateStylizedButton(
            RectTransform parent,
            string name,
            string label,
            Vector2 anchoredPos,
            Vector2 size,
            Color bgColor,
            UnityEngine.Events.UnityAction onClick,
            out Text buttonLabel)
        {
            RectTransform btnRect = CreatePanelCard(name, parent,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                anchoredPos, size, bgColor);

            bool lightSurface = bgColor.r * 0.2126f + bgColor.g * 0.7152f + bgColor.b * 0.0722f > 0.55f;
            var btn = AttachMetalButton(btnRect, bgColor, lightSurface ? ColBrassRim : ColIceCyan, lightSurface);
            if (onClick != null)
            {
                btn.onClick.AddListener(onClick);
            }

            buttonLabel = CreateLabel(btnRect, "BtnTxt", label,
                Vector2.zero, size, 15, FontStyle.Bold, ColTextLight, TextAnchor.MiddleCenter);
            StretchFull(buttonLabel.rectTransform, 7f);
            buttonLabel.resizeTextForBestFit = true;
            buttonLabel.resizeTextMinSize = 12;
            buttonLabel.resizeTextMaxSize = 15;
            buttonLabel.verticalOverflow = VerticalWrapMode.Truncate;
            btn.SetLabel(buttonLabel);

            return btn;
        }

        private static void StretchFull(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }
    }
}
