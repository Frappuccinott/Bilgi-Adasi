using System;
using BilgiAdasi.Core;
using BilgiAdasi.Player;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Atölye kapısı (karar 13.11: istasyon kamerası yok). F (gamepad X) kanadı açar/kapatır; kendiliğinden kapanmaz.
    /// Kapı açıkken oyuncu eşikten geçince (iki yönde de) Used yayınlanır → NeighborhoodRunner sakinle konuşmayı başlatır.
    /// Eşik düzlemi bu objenin konumu, geçiş ekseni yerel Z'dir (kapı kasasına dik).
    /// </summary>
    public sealed class DoorStation : WorldInteractable
    {
        [Tooltip("Kapı kapalıyken tuş simgesinin yanında görünen metin.")]
        [SerializeField] private string promptText = "Kapıyı aç";
        [Tooltip("Kapı açıkken tuş simgesinin yanında görünen metin.")]
        [SerializeField] private string closePromptText = "Kapıyı kapat";

        [Header("Kapı kanadı (Mustafa'nın Kapi_Rig'i; boşsa animasyon yok)")]
        [Tooltip("Kapi_Rig kökündeki Animator.")]
        [SerializeField] private Animator doorAnimator;
        [SerializeField] private string openClip = "Kapi_Ac";
        [SerializeField] private string closeClip = "Kapi_Kapat";
        [Tooltip("Klipler arası geçiş (sn). 0 = anında.")]
        [SerializeField] private float crossFade = 0.1f;

        [Header("Eşikten geçiş")]
        [Tooltip("Kapı açıkken yolu kapatan collider (bu objedeki kutu). Açılınca kapanır, kapanınca geri gelir. Boşsa bu objede aranır.")]
        [SerializeField] private Collider blocker;
        [Tooltip("Eşiğin yarı genişliği (m, yerel X). Bunun dışından dolaşan geçiş sayılmaz.")]
        [SerializeField] private float passHalfWidth = 0.9f;
        [Tooltip("Eşiğin iki yanındaki ölü bant (m). Oyuncu eşikte durup kıpırdayınca tekrar tetiklenmesin.")]
        [SerializeField] private float passBand = 0.3f;

        private int openHash, closeHash;
        private bool isOpen;
        private Transform player;
        private PlayerInteractor lastUser;
        private int lastSide;

        /// <summary>Oyuncu açık kapının eşiğinden geçti. Runner konuşmayı başlatır.</summary>
        public event Action<PlayerInteractor> Used;

        public bool IsOpen => isOpen;

        protected override void Awake()
        {
            base.Awake();
            openHash = Animator.StringToHash(openClip);
            closeHash = Animator.StringToHash(closeClip);
            if (blocker == null) blocker = GetComponent<Collider>();

            // Kanadın collider'ı (Kapi_Kanat) bakışı kesmesin: ona bakınca da bu kapı çalışır (açıkken kapatmak için de).
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

        /// <summary>Kapılar F ile açılır/kapanır (gamepad X).</summary>
        public override PromptAction TriggerAction => PromptAction.Use;

        public override string GetPrompt(PlayerInteractor interactor) => isOpen ? closePromptText : promptText;

        public override void Interact(PlayerInteractor interactor)
        {
            lastUser = interactor;
            player = interactor != null ? interactor.transform : null;
            if (isOpen) CloseDoor();
            else OpenDoor();
        }

        /// <summary>Kanadı açar (zaten açıksa bir şey yapmaz).</summary>
        public void OpenDoor()
        {
            if (isOpen) return;
            SetOpen(true);
            GameEvents.RaiseSoundAt(SoundId.DoorOpen, transform.position);
            lastSide = player != null ? SideOf(player.position) : 0;
        }

        /// <summary>Kanadı kapatır (zaten kapalıysa bir şey yapmaz).</summary>
        public void CloseDoor()
        {
            if (!isOpen) return;
            SetOpen(false);
            GameEvents.RaiseSoundAt(SoundId.DoorClose, transform.position);
        }

        private void SetOpen(bool open)
        {
            isOpen = open;
            if (blocker != null) blocker.enabled = !open;
            if (doorAnimator == null) return;
            int hash = open ? openHash : closeHash;
            if (crossFade > 0f) doorAnimator.CrossFadeInFixedTime(hash, crossFade);
            else doorAnimator.Play(hash);
        }

        private void Update()
        {
            if (!isOpen || player == null) return;
            Vector3 local = transform.InverseTransformPoint(player.position);
            int side = local.z > passBand ? 1 : local.z < -passBand ? -1 : 0;
            if (side == 0) return;
            if (lastSide != 0 && side != lastSide && Mathf.Abs(local.x) <= passHalfWidth)
            {
                lastSide = side;
                Used?.Invoke(lastUser);
                return;
            }
            lastSide = side;
        }

        private int SideOf(Vector3 worldPos)
        {
            float z = transform.InverseTransformPoint(worldPos).z;
            return z > 0f ? 1 : -1;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.35f);
            Gizmos.DrawCube(new Vector3(0f, 1f, 0f), new Vector3(passHalfWidth * 2f, 2f, passBand * 2f));
        }
#endif
    }
}
