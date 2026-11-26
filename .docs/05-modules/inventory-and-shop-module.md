# Módulo M-04: Inventario del Tanque-Mochila, Tolva y Tienda (`InventoryAndShop.cs`)

* **Namespace:** `Boreatek.Economy`
* **Ruta destino en Unity:** `Assets/_Project/Scripts/Economy/InventoryAndShop.cs`

---

## 1. Responsabilidades del Módulo

1. Gestionar el almacenamiento temporal de recursos dentro del **Tanque-Mochila Presurizado de Latón** del jugador, respetando el límite de ranuras según su nivel de mejora (`10`, `20` o `35`).
2. Aplicar la penalización del `30 %` de pérdida de materiales no asegurados cuando `TemperatureSystem` emite `OnHypothermiaRescueTriggered`.
3. Procesar la descarga de materiales en la **Tolva Central** de la Aldea Industrial, convirtiendo cada recurso en **Monedas de Jengibre / Fichas del Taller**.
4. Administrar las compras en las 4 categorías de la tienda:
   * **Comida y Bebidas:** Termo de chocolate caliente, Galleta de jengibre, Bastón de menta.
   * **Herramientas Tácticas:** Guirnaldas luminosas, Baliza térmica, Petardos festivos, Radar de regalos.
   * **Mejoras Permanentes:** Pico Energético Nv. 2/3, Tanque-Mochila Nv. 2/3, Abrigo Térmico Nv. 2/3.
   * **Automatización:** Duendes Mecánicos Extractores y Cintas Transportadoras.

---

## 2. Contrato de Clase

```csharp
namespace Boreatek.Economy
{
    public class InventoryAndShop : MonoBehaviour
    {
        [Header("Brass Tank-Backpack Settings")]
        [SerializeField] private int _maxBackpackCapacity = 10;
        [SerializeField] private float _hypothermiaLossRatio = 0.30f;

        public int CurrentGingerbreadCoins => _gingerbreadCoins;
        public int CurrentBackpackLoad => _currentTotalItems;
        public int MaxBackpackCapacity => _maxBackpackCapacity;

        public event Action<int, int> OnBackpackLoadChanged;
        public event Action<int> OnGingerbreadCoinsChanged;
        public event Action<ShopItemSO> OnShopItemPurchased;

        public bool TryAddResource(ResourceDataSO resource, int amount) { /* ... */ }
        public int DepositAllResourcesIntoHopper() { /* ... */ }
        public bool TryPurchaseShopItem(ShopItemSO item) { /* ... */ }
    }
}
```
