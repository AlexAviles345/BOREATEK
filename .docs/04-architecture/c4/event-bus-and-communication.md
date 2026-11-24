# Arquitectura de Eventos y Comunicación Desacoplada (Nivel C3)

---

## 1. Regla de Desacoplamiento entre Módulos

En **BOREATEK**, la interfaz gráfica (`UI`), el sistema de iluminación (`URP`) y el inventario **nunca son llamados directamente** por la lógica de física o de tiempo en cada frame. En su lugar, la comunicación sigue el patrón **Observer** mediante eventos nativos de C# (`event Action<T>`) e interfaces de contrato (`IInteractable`).

---

## 2. Mapa del Bus de Eventos del Juego

```mermaid
flowchart LR
    subgraph Publishers["Emisores de Eventos (Publishers)"]
        P_TS["TemperatureSystem"]
        P_DNC["DayNightCycleManager"]
        P_IS["InventoryAndShop"]
        P_HN["HarvestableNode"]
    end

    subgraph Events["Contratos de Eventos C# (Action<T>)"]
        E1(("OnTemperatureChanged\n(float current, float max)"))
        E2(("OnHypothermiaRescue\n()"))
        E3(("OnWeatherChanged\n(WeatherState state)"))
        E4(("OnChristmasEveStarted\n()"))
        E5(("OnInventoryUpdated\n(int usedSlots, int maxSlots)"))
        E6(("OnCoinsChanged\n(int totalCoins)"))
    end

    subgraph Subscribers["Suscriptores (Subscribers)"]
        S_HUD["ThermalHUDView &\nManómetros Mochila"]
        S_VOL["URP Frost Vignette\n& Fog Controller"]
        S_INV["InventoryAndShop\n(Pérdida 30% en rescate)"]
        S_NODE["HarvestableNode\n(Sobrecarga 2x)"]
    end

    P_TS --> E1 --> S_HUD
    E1 --> S_VOL
    P_TS --> E2 --> S_INV
    P_DNC --> E3 --> P_TS
    E3 --> S_VOL
    P_DNC --> E4 --> S_NODE
    P_IS --> E5 --> S_HUD
    P_IS --> E6 --> S_HUD
```

---

## 3. Contrato de Interacción Polimórfica (`IInteractable`)

Para evitar que `PlayerController.cs` tenga que conocer clases concretas como `CentralHopper`, `FrozenChest`, `ShopTerminal` o `HeatSourceNode`, todos implementan:

```csharp
namespace Boreatek.Core
{
    public interface IInteractable
    {
        string GetInteractionPrompt();
        bool CanInteract(int playerToolTier);
        void Interact(GameObject interactor);
    }
}
```
