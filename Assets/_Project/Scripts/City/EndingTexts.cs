using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>Prototip kapanışının metinleri (05-icerik-prototip §4). Jenerikteki isimler MenuTexts'ten gelir.</summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Metin/Kapanış Metinleri", fileName = "EndingTexts")]
    public sealed class EndingTexts : ScriptableObject
    {
        [Header("Ustanın veda mektubu (Masa kâğıdı şablonu)")]
        [SerializeField] private string letterFrom = "Câbir Usta’dan mektup";
        [SerializeField] private string letterTitle = "Uyanma vakti";
        [TextArea(3, 8)]
        [SerializeField] private string letterBody =
            "Çırağım, iki köyü ayağa kaldırdın. Tahtana baktım: hiçbir satırda bir atom eksik değil, bir atom fazla değil.\n\n" +
            "Hesaplamalar Köyü seni bekliyor. Ama önce... uyanman gerek.";
        [SerializeField] private string letterSignature = "— Câbir Usta";
        [SerializeField] private string letterContinue = "Devam";

        [Header("Emeği Geçenler")]
        [SerializeField] private string creditsDone = "Ana menü";

        public string LetterFrom => letterFrom;
        public string LetterTitle => letterTitle;
        public string LetterBody => letterBody;
        public string LetterSignature => letterSignature;
        public string LetterContinue => letterContinue;
        public string CreditsDone => creditsDone;
    }
}
