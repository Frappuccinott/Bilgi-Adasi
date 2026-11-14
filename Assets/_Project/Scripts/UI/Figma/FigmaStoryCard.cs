using System.Collections;
using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Hikâye kartı + açılış sinematiği — marka dili (Figma Ekran/Giriş): kutu ve çerçeve yok; arkada oyunun kendi
    /// dünyası, solda mürekkep gölgesi, sol altta altın italik yer adı + turuncu çizgi + Alegreya cümle.
    /// Göz kapakları: üst ve alt mürekkep perdeler, iç kenarları yumuşak. Kapalıyken ekran karanlık (sınıf kartları
    /// ortada italik); açılınca sinema şeridinde durur. Şeritte solda kare ilerlemesi, sağda tuş kapakları.
    /// HUD'da kendi Canvas sırasıyla en üstte. Zaman ölçeğinden bağımsız.
    /// </summary>
    public sealed class FigmaStoryCard : FigmaView<IStoryCardView>, IStoryCardView
    {
        [SerializeField] private float textFadeSeconds = 0.4f;
        [Tooltip("HUD Canvas'ının üstünde çizilme sırası (HUD 10).")]
        [SerializeField] private int sortingOrder = 100;

        // 1920×1080 referansı: sinema şeridi, kapak kenar yumuşaklığı, yazı sol kenarı, ilerleme parçası.
        private const float Letterbox = 92f, Soft = 150f, Left = 120f, Rise = 16f;

        private RectTransform rootRt, lidTop, lidBottom, softTop, softBottom, cardText, caption, nextKeyRoot, skipKeyRoot;
        private CanvasGroup group, cardGroup, captionGroup, shadeGroup, barGroup;
        private TextMeshProUGUI pageLabel, cardLabel, kicker, headline, nextKey, nextWord, skipKey, skipWord;
        private float lids;          // 0 kapalı · 1 şerit · >1 tamamen açık
        private int step = -1, stepCount;
        private bool shown;
        private Coroutine textRoutine, lidRoutine;

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
            SetShown(false);
        }

        private void OnScheme(ControlScheme _) => UpdateKeys();

        // ---------- IStoryCardView ----------

        public void BeginSequence(int count, string next, string skip)
        {
            step = -1;
            stepCount = Mathf.Max(0, count);
            nextWord.text = next ?? string.Empty;
            skipWord.text = skip ?? string.Empty;
            pageLabel.text = string.Empty;
            barGroup.alpha = stepCount > 0 ? 1f : 0f;
            UpdateKeys();
        }

        public void ShowImmediate(string text)
        {
            Open();
            StopText();
            Advance();
            SetLidsNow(0f);
            captionGroup.alpha = 0f;
            shadeGroup.alpha = 0f;
            cardLabel.text = text;
            cardGroup.alpha = 1f;
            cardText.anchoredPosition = Vector2.zero;
        }

        public void Show(string text)
        {
            Open();
            Advance();
            StopText();
            textRoutine = StartCoroutine(CardRoutine(text));
        }

        public void ShowShot(string kickerText, string text)
        {
            Open();
            Advance();
            StopText();
            textRoutine = StartCoroutine(ShotRoutine(kickerText, text));
        }

        public void SetLids(float open, float seconds)
        {
            Open();
            if (lidRoutine != null) StopCoroutine(lidRoutine);
            lidRoutine = StartCoroutine(LidRoutine(open, seconds));
        }

        public void Hide()
        {
            if (!isActiveAndEnabled || !shown) return;
            StopText();
            if (lidRoutine != null) StopCoroutine(lidRoutine);
            lidRoutine = StartCoroutine(HideRoutine());
        }

        // ---------- Akış ----------

        private void Open()
        {
            gameObject.SetActive(true);
            group.alpha = 1f;
            group.blocksRaycasts = true;
            SetShown(true);
        }

        private void SetShown(bool value)
        {
            if (shown == value) return;
            shown = value;
            FigmaDialogueView.Hold(value);   // mini harita, defter sırtı, envanter çekilir
        }

        private void StopText()
        {
            if (textRoutine != null) StopCoroutine(textRoutine);
            textRoutine = null;
        }

        private void Advance()
        {
            if (stepCount <= 0) return;
            step = Mathf.Min(step + 1, stepCount - 1);
            var th = Theme;
            // Kitap sayfası gibi: "3 / 10" — o anki sayfa altın, toplam soluk (Ders Notu altbilgisiyle aynı dil).
            pageLabel.text = "<color=#" + ColorUtility.ToHtmlStringRGB(th.GoldLight) + ">" + (step + 1) + "</color>  /  " + stepCount;
        }

        private IEnumerator CardRoutine(string text)
        {
            if (captionGroup.alpha > 0f || shadeGroup.alpha > 0f)
            {
                var a = Fade(captionGroup, 0f, textFadeSeconds);
                while (a.MoveNext()) { shadeGroup.alpha = captionGroup.alpha; yield return a.Current; }
            }
            if (cardGroup.alpha > 0f) yield return Fade(cardGroup, 0f, textFadeSeconds);
            cardLabel.text = text;
            yield return RiseIn(cardGroup, cardText, Vector2.zero, textFadeSeconds * 1.5f);
            textRoutine = null;
        }

        private IEnumerator ShotRoutine(string kickerText, string text)
        {
            if (cardGroup.alpha > 0f) yield return Fade(cardGroup, 0f, textFadeSeconds);
            if (captionGroup.alpha > 0f) yield return Fade(captionGroup, 0f, textFadeSeconds * 0.6f);
            kicker.text = kickerText ?? string.Empty;
            headline.text = text ?? string.Empty;
            shadeGroup.alpha = 1f;
            yield return RiseIn(captionGroup, caption, new Vector2(Left, Letterbox + 56f), textFadeSeconds * 1.6f);
            textRoutine = null;
        }

        /// <summary>Yazı aşağıdan süzülerek ve belirerek yerine oturur.</summary>
        private static IEnumerator RiseIn(CanvasGroup g, RectTransform rt, Vector2 target, float duration)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float e = Mathf.SmoothStep(0f, 1f, t / duration);
                g.alpha = e;
                rt.anchoredPosition = target + new Vector2(0f, -Rise * (1f - e));
                yield return null;
            }
            g.alpha = 1f;
            rt.anchoredPosition = target;
        }

        private IEnumerator LidRoutine(float target, float duration)
        {
            float start = lids;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                // Göz açılışı: önce yavaş aralanır, sonra açılır (ease-in-out).
                SetLidsNow(Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / duration)));
                // Göz kapanırken karenin yazısı da söner (kesmede eski cümle karanlıkta asılı kalmasın).
                if (target < start) captionGroup.alpha = shadeGroup.alpha = Mathf.Min(captionGroup.alpha, Mathf.Clamp01(lids));
                yield return null;
            }
            SetLidsNow(target);
            lidRoutine = null;
        }

        private IEnumerator HideRoutine()
        {
            float d = 0.7f, start = lids, cg = cardGroup.alpha, cp = captionGroup.alpha;
            for (float t = 0f; t < d; t += Time.unscaledDeltaTime)
            {
                float e = Mathf.SmoothStep(0f, 1f, t / d);
                SetLidsNow(Mathf.Lerp(start, 2f, e));
                cardGroup.alpha = Mathf.Lerp(cg, 0f, e);
                captionGroup.alpha = shadeGroup.alpha = Mathf.Lerp(cp, 0f, e);
                barGroup.alpha = Mathf.Min(barGroup.alpha, 1f - e);
                yield return null;
            }
            SetLidsNow(2f);
            cardGroup.alpha = captionGroup.alpha = shadeGroup.alpha = barGroup.alpha = 0f;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            lidRoutine = null;
            SetShown(false);
        }

        /// <summary>0 kapalı → 1 şerit → 2 tamamen açık (perdeler ekran dışına çekilir).</summary>
        private void SetLidsNow(float value)
        {
            lids = value;
            float h = rootRt.rect.height > 1f ? rootRt.rect.height : 1080f;
            float strip = Letterbox / 1080f * h;
            float half = h * 0.5f + Soft;               // kapalıyken yumuşak kenarlar da üst üste biner
            float size = value <= 1f ? Mathf.Lerp(half, strip, value) : Mathf.Lerp(strip, -Soft, value - 1f);
            lidTop.sizeDelta = new Vector2(0f, Mathf.Max(0f, size));
            lidBottom.sizeDelta = new Vector2(0f, Mathf.Max(0f, size));
            // Yumuşak kenar: perde kenarından içeri doğru; tam açılırken o da çekilir.
            float soft = Mathf.Clamp(size + Soft, 0f, Soft);
            softTop.sizeDelta = new Vector2(0f, soft);
            softBottom.sizeDelta = new Vector2(0f, soft);
            softTop.anchoredPosition = new Vector2(0f, -Mathf.Max(0f, size));
            softBottom.anchoredPosition = new Vector2(0f, Mathf.Max(0f, size));
        }

        private static IEnumerator Fade(CanvasGroup g, float target, float duration)
        {
            float start = g.alpha;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                g.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            g.alpha = target;
        }

        private void UpdateKeys()
        {
            if (nextKey == null) return;
            UiKit.SetKeyCap(nextKey, nextKeyRoot, ControlGlyphs.For(PromptAction.Interact), 34f);
            UiKit.SetKeyCap(skipKey, skipKeyRoot, ControlGlyphs.For(PromptAction.Pause), 34f);
        }

        // ---------- Kurulum ----------

        private void Build()
        {
            var th = Theme;
            rootRt = UiKit.Fill(UiKit.Rect(transform, "StoryCard"));
            var canvas = rootRt.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
            rootRt.gameObject.AddComponent<GraphicRaycaster>();
            group = rootRt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            // Sol gölge (Ekran/Giriş'teki gibi): yazı oyun görüntüsünün üstünde okunur.
            var shade = UiKit.Img(rootRt, "LeftShade", th.InkDeep,
                UiKit.Gradient(false, new Vector2(0f, 0.78f), new Vector2(0.35f, 0.45f), new Vector2(0.62f, 0f)));
            UiKit.Fill(shade.rectTransform);
            shadeGroup = shade.gameObject.AddComponent<CanvasGroup>();
            shadeGroup.alpha = 0f;

            // Göz kapakları
            lidTop = Lid(rootRt, th, true, out softTop);
            lidBottom = Lid(rootRt, th, false, out softBottom);

            // Kart yazısı (kapaklar kapalıyken ortada)
            cardText = UiKit.Rect(rootRt, "CardText");
            UiKit.Place(cardText, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240f, 220f));
            cardGroup = cardText.gameObject.AddComponent<CanvasGroup>();
            cardGroup.alpha = 0f;
            cardLabel = UiKit.Text(cardText, "Label", th.HeadingItalicFont, 50f, th.Paper, TextAlignmentOptions.Center,
                th.HasHeadingItalic ? FontStyles.Normal : FontStyles.Italic);
            cardLabel.lineSpacing = 8f;
            UiKit.Fill(cardLabel.rectTransform);

            // Kare yazısı (sol alt, şeridin üstünde)
            caption = UiKit.Rect(rootRt, "Caption");
            caption.anchorMin = caption.anchorMax = caption.pivot = Vector2.zero;
            caption.sizeDelta = new Vector2(1120f, 0f);
            caption.anchoredPosition = new Vector2(Left, Letterbox + 56f);
            captionGroup = caption.gameObject.AddComponent<CanvasGroup>();
            captionGroup.alpha = 0f;
            var col = caption.gameObject.AddComponent<VerticalLayoutGroup>();
            col.spacing = 14f;
            col.childAlignment = TextAnchor.LowerLeft;
            col.childControlWidth = col.childControlHeight = true;
            col.childForceExpandWidth = true;
            col.childForceExpandHeight = false;
            var fit = caption.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Yer adı: ana menüdeki köy satırı gibi Alegreya italik (büyük harf + açık aralık marka dilinde yok).
            kicker = UiKit.Text(caption, "Kicker", th.HeadingItalicFont, 32f, th.GoldLight, TextAlignmentOptions.Left,
                th.HasHeadingItalic ? FontStyles.Normal : FontStyles.Italic);
            kicker.textWrappingMode = TextWrappingModes.NoWrap;
            // Logo başlığının altındaki gibi kısa mühür çizgisi
            var ruleRow = UiKit.Rect(caption, "Rule");
            ruleRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 4f;
            var rule = UiKit.Img(ruleRow, "Line", th.Wax);
            UiKit.Place(rule.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(72f, 4f));
            headline = UiKit.Text(caption, "Headline", th.HeadingFont, 52f, th.Paper, TextAlignmentOptions.BottomLeft);
            headline.lineSpacing = -4f;
            foreach (var t in new[] { kicker, headline })
            {
                var sh = t.gameObject.AddComponent<Shadow>();
                sh.effectColor = UiKit.WithAlpha(th.InkDeep, 0.7f);
                sh.effectDistance = new Vector2(0f, -3f);
            }

            // Şerit: solda kare ilerlemesi, sağda tuşlar
            var bar = UiKit.Rect(rootRt, "Bar");
            bar.anchorMin = Vector2.zero;
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.offsetMin = Vector2.zero;
            bar.offsetMax = new Vector2(0f, Letterbox);
            barGroup = bar.gameObject.AddComponent<CanvasGroup>();
            barGroup.alpha = 0f;

            pageLabel = UiKit.Text(bar, "Page", th.HeadingItalicFont, 30f, UiKit.WithAlpha(th.Paper, 0.7f), TextAlignmentOptions.Left,
                th.HasHeadingItalic ? FontStyles.Normal : FontStyles.Italic);
            pageLabel.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Place(pageLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(Left, 0f), new Vector2(300f, 40f));

            var hints = UiKit.Rect(bar, "Hints");
            UiKit.Place(hints, new Vector2(1f, 0.5f), new Vector2(-Left, 0f), new Vector2(0f, 34f));
            var row = hints.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 10f;
            row.childAlignment = TextAnchor.MiddleRight;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            hints.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            nextKey = UiKit.KeyCap(hints, th, 34f, out nextKeyRoot);
            nextWord = HintWord(hints, th);
            var gap = UiKit.Rect(hints, "Gap");
            gap.gameObject.AddComponent<LayoutElement>().preferredWidth = 22f;
            skipKey = UiKit.KeyCap(hints, th, 34f, out skipKeyRoot);
            skipWord = HintWord(hints, th);

            SetLidsNow(2f);
            UpdateKeys();
        }

        private static TextMeshProUGUI HintWord(RectTransform parent, UiTheme th)
        {
            var w = UiKit.Text(parent, "Word", th.BodyFont, 24f, UiKit.WithAlpha(th.Paper, 0.8f), TextAlignmentOptions.Left);
            w.textWrappingMode = TextWrappingModes.NoWrap;
            return w;
        }

        /// <summary>Perde (üst ya da alt) + iç kenarında yumuşak geçiş.</summary>
        private static RectTransform Lid(RectTransform parent, UiTheme th, bool top, out RectTransform soft)
        {
            var lid = UiKit.Img(parent, top ? "LidTop" : "LidBottom", th.InkDeep, null, true).rectTransform;
            lid.anchorMin = new Vector2(0f, top ? 1f : 0f);
            lid.anchorMax = new Vector2(1f, top ? 1f : 0f);
            lid.pivot = new Vector2(0.5f, top ? 1f : 0f);
            lid.anchoredPosition = Vector2.zero;

            // Yumuşak kenar perdenin kardeşi: perdenin iç kenarından ekranın ortasına doğru söner.
            var s = UiKit.Img(parent, top ? "SoftTop" : "SoftBottom", th.InkDeep,
                UiKit.Gradient(true, top ? new Vector2(0f, 1f) : new Vector2(0f, 0f), top ? new Vector2(1f, 0f) : new Vector2(1f, 1f)));
            soft = s.rectTransform;
            soft.anchorMin = new Vector2(0f, top ? 1f : 0f);
            soft.anchorMax = new Vector2(1f, top ? 1f : 0f);
            soft.pivot = new Vector2(0.5f, top ? 1f : 0f);
            return lid;
        }
    }
}
