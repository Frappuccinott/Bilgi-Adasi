using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// Boy uzama tablosu ve notları (CLAUDE.md §7). Boy kaydedilmez; sınıf + biten köylerden hesaplanır.
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Oyuncu/Büyüme Ayarları", fileName = "GrowthSettings")]
    public sealed class GrowthSettings : ScriptableObject
    {
        [System.Serializable]
        public struct GradeHeight
        {
            [Tooltip("Sınıf (9–12).")]
            public int grade;
            [Tooltip("Bu sınıfa başlarken ayakta boy (m). Göz = boy − PlayerMovementSettings.EyeOffsetFromTop.")]
            public float height;
        }

        [Tooltip("Sınıf → başlangıç boyu. Sıralı yazılır; köy artışı bir sonraki sınıfın boyunu geçemez.")]
        [SerializeField] private List<GradeHeight> grades = new List<GradeHeight>
        {
            new GradeHeight { grade = 9, height = 1.60f },
            new GradeHeight { grade = 10, height = 1.66f },
            new GradeHeight { grade = 11, height = 1.71f },
            new GradeHeight { grade = 12, height = 1.75f },
        };

        [Tooltip("Her biten köy için artış (m).")]
        [SerializeField] private float perNeighborhood = 0.01f;

        [Tooltip("Boy artışı saymayan köyler (ör. çıraklık: öğretici bölüm).")]
        [SerializeField] private List<string> ignoredNeighborhoods = new List<string> { "ciraklik" };

        [Tooltip("Yumuşak geçiş süresi (sn).")]
        [SerializeField] private float transitionSeconds = 2f;

        [Tooltip("Köy bitince notun gecikmesi (sn). Köy kutlamasıyla üst üste binmesin.")]
        [SerializeField] private float noticeDelay = 2.5f;

        [Header("Oyuncuya görünen metin")]
        [TextArea(1, 2)] [SerializeField] private string grewNotice = "Biraz uzadın gibi…";
        [TextArea(1, 2)] [SerializeField] private string gradeUpNotice = "Bir sınıf daha atladın. Boyun da uzadı!";

        public float TransitionSeconds => Mathf.Max(0.01f, transitionSeconds);
        public float NoticeDelay => Mathf.Max(0f, noticeDelay);
        public string GrewNotice => grewNotice;
        public string GradeUpNotice => gradeUpNotice;

        public bool Counts(string neighborhoodId) =>
            !string.IsNullOrEmpty(neighborhoodId) && !ignoredNeighborhoods.Contains(neighborhoodId);

        /// <summary>Sınıf ve sayılan köy sayısından ayakta boy. Bir sonraki sınıfın boyunu geçmez.</summary>
        public float HeightFor(int grade, int countedNeighborhoods)
        {
            if (grades.Count == 0) return 1.66f;
            int i = 0;
            while (i < grades.Count - 1 && grades[i].grade < grade) i++;
            float baseHeight = grades[i].height;
            float grown = baseHeight + Mathf.Max(0, countedNeighborhoods) * perNeighborhood;
            // Son sınıfta (12) köy artışı yok; diğerlerinde bir sonraki sınıfın boyu tavan.
            return i + 1 < grades.Count ? Mathf.Min(grown, grades[i + 1].height) : baseHeight;
        }
    }
}
