using BilgiAdasi.Core;

namespace BilgiAdasi.Player
{
    /// <summary>Oyuncunun bakıp Interact ile kullanabildiği her şey (kazan, raf, pazar, kapı, istasyonlar).</summary>
    public interface IInteractable
    {
        bool CanInteract { get; }
        /// <summary>
        /// Hangi tuşla kullanılır: Interact (E / X) ya da Use (F / X — kapı, bisiklet). İpucundaki simge de buna göre.
        /// </summary>
        PromptAction TriggerAction { get; }
        /// <summary>
        /// Ekranda tuş simgesinin yanında görünen metin ("Kazana dök: O₂"). Duruma göre değişebilir; her karede
        /// sorulur, bu yüzden uygulayan metni önbellekte tutmalı (durum değişmedikçe aynı string'i döndürmeli).
        /// </summary>
        string GetPrompt(PlayerInteractor interactor);
        void SetHighlighted(bool highlighted);
        void Interact(PlayerInteractor interactor);
    }
}
