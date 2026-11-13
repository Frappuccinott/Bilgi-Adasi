using System.Collections.Generic;
using BilgiAdasi.Core;
using BilgiAdasi.Order;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Pazar tezgâhı — marka dili: karartılmış oyunun üstünde ahşap tezgâh, üstünde kâğıt etiketli kavanoz kartları
    /// (Malzeme_* ikonu, formül, ad, gümüş akçe + fiyat, "elde" rozeti). Kese tezgâhın sağ üstünde.
    /// Odaktaki kart altın çerçeveyle yükselir; kesenin yetmediği kartın fiyatı mühür renginde ve kart soluk.
    /// Gezinme UI Navigate (↑↓←→), alma Submit/tık, kapatma UI Cancel (MarketStall dinler) ya da "Kapat".
    /// Satın alınca denetleyici Open'ı yeniden çağırır: kartlar yerinde güncellenir, odak korunur.
    /// </summary>
    public sealed class FigmaMarketView : FigmaView<IMarketView>, IMarketView
    {
        [Header("Etiketler")]
        [SerializeField] private string title = "Pazar";
        [SerializeField] private string subtitle = "Kavanoz başına fiyat. Aldığın kavanoz envanterine girer.";
        [SerializeField] private string currencyWord = "akçe";
        [SerializeField] private string ownedWord = "elde";
        [SerializeField] private string buyWord = "Satın al";
        [SerializeField] private string browseWord = "Gez";
        [SerializeField] private string closeWord = "Kapat";

        private GameObject root;
        private RectTransform grid;
        private TextMeshProUGUI money, titleText, subtitleText;
        private readonly List<Card> cards = new List<Card>();
        private readonly List<(TextMeshProUGUI label, RectTransform rootRt, PromptAction action)> hints = new List<(TextMeshProUGUI, RectTransform, PromptAction)>();

        private void Awake() => Build();

        protected override void OnEnable()
        {
            base.OnEnable();
            GameInput.SchemeChanged += OnScheme;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameInput.SchemeChanged -= OnScheme;
        }

        private void OnScheme(ControlScheme scheme)
        {
            foreach (var h in hints) UiKit.SetKeyCap(h.label, h.rootRt, ControlGlyphs.For(h.action, scheme), 44f);
        }

        // ---------- IMarketView ----------

        public void Open(IReadOnlyList<MarketItem> items, int purse)
        {
            var th = Theme;
            // Tezgâh başlığı: açık tezgâh IMarketInfo verirse onunki, yoksa bu ekranın varsayılanı.
            var info = ViewLocator.Get<IMarketCommands>() as IMarketInfo;
            titleText.text = info != null && !string.IsNullOrEmpty(info.MarketTitle) ? info.MarketTitle : title;
            subtitleText.text = info != null && !string.IsNullOrEmpty(info.MarketSubtitle) ? info.MarketSubtitle : subtitle;
            bool wasOpen = root.activeSelf;
            int focus = 0;
            var es = EventSystem.current;
            if (wasOpen && es != null && es.currentSelectedGameObject != null)
            {
                var c = es.currentSelectedGameObject.GetComponent<Card>();
                if (c != null) focus = cards.IndexOf(c);
            }

            bool sameSet = cards.Count == items.Count;
            for (int i = 0; sameSet && i < items.Count; i++) sameSet = cards[i].Species == items[i].Species;
            if (!sameSet)
            {
                for (int i = grid.childCount - 1; i >= 0; i--)
                {
                    var child = grid.GetChild(i).gameObject;
                    child.SetActive(false);
                    Destroy(child);
                }
                cards.Clear();
                for (int i = 0; i < items.Count; i++) cards.Add(MakeCard(items[i], th));
            }
            for (int i = 0; i < items.Count; i++) cards[i].Refresh(items[i], purse, th, ownedWord);

            money.text = purse.ToString();
            root.SetActive(true);
            OnScheme(GameInput.LastScheme);
            if (cards.Count > 0 && es != null)
            {
                var target = cards[Mathf.Clamp(focus, 0, cards.Count - 1)].gameObject;
                if (!wasOpen || es.currentSelectedGameObject != target)
                {
                    es.SetSelectedGameObject(null);
                    es.SetSelectedGameObject(target);
                }
                if (wasOpen) cards[Mathf.Clamp(focus, 0, cards.Count - 1)].Bump();
            }
        }

        public void Close()
        {
            if (root == null) return;
            root.SetActive(false);
            var es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject != null && es.currentSelectedGameObject.transform.IsChildOf(transform))
                es.SetSelectedGameObject(null);
        }

        // ---------- Kart ----------

        private Card MakeCard(MarketItem item, UiTheme th)
        {
            var rt = UiKit.Rect(grid, "Item_" + item.Species.Formula);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var group = rt.gameObject.AddComponent<CanvasGroup>();

            var body = UiKit.Rect(rt, "Body");
            UiKit.Fill(body);
            var shadow = UiKit.Img(body, "Shadow", UiKit.WithAlpha(th.InkDeep, 0.5f), UiKit.Round8);
            UiKit.Fill(shadow.rectTransform, 0, 8, 0, -8);
            var frame = UiKit.Img(body, "Frame", th.WoodDark, UiKit.Round8);
            UiKit.Fill(frame.rectTransform);
            var paper = UiKit.Img(frame.transform, "Paper", th.Paper, UiKit.Round8);
            UiKit.Fill(paper.rectTransform, 5, 5, 5, 5);

            var icon = UiKit.Img(paper.transform, "Icon", Color.white);
            UiKit.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(112f, 112f));
            icon.preserveAspect = true;
            var sprite = th.IconFor(item.Species);
            icon.sprite = sprite != null ? sprite : UiKit.Disc;
            icon.color = sprite != null ? Color.white : item.Species.JarColor;
            if (sprite == null) icon.rectTransform.sizeDelta = new Vector2(64f, 64f);

            var formula = UiKit.Text(paper.transform, "Formula", th.BodyFont, 38f, th.InkDark, TextAlignmentOptions.Top, FontStyles.Bold);
            UiKit.Place(formula.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(240f, 46f));
            formula.text = item.Species.FormulaRich;
            var name = UiKit.Text(paper.transform, "Name", th.BodyFont, 24f, th.PaperShade, TextAlignmentOptions.Top);
            UiKit.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -174f), new Vector2(240f, 30f));
            name.text = item.Species.DisplayName;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;

            var rule = UiKit.Img(paper.transform, "Rule", UiKit.WithAlpha(th.PaperShade, 0.6f));
            UiKit.Place(rule.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(190f, 1.5f));

            var priceRow = UiKit.Rect(paper.transform, "Price");
            UiKit.Place(priceRow, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(0f, 40f));
            var hl = priceRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 8f;
            hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = hl.childControlHeight = true;
            hl.childForceExpandWidth = hl.childForceExpandHeight = false;
            priceRow.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var coin = UiKit.Img(priceRow, "Coin", th.Coin != null ? Color.white : th.PaperShade, th.Coin != null ? th.Coin : UiKit.Disc);
            var cle = coin.gameObject.AddComponent<LayoutElement>();
            cle.preferredWidth = cle.preferredHeight = 34f;
            coin.preserveAspect = true;
            var price = UiKit.Text(priceRow, "Amount", th.HeadingFont, 32f, th.InkDark, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            price.textWrappingMode = TextWrappingModes.NoWrap;
            var word = UiKit.Text(priceRow, "Word", th.BodyFont, 22f, th.PaperShade, TextAlignmentOptions.MidlineLeft);
            word.text = currencyWord;

            // "elde" rozeti: sağ üst köşede küçük mum mühür rengi etiket
            var badge = UiKit.Img(paper.transform, "Owned", th.WaxMid, UiKit.Round8);
            UiKit.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(76f, 30f));
            var owned = UiKit.Text(badge.transform, "Text", th.BodyFont, 20f, th.Paper, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Fill(owned.rectTransform, 4, 0, 4, 0);

            var card = rt.gameObject.AddComponent<Card>();
            card.transition = Selectable.Transition.None;
            card.Init(item.Species, body, frame, price, owned, badge.gameObject, group, th);
            return card;
        }

        private sealed class Card : Selectable, ISubmitHandler, IPointerClickHandler
        {
            public Chemistry.SpeciesData Species { get; private set; }
            private RectTransform body;
            private Image frame;
            private TextMeshProUGUI price, owned;
            private GameObject badge;
            private CanvasGroup group;
            private UiTheme th;
            private bool focused;
            private float lift, bump;

            public void Init(Chemistry.SpeciesData species, RectTransform b, Image f, TextMeshProUGUI p, TextMeshProUGUI o,
                GameObject badgeGo, CanvasGroup g, UiTheme theme)
            {
                Species = species;
                body = b;
                frame = f;
                price = p;
                owned = o;
                badge = badgeGo;
                group = g;
                th = theme;
            }

            public void Refresh(MarketItem item, int purse, UiTheme theme, string ownedWord)
            {
                bool afford = item.Price <= purse;
                price.text = item.Price.ToString();
                price.color = afford ? theme.InkDark : theme.WaxMid;
                group.alpha = afford ? 1f : 0.62f;
                badge.SetActive(item.Owned > 0);
                owned.text = ownedWord + " " + item.Owned;
            }

            public void Bump() => bump = 1f;

            private void Buy() => ViewLocator.Get<IMarketCommands>()?.Buy(Species);

            public override void OnSelect(BaseEventData e)
            {
                base.OnSelect(e);
                focused = true;
                frame.color = th.Gold;
            }

            public override void OnDeselect(BaseEventData e)
            {
                base.OnDeselect(e);
                focused = false;
                frame.color = th.WoodDark;
            }

            protected override void OnDisable()
            {
                base.OnDisable();
                focused = false;
                if (frame != null) frame.color = th.WoodDark;
            }

            public override void OnPointerEnter(PointerEventData e)
            {
                base.OnPointerEnter(e);
                EventSystem.current?.SetSelectedGameObject(gameObject);
            }

            public void OnSubmit(BaseEventData _) => Buy();
            public void OnPointerClick(PointerEventData _) => Buy();

            private void Update()
            {
                float dt = Time.unscaledDeltaTime;
                lift = Mathf.MoveTowards(lift, focused ? 8f : 0f, dt * 80f);
                bump = Mathf.MoveTowards(bump, 0f, dt / 0.25f);
                body.anchoredPosition = new Vector2(0f, lift);
                float s = 1f + 0.06f * Mathf.Sin(bump * Mathf.PI);
                body.localScale = new Vector3(s, s, 1f);
            }
        }

        // ---------- Kurulum ----------

        private void Build()
        {
            var th = Theme;
            var dim = UiKit.Img(transform, "Market", UiKit.WithAlpha(th.InkDeep, 0.72f), null, true);
            UiKit.Fill(dim.rectTransform);
            root = dim.gameObject;
            var vig = UiKit.Img(dim.transform, "Vignette", UiKit.WithAlpha(th.InkDeep, 0.8f), UiKit.Vignette);
            UiKit.Fill(vig.rectTransform);

            // Ahşap tezgâh
            var counter = UiKit.Rect(dim.transform, "Counter");
            UiKit.Place(counter, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1320f, 940f));
            var cshadow = UiKit.Img(counter, "Shadow", UiKit.WithAlpha(th.InkDeep, 0.6f), UiKit.Round8);
            UiKit.Fill(cshadow.rectTransform, 0, 12, 0, -12);
            var cframe = UiKit.Img(counter, "Frame", th.Wood, UiKit.Round8);
            UiKit.Fill(cframe.rectTransform);
            var top = UiKit.Img(cframe.transform, "Top", th.WoodDark, UiKit.Round8);
            UiKit.Fill(top.rectTransform, 10, 10, 10, 10);
            var grain = UiKit.Img(top.transform, "Grain", UiKit.WithAlpha(th.Wood, 0.45f), UiKit.WoodGrain);
            UiKit.Fill(grain.rectTransform);

            var head = UiKit.Text(top.transform, "Title", th.HeadingFont, 64f, th.Paper, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            UiKit.Place(head.rectTransform, new Vector2(0f, 1f), new Vector2(56f, -30f), new Vector2(700f, 80f));
            head.text = title;
            titleText = head;
            var sub = UiKit.Text(top.transform, "Subtitle", th.BodyFont, 26f, UiKit.WithAlpha(th.Paper, 0.75f), TextAlignmentOptions.TopLeft);
            UiKit.Place(sub.rectTransform, new Vector2(0f, 1f), new Vector2(58f, -112f), new Vector2(900f, 34f));
            sub.text = subtitle;
            subtitleText = sub;

            // Kese: tezgâhın sağ üstü
            var purse = UiKit.PaperCard(top.transform, "Purse", th, 4f, out var ppaper, 5f);
            UiKit.Place(purse, new Vector2(1f, 1f), new Vector2(-48f, -40f), new Vector2(200f, 62f));
            money = UiKit.Text(ppaper, "Amount", th.HeadingFont, 34f, th.InkDark, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
            UiKit.Fill(money.rectTransform, 56, 0, 74, 2);
            var mword = UiKit.Text(ppaper, "Word", th.BodyFont, 22f, th.PaperShade, TextAlignmentOptions.MidlineLeft);
            UiKit.Fill(mword.rectTransform, 132, 0, 6, 0);
            mword.text = currencyWord;
            var coin = UiKit.Img(purse, "Coin", th.Coin != null ? Color.white : th.PaperShade, th.Coin != null ? th.Coin : UiKit.Disc);
            UiKit.Place(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(-24f, 0f), new Vector2(78f, 78f));
            coin.preserveAspect = true;

            var rule = UiKit.Img(top.transform, "Rule", UiKit.WithAlpha(th.Paper, 0.25f));
            UiKit.Place(rule.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -164f), new Vector2(1200f, 1f));

            // Kart ızgarası (4 sütun), taşarsa kaydırılır
            var vp = UiKit.Img(top.transform, "Viewport", new Color(0, 0, 0, 0), null, true);
            UiKit.Place(vp.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -184f), new Vector2(1210f, 640f));
            vp.gameObject.AddComponent<RectMask2D>();
            grid = UiKit.Rect(vp.transform, "Grid");
            grid.anchorMin = new Vector2(0f, 1f);
            grid.anchorMax = new Vector2(1f, 1f);
            grid.pivot = new Vector2(0.5f, 1f);
            grid.offsetMin = grid.offsetMax = Vector2.zero;
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(276f, 290f);
            gl.spacing = new Vector2(30f, 30f);
            gl.padding = new RectOffset(4, 4, 14, 14);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 4;
            gl.childAlignment = TextAnchor.UpperCenter;
            grid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = vp.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = vp.rectTransform;
            scroll.content = grid;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            scroll.inertia = false;

            // Alt: tuş ipuçları + tıklanabilir "Kapat"
            var bar = UiKit.Rect(top.transform, "Hints");
            UiKit.Place(bar, new Vector2(0f, 0f), new Vector2(56f, 34f), new Vector2(0f, 44f));
            var row = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 36f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            bar.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            Hint(bar, th, PromptAction.Confirm, buyWord);
            Hint(bar, th, PromptAction.UiHorizontal, browseWord);
            Hint(bar, th, PromptAction.Cancel, closeWord);

            var close = UiKit.Img(top.transform, "Close", new Color(0, 0, 0, 0), UiKit.Round8, true);
            UiKit.Place(close.rectTransform, new Vector2(1f, 0f), new Vector2(-48f, 28f), new Vector2(180f, 56f));
            var outline = UiKit.Img(close.transform, "Outline", th.Paper, UiKit.Round8);
            UiKit.Fill(outline.rectTransform);
            var inner = UiKit.Img(outline.transform, "Inner", th.WoodDark, UiKit.Round8);
            UiKit.Fill(inner.rectTransform, 3, 3, 3, 3);
            var cl = UiKit.Text(inner.transform, "Label", th.BodyFont, 28f, th.Paper, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Fill(cl.rectTransform);
            cl.text = closeWord;
            var cb = close.gameObject.AddComponent<Button>();
            cb.navigation = new Navigation { mode = Navigation.Mode.None };
            cb.onClick.AddListener(() => ViewLocator.Get<IMarketCommands>()?.CloseMarket());

            root.SetActive(false);
        }

        private void Hint(RectTransform bar, UiTheme th, PromptAction action, string word)
        {
            var slot = UiKit.Rect(bar, "Hint_" + action);
            var row = slot.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 10f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var key = UiKit.KeyCap(slot, th, 44f, out var keyRoot);
            var w = UiKit.Text(slot, "Word", th.BodyFont, 26f, th.Paper, TextAlignmentOptions.Left);
            w.text = word;
            w.textWrappingMode = TextWrappingModes.NoWrap;
            hints.Add((key, keyRoot, action));
        }
    }
}
