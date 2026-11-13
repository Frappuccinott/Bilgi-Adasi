using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Sahne bağlantı noktası: mahallenin önce/sonra hali.
    /// Çocuklar: Before (bitmeden), After (bitince), Parts/Part_1…Part_5 (isteğe bağlı, görev başına bir parça).
    /// Alanlar boşsa Awake'te bu adlarla bulunur.
    /// </summary>
    public sealed class NeighborhoodView : MonoBehaviour
    {
        [Tooltip("NeighborhoodData.id ile aynı: denklestirme, tepkime_turleri")]
        [SerializeField] private string neighborhoodId = "denklestirme";
        [SerializeField] private GameObject before;
        [SerializeField] private GameObject after;
        [Tooltip("Görev başına açılan parçalar (sırayla). Boş bırakılabilir.")]
        [SerializeField] private GameObject[] parts = new GameObject[0];

        public string NeighborhoodId => neighborhoodId;

        private void Awake()
        {
            if (before == null) before = FindChild("Before");
            if (after == null) after = FindChild("After");
            if (parts == null || parts.Length == 0)
            {
                var root = transform.Find("Parts");
                if (root != null)
                {
                    parts = new GameObject[root.childCount];
                    for (int i = 0; i < root.childCount; i++) parts[i] = root.GetChild(i).gameObject;
                }
            }
        }

        private void OnEnable() => GameEvents.NeighborhoodCompleted += OnNeighborhoodCompleted;
        private void OnDisable() => GameEvents.NeighborhoodCompleted -= OnNeighborhoodCompleted;

        private void Start()
        {
            var data = SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;
            bool done = data != null && data.IsNeighborhoodCompleted(neighborhoodId);
            SetCompleted(done);
            if (!done) SetProgress(0);
        }

        /// <summary>İlk completedTasks kadar parçayı açar (NeighborhoodRunner çağırır).</summary>
        public void SetProgress(int completedTasks)
        {
            if (parts == null) return;
            for (int i = 0; i < parts.Length; i++)
                if (parts[i] != null) parts[i].SetActive(i < completedTasks);
        }

        public void SetCompleted(bool completed)
        {
            if (before != null) before.SetActive(!completed);
            if (after != null) after.SetActive(completed);
            if (completed && parts != null) SetProgress(parts.Length);
        }

        private void OnNeighborhoodCompleted(string id)
        {
            if (id == neighborhoodId) SetCompleted(true);
        }

        private GameObject FindChild(string childName)
        {
            var t = transform.Find(childName);
            return t != null ? t.gameObject : null;
        }
    }
}
