using System;
using BilgiAdasi.Player;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Atölye kapısı (karar 13.11: istasyon kamerası yok). E → sakinle konuşma: sipariş alma ya da elle teslim.
    /// Akış NeighborhoodRunner'da (Used olayını dinler). Bu bileşen yalnızca bağlantı noktasıdır.
    /// </summary>
    public sealed class DoorStation : WorldInteractable
    {
        [Tooltip("Ekranda tuş simgesinin yanında görünen metin.")]
        [SerializeField] private string promptText = "Kapıya bak";

        [Header("Kapı kanadı (Mustafa'nın Kapi_Rig'i; boşsa animasyon yok)")]
        [Tooltip("Kapi_Rig kökündeki Animator.")]
        [SerializeField] private Animator doorAnimator;
        [SerializeField] private string openClip = "Kapi_Ac";
        [SerializeField] private string closeClip = "Kapi_Kapat";
        [Tooltip("Klipler arası geçiş (sn). 0 = anında.")]
        [SerializeField] private float crossFade = 0.1f;

        private int openHash, closeHash;
        private bool isOpen;

        /// <summary>Oyuncu kapıyla etkileşti. Runner konuşmayı başlatır.</summary>
        public event Action<PlayerInteractor> Used;

        protected override void Awake()
        {
            base.Awake();
            openHash = Animator.StringToHash(openClip);
            closeHash = Animator.StringToHash(closeClip);

            // Kanadın collider'ı (Kapi_Kanat) bakışı kesmesin: ona bakınca da bu kapı çalışır.
            if (doorAnimator != null && !doorAnimator.transform.IsChildOf(transform))
            {
                foreach (var col in doorAnimator.GetComponentsInChildren<Collider>(true))
                {
                    var proxy = col.GetComponent<InteractableProxy>();
                    if (proxy == null) proxy = col.gameObject.AddComponent<InteractableProxy>();
                    proxy.SetTarget(this);
                }
            }
        }

        public override string GetPrompt(PlayerInteractor interactor) => promptText;

        public override void Interact(PlayerInteractor interactor)
        {
            OpenDoor();
            Used?.Invoke(interactor);
        }

        /// <summary>Kanadı açar (zaten açıksa bir şey yapmaz).</summary>
        public void OpenDoor() => PlayDoor(true);

        /// <summary>Kanadı kapatır. Runner konuşma bitince çağırır.</summary>
        public void CloseDoor() => PlayDoor(false);

        private void PlayDoor(bool open)
        {
            if (doorAnimator == null || isOpen == open) return;
            isOpen = open;
            int hash = open ? openHash : closeHash;
            if (crossFade > 0f) doorAnimator.CrossFadeInFixedTime(hash, crossFade);
            else doorAnimator.Play(hash);
        }
    }
}
