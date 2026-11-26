using UnityEngine;

namespace Boreatek.Core
{
    /// <summary>
    /// Contrato común para cualquier objeto del mundo 3D con el que el Duende Ingeniero
    /// pueda interactuar (Vetas minerales, Cofres congelados, Tolva central, Tienda, Autómatas).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Texto descriptivo que se muestra en el HUD al apuntar al objeto.
        /// </summary>
        string GetInteractionPrompt();

        /// <summary>
        /// Evalúa si el jugador cumple las condiciones actuales para interactuar.
        /// </summary>
        bool CanInteract(GameObject interactor);

        /// <summary>
        /// Ejecuta la interacción principal (Tecla E / Botón Interact).
        /// </summary>
        void Interact(GameObject interactor);
    }
}
