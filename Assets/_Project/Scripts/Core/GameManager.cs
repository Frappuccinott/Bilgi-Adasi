using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Oyun geneli durum. Görev/mahalle olaylarını kayda işler. Singleton.
    /// Sistem objesi (_Systems) GameBootstrap tarafından her sahnede kendiliğinden kurulur,
    /// böylece Town sahnesi Boot olmadan da doğrudan Play edilebilir.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.TaskCompleted += HandleTaskCompleted;
            GameEvents.NeighborhoodCompleted += HandleNeighborhoodCompleted;
            GameEvents.HintOpened += HandleHintOpened;
            GameEvents.Failed += HandleFailed;
            GameEvents.AutoSaved += HandleAutoSaved;
        }

        private static void HandleAutoSaved() =>
            ViewLocator.Get<IAutoSaveNoticeView>()?.ShowSaved(MenuTexts.Load().AutoSaved);

        private void OnDisable()
        {
            GameEvents.TaskCompleted -= HandleTaskCompleted;
            GameEvents.NeighborhoodCompleted -= HandleNeighborhoodCompleted;
            GameEvents.HintOpened -= HandleHintOpened;
            GameEvents.Failed -= HandleFailed;
            GameEvents.AutoSaved -= HandleAutoSaved;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static SaveData Data => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

        private static void HandleTaskCompleted(string taskId)
        {
            Data?.MarkTaskCompleted(taskId);
            Instance?.RequestCheckpoint();
        }

        private static void HandleNeighborhoodCompleted(string neighborhoodId)
        {
            Data?.MarkNeighborhoodCompleted(neighborhoodId);
            Instance?.RequestCheckpoint();
        }

        // ---------- Otomatik kayıt ----------

        [Tooltip("Görev bitince otomatik kayıttan önce bekleme (sn): köy tamamlanması, başarımlar, ödeme aynı anda işlensin.")]
        [SerializeField] private float checkpointDelay = 0.5f;

        private bool checkpointPending;

        /// <summary>Her görev sonunda (ve köy bitince) tek otomatik kayıt; aynı anda gelen istekler birleşir.</summary>
        public void RequestCheckpoint()
        {
            if (checkpointPending || !isActiveAndEnabled) return;
            checkpointPending = true;
            StartCoroutine(CheckpointRoutine());
        }

        private System.Collections.IEnumerator CheckpointRoutine()
        {
            yield return new WaitForSecondsRealtime(checkpointDelay);
            checkpointPending = false;
            SaveSystem.Instance?.Checkpoint();
        }

        private static void HandleHintOpened(string taskId) => Data?.MarkHintOpened(taskId);

        private static void HandleFailed(string taskId) => Data?.IncrementFailCount(taskId);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;
    }
}
