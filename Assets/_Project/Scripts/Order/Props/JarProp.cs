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

        public void SetLabel(string richText)
        {
            if (label != null) label.text = richText;
        }
    }
}
