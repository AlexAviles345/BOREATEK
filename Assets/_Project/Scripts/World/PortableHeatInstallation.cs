using Boreatek.Core;
using Boreatek.Economy;
using UnityEngine;

namespace Boreatek.World
{
    internal sealed class PortableHeatInstallation : MonoBehaviour, IInteractable
    {
        private InventoryAndShop _owner;
        private string _id;
        private string _prompt;

        internal void Initialize(InventoryAndShop owner, string id, bool beacon)
        {
            _owner = owner;
            _id = id;
            _prompt = beacon ? "[E] Recoger baliza térmica" : "[E] Recoger guirnalda";
        }

        public string GetInteractionPrompt() => _prompt;

        public bool CanInteract(GameObject interactor) =>
            isActiveAndEnabled && _owner != null && interactor == _owner.gameObject &&
            (interactor.transform.position - transform.position).sqrMagnitude <= 3.4f * 3.4f;

        public void Interact(GameObject interactor)
        {
            if (CanInteract(interactor)) _owner.RecoverHeatTool(_id, gameObject);
        }
    }
}
