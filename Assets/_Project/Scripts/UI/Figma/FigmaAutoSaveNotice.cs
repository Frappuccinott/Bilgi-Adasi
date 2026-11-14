using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// "Otomatik kaydedildi…" — marka dili: kesenin altında küçük kâğıt plaka (ahşap çerçeve, mürekkep italik yazı),
    /// solunda dönen küçük mühür. Sağ kenarın dışından kayarak gelir, kısa durur, yine sağa kayarak gider.
    /// Zaman ölçeğinden bağımsız (duraklatmada da biter).
    /// </summary>
    public sealed class FigmaAutoSaveNotice : FigmaView<IAutoSaveNoticeView>, IAutoSaveNoticeView
    {
        [Tooltip("Sağ üstten uzaklık: kese (28, 28, yükseklik 60) ve altındaki akçe farkı (+10) yazısının altında.")]
        [SerializeField] private Vector2 margin = new Vector2(28f, 158f);
        [SerializeField] private float height = 46f;
        [SerializeField] private float slideSeconds = 0.35f;
        [SerializeField] private float holdSeconds = 2.2f;

        private RectTransform plate, seal;
        private TextMeshProUGUI label;
        private float timer = -1f;
        private float width;

        private void Awake() => Build();

        public void ShowSaved(string text)
        {
            label.text = text;
            width = Mathf.Ceil(label.GetPreferredValues(text, 1000f, height).x) + 74f;
            plate.sizeDelta = new Vector2(width, height);
            plate.gameObject.SetActive(true);
            timer = 0f;
            Apply();
        }

        private void Update()
        {
            if (timer < 0f) return;
            timer += Time.unscaledDeltaTime;
            if (timer >= holdSeconds + 2f * slideSeconds)
            {
                timer = -1f;
                plate.gameObject.SetActive(false);
                return;
            }
            Apply();
        }

        private void Apply()
        {
            float total = holdSeconds + 2f * slideSeconds;
            float k = timer < slideSeconds ? timer / slideSeconds
                : timer > total - slideSeconds ? (total - timer) / slideSeconds
                : 1f;
            float eased = 1f - (1f - k) * (1f - k);
            // Kapalıyken tamamen sağ kenarın dışında (gölge dahil), açıkken kenardan margin.x içeride.
            plate.anchoredPosition = new Vector2(Mathf.Lerp(width + 24f, -margin.x, eased), -margin.y);
            seal.localEulerAngles = new Vector3(0f, 0f, -timer * 90f);
        }

        private void Build()
        {
            var th = Theme;
            plate = UiKit.PaperCard(transform, "AutoSave", th, 3f, out var paper, 4f);
            plate.anchorMin = plate.anchorMax = plate.pivot = new Vector2(1f, 1f);
            plate.sizeDelta = new Vector2(240f, height);

            // Solda küçük dönen mühür (kaydediliyor işareti).
            var sealImg = UiKit.Img(paper, "Seal", th.Seal != null ? Color.white : th.WaxMid, th.Seal != null ? th.Seal : UiKit.Disc);
            seal = sealImg.rectTransform;
            seal.anchorMin = seal.anchorMax = new Vector2(0f, 0.5f);
            seal.pivot = new Vector2(0.5f, 0.5f);
            seal.sizeDelta = new Vector2(height - 16f, height - 16f);
            seal.anchoredPosition = new Vector2(10f + (height - 16f) * 0.5f, 0f);
            sealImg.preserveAspect = true;

            label = UiKit.Text(paper, "Label", th.BodyFont, 24f, th.InkDark, TextAlignmentOptions.MidlineLeft, FontStyles.Italic);
            UiKit.Fill(label.rectTransform, height + 4f, 0f, 10f, 0f);
            label.textWrappingMode = TextWrappingModes.NoWrap;

            plate.gameObject.SetActive(false);
        }
    }
}
