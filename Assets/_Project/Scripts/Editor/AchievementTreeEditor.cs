using System.Collections.Generic;
using BilgiAdasi.Core;
using UnityEditor;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// AchievementTree Inspector'ı: alanların altında doğrulama (kimlik, ebeveyn, koşul hedefi; bütün sekmeler birlikte)
    /// ve ağacın önizlemesi (oyundaki yerleşimin aynısı, tür renkleriyle). Tasarımcı veriyi değiştirince hemen görür.
    /// </summary>
    [CustomEditor(typeof(AchievementTree))]
    public sealed class AchievementTreeEditor : Editor
    {
        private const float NodeW = 120f, NodeH = 34f, StepX = 150f, StepY = 46f, Pad = 8f;

        private readonly List<string> errors = new List<string>();
        private readonly List<string> infos = new List<string>();
        private readonly List<int> parents = new List<int>();
        private HashSet<string> knownTasks;
        private bool showPreview = true;

        private bool dirty = true;

        private void OnEnable()
        {
            knownTasks = CollectTaskIds();
            dirty = true;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck()) dirty = true;
            var tree = (AchievementTree)target;

            EditorGUILayout.Space(10);
            // Bütün sekmeleri taramak pahalı: yalnızca açılışta ve değişiklikte.
            if (dirty && Event.current.type == EventType.Layout)
            {
                Validate(tree);
                dirty = false;
            }
            for (int i = 0; i < errors.Count; i++) EditorGUILayout.HelpBox(errors[i], MessageType.Error);
            if (infos.Count > 0) EditorGUILayout.HelpBox(string.Join("\n", infos), MessageType.Info);
            if (errors.Count == 0) EditorGUILayout.HelpBox("Doğrulama: sorun yok.", MessageType.None);

            EditorGUILayout.Space(6);
            showPreview = EditorGUILayout.Foldout(showPreview, "Ağaç önizlemesi (oyundaki yerleşim)", true);
            if (showPreview) DrawPreview(tree);

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox("Görev listesi: BilgiAdasi_Docs/04_Icerik/11-gorev-ve-basarim-listesi.md · " +
                                    "Listeden yeniden üretmek: BilgiAdasi → Kurulum → 13b.", MessageType.None);
        }

        // ---------- Doğrulama ----------

        private void Validate(AchievementTree tree)
        {
            errors.Clear();
            infos.Clear();
            var entries = tree.Entries;
            var allIds = CollectAllIds(tree);
            var local = new HashSet<string>();
            var missingTasks = new List<string>();

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                string at = $"#{i} '{e.Title}'";
                if (string.IsNullOrWhiteSpace(e.Id)) { errors.Add($"{at}: kimlik boş."); continue; }
                if (!local.Add(e.Id)) errors.Add($"{at}: '{e.Id}' bu sekmede iki kez var.");
                else if (allIds.TryGetValue(e.Id, out var other) && other != tree.name)
                    errors.Add($"{at}: '{e.Id}' başka sekmede de var ({other}). Kimlikler bütün sekmelerde benzersiz olmalı.");

                if (!string.IsNullOrEmpty(e.ParentId))
                {
                    int p = tree.IndexOf(e.ParentId);
                    if (p < 0) errors.Add($"{at}: ebeveyn '{e.ParentId}' bu sekmede yok.");
                    else if (p >= i) errors.Add($"{at}: ebeveyn '{e.ParentId}' listede sonra geliyor; yukarı taşı.");
                }

                switch (e.Trigger)
                {
                    case AchievementTrigger.GameStart:
                        break;
                    case AchievementTrigger.AllOf:
                        foreach (var raw in e.Target.Split(','))
                        {
                            string id = raw.Trim();
                            if (id.Length > 0 && !allIds.ContainsKey(id)) errors.Add($"{at}: AllOf içindeki '{id}' hiçbir sekmede yok.");
                        }
                        if (string.IsNullOrWhiteSpace(e.Target)) errors.Add($"{at}: AllOf hedefi boş.");
                        break;
                    case AchievementTrigger.NeighborhoodCompleted:
                        if (string.IsNullOrWhiteSpace(e.Target)) errors.Add($"{at}: köy kimliği boş.");
                        break;
                    default:
                        if (string.IsNullOrWhiteSpace(e.Target)) errors.Add($"{at}: görev kimliği boş.");
                        else if (knownTasks != null && !knownTasks.Contains(e.Target) && !missingTasks.Contains(e.Target))
                            missingTasks.Add(e.Target);
                        break;
                }
            }
            if (missingTasks.Count > 0)
                infos.Add($"Henüz görev verisi olmayan kimlikler (görev yazılınca açılır): {string.Join(", ", missingTasks)}");
        }

        private static Dictionary<string, string> CollectAllIds(AchievementTree self)
        {
            var result = new Dictionary<string, string>();
            foreach (var guid in AssetDatabase.FindAssets("t:AchievementTree"))
            {
                var tree = AssetDatabase.LoadAssetAtPath<AchievementTree>(AssetDatabase.GUIDToAssetPath(guid));
                if (tree == null) continue;
                foreach (var e in tree.Entries)
                    if (!string.IsNullOrEmpty(e.Id) && (!result.ContainsKey(e.Id) || tree == self)) result[e.Id] = tree.name;
            }
            return result;
        }

        private static HashSet<string> CollectTaskIds()
        {
            var set = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:TaskData"))
            {
                var task = AssetDatabase.LoadAssetAtPath<TaskData>(AssetDatabase.GUIDToAssetPath(guid));
                if (task != null && !string.IsNullOrEmpty(task.Id)) set.Add(task.Id);
            }
            return set;
        }

        // ---------- Önizleme ----------

        private void DrawPreview(AchievementTree tree)
        {
            var entries = tree.Entries;
            int n = entries.Count;
            if (n == 0) return;
            tree.GetParents(parents);
            var cols = new int[n];
            var rows = new int[n];
            tree.ComputeLayout(parents, cols, rows);

            int maxCol = 0, maxRow = 0;
            for (int i = 0; i < n; i++)
            {
                maxCol = Mathf.Max(maxCol, cols[i]);
                maxRow = Mathf.Max(maxRow, rows[i]);
            }
            float width = maxCol * StepX + NodeW + 2 * Pad;
            float height = maxRow * StepY + NodeH + 2 * Pad;
            float available = EditorGUIUtility.currentViewWidth - 30f;
            float scale = Mathf.Min(1f, available / width);

            var area = GUILayoutUtility.GetRect(width * scale, height * scale);
            EditorGUI.DrawRect(area, new Color(0.15f, 0.12f, 0.1f));

            Rect NodeRect(int i) => new Rect(area.x + (Pad + cols[i] * StepX) * scale, area.y + (Pad + rows[i] * StepY) * scale,
                NodeW * scale, NodeH * scale);

            var line = new Color(0.55f, 0.4f, 0.24f);
            float t = Mathf.Max(1f, 2f * scale);
            for (int i = 0; i < n; i++)
            {
                int p = parents[i];
                if (p < 0) continue;
                var a = NodeRect(p);
                var b = NodeRect(i);
                float y1 = a.center.y, y2 = b.center.y, x1 = a.xMax, x2 = b.xMin, mid = (x1 + x2) * 0.5f;
                EditorGUI.DrawRect(new Rect(Mathf.Min(x1, mid), y1 - t / 2, Mathf.Abs(mid - x1), t), line);
                EditorGUI.DrawRect(new Rect(mid - t / 2, Mathf.Min(y1, y2), t, Mathf.Abs(y2 - y1) + t), line);
                EditorGUI.DrawRect(new Rect(Mathf.Min(mid, x2), y2 - t / 2, Mathf.Abs(x2 - mid), t), line);
            }

            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = Mathf.Max(7, Mathf.RoundToInt(10 * scale)),
                normal = { textColor = new Color(0.15f, 0.1f, 0.05f) }
            };
            for (int i = 0; i < n; i++)
            {
                var r = NodeRect(i);
                var e = entries[i];
                EditorGUI.DrawRect(r, tree.StyleFor(e.Kind).Color);
                EditorGUI.DrawRect(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4),
                    e.Secret ? new Color(0.55f, 0.5f, 0.45f) : new Color(0.91f, 0.85f, 0.71f));
                string label = (e.ManualPosition ? "◆ " : "") + StripTags(e.Title);
                GUI.Label(r, new GUIContent(label, $"{e.Id}\n{e.Trigger} {e.Target}"), style);
            }
            EditorGUILayout.LabelField("Renk = tür (Öğrenildi / Başarım / Meydan okuma) · gri = gizli · ◆ = elle konumlu",
                EditorStyles.miniLabel);
        }

        private static string StripTags(string s) =>
            string.IsNullOrEmpty(s) ? s : System.Text.RegularExpressions.Regex.Replace(s, "<.*?>", "");
    }
}
