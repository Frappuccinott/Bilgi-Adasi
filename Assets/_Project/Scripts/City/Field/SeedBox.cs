using BilgiAdasi.Player;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Tarladaki ekim sandığı (Tohum Nefes Alır). Bakıp Interact (E / X): eldeki tohum keselerinin hepsi sandığa dökülür.
    /// Dökülen geri alınmaz (kazan kuralı). Sandıktaki tohum yığını dolulukla yükselir (görünüm).
    /// Mantık FieldTaskDirector'da; sandık yalnızca haber verir.
    /// </summary>
    public sealed class SeedBox : WorldInteractable
    {
        [SerializeField] private FieldTaskDirector director;
        [Tooltip("Sandığın içindeki tohum yığını (yukarı doğru ölçeklenir). Boş bırakılabilir.")]
        [SerializeField] private Transform seedHeap;
        [SerializeField] private float fillSharpness = 4f;

        private float fill;      // 0..1 (hedef)
        private float shownFill; // görünen
        private Vector3 heapRest = Vector3.one;

        public override bool CanInteract => base.CanInteract && director != null && director.CanPour;

        protected override void Awake()
        {
            base.Awake();
            if (seedHeap != null)
            {
                heapRest = seedHeap.localScale;
                ApplyHeap(0f);
            }
        }

        public override string GetPrompt(PlayerInteractor interactor) => director != null ? director.PourPrompt : string.Empty;

        public override void Interact(PlayerInteractor interactor) => director?.Pour(interactor);

        /// <summary>Doluluk (0..1): dökülen kese / gereken kese.</summary>
        public void SetFill(float value, bool instant)
        {
            fill = Mathf.Clamp01(value);
            if (instant) { shownFill = fill; ApplyHeap(fill); }
        }

        private void Update()
        {
            if (seedHeap == null || Mathf.Approximately(shownFill, fill)) return;
            shownFill = Mathf.MoveTowards(shownFill, fill, Time.deltaTime * fillSharpness * 0.25f);
            ApplyHeap(shownFill);
        }

        private void ApplyHeap(float k)
        {
            seedHeap.gameObject.SetActive(k > 0.001f);
            seedHeap.localScale = new Vector3(heapRest.x, heapRest.y * Mathf.Max(0.05f, k), heapRest.z);
        }
    }
}
