using Boreatek.Core;
using Boreatek.Player;
using UnityEngine;

namespace Boreatek.Economy
{
    internal sealed class WorkshopMerchant : MonoBehaviour, IInteractable
    {
        internal const string StallName = "Puesto de Bruma";
        private const float TradeReach = 3.8f;
        private PlayerController _visitor;
        private ElfPose _pose;
        private Quaternion _rest;
        private bool _greeted;
        private float _idleTime;

        internal void Initialize(ElfPose pose)
        {
            _pose = pose;
            _rest = transform.rotation;
        }

        public string GetInteractionPrompt() => "[E] Comprar provisiones a Bruma";

        public bool CanInteract(GameObject interactor) =>
            isActiveAndEnabled && interactor != null && interactor.TryGetComponent<PlayerController>(out _) &&
            (interactor.transform.position - transform.position).sqrMagnitude <= TradeReach * TradeReach;

        public void Interact(GameObject interactor)
        {
            if (CanInteract(interactor) && interactor.TryGetComponent(out PlayerController player))
                player.OpenShopMenu(this);
        }

        private void Update()
        {
            if (_pose == null || Time.deltaTime <= 0f) return;
            _visitor ??= FindAnyObjectByType<PlayerController>();
            _idleTime += Time.deltaTime;
            _pose.Update(Time.deltaTime);
            if (_visitor == null) return;

            Vector3 view = _visitor.transform.position - transform.position;
            bool nearby = view.sqrMagnitude < 5f * 5f;
            Vector3 flat = Vector3.ProjectOnPlane(view, Vector3.up);
            Quaternion facing = _rest;
            if (nearby && flat.sqrMagnitude > 0.01f)
            {
                Quaternion watch = Quaternion.LookRotation(flat);
                facing = Quaternion.RotateTowards(_rest, watch, 76f);
                if (!_greeted)
                    _pose.BeginGesture(ElfPose.Gesture.Reach, transform.InverseTransformDirection(flat.normalized));
            }
            _greeted = nearby;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, Time.deltaTime * 108f);
            _pose.Neck.localRotation = Quaternion.Euler(Mathf.Sin(_idleTime * 1.7f) * 3.6f,
                nearby ? 0f : Mathf.Sin(_idleTime * 0.77f) * 11f, 0f);
        }
    }
}
