# Módulo M-05: Ciclo Día/Noche, Clima y Gran Nochebuena (`DayNightCycleManager.cs`)

* **Namespace:** `Boreatek.Core`
* **Ruta destino en Unity:** `Assets/_Project/Scripts/Core/DayNightCycleManager.cs`

---

## 1. Responsabilidades del Módulo

1. Ejecutar el reloj continuo de la campaña de **20 horas** (`60 ciclos` de **20 minutos en tiempo real** cada uno: `10 minutos` de luz ártica tenue y `10 minutos` de noche polar).
2. Rotar la `Directional Light` principal y ajustar su intensidad y temperatura de color entre el día polar y la noche de auroras boreales.
3. Activar y desactivar estados dinámicos del evento climático **Ventisca Ciega**, interpolando la densidad de niebla de URP (`RenderSettings.fogDensity`) y notificando a `TemperatureSystem` el nuevo multiplicador de enfriamiento (`2.5x`).
4. Disparar el evento final **La Gran Nochebuena** al cumplirse las 20 horas o entregarse los **Núcleos Luminosos**, activando el aterrizaje del Trineo Blindado y la sobrecarga `2x` de todos los nodos de recursos.

---

## 2. Contrato de Clase

```csharp
namespace Boreatek.Core
{
    public enum WeatherState { ClearArctic, PolarNight, BlindBlizzard, ChristmasEveOvercharge }

    public class DayNightCycleManager : MonoBehaviour
    {
        public const float SECONDS_PER_CYCLE = 1200f; // 20 minutos reales
        public const int TOTAL_CAMPAIGN_CYCLES = 60;  // 20 horas reales

        [Header("Lighting & Fog References")]
        [SerializeField] private Light _directionalLight;
        [SerializeField] private float _clearFogDensity = 0.008f;
        [SerializeField] private float _blizzardFogDensity = 0.065f;

        public int CurrentCycleIndex => _currentCycleIndex;
        public bool IsNightPhase => _normalizedCycleTime >= 0.5f;
        public WeatherState CurrentWeather => _currentWeather;
        public bool IsChristmasEveClimax => _isChristmasEveClimax;

        public event Action<int, bool> OnCyclePhaseChanged;
        public event Action<WeatherState, float> OnWeatherStateChanged;
        public event Action OnChristmasEveClimaxStarted;

        [ContextMenu("Debug: Trigger Blind Blizzard")]
        public void ForceTriggerBlindBlizzard() { /* ... */ }

        [ContextMenu("Debug: Start Christmas Eve Climax")]
        public void StartChristmasEveClimax() { /* ... */ }
    }
}
```
