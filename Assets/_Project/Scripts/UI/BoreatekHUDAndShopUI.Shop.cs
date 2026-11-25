using System.Collections.Generic;
using Boreatek.Core;
using Boreatek.Data;
using Boreatek.Economy;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Boreatek.UI
{
    public partial class BoreatekHUDAndShopUI
    {
        private const float ShopRowPitch = 121f;
        private GameObject _shopModalRoot;
        private ScrollRect _shopCatalogScroll;
        private Text _shopCoinsHeaderText;
        private Text _shopStatusText;
        private Text _shopDepositText;
        private Button _shopDepositButton;
        private Button _shopCloseButton;
        private BoreatekUiButton _selectedShopTab;
        private ShopCategory? _selectedShopCategoryFilter;
        private int _shownShopCoins = -1;
        private int _shownShopLoad = -1;
        private int _shownShopCapacity = -1;
        private readonly List<ShopPurchaseControl> _shopPurchaseControls = new List<ShopPurchaseControl>();

        private readonly struct ShopPurchaseControl
        {
            public readonly ShopItemSO Item;
            public readonly Button Button;
            public readonly Text Label;

            public ShopPurchaseControl(ShopItemSO item, Button button, Text label)
            {
                Item = item;
                Button = button;
                Label = label;
            }
        }

        private void BuildWorkshopShopModal(RectTransform parent)
        {
            _shopModalRoot = CreateUIObject("WorkshopShop_Modal", parent);
            RectTransform backdrop = (RectTransform)_shopModalRoot.transform;
            StretchFull(backdrop);
            backdrop.gameObject.AddComponent<Image>().color = new Color(0.008f, 0.02f, 0.035f, 0.79f);

            var center = new Vector2(0.5f, 0.5f);
            RectTransform panel = CreatePanelCard("WorkshopPanel", backdrop, center, center, center,
                Vector2.zero, new Vector2(1148f, 700f), ColDeepSteel);
            ApplyMetalPanel(panel, ColDeepSteel, ColSteelRim);

            CreateLabel(panel, "ShopTitle", "Taller de la aldea", new Vector2(26f, -21f), new Vector2(590f, 52f),
                36, FontStyle.Bold, ColTextLight, TextAnchor.MiddleLeft);
            CreateAccentLine(panel, "HeaderRule", new Vector2(26f, -87f), new Vector2(1096f, 2f), ColBrassRim);

            var topLeft = new Vector2(0f, 1f);
            RectTransform balance = CreatePanelCard("BalancePlate", panel, topLeft, topLeft, topLeft,
                new Vector2(725f, -26f), new Vector2(240f, 44f), ColBlueSteel);
            ApplyMetalPanel(balance, ColBlueSteel, ColSteelRim);
            _shopCoinsHeaderText = CreateLabel(balance, "ShopCoinsBalance", "", Vector2.zero,
                new Vector2(240f, 44f), 18, FontStyle.Bold, ColBrassRim, TextAnchor.MiddleCenter);
            StretchFull(_shopCoinsHeaderText.rectTransform, 4f);

            _shopCloseButton = CreateStylizedButton(panel, "BtnCloseShop", "Cerrar  [Tab]",
                new Vector2(980f, -26f), new Vector2(142f, 44f), ColBlueSteel, () => _player?.ToggleShopMenu());

            _selectedShopCategoryFilter = null;
            _selectedShopTab = CreateShopFilter(panel, "TabAll", "Todas", 26f, null);
            _selectedShopTab.SetLatched(true);
            CreateShopFilter(panel, "TabConsumables", "Comida", 146f, ShopCategory.Consumable);
            CreateShopFilter(panel, "TabTactical", "Tácticos", 266f, ShopCategory.TacticalTool);
            CreateShopFilter(panel, "TabUpgrades", "Mejoras", 386f, ShopCategory.PermanentUpgrade);
            CreateShopFilter(panel, "TabAuto", "Autómatas", 506f, ShopCategory.Automation);

            _shopDepositButton = CreateStylizedButton(panel, "BtnDepositHopper", "Depositar en la tolva",
                new Vector2(828f, -105f), new Vector2(294f, 48f), ColBrassGold, DepositShopLoad, out _shopDepositText);
            _shopCatalogScroll = CreateVerticalCatalog(panel, "ShopCardsGridArea", "ShopCards",
                new Vector2(26f, -168f), new Vector2(1074f, 467f));
            _shopStatusText = CreateLabel(panel, "ShopStatus", "", new Vector2(26f, -649f), new Vector2(1096f, 28f),
                16, FontStyle.Bold, ColIceCyan, TextAnchor.MiddleLeft);
            _shopModalRoot.SetActive(false);
        }

        private BoreatekUiButton CreateShopFilter(RectTransform panel, string name, string label, float x, ShopCategory? category)
        {
            var filter = CreateStylizedButton(panel, name, label, new Vector2(x, -105f),
                new Vector2(110f, 48f), ColBlueSteel, null);
            filter.onClick.AddListener(() =>
            {
                if (_selectedShopTab == filter) return;
                _selectedShopTab.SetLatched(false);
                _selectedShopTab = filter;
                filter.SetLatched(true);
                _selectedShopCategoryFilter = category;
                RebuildShopCardsList();
            });
            return filter;
        }

        private void RebuildShopCardsList()
        {
            if (_inventoryShop == null)
            {
                return;
            }

            RectTransform content = _shopCatalogScroll.content;
            _shopPurchaseControls.Clear();
            foreach (Transform child in content)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            int index = 0;
            foreach (ShopItemSO item in _inventoryShop.ShopCatalog)
            {
                if (!_selectedShopCategoryFilter.HasValue || item.Category == _selectedShopCategoryFilter.Value)
                {
                    BuildShopItem(content, item, index++);
                }
            }

            int rows = (index + 1) / 2;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Max(_shopCatalogScroll.viewport.rect.height, 10f + rows * ShopRowPitch));
            _shopCatalogScroll.StopMovement();
            content.anchoredPosition = Vector2.zero;
            RefreshShopValues(force: true);
        }

        private void BuildShopItem(RectTransform parent, ShopItemSO item, int index)
        {
            var topLeft = new Vector2(0f, 1f);
            float x = 10f + index % 2 * 532f;
            float y = -10f - index / 2 * ShopRowPitch;
            RectTransform card = CreatePanelCard($"ShopCard_{item.ItemId}", parent, topLeft, topLeft, topLeft,
                new Vector2(x, y), new Vector2(522f, 111f), ColBlueSteel);
            ApplyMetalPanel(card, ColBlueSteel, ColSteelRim);

            Color accent = item.Category switch
            {
                ShopCategory.Consumable => ColBrassRim,
                ShopCategory.TacticalTool => ColIceCyan,
                ShopCategory.PermanentUpgrade => new Color32(255, 164, 122, 255),
                _ => new Color32(137, 224, 200, 255)
            };
            string category = item.Category switch
            {
                ShopCategory.Consumable => "Comida",
                ShopCategory.TacticalTool => "Táctico",
                ShopCategory.PermanentUpgrade => "Mejora",
                _ => "Automatización"
            };
            CreateAccentLine(card, "CategoryMark", new Vector2(16f, -14f), new Vector2(15f, 2f), accent);
            CreateLabel(card, "CategoryLabel", category, new Vector2(39f, -6f), new Vector2(315f, 22f),
                14, FontStyle.Bold, accent, TextAnchor.MiddleLeft);

            Text title = CreateLabel(card, "ItemTitle", item.DisplayName, new Vector2(16f, -29f), new Vector2(350f, 27f),
                20, FontStyle.Bold, ColTextLight, TextAnchor.MiddleLeft);
            FitCatalogLabel(title, 15, 20);
            Text description = CreateLabel(card, "ItemDesc", item.Description, new Vector2(16f, -59f), new Vector2(350f, 42f),
                16, FontStyle.Normal, ColTextMuted, TextAnchor.UpperLeft);
            FitCatalogLabel(description, 13, 16);

            var purchase = CreateStylizedButton(card, "BtnBuy", "", new Vector2(378f, -29f), new Vector2(128f, 55f),
                ColBrassGold, () => BuyShopItem(item), out Text buyLabel);
            purchase.Focused += () => RevealCatalogItem(_shopCatalogScroll, card);
            _shopPurchaseControls.Add(new ShopPurchaseControl(item, purchase, buyLabel));
        }

        private static void FitCatalogLabel(Text label, int minSize, int maxSize)
        {
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = minSize;
            label.resizeTextMaxSize = maxSize;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void BuyShopItem(ShopItemSO item)
        {
            if (_inventoryShop != null && _inventoryShop.TryPurchaseShopItem(item))
            {
                _shopStatusText.text = $"Compra realizada · {item.DisplayName}";
                RefreshShopValues(force: true);
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
                {
                    _selectedShopTab.Select();
                }
            }
        }

        private void DepositShopLoad()
        {
            if (_inventoryShop == null || _inventoryShop.CurrentBackpackCount == 0)
            {
                return;
            }
            int coins = _inventoryShop.DepositAllResourcesIntoCentralHopper();
            _shopStatusText.text = $"Carga entregada · +{coins} monedas";
            RefreshShopValues(force: true);
            _selectedShopTab.Select();
        }

        private void RefreshShopValues(bool force = false)
        {
            if (_inventoryShop == null)
            {
                return;
            }

            int coins = _inventoryShop.GingerbreadCoins;
            if (force || _shownShopCoins != coins)
            {
                _shownShopCoins = coins;
                _shopCoinsHeaderText.text = $"{coins} monedas de jengibre";
                foreach (var control in _shopPurchaseControls)
                {
                    var state = _inventoryShop.GetPurchaseAvailability(control.Item);
                    control.Button.interactable = state == InventoryAndShop.PurchaseAvailability.Available;
                    int price = control.Item.CostInGingerbreadCoins;
                    control.Label.text = state switch
                    {
                        InventoryAndShop.PurchaseAvailability.Available => $"Comprar\n{price} monedas",
                        InventoryAndShop.PurchaseAvailability.AlreadyOwned => "Ya instalado",
                        _ => $"Sin saldo\n{price} monedas"
                    };
                }
            }

            int load = _inventoryShop.CurrentBackpackCount;
            int slots = _inventoryShop.MaxBackpackSlots;
            if (force || _shownShopLoad != load || _shownShopCapacity != slots)
            {
                _shownShopLoad = load;
                _shownShopCapacity = slots;
                _shopDepositButton.interactable = load > 0;
                _shopDepositText.text = load > 0 ? $"Depositar en la tolva · {load}/{slots}" : "Mochila vacía";
            }
        }

        private void UpdateShopModalVisibility()
        {
            bool show = _player.IsShopMenuOpen;
            if (_shopModalRoot.activeSelf != show)
            {
                _shopModalRoot.SetActive(show);
                if (show)
                {
                    _shopStatusText.text = "";
                    RebuildShopCardsList();
                    _shopCloseButton.Select();
                }
                else
                {
                    EventSystem events = EventSystem.current;
                    if (events != null && events.currentSelectedGameObject != null &&
                        events.currentSelectedGameObject.transform.IsChildOf(_shopModalRoot.transform))
                    {
                        events.SetSelectedGameObject(null);
                    }
                }
            }
            if (show)
            {
                RefreshShopValues();
            }
        }
    }
}
