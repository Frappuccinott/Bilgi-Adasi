using System;
using System.Collections;
using System.Collections.Generic;
using BilgiAdasi.Core;
using BilgiAdasi.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = System.Random;

namespace BilgiAdasi.Desk
{
    /// <summary>
    /// Masa görev akışı (03, 04 §5): güvercin gelir → zarf masada → oyuncu masaya oturur (DeskStation) → kâğıt açılır →
    /// sorular sırayla; yanlışta leke + tekrar (2 yanlışta ustanın notu), Lonca'da yanlış soru değişir →
    /// hepsi doğru → mühür → Devam: istasyondan çıkılır, görev biter (TaskCompleted), "Gerçek hayatta", Sorgu + Defter.
    /// Masa objesinde (DeskStation ile) durur. Görünümleri yalnızca arayüzle (ViewLocator) tanır.
    /// Gamepad: ↑↓ satır, RB/LB seçenek, A sonraki satır / Gönder; fare: seçeneğe tıkla.
    /// </summary>
    [RequireComponent(typeof(DeskStation))]
    public sealed class DeskController : MonoBehaviour, IPaperCommands
    {
        public enum State
        {
            Idle,
            Arriving,   // güvercin yolda
            Waiting,    // zarf masada
            Answering,
            Checking,   // doğru: kısa bekleme, sonra sıradaki soru
            Sealed
        }

        private const int HintFailThreshold = 2;

        [SerializeField] private DeskTexts texts;
        [Tooltip("Görev verildikten sonra güvercinin konmasına kadar (sn).")]
        [SerializeField] private float arrivalDelay = 2.5f;
        [Tooltip("Doğru cevaptan sonra sıradaki soruya geçiş (sn).")]
        [SerializeField] private float correctDelay = 0.9f;
        [Tooltip("Lonca'da yanlış soru, leke görüldükten sonra değişir (sn).")]
        [SerializeField] private float replaceDelay = 1.4f;
        [SerializeField] private float realLifeSeconds = 8f;

        private DeskStation station;
        private DeskTask task;
        private string intro;
        private Random rng;
        private readonly List<QuestionData> sequence = new List<QuestionData>();
        private GuildExamPool<QuestionData> pool;
        private int index;
        private PaperQuestion current;
        private int focus;
        private int fails;
        private bool listening;
        private Coroutine flow;

        private IPaperView paperView;
        private IDeskPropsView propsView;
        private IRealLifeCardView realLifeView;
        private INoticeView noticeView;
        private IHintBookView hintBookView;

        public State Current { get; private set; } = State.Idle;
        public DeskTask ActiveTask => task;
        public DeskTexts Texts => texts;
        /// <summary>Masada açılacak mektup var (ya da yarım kalmış kâğıt).</summary>
        public bool HasLetter => Current == State.Waiting || Current == State.Answering || Current == State.Checking || Current == State.Sealed;

        /// <summary>Görev bitti (mühür + Devam). NeighborhoodRunner dinler.</summary>
        public event Action<DeskTask> TaskFinished;

        private void Awake() => station = GetComponent<DeskStation>();

        private void OnEnable()
        {
            ViewLocator.Changed += OnViewsChanged;
            ViewLocator.Register<IPaperCommands>(this);
            CacheViews();
        }

        private void OnDisable()
        {
            ViewLocator.Changed -= OnViewsChanged;
            ViewLocator.Unregister<IPaperCommands>(this);
            StopListening();
            // Coroutine durur; yolda kalan güvercin konmuş sayılır, doğru-bekleme ortasındaysa soru yeniden açılır.
            if (Current == State.Arriving) Current = State.Waiting;
            if (Current == State.Checking) Current = State.Answering;
        }

        // ---------- Görev ----------

