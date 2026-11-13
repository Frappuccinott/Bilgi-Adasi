using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Ustanın defteri (IHintBookView) — marka dili: sipariş kartının sağında küçük ahşap rozet (mühür + "Ustanın
    /// defteri" + tuş kapağı) ve sipariş kartının altında açılan kâğıt not. Rozet yalnızca defter parlarken görünür
    /// (2 köpürmeden sonra) ve altın çerçevesi nefes alır; not açıkken rozet söner.
    /// </summary>
    public sealed class FigmaHintBook : FigmaView<IHintBookView>, IHintBookView
    {
        [Header("Etiketler")]
        [SerializeField] private string badgeWord = "Ustanın defteri";
        [SerializeField] private string noteTitle = "Ustanın notu";
        [SerializeField] private string closeWord = "Kapat";

        [Header("Yerleşim (sipariş kartı: sol üst, 360 px)")]
        [SerializeField] private Vector2 badgePos = new Vector2(404f, -28f);
        [SerializeField] private Vector2 notePos = new Vector2(28f, -300f);
        [SerializeField] private float noteWidth = 460f;
        [SerializeField] private float pulseSpeed = 3.2f;

        private GameObject badge;
        private Image badgeGlow;
        private TextMeshProUGUI badgeKey, noteKey, noteBody;
        private RectTransform badgeKeyRoot, noteKeyRoot, note;
        private bool glowing;

        public bool IsHintOpen => note != null && note.gameObject.activeSelf;

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

        public void SetGlowing(bool value)
        {
            glowing = value;
            Refresh();
        }

        public void ShowHint(string text)
        {
            noteBody.text = text;
            note.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(note);
            Refresh();
        }

        public void HideHint()
        {
            if (note != null) note.gameObject.SetActive(false);
            Refresh();
        }

        private void Update()
        {
            if (!badge.activeSelf) return;
            float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed);
            badgeGlow.color = UiKit.WithAlpha(Theme.Gold, 0.35f + 0.65f * k);
        }

        private void Refresh()
        {
            if (badge == null) return;
            badge.SetActive(glowing && !IsHintOpen);
            OnScheme(GameInput.LastScheme);
        }

        private void OnScheme(ControlScheme _)
        {
            if (badgeKey == null) return;
            string glyph = ControlGlyphs.For(PromptAction.WorkshopHint);
            UiKit.SetKeyCap(badgeKey, badgeKeyRoot, glyph, 34f);
            UiKit.SetKeyCap(noteKey, noteKeyRoot, glyph, 34f);
        }

        private void Build()
        {
            var th = Theme;
            var root = UiKit.Fill(UiKit.Rect(transform, "HintBook"));

            // Rozet: altın çerçeve (nabız) + ahşap zemin + mühür + yazı + tuş kapağı
            var b = UiKit.Rect(root, "Badge");
            UiKit.Place(b, new Vector2(0f, 1f), badgePos, new Vector2(0f, 64f));
            badge = b.gameObject;
            badgeGlow = UiKit.Img(b, "Glow", th.Gold, UiKit.Round8);
            UiKit.Fill(badgeGlow.rectTransform, -4, -4, -4, -4);
            var plate = UiKit.Img(b, "Plate", th.WoodDark, UiKit.Round8);
            UiKit.Fill(plate.rectTransform);
            var row = b.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(12, 14, 8, 8);
            row.spacing = 10f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var fit = b.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            // Düzen grubu Glow/Plate'i de dizer; ikisini düzen dışı bırak.
            foreach (var img in new[] { badgeGlow.gameObject, plate.gameObject })
                img.AddComponent<LayoutElement>().ignoreLayout = true;
            var seal = UiKit.Img(b, "Seal", Color.white, th.Seal);
            seal.preserveAspect = true;
            var le = seal.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = 44f;
            var word = UiKit.Text(b, "Word", th.BodyFont, 26f, th.Paper, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            word.text = badgeWord;
            word.textWrappingMode = TextWrappingModes.NoWrap;
            badgeKey = UiKit.KeyCap(b, th, 34f, out badgeKeyRoot);
            badge.SetActive(false);

            // Not: kâğıt kart, başlık mum kırmızısı, gövde mürekkep; altta "[H] Kapat"
            note = UiKit.PaperCard(root, "Note", th, 4f, out var paper);
            UiKit.Place(note, new Vector2(0f, 1f), notePos, new Vector2(noteWidth, 0f));
            var col = paper.gameObject.AddComponent<VerticalLayoutGroup>();
            col.padding = new RectOffset(28, 28, 20, 22);
            col.spacing = 10f;
            col.childControlWidth = col.childControlHeight = true;
            col.childForceExpandWidth = true;
            col.childForceExpandHeight = false;
            var head = UiKit.Text(paper, "Title", th.HeadingFont, 32f, th.WaxDark, TextAlignmentOptions.Left, FontStyles.Bold);
            head.text = noteTitle;
            noteBody = UiKit.Text(paper, "Body", th.BodyFont, 30f, th.Ink, TextAlignmentOptions.TopLeft);
            var foot = UiKit.Rect(paper, "Footer");
            var frow = foot.gameObject.AddComponent<HorizontalLayoutGroup>();
            frow.spacing = 8f;
            frow.childAlignment = TextAnchor.MiddleRight;
            frow.childControlWidth = frow.childControlHeight = true;
            frow.childForceExpandWidth = frow.childForceExpandHeight = false;
            noteKey = UiKit.KeyCap(foot, th, 34f, out noteKeyRoot);
            var cw = UiKit.Text(foot, "Close", th.BodyFont, 24f, UiKit.WithAlpha(th.Ink, 0.7f), TextAlignmentOptions.MidlineLeft);
            cw.text = closeWord;
            cw.textWrappingMode = TextWrappingModes.NoWrap;
            note.gameObject.AddComponent<CardSizer>().Init(paper, 4f);
            note.gameObject.SetActive(false);

            OnScheme(GameInput.LastScheme);
        }
    }
}
