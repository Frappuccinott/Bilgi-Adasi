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
        public static event Action<string> TaskAccepted;           // taskId (sakin siparişi verdi)
        public static event Action<string> DiscoveryAdded;         // taskId (Keşif Defteri kaydı)

        public static void RaiseTaskCompleted(string taskId) => TaskCompleted?.Invoke(taskId);
        public static void RaiseNeighborhoodCompleted(string neighborhoodId) => NeighborhoodCompleted?.Invoke(neighborhoodId);
        public static void RaiseHintOpened(string taskId) => HintOpened?.Invoke(taskId);
        public static void RaiseFailed(string taskId) => Failed?.Invoke(taskId);
        public static void RaiseTaskAccepted(string taskId) => TaskAccepted?.Invoke(taskId);
        public static void RaiseDiscoveryAdded(string taskId) => DiscoveryAdded?.Invoke(taskId);

        // Domain reload kapalıyken önceki Play oturumundan kalan dinleyicileri temizler.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            TaskCompleted = null;
            NeighborhoodCompleted = null;
            HintOpened = null;
            Failed = null;
            TaskAccepted = null;
            DiscoveryAdded = null;
        }
    }
}
