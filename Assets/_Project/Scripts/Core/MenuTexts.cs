using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>Ana menü ve otomatik kayıt bildiriminin metinleri (kodda sabit Türkçe metin olmaz).</summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Metin/Menü Metinleri", fileName = "MenuTexts")]
    public sealed class MenuTexts : ScriptableObject
    {
        [Header("Ana menü")]
        [SerializeField] private string title = "Bilgi Adası";
        [SerializeField] private string subtitle = "Kimya şehrinde bir çırağın defteri";
        [SerializeField] private string play = "Oyna";
        [SerializeField] private string continueGame = "Devam et";
        [SerializeField] private string quit = "Çık";
        [Tooltip("Kayıt varken Oyna'ya basınca sorulur.")]
        [TextArea(1, 3)] [SerializeField] private string newGameWarning = "Yeni oyun başlarsa kayıtlı ilerlemen silinir.";
        [SerializeField] private string confirmNewGame = "Yeni oyun";
        [SerializeField] private string cancel = "Vazgeç";

        [Header("Oyun içi")]
        [SerializeField] private string autoSaved = "Otomatik kaydedildi…";

        public string Title => title;
        public string Subtitle => subtitle;
        public string Play => play;
        public string ContinueGame => continueGame;
        public string Quit => quit;
        public string NewGameWarning => newGameWarning;
        public string ConfirmNewGame => confirmNewGame;
        public string Cancel => cancel;
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
