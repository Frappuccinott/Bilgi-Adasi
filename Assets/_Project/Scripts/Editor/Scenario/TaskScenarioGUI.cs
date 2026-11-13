using System.Collections.Generic;
using BilgiAdasi.Core;
using BilgiAdasi.Order;
using UnityEditor;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Bir görevin (sipariş ya da masa) senaryo alanlarını Türkçe etiketlerle çizer ve denetler.
    /// Köy dosyasının Inspector'ı (NeighborhoodScenarioEditor) ve görev dosyasının kendi Inspector'ı kullanır.
    /// Eski (13.11 öncesi) alanlar gösterilmez.
    /// </summary>
    public static class TaskScenarioGUI
    {
        // Etiketler bir kez oluşturulur (her OnGUI'de yeni GUIContent ayrılmasın).
        private static readonly GUIContent IdLabel = new GUIContent("Kimlik", "Kayıtta kullanılır. Oyun başladıktan sonra değiştirilirse eski kayıtta bu görev bitmemiş görünür.");
        private static readonly GUIContent GiverLabel = new GUIContent("Veren", "Görevi veren sakin: Muhtar Yusuf …");
        private static readonly GUIContent PortraitLabel = new GUIContent("Portre");
        private static readonly GUIContent GiverLinesLabel = new GUIContent("Derdi (kapıda)", "Sakinin kapıda söyledikleri; her satır ayrı balon.");
        private static readonly GUIContent DeliveryLabel = new GUIContent("Teslim sözü", "Teslimde teşekkür; her satır ayrı balon.");
        private static readonly GUIContent HintLabel = new GUIContent("İpucu (ustanın defteri)");
        private static readonly GUIContent RealLifeLabel = new GUIContent("Gerçek hayatta");
        private static readonly GUIContent ModeLabel = new GUIContent("Mod", "Exact: tam denge. Purchase: her girenden en az gereken, fazlası artan.");
        private static readonly GUIContent RewardLabel = new GUIContent("Ödeme (akçe)");
        private static readonly GUIContent StepsLabel = new GUIContent("Adımlar", "Her adım bir tepkime + istenen ürünler. Birden fazla adım = zincir görev; son adımın ürünü teslim edilir.");

        private static readonly HashSet<string> LegacyFields = new HashSet<string> { "shelf", "budget", "prices" };
        private static readonly HashSet<string> CommonFields = new HashSet<string>
        {
            "m_Script", "id", "giverName", "giverPortrait", "giverLines", "deliveryLines", "hintText", "realLifeText",
        };

        /// <summary>Görevin bütün senaryo alanlarını çizer. Değişiklik varsa true.</summary>
        public static bool Draw(SerializedObject so)
        {
            so.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(so.FindProperty("id"), IdLabel);
            EditorGUILayout.PropertyField(so.FindProperty("giverName"), GiverLabel);
            EditorGUILayout.PropertyField(so.FindProperty("giverPortrait"), PortraitLabel);

            if (so.targetObject is OrderTask)
            {
                EditorGUILayout.PropertyField(so.FindProperty("mode"), ModeLabel);
                EditorGUILayout.PropertyField(so.FindProperty("reward"), RewardLabel);
                EditorGUILayout.PropertyField(so.FindProperty("steps"), StepsLabel, true);
            }
            else
            {
                // Masa görevi vb.: kendi alanları (Faz 5) olduğu gibi.
                var it = so.GetIterator();
                for (bool enter = true; it.NextVisible(enter); enter = false)
                    if (!CommonFields.Contains(it.name) && !LegacyFields.Contains(it.name))
                        EditorGUILayout.PropertyField(it, true);
            }

            EditorGUILayout.PropertyField(so.FindProperty("giverLines"), GiverLinesLabel, true);
            EditorGUILayout.PropertyField(so.FindProperty("deliveryLines"), DeliveryLabel, true);
            EditorGUILayout.PropertyField(so.FindProperty("hintText"), HintLabel);
            EditorGUILayout.PropertyField(so.FindProperty("realLifeText"), RealLifeLabel);

            bool changed = EditorGUI.EndChangeCheck();
            if (changed) so.ApplyModifiedProperties();
            return changed;
        }

        /// <summary>Tek satırlık özet: "Muhtar Yusuf — 2 H₂O".</summary>
        public static string Summary(TaskData task)
        {
            if (task == null) return "(boş)";
            string giver = string.IsNullOrEmpty(task.GiverName) ? "(veren yok)" : task.GiverName;
            if (task is not OrderTask order || order.Steps.Count == 0) return giver;

            var products = order.DeliveryProducts;
            var sb = new System.Text.StringBuilder(giver).Append(" — ");
            for (int i = 0; i < products.Count; i++)
            {
                if (i > 0) sb.Append(" + ");
                sb.Append(products[i].Amount).Append(' ').Append(products[i].Species != null ? products[i].Species.Formula : "?");
            }
            if (order.Steps.Count > 1) sb.Append("  (").Append(order.Steps.Count).Append(" adım)");
            return sb.ToString();
        }

        /// <summary>Görevdeki eksik/hatalı alanları listeye ekler (oyunu bozacak olanlar).</summary>
        public static void Validate(TaskData task, List<string> problems)
        {
            if (task == null) return;
            string who = string.IsNullOrEmpty(task.Id) ? task.name : task.Id;
            if (string.IsNullOrWhiteSpace(task.Id)) problems.Add($"{task.name}: kimlik boş.");
            if (task.GiverLines == null || task.GiverLines.Length == 0) problems.Add($"{who}: sakinin derdi (kapıda) boş.");

            if (task is not OrderTask order) return;
            if (order.Steps.Count == 0)
            {
                problems.Add($"{who}: hiç adım yok (tepkime + istenen ürün).");
                return;
            }
            for (int s = 0; s < order.Steps.Count; s++)
            {
                var step = order.Steps[s];
                string where = order.Steps.Count > 1 ? $"{who} adım {s + 1}" : who;
                if (step.Reaction == null) problems.Add($"{where}: tepkime seçilmemiş.");
                if (step.TargetProducts.Count == 0) problems.Add($"{where}: istenen ürün yok.");
                for (int p = 0; p < step.TargetProducts.Count; p++)
                {
                    var target = step.TargetProducts[p];
                    if (target.Species == null) problems.Add($"{where}: istenen ürünlerden biri boş.");
                    else if (target.Amount <= 0) problems.Add($"{where}: {target.Species.Formula} adedi 0.");
                    else if (step.Reaction != null && !IsProduct(step.Reaction, target.Species))
                        problems.Add($"{where}: {target.Species.Formula}, {step.Reaction.Id} tepkimesinin ürünü değil.");
                }
            }
        }

        private static bool IsProduct(Chemistry.ReactionData reaction, Chemistry.SpeciesData species)
        {
            var products = reaction.Products;
            for (int i = 0; i < products.Count; i++)
                if (products[i].Species == species) return true;
            return false;
        }
    }

    /// <summary>Görev dosyası tek başına açılınca da aynı düzen (eski alanlar gizli).</summary>
    [CustomEditor(typeof(OrderTask))]
    public sealed class OrderTaskEditor : Editor
    {
        private readonly List<string> problems = new List<string>();
        private bool dirty = true;

        public override void OnInspectorGUI()
        {
            if (dirty)
            {
                problems.Clear();
                TaskScenarioGUI.Validate((TaskData)target, problems);
                dirty = false;
            }
            for (int i = 0; i < problems.Count; i++) EditorGUILayout.HelpBox(problems[i], MessageType.Warning);
            if (TaskScenarioGUI.Draw(serializedObject)) dirty = true;
        }
    }
}
