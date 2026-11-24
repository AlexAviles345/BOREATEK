# Estándares y Convenciones de Codificación en C#

---

## 1. Tabla de Nomenclatura Obligatoria

| Elemento | Estilo | Prefijo / Regla | Ejemplo en BOREATEK |
| :--- | :---: | :---: | :--- |
| **Namespace** | `PascalCase` | `Boreatek.<Modulo>` | `namespace Boreatek.Player` |
| **Clase / Struct / Enum** | `PascalCase` |Sustantivo descriptivo | `public class TemperatureSystem : MonoBehaviour` |
| **Interfaz** | `PascalCase` | Prefijo `I` | `public interface IInteractable` |
| **ScriptableObject (Clase)** | `PascalCase` | Sufijo `SO` | `public class ResourceDataSO : ScriptableObject` |
| **Campo Privado / Serializado** | `_camelCase` | Guion bajo `_` + `[SerializeField]` | `[SerializeField] private float _maxHeat = 100f;` |
| **Propiedad Pública** | `PascalCase` | Expression-bodied si es solo lectura | `public float CurrentHeat => _currentHeat;` |
| **Método Público / Privado** | `PascalCase` | Verbo en imperativo | `public void ApplyThermalDamage(float amount)` |
| **Evento C#** | `PascalCase` | Prefijo `On` + participio | `public event Action<float> OnHeatChanged;` |
| **Constante** | `UPPER_SNAKE_CASE` | `const` | `private const float CYCLE_SECONDS = 1200f;` |

---

## 2. Reglas de Encapsulamiento e Inspector

1. **Cero variables `public` en `MonoBehaviour`:**
   Nunca uses `public float speed;` solo para verlo en el Inspector de Unity. Usa siempre:
   ```csharp
   [Header("Locomotion Settings")]
   [Tooltip("Velocidad base del Duende Ingeniero sobre nieve normal (m/s).")]
   [SerializeField] private float _baseMoveSpeed = 5.5f;
   ```
2. **Validación de Referencias en `Awake()` o `OnValidate()`:**
   Usa `[RequireComponent(typeof(...))]` cuando un script dependa obligatoriamente de otro componente en el mismo `GameObject` (ej. `CharacterController`, `SphereCollider` o `NavMeshAgent`).
3. **Suscripción Segura a Eventos:**
   Toda suscripción realizada en `OnEnable()` (`_cycleManager.OnBlizzardChanged += HandleBlizzard;`) debe tener su desuscripción simétrica en `OnDisable()` (`_cycleManager.OnBlizzardChanged -= HandleBlizzard;`).
