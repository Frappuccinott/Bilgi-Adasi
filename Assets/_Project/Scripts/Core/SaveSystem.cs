using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Kayıt: JSON → PlayerPrefs (tek anahtar). Singleton.
    /// Oyun sırasında Data canlı tutulur (sistemler doğrudan yazar), diske yalnızca kontrol noktasında yazılır:
    /// her görev bitince otomatik kayıt (Checkpoint). Kayıt almadan çıkılırsa son görev anına dönülür (karar 14.11).
    /// </summary>
    public sealed class SaveSystem : MonoBehaviour
    {
        private const string PrefsKey = "BilgiAdasi.Save.v1";

        public static SaveSystem Instance { get; private set; }

        public SaveData Data { get; private set; }

        /// <summary>Diskte devam edilebilecek bir kayıt var mı (ana menüde "Devam et").</summary>
        public static bool HasSave => PlayerPrefs.HasKey(PrefsKey);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Diskteki son kaydı belleğe alır (ana menüde "Devam et"; yoksa boş kayıt).</summary>
        public void Load()
        {
            string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
            Data = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        }

        /// <summary>Belleği diske yazar (olay yayınlamaz). Oyun içinde Checkpoint kullanın.</summary>
        public void Save()
        {
            if (Data == null) return;
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Otomatik kayıt: dünyadaki canlı durum (oyuncu, bisiklet konumu …) GameEvents.BeforeSave ile Data'ya yazılır,
        /// sonra diske; ardından GameEvents.AutoSaved (ekranda "Otomatik kaydedildi").
        /// </summary>
        public void Checkpoint()
        {
            if (Data == null) return;
            GameEvents.RaiseBeforeSave(Data);
            Save();
            GameEvents.RaiseAutoSaved();
        }

        /// <summary>Yeni oyun: diskteki kaydı siler, boş kayıtla başlar (ilk görev bitene kadar "Devam et" kapalı).</summary>
        public void StartNewGame()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
            Data = new SaveData();
        }

        /// <summary>Kaydı siler ve boş kayıtla başlar (geliştirici kısayolu).</summary>
        public void ResetSave()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            Data = new SaveData();
            Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;
    }
}
