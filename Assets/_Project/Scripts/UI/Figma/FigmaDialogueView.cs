using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Konuşma — Figma: Ekranlar (v2) / Ekran/Diyalog. Kutu yok: alttan koyulaşan geçiş üstünde yuvarlak portre
    /// (kâğıt çerçeve), altın konuşan adı, kâğıt renginde metin; sağ altta tuş kapağı + "devam"/"kapat".
    /// Ekrana tıklamak IDialogueCommands.Advance'ı çağırır; klavye/gamepad onayını DialogueDirector dinler.
    /// </summary>
    public sealed class FigmaDialogueView : FigmaView<IDialogueView>, IDialogueView
    {
        [SerializeField] private string continueWord = "Devam";
        [SerializeField] private string closeWord = "Kapat";

        // Yerleşim (1920×1080): sol kenar, portre boyu, metin bloğunun üst kenarı (alttan) ve genişliği.
        private const float Left = 120f, PortraitSize = 108f, Top = 300f, TextWidth = 1000f;

        /// <summary>Konuşma açıldı (true) / kapandı (false). HUD öğeleri (envanter) kendini gizlemek için dinler.</summary>
        public static event System.Action<bool> VisibilityChanged;
        public static bool IsVisible { get; private set; }

        private GameObject root;
        private RectTransform textBlock, hintKeyRoot;
        private TextMeshProUGUI speaker, body, hintKey, hintWord;
        private GameObject portraitFrame;
        private Image portrait;
        private bool hasMore;

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

        public void ShowLine(DialogueLine line, bool more)
        {
            hasMore = more;
            speaker.text = line.Speaker;
            body.text = line.Text;
            bool hasPortrait = line.Portrait != null;
            portrait.sprite = line.Portrait;
            portraitFrame.SetActive(hasPortrait);
            textBlock.offsetMin = new Vector2(hasPortrait ? Left + PortraitSize + 28f : Left, textBlock.offsetMin.y);
            textBlock.offsetMax = new Vector2(textBlock.offsetMin.x + TextWidth, textBlock.offsetMax.y);
            UpdateHint();
            root.SetActive(true);
            SetVisible(true);
        }

        public void Hide()
        {
            if (root == null || !root.activeSelf) return;
            root.SetActive(false);
            SetVisible(false);
        }

        private static void SetVisible(bool visible)
        {
            if (IsVisible == visible) return;
            IsVisible = visible;
            VisibilityChanged?.Invoke(visible);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            VisibilityChanged = null;
            IsVisible = false;
        }

        private void OnScheme(ControlScheme _) => UpdateHint();

        private void UpdateHint()
        {
            if (hintWord == null) return;
            hintWord.text = hasMore ? continueWord : closeWord;
            UiKit.SetKeyCap(hintKey, hintKeyRoot, ControlGlyphs.For(PromptAction.Interact), 40f);
        }

        private void Build()
        {
            var th = Theme;
            var rootRt = UiKit.Fill(UiKit.Rect(transform, "Dialogue"));
            root = rootRt.gameObject;

            // Alt geçiş: tıklama yakalayıcı da budur (ekranın tamamı).
            var catcher = UiKit.Img(rootRt, "ClickCatcher", new Color(0, 0, 0, 0), null, true);
            UiKit.Fill(catcher.rectTransform);
            catcher.gameObject.AddComponent<Button>().onClick.AddListener(() => ViewLocator.Get<IDialogueCommands>()?.Advance());
            var fade = UiKit.Img(rootRt, "BottomFade", UiKit.WithAlpha(th.InkDeep, 0.85f), UiKit.FadeUp);
            var frt = fade.rectTransform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(1f, 0f);
            frt.pivot = new Vector2(0.5f, 0f);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = new Vector2(0f, 400f);

            // Portre: 108 px daire, kâğıt çerçeve (Figma oranında, ekran köşesine biraz daha yakın ve küçük)
            var pf = UiKit.Img(rootRt, "PortraitFrame", th.Paper, UiKit.Disc);
            UiKit.Place(pf.rectTransform, new Vector2(0f, 0f), new Vector2(Left, Top - PortraitSize), new Vector2(PortraitSize, PortraitSize));
            portraitFrame = pf.gameObject;
            var mask = UiKit.Img(pf.transform, "Mask", Color.white, UiKit.Disc);
            UiKit.Fill(mask.rectTransform, 5, 5, 5, 5);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            portrait = UiKit.Img(mask.transform, "Portrait", Color.white);
            UiKit.Fill(portrait.rectTransform, -12, -6, -12, -18);
            portrait.preserveAspect = true;

            // Metin bloğu: portrenin sağında, üst kenarı portreyle hizalı
            textBlock = UiKit.Rect(rootRt, "Text");
            textBlock.anchorMin = Vector2.zero;
            textBlock.anchorMax = Vector2.zero;
            textBlock.pivot = new Vector2(0f, 1f);
            textBlock.offsetMin = new Vector2(Left + PortraitSize + 28f, 60f);
            textBlock.offsetMax = new Vector2(Left + PortraitSize + 28f + TextWidth, Top);
            var col = textBlock.gameObject.AddComponent<VerticalLayoutGroup>();
            col.spacing = 8f;
            col.childControlWidth = col.childControlHeight = true;
            col.childForceExpandWidth = true;
            col.childForceExpandHeight = false;
            speaker = UiKit.Text(textBlock, "Speaker", th.BodyFont, 24f, th.GoldLight, TextAlignmentOptions.Left, FontStyles.Bold);
            body = UiKit.Text(textBlock, "Body", th.BodyFont, 31f, th.Paper);
            foreach (var t in new[] { speaker, body })
            {
                var sh = t.gameObject.AddComponent<Shadow>();
                sh.effectColor = UiKit.WithAlpha(th.InkDeep, 0.6f);
                sh.effectDistance = new Vector2(0f, -2f);
            }

            // Sağ alt: tuş kapağı + devam/kapat (Figma x 1730, y 972)
            var hint = UiKit.Rect(rootRt, "Hint");
            UiKit.Place(hint, new Vector2(1f, 0f), new Vector2(-72f, 56f), new Vector2(0f, 40f));
            var row = hint.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 10f;
            row.childAlignment = TextAnchor.MiddleRight;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var fit = hint.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            hintKey = UiKit.KeyCap(hint, th, 40f, out hintKeyRoot);
            hintWord = UiKit.Text(hint, "Word", th.BodyFont, 26f, th.Paper, TextAlignmentOptions.Left);
            hintWord.textWrappingMode = TextWrappingModes.NoWrap;

            root.SetActive(false);
        }
    }
}
