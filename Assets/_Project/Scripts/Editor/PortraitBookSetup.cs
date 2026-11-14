using BilgiAdasi.City;
using BilgiAdasi.Core;
using UnityEditor;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Kurulum 21: portre defteri (Data/Resources/PortraitBook). Konuşan adı → portre: ustanın portresi
    /// (NeighborhoodTexts) ve her görevde görevi verenin portresi (TaskData: sipariş + masa). Diyalog satırı ya da soru portre
    /// getirmezse görünüm buradan bulur. Yeni NPC / görev eklenince yeniden çalıştırın; elle eklenenler korunur.
    /// </summary>
    public static class PortraitBookSetup
    {
        private const string BookPath = "Assets/_Project/Data/Resources/PortraitBook.asset";

        [MenuItem("BilgiAdasi/Kurulum/21 - Portre defteri (konuşan adı → portre)", priority = 22)]
        public static void Run()
        {
            var book = AssetDatabase.LoadAssetAtPath<PortraitBook>(BookPath);
            if (book == null)
            {
                book = ScriptableObject.CreateInstance<PortraitBook>();
                AssetDatabase.CreateAsset(book, BookPath);
            }

            int added = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:NeighborhoodTexts"))
            {
                var t = AssetDatabase.LoadAssetAtPath<NeighborhoodTexts>(AssetDatabase.GUIDToAssetPath(guid));
                if (t != null && t.MasterPortrait != null && !string.IsNullOrWhiteSpace(t.MasterName))
                {
                    book.Set(t.MasterName.Trim(), t.MasterPortrait);
                    added++;
                }
            }
            foreach (var guid in AssetDatabase.FindAssets("t:TaskData"))
            {
                var task = AssetDatabase.LoadAssetAtPath<TaskData>(AssetDatabase.GUIDToAssetPath(guid));
                if (task == null || task.GiverPortrait == null || string.IsNullOrWhiteSpace(task.GiverName)) continue;
                book.Set(task.GiverName.Trim(), task.GiverPortrait);
                added++;
            }

            EditorUtility.SetDirty(book);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Kurulum 21] Portre defteri: {book.Entries.Count} konuşan ({added} kaynak tarandı).");
        }
    }
}
