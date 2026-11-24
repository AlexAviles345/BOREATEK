# Módulo M-03: Nodos Recolectables y Cofres Congelados (`HarvestableNode.cs`)

* **Namespace:** `Boreatek.World`
* **Ruta destino en Unity:** `Assets/_Project/Scripts/World/HarvestableNode.cs`

---

## 1. Responsabilidades del Módulo

1. Representar cualquier nodo de extracción del mapa:
   * **Superficie:** *Pinos Cristalizados*, *Árboles de Caramelo Gigantes*, *Trineos Estrellados* y *Cofres Congelados*.
   * **Subsuelo:** Vetas de *Carbón Negro*, *Cristal de Caramelo*, *Hierro Polar*, bloques de *Hielo Azul Profundo (Nivel 2)* y fisuras de *Núcleos Luminosos (Nivel 3)*.
2. Validar que el nivel del **Pico Industrial con Cristal Energético Rojo** del jugador sea igual o superior a `_config.RequiredPickaxeTier`.
3. En el caso de los **Cofres Congelados**, bloquear la apertura hasta que una **Baliza Térmica** (`HeatSourceNode` con `CanMeltFrozenChests = true`) permanezca activa dentro de su radio durante `15 segundos`.
4. Multiplicar por `2x` los recursos entregados cuando `DayNightCycleManager.IsChristmasEveClimax` esté activo.

---

## 2. Contrato de Clase

```csharp
namespace Boreatek.World
{
    public class HarvestableNode : MonoBehaviour, IInteractable
    {
        [Header("Node Configuration")]
        [SerializeField] private HarvestableNodeConfigSO _nodeConfig;
        [SerializeField] private Renderer _iceBlockOverlayRenderer;

        public bool IsFrozenLocked => _nodeConfig.RequiresHeatToMelt && _currentMeltProgress < 1f;
        public float MeltProgress => _currentMeltProgress;

        public event Action<float> OnMeltProgressChanged;
        public event Action<ResourceDataSO, int> OnNodeDepleted;

        public bool TryApplyPickaxeHit(int playerPickaxeTier, int damage, out int harvestedAmount) { /* ... */ }
        public void NotifyNearbyHeatTick(float deltaTime) { /* ... */ }
    }
}
```
