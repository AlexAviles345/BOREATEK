using Boreatek.Core;
using Boreatek.Economy;
using Boreatek.Audio;
using Boreatek.Player;
using UnityEngine;

namespace Boreatek.World
{
    internal sealed class AviationFuelPump : MonoBehaviour, IInteractable
    {
        private BoreatekBushPlane _plane;
        internal void BindPlane(BoreatekBushPlane plane) => _plane = plane;

        public string GetInteractionPrompt()
        {
            if (_plane != null && _plane.NeedsServiceAt(transform.position))
                return _plane.IsWrecked ? "[E] Solicitar avioneta de reemplazo" : "[E] Reparar avioneta estacionada";
            return $"[E] Bidón de gasolina polar · {BoreatekBushPlane.CanLiters} L · {BoreatekBushPlane.CanPrice} monedas";
        }

        public bool CanInteract(GameObject interactor) =>
            interactor != null && interactor.TryGetComponent<InventoryAndShop>(out _) &&
            (interactor.transform.position - transform.position).sqrMagnitude <= 3.8f * 3.8f;

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;
            if (_plane != null && _plane.NeedsServiceAt(transform.position))
            {
                bool replacement = _plane.IsWrecked;
                _plane.Service();
                BoreatekSoundscape.Play(SoundCue.Confirm);
                if (interactor.TryGetComponent(out PlayerController player))
                    player.PublishStatus(replacement ? "Hay otra avioneta lista en la pista." : "Avioneta reparada.");
                return;
            }
            interactor.GetComponent<InventoryAndShop>().BuyAviationCanister();
        }
    }
}
