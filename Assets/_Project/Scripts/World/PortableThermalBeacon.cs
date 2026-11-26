using Boreatek.Core;
using Boreatek.Economy;
using UnityEngine;

namespace Boreatek.World
{
    internal sealed class PortableThermalBeacon : MonoBehaviour, IInteractable
    {
        private InventoryAndShop _owner;
        private string _id;

        internal void Initialize(InventoryAndShop owner, string id)
        {
            _owner = owner;
            _id = id;
        }

        public string GetInteractionPrompt() => "[E] Recoger baliza térmica";

        public bool CanInteract(GameObject interactor) =>
            _owner != null && interactor == _owner.gameObject &&
            (interactor.transform.position - transform.position).sqrMagnitude <= 3.4f * 3.4f;

        public void Interact(GameObject interactor)
        {
            if (CanInteract(interactor)) _owner.RecoverBeacon(_id, gameObject);
        }
    }
}
