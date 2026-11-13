using System.Collections.Generic;
using System.IO;
using BilgiAdasi.City;
using BilgiAdasi.Core;
using BilgiAdasi.Order;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Köy dosyası = senaryo. Tek sayfada: Usta'nın tanıtımı, görevlerin sırası (sürükle-bırak = müşterilerin geliş
    /// sırası) ve her görevin bütün metinleri/istenenleri (açılır kutu). "Yeni sipariş ekle" görev dosyasını oluşturur.
    /// Yalnızca editör; oyun kodu ve veri yapısı değişmez.
    /// </summary>
    [CustomEditor(typeof(NeighborhoodData))]
    public sealed class NeighborhoodScenarioEditor : Editor
    {
        private static readonly GUIContent IdLabel = new GUIContent("Köy kimliği", "Kayıtta ve NeighborhoodView'da kullanılır.");
        private static readonly GUIContent TitleLabel = new GUIContent("Köy adı");
        private static readonly GUIContent LockedLabel = new GUIContent("Baştan kilitli");
        private static readonly GUIContent IntroLabel = new GUIContent("Usta'nın tanıtımı", "Köyün ilk görevi alınırken bir kez söylenir.");

        private SerializedProperty tasksProp;
        private ReorderableList list;
        private readonly Dictionary<Object, SerializedObject> taskObjects = new Dictionary<Object, SerializedObject>();
        private readonly HashSet<Object> expanded = new HashSet<Object>();
        private readonly List<string> problems = new List<string>();
        private readonly Dictionary<string, int> idCounts = new Dictionary<string, int>();
        // Satır/başlık yazıları yalnızca değişiklikte üretilir (her OnGUI'de string kurulmasın).
        private readonly Dictionary<Object, string> summaries = new Dictionary<Object, string>();
        private readonly Dictionary<Object, string> headers = new Dictionary<Object, string>();
        private bool dirty = true;

        private void OnEnable()
        {
            tasksProp = serializedObject.FindProperty("tasks");
            list = new ReorderableList(serializedObject, tasksProp, true, true, false, true)
            {
                drawHeaderCallback = r => EditorGUI.LabelField(r, "Sıra (yukarıdan aşağı müşterilerin geliş sırası)"),
                drawElementCallback = DrawRow,
                onChangedCallback = _ => dirty = true,
                onRemoveCallback = RemoveRow,
            };
            Undo.undoRedoPerformed += MarkDirty;
        }

        private void OnDisable() => Undo.undoRedoPerformed -= MarkDirty;

        private void MarkDirty()
        {
            dirty = true;
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("id"), IdLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("title"), TitleLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lockedByDefault"), LockedLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("introLines"), IntroLabel, true);

            EditorGUILayout.Space(8);
            if (dirty) Validate();
            list.DoLayoutList();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                dirty = true;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ Yeni sipariş ekle")) AddOrder();
                if (GUILayout.Button("Hepsini aç", GUILayout.Width(90))) ExpandAll(true);
                if (GUILayout.Button("Hepsini kapat", GUILayout.Width(100))) ExpandAll(false);
            }

            if (dirty) Validate();
            if (problems.Count > 0)
            {
                EditorGUILayout.Space(4);
                for (int i = 0; i < problems.Count; i++) EditorGUILayout.HelpBox(problems[i], MessageType.Warning);
            }

            EditorGUILayout.Space(8);
            DrawTaskDetails();
        }

        // ---------- Sıra listesi ----------

        private void DrawRow(Rect rect, int index, bool active, bool focused)
        {
            var element = tasksProp.GetArrayElementAtIndex(index);
            var task = element.objectReferenceValue as TaskData;
            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;

            var numberRect = new Rect(rect.x, rect.y, 24, rect.height);
            var fieldRect = new Rect(rect.x + 26, rect.y, Mathf.Min(170, rect.width * 0.4f), rect.height);
            var summaryRect = new Rect(fieldRect.xMax + 6, rect.y, rect.xMax - fieldRect.xMax - 6, rect.height);

            EditorGUI.LabelField(numberRect, (index + 1) + ".");
            EditorGUI.PropertyField(fieldRect, element, GUIContent.none);
            if (task != null && summaries.TryGetValue(task, out var summary))
                EditorGUI.LabelField(summaryRect, summary, EditorStyles.miniLabel);
        }

        /// <summary>Listeden çıkarır; görev dosyası silinmez (yanlışlıkla emek kaybolmasın).</summary>
        private void RemoveRow(ReorderableList l)
        {
            int index = l.index;
            if (index < 0 || index >= tasksProp.arraySize) return;
            tasksProp.GetArrayElementAtIndex(index).objectReferenceValue = null;
            tasksProp.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            dirty = true;
        }

        // ---------- Görev ayrıntıları ----------

        private void DrawTaskDetails()
        {
            var n = (NeighborhoodData)target;
            var tasks = n.Tasks;
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task == null) continue;

                bool open = expanded.Contains(task);
                if (!headers.TryGetValue(task, out var header)) header = task.name;
                bool nowOpen = EditorGUILayout.BeginFoldoutHeaderGroup(open, header);
                if (nowOpen != open)
                {
                    if (nowOpen) expanded.Add(task);
                    else expanded.Remove(task);
                }
                if (nowOpen)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        if (TaskScenarioGUI.Draw(GetTaskObject(task))) dirty = true;
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Dosyayı göster", EditorStyles.miniButton, GUILayout.Width(100)))
                                EditorGUIUtility.PingObject(task);
                        }
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        private SerializedObject GetTaskObject(Object task)
        {
            if (!taskObjects.TryGetValue(task, out var so) || so.targetObject == null)
            {
                so = new SerializedObject(task);
                taskObjects[task] = so;
            }
            return so;
        }

        private void ExpandAll(bool open)
        {
            expanded.Clear();
            if (!open) return;
            var tasks = ((NeighborhoodData)target).Tasks;
            for (int i = 0; i < tasks.Count; i++)
                if (tasks[i] != null) expanded.Add(tasks[i]);
        }

        // ---------- Yeni sipariş ----------

        /// <summary>
        /// Yeni OrderTask dosyası: son görevin klasörüne, kimlik "önek_(n+1)" (denk_6 …). Adımı, ödemesi ve
        /// adım ayarları son siparişten kopyalanır; metinler boş başlar.
        /// </summary>
        private void AddOrder()
        {
            var n = (NeighborhoodData)target;
            var tasks = n.Tasks;
            OrderTask last = null;
            for (int i = tasks.Count - 1; i >= 0 && last == null; i--) last = tasks[i] as OrderTask;

            string folder = last != null
                ? Path.GetDirectoryName(AssetDatabase.GetAssetPath(last)).Replace('\\', '/')
                : Path.GetDirectoryName(AssetDatabase.GetAssetPath(n)).Replace('\\', '/');
            string prefix = IdPrefix(last != null ? last.Id : null, n.Id);
            string id = NextFreeId(prefix, tasks.Count + 1);

            OrderTask task;
            if (last != null)
            {
                task = Instantiate(last);
                var so = new SerializedObject(task);
                so.FindProperty("giverName").stringValue = string.Empty;
                so.FindProperty("giverPortrait").objectReferenceValue = null;
                so.FindProperty("giverLines").arraySize = 1;
                so.FindProperty("giverLines").GetArrayElementAtIndex(0).stringValue = string.Empty;
                so.FindProperty("deliveryLines").arraySize = 0;
                so.FindProperty("hintText").stringValue = string.Empty;
                so.FindProperty("realLifeText").stringValue = string.Empty;
                so.FindProperty("id").stringValue = id;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                task = CreateInstance<OrderTask>();
                var so = new SerializedObject(task);
                so.FindProperty("id").stringValue = id;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{id}.asset");
            AssetDatabase.CreateAsset(task, path);

            serializedObject.Update();
            tasksProp.arraySize++;
            tasksProp.GetArrayElementAtIndex(tasksProp.arraySize - 1).objectReferenceValue = task;
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            expanded.Add(task);
            dirty = true;
        }

        /// <summary>"denk_5" → "denk_"; yoksa köy kimliği + "_".</summary>
        private static string IdPrefix(string lastId, string neighborhoodId)
        {
            if (!string.IsNullOrEmpty(lastId))
            {
                int us = lastId.LastIndexOf('_');
                if (us >= 0) return lastId.Substring(0, us + 1);
            }
            return string.IsNullOrEmpty(neighborhoodId) ? "gorev_" : neighborhoodId + "_";
        }

        /// <summary>Projede hiçbir görevde kullanılmayan ilk "önek_n" kimliği.</summary>
        private static string NextFreeId(string prefix, int start)
        {
            var used = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(TaskData)))
            {
                var t = AssetDatabase.LoadAssetAtPath<TaskData>(AssetDatabase.GUIDToAssetPath(guid));
                if (t != null) used.Add(t.Id);
            }
            int i = start;
            while (used.Contains(prefix + i)) i++;
            return prefix + i;
        }

        // ---------- Denetim ----------

        /// <summary>Yalnızca bir şey değişince çalışır (her OnGUI'de değil).</summary>
        private void Validate()
        {
            dirty = false;
            problems.Clear();
            idCounts.Clear();
            summaries.Clear();
            headers.Clear();
            var n = (NeighborhoodData)target;
            if (string.IsNullOrWhiteSpace(n.Id)) problems.Add("Köy kimliği boş.");

            var tasks = n.Tasks;
            if (tasks.Count == 0) problems.Add("Köyde hiç görev yok.");
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task == null)
                {
                    problems.Add($"{i + 1}. sıra boş.");
                    continue;
                }
                string summary = TaskScenarioGUI.Summary(task);
                summaries[task] = summary;
                headers[task] = $"{i + 1}. {task.Id}   {summary}";
                if (!string.IsNullOrEmpty(task.Id))
                    idCounts[task.Id] = idCounts.TryGetValue(task.Id, out int c) ? c + 1 : 1;
                TaskScenarioGUI.Validate(task, problems);
            }
            foreach (var pair in idCounts)
                if (pair.Value > 1) problems.Add($"\"{pair.Key}\" kimliği {pair.Value} görevde var; her görevin kimliği farklı olmalı.");
        }
    }
}
