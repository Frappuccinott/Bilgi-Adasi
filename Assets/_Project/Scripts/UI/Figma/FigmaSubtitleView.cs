using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Yol sohbeti altyazısı (10-gorev-pazar-npc §6): envanterin üstünde, koyu saydam şerit; konuşanın adı
    /// altın, satır kâğıt renginde (bildirim şeridiyle aynı dil). Tıklanmaz, oyuncuyu durdurmaz.
    /// Zamanlamayı AmbientDialogueTrigger yapar; view yalnızca yumuşak girer/çıkar.
    /// </summary>
    public sealed class FigmaSubtitleView : FigmaView<ISubtitleView>, ISubtitleView
    {
        [Tooltip("Alt kenardan uzaklık: envanter çubuğu ve seçili madde adının üstü.")]
        [SerializeField] private float bottom = 290f;
        [SerializeField] private float width = 1240f;
        [SerializeField] private float fadeSeconds = 0.2f;

        private RectTransform band;
        private CanvasGroup group;
        private TextMeshProUGUI label;
        private string speakerHex;
        private float alpha;
        private bool visible;

        private void Awake() => Build();

        public void ShowSubtitle(string speaker, string text)
        {
            label.text = string.IsNullOrEmpty(speaker)
                ? text
                : "<color=#" + speakerHex + "><b>" + speaker + "</b></color>   " + text;
            band.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(band);
            visible = true;
        }

        public void HideSubtitle() => visible = false;

        private void Update()
        {
            if (!band.gameObject.activeSelf) return;
            float step = fadeSeconds > 0f ? Time.unscaledDeltaTime / fadeSeconds : 1f;
            float next = Mathf.MoveTowards(alpha, visible ? 1f : 0f, step);
            // Geçiş bittiyse her kare aynı değerleri yazma.
            if (next == alpha && group.alpha == alpha) return;
            alpha = next;
            group.alpha = alpha;
            band.anchoredPosition = new Vector2(0f, bottom - (1f - alpha) * 10f);
            if (!visible && alpha <= 0f) band.gameObject.SetActive(false);
        }

        private void Build()
        {
            var th = Theme;
            speakerHex = ColorUtility.ToHtmlStringRGB(th.GoldLight);

            var bg = UiKit.Img(transform, "Subtitle", UiKit.WithAlpha(th.InkDeep, 0.74f), UiKit.Round8);
            band = bg.rectTransform;
            UiKit.Place(band, new Vector2(0.5f, 0f), new Vector2(0f, bottom), new Vector2(width, 0f));
            group = band.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var column = band.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(44, 44, 14, 16);
            column.childAlignment = TextAnchor.MiddleCenter;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            var fit = band.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            label = UiKit.Text(band, "Text", th.BodyFont, 30f, th.Paper, TextAlignmentOptions.Center);
            label.richText = true;

            group.alpha = 0f;
            band.gameObject.SetActive(false);
        }
    }
}
