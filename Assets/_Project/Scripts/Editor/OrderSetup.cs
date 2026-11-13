using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Order;
using UnityEditor;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Faz 3 kurulumu: OrderTexts + Denkleştirme siparişleri (05 §2) asset'leri. Var olanın üzerine yazmaz.
    /// Kazan, raf, pazar ve HUD kurulumu 13.11'den beri 'Kurulum → 8'de.
    /// </summary>
    public static class OrderSetup
    {
        private const string TextsFolder = "Assets/_Project/Data/Texts";
        private const string TextsPath = TextsFolder + "/OrderTexts.asset";
        private const string TasksFolder = "Assets/_Project/Data/Tasks/Denklestirme";

        [MenuItem("BilgiAdasi/Kurulum/5 - Sipariş verisi", priority = 4)]
        public static void Run()
        {
            EnsureTexts();
            if (EnsureDenklestirmeTasks() == null) return;
            AssetDatabase.SaveAssets();
            Debug.Log("[BilgiAdasi] Sipariş verisi tamam: OrderTexts + 5 Denkleştirme siparişi.");
        }

        public static OrderTexts EnsureTexts()
        {
            var existing = AssetDatabase.LoadAssetAtPath<OrderTexts>(TextsPath);
            if (existing != null) return existing;
            EnsureFolder("Assets/_Project/Data", "Texts");
            var texts = ScriptableObject.CreateInstance<OrderTexts>();
            AssetDatabase.CreateAsset(texts, TextsPath);
            return texts;
        }

        public static OrderTask[] EnsureDenklestirmeTasks()
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

        public static Dictionary<string, T> LoadAll<T>(string folder, System.Func<T, string> key) where T : Object
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
