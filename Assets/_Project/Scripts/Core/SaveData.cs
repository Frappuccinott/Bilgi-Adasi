using System;
using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Kayıt verisi. JsonUtility HashSet/Dictionary serileştiremediği için listeler kullanılır.
    /// Boy kaydedilmez; grade + completedNeighborhoods'tan hesaplanır.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        [Serializable]
        private struct FailEntry
        {
            public string taskId;
            public int count;
        }

        public const int DefaultGrade = 10;

        [SerializeField] private int grade = DefaultGrade;
        [SerializeField] private List<string> completedNeighborhoods = new List<string>();
        [SerializeField] private List<string> completedTasks = new List<string>();
        [SerializeField] private List<FailEntry> failCounts = new List<FailEntry>();
        [SerializeField] private List<string> hintsOpened = new List<string>();
        [SerializeField] private List<string> boardEquations = new List<string>();
        [SerializeField] private List<string> acceptedTasks = new List<string>();
        [SerializeField] private List<string> discoveries = new List<string>();
        [SerializeField] private List<string> flags = new List<string>();

        public int Grade { get => grade; set => grade = value; }
        public IReadOnlyList<string> CompletedNeighborhoods => completedNeighborhoods;
        public IReadOnlyList<string> CompletedTasks => completedTasks;
        public IReadOnlyList<string> BoardEquations => boardEquations;
        /// <summary>Keşif Defteri kayıtları (TaskData.taskId sırasıyla, metin TaskData'dan okunur).</summary>
        public IReadOnlyList<string> Discoveries => discoveries;

        public bool IsTaskAccepted(string taskId) => acceptedTasks.Contains(taskId);
        public void MarkTaskAccepted(string taskId) => AddUnique(acceptedTasks, taskId);
        public void AddDiscovery(string taskId) => AddUnique(discoveries, taskId);
        public bool HasDiscovery(string taskId) => discoveries.Contains(taskId);
        public bool HasFlag(string flag) => flags.Contains(flag);
        public void SetFlag(string flag) => AddUnique(flags, flag);

        public bool IsTaskCompleted(string taskId) => completedTasks.Contains(taskId);
        public bool IsNeighborhoodCompleted(string id) => completedNeighborhoods.Contains(id);
        public bool IsHintOpened(string taskId) => hintsOpened.Contains(taskId);

        public void MarkTaskCompleted(string taskId) => AddUnique(completedTasks, taskId);
        public void MarkNeighborhoodCompleted(string id) => AddUnique(completedNeighborhoods, id);
        public void MarkHintOpened(string taskId) => AddUnique(hintsOpened, taskId);
        public void AddBoardEquation(string richEquation) => AddUnique(boardEquations, richEquation);

        public int GetFailCount(string taskId)
        {
            for (int i = 0; i < failCounts.Count; i++)
                if (failCounts[i].taskId == taskId) return failCounts[i].count;
            return 0;
        }

        public int IncrementFailCount(string taskId)
        {
            for (int i = 0; i < failCounts.Count; i++)
            {
                if (failCounts[i].taskId != taskId) continue;
                var e = failCounts[i];
                e.count++;
                failCounts[i] = e;
                return e.count;
            }
            failCounts.Add(new FailEntry { taskId = taskId, count = 1 });
            return 1;
        }

        private static void AddUnique(List<string> list, string value)
        {
            if (!string.IsNullOrEmpty(value) && !list.Contains(value)) list.Add(value);
        }
    }
}
