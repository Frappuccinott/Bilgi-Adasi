using System;
using System.Collections;
using System.Collections.Generic;
using BilgiAdasi.Core;
using BilgiAdasi.Player;
using BilgiAdasi.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Öğrenme katmanı (04-gorev-plani §1 adım 4–5): sipariş ürünü hazır olunca (OrderController.ReadyToDeliver)
    /// Câbir Usta sorar → oyuncu açıklama seçer → usta cevaplar → Keşif Defteri'ne sayfa yazılır.
    /// Sahneye elle konmaz: kazanı (OrderController) olan her sahnede kendiliğinden kurulur, veriyi
    /// Resources/LearningData'dan alır. Görünüme yalnızca arayüzle konuşur (IReasoningView, IDiscoveryBookView).
    ///
    /// Defterdeki sayfalar ayrıca kaydedilmez: görevi bitmiş ya da ürünü teslim bekleyen her siparişin sayfası
    /// yazılmış sayılır (kayıttaki completedTasks + readyTaskId).
    /// </summary>
    public sealed class LearningDirector : MonoBehaviour, IDiscoveryBookCommands, ILessonCommands
    {
        private const string ObjectName = "_LearningDirector";

        [SerializeField] private LearningData data;
        [SerializeField] private OrderController orderController;

        private readonly List<string> pages = new List<string>(8);
        private PlayerInteractor blockedUser;
        private Coroutine routine;
        // Ders notu: alınmış siparişin kimliği (Esc menüsünden yeniden okumak için) ve açıkken önceki girdi modu.
        private string currentTaskId;
        private InputMode modeBeforeLesson;
        private bool lessonOpen;

        /// <summary>Sorgu ya da defter açıkken true (oyuncu hareketi kapalıdır).</summary>
        public static bool IsBusy { get; private set; }

        public static LearningDirector Instance { get; private set; }

        private static SaveData Save => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

        // ---------- kendiliğinden kurulum ----------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureFor(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode _) => EnsureFor(scene);

        private static void EnsureFor(Scene scene)
        {
            if (Instance != null) return;
            var controller = FindAnyObjectByType<OrderController>();
            if (controller == null) return;
            var asset = Resources.Load<LearningData>(LearningData.ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning("[Learning] Resources/LearningData yok (menü: BilgiAdasi → Seviye → Sorgu + Keşif Defteri verisini üret).");
                return;
            }
            var go = new GameObject(ObjectName);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.SetActive(false);
            var director = go.AddComponent<LearningDirector>();
            director.data = asset;
            director.orderController = controller;
            go.SetActive(true);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            // Play'de yeniden derlemede Awake tekrar çalışmaz; statik Instance burada da kurulur.
            if (Instance == null) Instance = this;
            if (orderController != null) orderController.ReadyToDeliver += OnReadyToDeliver;
            GameEvents.ActiveOrderChanged += OnActiveOrderChanged;
            ViewLocator.Register<IDiscoveryBookCommands>(this);
            ViewLocator.Register<ILessonCommands>(this);
        }

        private void OnDisable()
        {
            if (orderController != null) orderController.ReadyToDeliver -= OnReadyToDeliver;
            GameEvents.ActiveOrderChanged -= OnActiveOrderChanged;
            ViewLocator.Unregister<IDiscoveryBookCommands>(this);
            ViewLocator.Unregister<ILessonCommands>(this);
            if (lessonOpen) ViewLocator.Get<ILessonView>()?.Close();
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
                ViewLocator.Get<IReasoningView>()?.Hide();
            }
            Unblock();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------- sorgu ----------

        private void OnReadyToDeliver(OrderTask task)
        {
            if (task != null) Play(task.Id);
        }

        /// <summary>
        /// Görevin sorgusu + defter kaydı (varsa). Sipariş için ürün hazır olunca, masa görevi için mühürden sonra
        /// (DeskController, istasyondan çıkıldıktan sonra) çağrılır.
        /// </summary>
        public void Play(string taskId)
        {
            var entry = data != null ? data.Find(taskId) : null;
            if (entry == null) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Run(entry));
        }

        private IEnumerator Run(LearningEntry entry)
        {
            yield return new WaitForSeconds(data.DelayAfterReaction);
            routine = null;

            var view = ViewLocator.Get<IReasoningView>();
            if (view == null)
            {
                Notify(entry);
                yield break;
            }

            Block();
            var r = entry.Reasoning;
            if (r != null && !r.IsEmpty)
                view.ShowQuestion(data.MasterName, r.Question, r.Options,
                    index => view.ShowMessage(data.MasterName, r.ReplyFor(index), data.ContinueWord, () => WriteDiscovery(view, entry)));
            else
                WriteDiscovery(view, entry);
        }

        private void WriteDiscovery(IReasoningView view, LearningEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.DiscoveryText))
            {
                Finish(view, entry);
                return;
            }
            view.ShowMessage(data.BookTitle, string.Format(data.WrittenNote, entry.DiscoveryId, entry.DiscoveryText),
                data.ContinueWord, () => Finish(view, entry));
        }

        private void Finish(IReasoningView view, LearningEntry entry)
        {
            view.Hide();
            Unblock();
            Notify(entry);
        }

        private void Notify(LearningEntry entry)
        {
            if (string.IsNullOrEmpty(entry.DiscoveryId)) return;
            ViewLocator.Get<INoticeView>()?.ShowNotice(string.Format(data.WrittenNotice, entry.DiscoveryId));
        }

        // ---------- ders notu (görevden önce) ----------

        /// <summary>Görevin gösterilecek ders notu var mı?</summary>
        public bool HasLesson(string taskId)
        {
            var entry = data != null ? data.Find(taskId) : null;
            return entry != null && entry.Lesson != null && entry.Lesson.HasPages;
        }

        /// <summary>Kapı konuşmasının sonuna eklenecek ustanın satırı (ders notu öncesi).</summary>
        public string LessonIntroLine => data != null ? data.LessonTexts.IntroLine : string.Empty;

        /// <summary>
        /// Görevin ders notunu açar; kapanınca onDone. Not ya da view yoksa false döner ve onDone çağrılmaz
        /// (çağıran akışı kendisi sürdürür). Açıkken IsBusy true, oyuncu kilitli, girdi UI modunda.
        /// </summary>
        public bool ShowLesson(string taskId, Action onDone) => OpenLesson(taskId, false, onDone);

        private bool OpenLesson(string taskId, bool fromMenu, Action onDone)
        {
            if (lessonOpen || !HasLesson(taskId)) return false;
            var view = ViewLocator.Get<ILessonView>();
            if (view == null) return false;

            var texts = data.LessonTexts;
            lessonOpen = true;
            if (!fromMenu)
            {
                Block();
                modeBeforeLesson = GameInput.Mode;
                GameInput.SetMode(InputMode.UI);
            }
            view.Open(data.Find(taskId).Lesson, texts, fromMenu ? texts.CloseWord : texts.StartWord, () =>
            {
                lessonOpen = false;
                if (!fromMenu)
                {
                    GameInput.SetMode(modeBeforeLesson == InputMode.None ? InputMode.Player : modeBeforeLesson);
                    Unblock();
                }
                onDone?.Invoke();
            });
            return true;
        }

        private void OnActiveOrderChanged(TaskData task)
        {
            // Teslimden sonra (null) not menüden kalkar; yeni sipariş gelince onun notu.
            currentTaskId = task != null ? task.Id : null;
        }

        public bool HasCurrentLesson => !string.IsNullOrEmpty(currentTaskId) && HasLesson(currentTaskId);

        /// <summary>Alınmış (kazana verilmiş) sipariş bu mu? Defter, notunu buna göre açık gösterir.</summary>
        public bool IsCurrentTask(string taskId) => !string.IsNullOrEmpty(taskId) && taskId == currentTaskId;

        /// <summary>Esc menüsünden: menü zaten UI modunda ve oyuncu duraklatılmış; kapanınca menüye dönülür.</summary>
        public void OpenCurrentLesson(Action onClosed)
        {
            if (!HasCurrentLesson || !OpenLesson(currentTaskId, true, onClosed)) onClosed?.Invoke();
        }

        // ---------- defter ----------

        /// <summary>Keşif Defteri'ni açar (Esc menüsündeki düğme çağırır). onClosed: kapanınca.</summary>
        public void OpenBook(Action onClosed)
        {
            var book = ViewLocator.Get<IDiscoveryBookView>();
            if (book == null || data == null)
            {
                onClosed?.Invoke();
                return;
            }
            BuildPages();
            book.Open(data.BookTitle, pages, data.EmptyBook, data.CloseWord, onClosed);
        }

        private void BuildPages()
        {
            pages.Clear();
            var save = Save;
            if (save == null) return;
            var entries = data.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || string.IsNullOrWhiteSpace(e.DiscoveryText)) continue;
                bool written = save.IsTaskCompleted(e.TaskId) || save.ReadyTaskId == e.TaskId;
                if (written) pages.Add("<b>" + e.DiscoveryId + "</b>   " + e.DiscoveryText);
            }
        }

        // ---------- oyuncu kilidi ----------

        private void Block()
        {
            IsBusy = true;
            if (blockedUser == null) blockedUser = FindAnyObjectByType<PlayerInteractor>();
            if (blockedUser != null) blockedUser.SetControlEnabled(false);
            GameInput.SetCursorFree(true);
        }

        private void Unblock()
        {
            if (!IsBusy) return;
            IsBusy = false;
            // Masa/istasyon açıkken oyuncu kilitli kalmalı (istasyondan çıkınca StationBase açar).
            if (blockedUser != null && StationBase.Active == null) blockedUser.SetControlEnabled(true);
            GameInput.SetCursorFree(GameInput.Mode != InputMode.Player);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            IsBusy = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}
