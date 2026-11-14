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

        /// <summary>Konuşma açıldı (true) / kapandı (false). HUD öğeleri (envanter) kendini gizlemek için dinler.</summary>
        public static event System.Action<bool> VisibilityChanged;
        public static bool IsVisible { get; private set; }

        private GameObject root;
        private DialogueFrame frame;
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
            frame.SetSpeaker(line.Speaker, line.Portrait);
            frame.Body.text = line.Text;
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

        private static bool lineShown;
        private static int holds;

        /// <summary>
        /// Konuşma balonu dışında konuşma sayılan görünümler (ustanın tahmin sorusu) için: açıkken HUD'un
        /// konuşmada çekilen parçaları (mini harita, defter sırtı, envanter) yine çekilir. Her Hold(true) bir Hold(false) ister.
        /// </summary>
        public static void Hold(bool on)
        {
            holds = Mathf.Max(0, holds + (on ? 1 : -1));
            Publish();
        }

        private static void SetVisible(bool visible)
        {
            lineShown = visible;
            Publish();
        }

        private static void Publish()
        {
            bool visible = lineShown || holds > 0;
            if (IsVisible == visible) return;
            IsVisible = visible;
            VisibilityChanged?.Invoke(visible);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            VisibilityChanged = null;
            IsVisible = false;
            lineShown = false;
            holds = 0;
        }

        private void OnScheme(ControlScheme _) => UpdateHint();

        private void UpdateHint()
        {
            if (frame == null) return;
            frame.HintWord.text = hasMore ? continueWord : closeWord;
            UiKit.SetKeyCap(frame.HintKey, frame.HintKeyRoot, ControlGlyphs.For(PromptAction.Interact), 40f);
        }

        private void Build()
        {
            // Ortak çerçeve (DialogueFrame): ustanın sorusuyla birebir aynı görünüm.
            frame = DialogueFrame.Build(transform, "Dialogue", Theme);
            root = frame.Root.gameObject;
            frame.Catcher.onClick.AddListener(() => ViewLocator.Get<IDialogueCommands>()?.Advance());
            root.SetActive(false);
        }
    }
}
