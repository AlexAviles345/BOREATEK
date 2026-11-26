using Boreatek.Core;
using Boreatek.Economy;
using UnityEngine;

namespace Boreatek.World
{
    internal sealed class AviationFuelPump : MonoBehaviour, IInteractable
    {
        public string GetInteractionPrompt() =>
            $"[E] Bidón de gasolina polar · {BoreatekBushPlane.CanLiters} L · {BoreatekBushPlane.CanPrice} monedas";

        public bool CanInteract(GameObject interactor) =>
            interactor != null && interactor.TryGetComponent<InventoryAndShop>(out _) &&
            (interactor.transform.position - transform.position).sqrMagnitude <= 3.8f * 3.8f;

        public void Interact(GameObject interactor)
        {
            if (CanInteract(interactor)) interactor.GetComponent<InventoryAndShop>().BuyAviationCanister();
        }
    }
}
