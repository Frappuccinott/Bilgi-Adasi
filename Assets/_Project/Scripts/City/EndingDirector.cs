using System.Collections;
using System.Collections.Generic;
using BilgiAdasi.Core;
using BilgiAdasi.Order;
using BilgiAdasi.Player;
using BilgiAdasi.UI;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Prototip kapanışı (05-icerik-prototip §4, M14): bütün köyler bitince, açık ekranlar (sorgu, Keşif Defteri,
    /// konuşma, masa) kapandıktan sonra ustanın veda mektubu → Emeği Geçenler → ana menü. Bir kez oynar (kayıtta
    /// "kapanis" bayrağı); bayrak yazılınca otomatik kaydedilir, "Devam et" kapanışı yeniden açmaz.
    /// Görünümü yalnızca IEndingView ile tanır. Sınıfta uyanma sahnesi yok (Classroom sahnesi kaldırıldı, 13.11).
    /// </summary>
    public sealed class EndingDirector : MonoBehaviour
    {
        public const string EndingFlag = "kapanis";

        [SerializeField] private NeighborhoodRunner runner;
        [SerializeField] private EndingTexts texts;
        [Tooltip("Son görev ekranları kapandıktan sonra mektuba kadar beklenen süre (sn).")]
        [SerializeField] private float delay = 2f;

        private Coroutine routine;
        private bool playing;
        private InputMode modeBefore;

        private static SaveData Data => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

        private void OnEnable()
        {
            GameEvents.NeighborhoodCompleted += OnProgress;
            GameEvents.TaskCompleted += OnProgress;
        }

        private void OnDisable()
        {
            GameEvents.NeighborhoodCompleted -= OnProgress;
            GameEvents.TaskCompleted -= OnProgress;
        }

        // Kayıttan: hepsi bitmiş ama kapanış görülmemişse (ör. son görevden sonra oyun kapandı) yine oynar.
        private void Start() => OnProgress(null);

        private void OnProgress(string _)
        {
            if (playing || routine != null || !AllDone()) return;
            var data = Data;
            if (data != null && data.HasFlag(EndingFlag)) return;
            routine = StartCoroutine(PlayWhenFree());
        }

        private bool AllDone()
        {
            if (runner == null) runner = FindAnyObjectByType<NeighborhoodRunner>();
            if (runner == null || runner.Neighborhoods.Count == 0) return false;
            var list = runner.Neighborhoods;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && runner.CompletedCount(list[i]) < list[i].Tasks.Count) return false;
            return true;
        }

        private IEnumerator PlayWhenFree()
        {
            // Son görevin sorgusu, Keşif Defteri, konuşma ve masa kapansın; sonra kısa bir nefes.
            float free = 0f;
            while (free < delay)
            {
                free = IsSomethingOpen() ? 0f : free + Time.unscaledDeltaTime;
                yield return null;
            }
            routine = null;
            Play();
        }

        private static bool IsSomethingOpen() =>
            LearningDirector.IsBusy || PredictionDirector.IsBusy || StationBase.Active != null
            || GameInput.Mode != InputMode.Player || DialogueFocus.IsOpen;

        private bool trial;

        /// <summary>
        /// Deneme: köyler bitmeden kapanışı oynatır; kayda "kapanis" yazmaz ve kaydetmez, sonunda yalnızca ana menüye
        /// döner. Play modunda Inspector'da EndingDirector bileşeninin ⋮ menüsünden.
        /// </summary>
        [ContextMenu("Kapanışı dene (kayda yazmaz)")]
        public void PlayTrial()
        {
            if (!Application.isPlaying) return;
            trial = true;
            Play();
        }

        /// <summary>Kapanışı başlatır.</summary>
        public void Play()
        {
            var view = ViewLocator.Get<IEndingView>();
            if (view == null || texts == null || playing) return;
            playing = true;
            modeBefore = GameInput.Mode;
            GameInput.SetMode(InputMode.UI);
            GameInput.SetCursorFree(true);
            view.ShowLetter(texts.LetterFrom, texts.LetterTitle, texts.LetterBody, texts.LetterSignature, texts.LetterContinue, ShowCredits);
        }

        private void ShowCredits()
        {
            var view = ViewLocator.Get<IEndingView>();
            if (view == null) { Finish(); return; }
            var menu = MenuTexts.Load();
            names.Clear();
            foreach (var e in menu.CreditEntries)
                if (!string.IsNullOrWhiteSpace(e.name)) names.Add(e.name);
            view.ShowCredits(menu.Title, names, menu.CreditsTeaser, texts.CreditsDone, Finish);
        }

        private readonly List<string> names = new List<string>(4);

        private void Finish()
        {
            // Kapanış görüldü: kayda yaz ve kaydet (Devam et kapanışı yeniden açmasın), sonra ana menü.
            var data = Data;
            if (!trial)
            {
                if (data != null) data.SetFlag(EndingFlag);
                if (GameManager.Instance != null) GameManager.Instance.RequestCheckpoint();
            }
            trial = false;
            if (SceneFlow.Instance != null)
            {
                SceneFlow.Instance.Load(SceneFlow.MenuScene);
                return;
            }
            ViewLocator.Get<IEndingView>()?.Hide();
            playing = false;
            GameInput.SetMode(modeBefore == InputMode.None ? InputMode.Player : modeBefore);
            GameInput.SetCursorFree(GameInput.Mode != InputMode.Player);
        }
    }
}
