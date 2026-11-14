using BilgiAdasi.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Boy uzama (CLAUDE.md §7): GrowthSettings asset'ini oluşturur, PlayerRig prefab'ına ve açık sahnedeki
    /// her PlayerMotor'a PlayerGrowth ekleyip bağlar. Sahneyi kaydetmez. Tekrar çalıştırılabilir.
    /// </summary>
    public static class GrowthSetup
    {
        private const string SettingsPath = "Assets/_Project/Data/Player/GrowthSettings.asset";
        private const string PrefabPath = "Assets/_Project/Prefabs/PlayerRig.prefab";

        [MenuItem("BilgiAdasi/Kurulum/25 - Boy uzama (PlayerGrowth)", priority = 42)]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[BilgiAdasi] Kurulum 25: önce Play modundan çık.");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Data/Player")) AssetDatabase.CreateFolder("Assets/_Project/Data", "Player");
            var settings = AssetDatabase.LoadAssetAtPath<GrowthSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<GrowthSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            // Prefab
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                var root = PrefabUtility.LoadPrefabContents(PrefabPath);
                foreach (var motor in root.GetComponentsInChildren<PlayerMotor>(true)) Bind(motor, settings, false);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                PrefabUtility.UnloadPrefabContents(root);
            }

            // Açık sahne (Harita'daki oyuncu prefab'dan ayrılmış olabilir)
            int count = 0;
            foreach (var motor in Object.FindObjectsByType<PlayerMotor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Bind(motor, settings, true);
                EditorSceneManager.MarkSceneDirty(motor.gameObject.scene);
                count++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[BilgiAdasi] Kurulum 25: PlayerGrowth prefab'a ve sahnedeki {count} oyuncuya bağlandı. Sahneyi kaydet (Ctrl+S).");
        }

        private static void Bind(PlayerMotor motor, GrowthSettings settings, bool undo)
        {
            var growth = motor.GetComponent<PlayerGrowth>();
            if (growth == null) growth = undo ? Undo.AddComponent<PlayerGrowth>(motor.gameObject) : motor.gameObject.AddComponent<PlayerGrowth>();
            var so = new SerializedObject(growth);
            so.FindProperty("settings").objectReferenceValue = settings;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
