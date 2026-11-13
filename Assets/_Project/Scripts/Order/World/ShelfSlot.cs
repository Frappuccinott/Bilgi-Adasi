using System.Text;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using BilgiAdasi.Player;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Raftaki bir yer: tek maddenin yığını durur (kavanoz modeli + etiket). E: dolu yerden yığının tamamı envantere,
    /// boş yere eldeki yığının tamamı. İçerik kayda yazılır (anahtar: rafId/yer). Shelf kurar; collider bu objede.
    /// Elde madde varken boş yerlerde yeşil işaret (placeMarker) yanar: "buraya koyabilirsin".
    /// </summary>
    public sealed class ShelfSlot : WorldInteractable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Elde madde varken boş yerde görünen yeşil işaret (\"buraya koyabilirsin\"). Boş bırakılabilir.")]
        [SerializeField] private Renderer placeMarker;
        [SerializeField] private Color markerColor = new Color(0.247f, 0.659f, 0.227f);   // 09 palet: yeşil yaprak orta #3FA83A
        [Tooltip("Oyuncu bu yere bakarken işaretin rengi.")]
        [SerializeField] private Color markerFocusColor = new Color(0.651f, 0.878f, 0.235f); // çimen açık #A6E03C

        private MaterialPropertyBlock markerBlock;
        private bool focused;

        private Shelf shelf;
        private string saveKey;
        private SpeciesData species;
        private int count;
        private JarProp jar;
        private readonly StringBuilder sb = new StringBuilder(48);

        private string cachedPrompt;
        private SpeciesData cachedHand;
        private int cachedHandCount = -1;
        private bool dirty = true;

        public SpeciesData Species => species;
        public int Count => count;

        private static SaveData Data => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

        /// <summary>Shelf çağırır (Awake'te): kimlik ve kayıttan içerik.</summary>
        public void Init(Shelf owner, string key)
        {
            shelf = owner;
            saveKey = key;
            var data = Data;
            if (data != null && data.TryGetShelf(key, out var entry))
                Set(owner.Economy != null ? owner.Economy.FindSpecies(entry.species) : null, entry.count, false);
        }

        private void OnEnable()
        {
            GameEvents.HandItemChanged += OnHandItemChanged;
            UpdateMarker();
        }

        private void OnDisable() => GameEvents.HandItemChanged -= OnHandItemChanged;

        public override void SetHighlighted(bool highlighted)
        {
            base.SetHighlighted(highlighted);
            focused = highlighted;
            UpdateMarker();
        }

        private void OnHandItemChanged(bool _) => UpdateMarker();

        /// <summary>Yer boşsa ve elde madde varsa yeşil işaret görünür; bakılan yer daha açık yeşil.</summary>
        private void UpdateMarker()
        {
            if (placeMarker == null) return;
            bool show = count == 0 && GameEvents.HasHandItem;
            placeMarker.gameObject.SetActive(show);
            if (!show) return;
            if (markerBlock == null) markerBlock = new MaterialPropertyBlock();
            placeMarker.GetPropertyBlock(markerBlock);
            markerBlock.SetColor(BaseColorId, focused ? markerFocusColor : markerColor);
            placeMarker.SetPropertyBlock(markerBlock);
        }

        public override string GetPrompt(PlayerInteractor interactor)
        {
            var texts = shelf != null ? shelf.Texts : null;
            if (texts == null) return string.Empty;
            var inv = interactor.Inventory;
            var hand = inv != null ? inv.SelectedSpecies : null;
            int handCount = inv != null ? inv.SelectedCount : 0;
            if (!dirty && cachedPrompt != null && hand == cachedHand && handCount == cachedHandCount) return cachedPrompt;

            dirty = false;
            cachedHand = hand;
            cachedHandCount = handCount;
            if (count > 0) cachedPrompt = string.Format(texts.TakeShelfPrompt, species.FormulaRich, count);
            else if (hand != null) cachedPrompt = string.Format(texts.PlaceShelfPrompt, hand.FormulaRich, handCount);
            else cachedPrompt = texts.EmptyShelfPrompt;
            return cachedPrompt;
        }

        public override void Interact(PlayerInteractor interactor)
        {
            var inv = interactor.Inventory;
            if (inv == null) return;

            if (count > 0)
            {
                if (!inv.Add(species, count))
                {
                    shelf.Notice(shelf.Texts != null ? shelf.Texts.InventoryFull : null);
                    return;
                }
                Set(null, 0, true);
            }
            else if (inv.SelectedSpecies != null)
            {
                var stack = inv.TakeSelected();
                Set(stack.Species, stack.Count, true);
            }
        }

        private void Set(SpeciesData s, int n, bool save)
        {
            species = n > 0 ? s : null;
            count = species != null ? n : 0;
            dirty = true;

            if (jar != null && (species == null || jar.Species != species))
            {
                Destroy(jar.gameObject);
                jar = null;
            }
            if (species != null && jar == null) jar = shelf.SpawnJar(species, transform);
            if (jar != null)
            {
                sb.Clear();
                sb.Append("<b>").Append(species.FormulaRich).Append("</b>  ×").Append(count);
                jar.SetLabel(sb.ToString());
                shelf.FaceLabel(jar);
            }

            UpdateMarker();
            if (save) Data?.SetShelf(saveKey, species != null ? species.Formula : null, count);
        }
    }
}
