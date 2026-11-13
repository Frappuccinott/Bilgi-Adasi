using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// HUD'da defterin sırtı (INotebookHudView) — Figma: Defter/1 HUD (kapalı). Sağ altta deri sırt + kurdele (karar 14.11:
    /// sol alt mini haritanın), solunda "Deftere yeni sayfa / K-1 Kuru Çeşme" (yalnızca yeni kayıttan sonra) ve tuş
    /// ipucu "N Defter". Yalnızca oyuncu modunda görünür; yeni kayıtta sırt bir süre altın parlar.
    /// </summary>
    public sealed class FigmaNotebookSpine : FigmaView<INotebookHudView>, INotebookHudView
    {
        [SerializeField] private string hudWord = "Defter";
        [SerializeField] private string newPageWord = "Deftere yeni sayfa";
        [Tooltip("Köşeden uzaklık (px).")]
        [SerializeField] private Vector2 position = new Vector2(36f, 28f);
        [Tooltip("Açık: sağ alt köşe (yazı sırtın solunda). Kapalı: sol alt.")]
        [SerializeField] private bool rightSide = true;
        [Tooltip("Yeni sayfa notunun ekranda kaldığı süre (sn).")]
        [SerializeField] private float newPageSeconds = 7f;
        [SerializeField] private float pulseSpeed = 3.2f;

        private GameObject root, newPage;
        private Image glow;
        private TextMeshProUGUI keyLabel, pageTitle;
        private RectTransform keyRoot;
        private float newPageUntil;

        private void Awake() => Build();

        protected override void OnEnable()
        {
            base.OnEnable();
            GameInput.SchemeChanged += OnScheme;
            GameInput.ModeChanged += OnMode;
            // Konuşmada sağ alt "Devam" ipucuna kalır: sırt gizlenir.
            FigmaDialogueView.VisibilityChanged += OnDialogue;
        }

        protected override void OnDisable()
        {
            GameInput.SchemeChanged -= OnScheme;
            GameInput.ModeChanged -= OnMode;
            FigmaDialogueView.VisibilityChanged -= OnDialogue;
            base.OnDisable();
        }

        protected override void Start()
        {
            base.Start();
            OnMode(GameInput.Mode);
            OnScheme(GameInput.LastScheme);
        }

        public void ShowNewPage(string code, string title)
        {
            pageTitle.text = string.IsNullOrEmpty(title) ? code : code + "  " + title;
            newPageUntil = Time.unscaledTime + newPageSeconds;
            newPage.SetActive(true);
            glow.enabled = true;
        }

        private void Update()
        {
            if (!newPage.activeSelf) return;
            float left = newPageUntil - Time.unscaledTime;
            if (left <= 0f)
            {
                newPage.SetActive(false);
                glow.enabled = false;
                return;
            }
            float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed);
            glow.color = UiKit.WithAlpha(Theme.Gold, (0.35f + 0.65f * k) * Mathf.Clamp01(left));
        }

        private void OnMode(InputMode mode) => root.SetActive(mode == InputMode.Player && !FigmaDialogueView.IsVisible);

        private void OnDialogue(bool _) => OnMode(GameInput.Mode);

        private void OnScheme(ControlScheme _)
        {
            if (keyLabel != null) UiKit.SetKeyCap(keyLabel, keyRoot, ControlGlyphs.For(PromptAction.OpenBook), 40f);
        }

        private void Build()
        {
            var th = Theme;
            // Sağdaysa her şey aynalanır: köşe (1,0), yaprak kenarı ve kurdele sırtın solunda, yazı sağa yaslı.
            float s = rightSide ? -1f : 1f;
            var corner = rightSide ? new Vector2(1f, 0f) : Vector2.zero;
            var rt = UiKit.Rect(transform, "NotebookSpine");
            UiKit.Place(rt, corner, new Vector2(position.x * s, position.y), new Vector2(520f, 150f));
            root = rt.gameObject;

            // Sırt: deri, dikiş çizgisi, kurdele; altın parıltı arkada.
            var spine = UiKit.Rect(rt, "Spine");
            UiKit.Place(spine, corner, Vector2.zero, new Vector2(96f, 150f));
            spine.localRotation = Quaternion.Euler(0f, 0f, 4f * s);
            glow = UiKit.Img(spine, "Glow", th.Gold, UiKit.Round8);
            UiKit.Fill(glow.rectTransform, -6f, -6f, -6f, -6f);
            glow.enabled = false;
            var leather = UiKit.Img(spine, "Leather", th.WoodDark, UiKit.Round8);
            UiKit.Fill(leather.rectTransform);
            var grain = UiKit.Img(leather.transform, "Grain", UiKit.WithAlpha(th.Wood, 0.5f), UiKit.WoodGrain);
            UiKit.Fill(grain.rectTransform);
            var pages = UiKit.Img(spine, "Pages", th.PaperMid, UiKit.Round4);
            UiKit.Place(pages.rectTransform, new Vector2(rightSide ? 0f : 1f, 0.5f), new Vector2(-6f * s, 0f), new Vector2(10f, 136f));
            pages.transform.SetSiblingIndex(0);
            var stitch = UiKit.Img(leather.transform, "Stitch", UiKit.WithAlpha(th.PaperShade, 0.7f));
            UiKit.Place(stitch.rectTransform, new Vector2(rightSide ? 1f : 0f, 0.5f), new Vector2(12f * s, 0f), new Vector2(2f, 126f));
            var band = UiKit.Img(leather.transform, "Band", th.Wood);
            UiKit.Place(band.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(96f, 8f));
            var mark = UiKit.Img(spine, "Bookmark", th.WaxMid);
            UiKit.Place(mark.rectTransform, new Vector2(rightSide ? 0f : 1f, 1f), new Vector2(-30f * s, 14f), new Vector2(16f, 40f));

            // Sırtın iç yanında: yeni sayfa (gizli) + tuş ipucu
            var col = UiKit.Rect(rt, "Text");
            UiKit.Place(col, corner, new Vector2(120f * s, 0f), new Vector2(400f, 150f));
            var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = rightSide ? TextAnchor.LowerRight : TextAnchor.LowerLeft;
            v.spacing = 6f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = v.childForceExpandHeight = false;

            newPage = UiKit.Rect(col, "NewPage").gameObject;
            var nv = newPage.AddComponent<VerticalLayoutGroup>();
            nv.childControlWidth = nv.childControlHeight = true;
            nv.childForceExpandWidth = nv.childForceExpandHeight = false;
            nv.childAlignment = rightSide ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            var align = rightSide ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            var head = UiKit.Text(newPage.transform, "Head", th.BodyFont, 26f, th.GoldLight, align, FontStyles.Bold);
            head.text = newPageWord;
            head.textWrappingMode = TextWrappingModes.NoWrap;
            pageTitle = UiKit.Text(newPage.transform, "Title", th.HeadingFont, 34f, th.Paper, align, FontStyles.Italic);
            pageTitle.textWrappingMode = TextWrappingModes.NoWrap;
            newPage.SetActive(false);

            var hint = UiKit.Rect(col, "Hint");
            var h = hint.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.childAlignment = rightSide ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            keyLabel = UiKit.KeyCap(hint, th, 40f, out keyRoot);
            var word = UiKit.Text(hint, "Word", th.BodyFont, 28f, th.Paper, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            word.text = hudWord;
            word.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }
}
