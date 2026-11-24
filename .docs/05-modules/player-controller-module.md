# Módulo M-01: Controlador del Jugador y Faro Frontal (`PlayerController.cs`)

* **Namespace:** `Boreatek.Player`
* **Ruta destino en Unity:** `Assets/_Project/Scripts/Player/PlayerController.cs`
* **Componentes Requeridos:** `[RequireComponent(typeof(CharacterController))]`

---

## 1. Responsabilidades del Módulo

1. Leer los vectores `Move`, `Look`, `Jump`, `Sprint`, `Attack` e `Interact` desde `InputSystem_Actions.inputactions`.
2. Desplazar al **Duende Ingeniero** sobre terreno nevado aplicando multiplicadores de velocidad (bonificación de *Galleta de Jengibre* o penalización de nieve profunda en el *Cementerio de Trineos* si no tiene *Botas Mejoradas*).
3. Gestionar el encendido y orientación del **Casco Minero con Faro Frontal LED** (`Spot Light`).
4. Ejecutar el golpe del **Pico Industrial con Cristal Energético Rojo** contra objetos que implementen `HarvestableNode` o enemigos.
5. Lanzar un `Physics.Raycast` frontal para interactuar con `IInteractable` (Tolva Central, Tienda, Cofres Congelados) y desplegar **Guirnaldas Luminosas** o **Balizas Térmicas**.

---

## 2. Contrato de Propiedades y Eventos

```csharp
namespace Boreatek.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _walkSpeed = 5.5f;
        [SerializeField] private float _sprintSpeed = 8.0f;
        [SerializeField] private float _deepSnowMultiplier = 0.75f;
        [SerializeField] private float _jumpHeight = 1.4f;
        [SerializeField] private float _gravity = -19.62f;

        [Header("Equipment & Interaction")]
        [SerializeField] private int _currentPickaxeTier = 1;
        [SerializeField] private int _pickaxeDamage = 1;
        [SerializeField] private float _interactionDistance = 3.5f;
        [SerializeField] private LayerMask _interactableMask;
        [SerializeField] private Light _helmetHeadlampLight;

        public int CurrentPickaxeTier => _currentPickaxeTier;
        public bool IsHeadlampOn => _helmetHeadlampLight != null && _helmetHeadlampLight.enabled;

        public event Action<int> OnPickaxeTierUpgraded;
        public event Action<IInteractable> OnHoverInteractableChanged;

        public void UpgradePickaxe(int newTier, int newDamage) { /* ... */ }
        public void ApplySpeedBuff(float multiplier, float durationSeconds) { /* ... */ }
    }
}
```
