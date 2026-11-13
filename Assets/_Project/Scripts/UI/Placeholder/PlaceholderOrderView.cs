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
    /// TEST YER TUTUCUSU — sipariş istasyonunun bütün arayüzlerini uGUI ile, asset kullanmadan uygular
    /// (parşömen kart, renkli kavanozlar, atom toplu iki kefe, eğilen terazi kolu, karatahta).
    /// Arayüzü çalışma zamanında kendisi kurar; Canvas altına konması yeter. Mustafa'nın view'ları gelince silinir.
    /// </summary>
    public sealed class PlaceholderOrderView : MonoBehaviour,
        IOrderStationView, IOrderCardView, IShelfView, ICauldronView, IScaleView,
        IChalkboardView, IHintBookView, IRealLifeCardView
    {
        [Header("Ham etiketler (yer tutucu)")]
        // Tuş simgeleri ControlGlyphs'ten gelir (klavye / Xbox / PS / Switch); burada yalnızca kelimeler var.
        [SerializeField] private string fireLabel = "Ateşi yak";
        [SerializeField] private string hintLabel = "Defter";
        [SerializeField] private string closeLabel = "Tamam";
        [SerializeField] private string addLabel = "Kazana";
        [SerializeField] private string selectWord = "seç";
        [SerializeField] private string addWord = "ekle";
        [SerializeField] private string removeWord = "çıkar";
        [SerializeField] private string exitWord = "çık";

        // Renk paleti (simya atölyesi)
        private static readonly Color Parchment = new Color(0.91f, 0.85f, 0.71f, 0.97f);
        private static readonly Color ParchmentDark = new Color(0.80f, 0.71f, 0.53f, 1f);
        private static readonly Color Ink = new Color(0.23f, 0.16f, 0.10f, 1f);
        private static readonly Color Wood = new Color(0.33f, 0.22f, 0.13f, 0.96f);
        private static readonly Color WoodLight = new Color(0.47f, 0.33f, 0.20f, 1f);
        private static readonly Color Brass = new Color(0.78f, 0.62f, 0.25f, 1f);
        private static readonly Color Board = new Color(0.12f, 0.22f, 0.17f, 0.97f);
        private static readonly Color Chalk = new Color(0.93f, 0.95f, 0.90f, 1f);
        private static readonly Color Fire = new Color(0.85f, 0.38f, 0.12f, 1f);
        private static readonly Color Good = new Color(0.30f, 0.55f, 0.25f, 1f);
        private static readonly Color Bad = new Color(0.70f, 0.18f, 0.14f, 1f);
        private static readonly Color Leftover = new Color(0.55f, 0.55f, 0.55f, 1f);

        private const int MaxAtomsDrawn = 10;
        private const float AtomSize = 26f;

        private static Sprite roundedSprite, circleSprite, ringSprite;

        private GameObject panel;
        private TMP_Text cardText, messageText, stoveText, selectText, hintText, realLifeText, boardText, balanceText;
        private RectTransform shelfList, cauldronList, leftPan, rightPan, beam;
        private Image hintButtonImage, stoveIcon;
        private TMP_Text fireButtonText, hintButtonText, closeButtonText;
        private GameObject messageBox, hintBox, realLifeBox;

        private IReadOnlyList<AtomRow> lastRows;
        private bool lastBalanced;
        private bool foamActive;
        private float beamTarget;
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
            GameInput.SchemeChanged += ApplySchemeLabels;
            ApplySchemeLabels(GameInput.LastScheme);
        }

        /// <summary>Tuş ipuçları son kullanılan cihazın düzenine göre (CLAUDE.md §3).</summary>
        private void ApplySchemeLabels(ControlScheme scheme)
        {
            if (fireButtonText != null) fireButtonText.text = fireLabel + "  " + ControlGlyphs.For(PromptAction.FireStove, scheme);
            if (hintButtonText != null) hintButtonText.text = hintLabel + "  " + ControlGlyphs.For(PromptAction.OpenHint, scheme);
            if (closeButtonText != null) closeButtonText.text = closeLabel + "  " + ControlGlyphs.For(PromptAction.Confirm, scheme);
            if (selectText == null) return;

            sb.Clear();
            sb.Append(ControlGlyphs.For(PromptAction.Navigate, scheme)).Append(' ').Append(selectWord).Append("   ·   ")
              .Append(ControlGlyphs.For(PromptAction.Confirm, scheme)).Append(" / ")
              .Append(ControlGlyphs.For(PromptAction.Increase, scheme)).Append(' ').Append(addWord).Append("   ·   ")
              .Append(ControlGlyphs.For(PromptAction.Decrease, scheme)).Append(' ').Append(removeWord).Append("   ·   ")
              .Append(ControlGlyphs.For(PromptAction.ExitStation, scheme)).Append(' ').Append(exitWord);
            selectText.text = sb.ToString();
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
            GameInput.SchemeChanged -= ApplySchemeLabels;
        }

        private void Update()
        {
            // Yalnızca terazi kolunun yumuşak eğilmesi (görsel).
            if (beam == null || !panel.activeSelf) return;
            float z = beam.localEulerAngles.z;
            if (z > 180f) z -= 360f;
            if (Mathf.Abs(z - beamTarget) < 0.05f) return;
            beam.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(z, beamTarget, 1f - Mathf.Exp(-8f * Time.deltaTime)));
        }

        // ---------- Arayüzler ----------

        public void SetOpen(bool open)
        {
            if (panel != null) panel.SetActive(open);
        }

        public void ShowOrder(OrderCardInfo info)
        {
            sb.Clear();
            sb.Append("<size=34><b>").Append(info.GiverName).Append("</b></size>");
            if (info.StepCount > 1) sb.Append("   <size=24>adım ").Append(info.StepIndex + 1).Append('/').Append(info.StepCount).Append("</size>");
            sb.Append("\n<size=24>İstediği</size>\n<size=34>");
            for (int i = 0; i < info.Products.Count; i++)
            {
                if (i > 0) sb.Append(" + ");
                if (info.Products[i].Amount != 1) sb.Append(info.Products[i].Amount).Append(' ');
                sb.Append(info.Products[i].Species.FormulaRich);
            }
            sb.Append("</size>");
            if (info.Reactants != null)
            {
                sb.Append("\n<size=24>Malzemeler: ");
                for (int i = 0; i < info.Reactants.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(info.Reactants[i].FormulaRich);
                }
                sb.Append("</size>");
            }
            if (info.IsPurchase)
                sb.Append("\n<size=28>Kese: <b>").Append(info.Money).Append("</b> / ").Append(info.Budget).Append(' ').Append(info.CurrencyName).Append("</size>");
            cardText.text = sb.ToString();
        }

        public void ShowMessage(string text)
        {
            messageText.text = text;
            messageBox.SetActive(!string.IsNullOrEmpty(text));
        }

        public void ShowShelf(IReadOnlyList<ShelfItem> items, SpeciesData selected)
        {
            Clear(shelfList);
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var s = item.Species;
                var row = HRow(shelfList, 64, s == selected ? WoodLight : new Color(0, 0, 0, 0));

                // Kavanoz: renkli gövde + formül etiketi. Etikete tıklamak = mühür kuralı.
                Box(row, s.JarColor, 40, 52);
                var label = MakeButton(row, s.FormulaRich, () => Commands?.TouchLabel(s), Parchment, Ink, 150, 30);
                label.GetComponentInChildren<TMP_Text>().fontStyle = FontStyles.Bold;

                string info = item.Price > 0 ? $"{item.Price} akçe" : "";
                if (item.Owned >= 0) info += (info.Length > 0 ? "  ·  " : "") + $"elde {item.Owned}";
                Text(row, info, 22, Chalk, 130, TextAlignmentOptions.Left);
                MakeButton(row, addLabel + "  +", () => Commands?.AddToCauldron(s), Brass, Ink, 140, 26);
            }
        }

        public void PlaySealShake(SpeciesData species, string note)
        {
            ShowMessage(note ?? $"{species.FormulaRich} mühürlü.");
        }

        public void ShowContents(IReadOnlyList<SpeciesAmount> contents, SpeciesData selected)
        {
            Clear(cauldronList);
            if (contents.Count == 0)
            {
                Text(HRow(cauldronList, 56, new Color(0, 0, 0, 0)), "Kazan boş — raftan kavanoz ekle.", 26, Chalk, 0, TextAlignmentOptions.Center);
                return;
            }

            SpeciesData effectiveSelected = selected;
            bool found = false;
            for (int i = 0; i < contents.Count; i++) if (contents[i].Species == selected) found = true;
            if (!found) effectiveSelected = contents[0].Species;

            for (int i = 0; i < contents.Count; i++)
            {
                var c = contents[i];
                var s = c.Species;
                bool isSel = s == effectiveSelected;
                var row = HRow(cauldronList, 60, isSel ? WoodLight : new Color(0, 0, 0, 0.15f));
                MakeButton(row, "−", () => Commands?.Decrease(s), ParchmentDark, Ink, 56, 34);
                Box(row, s.JarColor, 30, 40);
                // Satıra (ada) tıklamak seçer: tekerlek / RB-LB bu kavanozu değiştirir.
                MakeButton(row, $"<b>{c.Amount}</b>  ×  {s.FormulaRich}", () => Commands?.Select(s),
                    new Color(0, 0, 0, 0), Chalk, 300, 32);
                MakeButton(row, "+", () => Commands?.Increase(s), ParchmentDark, Ink, 56, 34);
            }
        }

        public void SetStoveLit(bool lit)
        {
            stoveIcon.color = lit ? Fire : new Color(0.25f, 0.2f, 0.18f, 1f);
            stoveText.text = lit ? "Ocak yanıyor" : "Ocak sönük";
        }

        public void PlayFoam(float duration)
        {
            stoveIcon.color = new Color(0.55f, 0.75f, 0.95f, 1f);
            stoveText.text = "Köpürdü! Teraziye bak.";
        }

        public void PlayReaction(float duration)
        {
            stoveIcon.color = Fire;
            stoveText.text = "Tepkime…";
        }

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
            SetStoveLit(false);
        }

        public void ShowEquations(IReadOnlyList<string> richEquations)
        {
            sb.Clear();
            if (richEquations.Count == 0) sb.Append("<alpha=#80>(henüz denklem yok)");
            for (int i = 0; i < richEquations.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(richEquations[i]);
            }
            boardText.text = sb.ToString();
        }

        public void SetGlowing(bool glowing) => hintButtonImage.color = glowing ? Brass : ParchmentDark;

        public void ShowHint(string text)
        {
            IsHintOpen = true;
            hintText.text = "<b>Ustanın defteri</b>\n" + text;
            hintBox.SetActive(true);
        }

        public void HideHint()
        {
            IsHintOpen = false;
            if (hintBox != null) hintBox.SetActive(false);
        }

        public void Show(string text)
        {
            realLifeText.text = "<size=34><b>Gerçek hayatta</b></size>\n\n" + text;
            realLifeBox.SetActive(true);
        }

        public void Hide()
        {
            if (realLifeBox != null) realLifeBox.SetActive(false);
        }

        // ---------- Terazi ----------

        private void RenderScale()
        {
            if (lastRows == null || leftPan == null) return;
            Clear(leftPan);
            Clear(rightPan);

            int totalLeft = 0, totalRight = 0;
            for (int i = 0; i < lastRows.Count; i++)
            {
                var r = lastRows[i];
                totalLeft += r.Left;
                totalRight += r.Right;

                var lrow = HRow(leftPan, 40, new Color(0, 0, 0, 0));
                var rrow = HRow(rightPan, 40, new Color(0, 0, 0, 0));
                lrow.GetComponent<HorizontalLayoutGroup>().spacing = 3;
                rrow.GetComponent<HorizontalLayoutGroup>().spacing = 3;
                Color countColor = r.IsEqual ? Good : Bad;

                // Sol kefe: kazandaki atomlar. Köpürmede fazlalar kırmızı halkalı, eksikler boş halka.
                int excess = foamActive ? Mathf.Max(0, r.Left - r.Right) : 0;
                int missing = foamActive ? Mathf.Max(0, r.Right - r.Left) : 0;
                Text(lrow, $"<b>{r.Left}</b>", 26, countColor, 44, TextAlignmentOptions.Right);
                Atoms(lrow, r.Element, r.Left - excess, false, false);
                Atoms(lrow, r.Element, excess, false, true);
                Atoms(lrow, r.Element, missing, true, false);
                if (r.Leftover > 0) AtomsLeftover(lrow, r.Element, r.Leftover);

                Text(rrow, $"<b>{r.Right}</b>", 26, Ink, 44, TextAlignmentOptions.Right);
                Atoms(rrow, r.Element, r.Right, false, false);
            }

            beamTarget = Mathf.Clamp((totalLeft - totalRight) * 1.5f, -10f, 10f);
            balanceText.text = lastBalanced ? "<color=#3E7A2E><b>DENGEDE</b></color> — ateşi yakabilirsin"
                : "<color=#A12E22><b>Dengede değil</b></color>";
        }

        private void Atoms(RectTransform row, ElementData element, int count, bool hollow, bool excess)
        {
            if (count <= 0) return;
            int drawn = Mathf.Min(count, MaxAtomsDrawn);
            for (int i = 0; i < drawn; i++)
            {
                var go = new GameObject("Atom", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                go.transform.SetParent(row, false);
                var img = go.GetComponent<Image>();
                img.sprite = hollow ? ringSprite : circleSprite;
                img.color = hollow ? Bad : element.Color;
                img.raycastTarget = false;
                var le = go.GetComponent<LayoutElement>();
                le.preferredWidth = le.preferredHeight = AtomSize;

                if (!hollow)
                {
                    var letter = Text((RectTransform)go.transform, element.Symbol, 16, ContrastFor(element.Color), 0, TextAlignmentOptions.Center);
                    Stretch(letter.rectTransform);
                    letter.fontStyle = FontStyles.Bold;
                }
                if (excess)
                {
                    var ring = new GameObject("Excess", typeof(RectTransform), typeof(Image));
                    ring.transform.SetParent(go.transform, false);
                    Stretch((RectTransform)ring.transform);
                    var ri = ring.GetComponent<Image>();
                    ri.sprite = ringSprite;
                    ri.color = Bad;
                    ri.raycastTarget = false;
                }
            }
            if (count > drawn) Text(row, $"+{count - drawn}", 20, Ink, 40, TextAlignmentOptions.Left);
        }

        private void AtomsLeftover(RectTransform row, ElementData element, int count)
        {
            Text(row, "artan", 18, Leftover, 52, TextAlignmentOptions.Right);
            int drawn = Mathf.Min(count, 6);
            for (int i = 0; i < drawn; i++)
            {
                var go = new GameObject("Leftover", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                go.transform.SetParent(row, false);
                var img = go.GetComponent<Image>();
                img.sprite = circleSprite;
                img.color = new Color(element.Color.r, element.Color.g, element.Color.b, 0.35f);
                img.raycastTarget = false;
                var le = go.GetComponent<LayoutElement>();
                le.preferredWidth = le.preferredHeight = 22;
            }
            if (count > drawn) Text(row, $"+{count - drawn}", 18, Leftover, 36, TextAlignmentOptions.Left);
        }

        private static Color ContrastFor(Color c) =>
            c.r * 0.299f + c.g * 0.587f + c.b * 0.114f > 0.55f ? Ink : Color.white;

        // ---------- Kurulum (yalnızca Awake) ----------

        private void BuildUi()
        {
            EnsureSprites();

            panel = new GameObject("OrderPanel", typeof(RectTransform));
            var root = (RectTransform)panel.transform;
            root.SetParent(transform, false);
            Stretch(root);

            // Sol üst: sipariş kartı (parşömen)
            var card = Panel(root, "Card", Parchment, new Vector2(0, 1), new Vector2(24, -24), new Vector2(560, 230));
            cardText = Text(card, "", 28, Ink, 0, TextAlignmentOptions.TopLeft);
            Inset(cardText.rectTransform, 22);

            // Kartın altında kısa mesaj (sakinin repliği, mühür notu)
            messageBox = Panel(root, "Message", ParchmentDark, new Vector2(0, 1), new Vector2(24, -266), new Vector2(560, 96)).gameObject;
            messageText = Text((RectTransform)messageBox.transform, "", 24, Ink, 0, TextAlignmentOptions.MidlineLeft);
            messageText.fontStyle = FontStyles.Italic;
            Inset(messageText.rectTransform, 18);
            messageBox.SetActive(false);

            // Sol: raf (ahşap)
            var shelf = Panel(root, "Shelf", Wood, new Vector2(0, 0), new Vector2(24, 24), new Vector2(560, 420));
            Title(shelf, "Raf — mühürlü kavanozlar", Chalk);
            shelfList = List(shelf, 64);

            // Sağ üst: teraziler
            var scales = Panel(root, "Scales", Parchment, new Vector2(1, 1), new Vector2(-24, -24), new Vector2(760, 470));
            Title(scales, "Teraziler", Ink);
            var beamGo = new GameObject("Beam", typeof(RectTransform), typeof(Image));
            beam = (RectTransform)beamGo.transform;
            beam.SetParent(scales, false);
            beam.anchorMin = beam.anchorMax = new Vector2(0.5f, 1f);
            beam.anchoredPosition = new Vector2(0, -78);
            beam.sizeDelta = new Vector2(620, 10);
            var beamImg = beamGo.GetComponent<Image>();
            beamImg.sprite = roundedSprite;
            beamImg.type = Image.Type.Sliced;
            beamImg.color = Brass;
            var pivot = Box(scales, Brass, 22, 22);
            pivot.anchorMin = pivot.anchorMax = new Vector2(0.5f, 1f);
            pivot.anchoredPosition = new Vector2(0, -78);
            pivot.GetComponent<Image>().sprite = circleSprite;

            leftPan = PanColumn(scales, "Giren (kazan)", new Vector2(0.02f, 0.12f), new Vector2(0.495f, 0.80f));
            rightPan = PanColumn(scales, "Çıkan (sipariş)", new Vector2(0.505f, 0.12f), new Vector2(0.98f, 0.80f));
            balanceText = Text(scales, "", 28, Ink, 0, TextAlignmentOptions.Center);
            var bt = balanceText.rectTransform;
            bt.anchorMin = new Vector2(0, 0);
            bt.anchorMax = new Vector2(1, 0.12f);
            bt.offsetMin = bt.offsetMax = Vector2.zero;

            // Sağ alt: karatahta
            var board = Panel(root, "Chalkboard", Board, new Vector2(1, 0), new Vector2(-24, 24), new Vector2(620, 300));
            Title(board, "Karatahta", Chalk);
            boardText = Text(board, "", 32, Chalk, 0, TextAlignmentOptions.TopLeft);
            Inset(boardText.rectTransform, 22, 64);

            // Alt orta: kazan
            var cauldron = Panel(root, "Cauldron", Wood, new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(560, 420));
            stoveIcon = Box(cauldron, Fire, 26, 26).GetComponent<Image>();
            stoveIcon.sprite = circleSprite;
            var si = (RectTransform)stoveIcon.transform;
            si.anchorMin = si.anchorMax = new Vector2(0, 1);
            si.anchoredPosition = new Vector2(34, -34);
            stoveText = Text(cauldron, "Ocak sönük", 26, Chalk, 0, TextAlignmentOptions.MidlineLeft);
            var st = stoveText.rectTransform;
            st.anchorMin = new Vector2(0, 1);
            st.anchorMax = new Vector2(1, 1);
            st.offsetMin = new Vector2(60, -56);
            st.offsetMax = new Vector2(-16, -12);
            Text(cauldron, "<b>Kazan</b>", 30, Chalk, 0, TextAlignmentOptions.TopRight).rectTransform.offsetMax = new Vector2(-18, -14);

            cauldronList = List(cauldron, 60, 140);
            selectText = Text(cauldron, "", 18, new Color(1, 1, 1, 0.75f), 0, TextAlignmentOptions.Center);
            var sel = selectText.rectTransform;
            sel.anchorMin = new Vector2(0, 0);
            sel.anchorMax = new Vector2(1, 0);
            sel.offsetMin = new Vector2(10, 88);
            sel.offsetMax = new Vector2(-10, 150);

            var fire = MakeButton(cauldron, fireLabel, () => Commands?.FireStove(), Fire, Color.white, 0, 30);
            fireButtonText = fire.GetComponentInChildren<TMP_Text>();
            Place((RectTransform)fire.transform, new Vector2(0, 0), new Vector2(0.62f, 0), new Vector2(16, 16), new Vector2(-6, 84));
            var book = MakeButton(cauldron, hintLabel, () => Commands?.ToggleHint(), ParchmentDark, Ink, 0, 26);
            hintButtonText = book.GetComponentInChildren<TMP_Text>();
            hintButtonImage = book.GetComponent<Image>();
            Place((RectTransform)book.transform, new Vector2(0.62f, 0), new Vector2(1, 0), new Vector2(6, 16), new Vector2(-16, 84));

            // Defter ipucu: kazanın üstünde parşömen
            hintBox = Panel(root, "Hint", Parchment, new Vector2(0.5f, 0), new Vector2(0, 460), new Vector2(560, 150)).gameObject;
            hintText = Text((RectTransform)hintBox.transform, "", 26, Ink, 0, TextAlignmentOptions.MidlineLeft);
            Inset(hintText.rectTransform, 20);
            hintBox.SetActive(false);

            // Gerçek hayatta kartı: ortada büyük parşömen
            realLifeBox = Panel(root, "RealLife", Parchment, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(820, 380)).gameObject;
            realLifeText = Text((RectTransform)realLifeBox.transform, "", 28, Ink, 0, TextAlignmentOptions.TopLeft);
            Inset(realLifeText.rectTransform, 30, 30, 100);
            var ok = MakeButton((RectTransform)realLifeBox.transform, closeLabel, () => Commands?.CloseRealLifeCard(), Brass, Ink, 0, 28);
            closeButtonText = ok.GetComponentInChildren<TMP_Text>();
            Place((RectTransform)ok.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-110, 24), new Vector2(110, 84));
            realLifeBox.SetActive(false);

            SetStoveLit(false);
            ApplySchemeLabels(GameInput.LastScheme);
            panel.SetActive(false);
        }

        private RectTransform PanColumn(RectTransform parent, string title, Vector2 min, Vector2 max)
        {
            var go = new GameObject(title, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.sprite = roundedSprite;
            img.type = Image.Type.Sliced;
            img.color = ParchmentDark;

            var t = Text(rt, $"<b>{title}</b>", 22, Ink, 0, TextAlignmentOptions.Top);
            t.rectTransform.offsetMax = new Vector2(0, -6);
            return List(rt, 40, 0, 40);
        }

        // ---------- Yardımcılar ----------

        private static RectTransform Panel(RectTransform parent, string name, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = roundedSprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            var outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0.15f, 0.09f, 0.05f, 0.8f);
            outline.effectDistance = new Vector2(3, -3);
            return rt;
        }

        private static TMP_Text Title(RectTransform panel, string text, Color color)
        {
            var t = Text(panel, $"<b>{text}</b>", 28, color, 0, TextAlignmentOptions.TopLeft);
            Inset(t.rectTransform, 18, 14);
            return t;
        }

        /// <summary>Panel içinde dikey liste (üstte başlık payı bırakır).</summary>
        private static RectTransform List(RectTransform parent, float rowHeight, float bottom = 0, float top = 56)
        {
            var go = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(14, 14 + bottom);
            rt.offsetMax = new Vector2(-14, -top);
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rt;
        }

        private static RectTransform HRow(RectTransform parent, float height, Color background)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = roundedSprite;
            img.type = Image.Type.Sliced;
            img.color = background;
            img.raycastTarget = false;
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.padding = new RectOffset(6, 6, 4, 4);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = le.minHeight = height;
            le.flexibleHeight = 0;
            return rt;
        }

        private static TMP_Text Text(RectTransform parent, string text, float size, Color color, float width, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(LayoutElement));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Stretch(rt);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.richText = true;
            t.raycastTarget = false;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            var le = go.GetComponent<LayoutElement>();
            if (width > 0) le.preferredWidth = width;
            else le.flexibleWidth = 1;
            return t;
        }

        private static RectTransform Box(RectTransform parent, Color color, float width, float height)
        {
            var go = new GameObject("Box", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(width, height);
            var img = go.GetComponent<Image>();
            img.sprite = roundedSprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            return rt;
        }

        private static Button MakeButton(RectTransform parent, string text, UnityAction onClick, Color background, Color textColor,
            float width, float fontSize)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = roundedSprite;
            img.type = Image.Type.Sliced;
            img.color = background;
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);
            var colors = button.colors;
            colors.highlightedColor = new Color(1.1f, 1.05f, 0.9f, 1f);
            colors.pressedColor = new Color(0.75f, 0.7f, 0.6f, 1f);
            button.colors = colors;
            var le = go.GetComponent<LayoutElement>();
            if (width > 0) le.preferredWidth = width;
            else le.flexibleWidth = 1;

            var t = Text(rt, text, fontSize, textColor, 0, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return button;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void Inset(RectTransform rt, float side, float top = -1, float bottom = -1)
        {
            Stretch(rt);
            rt.offsetMin = new Vector2(side, bottom < 0 ? side : bottom);
            rt.offsetMax = new Vector2(-side, -(top < 0 ? side : top));
        }

        private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static void Clear(RectTransform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false); // Destroy kare sonunda; layout'u bu karede bozmasın
                Destroy(child);
            }
        }

        /// <summary>Asset kullanmamak için yuvarlak köşe, daire ve halka sprite'larını kodla üretir (bir kez).</summary>
        private static void EnsureSprites()
        {
            if (roundedSprite != null) return;
            roundedSprite = MakeSprite(32, (x, y, s) => RoundedAlpha(x, y, s, 10f), new Vector4(12, 12, 12, 12));
            circleSprite = MakeSprite(64, (x, y, s) => CircleAlpha(x, y, s, 0f), Vector4.zero);
            ringSprite = MakeSprite(64, (x, y, s) => CircleAlpha(x, y, s, 0.72f), Vector4.zero);
        }

        private delegate float AlphaFn(float x, float y, int size);

        private static Sprite MakeSprite(int size, AlphaFn alpha, Vector4 border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha(x + 0.5f, y + 0.5f, size)) * 255));
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        private static float CircleAlpha(float x, float y, int size, float innerRatio)
        {
            float r = size * 0.5f;
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(r, r));
            float outer = Mathf.Clamp01(r - d);
            if (innerRatio <= 0f) return outer;
            float inner = Mathf.Clamp01(d - r * innerRatio);
            return outer * inner;
        }

        private static float RoundedAlpha(float x, float y, int size, float radius)
        {
            float cx = Mathf.Clamp(x, radius, size - radius);
            float cy = Mathf.Clamp(y, radius, size - radius);
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            return Mathf.Clamp01(radius - d);
        }
    }
}
