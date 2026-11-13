using System.Collections.Generic;
using BilgiAdasi.City;
using BilgiAdasi.Core;
using BilgiAdasi.Order;
using BilgiAdasi.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Faz 4 kurulumu: köy verisi (Denkleştirme, 05 §2), NeighborhoodTexts, Town'a NeighborhoodRunner
    /// (+ DialogueDirector, DevShortcuts) ve ham konuşma balonu. Kazanın test görevi temizlenir: görev kapıda alınır.
    /// Var olanın üzerine yazmaz; tekrar çalıştırmak güvenlidir.
    /// </summary>
    public static class NeighborhoodSetup
    {
        private const string TextsPath = "Assets/_Project/Data/Texts/NeighborhoodTexts.asset";
        private const string NeighborhoodsFolder = "Assets/_Project/Data/Neighborhoods";
        private const string TasksFolder = "Assets/_Project/Data/Tasks/Denklestirme";
        private const string CauldronPrefabPath = "Assets/_Project/Prefabs/Stations/Station_Cauldron.prefab";
        private const string TownScenePath = "Assets/_Project/Scenes/Town.unity";
        private const string RunnerObjectName = "NeighborhoodRunner";

        // 05-icerik-prototip.md §2 — içerik değişirse önce 05 güncellenir.
        private static readonly string[] DenklestirmeIntro =
        {
            "Bu köyde herkesin derdi bir 'kaç tane' sorusu. Kazana ne girerse o çıkar; ne bir eksik, ne bir fazla.",
            "Kavanozlar mühürlü, içlerini değiştiremezsin. Sen yalnızca kaç tane koyacağını seçersin."
        };

        [MenuItem("BilgiAdasi/Kurulum/6 - Köy akışı (kapı, konuşma, kısayollar)", priority = 5)]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var tasks = LoadDenklestirmeTasks();
            if (tasks == null) return;

            var texts = EnsureTexts();
            var denklestirme = EnsureNeighborhood("denklestirme", "Denkleştirme", DenklestirmeIntro, tasks);
            SetUpTown(texts, denklestirme);
            AssetDatabase.SaveAssets();
            Debug.Log("[BilgiAdasi] Köy akışı kurulumu tamam: Denkleştirme köyü, NeighborhoodRunner, ham konuşma balonu.");
        }

        private static List<TaskData> LoadDenklestirmeTasks()
        {
            var result = new List<TaskData>();
            for (int i = 1; i <= 5; i++)
            {
                var task = AssetDatabase.LoadAssetAtPath<OrderTask>($"{TasksFolder}/denk_{i}.asset");
                if (task == null)
                {
                    EditorUtility.DisplayDialog("Köy akışı", $"denk_{i} bulunamadı. Önce 'Kurulum → 5' çalıştırılmalı.", "Tamam");
                    return null;
                }
                result.Add(task);
            }
            return result;
        }

        private static NeighborhoodTexts EnsureTexts()
        {
            var existing = AssetDatabase.LoadAssetAtPath<NeighborhoodTexts>(TextsPath);
            if (existing != null) return existing;
            var texts = ScriptableObject.CreateInstance<NeighborhoodTexts>();
            AssetDatabase.CreateAsset(texts, TextsPath);
            return texts;
        }

        private static NeighborhoodData EnsureNeighborhood(string id, string title, string[] intro, List<TaskData> tasks)
        {
            string path = $"{NeighborhoodsFolder}/Neighborhood_{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<NeighborhoodData>(path);
            if (existing != null) return existing;
            var n = NeighborhoodData.Create(id, title, intro, tasks);
            AssetDatabase.CreateAsset(n, path);
            return n;
        }

        private static void SetUpTown(NeighborhoodTexts texts, NeighborhoodData denklestirme)
        {
            // OpenScene kullanılmayan asset'leri boşaltır; elimizdeki referanslar "yok edilmiş" olur. Yoldan yeniden yükle.
            string textsPath = AssetDatabase.GetAssetPath(texts);
            string neighborhoodPath = AssetDatabase.GetAssetPath(denklestirme);
            var scene = EditorSceneManager.OpenScene(TownScenePath);
            texts = AssetDatabase.LoadAssetAtPath<NeighborhoodTexts>(textsPath);
            denklestirme = AssetDatabase.LoadAssetAtPath<NeighborhoodData>(neighborhoodPath);

            var controller = Object.FindAnyObjectByType<OrderController>(FindObjectsInactive.Include);
            var door = Object.FindAnyObjectByType<DoorStation>(FindObjectsInactive.Include);
            if (controller == null || door == null)
                Debug.LogWarning("[BilgiAdasi] Town'da kazan ya da kapı istasyonu yok; runner'ın alanları boş kaldı.");

            var runner = Object.FindAnyObjectByType<NeighborhoodRunner>(FindObjectsInactive.Include);
            if (runner == null)
            {
                var go = new GameObject(RunnerObjectName);
                runner = go.AddComponent<NeighborhoodRunner>(); // DialogueDirector RequireComponent ile gelir
            }
            if (!runner.TryGetComponent<DialogueDirector>(out _)) runner.gameObject.AddComponent<DialogueDirector>();
            if (!runner.TryGetComponent<DevShortcuts>(out _)) runner.gameObject.AddComponent<DevShortcuts>();

            var so = new SerializedObject(runner);
            var list = so.FindProperty("neighborhoods");
            for (int i = list.arraySize - 1; i >= 0; i--)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == null) list.DeleteArrayElementAtIndex(i);
            if (!Contains(list, denklestirme))
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = denklestirme;
            }
            so.FindProperty("texts").objectReferenceValue = texts;
            if (controller != null) so.FindProperty("orderController").objectReferenceValue = controller;
            if (door != null) so.FindProperty("door").objectReferenceValue = door;
            so.ApplyModifiedPropertiesWithoutUndo();

            AddPlaceholderDialogueView();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static bool Contains(SerializedProperty list, Object value)
        {
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == value) return true;
            return false;
        }

        private static void AddPlaceholderDialogueView()
        {
            var hud = GameObject.Find("_Placeholder/HUD");
            if (hud == null)
            {
                Debug.LogWarning("[BilgiAdasi] _Placeholder/HUD bulunamadı; ham konuşma balonu eklenmedi.");
                return;
            }
            if (hud.GetComponentInChildren<PlaceholderDialogueView>(true) != null) return;

            var go = new GameObject("DialogueView", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(hud.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling(); // sipariş panelinin üstünde çizilsin
            go.AddComponent<PlaceholderDialogueView>();
        }
    }
}
