using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>Hata Dedektifi verisi (Resources/ErrorHuntData): görev başına turlar.</summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Öğrenme/Hata Dedektifi verisi", fileName = "ErrorHuntData")]
    public sealed class ErrorHuntData : ScriptableObject
    {
        public const string ResourcePath = "ErrorHuntData";

        [SerializeField] private string masterName = "Câbir Usta";
        [SerializeField] private List<ErrorHuntEntry> entries = new List<ErrorHuntEntry>();

        public string MasterName => masterName;
        public IReadOnlyList<ErrorHuntEntry> Entries => entries;

        public ErrorHuntEntry Find(string taskId)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] != null && entries[i].TaskId == taskId) return entries[i];
            return null;
        }

        public void SetEntries(List<ErrorHuntEntry> list) => entries = list;
    }
}
