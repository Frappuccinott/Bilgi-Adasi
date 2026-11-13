using BilgiAdasi.Player;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Kazanın boşaltma (sıfırlama) kolu: E → kol öne çekilir, kazan boşalır, malzeme gider (hata sayılmaz);
    /// kol kısa bekleyip yaylanarak yerine döner. Dökülen geri alınamaz. Kazan boşken de çekilir (yalnızca kol oynar).
    /// Model: Art/Esya/Kazan_Bosaltma_Kolu.fbx (Kazan_Kolu_Cubuk pivotu pimde; üst objenin X ekseninde döner).
    /// </summary>
    public sealed class CauldronDrain : WorldInteractable
    {
        [Tooltip("Kazanın OrderController'ı (üst objede).")]
        [SerializeField] private OrderController controller;
        [Tooltip("Çekilince dönen kol parçası (pivotu menteşede). Boş bırakılabilir.")]
        [SerializeField] private Transform lever;
        [Tooltip("Çekilmiş açı (derece, oyuncuya doğru).")]
        [SerializeField] private float pullAngle = 55f;
        [SerializeField] private float pullTime = 0.22f;
        [SerializeField] private float holdTime = 0.35f;
        [SerializeField] private float returnTime = 0.6f;
        [Tooltip("Kazan boşalırken çalan ses (isteğe bağlı).")]
        [SerializeField] private AudioSource drainSound;

        private Quaternion rest;
        private float t = -1f;

        protected override void Awake()
        {
            base.Awake();
            if (controller == null) controller = GetComponentInParent<OrderController>();
            if (lever != null) rest = lever.localRotation;
        }

        public override bool CanInteract => base.CanInteract && controller != null && !controller.IsBusy && t < 0f;

        public override string GetPrompt(PlayerInteractor interactor) =>
            controller.Texts != null ? controller.Texts.DrainPrompt : string.Empty;

        public override void Interact(PlayerInteractor interactor)
        {
            t = 0f;
            if (controller.CanPour && !controller.Session.IsEmpty)
            {
                controller.Drain();
                if (drainSound != null) drainSound.Play();
            }
        }

        private void Update()
        {
            if (t < 0f || lever == null) return;
            t += Time.deltaTime;
            float a;
            if (t < pullTime)
            {
                float k = t / pullTime;
                a = pullAngle * (1f - (1f - k) * (1f - k));                      // hızlı başlar, sona doğru yavaşlar
            }
            else if (t < pullTime + holdTime)
            {
                a = pullAngle;
            }
            else if (t < pullTime + holdTime + returnTime)
            {
                float k = (t - pullTime - holdTime) / returnTime;
                // yay: hedefi biraz geçip (geri) sönümlenerek durur
                a = pullAngle * Mathf.Exp(-5f * k) * Mathf.Cos(k * Mathf.PI * 2.2f) * (1f - k);
            }
            else
            {
                a = 0f;
                t = -1f;
            }
            lever.localRotation = Quaternion.AngleAxis(a, Vector3.right) * rest;
        }
    }
}
