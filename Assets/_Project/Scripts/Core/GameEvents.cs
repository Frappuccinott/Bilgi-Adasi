using System;
using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Sistemler arası tek iletişim noktası. Denetleyiciler olay yayınlar; görseller, kayıt ve istatistik dinler.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<string> TaskCompleted;          // taskId
        public static event Action<string> NeighborhoodCompleted;  // neighborhoodId
        public static event Action<string> HintOpened;             // taskId
        public static event Action<string> Failed;                 // taskId (köpürme / yanlış)
        /// <summary>Köy ilerlemesi: neighborhoodId, biten görev sayısı, toplam görev. Açılışta ve her görevde.</summary>
        public static event Action<string, int, int> NeighborhoodProgress;
        /// <summary>Alınmış sipariş değişti: yeni görev ya da null (teslim edildi / bırakıldı). Pazar dinler.</summary>
        public static event Action<TaskData> ActiveOrderChanged;

        /// <summary>Şu an alınmış (teslim edilmemiş) sipariş; yoksa null. Sonradan açılan dinleyiciler için.</summary>
        public static TaskData ActiveOrder { get; private set; }

        /// <summary>Oyuncunun elinde (seçili slotta) madde var mı değişti. Raf boş yerleri yeşil işaretler.</summary>
        public static event Action<bool> HandItemChanged;

        /// <summary>Şu an elde madde var mı (sonradan açılan dinleyiciler için).</summary>
        public static bool HasHandItem { get; private set; }

        /// <summary>Başarım açıldı (achievementId). Ses/efekt dinleyebilir; bildirimi AchievementController gösterir.</summary>
        public static event Action<string> AchievementUnlocked;

        /// <summary>Duraklatma menüsü açıldı (true) / kapandı (false).</summary>
        public static event Action<bool> PauseChanged;

        /// <summary>Ocak yakıldı: true = tepkime başladı, false = kazan köpürüyor. Titreşim/ses dinler.</summary>
        public static event Action<bool> StoveFired;

        public static void RaiseStoveFired(bool reacting) => StoveFired?.Invoke(reacting);
        public static void RaiseAchievementUnlocked(string achievementId) => AchievementUnlocked?.Invoke(achievementId);
        public static void RaisePauseChanged(bool paused) => PauseChanged?.Invoke(paused);

        public static void RaiseHandItemChanged(bool hasItem)
        {
            if (hasItem == HasHandItem) return;
            HasHandItem = hasItem;
            HandItemChanged?.Invoke(hasItem);
        }

        public static void RaiseActiveOrderChanged(TaskData task)
        {
            ActiveOrder = task;
            ActiveOrderChanged?.Invoke(task);
        }

        public static void RaiseNeighborhoodProgress(string neighborhoodId, int completed, int total) =>
            NeighborhoodProgress?.Invoke(neighborhoodId, completed, total);
        public static void RaiseTaskCompleted(string taskId) => TaskCompleted?.Invoke(taskId);
        public static void RaiseNeighborhoodCompleted(string neighborhoodId) => NeighborhoodCompleted?.Invoke(neighborhoodId);
        public static void RaiseHintOpened(string taskId) => HintOpened?.Invoke(taskId);
        public static void RaiseFailed(string taskId) => Failed?.Invoke(taskId);

        // Domain reload kapalıyken önceki Play oturumundan kalan dinleyicileri temizler.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            TaskCompleted = null;
            NeighborhoodCompleted = null;
            HintOpened = null;
            Failed = null;
            NeighborhoodProgress = null;
            ActiveOrderChanged = null;
            ActiveOrder = null;
            HandItemChanged = null;
            HasHandItem = false;
            AchievementUnlocked = null;
            PauseChanged = null;
            StoveFired = null;
        }
    }
}
