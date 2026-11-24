using System.Collections.Generic;
using Boreatek.Core;
using Boreatek.Data;
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
    /// - Hotbar inferior central de 7 casillas estilizadas ([1] a [3], [G], [H], [Q], [R]) + Banner de Notificaciones.
    /// - Retícula central minimalista + Tarjeta flotante contextual al apuntar a un nodo, cofre congelado o tolva.
    /// - Viñeta perimetral de escarcha cuando la temperatura cae de 35 °T.
    /// - Ventana Modal de la Tienda del Taller ([Tab]) con tarjetas, filtros y descarga en Tolva Central.
    /// </summary>
    public class BoreatekHUDAndShopUI : MonoBehaviour
    {
        private PlayerController _player;
        private TemperatureSystem _tempSystem;
        private InventoryAndShop _inventoryShop;

        private Canvas _hudCanvas;
        private Sprite _roundedCardSprite;
        private Sprite _flatBarSprite;
        private Sprite _circleMedallionSprite;
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
        private Text _interactionPromptText;
        private Text _statusToastText;
        private Image _frostVignetteOverlay;

        // Referencias del Hotbar de 7 Casillas
        private readonly List<Text> _hotbarCountTexts = new List<Text>();
        private readonly List<Image> _hotbarBorders = new List<Image>();

        // Referencias del Modal de la Tienda del Taller
        private GameObject _shopModalRoot;
        private Text _shopCoinsHeaderText;
        private Text _shopHopperButtonText;
        private ShopCategory? _selectedShopCategoryFilter;
        private Transform _shopCardsContainer;

        // Paleta cromática oficial Boreatek UI
        private static readonly Color ColPanelDark = new Color(0.05f, 0.10f, 0.16f, 0.90f);
        private static readonly Color ColCardInner = new Color(0.09f, 0.16f, 0.24f, 0.94f);
        private static readonly Color ColBrassGold = new Color(0.85f, 0.64f, 0.30f, 1.0f);
        private static readonly Color ColWarmAmber = new Color(1.00f, 0.62f, 0.11f, 1.0f);
        private static readonly Color ColIceCyan = new Color(0.35f, 0.75f, 0.96f, 1.0f);
        private static readonly Color ColCandyRed = new Color(0.90f, 0.22f, 0.27f, 1.0f);
        private static readonly Color ColEmeraldSafe = new Color(0.22f, 0.78f, 0.52f, 1.0f);
        private static readonly Color ColTextLight = new Color(0.93f, 0.96f, 1.00f, 1.0f);
        private static readonly Color ColTextMuted = new Color(0.66f, 0.76f, 0.86f, 1.0f);

        private void Awake()
        {
            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_uiFont == null)
            {
                _uiFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 16);
            }

            GenerateProceduralUISprites();
            EnsureEventSystem();
            BuildResponsiveCanvasHierarchy();
        }

        public void BindPlayer(PlayerController player)
        {
            _player = player;
            if (_player != null)
            {
                _tempSystem = _player.GetComponent<TemperatureSystem>();
                _inventoryShop = _player.GetComponent<InventoryAndShop>();
            }

            if (_hudCanvas == null)
            {
                BuildResponsiveCanvasHierarchy();
            }

            RebuildShopCardsList();
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

            UpdateVitalAndThermalTelemetry();
            UpdateClockWeatherAndEquipment();
            UpdateHotbarAndToast();
            UpdateCrosshairInteractionPrompt();
            UpdateFrostWarningVignette();
            UpdateShopModalVisibility();
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
            _circleMedallionSprite = CreateCircleSprite(48);
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

        private static Sprite CreateCircleSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float r = size * 0.5f - 1f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    float a = Mathf.Clamp01(r + 0.5f - d);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private void BuildResponsiveCanvasHierarchy()
        {
            GameObject oldCanvas = GameObject.Find("Boreatek_HUD_Canvas");
            if (oldCanvas != null)
            {
                if (Application.isPlaying) Destroy(oldCanvas);
                else DestroyImmediate(oldCanvas);
            }

            GameObject canvasObj = new GameObject("Boreatek_HUD_Canvas");
            _hudCanvas = canvasObj.AddComponent<Canvas>();
            _hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _hudCanvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

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
            _frostVignetteOverlay = frostObj.AddComponent<Image>();
            _frostVignetteOverlay.sprite = _roundedCardSprite;
            _frostVignetteOverlay.type = Image.Type.Sliced;
            _frostVignetteOverlay.color = new Color(0.25f, 0.72f, 0.96f, 0f);
            _frostVignetteOverlay.raycastTarget = false;
        }

        /// <summary>
        /// Panel Superior Izquierdo: Temperatura Corporal, Salud Estructural, Tanque-Mochila y Economía.
        /// </summary>
        private void BuildTopLeftVitalPanel(RectTransform parent)
        {
            RectTransform card = CreatePanelCard("Panel_TopLeft_Telemetry", parent,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f),
                pivot: new Vector2(0f, 1f), anchoredPos: new Vector2(24f, -24f),
                size: new Vector2(450f, 265f), ColPanelDark);

            // Cabecera con acento dorado de latón
            CreateLabel(card, "HeaderTitle", "🎄⚙️ TELEMETRÍA DEL DUENDE INGENIERO",
                new Vector2(18f, -14f), new Vector2(414f, 26f), 17, FontStyle.Bold, ColBrassGold, TextAnchor.MiddleLeft);

            // Badge de estado térmico (En Burbuja / Expuesto al Frío / Chocolate)
            RectTransform badgeRect = CreatePanelCard("ThermalStateBadge", card,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-18f, -46f), new Vector2(185f, 26f), ColEmeraldSafe);
            _tempBadgeBg = badgeRect.GetComponent<Image>();
            _tempBadgeText = CreateLabel(badgeRect, "BadgeTxt", "EN BURBUJA TÉRMICA",
                Vector2.zero, new Vector2(185f, 26f), 12, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            StretchFull(_tempBadgeText.GetComponent<RectTransform>());

            // 1. Barra de Temperatura Corporal
            CreateLabel(card, "TempLabel", "🌡️ TEMPERATURA CORPORAL",
                new Vector2(18f, -46f), new Vector2(230f, 24f), 13, FontStyle.Bold, ColTextLight, TextAnchor.MiddleLeft);
            _tempBarFill = CreateProgressBar(card, "TempProgressBar", new Vector2(18f, -74f), new Vector2(414f, 22f),
                ColWarmAmber, out _tempValueText);

            // 2. Barra de Salud Estructural
            CreateLabel(card, "HpLabel", "❤️ SALUD ESTRUCTURAL",
                new Vector2(18f, -104f), new Vector2(230f, 22f), 13, FontStyle.Bold, ColTextLight, TextAnchor.MiddleLeft);
            _healthBarFill = CreateProgressBar(card, "HealthProgressBar", new Vector2(18f, -128f), new Vector2(414f, 20f),
                ColCandyRed, out _healthValueText);

            // 3. Barra de Capacidad del Tanque-Mochila Presurizado de Latón
            CreateLabel(card, "PackLabel", "🎒 TANQUE-MOCHILA PRESURIZADO DE LATÓN",
                new Vector2(18f, -156f), new Vector2(350f, 22f), 13, FontStyle.Bold, ColTextLight, TextAnchor.MiddleLeft);
            _backpackBarFill = CreateProgressBar(card, "BackpackProgressBar", new Vector2(18f, -180f), new Vector2(414f, 20f),
                ColBrassGold, out _backpackValueText);

            // 4. Fila Inferior: Monedas de Jengibre, Núcleos Luminosos y Bioma Actual
            RectTransform economyStrip = CreatePanelCard("EconomyStrip", card,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -208f), new Vector2(414f, 44f), ColCardInner);

            _coinsValueText = CreateLabel(economyStrip, "CoinsTxt", "🪙 60 Monedas de Jengibre",
                new Vector2(12f, -4f), new Vector2(215f, 20f), 14, FontStyle.Bold, ColWarmAmber, TextAnchor.MiddleLeft);
            _coresValueText = CreateLabel(economyStrip, "CoresTxt", "🌋 Núcleos Forja: 0 / 3",
                new Vector2(225f, -4f), new Vector2(175f, 20f), 13, FontStyle.Bold, ColCandyRed, TextAnchor.MiddleRight);
            _biomeNameText = CreateLabel(economyStrip, "BiomeTxt", "📍 Zona: Aldea Industrial de Santa",
                new Vector2(12f, -23f), new Vector2(390f, 18f), 12, FontStyle.Normal, ColIceCyan, TextAnchor.MiddleLeft);
        }

        /// <summary>
        /// Panel Superior Derecho: Reloj de Campaña de 20 Horas, Clima Dinámico, Equipo y Radar de Regalos.
        /// </summary>
        private void BuildTopRightCampaignAndEquipmentPanel(RectTransform parent)
        {
            RectTransform card = CreatePanelCard("Panel_TopRight_Campaign", parent,
                anchorMin: new Vector2(1f, 1f), anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(1f, 1f), anchoredPos: new Vector2(-24f, -24f),
                size: new Vector2(430f, 235f), ColPanelDark);

            CreateLabel(card, "CampaignHeader", "⏱️ CAMPAÑA DE 20 HORAS & EQUIPO",
                new Vector2(18f, -14f), new Vector2(394f, 26f), 17, FontStyle.Bold, ColBrassGold, TextAnchor.MiddleLeft);

            // Barra del Ciclo Día / Noche de 20 Minutos Reales
            _cycleBarFill = CreateProgressBar(card, "CycleProgressBar", new Vector2(18f, -46f), new Vector2(394f, 22f),
                ColIceCyan, out _cycleTitleText);

            // Badge del Estado Climático (Día Ártico / Noche Polar / Ventisca Ciega)
            RectTransform weatherPill = CreatePanelCard("WeatherBadge", card,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -76f), new Vector2(394f, 30f), ColCardInner);
            _weatherBadgeBg = weatherPill.GetComponent<Image>();
            _weatherBadgeText = CreateLabel(weatherPill, "WeatherTxt", "☀️ CLIMA: Día Ártico Despejado (Frío x1.0)",
                Vector2.zero, new Vector2(394f, 30f), 13, FontStyle.Bold, ColTextLight, TextAnchor.MiddleCenter);
            StretchFull(_weatherBadgeText.GetComponent<RectTransform>());

            // Resumen de Equipamiento del Duende Ingeniero
            RectTransform equipBox = CreatePanelCard("EquipBox", card,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -114f), new Vector2(394f, 76f), ColCardInner);

            _equipmentSummaryText = CreateLabel(equipBox, "EquipSummaryTxt",
                "⛏️ Pico Cristal Energético: Nivel 1 (Daño 1)\n" +
                "🧥 Abrigo Térmico + Botas: Nivel 1 (-0% frío)\n" +
                "🔦 Faro LED Frontal Casco: ENCENDIDO [F]",
                new Vector2(12f, -6f), new Vector2(370f, 64f), 13, FontStyle.Normal, ColTextLight, TextAnchor.MiddleLeft);

            // Radar de Regalos / Ayuda de Tienda
            _radarStatusText = CreateLabel(card, "RadarTxt",
                "🛒 Presiona [Tab] o [B] para abrir la Tienda del Taller",
                new Vector2(18f, -198f), new Vector2(394f, 24f), 13, FontStyle.Bold, ColWarmAmber, TextAnchor.MiddleCenter);
        }

        /// <summary>
        /// Retícula central minimalista y Tarjeta Flotante de Interacción contextual.
        /// </summary>
        private void BuildCenterCrosshairAndInteractionCard(RectTransform parent)
        {
            // Punto central de mira con borde cálido
            RectTransform crosshair = CreatePanelCard("CenterCrosshair", parent,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(10f, 10f), ColWarmAmber);
            crosshair.GetComponent<Image>().sprite = _circleMedallionSprite;
            crosshair.GetComponent<Image>().raycastTarget = false;

            // Tarjeta contextual debajo de la mira
            RectTransform promptCard = CreatePanelCard("InteractionPromptCard", parent,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -54f), new Vector2(620f, 44f), ColPanelDark);
            _interactionCardRoot = promptCard.gameObject;
            _interactionPromptText = CreateLabel(promptCard, "PromptTxt", "",
                Vector2.zero, new Vector2(600f, 44f), 15, FontStyle.Bold, ColWarmAmber, TextAnchor.MiddleCenter);
            StretchFull(_interactionPromptText.GetComponent<RectTransform>());
            _interactionCardRoot.SetActive(false);
        }

        /// <summary>
        /// Hotbar inferior central de 7 casillas estilizadas + Banner de Estado y guía compacta de controles.
        /// </summary>
        private void BuildBottomHotbarAndToastBanner(RectTransform parent)
        {
            // Banner de notificaciones (Toast) encima del Hotbar
            RectTransform toastCard = CreatePanelCard("StatusToastBanner", parent,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 134f), new Vector2(860f, 36f), ColPanelDark);

            _statusToastText = CreateLabel(toastCard, "ToastTxt",
                "📢 Duende Ingeniero listo en la Aldea Industrial.",
                Vector2.zero, new Vector2(840f, 36f), 14, FontStyle.Bold, ColWarmAmber, TextAnchor.MiddleCenter);
            StretchFull(_statusToastText.GetComponent<RectTransform>());

            // Contenedor de las 7 casillas del Hotbar
            RectTransform hotbarRoot = CreatePanelCard("BottomHotbarContainer", parent,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 32f), new Vector2(860f, 94f), ColPanelDark);

            string[] keys = { "1", "2", "3", "G", "H", "Q", "R" };
            string[] names = { "Chocolate", "Galleta", "B. Menta", "Guirnalda", "Baliza", "Petardo", "Autómata" };
            Color[] iconColors =
            {
                new Color(0.55f, 0.32f, 0.18f), // Chocolate caliente
                new Color(0.88f, 0.62f, 0.24f), // Galleta jengibre
                new Color(0.92f, 0.24f, 0.30f), // Bastón menta
                new Color(1.00f, 0.80f, 0.22f), // Guirnalda luminosa
                new Color(1.00f, 0.45f, 0.08f), // Baliza térmica
                new Color(0.95f, 0.20f, 0.18f), // Petardo festivo
                new Color(0.35f, 0.78f, 0.68f)  // Duende mecánico
            };

            _hotbarCountTexts.Clear();
            _hotbarBorders.Clear();

            float startX = 14f;
            float slotWidth = 114f;
            float spacing = 6f;

            for (int i = 0; i < 7; i++)
            {
                float x = startX + i * (slotWidth + spacing);
                RectTransform slot = CreatePanelCard($"HotbarSlot_{keys[i]}", hotbarRoot,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(x, -8f), new Vector2(slotWidth, 78f), ColCardInner);

                _hotbarBorders.Add(slot.GetComponent<Image>());

                // Badge de tecla (Arriba Izquierda)
                RectTransform keyBadge = CreatePanelCard("KeyBadge", slot,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(6f, -6f), new Vector2(26f, 22f), ColBrassGold);
                Text keyTxt = CreateLabel(keyBadge, "KeyTxt", keys[i],
                    Vector2.zero, new Vector2(26f, 22f), 13, FontStyle.Bold, ColPanelDark, TextAnchor.MiddleCenter);
                StretchFull(keyTxt.GetComponent<RectTransform>());

                // Medallón cromático del ítem
                RectTransform medallion = CreatePanelCard("ItemMedallion", slot,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(8f, -10f), new Vector2(34f, 34f), iconColors[i]);
                medallion.GetComponent<Image>().sprite = _circleMedallionSprite;

                // Cantidad disponible (Arriba Derecha)
                Text countTxt = CreateLabel(slot, "CountTxt", "x1",
                    new Vector2(slotWidth - 42f, -6f), new Vector2(36f, 22f), 14, FontStyle.Bold, ColTextLight, TextAnchor.MiddleRight);
                _hotbarCountTexts.Add(countTxt);

                // Nombre corto del ítem (Abajo)
                CreateLabel(slot, "ItemNameTxt", names[i],
                    new Vector2(4f, -52f), new Vector2(slotWidth - 8f, 22f), 12, FontStyle.Bold, ColTextMuted, TextAnchor.MiddleCenter);
            }

            // Guía sutil de controles en el borde inferior
            RectTransform footerHint = CreatePanelCard("ControlsFooterStrip", parent,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 4f), new Vector2(860f, 24f), new Color(0.03f, 0.06f, 0.10f, 0.82f));

            Text hintTxt = CreateLabel(footerHint, "ControlsTxt",
                "[WASD] Mover  •  [Shift] Correr  •  [Clic Izq] Picar  •  [E] Interactuar / Tolva  •  [F] Faro LED  •  [V] Cámara 1ra/3ra  •  [Tab] Tienda",
                Vector2.zero, new Vector2(850f, 24f), 12, FontStyle.Normal, ColTextMuted, TextAnchor.MiddleCenter);
            StretchFull(hintTxt.GetComponent<RectTransform>());
        }

        /// <summary>
        /// Ventana Modal de la Tienda del Taller y Tolva Central ([Tab]).
        /// </summary>
        private void BuildWorkshopShopModal(RectTransform parent)
        {
            RectTransform modalOverlay = CreatePanelCard("WorkshopShop_Modal", parent,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1140f, 700f), new Color(0.04f, 0.08f, 0.13f, 0.97f));
            _shopModalRoot = modalOverlay.gameObject;

            // Barra de título del taller
            CreateLabel(modalOverlay, "ShopTitle", "🛒 TALLER Y TIENDA DE LA ALDEA INDUSTRIAL DE SANTA",
                new Vector2(28f, -20f), new Vector2(680f, 34f), 22, FontStyle.Bold, ColBrassGold, TextAnchor.MiddleLeft);

            _shopCoinsHeaderText = CreateLabel(modalOverlay, "ShopCoinsBalance", "🪙 Saldo: 60 Monedas de Jengibre",
                new Vector2(710f, -20f), new Vector2(280f, 34f), 18, FontStyle.Bold, ColWarmAmber, TextAnchor.MiddleRight);

            // Botón Cerrar
            CreateStylizedButton(modalOverlay, "BtnCloseShop", "CERRAR [Tab]",
                new Vector2(1005f, -20f), new Vector2(110f, 34f), ColCandyRed, () => _player?.ToggleShopMenu());

            // Botón destacado de Descarga en Tolva Central + Pestañas de Categoría
            CreateStylizedButton(modalOverlay, "BtnDepositHopper",
                "📥 DEPOSITAR TANQUE-MOCHILA EN TOLVA CENTRAL",
                new Vector2(28f, -66f), new Vector2(460f, 40f), ColEmeraldSafe,
                () =>
                {
                    _inventoryShop?.DepositAllResourcesIntoCentralHopper();
                    RebuildShopCardsList();
                }, out _shopHopperButtonText);

            // Filtros por categoría
            CreateStylizedButton(modalOverlay, "TabAll", "Todos",
                new Vector2(510f, -66f), new Vector2(105f, 40f), ColBrassGold, () => SetShopFilter(null));
            CreateStylizedButton(modalOverlay, "TabConsumables", "Comida",
                new Vector2(623f, -66f), new Vector2(115f, 40f), ColCardInner, () => SetShopFilter(ShopCategory.Consumable));
            CreateStylizedButton(modalOverlay, "TabTactical", "Tácticos",
                new Vector2(746f, -66f), new Vector2(115f, 40f), ColCardInner, () => SetShopFilter(ShopCategory.TacticalTool));
            CreateStylizedButton(modalOverlay, "TabUpgrades", "Mejoras",
                new Vector2(869f, -66f), new Vector2(115f, 40f), ColCardInner, () => SetShopFilter(ShopCategory.PermanentUpgrade));
            CreateStylizedButton(modalOverlay, "TabAuto", "Autómatas",
                new Vector2(992f, -66f), new Vector2(120f, 40f), ColCardInner, () => SetShopFilter(ShopCategory.Automation));

            // Área contenedora de tarjetas de la tienda (Grid de 2 columnas)
            RectTransform gridArea = CreatePanelCard("ShopCardsGridArea", modalOverlay,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(28f, -120f), new Vector2(1084f, 552f), new Color(0.03f, 0.06f, 0.09f, 0.85f));

            _shopCardsContainer = gridArea;
            _shopModalRoot.SetActive(false);
        }

        private void SetShopFilter(ShopCategory? category)
        {
            _selectedShopCategoryFilter = category;
            RebuildShopCardsList();
        }

        private void RebuildShopCardsList()
        {
            if (_shopCardsContainer == null || _inventoryShop == null)
            {
                return;
            }

            for (int i = _shopCardsContainer.childCount - 1; i >= 0; i--)
            {
                GameObject child = _shopCardsContainer.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            IReadOnlyList<ShopItemSO> items = _inventoryShop.ShopCatalog;
            int visibleIdx = 0;

            for (int i = 0; i < items.Count; i++)
            {
                ShopItemSO item = items[i];
                if (_selectedShopCategoryFilter.HasValue && item.Category != _selectedShopCategoryFilter.Value)
                {
                    continue;
                }

                int col = visibleIdx % 2;
                int row = visibleIdx / 2;
                if (row >= 7)
                {
                    break;
                }

                float cardW = 524f;
                float cardH = 72f;
                float x = 12f + col * (cardW + 12f);
                float y = -10f - row * (cardH + 6f);

                RectTransform itemCard = CreatePanelCard($"ShopCard_{item.ItemId}", _shopCardsContainer as RectTransform,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(x, y), new Vector2(cardW, cardH), ColCardInner);

                string catLabel = GetCategorySpanishName(item.Category);
                CreateLabel(itemCard, "ItemTitle", $"[{catLabel}] {item.DisplayName}",
                    new Vector2(14f, -8f), new Vector2(360f, 24f), 15, FontStyle.Bold, ColWarmAmber, TextAnchor.MiddleLeft);

                CreateLabel(itemCard, "ItemDesc", item.Description,
                    new Vector2(14f, -32f), new Vector2(360f, 34f), 12, FontStyle.Normal, ColTextLight, TextAnchor.UpperLeft);

                ShopItemSO capturedItem = item;
                bool canAfford = _inventoryShop.GingerbreadCoins >= item.CostInGingerbreadCoins;
                Color btnColor = canAfford ? ColBrassGold : new Color(0.25f, 0.28f, 0.32f, 0.9f);

                CreateStylizedButton(itemCard, "BtnBuy", $"COMPRAR\n{item.CostInGingerbreadCoins} 🪙",
                    new Vector2(384f, -10f), new Vector2(128f, 52f), btnColor,
                    () =>
                    {
                        if (_inventoryShop.TryPurchaseShopItem(capturedItem))
                        {
                            RebuildShopCardsList();
                        }
                    });

                visibleIdx++;
            }
        }

        private void UpdateVitalAndThermalTelemetry()
        {
            if (_tempSystem != null)
            {
                float normTemp = _tempSystem.NormalizedTemperature;
                _tempBarFill.fillAmount = normTemp;
                _tempBarFill.color = Color.Lerp(ColIceCyan, ColWarmAmber, normTemp);
                _tempValueText.text = $"{Mathf.RoundToInt(_tempSystem.CurrentTemperature)} / 100 °T";

                if (_tempSystem.IsInsideHeatBubble)
                {
                    _tempBadgeText.text = "🔥 EN BURBUJA TÉRMICA";
                    _tempBadgeBg.color = ColEmeraldSafe;
                }
                else if (_tempSystem.IsHotChocolateActive)
                {
                    _tempBadgeText.text = $"☕ CHOCOLATE ({Mathf.CeilToInt(_tempSystem.HotChocolateRemainingSeconds)}s)";
                    _tempBadgeBg.color = ColWarmAmber;
                }
                else
                {
                    _tempBadgeText.text = "❄️ EXPUESTO AL FRÍO";
                    _tempBadgeBg.color = _tempSystem.CurrentTemperature < 30f ? ColCandyRed : new Color(0.18f, 0.45f, 0.68f);
                }

                float normHp = _tempSystem.NormalizedHealth;
                _healthBarFill.fillAmount = normHp;
                _healthValueText.text = $"{Mathf.RoundToInt(_tempSystem.CurrentHealth)} / 100 HP";
                _biomeNameText.text = $"📍 Zona: {_tempSystem.ActiveHeatSourceName}";
            }

            if (_inventoryShop != null)
            {
                float normLoad = _inventoryShop.MaxBackpackSlots > 0
                    ? (float)_inventoryShop.CurrentBackpackCount / _inventoryShop.MaxBackpackSlots
                    : 0f;
                _backpackBarFill.fillAmount = Mathf.Clamp01(normLoad);
                bool isFull = _inventoryShop.CurrentBackpackCount >= _inventoryShop.MaxBackpackSlots;
                _backpackBarFill.color = isFull ? ColCandyRed : ColBrassGold;
                _backpackValueText.text = isFull
                    ? $"{_inventoryShop.CurrentBackpackCount} / {_inventoryShop.MaxBackpackSlots} (¡LLENO! Ve a la Tolva)"
                    : $"Nv.{_inventoryShop.BackpackTier} — {_inventoryShop.CurrentBackpackCount} / {_inventoryShop.MaxBackpackSlots} Minerales";

                _coinsValueText.text = $"🪙 {_inventoryShop.GingerbreadCoins} Monedas de Jengibre";
                _coresValueText.text = $"🌋 Núcleos Forja: {_inventoryShop.LuminousCoresDeliveredToForge} / {_inventoryShop.LuminousCoresRequiredForChristmasEve}";
            }
        }

        private void UpdateClockWeatherAndEquipment()
        {
            DayNightCycleManager dnc = DayNightCycleManager.Instance;
            if (dnc != null)
            {
                _cycleBarFill.fillAmount = dnc.NormalizedCycleTime;
                _cycleBarFill.color = dnc.IsNightPhase ? new Color(0.28f, 0.48f, 0.85f) : ColWarmAmber;

                int minsRemaining = Mathf.CeilToInt((1f - dnc.NormalizedCycleTime) * 20f);
                string phaseName = dnc.IsNightPhase ? "🌙 Noche Polar" : "☀️ Día Ártico";
                _cycleTitleText.text = $"Ciclo {dnc.CurrentCycleNumber}/{DayNightCycleManager.CAMPAIGN_TOTAL_CYCLES} — {phaseName} (~{minsRemaining}m rest.)";

                _weatherBadgeText.text = GetSpanishWeatherDescription(dnc.CurrentWeather, dnc.CurrentWeatherCoolingMultiplier);
                _weatherBadgeBg.color = dnc.IsBlindBlizzardActive
                    ? new Color(0.22f, 0.42f, 0.62f, 0.95f)
                    : ColCardInner;
            }

            int coatTier = _tempSystem != null ? _tempSystem.ThermalCoatTier : 1;
            int coatPct = _tempSystem != null ? Mathf.RoundToInt(_tempSystem.CoatCoolingReduction * 100f) : 0;
            string headlampState = _player.IsHeadlampEnabled ? "ENCENDIDO [F]" : "APAGADO [F]";

            _equipmentSummaryText.text =
                $"⛏️ Pico Cristal Energético: Nivel {_player.CurrentPickaxeTier} (Daño {_player.PickaxeDamage})\n" +
                $"🧥 Abrigo Térmico + Botas: Nivel {coatTier} (-{coatPct}% enfriamiento)\n" +
                $"🔦 Faro LED Frontal Casco: {headlampState}";

            if (_inventoryShop != null && _inventoryShop.HasGiftRadarUnlocked)
            {
                float d = FindNearestFrozenChestDistance();
                _radarStatusText.text = d < 999f
                    ? $"📡 RADAR DE REGALOS: Cofre Congelado a {d:0.0} m"
                    : "📡 RADAR DE REGALOS: Zona despejada de cofres";
            }
        }

        private void UpdateHotbarAndToast()
        {
            if (_statusToastText != null)
            {
                _statusToastText.text = $"📢 {_player.LastStatusMessage}";
            }

            if (_inventoryShop == null || _hotbarCountTexts.Count < 7)
            {
                return;
            }

            int[] counts =
            {
                _inventoryShop.HotChocolateThermosCount,
                _inventoryShop.GingerbreadCookieCount,
                _inventoryShop.PeppermintCaneCount,
                _inventoryShop.StringLightsStock,
                _inventoryShop.ThermalBeaconsStock,
                _inventoryShop.FestiveFirecrackersStock,
                _inventoryShop.MechanicalElfExtractorsStock
            };

            for (int i = 0; i < 7; i++)
            {
                _hotbarCountTexts[i].text = $"x{counts[i]}";
                _hotbarCountTexts[i].color = counts[i] > 0 ? ColTextLight : ColCandyRed;
            }
        }

        private void UpdateCrosshairInteractionPrompt()
        {
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
                    _interactionPromptText.text = prompt;
                    return;
                }
            }

            _interactionCardRoot.SetActive(false);
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

        private void UpdateShopModalVisibility()
        {
            if (_shopModalRoot == null)
            {
                return;
            }

            bool shouldShow = _player.IsShopMenuOpen;
            if (_shopModalRoot.activeSelf != shouldShow)
            {
                _shopModalRoot.SetActive(shouldShow);
                if (shouldShow)
                {
                    RebuildShopCardsList();
                }
            }

            if (shouldShow && _inventoryShop != null)
            {
                _shopCoinsHeaderText.text = $"🪙 Saldo: {_inventoryShop.GingerbreadCoins} Monedas de Jengibre";
                if (_shopHopperButtonText != null)
                {
                    _shopHopperButtonText.text = $"📥 DEPOSITAR TANQUE-MOCHILA EN TOLVA CENTRAL ({_inventoryShop.CurrentBackpackCount}/{_inventoryShop.MaxBackpackSlots})";
                }
            }
        }

        private static string GetSpanishWeatherDescription(WeatherState state, float mult)
        {
            switch (state)
            {
                case WeatherState.ClearArcticDay:
                    return $"☀️ Clima: Día Ártico Despejado (Enfriamiento x{mult:0.0})";
                case WeatherState.PolarNight:
                    return $"🌙 Clima: Noche Polar con Auroras (Enfriamiento x{mult:0.0})";
                case WeatherState.BlindBlizzard:
                    return $"🌨️ ¡EVENTO: VENTISCA CIEGA! (Enfriamiento x{mult:0.0})";
                case WeatherState.ChristmasEveOvercharge:
                    return $"🎄 ¡LA GRAN NOCHEBUENA! (Recursos 2x | Tormenta x{mult:0.0})";
                default:
                    return "Clima Polar";
            }
        }

        private static string GetCategorySpanishName(ShopCategory category)
        {
            switch (category)
            {
                case ShopCategory.Consumable: return "Comida";
                case ShopCategory.TacticalTool: return "Táctico";
                case ShopCategory.PermanentUpgrade: return "Mejora";
                case ShopCategory.Automation: return "Autómata";
                default: return "Taller";
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

        private Image CreateProgressBar(
            RectTransform parent,
            string name,
            Vector2 anchoredPos,
            Vector2 size,
            Color fillColor,
            out Text valueLabel)
        {
            RectTransform bgRect = CreatePanelCard(name + "_BG", parent,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                anchoredPos, size, new Color(0.03f, 0.06f, 0.09f, 0.95f));

            GameObject fillObj = CreateUIObject(name + "_Fill", bgRect);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            StretchFull(fillRect, padding: 2f);

            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.sprite = _flatBarSprite;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 1f;
            fillImg.color = fillColor;

            valueLabel = CreateLabel(bgRect, name + "_ValueTxt", "100 / 100",
                Vector2.zero, size, 12, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            StretchFull(valueLabel.GetComponent<RectTransform>());

            return fillImg;
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

        private Button CreateStylizedButton(
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

        private Button CreateStylizedButton(
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

            Button btn = btnRect.gameObject.AddComponent<Button>();
            btn.targetGraphic = btnRect.GetComponent<Image>();
            btn.onClick.AddListener(onClick);

            Color textCol = (bgColor.r + bgColor.g + bgColor.b) > 1.65f ? ColPanelDark : Color.white;
            buttonLabel = CreateLabel(btnRect, "BtnTxt", label,
                Vector2.zero, size, 13, FontStyle.Bold, textCol, TextAnchor.MiddleCenter);
            StretchFull(buttonLabel.GetComponent<RectTransform>());

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
