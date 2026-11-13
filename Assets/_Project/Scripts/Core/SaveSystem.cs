using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>Kayıt: JSON → PlayerPrefs (tek anahtar). Singleton.</summary>
    public sealed class SaveSystem : MonoBehaviour
    {
        private const string PrefsKey = "BilgiAdasi.Save.v1";

        public static SaveSystem Instance { get; private set; }

        public SaveData Data { get; private set; }

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

        private void OnApplicationQuit() => Save();

        public void Load()
        {
            string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
            Data = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        }

        public void Save()
        {
            if (Data == null) return;
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        /// <summary>Kaydı siler ve boş kayıtla başlar (geliştirici kısayolu R).</summary>
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
