using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Kısa not şeridi — Figma: Ekran/Oyun içi HUD → "Büyüme bildirimi" (üst ortada, koyu saydam şerit, Alegreya italik).
    /// "Biraz uzadın gibi…", "Envanter dolu", "Kese yetmiyor" gibi notlar. Yumuşak girer/çıkar, zaman ölçeğinden bağımsız.
    /// </summary>
    public sealed class FigmaNoticeView : FigmaView<INoticeView>, INoticeView
    {
        [SerializeField] private float seconds = 3.5f;
        [SerializeField] private float fadeSeconds = 0.25f;
        [Tooltip("Üst kenardan uzaklık (Figma: y 150).")]
        [SerializeField] private float top = 150f;

        private RectTransform band;
        private CanvasGroup group;
        private TextMeshProUGUI label;
        private float timer = -1f;

        private void Awake() => Build();

        [Tooltip("Yazı boyu. Uzun notlar (kilitli dolap) için 36'dan küçültüldü (Play 14.11).")]
        [SerializeField] private float fontSize = 28f;

        private ContentSizeFitter fit;

        public void ShowNotice(string text)
        {
            label.text = text;
            band.gameObject.SetActive(true);
            // Şerit ortada kalır ve sol üstteki görev kartına değmez: en fazla kartın iki yanındaki boşluk kadar geniş;
            // daha uzun not alt satıra geçer (Play 14.11: kilitli dolap notu karta biniyordu).
            float canvasWidth = transform is RectTransform rt && rt.rect.width > 0f ? rt.rect.width : 1920f;
            float maxWidth = Mathf.Max(400f, canvasWidth - 2f * HudLayout.DialogueLeft);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            float pad = row.padding.horizontal;
            bool wide = label.GetPreferredValues(text).x + pad > maxWidth;
            label.textWrappingMode = wide ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            fit.horizontalFit = wide ? ContentSizeFitter.FitMode.Unconstrained : ContentSizeFitter.FitMode.PreferredSize;
            if (wide) band.sizeDelta = new Vector2(maxWidth, band.sizeDelta.y);
            LayoutRebuilder.ForceRebuildLayoutImmediate(band);
            timer = 0f;
        }

        private HorizontalLayoutGroup row;

        private void Update()
        {
            if (timer < 0f) return;
            timer += Time.unscaledDeltaTime;
            float total = seconds + 2f * fadeSeconds;
            if (timer >= total)
            {
                timer = -1f;
                band.gameObject.SetActive(false);
                return;
            }
            float a = timer < fadeSeconds ? timer / fadeSeconds : timer > total - fadeSeconds ? (total - timer) / fadeSeconds : 1f;
            group.alpha = a;
            band.anchoredPosition = new Vector2(0f, -top + (1f - a) * 12f);
        }

        private void Build()
        {
            var th = Theme;
            var bg = UiKit.Img(transform, "Notice", UiKit.WithAlpha(th.InkDeep, 0.78f), UiKit.Round8);
            band = bg.rectTransform;
            UiKit.Place(band, new Vector2(0.5f, 1f), new Vector2(0f, -top), Vector2.zero);
            group = band.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            row = band.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(40, 40, 12, 12);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            fit = band.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            label = UiKit.Text(band, "Text", th.HeadingFont, fontSize, th.Paper, TextAlignmentOptions.Center, FontStyles.Italic);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            band.gameObject.SetActive(false);
        }
    }
}
