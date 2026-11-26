using Boreatek.Core;
using Boreatek.Audio;
using UnityEngine;

namespace Boreatek.Economy
{
    /// <summary>
    /// Canjea la carga de minerales por monedas de jengibre en la aldea.
    /// </summary>
    public class CentralHopper : MonoBehaviour, IInteractable
    {
        public string GetInteractionPrompt() => "[E] Entregar minerales por monedas de jengibre";

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

            if (interactor.TryGetComponent(out InventoryAndShop inventory))
            {
                int deposited = inventory.DepositAllResourcesIntoCentralHopper();
                BoreatekSoundscape.Play(deposited > 0 ? SoundCue.Confirm : SoundCue.Blocked, transform.position);
            }
        }
    }
}
