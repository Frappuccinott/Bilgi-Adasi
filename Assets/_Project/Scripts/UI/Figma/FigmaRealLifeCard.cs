using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// "Gerçek hayatta" kartı — Figma: Ekran/Diyalog → "Gerçek hayatta" (sağ üst, 587 px kâğıt kart, mürekkep çerçeve;
    /// başlık su rengi, gövde mürekkep). Güvenlik notu korkutmadan, tek cümle.
    /// </summary>
    public sealed class FigmaRealLifeCard : FigmaView<IRealLifeCardView>, IRealLifeCardView
    {
        [SerializeField] private string title = "Gerçek hayatta";
        [SerializeField] private float width = 587f;
        [SerializeField] private Vector2 margin = new Vector2(72f, 72f);

        private RectTransform card;
        private TextMeshProUGUI body;

        private void Awake() => Build();

        public void Show(string text)
        {
            body.text = text;
            card.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(card);
        }

        public void Hide()
        {
            if (card != null) card.gameObject.SetActive(false);
        }

        private void Build()
        {
            var th = Theme;
            card = UiKit.PaperCard(transform, "RealLifeCard", th, 4f, out var paper);
            UiKit.Place(card, new Vector2(1f, 1f), new Vector2(-margin.x, -margin.y), new Vector2(width, 0f));
            // Kart yüksekliği içeriğe göre: kâğıdın içindeki dikey düzen kökü büyütür.
            var col = paper.gameObject.AddComponent<VerticalLayoutGroup>();
            col.padding = new RectOffset(30, 30, 22, 26);
            col.spacing = 10f;
            col.childControlWidth = col.childControlHeight = true;
            col.childForceExpandWidth = true;
            col.childForceExpandHeight = false;

            var head = UiKit.Text(paper, "Title", th.BodyFont, 28f, th.WaterDark, TextAlignmentOptions.Left, FontStyles.Bold);
            head.text = title;
            body = UiKit.Text(paper, "Body", th.BodyFont, 30f, th.Ink, TextAlignmentOptions.TopLeft);

            var sizer = card.gameObject.AddComponent<CardSizer>();
            sizer.Init(paper, 4f);
            card.gameObject.SetActive(false);
        }
    }

    /// <summary>Kâğıt kartın yüksekliğini iç düzenin tercih ettiği yüksekliğe eşitler (genişlik sabit).</summary>
    [ExecuteAlways]
    public sealed class CardSizer : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private float border;

        public void Init(RectTransform inner, float frame)
        {
            content = inner;
            border = frame;
        }

        private void LateUpdate()
        {
            if (content == null) return;
            float h = LayoutUtility.GetPreferredHeight(content) + 2f * border;
            var rt = (RectTransform)transform;
            if (Mathf.Abs(rt.sizeDelta.y - h) > 0.5f) rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
        }
    }
}
