using System.Collections.Generic;
using System.Text;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using BilgiAdasi.Order;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Sipariş kartı — Figma: Marka Kimliği / Arayüz dili → UI/Sipariş kartı (kâğıt, ahşap çerçeve, sağ üstte mum
    /// mühür; sipariş veren Alegreya). Ortada adım rehberi (karar 14.11): biten adım soluk + mürekkep tik, sıradaki
    /// adım altın şeritte numaralı altın mühür + kısa açıklama, bekleyenler numaralı halka. En altta her zaman
    /// "İstenen" (büyük formül), malzemeler ve ödeme; hazırsa altın "kapıya götür" şeridi. HUD'da sol üstte.
    /// </summary>
    public sealed class FigmaOrderCard : FigmaView<IOrderCardView>, IOrderCardView
    {
        [Header("Etiketler")]
        [SerializeField] private string stepWord = "Aşama";
        [SerializeField] private string wantedWord = "İstenen";
        [SerializeField] private string ingredientsWord = "Malzemeler";
        [SerializeField] private string rewardWord = "Ödeme";
        [SerializeField] private string currencyWord = "akçe";
        [SerializeField] private string readyWord = "Hazır, kapıya götür";

        [Header("Yerleşim")]
        [SerializeField] private float width = HudLayout.OrderCardWidth;   // diyalog bu genişliğin sağından başlar
        [SerializeField] private Vector2 margin = new Vector2(28f, 28f);

        private const int MaxGuide = 6;

        private RectTransform card;
        private TextMeshProUGUI giver, sub, formula, ingredients, reward, ready;
        private GameObject readyStrip, guideRoot;
        private readonly GuideRow[] rows = new GuideRow[MaxGuide];
        private readonly StringBuilder sb = new StringBuilder(128);

        private sealed class GuideRow
        {
            public GameObject Root;
            public Image Band, Marker, Tick;
            public TextMeshProUGUI Number, Text, Detail;
        }

        [Tooltip("Oyuncu modu dışında (pazar, Esc menüsü, harita, masa) kart solup gizlenir (mini harita gibi).")]
        [SerializeField] private float fadeSeconds = 0.15f;

        private CanvasGroup group;
        private float fadeTarget = 1f;

        private void Awake() => Build();

        protected override void OnEnable()
        {
            base.OnEnable();
            GameInput.ModeChanged += OnModeChanged;
            OnModeChanged(GameInput.Mode);
            if (group != null) group.alpha = fadeTarget;
        }

        protected override void OnDisable()
        {
            GameInput.ModeChanged -= OnModeChanged;
            base.OnDisable();
        }

        // Mini harita gibi yalnızca oyuncu modunda: pazar / Esc / harita (UI) ve masa / tahta (istasyon) ekranıyla çakışmaz.
        private void OnModeChanged(InputMode mode) => fadeTarget = mode == InputMode.Player ? 1f : 0f;

        private void Update()
        {
            if (group == null || Mathf.Approximately(group.alpha, fadeTarget)) return;
            group.alpha = Mathf.MoveTowards(group.alpha, fadeTarget, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds));
        }

        public void ShowOrder(OrderCardInfo info)
        {
            giver.text = info.GiverName;
            sb.Clear();
            if (info.StepCount > 1) sb.Append(stepWord).Append(' ').Append(info.StepIndex + 1).Append('/').Append(info.StepCount);
            sub.text = sb.ToString();
            sub.gameObject.SetActive(sb.Length > 0);

            ShowGuide(info.Guide);

            sb.Clear();
            if (!string.IsNullOrEmpty(info.WantedText)) sb.Append(info.WantedText);
            else AppendAmounts(info.Products);
            formula.text = sb.ToString();

            if (info.Reactants != null && info.Reactants.Count > 0)
            {
                sb.Clear().Append(ingredientsWord).Append(": ");
                for (int i = 0; i < info.Reactants.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(info.Reactants[i].FormulaRich);
                }
                ingredients.text = sb.ToString();
                ingredients.gameObject.SetActive(true);
            }
            else ingredients.gameObject.SetActive(false);

            sb.Clear().Append(rewardWord).Append(": <b>").Append(info.Reward).Append("</b> ").Append(currencyWord);
            reward.text = sb.ToString();
            reward.gameObject.SetActive(info.Reward > 0); // masa mektubunda ödeme yok
            readyStrip.SetActive(info.ReadyToDeliver);
            card.gameObject.SetActive(true);
        }

        public void ClearOrder()
        {
            if (card != null) card.gameObject.SetActive(false);
        }

        private void ShowGuide(IReadOnlyList<OrderGuideStep> guide)
        {
            int count = guide != null ? Mathf.Min(guide.Count, MaxGuide) : 0;
            guideRoot.SetActive(count > 0);
            var th = Theme;
            for (int i = 0; i < MaxGuide; i++)
            {
                var row = rows[i];
                bool on = i < count;
                row.Root.SetActive(on);
                if (!on) continue;
                var step = guide[i];
                row.Text.text = step.Text;
                row.Number.text = (i + 1).ToString();
                bool hasDetail = step.State == GuideState.Current && !string.IsNullOrEmpty(step.Detail);
                row.Detail.gameObject.SetActive(hasDetail);
                if (hasDetail) row.Detail.text = step.Detail;

                switch (step.State)
                {
                    case GuideState.Done:
                        row.Band.enabled = false;
                        row.Marker.sprite = UiKit.Disc;
                        row.Marker.color = UiKit.WithAlpha(th.PaperShade, 0.35f);
                        row.Tick.enabled = true;
                        row.Number.enabled = false;
                        row.Text.color = th.PaperShade;
                        row.Text.fontStyle = FontStyles.Normal;
                        break;
                    case GuideState.Current:
                        row.Band.enabled = true;
                        row.Marker.sprite = th.SealWax != null ? th.SealWax : UiKit.Disc;
                        row.Marker.color = th.SealWax != null ? Color.white : th.GoldDark;
                        row.Tick.enabled = false;
                        row.Number.enabled = true;
                        row.Number.color = th.Paper;
                        row.Text.color = th.InkDeep;
                        row.Text.fontStyle = FontStyles.Bold;
                        break;
                    default:
                        row.Band.enabled = false;
                        row.Marker.sprite = UiKit.Ring;
                        row.Marker.color = UiKit.WithAlpha(th.PaperShade, 0.8f);
                        row.Tick.enabled = false;
                        row.Number.enabled = true;
                        row.Number.color = th.PaperShade;
                        row.Text.color = UiKit.WithAlpha(th.Ink, 0.75f);
                        row.Text.fontStyle = FontStyles.Normal;
                        break;
                }
            }
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

        // ---------- Kurulum ----------

        private void Build()
        {
            var th = Theme;
            card = UiKit.PaperCard(transform, "OrderCard", th, 6f, out var paper, 6f);
            UiKit.Place(card, new Vector2(0f, 1f), new Vector2(margin.x, -margin.y), new Vector2(width, 0f));

            var col = paper.gameObject.AddComponent<VerticalLayoutGroup>();
            col.padding = new RectOffset(22, 22, 16, 18);
            col.spacing = 2f;
            col.childControlWidth = col.childControlHeight = true;
            col.childForceExpandWidth = true;
            col.childForceExpandHeight = false;

            // Üst: sipariş veren (mühre yer bırak) + aşama
            giver = UiKit.Text(paper, "Giver", th.HeadingFont, 30f, th.Ink, TextAlignmentOptions.Left, FontStyles.Bold);
            giver.margin = new Vector4(0f, 0f, 52f, 0f);
            sub = UiKit.Text(paper, "Step", th.BodyFont, 22f, th.PaperShade);
            Rule(paper, th, 8f);

            // Orta: adım rehberi
            var g = UiKit.Rect(paper, "Guide");
            guideRoot = g.gameObject;
            var gcol = guideRoot.AddComponent<VerticalLayoutGroup>();
            gcol.spacing = 2f;
            gcol.childControlWidth = gcol.childControlHeight = true;
            gcol.childForceExpandWidth = true;
            gcol.childForceExpandHeight = false;
            for (int i = 0; i < MaxGuide; i++) rows[i] = MakeRow(g, th, i);
            Rule(paper, th, 8f);

            // Alt: her zaman İstenen
            var wanted = UiKit.Text(paper, "Wanted", th.BodyFont, 22f, th.PaperShade);
            wanted.text = wantedWord;
            formula = UiKit.Text(paper, "Formula", th.BodyFont, 40f, th.Ink, TextAlignmentOptions.Left, FontStyles.Bold);
            ingredients = UiKit.Text(paper, "Ingredients", th.BodyFont, 22f, th.Ink);
            reward = UiKit.Text(paper, "Reward", th.BodyFont, 22f, th.PaperShade);

            // Hazır şeridi: birincil altın düğme dili (Figma "Teslim et")
            var strip = UiKit.Img(paper, "Ready", th.Gold, UiKit.Round4);
            readyStrip = strip.gameObject;
            var stripLe = readyStrip.AddComponent<LayoutElement>();
            stripLe.preferredHeight = 38f;
            ready = UiKit.Text(strip.transform, "Label", th.BodyFont, 22f, th.InkDeep, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Fill(ready.rectTransform, 12, 0, 12, 0);
            ready.text = readyWord;

            // Mühür sağ üstte, kenardan taşar (Figma'daki gibi)
            var seal = UiKit.Img(card, "Seal", th.Seal != null ? Color.white : th.WaxMid, th.Seal != null ? th.Seal : UiKit.Disc);
            UiKit.Place(seal.rectTransform, new Vector2(1f, 1f), new Vector2(-8f, 14f), new Vector2(68f, 68f));
            seal.preserveAspect = true;

            card.gameObject.AddComponent<CardSizer>().Init(paper, 6f);
            group = card.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            group.alpha = fadeTarget;
            card.gameObject.SetActive(false);
        }

        private static void Rule(RectTransform parent, UiTheme th, float space)
        {
            var holder = UiKit.Rect(parent, "RuleSpace");
            holder.gameObject.AddComponent<LayoutElement>().preferredHeight = space * 2f + 2f;
            var rule = UiKit.Img(holder, "Rule", UiKit.WithAlpha(th.PaperShade, 0.7f));
            var rt = rule.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(0f, 2f);
            rt.anchoredPosition = Vector2.zero;
        }

        private static GuideRow MakeRow(RectTransform parent, UiTheme th, int index)
        {
            var r = new GuideRow();
            var rt = UiKit.Rect(parent, "Step_" + (index + 1));
            r.Root = rt.gameObject;
            var h = r.Root.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(6, 8, 4, 4);
            h.spacing = 10f;
            h.childAlignment = TextAnchor.UpperLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            // Sıradaki adımın altın şeridi (satırın arkasında, düzene katılmaz)
            r.Band = UiKit.Img(rt, "Band", UiKit.WithAlpha(th.GoldLight, 0.28f), UiKit.Round4);
            r.Band.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiKit.Fill(r.Band.rectTransform, -6f, 0f, -6f, 0f);
            r.Band.rectTransform.SetAsFirstSibling();

            var markerRt = UiKit.Rect(rt, "Marker");
            var mle = markerRt.gameObject.AddComponent<LayoutElement>();
            mle.preferredWidth = mle.minWidth = 28f;
            mle.preferredHeight = mle.minHeight = 28f;
            r.Marker = markerRt.gameObject.AddComponent<Image>();
            r.Marker.preserveAspect = true;
            r.Number = UiKit.Text(markerRt, "N", th.HeadingFont, 17f, th.Paper, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Fill(r.Number.rectTransform, 0f, 0f, 0f, 1f);
            r.Tick = UiKit.Img(markerRt, "Tick", th.InkDark, UiKit.Check);
            UiKit.Fill(r.Tick.rectTransform, 5f, 5f, 5f, 5f);
            r.Tick.preserveAspect = true;

            var textCol = UiKit.Rect(rt, "Texts");
            var v = textCol.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 0f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            textCol.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            r.Text = UiKit.Text(textCol, "Text", th.BodyFont, 23f, th.Ink, TextAlignmentOptions.Left);
            r.Detail = UiKit.Text(textCol, "Detail", th.BodyFont, 18f, th.Ink, TextAlignmentOptions.Left, FontStyles.Italic);
            r.Detail.color = UiKit.WithAlpha(th.InkDark, 0.85f);
            return r;
        }
    }
}
