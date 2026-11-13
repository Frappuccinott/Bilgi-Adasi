using System.Collections.Generic;
using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Köy (04 §3): bir konunun 5 görevi. Görevler sırayla açılır; NeighborhoodRunner bu sırayı izler.
    /// Kodda ad "Neighborhood" kalır (CLAUDE.md §8 sözlük).
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Köy", fileName = "Neighborhood_")]
    public sealed class NeighborhoodData : ScriptableObject
    {
        [Tooltip("Kayıtta ve NeighborhoodView'da kullanılan kimlik: denklestirme, tepkime_turleri")]
        [SerializeField] private string id = "";
        [SerializeField] private string title = "";
        [Tooltip("Ustanın köy tanıtımı (01 §5): yalnızca köyün ilk görevi alınırken bir kez söylenir.")]
        [TextArea(2, 5)] [SerializeField] private string[] introLines = new string[0];
        [Tooltip("Sırayla oynanır. Son görev köyün sınavıdır.")]
        [SerializeField] private List<TaskData> tasks = new List<TaskData>();
        [Tooltip("Prototipte iki oynanabilir köy baştan açık (01 §3).")]
        [SerializeField] private bool lockedByDefault;

        public string Id => id;
        public string Title => title;
        public string[] IntroLines => introLines;
        public IReadOnlyList<TaskData> Tasks => tasks;
        public bool LockedByDefault => lockedByDefault;

        /// <summary>Görevin bu köydeki sırası; yoksa -1.</summary>
        public int IndexOf(TaskData task) => tasks.IndexOf(task);

        /// <summary>Editör veri üreticisi için.</summary>
        public static NeighborhoodData Create(string neighborhoodId, string displayTitle, string[] intro, IEnumerable<TaskData> taskList)
        {
            var n = CreateInstance<NeighborhoodData>();
            n.id = neighborhoodId;
            n.title = displayTitle;
            n.introLines = intro;
            n.tasks = new List<TaskData>(taskList);
            n.name = "Neighborhood_" + neighborhoodId;
            return n;
        }
    }
}
