using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Crosshair + etkileşim ipucu — Figma: Ekranlar (v2) / Ekran/Oyun içi HUD → "Crosshair" (8 px kâğıt nokta) ve
    /// "Etkileşim ipucu" (alt ortada koyu mürekkep kutu: tuş kapağı + eylem). Tuş kapağı son cihaza göre değişir.
    /// HUD Canvas'ı altına tam ekran konur.
    /// </summary>
    public sealed class FigmaInteractPrompt : FigmaView<IInteractPromptView>, IInteractPromptView
    {
        [SerializeField] private float crosshairSize = 8f;
        [Tooltip("İpucunun alt kenardan uzaklığı: envanter + seçili madde adının (alttan ~165) üstünde.")]
        [SerializeField] private float bottom = 196f;
        [SerializeField] private float keyHeight = 44f;

        private GameObject crosshair;
        private RectTransform box, keyRoot;
        private TextMeshProUGUI keyLabel, actionLabel;
        private string lastText, lastGlyph;

        private void Awake() => Build();

        protected override void OnEnable()
        {
            base.OnEnable();
            if (box != null) box.gameObject.SetActive(false);
        }

        public void ShowPrompt(string text, string glyph)
        {
            // Aynı ipucu zaten açıksa yeniden kurma: her kare TMP + layout rebuild kasma yaratıyordu.
            if (box.gameObject.activeSelf && text == lastText && glyph == lastGlyph) return;
            lastText = text;
            lastGlyph = glyph;
            actionLabel.text = text;
            UiKit.SetKeyCap(keyLabel, keyRoot, glyph, keyHeight);
            box.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(box);
        }

        public void HidePrompt()
        {
            if (box != null) box.gameObject.SetActive(false);
        }

        public void SetCrosshairVisible(bool visible)
        {
            if (crosshair != null) crosshair.SetActive(visible);
        }

        private void Build()
        {
            var th = Theme;
            var dot = UiKit.Img(transform, "Crosshair", th.Paper, UiKit.Disc);
            UiKit.Place(dot.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(crosshairSize, crosshairSize));
            var outline = dot.gameObject.AddComponent<Outline>();
            outline.effectColor = UiKit.WithAlpha(th.InkDeep, 0.5f);
            outline.effectDistance = new Vector2(1f, -1f);
            crosshair = dot.gameObject;

            var bg = UiKit.Img(transform, "InteractPrompt", UiKit.WithAlpha(th.InkDeep, 0.82f), UiKit.Round8);
            box = bg.rectTransform;
            UiKit.Place(box, new Vector2(0.5f, 0f), new Vector2(0f, bottom), Vector2.zero);
            var row = box.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(20, 24, 12, 12);
            row.spacing = 12f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var fit = box.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            keyLabel = UiKit.KeyCap(box, th, keyHeight, out keyRoot);
            actionLabel = UiKit.Text(box, "Action", th.BodyFont, 28f, th.Paper, TextAlignmentOptions.Left, FontStyles.Bold);
            actionLabel.textWrappingMode = TextWrappingModes.NoWrap;

            box.gameObject.SetActive(false);
        }
    }
}
