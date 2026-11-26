using Boreatek.Core;
using Boreatek.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Boreatek.UI
{
    public partial class BoreatekHUDAndShopUI
    {
        private static readonly (string label, string detail, BoreatekGlyph glyph, Color accent)[] EquipmentDisplay =
        {
            ("Pico", "Extrae madera y minerales, y golpea enemigos.", BoreatekGlyph.Pickaxe, new Color32(115, 217, 242, 255)),
            ("Chocolate", "Recupera todo el calor y protege del frío durante 60 segundos.", BoreatekGlyph.Thermos, new Color32(237, 184, 106, 255)),
            ("Galleta", "Aumenta la velocidad al moverte y picar durante 45 segundos.", BoreatekGlyph.Cookie, new Color32(247, 190, 95, 255)),
            ("Menta", "Recupera hasta 50 puntos de salud.", BoreatekGlyph.Candy, new Color32(250, 129, 141, 255)),
            ("Guirnalda", "Deja una fuente de luz y calor en el suelo.", BoreatekGlyph.Garland, new Color32(239, 208, 141, 255)),
            ("Baliza", "Calienta y derrite cofres congelados. Se recoge con E.", BoreatekGlyph.Beacon, new Color32(255, 164, 66, 255)),
            ("Petardo", "Fractura hielo y roca y alcanza enemigos cercanos.", BoreatekGlyph.Firecracker, new Color32(254, 128, 119, 255)),
            ("Autómata", "Instala un extractor sobre una veta cercana.", BoreatekGlyph.Automaton, new Color32(106, 205, 238, 255))
        };
        private static readonly ResourceType[] PackResources = (ResourceType[])System.Enum.GetValues(typeof(ResourceType));
        private readonly BoreatekUiButton[] _backpackSlots = new BoreatekUiButton[EquipmentDisplay.Length];
        private readonly Text[] _backpackStock = new Text[EquipmentDisplay.Length];
        private readonly Text[] _resourceLines = new Text[PackResources.Length];
        private RectTransform _backpackPanel;
        private Text _capacityLabel, _backpackDetails, _backpackFeedback;
        private Button _wearEquipment, _consumeEquipment;
        private EquipmentSlot _backpackSelection;

        private void BuildBackpackScreen(RectTransform parent, BoreatekGameSession session)
        {
            Vector2 center = Vector2.one * 0.5f;
            _backpackPanel = CreatePanelCard("Backpack", parent, center, center, center, Vector2.zero,
                new Vector2(1010f, 605f), ColDeepSteel);
            ApplyMetalPanel(_backpackPanel, ColDeepSteel, ColSteelRim);
            CreateLabel(_backpackPanel, "Title", "Mochila de expedición", new Vector2(26f, -16f),
                new Vector2(710f, 49f), 39, FontStyle.Bold, ColTextLight, TextAnchor.MiddleLeft);
            CreateMenuButton(_backpackPanel, "CloseBackpack", "Cerrar [B]", 840f, 15f, 145f, ColBlueSteel, session.CloseBackpack);
            CreateAccentLine(_backpackPanel, "HeaderRule", new Vector2(26f, -83f), new Vector2(959f, 2f), ColBrassRim);
            CreateLabel(_backpackPanel, "EquipmentTitle", "Equipo y provisiones", new Vector2(26f, -99f),
                new Vector2(565f, 33f), 24, FontStyle.Bold, ColIceCyan, TextAnchor.MiddleLeft);

            var topLeft = new Vector2(0f, 1f);
            for (int i = 0; i < EquipmentDisplay.Length; i++)
            {
                EquipmentSlot slot = (EquipmentSlot)i;
                var entry = EquipmentDisplay[i];
                RectTransform card = CreatePanelCard($"Pack_{slot}", _backpackPanel, topLeft, topLeft, topLeft,
                    new Vector2(26f + (i % 4) * 151f, -145f - (i / 4) * 93f), new Vector2(141f, 81f), ColBlueSteel);
                _backpackSlots[i] = AttachMetalButton(card, ColBlueSteel, ColBrassRim);
                _backpackSlots[i].onClick.AddListener(() => SelectBackpackItem(slot));
                AddHudGlyph(card, "EquipmentGlyph", entry.glyph, topLeft, new Vector2(33f, -28f), 30f, entry.accent);
                _backpackStock[i] = CreateLabel(card, "Stock", "", new Vector2(75f, -10f), new Vector2(52f, 28f),
                    16, FontStyle.Bold, ColTextMuted, TextAnchor.MiddleRight);
                CreateLabel(card, "EquipmentName", entry.label, new Vector2(12f, -49f), new Vector2(117f, 24f),
                    17, FontStyle.Bold, ColTextLight, TextAnchor.MiddleCenter);
            }

            _backpackDetails = CreateLabel(_backpackPanel, "Details", "", new Vector2(28f, -350f), new Vector2(580f, 55f),
                19, FontStyle.Normal, ColTextLight, TextAnchor.MiddleLeft);
            _wearEquipment = CreateMenuButton(_backpackPanel, "EquipItem", "Equipar", 26f, 426f, 283f,
                ColBlueSteel, () => session.EquipFromBackpack(_backpackSelection));
            _consumeEquipment = CreateMenuButton(_backpackPanel, "ConsumeItem", "Consumir", 320f, 426f, 299f,
                ColBrassGold, () =>
                {
                    if (session.ConsumeFromBackpack(_backpackSelection))
                    {
                        _backpackFeedback.text = RemoveMessageGlyphPrefix(_player.LastStatusMessage);
                        RefreshBackpackContents();
                    }
                });
            _backpackFeedback = CreateLabel(_backpackPanel, "Feedback", "", new Vector2(28f, -503f), new Vector2(578f, 64f),
                17, FontStyle.Normal, ColIceCyan, TextAnchor.UpperLeft);

            RectTransform cargo = CreatePanelCard("Cargo", _backpackPanel, topLeft, topLeft, topLeft,
                new Vector2(644f, -102f), new Vector2(341f, 467f), ColPanelDark);
            ApplyMetalPanel(cargo, ColPanelDark, ColSteelRim);
            CreateLabel(cargo, "CargoTitle", "Carga de minerales", new Vector2(18f, -16f), new Vector2(306f, 31f),
                23, FontStyle.Bold, ColTextLight, TextAnchor.MiddleLeft);
            _capacityLabel = CreateLabel(cargo, "Capacity", "", new Vector2(18f, -53f), new Vector2(306f, 29f),
                18, FontStyle.Bold, ColWarmAmber, TextAnchor.MiddleLeft);
            CreateAccentLine(cargo, "CargoRule", new Vector2(18f, -91f), new Vector2(303f, 2f), ColSteelRim);
            for (int i = 0; i < PackResources.Length; i++)
                _resourceLines[i] = CreateLabel(cargo, $"Cargo_{PackResources[i]}", "",
                    new Vector2(18f, -103f - i * 39f), new Vector2(306f, 34f), 18, FontStyle.Normal,
                    ColTextMuted, TextAnchor.MiddleLeft);
        }

        private void OpenBackpackScreen()
        {
            _backpackFeedback.text = "";
            SelectBackpackItem(_player.SelectedEquipment);
        }

        private void SelectBackpackItem(EquipmentSlot slot)
        {
            _backpackSlots[(int)_backpackSelection].SetLatched(false);
            _backpackSelection = slot;
            _backpackSlots[(int)slot].SetLatched(true);
            _backpackDetails.text = EquipmentDisplay[(int)slot].detail;
            _backpackFeedback.text = "";
            RefreshBackpackContents();
        }

        private void RefreshBackpackContents()
        {
            for (int i = 0; i < _backpackStock.Length; i++)
            {
                int stock = _inventoryShop.GetEquipmentStock((EquipmentSlot)i);
                _backpackStock[i].text = i == (int)EquipmentSlot.Pickaxe ? $"Nv. {_player.CurrentPickaxeTier}" : $"×{stock}";
                _backpackStock[i].color = stock > 0 ? ColTextLight : ColTextMuted;
            }
            _capacityLabel.text = $"{_inventoryShop.CurrentBackpackCount} / {_inventoryShop.MaxBackpackSlots} espacios";
            for (int i = 0; i < PackResources.Length; i++)
            {
                ResourceType type = PackResources[i];
                _inventoryShop.BackpackContents.TryGetValue(type, out int count);
                string name = _inventoryShop.GetResourceDefinition(type)?.DisplayName ?? type.ToString();
                _resourceLines[i].text = $"{name}  ×{count}";
                _resourceLines[i].color = count > 0 ? ColTextLight : ColTextMuted;
            }
            bool hasItem = _inventoryShop.GetEquipmentStock(_backpackSelection) > 0;
            _wearEquipment.interactable = hasItem;
            _consumeEquipment.gameObject.SetActive(PlayerController.IsConsumable(_backpackSelection));
            _consumeEquipment.interactable = hasItem;
        }
    }
}