        /// <summary>
        /// Masaya görev verir: güvercin gelir, zarf masaya düşer. introLines: köyün ilk görevinde ustanın tanıtımı
        /// (kâğıdın başına). immediate: geliştirici atlaması (güvercin beklenmez).
        /// </summary>
        public void Assign(DeskTask newTask, IReadOnlyList<string> introLines, bool immediate = false)
        {
            if (newTask == null) return;
            ClearTask();
            task = newTask;
            intro = Join(introLines);
            rng = new Random();
            sequence.Clear();
            pool = null;
            index = 0;
            fails = 0;
            current = null;
            Current = State.Arriving;
            flow = StartCoroutine(ArriveRoutine(immediate ? 0f : arrivalDelay));
        }

        /// <summary>Görevi bırakır (geliştirici kısayolu). Kâğıt açıksa kapanır, istasyondan çıkılır.</summary>
        public void ClearTask()
        {
            if (flow != null) StopCoroutine(flow);
            flow = null;
            if (station != null && station.IsActive) station.Exit();
            paperView?.Close();
            propsView?.SetEnvelope(false, false);
            propsView?.SetPaperOpen(false);
            task = null;
            current = null;
            Current = State.Idle;
        }

        private IEnumerator ArriveRoutine(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            flow = null;
            Current = State.Waiting;
            propsView?.PlayArrival(task.IsGuildExam);
            propsView?.SetEnvelope(true, task.IsGuildExam);
            if (texts != null) noticeView?.ShowNotice(task.IsGuildExam ? texts.GuildArrived : texts.PigeonArrived);
        }

        // ---------- İstasyon ----------

        internal void OnStationEnter()
        {
            if (task == null) return;
            if (Current == State.Waiting) BeginPaper();
            propsView?.SetEnvelope(false, task.IsGuildExam);
            propsView?.SetPaperOpen(true);
            OpenView();
            StartListening();
        }

        internal void OnStationExit()
        {
            StopListening();
            paperView?.Close();
            propsView?.SetPaperOpen(false);
            // Yarım kâğıt masada kalır.
            if (task != null && Current != State.Idle) propsView?.SetEnvelope(true, task.IsGuildExam);
        }

        private void BeginPaper()
        {
            sequence.Clear();
            if (task.RandomCount > 0)
            {
                pool = new GuildExamPool<QuestionData>(task.Questions, rng);
                sequence.AddRange(pool.Draw(task.RandomCount));
            }
            else
            {
                for (int i = 0; i < task.Questions.Count; i++)
                    if (task.Questions[i] != null) sequence.Add(task.Questions[i]);
            }
            index = 0;
            current = sequence.Count > 0 ? sequence[0].Build(rng) : null;
            focus = 0;
            Current = current != null ? State.Answering : State.Sealed;
        }

        private void OpenView()
        {
            if (paperView == null) return;
            string letter = Join(task.GiverLines);
            if (!string.IsNullOrEmpty(intro))
                letter = (texts != null ? "<b>" + texts.MasterName + ":</b> " : string.Empty) + intro + "\n\n" + letter;
            paperView.Open(new PaperHeader(task.PaperTitle, texts != null ? texts.FromLabel : string.Empty, task.GiverName,
                letter, texts != null ? texts.SubmitWord : string.Empty, task.IsGuildExam));
            if (Current == State.Sealed)
            {
                ShowSeal();
                return;
            }
            ShowCurrent();
        }

        private void ShowCurrent()
        {
            if (paperView == null || current == null) return;
            string counter = texts != null ? string.Format(texts.QuestionCounter, index + 1, sequence.Count) : string.Empty;
            focus = FirstVisible(0, 1);
            paperView.ShowQuestion(current, counter);
            paperView.Refresh(current, focus);
            if (fails >= HintFailThreshold) hintBookView?.SetGlowing(true);
        }

        // ---------- Komutlar (fare + girdi) ----------

        public void Focus(int row)
        {
            if (current == null || Current != State.Answering) return;
            if (row < 0 || row > current.Rows.Count) return;
            if (row < current.Rows.Count && !current.IsVisible(row)) return;
            focus = row;
            paperView?.Refresh(current, focus);
        }

        public void SelectOption(int row, int option)
        {
            if (current == null || Current != State.Answering) return;
            current.Select(row, option);
            focus = row;
            paperView?.Refresh(current, focus);
        }

