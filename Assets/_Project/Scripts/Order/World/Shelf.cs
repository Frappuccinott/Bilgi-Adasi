using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using BilgiAdasi.UI;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Atölye rafı: çocuklarındaki ShelfSlot'ları yönetir (her biri bir yığın). Raflar oyuncuya aittir:
    /// pazardan alınan maddeler buraya elle konur. Kavanoz modeli SpeciesData.jarPrefab, yoksa defaultJarPrefab.
    /// </summary>
    public sealed class Shelf : MonoBehaviour
    {
        [Tooltip("Kayıt anahtarı; sahnede birden fazla raf varsa hepsine farklı ad verin.")]
        [SerializeField] private string shelfId = "raf1";
        [SerializeField] private EconomyData economy;
        [SerializeField] private OrderTexts texts;
        [Tooltip("SpeciesData.jarPrefab boşsa kullanılan kavanoz (kökünde JarProp).")]
        [SerializeField] private JarProp defaultJarPrefab;
        [Tooltip("Etiketlerin döneceği yön: raf önünden bakan göz (boşsa rafın -Z yönüne bakar).")]
        [SerializeField] private Transform labelFacing;

        public EconomyData Economy => economy;
        public OrderTexts Texts => texts;

        private void Awake()
        {
            var slots = GetComponentsInChildren<ShelfSlot>(true);
            for (int i = 0; i < slots.Length; i++) slots[i].Init(this, shelfId + "/" + i);
        }

        public JarProp SpawnJar(SpeciesData species, Transform parent)
        {
            JarProp prefab = null;
            if (species.JarPrefab != null) prefab = species.JarPrefab.GetComponent<JarProp>();
            // Model JarProp'suz bir sanat prefab'ıysa (Malzeme_*/Alet_*): yer tutucu kavanoz etiket için açılır,
            // gövdesi modelle değiştirilir.
            bool plainModel = prefab == null && species.JarPrefab != null;
            if (prefab == null) prefab = defaultJarPrefab;
            if (prefab == null) return null;

            var jar = Instantiate(prefab, parent);
            jar.transform.localPosition = Vector3.zero;
            jar.transform.localRotation = Quaternion.identity;
            jar.name = "Jar_" + species.Formula;
            if (plainModel) jar.AttachModel(species.JarPrefab);
            jar.Setup(species);
            return jar;
        }

        public void FaceLabel(JarProp jar)
        {
            var label = jar.Label;
            if (label == null) return;
            Vector3 dir = labelFacing != null ? label.position - labelFacing.position : transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) label.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        public void Notice(string text)
        {
            if (!string.IsNullOrEmpty(text)) ViewLocator.Get<INoticeView>()?.ShowNotice(text);
        }
    }
}
