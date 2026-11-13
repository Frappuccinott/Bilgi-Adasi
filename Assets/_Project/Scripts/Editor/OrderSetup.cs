using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Order;
using BilgiAdasi.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Faz 3 kurulumu: OrderTexts + Denkleştirme siparişleri (05 §2) asset'leri, kazan prefab'ına OrderController,
    /// Town'daki _Placeholder/HUD'a ham sipariş paneli. Var olanın üzerine yazmaz.
    /// </summary>
    public static class OrderSetup
    {
        private const string TextsFolder = "Assets/_Project/Data/Texts";
        private const string TextsPath = TextsFolder + "/OrderTexts.asset";
        private const string TasksFolder = "Assets/_Project/Data/Tasks/Denklestirme";
        private const string CauldronPrefabPath = "Assets/_Project/Prefabs/Stations/Station_Cauldron.prefab";
        private const string TownScenePath = "Assets/_Project/Scenes/Town.unity";

        [MenuItem("BilgiAdasi/Kurulum/5 - Sipariş (kazan) kurulumu", priority = 4)]
        public static void Run()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CauldronPrefabPath) == null)
            {
                EditorUtility.DisplayDialog("Sipariş", "Önce 'Kurulum → 3' çalıştırılmalı.", "Tamam");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var texts = EnsureTexts();
            var tasks = EnsureDenklestirmeTasks();
            if (tasks == null) return;

            ConfigureCauldronPrefab(texts, tasks[0]);
            AddPlaceholderView();
            AssetDatabase.SaveAssets();
            Debug.Log("[BilgiAdasi] Sipariş kurulumu tamam: 5 Denkleştirme siparişi, OrderController, ham sipariş paneli.");
        }

        private static OrderTexts EnsureTexts()
        {
            var existing = AssetDatabase.LoadAssetAtPath<OrderTexts>(TextsPath);
            if (existing != null) return existing;
            EnsureFolder("Assets/_Project/Data", "Texts");
            var texts = ScriptableObject.CreateInstance<OrderTexts>();
            AssetDatabase.CreateAsset(texts, TextsPath);
            return texts;
        }

        private static OrderTask[] EnsureDenklestirmeTasks()
        {
            EnsureFolder("Assets/_Project/Data", "Tasks");
            EnsureFolder("Assets/_Project/Data/Tasks", "Denklestirme");

            var species = LoadAll<SpeciesData>("Assets/_Project/Data/Species", s => s.Formula);
            var reactions = LoadAll<ReactionData>("Assets/_Project/Data/Reactions", r => r.Id);
            if (species.Count == 0 || reactions.Count == 0)
            {
                EditorUtility.DisplayDialog("Sipariş", "Kimya verisi yok. Önce 'Kurulum → 2' çalıştırılmalı.", "Tamam");
                return null;
            }

            var seeded = DenklestirmeSeed.Build(f => species[f], id => reactions[id]);
            var result = new OrderTask[seeded.Length];
            for (int i = 0; i < seeded.Length; i++)
            {
                string path = $"{TasksFolder}/{seeded[i].Id}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<OrderTask>(path);
                if (existing != null)
                {
                    Object.DestroyImmediate(seeded[i]);
                    result[i] = existing;
                    continue;
                }
                AssetDatabase.CreateAsset(seeded[i], path);
                result[i] = seeded[i];
            }
            return result;
        }

        private static void ConfigureCauldronPrefab(OrderTexts texts, OrderTask firstTask)
        {
            var root = PrefabUtility.LoadPrefabContents(CauldronPrefabPath);
            try
            {
                if (!root.TryGetComponent<OrderController>(out var controller))
                    controller = root.AddComponent<OrderController>();
                var so = new SerializedObject(controller);
                so.FindProperty("texts").objectReferenceValue = texts;
                if (so.FindProperty("testTask").objectReferenceValue == null)
                    so.FindProperty("testTask").objectReferenceValue = firstTask;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, CauldronPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AddPlaceholderView()
        {
            var scene = EditorSceneManager.OpenScene(TownScenePath);
            var hud = GameObject.Find("_Placeholder/HUD");
            if (hud == null)
            {
                Debug.LogWarning("[BilgiAdasi] _Placeholder/HUD bulunamadı; ham sipariş paneli eklenmedi.");
                return;
            }
            if (hud.GetComponentInChildren<PlaceholderOrderView>(true) != null) return;

            var go = new GameObject("OrderView", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(hud.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.AddComponent<PlaceholderOrderView>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Dictionary<string, T> LoadAll<T>(string folder, System.Func<T, string> key) where T : Object
        {
            var dict = new Dictionary<string, T>();
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) dict[key(asset)] = asset;
            }
            return dict;
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}")) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
