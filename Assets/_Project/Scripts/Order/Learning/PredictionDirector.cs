using System.Collections;
using BilgiAdasi.Core;
using BilgiAdasi.Player;
using BilgiAdasi.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Tahmin katmanı: sipariş alınınca (kapı konuşması bittikten sonra) Câbir Usta görevden önce bir soru sorar;
    /// zincir görevde bir adım başlarken de (D-4 ara ürün: "Harç için su ile neyi birleştireceksin?").
    /// Her soru bir kez sorulur (SaveData flag). Görünüm Sorgu ile aynı (IReasoningView).
    /// Sahneye elle konmaz: kazanı (OrderController) olan sahnede kendiliğinden kurulur, veri Resources/PredictionData.
    /// </summary>
    public sealed class PredictionDirector : MonoBehaviour
    {
        private const string ObjectName = "_PredictionDirector";

        [SerializeField] private PredictionData data;
        [SerializeField] private OrderController orderController;

        private Coroutine routine;
        private PlayerInteractor player;
        private bool blocked;

        public static bool IsBusy { get; private set; }
        public static PredictionDirector Instance { get; private set; }

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
            var asset = Resources.Load<PredictionData>(PredictionData.ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning("[Prediction] Resources/PredictionData yok (menü: BilgiAdasi → Seviye → Atölye katmanları verisini üret).");
                return;
            }
            var go = new GameObject(ObjectName);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.SetActive(false);
            var d = go.AddComponent<PredictionDirector>();
            d.data = asset;
            d.orderController = controller;
            go.SetActive(true);
        }

        private void OnEnable()
        {
            if (Instance == null) Instance = this;
            GameEvents.ActiveOrderChanged += OnActiveOrderChanged;
            if (orderController != null) orderController.StepStarted += OnStepStarted;
        }

        private void OnDisable()
        {
            GameEvents.ActiveOrderChanged -= OnActiveOrderChanged;
            if (orderController != null) orderController.StepStarted -= OnStepStarted;
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
            if (IsBusy) ViewLocator.Get<IReasoningView>()?.Hide();
            Unblock();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------- tetikler ----------

        private void OnActiveOrderChanged(TaskData task)
        {
            if (task is OrderTask order) TryAsk(order, 0);
        }

        private void OnStepStarted(OrderTask task, int stepIndex) => TryAsk(task, stepIndex);

        private void TryAsk(OrderTask task, int stepIndex)
        {
            if (task == null || data == null) return;
            var entry = data.Find(task.Id, stepIndex);
            if (entry == null || entry.Prompt == null || entry.Prompt.IsEmpty) return;
            var save = Save;
            if (save != null && (save.HasFlag(entry.FlagId) || save.IsTaskCompleted(task.Id))) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Run(task, entry));
        }

        private IEnumerator Run(OrderTask task, PredictionEntry entry)
        {
            // Kapı konuşması, istasyon, sorgu ya da menü bitene kadar bekle; sonra kısa bir nefes.
            float calm = 0f;
            while (calm < data.Delay)
            {
                yield return null;
                calm = IsCalm() ? calm + Time.unscaledDeltaTime : 0f;
            }
            routine = null;
            // Bu arada görev değiştiyse ya da ürün hazırsa (kayıttan) sorulmaz.
            if (orderController == null || orderController.ActiveTask != task) yield break;
            if (entry.StepIndex == 0 && (orderController.IsReadyToDeliver || orderController.Session.StepIndex > 0)) yield break;

            var view = ViewLocator.Get<IReasoningView>();
            if (view == null) yield break;

            Save?.SetFlag(entry.FlagId);
            Block();
            var p = entry.Prompt;
            view.ShowQuestion(data.MasterName, p.Question, p.Options, index =>
            {
                view.ShowMessage(data.MasterName, p.ReplyFor(index), data.ContinueWord, () =>
                {
                    view.Hide();
                    Unblock();
                });
            });
        }

        private bool IsCalm()
        {
            if (player == null) player = FindAnyObjectByType<PlayerInteractor>();
            if (player == null || !player.ControlEnabled) return false;
            if (StationBase.Active != null || LearningDirector.IsBusy) return false;
            if (GameInput.Mode != InputMode.Player || Time.timeScale <= 0f) return false;
            return true;
        }

        // ---------- oyuncu kilidi (LearningDirector ile aynı) ----------

        private void Block()
        {
            IsBusy = true;
            blocked = true;
            if (player != null) player.SetControlEnabled(false);
            GameInput.SetCursorFree(true);
        }

        private void Unblock()
        {
            IsBusy = false;
            if (!blocked) return;
            blocked = false;
            if (player != null && StationBase.Active == null) player.SetControlEnabled(true);
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
