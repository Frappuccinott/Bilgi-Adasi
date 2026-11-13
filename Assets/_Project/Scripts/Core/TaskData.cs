using System;
using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Ustanın Sorgusu (04-gorev-plani §2.3): başarıdan sonra 3 açıklama, biri doğru.
    /// Yanlışta görev geri alınmaz; usta yanılgıyı tek cümleyle düzeltir.
    /// </summary>
    [Serializable]
    public sealed class ReasoningPrompt
    {
        [TextArea(1, 3)] [SerializeField] private string question;
        [SerializeField] private string[] options = new string[3];
        [SerializeField] private int correctIndex;
        [Tooltip("Her seçenek için ustanın tek cümlelik cevabı (doğru olanınki 'Aynen öyle.' gibi).")]
        [SerializeField] private string[] replies = new string[3];

        public bool IsEmpty => string.IsNullOrWhiteSpace(question) || options == null || options.Length == 0;
        public string Question => question;
        public string[] Options => options;
        public int CorrectIndex => correctIndex;
        public string ReplyFor(int index) =>
            replies != null && index >= 0 && index < replies.Length ? replies[index] : string.Empty;
    }

    /// <summary>
    /// Bir görev (sipariş ya da masa). Oyuncuya görünen bütün metin burada durur (CLAUDE.md §8).
    /// </summary>
    public abstract class TaskData : ScriptableObject
    {
        [Tooltip("Kayıt anahtarı: D-1, D-2 … T-1 …")]
        [SerializeField] private string taskId = "D-1";
        [SerializeField] private string title = "Kuru Çeşme";

        [Header("Sakin")]
        [SerializeField] private string residentName = "Muhtar Yusuf";
        [TextArea(2, 4)] [SerializeField] private string requestLine;
        [Tooltip("Görev alınmış ama bitmemişken sakinle yeniden konuşunca.")]
        [TextArea(1, 3)] [SerializeField] private string reminderLine;
        [TextArea(1, 3)] [SerializeField] private string deliverLine;
        [Tooltip("Görev bittikten sonra sakinle konuşunca.")]
        [TextArea(1, 3)] [SerializeField] private string afterLine;

        [Header("Öğrenme")]
        [TextArea(1, 3)] [SerializeField] private string hint;
        [TextArea(1, 3)] [SerializeField] private string realLife;
        [SerializeField] private ReasoningPrompt reasoning = new ReasoningPrompt();
        [Tooltip("Keşif Defteri kayıt numarası: K-1")]
        [SerializeField] private string discoveryId = "K-1";
        [TextArea(2, 4)] [SerializeField] private string discoveryText;

        public string TaskId => taskId;
        public string Title => title;
        public string ResidentName => residentName;
        public string RequestLine => requestLine;
        public string ReminderLine => reminderLine;
        public string DeliverLine => deliverLine;
        public string AfterLine => afterLine;
        public string Hint => hint;
        public string RealLife => realLife;
        public ReasoningPrompt Reasoning => reasoning;
        public string DiscoveryId => discoveryId;
        public string DiscoveryText => discoveryText;
    }
}
