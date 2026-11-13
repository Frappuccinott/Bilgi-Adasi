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
            GameEvents.TaskAccepted += HandleTaskAccepted;
            GameEvents.DiscoveryAdded += HandleDiscoveryAdded;
        }

        private void OnDisable()
        {
            GameEvents.TaskCompleted -= HandleTaskCompleted;
            GameEvents.NeighborhoodCompleted -= HandleNeighborhoodCompleted;
            GameEvents.HintOpened -= HandleHintOpened;
            GameEvents.Failed -= HandleFailed;
            GameEvents.TaskAccepted -= HandleTaskAccepted;
            GameEvents.DiscoveryAdded -= HandleDiscoveryAdded;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static SaveData Data => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

        private static void HandleTaskCompleted(string taskId)
        {
            Data?.MarkTaskCompleted(taskId);
            SaveSystem.Instance?.Save();
        }

        private static void HandleNeighborhoodCompleted(string neighborhoodId)
        {
            Data?.MarkNeighborhoodCompleted(neighborhoodId);
            SaveSystem.Instance?.Save();
        }

        private static void HandleHintOpened(string taskId) => Data?.MarkHintOpened(taskId);

        private static void HandleFailed(string taskId) => Data?.IncrementFailCount(taskId);

        private static void HandleTaskAccepted(string taskId)
        {
            Data?.MarkTaskAccepted(taskId);
            SaveSystem.Instance?.Save();
        }

        private static void HandleDiscoveryAdded(string taskId)
        {
            Data?.AddDiscovery(taskId);
            SaveSystem.Instance?.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;
    }
}
