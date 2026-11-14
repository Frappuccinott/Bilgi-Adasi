using System.IO;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BilgiAdasi.EditorTools
{
    /// <summary>Tek seferlik proje kurulumu: layer'lar, ürün adı, sahneler, Build Settings, kimya verisi.</summary>
    public static class ProjectSetup
    {
        private const string ScenesFolder = "Assets/_Project/Scenes";
        private const string DataFolder = "Assets/_Project/Data";

        public const int InteractableLayer = 6;
        public const int PlayerLayer = 7;

        [MenuItem("BilgiAdasi/Kurulum/1 - Proje ayarları ve sahneler", priority = 0)]
        public static void SetupProject()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            SetLayer(InteractableLayer, "Interactable");
            SetLayer(PlayerLayer, "Player");

            PlayerSettings.productName = "Bilgi Adası";

            // Proje geneli (project-wide) aksiyon kullanılmıyor; üretilen BilgiAdasiControls sınıfı kullanılıyor.
            EditorBuildSettings.RemoveConfigObject("com.unity.input.settings.actions");

            // Tek oyun sahnesi: Harita. Sistemler (GameManager, SaveSystem, SceneFlow) GameBootstrap'ta kurulur.
            string game = EnsureScene(SceneFlow.GameScene, BuildTownScene);

            EditorBuildSettings.scenes = new[]
            {
                // Ana menü (Kurulum 20) varsa ilk sahne o.
                new EditorBuildSettingsScene(SceneFlow.MenuScenePath, AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneFlow.MenuScenePath) != null),
                new EditorBuildSettingsScene(game, true),
            };

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(game);
            Debug.Log("[BilgiAdasi] Kurulum tamam: layer'lar, ürün adı, Harita sahnesi ve Build Settings.");
        }

        [MenuItem("BilgiAdasi/Kurulum/2 - Kimya verisini oluştur (05 §1)", priority = 1)]
        public static void CreateChemistryData()
        {
            string elements = EnsureFolder(DataFolder, "Elements");
            string species = EnsureFolder(DataFolder, "Species");
            string reactions = EnsureFolder(DataFolder, "Reactions");

            if (AssetDatabase.FindAssets("t:ElementData", new[] { elements }).Length > 0)
            {
                EditorUtility.DisplayDialog("Kimya verisi",
                    "Data/Elements içinde zaten element var. Elle yapılan değişiklikler kaybolmasın diye işlem durduruldu.\n" +
                    "Yeniden üretmek için Elements, Species ve Reactions klasörlerini boşaltın.", "Tamam");
                return;
            }

            var seed = PrototypeChemistrySeed.Build();
            foreach (var pair in seed.Elements)
                AssetDatabase.CreateAsset(pair.Value, $"{elements}/Element_{pair.Key}.asset");
            foreach (var pair in seed.Species)
                AssetDatabase.CreateAsset(pair.Value, $"{species}/Species_{pair.Key}.asset");
            foreach (var pair in seed.Reactions)
                AssetDatabase.CreateAsset(pair.Value, $"{reactions}/{pair.Key}.asset");

            AssetDatabase.SaveAssets();
            Debug.Log($"[BilgiAdasi] Kimya verisi oluşturuldu: {seed.Elements.Count} element, {seed.Species.Count} tür, {seed.Reactions.Count} tepkime.");
        }

        private static string EnsureScene(string name, System.Action build)
        {
            string path = $"{ScenesFolder}/{name}.unity";
            if (File.Exists(path)) return path;

            EnsureFolder("Assets/_Project", "Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            build();
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static void BuildTownScene()
        {
            var light = new GameObject("Directional Light", typeof(Light));
            light.GetComponent<Light>().type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Test yer tutucusu: Mustafa'nın sahnesi gelince silinir.
            var placeholder = new GameObject("_Placeholder");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(placeholder.transform);
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
        }

        private static string EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
            return path;
        }

        private static void SetLayer(int index, string layerName)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            var slot = layers.GetArrayElementAtIndex(index);
            if (!string.IsNullOrEmpty(slot.stringValue) && slot.stringValue != layerName)
            {
                Debug.LogWarning($"[BilgiAdasi] Layer {index} zaten '{slot.stringValue}'. '{layerName}' yazılmadı.");
                return;
            }
            slot.stringValue = layerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