        public void StepOption(int row, int delta)
        {
            if (current == null || Current != State.Answering || row < 0 || row >= current.Rows.Count) return;
            current.Step(row, delta);
            focus = row;
            paperView?.Refresh(current, focus);
        }

        public void Submit()
        {
            if (current == null || Current != State.Answering) return;
            switch (current.Evaluate())
            {
                case PaperVerdict.Incomplete:
                    GameEvents.RaiseSound(SoundId.Info);
                    Note(texts != null ? texts.Incomplete : null, PaperNoteKind.Info);
                    break;
                case PaperVerdict.NotMinimal:
                    GameEvents.RaiseSound(SoundId.Info);
                    Note(texts != null ? texts.NotMinimal : null, PaperNoteKind.Info);
                    break;
                case PaperVerdict.Wrong:
                    OnWrong();
                    break;
                default:
                    Current = State.Checking;
                    GameEvents.RaiseSound(SoundId.Correct);
                    Note(texts != null ? texts.Correct : null, PaperNoteKind.Correct);
                    paperView?.Refresh(current, focus);
                    flow = StartCoroutine(NextRoutine());
                    break;
            }
        }

        public void Continue()
        {
            if (Current != State.Sealed || task == null) return;
            Finish();
        }

        private void OnWrong()
        {
            fails++;
            GameEvents.RaiseFailed(task.Id);
            GameEvents.RaiseSound(SoundId.Wrong);
            bool equation = current is BalancePaper;
            Note(texts != null ? (equation ? texts.WrongEquation : texts.Wrong) : null, PaperNoteKind.Wrong);
            paperView?.Refresh(current, focus);
            if (fails >= HintFailThreshold) hintBookView?.SetGlowing(true);

            // Lonca: yanlış soru havuzdan başkasıyla değişir (havuz boşsa aynı soru kalır).
            if (pool != null && pool.TryReplace(out var replacement))
            {
                sequence[index] = replacement;
                Current = State.Checking;
                flow = StartCoroutine(ReplaceRoutine(replacement));
            }
        }

        private IEnumerator ReplaceRoutine(QuestionData replacement)
        {
            yield return new WaitForSeconds(replaceDelay);
            flow = null;
            current = replacement.Build(rng);
            Current = State.Answering;
            paperView?.HideHint();
            ShowCurrent();
            Note(texts != null ? texts.Replaced : null, PaperNoteKind.Info);
        }

        private IEnumerator NextRoutine()
        {
            yield return new WaitForSeconds(correctDelay);
            flow = null;
            index++;
            paperView?.HideHint();
            if (index < sequence.Count)
            {
                current = sequence[index].Build(rng);
                Current = State.Answering;
                ShowCurrent();
                yield break;
            }
            current = null;
            Current = State.Sealed;
            ShowSeal();
        }

        private void ShowSeal()
        {
            hintBookView?.SetGlowing(false);
            if (texts == null) return;
            string seal = task.IsGuildExam ? texts.GuildSeal : texts.MasterSeal;
            string closing = Join(task.DeliveryLines);
            if (!string.IsNullOrEmpty(closing)) seal += "\n<size=75%><i>" + closing + "</i></size>";
            paperView?.ShowSeal(seal, texts.ContinueWord, task.IsGuildExam);
        }

        private void Finish()
        {
            var done = task;
            task = null;
            current = null;
            Current = State.Idle;
            StopListening();
            paperView?.Close();
            if (station != null && station.IsActive) station.Exit();
            propsView?.SetEnvelope(false, false);
            propsView?.SetPaperOpen(false);
            propsView?.PlayDeparture();

            GameEvents.RaiseTaskCompleted(done.Id);
            if (!string.IsNullOrEmpty(done.RealLifeText) && realLifeView != null) StartCoroutine(RealLifeRoutine(done.RealLifeText));
            TaskFinished?.Invoke(done);
            Order.LearningDirector.Instance?.Play(done.Id);
        }

        private IEnumerator RealLifeRoutine(string text)
        {
            realLifeView?.Show(text);
            yield return new WaitForSeconds(realLifeSeconds);
            realLifeView?.Hide();
        }

