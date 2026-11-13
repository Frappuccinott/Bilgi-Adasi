using System;
using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Tahmin (04-gorev-plani §1 adım 1) ve adım soruları (D-4 ara ürün): görev başlarken ya da zincirin bir adımı
    /// başlarken Câbir Usta sorar. Görev geri alınmaz; yanlışta yanılgı notu + doğru açıklama.
    /// stepIndex 0 = görev başında (tahmin), 1+ = o adım başlarken.
    /// </summary>
    [Serializable]
    public sealed class PredictionEntry
    {
        [SerializeField] private string taskId = "";
        [Tooltip("0: görev başında (tahmin). 1, 2…: zincirde o adım başlarken (ara ürün sorusu).")]
        [Min(0)] [SerializeField] private int stepIndex;
        [SerializeField] private ReasoningPrompt prompt = new ReasoningPrompt();

        public string TaskId => taskId;
        public int StepIndex => stepIndex;
        public ReasoningPrompt Prompt => prompt;
        public string FlagId => "tahmin:" + taskId + ":" + stepIndex;

        public PredictionEntry() { }

        public PredictionEntry(string taskId, int stepIndex, ReasoningPrompt prompt)
        {
            this.taskId = taskId;
            this.stepIndex = stepIndex;
            this.prompt = prompt;
        }
    }

    [CreateAssetMenu(menuName = "Bilgi Adası/Öğrenme/Tahmin verisi", fileName = "PredictionData")]
    public sealed class PredictionData : ScriptableObject
    {
        public const string ResourcePath = "PredictionData";

        [SerializeField] private string masterName = "Câbir Usta";
        [SerializeField] private string continueWord = "Devam";
        [Tooltip("Konuşma/kapı bittikten sonra sorudan önceki bekleme (sn).")]
        [SerializeField] private float delay = 0.8f;
        [SerializeField] private List<PredictionEntry> entries = new List<PredictionEntry>();

        public string MasterName => masterName;
        public string ContinueWord => continueWord;
        public float Delay => delay;
        public IReadOnlyList<PredictionEntry> Entries => entries;

        public PredictionEntry Find(string taskId, int stepIndex)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] != null && entries[i].TaskId == taskId && entries[i].StepIndex == stepIndex) return entries[i];
            return null;
        }

        public void SetEntries(List<PredictionEntry> list) => entries = list;
    }
}
