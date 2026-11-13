using System.Collections.Generic;
using System.Text;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using BilgiAdasi.Order;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// TEST YER TUTUCUSU — sipariş istasyonunun bütün arayüzlerini ham TMP metni ve düğmelerle uygular.
    /// Arayüzü çalışma zamanında kendisi kurar (Canvas altına konması yeter). Mustafa'nın view'ları gelince silinir;
    /// onlar aynı arayüzleri ayrı ayrı uygular (IShelfView, IScaleView …).
    /// </summary>
    public sealed class PlaceholderOrderView : MonoBehaviour,
        IOrderStationView, IOrderCardView, IShelfView, ICauldronView, IScaleView,
        IChalkboardView, IHintBookView, IRealLifeCardView
    {
        [Header("Ham etiketler (yer tutucu)")]
        [SerializeField] private string fireLabel = "Ateşi yak [F / Y]";
        [SerializeField] private string hintLabel = "Defter [H / X]";
        [SerializeField] private string closeLabel = "Kapat";
        [SerializeField] private string addLabel = "Kazana";

        private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color ButtonColor = new Color(0.25f, 0.25f, 0.3f, 0.95f);
        private static readonly Color SelectedColor = new Color(0.45f, 0.35f, 0.1f, 0.95f);

        private GameObject panel;
        private TMP_Text cardText, messageText, scaleText, boardText, hintText, realLifeText, stoveText;
        private RectTransform shelfList, cauldronList;
        private Button hintButton;
        private GameObject realLifeBox;

        private IReadOnlyList<AtomRow> lastRows;
        private bool lastBalanced;
        private bool foamActive;
        private readonly StringBuilder sb = new StringBuilder(256);

        public bool IsHintOpen { get; private set; }

        private static IOrderCommands Commands => ViewLocator.Get<IOrderCommands>();

        private void Awake() => BuildUi();

        private void OnEnable()
        {
            ViewLocator.Register<IOrderStationView>(this);
            ViewLocator.Register<IOrderCardView>(this);
            ViewLocator.Register<IShelfView>(this);
            ViewLocator.Register<ICauldronView>(this);
            ViewLocator.Register<IScaleView>(this);
            ViewLocator.Register<IChalkboardView>(this);
            ViewLocator.Register<IHintBookView>(this);
            ViewLocator.Register<IRealLifeCardView>(this);
        }

        private void OnDisable()
        {
            ViewLocator.Unregister<IOrderStationView>(this);
            ViewLocator.Unregister<IOrderCardView>(this);
            ViewLocator.Unregister<IShelfView>(this);
            ViewLocator.Unregister<ICauldronView>(this);
            ViewLocator.Unregister<IScaleView>(this);
            ViewLocator.Unregister<IChalkboardView>(this);
            ViewLocator.Unregister<IHintBookView>(this);
            ViewLocator.Unregister<IRealLifeCardView>(this);
        }

        // ---------- Arayüzler ----------

        public void SetOpen(bool open)
        {
            if (panel != null) panel.SetActive(open);
        }

        public void ShowOrder(OrderCardInfo info)
        {
            sb.Clear();
            sb.Append("<b>").Append(info.GiverName).Append("</b>");
            if (info.StepCount > 1) sb.Append("  (adım ").Append(info.StepIndex + 1).Append('/').Append(info.StepCount).Append(')');
            sb.Append("\nİstenen: ");
            AppendAmounts(info.Products);
            if (info.Reactants != null)
            {
                sb.Append("\nGirenler: ");
                for (int i = 0; i < info.Reactants.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(info.Reactants[i].FormulaRich);
                }
            }
            if (info.IsPurchase) sb.Append("\nKese: ").Append(info.Money).Append(" / ").Append(info.Budget).Append(' ').Append(info.CurrencyName);
            cardText.text = sb.ToString();
        }

        public void ShowMessage(string text) => messageText.text = text;

        public void ShowShelf(IReadOnlyList<ShelfItem> items, SpeciesData selected)
        {
            Clear(shelfList);
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var row = Row(shelfList);
                var s = item.Species;
                // Kavanoz etiketi = formülün kendisi. Tıklanınca mühür titrer (formül değiştirilemez).
                MakeButton(row, s.FormulaRich, () => Commands?.TouchLabel(s), false, 150);
                MakeButton(row, addLabel, () => Commands?.AddToCauldron(s), s == selected, 120);
                string price = item.Price > 0 ? $"{item.Price}" : "";
                string owned = item.Owned >= 0 ? $"  elde {item.Owned}" : "";
                MakeLabel(row, price + owned, 22, 0);
            }
        }

        public void PlaySealShake(SpeciesData species, string note)
        {
            messageText.text = note != null ? note : $"[mühür titredi: {species.FormulaRich}]";
        }

        public void ShowContents(IReadOnlyList<SpeciesAmount> contents, SpeciesData selected)
        {
            Clear(cauldronList);
            if (contents.Count == 0)
            {
                MakeLabel(Row(cauldronList), "(kazan boş)", 26, 300);
                return;
            }
            for (int i = 0; i < contents.Count; i++)
            {
                var c = contents[i];
                var s = c.Species;
                var row = Row(cauldronList);
                MakeButton(row, "−", () => Commands?.Decrease(s), false, 60);
                MakeLabel(row, $"{c.Amount} × {s.FormulaRich}", 30, 220);
                MakeButton(row, "+", () => Commands?.Increase(s), false, 60);
                MakeButton(row, "seç", () => Commands?.Select(s), s == selected, 80);
            }
        }

        public void SetStoveLit(bool lit) => stoveText.text = lit ? "<color=#FFA040>OCAK YANIYOR</color>" : "ocak sönük";

        public void PlayFoam(float duration) => stoveText.text = "<color=#80C0FF>KÖPÜRDÜ!</color>";

        public void PlayReaction(float duration) => stoveText.text = "<color=#FFA040>TEPKİME…</color>";

        public void ShowAtoms(IReadOnlyList<AtomRow> rows, bool balanced)
        {
            lastRows = rows;
            lastBalanced = balanced;
            RenderScale();
        }

        public void ShowFoamDifference(IReadOnlyList<AtomRow> rows)
        {
            lastRows = rows;
            foamActive = true;
            RenderScale();
        }

        public void ClearFoamDifference()
        {
            foamActive = false;
            RenderScale();
            stoveText.text = "ocak sönük";
        }

        public void ShowEquations(IReadOnlyList<string> richEquations)
        {
            sb.Clear();
            sb.Append("<b>Tahta</b>");
            for (int i = 0; i < richEquations.Count; i++) sb.Append('\n').Append(richEquations[i]);
            boardText.text = sb.ToString();
        }

        public void SetGlowing(bool glowing)
        {
            if (hintButton == null) return;
            hintButton.GetComponent<Image>().color = glowing ? new Color(0.8f, 0.65f, 0.1f, 1f) : ButtonColor;
        }

        public void ShowHint(string text)
        {
            IsHintOpen = true;
            hintText.text = "<i>" + text + "</i>";
        }

        public void HideHint()
        {
            IsHintOpen = false;
            if (hintText != null) hintText.text = "";
        }

        public void Show(string text)
        {
            realLifeBox.SetActive(true);
            realLifeText.text = "<b>Gerçek hayatta</b>\n" + text;
        }

        public void Hide()
        {
            if (realLifeBox != null) realLifeBox.SetActive(false);
        }

        // ---------- Çizim ----------

        private void RenderScale()
        {
            if (scaleText == null || lastRows == null) return;
            sb.Clear();
            sb.Append("<b>Terazi</b>  sol / sağ");
            for (int i = 0; i < lastRows.Count; i++)
            {
                var r = lastRows[i];
                string color = r.IsEqual ? "#7CFC7C" : "#FF6060";
                sb.Append("\n<color=").Append(color).Append('>').Append(r.Element.Symbol).Append("  ")
                  .Append(r.Left).Append(" / ").Append(r.Right).Append("</color>");
                if (r.Leftover > 0) sb.Append("  <color=#C0C0C0>(+").Append(r.Leftover).Append(" artan)</color>");
                if (foamActive && r.Left > r.Right) sb.Append("  ← fazla");
                if (foamActive && r.Left < r.Right) sb.Append("  ← eksik");
            }
            sb.Append(lastBalanced ? "\n<color=#7CFC7C>DENGEDE</color>" : "\n<color=#FF6060>dengede değil</color>");
            scaleText.text = sb.ToString();
        }

        private void AppendAmounts(IReadOnlyList<SpeciesAmount> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(" + ");
                if (list[i].Amount != 1) sb.Append(list[i].Amount).Append(' ');
                sb.Append(list[i].Species.FormulaRich);
            }
        }

        // ---------- Arayüz kurulumu (yalnızca Awake'te) ----------

        private void BuildUi()
        {
            panel = new GameObject("OrderPanel", typeof(RectTransform));
            var prt = (RectTransform)panel.transform;
            prt.SetParent(transform, false);
            Stretch(prt);

            var leftCol = Column(prt, new Vector2(0.01f, 0.02f), new Vector2(0.33f, 0.98f));
            cardText = MakeLabel(leftCol, "", 28, 0);
            messageText = MakeLabel(leftCol, "", 24, 0);
            shelfList = Column(leftCol, Vector2.zero, Vector2.one, true);

            var midCol = Column(prt, new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.6f));
            stoveText = MakeLabel(midCol, "ocak sönük", 30, 0);
            cauldronList = Column(midCol, Vector2.zero, Vector2.one, true);
            var actions = Row(midCol);
            MakeButton(actions, fireLabel, () => Commands?.FireStove(), false, 300);
            hintButton = MakeButton(actions, hintLabel, () => Commands?.ToggleHint(), false, 260);
            hintText = MakeLabel(midCol, "", 24, 0);

            var rightCol = Column(prt, new Vector2(0.67f, 0.02f), new Vector2(0.99f, 0.98f));
            scaleText = MakeLabel(rightCol, "", 30, 0);
            boardText = MakeLabel(rightCol, "<b>Tahta</b>", 26, 0);

            realLifeBox = new GameObject("RealLifeCard", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            var rl = (RectTransform)realLifeBox.transform;
            rl.SetParent(prt, false);
            rl.anchorMin = new Vector2(0.3f, 0.62f);
            rl.anchorMax = new Vector2(0.7f, 0.95f);
            rl.offsetMin = rl.offsetMax = Vector2.zero;
            realLifeBox.GetComponent<Image>().color = new Color(0.15f, 0.12f, 0.05f, 0.95f);
            var rlLayout = realLifeBox.GetComponent<VerticalLayoutGroup>();
            rlLayout.padding = new RectOffset(20, 20, 20, 20);
            rlLayout.childControlHeight = rlLayout.childControlWidth = true;
            rlLayout.childForceExpandHeight = false;
            realLifeText = MakeLabel(rl, "", 28, 0);
            MakeButton(rl, closeLabel, () => Commands?.CloseRealLifeCard(), false, 200);
            realLifeBox.SetActive(false);

            panel.SetActive(false);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static RectTransform Column(RectTransform parent, Vector2 min, Vector2 max, bool nested = false)
        {
            var go = new GameObject("Column", typeof(RectTransform), typeof(VerticalLayoutGroup));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            if (nested)
            {
                go.AddComponent<LayoutElement>().minHeight = 40;
                return rt;
            }
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var bg = go.AddComponent<Image>();
            bg.color = PanelColor;
            layout.padding = new RectOffset(12, 12, 12, 12);
            return rt;
        }

        private static RectTransform Row(RectTransform parent)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            go.GetComponent<LayoutElement>().minHeight = 48;
            return rt;
        }

        private static TMP_Text MakeLabel(RectTransform parent, string text, float size, float width)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.richText = true;
            t.raycastTarget = false;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            var le = go.GetComponent<LayoutElement>();
            if (width > 0) le.preferredWidth = width;
            else le.flexibleWidth = 1;
            return t;
        }

        private static Button MakeButton(RectTransform parent, string text, UnityAction onClick, bool highlighted, float width = 140)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = highlighted ? SelectedColor : ButtonColor;
            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minHeight = 44;
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);

            var label = new GameObject("Text", typeof(RectTransform));
            var lrt = (RectTransform)label.transform;
            lrt.SetParent(go.transform, false);
            Stretch(lrt);
            var t = label.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = 24;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return button;
        }

        private static void Clear(RectTransform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--) Destroy(parent.GetChild(i).gameObject);
        }
    }
}
