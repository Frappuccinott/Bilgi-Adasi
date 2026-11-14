using System;
using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>Ana menü (Figma Ekran/Giriş + Ekran/Emeği Geçenler) ve otomatik kayıt bildiriminin metinleri.</summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Metin/Menü Metinleri", fileName = "MenuTexts")]
    public sealed class MenuTexts : ScriptableObject
    {
        [Serializable]
        public struct CreditEntry
        {
            public string role;
            public string name;
        }

        [Header("Ana menü")]
        [SerializeField] private string title = "Bilgi Adası";
        [SerializeField] private string continueLabel = "Devam Et";
        [SerializeField] private string newGame = "Yeni Oyun";
        [SerializeField] private string settings = "Ayarlar";
        [SerializeField] private string credits = "Emeği Geçenler";
        [SerializeField] private string quitLabel = "Çıkış";
        [Tooltip("Kayıt özetinde köy adı: {0} = köyün adı (NeighborhoodData.Title).")]
        [SerializeField] private string placeFormat = "{0} Köyü";
        [Tooltip("Kayıt özetinde ilerleme: {0} biten, {1} toplam görev.")]
        [SerializeField] private string progressFormat = "{0}/{1}";
        [TextArea(1, 3)]
        [SerializeField] private string disclaimer =
            "Bu atölyedeki tepkimeler bir oyundur. Gerçek kimyasallar yalnızca okul laboratuvarında, öğretmen gözetiminde denenir.";
        [SerializeField] private string hintSelect = "Seç";
        [SerializeField] private string hintBack = "Geri";

        [Header("Yeni oyun onayı")]
        [Tooltip("Kayıt varken Yeni Oyun'a basınca sorulur.")]
        [TextArea(1, 3)] [SerializeField] private string newGameWarning = "Yeni oyun başlarsa kayıtlı ilerlemen silinir.";
        [SerializeField] private string confirmNewGame = "Yeni oyun";
        [SerializeField] private string cancel = "Vazgeç";

        [Header("Emeği Geçenler")]
        [Tooltip("Rol boşsa yalnızca isim yazılır (karar 14.11: jenerikte yalnızca isimler).")]
        [SerializeField] private List<CreditEntry> creditEntries = new List<CreditEntry>
        {
            new CreditEntry { name = "Mustafa Akbaş" },
            new CreditEntry { name = "Mehmet Emin Çoban" },
            new CreditEntry { name = "Efe Karacabey" },
        };
        [Tooltip("Jenerik altı not. Boşsa gösterilmez.")]
        [TextArea(1, 3)]
        [SerializeField] private string creditsNote = "";
        [SerializeField] private string creditsTeaser = "Devam edecek: Hesaplamalar Köyü";

        [Header("Oyun içi")]
        [SerializeField] private string autoSaved = "Otomatik kaydedildi…";

        public string Title => title;
        public string ContinueGame => continueLabel;
        public string NewGame => newGame;
        public string Settings => settings;
        public string Credits => credits;
        public string Quit => quitLabel;
        public string PlaceFormat => placeFormat;
        public string ProgressFormat => progressFormat;
        public string Disclaimer => disclaimer;
        public string HintSelect => hintSelect;
        public string HintBack => hintBack;
        public string NewGameWarning => newGameWarning;
        public string ConfirmNewGame => confirmNewGame;
        public string Cancel => cancel;
        public IReadOnlyList<CreditEntry> CreditEntries => creditEntries;
        public string CreditsNote => creditsNote;
        public string CreditsTeaser => creditsTeaser;
        public string AutoSaved => autoSaved;

        /// <summary>Resources/MenuTexts (Data/Resources). Yoksa varsayılan metinlerle geçici örnek.</summary>
        public static MenuTexts Load()
        {
            if (cached != null) return cached;
            cached = Resources.Load<MenuTexts>("MenuTexts");
            if (cached == null) cached = CreateInstance<MenuTexts>();
            return cached;
        }

        private static MenuTexts cached;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => cached = null;
    }
}
