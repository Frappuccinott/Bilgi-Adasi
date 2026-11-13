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

        public void ShowNotice(string text)
        {
            label.text = text;
            band.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(band);
            timer = 0f;
        }

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
            var row = band.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(48, 48, 14, 14);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var fit = band.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            label = UiKit.Text(band, "Text", th.HeadingFont, 36f, th.Paper, TextAlignmentOptions.Center, FontStyles.Italic);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            band.gameObject.SetActive(false);
        }
    }
}
