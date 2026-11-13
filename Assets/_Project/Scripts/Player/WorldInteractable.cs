using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// İstasyonsuz (kamera değişmeyen) etkileşimler için taban: kazan, ocak, kol, raf yeri, pazar, kapı.
    /// Objede collider olmalı, layer Interactable. Bakılınca highlight objesi açılır.
    /// </summary>
    public abstract class WorldInteractable : MonoBehaviour, IInteractable
    {
        [Tooltip("Bakılınca açılan vurgu objesi (kontur vb.). Boş bırakılabilir.")]
        [SerializeField] private GameObject highlight;

        public virtual bool CanInteract => isActiveAndEnabled;

        /// <summary>Varsayılan E / X. Kapı ve bisiklet Use (F / X) döndürür.</summary>
        public virtual PromptAction TriggerAction => PromptAction.Interact;

        protected virtual void Awake()
        {
            if (highlight != null) highlight.SetActive(false);
        }

        public virtual void SetHighlighted(bool highlighted)
        {
            if (highlight != null) highlight.SetActive(highlighted);
        }

        public abstract string GetPrompt(PlayerInteractor interactor);
        public abstract void Interact(PlayerInteractor interactor);
    }
}
