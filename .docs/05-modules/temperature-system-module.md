# Módulo M-02: Sistema de Temperatura y Burbujas de Calor (`TemperatureSystem.cs`)

* **Namespace:** `Boreatek.Player` y `Boreatek.World`
* **Ruta destino en Unity:** `Assets/_Project/Scripts/Player/TemperatureSystem.cs`

---

## 1. Responsabilidades del Módulo

1. Mantener el estado de calor corporal del jugador (`_currentTemperature` entre `0f` y `100f`).
2. Mantener un registro (`HashSet<HeatSourceNode>`) de todas las burbujas térmicas activas en las que se encuentra el jugador (perímetro de la **Aldea Industrial de Santa**, **Guirnaldas Luminosas** o **Balizas Térmicas**).
3. Aplicar la fórmula de drenaje térmico cuando el jugador está expuesto al frío exterior o subterráneo, escalada por el evento climático **Ventisca Ciega** y mitigada por el nivel del **Abrigo Térmico Acolchado**.
4. Actualizar los manómetros diegéticos del **Tanque-Mochila Presurizado de Latón** y disparar el rescate por hipotermia cuando el calor permanece en `0 °T` durante `5 segundos`.

---

## 2. Contrato de Propiedades y Eventos

```csharp
namespace Boreatek.Player
{
    public class TemperatureSystem : MonoBehaviour
    {
        [Header("Thermal Parameters")]
        [SerializeField] private float _maxTemperature = 100f;
        [SerializeField] private float _hypothermiaGraceSeconds = 5f;
        [SerializeField] private float _coatThermalResistance = 0f; // 0.0 (Nv1), 0.25 (Nv2), 0.50 (Nv3)

        public float CurrentTemperature => _currentTemperature;
        public float NormalizedTemperature => _currentTemperature / _maxTemperature;
        public bool IsInWarmZone => _activeHeatSources.Count > 0;
        public bool HasHotChocolateBuff => _hotChocolateTimer > 0f;

        public event Action<float, float> OnTemperatureChanged;
        public event Action<bool> OnWarmZoneStatusChanged;
        public event Action OnHypothermiaRescueTriggered;

        public void ConsumeHotChocolate(float immunityDurationSeconds = 60f) { /* ... */ }
        public void UpgradeThermalCoat(float newResistancePercent) { /* ... */ }
    }
}
```
