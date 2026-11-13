using UnityEngine;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// Başka bir etkileşimin collider'ı (ör. Mustafa'nın kapı kanadı) bakışı keserse, o collider'a bakınca hedef etkileşim çalışsın.
    /// Model prefab'ına konmaz; sahibi (ör. DoorStation) çalışırken ekler. Böylece model yeniden kurulsa da bozulmaz.
    /// </summary>
    public sealed class InteractableProxy : MonoBehaviour, IInteractable
    {
        private IInteractable target;

        public void SetTarget(IInteractable value) => target = value;

        public bool CanInteract => target != null && target.CanInteract;
        public string GetPrompt(PlayerInteractor interactor) => target != null ? target.GetPrompt(interactor) : string.Empty;
        public void SetHighlighted(bool highlighted) => target?.SetHighlighted(highlighted);
        public void Interact(PlayerInteractor interactor) => target?.Interact(interactor);
    }
}
