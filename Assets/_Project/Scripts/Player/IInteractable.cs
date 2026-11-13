namespace BilgiAdasi.Player
{
    /// <summary>Oyuncunun bakıp Interact ile kullanabildiği her şey (istasyonlar).</summary>
    public interface IInteractable
    {
        bool CanInteract { get; }
        /// <summary>Ekranda tuş simgesinin yanında görünen metin ("Kazana geç"). Inspector'dan gelir.</summary>
        string PromptText { get; }
        void SetHighlighted(bool highlighted);
        void Interact(PlayerInteractor interactor);
    }
}
