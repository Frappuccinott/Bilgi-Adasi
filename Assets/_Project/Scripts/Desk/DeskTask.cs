using System.Collections.Generic;
using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.Desk
{
    /// <summary>Masa görevinin hikâye sarmalayıcısı (03 §3).</summary>
    public enum DeskWrapper
    {
        MasterNotebook, // Ustanın Defteri
        LabelTable,     // Etiket Masası
        Letter,         // Mektup
        GuildExam       // Lonca Sınavı
    }

    /// <summary>
    /// Masa görevi (Tepkime Türleri köyü, Seviye 6–10). Görev güvercinle gelir, masada kâğıtta çözülür.
    /// giverLines kâğıdın başındaki mektup; deliveryLines mühürden sonra. Lonca Sınavı: havuzdan randomCount soru.
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Masa/Masa görevi", fileName = "tur_")]
    public sealed class DeskTask : TaskData
    {
        [SerializeField] private DeskWrapper wrapper;
        [Tooltip("Kâğıdın başlığı: Ustanın Defteri, Fırıncının Mektubu …")]
        [SerializeField] private string paperTitle = "";
        [Tooltip("Sırayla sorulur. Lonca Sınavı'nda havuzdur.")]
        [SerializeField] private List<QuestionData> questions = new List<QuestionData>();
        [Tooltip("0 = hepsi sırayla. > 0: havuzdan bu kadar rastgele soru; yanlış soru havuzdan başkasıyla değişir.")]
        [Min(0)] [SerializeField] private int randomCount;

        public DeskWrapper Wrapper => wrapper;
        public string PaperTitle => paperTitle;
        public IReadOnlyList<QuestionData> Questions => questions;
        public int RandomCount => randomCount;
        public bool IsGuildExam => wrapper == DeskWrapper.GuildExam;

#if UNITY_EDITOR
        public static DeskTask Create(string id, string giver, string[] letter, string[] closing, string hint, string realLife,
            DeskWrapper wrapper, string paperTitle, List<QuestionData> questions, int randomCount = 0)
        {
            var t = CreateInstance<DeskTask>();
            t.SetCommon(id, giver, letter, closing, hint, realLife);
            t.wrapper = wrapper;
            t.paperTitle = paperTitle;
            t.questions = questions;
            t.randomCount = randomCount;
            t.name = id;
            return t;
        }

        public void SetQuestions(List<QuestionData> list, int count)
        {
            questions = list;
            randomCount = count;
        }
#endif
    }
}
