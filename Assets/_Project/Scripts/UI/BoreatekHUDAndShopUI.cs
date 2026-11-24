using System.Collections.Generic;
using Boreatek.Core;
using Boreatek.Data;
using Boreatek.Economy;
using Boreatek.Player;
using Boreatek.World;
using UnityEngine;

namespace Boreatek.UI
{
    /// <summary>
    /// Interfaz gráfica (HUD Térmico, Manómetro del Tanque-Mochila, Reloj de Campaña de 20 Horas,
    /// Radar de Regalos, Retícula de Interacción y Ventana de la Tienda del Taller).
    /// </summary>
    public class BoreatekHUDAndShopUI : MonoBehaviour
    {
        private PlayerController _player;
        private TemperatureSystem _tempSystem;
        private InventoryAndShop _inventoryShop;

        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _promptStyle;
        private GUIStyle _boxStyle;
        private Vector2 _shopScrollPos;

        public void BindPlayer(PlayerController player)
        {
            _player = player;
            if (_player != null)
            {
                _tempSystem = _player.GetComponent<TemperatureSystem>();
                _inventoryShop = _player.GetComponent<InventoryAndShop>();
            }
        }

        private void EnsureStyles()
        {
            if (_headerStyle != null)
            {
                return;
            }

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.78f, 0.25f) }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.90f, 0.95f, 1.0f) }
            };

            _promptStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.92f, 0.55f) }
            };

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 13,
                alignment = TextAnchor.UpperLeft
            };
        }

        private void OnGUI()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerController>();
                if (_player != null)
                {
                    BindPlayer(_player);
                }
                return;
            }

            EnsureStyles();

            DrawFrostWarningOverlay();
            DrawTopLeftVitalAndThermalPanel();
            DrawTopRightCampaignClockAndEquipmentPanel();
            DrawBottomQuickbarAndStatusBanner();
            DrawCenterCrosshairAndInteractionPrompt();

            if (_player.IsShopMenuOpen)
            {
                DrawWorkshopShopWindow();
            }
        }

        private void DrawFrostWarningOverlay()
        {
            if (_tempSystem == null || _tempSystem.CurrentTemperature >= 32f)
            {
                return;
            }

            float alpha = Mathf.Lerp(0.45f, 0.05f, _tempSystem.CurrentTemperature / 32f);
            Color prev = GUI.color;
            GUI.color = new Color(0.37f, 0.66f, 0.83f, alpha);
            GUI.Box(new Rect(0f, 0f, Screen.width, 28f), "❄️ ¡ALERTA DE HIPOTERMIA! Busca una Guirnalda [G], Baliza [H], Chocolate Caliente [1] o regresa a la Aldea.");
            GUI.color = prev;
        }

        private void DrawTopLeftVitalAndThermalPanel()
        {
            GUILayout.BeginArea(new Rect(16f, 16f, 360f, 235f), GUI.skin.box);
            GUILayout.Label("🎄⚙️ BOREATEK — TELEMETRÍA DEL DUENDE", _headerStyle);

            if (_tempSystem != null)
            {
                string heatStatus = _tempSystem.IsInsideHeatBubble
                    ? "🔥 EN BURBUJA TÉRMICA"
                    : (_tempSystem.IsHotChocolateActive ? $"☕ CHOCOLATE ({Mathf.CeilToInt(_tempSystem.HotChocolateRemainingSeconds)}s)" : "❄️ EXPUESTO AL FRÍO");

                GUILayout.Label($"🌡️ Temperatura Corporal: {Mathf.RoundToInt(_tempSystem.CurrentTemperature)} / 100 °T  [{heatStatus}]", _labelStyle);
                GUILayout.Label($"❤️ Salud Estructural: {Mathf.RoundToInt(_tempSystem.CurrentHealth)} / 100 HP", _labelStyle);
                GUILayout.Label($"📍 Bioma / Zona: {_tempSystem.ActiveHeatSourceName}", _labelStyle);
            }

            if (_inventoryShop != null)
            {
                GUILayout.Space(4f);
                GUILayout.Label($"🪙 Monedas de Jengibre (Taller): {_inventoryShop.GingerbreadCoins}", _headerStyle);
                GUILayout.Label($"🎒 Tanque-Mochila Latón (Nv.{_inventoryShop.BackpackTier}): {_inventoryShop.CurrentBackpackCount} / {_inventoryShop.MaxBackpackSlots} ranuras", _labelStyle);
                GUILayout.Label($"🌋 Núcleos Luminosos en Forja: {_inventoryShop.LuminousCoresDeliveredToForge} / {_inventoryShop.LuminousCoresRequiredForChristmasEve}", _labelStyle);
            }

            GUILayout.EndArea();
        }

        private void DrawTopRightCampaignClockAndEquipmentPanel()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 376f, 16f, 360f, 245f), GUI.skin.box);
            GUILayout.Label("⏱️ CAMPAÑA DE 20 HORAS & EQUIPO", _headerStyle);

            DayNightCycleManager dnc = DayNightCycleManager.Instance;
            if (dnc != null)
            {
                string phase = dnc.IsNightPhase ? "🌙 Noche Polar (10m)" : "☀️ Día Ártico Tenue (10m)";
                int cycleMinutesLeft = Mathf.CeilToInt((1f - dnc.NormalizedCycleTime) * 20f);
                GUILayout.Label($"Ciclo {dnc.CurrentCycleNumber}/{DayNightCycleManager.CAMPAIGN_TOTAL_CYCLES} — {phase} (~{cycleMinutesLeft} min restantes)", _labelStyle);
                GUILayout.Label($"🌨️ Clima: {dnc.CurrentWeather} (Enfriamiento x{dnc.CurrentWeatherCoolingMultiplier:0.0})", _labelStyle);
            }

            int coatTier = _tempSystem != null ? _tempSystem.ThermalCoatTier : 1;
            int coatRed = _tempSystem != null ? Mathf.RoundToInt(_tempSystem.CoatCoolingReduction * 100f) : 0;
            GUILayout.Label($"⛏️ Pico Cristal Energético: Nivel {_player.CurrentPickaxeTier} (Daño {_player.PickaxeDamage})", _labelStyle);
            GUILayout.Label($"🧥 Abrigo Térmico + Botas: Nivel {coatTier} (-{coatRed}% frío)", _labelStyle);
            GUILayout.Label($"🔦 Faro LED Casco: {(_player.IsHeadlampEnabled ? "ENCENDIDO [F]" : "APAGADO [F]")}", _labelStyle);

            if (_inventoryShop != null && _inventoryShop.HasGiftRadarUnlocked)
            {
                float nearestChest = FindNearestFrozenChestDistance();
                string radarText = nearestChest < 999f
                    ? $"📡 Radar de Regalos: Cofre Congelado a {nearestChest:0.0} m"
                    : "📡 Radar de Regalos: Todos los cofres cercanos abiertos";
                GUILayout.Label(radarText, _headerStyle);
            }

            GUILayout.EndArea();
        }

        private void DrawBottomQuickbarAndStatusBanner()
        {
            if (_inventoryShop == null)
            {
                return;
            }

            Rect bottomRect = new Rect(16f, Screen.height - 108f, Screen.width - 32f, 92f);
            GUILayout.BeginArea(bottomRect, GUI.skin.box);

            GUILayout.Label($"📢 {_player.LastStatusMessage}", _headerStyle);

            string quickbar =
                $"[1] ☕ Termo Chocolate ({_inventoryShop.HotChocolateThermosCount})   |   " +
                $"[2] 🍪 Galleta Jengibre ({_inventoryShop.GingerbreadCookieCount})   |   " +
                $"[3] 🍬 Bastón Menta ({_inventoryShop.PeppermintCaneCount})   |   " +
                $"[G] 💡 Guirnalda ({_inventoryShop.StringLightsStock})   |   " +
                $"[H] 🔥 Baliza Térmica ({_inventoryShop.ThermalBeaconsStock})   |   " +
                $"[Q] 🧨 Petardo ({_inventoryShop.FestiveFirecrackersStock})   |   " +
                $"[R] 🤖 Duende Extractor ({_inventoryShop.MechanicalElfExtractorsStock})";
            GUILayout.Label(quickbar, _labelStyle);

            string controls = "Controles: [WASD] Mover | [Shift] Correr | [Clic Izq] Picar | [E] Interactuar/Tolva | [F] Faro Casco | [V] Cámara 1ra/3ra | [Tab] Tienda del Taller";
            GUILayout.Label(controls, _labelStyle);

            GUILayout.EndArea();
        }

        private void DrawCenterCrosshairAndInteractionPrompt()
        {
            if (_player.IsShopMenuOpen)
            {
                return;
            }

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            GUI.Box(new Rect(cx - 4f, cy - 4f, 8f, 8f), "");

            IInteractable focused = _player.CurrentFocusedInteractable;
            if (focused != null)
            {
                string prompt = focused.GetInteractionPrompt();
                if (!string.IsNullOrEmpty(prompt))
                {
                    GUI.Box(new Rect(cx - 290f, cy + 24f, 580f, 36f), prompt, _promptStyle);
                }
            }
        }

        private void DrawWorkshopShopWindow()
        {
            if (_inventoryShop == null)
            {
                return;
            }

            Rect winRect = new Rect(Screen.width * 0.5f - 390f, Screen.height * 0.5f - 250f, 780f, 500f);
            GUILayout.BeginArea(winRect, GUI.skin.window);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"🛒 TIENDA Y TALLER DE LA ALDEA INDUSTRIAL — Saldo: {_inventoryShop.GingerbreadCoins} Monedas de Jengibre", _headerStyle);
            if (GUILayout.Button("Cerrar [Tab]", GUILayout.Width(110f), GUILayout.Height(26f)))
            {
                _player.ToggleShopMenu();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            // Botón rápido para descargar en la Tolva Central si el jugador está dentro de la Aldea
            if (_tempSystem != null && _tempSystem.CurrentBiome == BiomeType.SantaIndustrialVillage)
            {
                if (GUILayout.Button($"📥 Depositar todo el Tanque-Mochila en la Tolva Central ({_inventoryShop.CurrentBackpackCount} minerales cargados)", GUILayout.Height(32f)))
                {
                    _inventoryShop.DepositAllResourcesIntoCentralHopper();
                }
            }

            GUILayout.Space(6f);
            _shopScrollPos = GUILayout.BeginScrollView(_shopScrollPos);

            IReadOnlyList<ShopItemSO> items = _inventoryShop.ShopCatalog;
            for (int i = 0; i < items.Count; i++)
            {
                ShopItemSO item = items[i];
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.BeginVertical(GUILayout.Width(540f));
                GUILayout.Label($"[{item.Category}] {item.DisplayName} — {item.CostInGingerbreadCoins} Monedas de Jengibre", _headerStyle);
                GUILayout.Label(item.Description, _labelStyle);
                GUILayout.EndVertical();

                if (GUILayout.Button($"Comprar ({item.CostInGingerbreadCoins} 🪙)", GUILayout.Width(170f), GUILayout.Height(42f)))
                {
                    _inventoryShop.TryPurchaseShopItem(item);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
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
    }
}
