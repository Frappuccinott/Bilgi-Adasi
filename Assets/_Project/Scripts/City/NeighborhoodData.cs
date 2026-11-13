using System.Collections.Generic;
using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>Bir köy: görevleri sırayla (köy = konu, her köy = 5 görev).</summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Köy", fileName = "Neighborhood_")]
    public sealed class NeighborhoodData : ScriptableObject
    {
        [Tooltip("NeighborhoodView.neighborhoodId ile aynı: denklestirme")]
        [SerializeField] private string id = "denklestirme";
        [SerializeField] private string displayName = "Denkleştirme Köyü";
        [Tooltip("Ustanın köy girişi (ilk görev alınırken bir kez).")]
        [TextArea(2, 5)] [SerializeField] private string masterIntro;
        [SerializeField] private List<TaskData> tasks = new List<TaskData>();

        public string Id => id;
        public string DisplayName => displayName;
        public string MasterIntro => masterIntro;
        public IReadOnlyList<TaskData> Tasks => tasks;

        public int IndexOf(TaskData task) => tasks.IndexOf(task);
    }
}
