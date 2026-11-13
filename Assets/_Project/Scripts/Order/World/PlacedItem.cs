using System.Text;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using BilgiAdasi.Player;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Yere ya da masaya bırakılmış bir yığın (alet ya da kavanoz). E / X: yığının tamamı envantere girer.
    /// ItemPlacer kurar ve kaydeder (SaveData.PlacedItems); collider modelin sınırına göre bu objede kurulur.
    /// </summary>
    public sealed class PlacedItem : WorldInteractable
    {
        private ItemPlacer placer;
        private GameObject model;
        private JarProp jar;
        private readonly StringBuilder sb = new StringBuilder(48);
        private string cachedPrompt;

        public string Id { get; private set; }
        public SpeciesData Species { get; private set; }
        public int Count { get; private set; }

        public void Init(ItemPlacer owner, string id, SpeciesData species, int count, JarProp defaultJar)
        {
            placer = owner;
            Id = id;
            Species = species;
            Count = count;
            model = ItemModels.Spawn(species, defaultJar, transform, out jar);
            if (model != null)
            {
                var anim = model.GetComponentInChildren<Animator>();
                if (anim != null) anim.enabled = false; // yerde duran alet oynamaz
                // Modelin kendi collider'ları kapanır; tek kutu bu objede (ışın ve oyuncu için).
                var cols = model.GetComponentsInChildren<Collider>();
                for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;
                sb.Clear();
                sb.Append("<b>").Append(species.FormulaRich).Append("</b>  ×").Append(count);
                if (jar != null) jar.SetLabel(sb.ToString());
            }
            FitCollider();
            cachedPrompt = null;
        }

        private void FitCollider()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) box = gameObject.AddComponent<BoxCollider>();
            if (model == null)
            {
                box.center = new Vector3(0f, 0.08f, 0f);
                box.size = new Vector3(0.16f, 0.16f, 0.16f);
                return;
            }
            var b = ItemModels.RendererBounds(model);
            if (b.size == Vector3.zero) b.size = new Vector3(0.16f, 0.16f, 0.16f);
            // Sınır dünya uzayında; obje yalnızca Y ekseninde döndüğü için yerel eksene yaklaşık çevrilir.
            var center = transform.InverseTransformPoint(b.center);
            var size = transform.InverseTransformVector(b.size);
            size = new Vector3(Mathf.Max(0.08f, Mathf.Abs(size.x)), Mathf.Max(0.08f, Mathf.Abs(size.y)), Mathf.Max(0.08f, Mathf.Abs(size.z)));
            box.center = center;
            box.size = size;
        }

        public override string GetPrompt(PlayerInteractor interactor)
        {
            if (cachedPrompt != null) return cachedPrompt;
            var texts = WorkshopTexts.Get();
            cachedPrompt = Species != null ? string.Format(texts.PickPrompt, Species.FormulaRich, Count) : string.Empty;
            return cachedPrompt;
        }

        public override void Interact(PlayerInteractor interactor)
        {
            var inv = interactor.Inventory;
            if (inv == null || Species == null) return;
            if (!inv.Add(Species, Count))
            {
                ViewLocator.Get<UI.INoticeView>()?.ShowNotice(WorkshopTexts.Get().InventoryFull);
                return;
            }
            if (placer != null) placer.Forget(this);
            Destroy(gameObject);
        }
    }
}
