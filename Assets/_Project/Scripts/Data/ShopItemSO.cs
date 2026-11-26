using Boreatek.Core;
using UnityEngine;

namespace Boreatek.Data
{
    /// <summary>
    /// Define un artículo o mejora disponible en la Tienda del Taller de la Aldea Industrial.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_ShopItem_", menuName = "Boreatek/Data/Shop Item")]
    public class ShopItemSO : ScriptableObject
    {
        [Header("Item Identity")]
        [SerializeField] private string _itemId = "shop_item";
        [SerializeField] private string _displayName = "Artículo del Taller";
        [SerializeField] private ShopCategory _category = ShopCategory.Consumable;
        [SerializeField] private ShopActionType _actionType = ShopActionType.HotChocolateThermos;

        [TextArea(2, 3)]
        [SerializeField] private string _description = "Descripción del artículo.";

        [Header("Cost & Tier")]
        [Min(1)]
        [SerializeField] private int _costInGingerbreadCoins = 30;

        [Tooltip("Para mejoras permanentes: nivel que desbloquea (2 o 3). Para consumibles/tácticos: cantidad entregada.")]
        [Min(1)]
        [SerializeField] private int _amountOrTargetTier = 1;

        [Tooltip("Valor numérico del efecto (ej. 60s de chocolate caliente, 20 slots de mochila, 0.25 resistencia térmica).")]
        [SerializeField] private float _effectMagnitude = 1f;

        public string ItemId => _itemId;
        public string DisplayName => _displayName;
        public ShopCategory Category => _category;
        public ShopActionType ActionType => _actionType;
        public string Description => _description;
        public int CostInGingerbreadCoins => _costInGingerbreadCoins;
        public int AmountOrTargetTier => _amountOrTargetTier;
        public float EffectMagnitude => _effectMagnitude;

        public void InitializeRuntime(
            string id,
            string displayName,
            ShopCategory category,
            ShopActionType actionType,
            string description,
            int cost,
            int amountOrTier,
            float effectMagnitude = 1f)
        {
            _itemId = id;
            _displayName = displayName;
            _category = category;
            _actionType = actionType;
            _description = description;
            _costInGingerbreadCoins = cost;
            _amountOrTargetTier = amountOrTier;
            _effectMagnitude = effectMagnitude;
        }
    }
}
