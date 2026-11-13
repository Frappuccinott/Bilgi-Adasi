using BilgiAdasi.Chemistry;
using TMPro;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// 3D kavanoz görseli (raf yerinde durur). Modeli değiştirmek için kökünde bu bileşen olan bir prefab yapılıp
    /// SpeciesData.jarPrefab'a verilir. Renk jarColor'dan (tintRenderers boyanır), etiket formül + adet.
    /// Etkileşim kavanozda değil raf yerindedir (ShelfSlot); kavanozdaki collider'lar trigger olmalı.
    /// </summary>
    public sealed class JarProp : MonoBehaviour
    {
        [Tooltip("Formül + adet. Dünya uzayı TextMeshPro.")]
        [SerializeField] private TMP_Text label;
        [Tooltip("jarColor ile boyanacak renderer'lar (cam/sıvı). Boşsa boyanmaz (model kendi rengini taşır).")]
        [SerializeField] private Renderer[] tintRenderers = new Renderer[0];

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock block;

        public SpeciesData Species { get; private set; }
        public Transform Label => label != null ? label.transform : null;

        public void Setup(SpeciesData species)
        {
            Species = species;
            if (tintRenderers.Length == 0 || species == null) return;
            block ??= new MaterialPropertyBlock();
            for (int i = 0; i < tintRenderers.Length; i++)
            {
                var r = tintRenderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, species.JarColor);
                block.SetColor(ColorId, species.JarColor);
                r.SetPropertyBlock(block);
            }
        }

        /// <summary>
        /// JarProp'u olmayan bir modeli (Art/Prefablar'daki Malzeme_*/Alet_* gibi) bu kavanozun yerine koyar:
        /// yer tutucu gövde gizlenir, model kökün altına (pivot tabanda) yerleşir, etiket modelin üstüne çıkar.
        /// Model kendi rengini taşır; tintRenderers boyaması yalnızca yer tutucuya uygulanır.
        /// </summary>
        public void AttachModel(GameObject modelPrefab)
        {
            if (modelPrefab == null) return;

            var labelRenderer = label != null ? label.GetComponent<Renderer>() : null;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (r != labelRenderer) r.enabled = false;
            tintRenderers = new Renderer[0];

            var model = Instantiate(modelPrefab, transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            // Etkileşim raf yerinde (ShelfSlot): modelin collider'ları ışını kesmesin.
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) c.isTrigger = true;

            if (label == null) return;
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float top = transform.InverseTransformPoint(bounds.max).y + 0.06f;
            var lp = label.transform.localPosition;
            if (top > lp.y) label.transform.localPosition = new Vector3(lp.x, top, lp.z);
        }

        public void SetLabel(string richText)
        {
            if (label != null) label.text = richText;
        }
    }
}
