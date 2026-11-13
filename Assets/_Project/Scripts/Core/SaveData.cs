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

        /// <summary>Bir yığın: envanter slotu ya da raf yeri. Madde formülüyle saklanır.</summary>
        [Serializable]
        public struct StackEntry
        {
            public string key;      // envanter: "0".."5", raf: "<rafId>/<yer>"
            public string species;  // SpeciesData.Formula
            public int count;
        }

        public const int DefaultGrade = 10;

        [SerializeField] private bool economyStarted;
        [SerializeField] private int money;
        [SerializeField] private List<StackEntry> inventory = new List<StackEntry>();
        [SerializeField] private List<StackEntry> shelves = new List<StackEntry>();
        [Tooltip("Tepkimesi bitmiş, teslim bekleyen sipariş (yeniden açılışta kaybolmasın).")]
        [SerializeField] private string readyTaskId = "";

        /// <summary>İlk açılışta false: başlangıç kesesi verilir.</summary>
        public bool EconomyStarted { get => economyStarted; set => economyStarted = value; }
        public int Money { get => money; set => money = value; }
        public List<StackEntry> Inventory => inventory;
        public string ReadyTaskId { get => readyTaskId; set => readyTaskId = value ?? ""; }

        public bool TryGetShelf(string key, out StackEntry entry)
        {
            for (int i = 0; i < shelves.Count; i++)
                if (shelves[i].key == key) { entry = shelves[i]; return true; }
            entry = default;
            return false;
        }

        /// <summary>Raf yerini yazar; species boşsa yeri siler.</summary>
        public void SetShelf(string key, string species, int count)
        {
            for (int i = shelves.Count - 1; i >= 0; i--)
                if (shelves[i].key == key) shelves.RemoveAt(i);
            if (!string.IsNullOrEmpty(species) && count > 0)
                shelves.Add(new StackEntry { key = key, species = species, count = count });
        }

        [Tooltip("Pazarda satılan adetler (stoklu satırlar için). key = katalog adı, species = formül.")]
        [SerializeField] private List<StackEntry> marketSold = new List<StackEntry>();

        public int GetMarketSold(string catalogId, string species)
        {
            for (int i = 0; i < marketSold.Count; i++)
                if (marketSold[i].key == catalogId && marketSold[i].species == species) return marketSold[i].count;
            return 0;
        }

        public void AddMarketSold(string catalogId, string species, int amount)
        {
            for (int i = 0; i < marketSold.Count; i++)
            {
                if (marketSold[i].key != catalogId || marketSold[i].species != species) continue;
                var e = marketSold[i];
                e.count += amount;
                marketSold[i] = e;
                return;
            }
            marketSold.Add(new StackEntry { key = catalogId, species = species, count = amount });
        }

        /// <summary>Dünyada yeri değişen bir nesnenin konumu (bisiklet). key = nesnenin kayıt anahtarı.</summary>
        [Serializable]
        public struct PoseEntry
        {
            public string key;
            public Vector3 position;
            public float yaw;
            /// <summary>Kayıt anında oyuncu üstündeydi (bisiklet): yüklemede oyuncu üstünde başlar.</summary>
            public bool ridden;
        }

        [SerializeField] private List<PoseEntry> poses = new List<PoseEntry>();

        public bool TryGetPose(string key, out PoseEntry entry)
        {
            if (poses != null)
                for (int i = 0; i < poses.Count; i++)
                    if (poses[i].key == key) { entry = poses[i]; return true; }
            entry = default;
            return false;
        }

        public void SetPose(string key, Vector3 position, float yaw, bool ridden = false)
        {
            poses ??= new List<PoseEntry>();
            var entry = new PoseEntry { key = key, position = position, yaw = yaw, ridden = ridden };
            for (int i = 0; i < poses.Count; i++)
                if (poses[i].key == key) { poses[i] = entry; return; }
            poses.Add(entry);
        }

        [SerializeField] private int grade = DefaultGrade;
        [SerializeField] private List<string> completedNeighborhoods = new List<string>();
        [SerializeField] private List<string> completedTasks = new List<string>();
        [SerializeField] private List<FailEntry> failCounts = new List<FailEntry>();
        [SerializeField] private List<string> hintsOpened = new List<string>();
        [SerializeField] private List<string> boardEquations = new List<string>();

        public int Grade { get => grade; set => grade = value; }
        public IReadOnlyList<string> CompletedNeighborhoods => completedNeighborhoods;
        public IReadOnlyList<string> CompletedTasks => completedTasks;
        public IReadOnlyList<string> BoardEquations => boardEquations;

        public bool IsTaskCompleted(string taskId) => completedTasks.Contains(taskId);
        public bool IsNeighborhoodCompleted(string id) => completedNeighborhoods.Contains(id);
        public bool IsHintOpened(string taskId) => hintsOpened.Contains(taskId);

        public void MarkTaskCompleted(string taskId) => AddUnique(completedTasks, taskId);
        public void MarkNeighborhoodCompleted(string id) => AddUnique(completedNeighborhoods, id);
        public void MarkHintOpened(string taskId) => AddUnique(hintsOpened, taskId);
        public void AddBoardEquation(string richEquation) => AddUnique(boardEquations, richEquation);

        // Yalnızca geliştirici kısayolları (görev atlama geri de gidebilir).
        public void UnmarkTaskCompleted(string taskId) => completedTasks.Remove(taskId);
        public void UnmarkNeighborhoodCompleted(string id) => completedNeighborhoods.Remove(id);
        public void RemoveBoardEquation(string richEquation) => boardEquations.Remove(richEquation);

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