        // ---------- İpucu ----------

        private void ToggleHint()
        {
            if (paperView == null || task == null) return;
            if (paperView.IsHintOpen)
            {
                paperView.HideHint();
                return;
            }
            if (texts == null) return;
            if (fails < HintFailThreshold)
            {
                Note(texts.HintLocked, PaperNoteKind.Info);
                return;
            }
            string hint = current != null && !string.IsNullOrWhiteSpace(current.Hint) ? current.Hint : task.HintText;
            paperView.ShowHint(texts.HintTitle, hint);
            GameEvents.RaiseHintOpened(task.Id);
        }

        // ---------- Girdi (Station map) ----------

        private void StartListening()
        {
            if (listening) return;
            listening = true;
            var s = GameInput.Controls.Station;
            s.SelectPrevious.performed += OnPrevious;
            s.SelectNext.performed += OnNext;
            s.Increase.performed += OnIncrease;
            s.Decrease.performed += OnDecrease;
            s.Confirm.performed += OnConfirm;
            s.OpenHint.performed += OnHint;
        }

        private void StopListening()
        {
            if (!listening) return;
            listening = false;
            if (!GameInput.TryGetControls(out var c)) return;
            var s = c.Station;
            s.SelectPrevious.performed -= OnPrevious;
            s.SelectNext.performed -= OnNext;
            s.Increase.performed -= OnIncrease;
            s.Decrease.performed -= OnDecrease;
            s.Confirm.performed -= OnConfirm;
            s.OpenHint.performed -= OnHint;
        }

        private void OnPrevious(InputAction.CallbackContext _) => MoveFocus(-1);
        private void OnNext(InputAction.CallbackContext _) => MoveFocus(1);
        private void OnIncrease(InputAction.CallbackContext _) => StepFocused(1);
        private void OnDecrease(InputAction.CallbackContext _) => StepFocused(-1);
        private void OnHint(InputAction.CallbackContext _) => ToggleHint();

        private void OnConfirm(InputAction.CallbackContext _)
        {
            if (Current == State.Sealed)
            {
                Continue();
                return;
            }
            if (current == null || Current != State.Answering) return;
            if (focus >= current.Rows.Count)
            {
                Submit();
                return;
            }
            // Satırda A/Enter: boşsa ilk seçenek, değilse sonraki satıra geç.
            if (current.Rows[focus].Selected < 0) StepOption(focus, 1);
            else MoveFocus(1);
        }

        private void StepFocused(int delta)
        {
            if (current == null || focus >= current.Rows.Count) return;
            StepOption(focus, delta);
        }

        private void MoveFocus(int delta)
        {
            if (current == null || Current != State.Answering) return;
            int next = FirstVisible(focus + delta, delta);
            if (next < 0) next = delta < 0 ? FirstVisible(0, 1) : current.Rows.Count;
            focus = next;
            paperView?.Refresh(current, focus);
        }

        /// <summary>start'tan step yönünde ilk görünür satır; Rows.Count = Gönder; bulunamazsa -1.</summary>
        private int FirstVisible(int start, int step)
        {
            if (current == null) return 0;
            for (int i = start; i >= 0 && i <= current.Rows.Count; i += step)
                if (i == current.Rows.Count || current.IsVisible(i)) return i;
            return -1;
        }

        // ---------- Yardımcılar ----------

        private void Note(string text, PaperNoteKind kind)
        {
            if (!string.IsNullOrEmpty(text)) paperView?.ShowNote(text, kind);
        }

        private static string Join(IReadOnlyList<string> lines)
        {
            if (lines == null || lines.Count == 0) return string.Empty;
            return string.Join("\n", lines);
        }

        private void OnViewsChanged(Type _) => CacheViews();

        private void CacheViews()
        {
            paperView = ViewLocator.Get<IPaperView>();
            propsView = ViewLocator.Get<IDeskPropsView>();
            realLifeView = ViewLocator.Get<IRealLifeCardView>();
            noticeView = ViewLocator.Get<INoticeView>();
            hintBookView = ViewLocator.Get<IHintBookView>();
        }
    }
}
