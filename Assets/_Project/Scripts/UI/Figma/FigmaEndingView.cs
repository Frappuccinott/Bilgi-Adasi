using System;
using System.Collections.Generic;
using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Prototip kapanışı (IEndingView) — Figma'dan birebir:
    /// 1) Mektup: "Ekran/Masa kâğıdı" şablonu. Yırtık kenarlı kâğıt (Figma "Kâğıt" şekli, UI_MektupKagidi.png),
    ///    içerik sol üst 96/72: gönderen etiketi (mühür kırmızısı), başlık (Alegreya Black), metin, ince çizgi;
    ///    altta ortada tuş ipucu.
    /// 2) "Ekran/Emeği Geçenler": karartma %80, logo (y 110), jenerik (y 330, aralık 34; yalnızca isimler),
    ///    alt degrade, "Devam edecek" (y 960), sağ altta tuş ipucu.
    /// Sayfalar yumuşak geçer; onay (Enter/Space · A) ya da geri (Esc · B) ve fare tıklaması ilerletir.
    /// </summary>
    public sealed class FigmaEndingView : FigmaView<IEndingView>, IEndingView
    {
        [Tooltip("Figma Ekran/Masa kâğıdı → Kâğıt (yırtık kenar + gölge). Boşsa düz kâğıt kart.")]
        [SerializeField] private Sprite letterPaper;
        [SerializeField] private float fadeSeconds = 0.6f;
        [Tooltip("Sayfa açıldıktan sonra girdinin beklediği süre (önceki ekranın tuşu buraya düşmesin).")]
        [SerializeField] private float inputGrace = 0.6f;

        private const float Left = 120f;

        private RectTransform root;
        private CanvasGroup rootGroup, letterGroup, creditsGroup;
        private TextMeshProUGUI letterFrom, letterTitle, letterBody, letterSign, letterWord, creditsLogo, creditsTeaser, creditsWord;
        private RectTransform creditsColumn;
        private readonly List<(TextMeshProUGUI label, RectTransform rootRt, PromptAction action)> keys =
            new List<(TextMeshProUGUI, RectTransform, PromptAction)>();

        private Action onNext;
        private float shownAt;
        private CanvasGroup fadingIn, fadingOut;
        private InputAction submit, cancel, click;

        private void Awake() => Build();

        protected override void OnEnable()
        {
            base.OnEnable();
            GameInput.SchemeChanged += OnScheme;
            submit = GameInput.FindAction("UI/Submit");
            cancel = GameInput.FindAction("UI/Cancel");
            submit.performed += OnAdvance;
            cancel.performed += OnAdvance;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameInput.SchemeChanged -= OnScheme;
            if (GameInput.TryGetControls(out _))
            {
                submit.performed -= OnAdvance;
                cancel.performed -= OnAdvance;
            }
        }

        // ---------- IEndingView ----------

        public void ShowLetter(string fromLabel, string title, string body, string signature, string continueWord, Action onContinue)
        {
            letterFrom.text = fromLabel;
            letterTitle.text = title;
            letterBody.text = body;
            letterSign.text = signature;
            letterWord.text = continueWord;
            Open(letterGroup, creditsGroup, onContinue);
        }

        public void ShowCredits(string logo, IReadOnlyList<string> names, string teaser, string doneWord, Action onDone)
        {
            creditsLogo.text = logo;
            creditsTeaser.text = teaser;
            creditsWord.text = doneWord;
            FillNames(names);
            Open(creditsGroup, letterGroup, onDone);
        }

        public void Hide()
        {
            onNext = null;
            root.gameObject.SetActive(false);
            SetHudHidden(false);
        }

        // Kapanış açıkken alttaki HUD (envanter, para, görev kartı) çizilmez: mektup ve jenerik tek başına kalır.
        private readonly List<Canvas> hiddenCanvases = new List<Canvas>();

        private void SetHudHidden(bool hidden)
        {
            if (!hidden)
            {
                foreach (var c in hiddenCanvases) if (c != null) c.enabled = true;
                hiddenCanvases.Clear();
                return;
            }
            var own = GetComponentInParent<Canvas>();
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                // Üstteki canvas'lar (sahne geçişi kararması gibi) kalır.
                if (!c.isRootCanvas || c == own || !c.enabled || c.renderMode == RenderMode.WorldSpace
                    || (own != null && c.sortingOrder >= own.sortingOrder)) continue;
                c.enabled = false;
                hiddenCanvases.Add(c);
            }
        }

        private void Open(CanvasGroup page, CanvasGroup other, Action next)
        {
            onNext = next;
            shownAt = Time.unscaledTime;
            bool first = !root.gameObject.activeSelf;
            if (first) SetHudHidden(true);
            root.gameObject.SetActive(true);
            page.gameObject.SetActive(true);
            page.alpha = 0f;
            fadingIn = page;
            fadingOut = other.gameObject.activeSelf ? other : null;
            if (first) rootGroup.alpha = 1f;
            OnScheme(GameInput.LastScheme);
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private void Update()
        {
            if (!root.gameObject.activeSelf) return;
            float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds);
            if (fadingOut != null)
            {
                fadingOut.alpha = Mathf.MoveTowards(fadingOut.alpha, 0f, step);
                if (fadingOut.alpha <= 0f) { fadingOut.gameObject.SetActive(false); fadingOut = null; }
                return; // önce eski sayfa söner, sonra yenisi belirir
            }
            if (fadingIn != null)
            {
                fadingIn.alpha = Mathf.MoveTowards(fadingIn.alpha, 1f, step);
                if (fadingIn.alpha >= 1f) fadingIn = null;
            }
        }

        private void OnAdvance(InputAction.CallbackContext _) => Advance();

        private void Advance()
        {
            if (onNext == null || fadingIn != null || fadingOut != null) return;
            if (Time.unscaledTime - shownAt < inputGrace + fadeSeconds) return;
            var next = onNext;
            onNext = null;
            next.Invoke();
        }

        private void OnScheme(ControlScheme scheme)
        {
            foreach (var k in keys) UiKit.SetKeyCap(k.label, k.rootRt, ControlGlyphs.For(k.action, scheme), 44f);
        }

        // ---------- Kurulum ----------

        private void Build()
        {
            var th = Theme;
            root = UiKit.Fill(UiKit.Rect(transform, "Ending"));
            // Kendi kök Canvas'ında (Kurulum 27: UI/Ending, sıra 100): HUD'un üstünde çizilir.
            rootGroup = root.gameObject.AddComponent<CanvasGroup>();
            // Arka plan her iki sayfada da koyu: oyun dünyası mektubun arkasında soluk kalır.
            var back = UiKit.Img(root, "Back", UiKit.WithAlpha(th.ShadowInk, 0.7f), null, true);
            UiKit.Fill(back.rectTransform);
            // Ekranın herhangi bir yerine tıklamak da ilerletir (fare).
            back.gameObject.AddComponent<ClickCatcher>().Init(Advance);

            BuildLetter(th);
            BuildCredits(th);
            root.gameObject.SetActive(false);
        }

        /// <summary>Figma Ekran/Masa kâğıdı: kâğıt 1127×693 ortada; içerik 96/72'den, genişlik 934.</summary>
        private void BuildLetter(UiTheme th)
        {
            var page = UiKit.Fill(UiKit.Rect(root, "Letter"));
            letterGroup = page.gameObject.AddComponent<CanvasGroup>();
            letterGroup.blocksRaycasts = false;

            RectTransform content;
            if (letterPaper != null)
            {
                // Dışa aktarılan şekil kâğıttan 33 / 32 px büyük (gölge payı).
                var paper = UiKit.Img(page, "Paper", Color.white, letterPaper);
                paper.preserveAspect = true;
                UiKit.Place(paper.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1194f, 758f));
                content = UiKit.Rect(paper.rectTransform, "Content");
                UiKit.Fill(content, 33f + 96f, 32f + 72f, 33f + 98f, 32f + 72f);
            }
            else
            {
                var card = UiKit.PaperCard(page, "Paper", th, 6f, out var inner, 10f);
                UiKit.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1127f, 693f));
                content = UiKit.Rect(inner, "Content");
                UiKit.Fill(content, 90f, 66f, 92f, 66f);
            }

            var col = content.gameObject.AddComponent<VerticalLayoutGroup>();
            col.spacing = 18f;
            col.childAlignment = TextAnchor.UpperLeft;
            col.childControlWidth = col.childControlHeight = true;
            col.childForceExpandWidth = true;
            col.childForceExpandHeight = false;

            letterFrom = UiKit.Text(content, "From", th.BodyFont, 24f, th.WaxDark, TextAlignmentOptions.Left);
            letterTitle = UiKit.Text(content, "Title", th.HeadingBlackFont, 56f, th.InkDeep, TextAlignmentOptions.Left,
                th.HasHeadingBlack ? FontStyles.Normal : FontStyles.Bold);
            var rule = UiKit.Img(content, "Rule", UiKit.WithAlpha(th.WaxDark, 0.45f));
            rule.gameObject.AddComponent<LayoutElement>().preferredHeight = 2f;
            letterBody = UiKit.Text(content, "Body", th.BodyFont, 36f, th.Ink, TextAlignmentOptions.TopLeft);
            letterBody.lineSpacing = 10f;

            // Sağ altta ustanın mumu (UI/Mühür, Figma işaret mührü) ve imza: mektup ustadan geldiği belli olsun.
            var sealRt = UiKit.Rect(content.parent, "Seal");
            UiKit.Place(sealRt, new Vector2(1f, 0f), new Vector2(-(33f + 96f), 32f + 60f), new Vector2(150f, 150f));
            sealRt.localEulerAngles = new Vector3(0f, 0f, -10f);
            var wax = UiKit.Img(sealRt, "Wax", th.SealWax != null ? Color.white : th.Wax, th.SealWax != null ? th.SealWax : UiKit.Disc);
            UiKit.Fill(wax.rectTransform);
            wax.preserveAspect = true;
            if (th.Seal != null)
            {
                var mark = UiKit.Img(sealRt, "Mark", UiKit.WithAlpha(th.WaxDark, 0.9f), th.Seal);
                UiKit.Fill(mark.rectTransform, 30f, 30f, 30f, 30f);
                mark.preserveAspect = true;
            }
            letterSign = UiKit.Text(content.parent, "Sign", th.HeadingItalicFont, 34f, th.InkDeep, TextAlignmentOptions.Right,
                th.HasHeadingItalic ? FontStyles.Normal : FontStyles.Italic);
            UiKit.Place(letterSign.rectTransform, new Vector2(1f, 0f), new Vector2(-(33f + 96f + 170f), 32f + 100f), new Vector2(600f, 50f));
            letterSign.textWrappingMode = TextWrappingModes.NoWrap;

            // Altta ortada tuş ipucu (Figma: y 996).
            var bar = HintBar(page, new Vector2(0.5f, 0f), new Vector2(0f, 1080f - 996f - 44f));
            letterWord = AddHint(bar, th, PromptAction.Confirm);
            page.gameObject.SetActive(false);
        }

        /// <summary>Figma Ekran/Emeği Geçenler: karartma %80, logo (y 110), jenerik (y 330, aralık 34), alt degrade, "Devam edecek".</summary>
        private void BuildCredits(UiTheme th)
        {
            var page = UiKit.Fill(UiKit.Rect(root, "Credits"));
            creditsGroup = page.gameObject.AddComponent<CanvasGroup>();
            creditsGroup.blocksRaycasts = false;
            var dim = UiKit.Img(page, "Dim", UiKit.WithAlpha(th.ShadowInk, 0.8f));
            UiKit.Fill(dim.rectTransform);

            creditsLogo = Logo(page, th);

            creditsColumn = UiKit.Rect(page, "Column");
            UiKit.Place(creditsColumn, new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(1400f, 0f));
            var v = creditsColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 34f;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            creditsColumn.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var fade = UiKit.Img(page, "BottomFade", th.ShadowInk, UiKit.Gradient(true, new Vector2(0f, 0f), new Vector2(1f, 0.9f)));
            fade.rectTransform.anchorMin = new Vector2(0f, 0f);
            fade.rectTransform.anchorMax = new Vector2(1f, 0f);
            fade.rectTransform.pivot = new Vector2(0.5f, 0f);
            fade.rectTransform.anchoredPosition = Vector2.zero;
            fade.rectTransform.sizeDelta = new Vector2(0f, 200f);

            creditsTeaser = UiKit.Text(page, "Teaser", th.HeadingItalicFont, 34f, th.GoldLight, TextAlignmentOptions.Center,
                th.HasHeadingItalic ? FontStyles.Normal : FontStyles.Italic);
            UiKit.Place(creditsTeaser.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 1080f - 960f - 46f), new Vector2(1200f, 46f));
            creditsTeaser.textWrappingMode = TextWrappingModes.NoWrap;

            var bar = HintBar(page, new Vector2(1f, 0f), new Vector2(-Left, 1080f - 990f - 44f));
            creditsWord = AddHint(bar, th, PromptAction.Confirm);
            page.gameObject.SetActive(false);
        }

        private void FillNames(IReadOnlyList<string> names)
        {
            var th = Theme;
            for (int i = creditsColumn.childCount - 1; i >= 0; i--) Destroy(creditsColumn.GetChild(i).gameObject);
            for (int i = 0; names != null && i < names.Count; i++)
            {
                var name = UiKit.Text(creditsColumn, "Name", th.HeadingFont, 38f, th.Paper, TextAlignmentOptions.Center); // Alegreya Bold
                name.text = names[i];
            }
        }

        /// <summary>Logo/Yazı (arayüz): Alegreya 120 (Black), harf aralığı −1, gölge 0 4 16; altında 140×6 turuncu çizgi.</summary>
        private static TextMeshProUGUI Logo(RectTransform parent, UiTheme th)
        {
            var logo = UiKit.Rect(parent, "Logo");
            UiKit.Place(logo, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(545f, 130f));
            var text = UiKit.Text(logo, "Text", th.HeadingBlackFont, 120f, th.Paper, TextAlignmentOptions.Left,
                th.HasHeadingBlack ? FontStyles.Normal : FontStyles.Bold);
            UiKit.Place(text.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(545f, 120f));
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.characterSpacing = -1f;
            UiKit.TextShadow(text, UiKit.WithAlpha(th.ShadowInk, 0.6f), 4f, 16f);
            var rule = UiKit.Img(logo, "Rule", th.LogoAccent, UiKit.Round4);
            UiKit.Place(rule.rectTransform, new Vector2(0f, 1f), new Vector2(0f, -124f), new Vector2(140f, 6f));
            return text;
        }

        private static RectTransform HintBar(RectTransform parent, Vector2 anchor, Vector2 pos)
        {
            var bar = UiKit.Rect(parent, "Hints");
            UiKit.Place(bar, anchor, pos, new Vector2(0f, 44f));
            var row = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 36f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            bar.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            return bar;
        }

        /// <summary>Tuş kapağı (44) + kelime (Alegreya Sans 26, kâğıt rengi, hafif gölge).</summary>
        private TextMeshProUGUI AddHint(RectTransform bar, UiTheme th, PromptAction action)
        {
            var slot = UiKit.Rect(bar, "Hint_" + action);
            var row = slot.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 10f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var key = UiKit.KeyCap(slot, th, 44f, out var keyRoot);
            var w = UiKit.Text(slot, "Word", th.BodyFont, 26f, th.Paper, TextAlignmentOptions.Left);
            w.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.TextShadow(w, UiKit.WithAlpha(th.ShadowInk, 0.7f), 2f, 8f);
            keys.Add((key, keyRoot, action));
            return w;
        }

        private sealed class ClickCatcher : MonoBehaviour, IPointerClickHandler
        {
            private Action click;
            public void Init(Action onClick) => click = onClick;

            public void OnPointerClick(PointerEventData e)
            {
                if (e.button == PointerEventData.InputButton.Left) click?.Invoke();
            }
        }
    }
}
