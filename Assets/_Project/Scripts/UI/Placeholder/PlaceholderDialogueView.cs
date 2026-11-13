using BilgiAdasi.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// TEST YER TUTUCUSU — ham konuşma balonu: ekranın altında konuşan adı, metin, varsa portre ve "devam" ipucu.
    /// Arayüzü çalışma zamanında kendisi kurar; Canvas altına konması yeter. Mustafa'nın view'ı gelince silinir.
    /// Gerçek view aynı arayüzü uygular: RegisteredView&lt;IDialogueView&gt;, IDialogueView.
    /// </summary>
    public sealed class PlaceholderDialogueView : RegisteredView<IDialogueView>, IDialogueView
    {
        [Header("Ham etiketler (yer tutucu)")]
        [SerializeField] private string continueWord = "devam";
        [SerializeField] private string closeWord = "kapat";

        private static readonly Color Parchment = new Color(0.91f, 0.85f, 0.71f, 0.97f);
        private static readonly Color Ink = new Color(0.23f, 0.16f, 0.10f, 1f);
        private static readonly Color Wood = new Color(0.33f, 0.22f, 0.13f, 0.96f);

        private GameObject box, catcher;
        private TMP_Text speakerText, bodyText, promptText;
        private Image portrait;
        private bool hasMore;

        private void Awake() => BuildUi();

        protected override void OnEnable()
        {
            base.OnEnable();
            GameInput.SchemeChanged += OnSchemeChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameInput.SchemeChanged -= OnSchemeChanged;
        }

        public void ShowLine(DialogueLine line, bool more)
        {
            hasMore = more;
            speakerText.text = line.Speaker;
            bodyText.text = line.Text;
            portrait.sprite = line.Portrait;
            portrait.gameObject.SetActive(line.Portrait != null);
            var bodyRect = (RectTransform)bodyText.transform;
            bodyRect.offsetMin = new Vector2(line.Portrait != null ? 248f : 40f, bodyRect.offsetMin.y);
            UpdatePrompt();
            catcher.SetActive(true);
            box.SetActive(true);
        }

        public void Hide()
        {
            if (box != null) box.SetActive(false);
            if (catcher != null) catcher.SetActive(false);
        }

        private void OnSchemeChanged(ControlScheme _) => UpdatePrompt();

        private void UpdatePrompt()
        {
            if (promptText == null) return;
            promptText.text = ControlGlyphs.For(PromptAction.Interact) + "  " + (hasMore ? continueWord : closeWord);
        }

        private static void OnBoxClicked() => ViewLocator.Get<IDialogueCommands>()?.Advance();

        // ---------- Kurulum ----------

        private void BuildUi()
        {
            // Ekranın herhangi bir yerine tıklamak da konuşmayı ilerletir (balonun arkasında, görünmez).
            catcher = new GameObject("ClickCatcher", typeof(RectTransform), typeof(Image), typeof(Button));
            var crt = (RectTransform)catcher.transform;
            crt.SetParent(transform, false);
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            catcher.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            catcher.GetComponent<Button>().onClick.AddListener(OnBoxClicked);
            catcher.SetActive(false);

            box = new GameObject("DialogueBox", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = (RectTransform)box.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 170f); // envanter çubuğunun üstünde
            rt.sizeDelta = new Vector2(1300f, 260f);
            box.GetComponent<Image>().color = Parchment;
            box.GetComponent<Button>().onClick.AddListener(OnBoxClicked);

            var portraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            var prt = (RectTransform)portraitGo.transform;
            prt.SetParent(rt, false);
            prt.anchorMin = prt.anchorMax = new Vector2(0f, 0.5f);
            prt.pivot = new Vector2(0f, 0.5f);
            prt.anchoredPosition = new Vector2(24f, 0f);
            prt.sizeDelta = new Vector2(200f, 200f);
            portrait = portraitGo.GetComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            portraitGo.SetActive(false);

            var header = new GameObject("SpeakerBand", typeof(RectTransform), typeof(Image));
            var hrt = (RectTransform)header.transform;
            hrt.SetParent(rt, false);
            hrt.anchorMin = new Vector2(0f, 1f);
            hrt.anchorMax = new Vector2(0f, 1f);
            hrt.pivot = new Vector2(0f, 0f);
            hrt.anchoredPosition = new Vector2(24f, -8f);
            hrt.sizeDelta = new Vector2(420f, 56f);
            var headerImage = header.GetComponent<Image>();
            headerImage.color = Wood;
            headerImage.raycastTarget = false;

            speakerText = MakeText(hrt, 34, Parchment, FontStyles.Bold, TextAlignmentOptions.Center);
            Fill((RectTransform)speakerText.transform, 12f, 0f, 12f, 0f);

            bodyText = MakeText(rt, 32, Ink, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Fill((RectTransform)bodyText.transform, 40f, 64f, 40f, 56f);

            promptText = MakeText(rt, 28, Ink, FontStyles.Italic, TextAlignmentOptions.BottomRight);
            Fill((RectTransform)promptText.transform, 40f, 200f, 32f, 16f);

            box.SetActive(false);
        }

        private static TMP_Text MakeText(RectTransform parent, float size, Color color, FontStyles style, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        /// <summary>Ebeveyni kenarlardan içeri boşlukla doldurur (sol, üst, sağ, alt).</summary>
        private static void Fill(RectTransform rt, float left, float top, float right, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
