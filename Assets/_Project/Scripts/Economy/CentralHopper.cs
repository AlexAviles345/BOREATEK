using Boreatek.Core;
using Boreatek.Player;
using UnityEngine;

namespace Boreatek.Economy
{
    /// <summary>
    /// Representa la Tolva Central y la Terminal del Taller en la Aldea Industrial de Santa.
    /// Al interactuar con 'E', deposita los minerales recolectados a cambio de Monedas de Jengibre
    /// o abre la Tienda del Taller.
    /// </summary>
    public class CentralHopper : MonoBehaviour, IInteractable
    {
        [Header("Hopper Configuration")]
        [Tooltip("Si es verdadero, actúa como la Terminal de la Tienda del Taller además de la Tolva.")]
        [SerializeField] private bool _opensWorkshopShopTerminal;

        public void ConfigureRuntime(bool isShopTerminal)
        {
            _opensWorkshopShopTerminal = isShopTerminal;
        }

        public string GetInteractionPrompt()
        {
            return _opensWorkshopShopTerminal
                ? "🛒 [E] Abrir Tienda del Taller (Comida, Herramientas, Mejoras y Autómatas) | [Tab]"
                : "📥 [E] Depositar Minerales en la Tolva Central (Convertir a Monedas de Jengibre)";
        }

        public bool CanInteract(GameObject interactor)
        {
            return interactor != null;
        }

        public void Interact(GameObject interactor)
        {
            if (interactor == null)
            {
                return;
            }

            if (_opensWorkshopShopTerminal)
            {
                if (interactor.TryGetComponent(out PlayerController player))
                {
                    player.ToggleShopMenu();
                }
                return;
            }

            if (interactor.TryGetComponent(out InventoryAndShop inventory))
            {
                inventory.DepositAllResourcesIntoCentralHopper();
            }
        }
    }
}
