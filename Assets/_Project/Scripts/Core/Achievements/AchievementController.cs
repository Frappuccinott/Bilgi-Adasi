using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Başarımlar: görev/köy olaylarında koşulları kayıttan değerlendirir, yeni açılanı kaydeder,
    /// GameEvents.AchievementUnlocked yayınlar ve sağ alttaki bildirimi (IAchievementToastView) gösterir.
    /// Her sekme ayrı bir AchievementTree asset'idir (Kimya, Bereket Tarlası…); kimlikler bütün sekmelerde benzersiz.
    /// Açılışta kayıtla eşitlenir (eski kayıtta bitmiş görevler bildirim göstermeden açılır).
    /// Geliştirici atlamaları (F1–F5) da NeighborhoodProgress ile yakalanır ve bildirim gösterir.
    /// Esc menüsüne BuildMenuInfo verir. Görev listesi: BilgiAdasi_Docs/04_Icerik/11-gorev-ve-basarim-listesi.md.
    /// </summary>
    public sealed class AchievementController : MonoBehaviour
    {
        [Tooltip("Ders sekmeleri, menüdeki sırayla. Editör projedeki bütün AchievementTree asset'lerini (Tab Order'a göre) kendisi bağlar.")]
        [SerializeField] private List<AchievementTree> trees = new List<AchievementTree>();
        // İlk sürümün tek ağaç alanı: eski sahnelerde dolu kalmış olabilir; trees boşsa ona eklenir.
        [SerializeField, HideInInspector] private AchievementTree tree;

        // Kayıt yoksa (test) açılanlar burada.
        private readonly HashSet<string> localUnlocked = new HashSet<string>();
        private readonly List<int> parentBuffer = new List<int>();
        private bool started;
        // Olayın bildirdiği ama GameManager'ın kayda henüz yazmamış olabileceği kimlikler (dinleyici sırası belirsiz).
        private string pendingTask, pendingNeighborhood;

        public IReadOnlyList<AchievementTree> Trees => trees;

        private static SaveData Data => SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;

        private void Awake()
        {
            if (trees.Count == 0 && tree != null) trees.Add(tree);
            trees.RemoveAll(t => t == null);
            if (trees.Count == 0)
                Debug.LogWarning("[Achievements] Hiç başarım sekmesi bağlı değil: BilgiAdasi → Kurulum → 13.", this);
        }

#if UNITY_EDITOR
        private void OnValidate() => EditorSyncTrees();

        /// <summary>
        /// Editör: projedeki bütün AchievementTree asset'lerini (ders sekmeleri) Tab Order'a göre bağlar; silinenler düşer.
        /// Yeni ders sekmesi eklemek = asset oluşturmak; elle bağlamak gerekmez. Değişirse sahne kirli işaretlenir (Ctrl+S).
        /// </summary>
        public void EditorSyncTrees()
        {
            var found = new List<AchievementTree>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:AchievementTree"))
            {
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<AchievementTree>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) found.Add(asset);
            }
            if (found.Count == 0) return;
            found.Sort((a, b) => a.TabOrder != b.TabOrder ? a.TabOrder.CompareTo(b.TabOrder) : string.CompareOrdinal(a.name, b.name));

            bool same = found.Count == trees.Count && tree == null;
            for (int i = 0; same && i < found.Count; i++) same = trees[i] == found[i];
            if (same) return;

            trees.Clear();
            trees.AddRange(found);
            tree = null;
            if (!Application.isPlaying && gameObject.scene.IsValid())
            {
                var self = this;
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (self != null) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(self.gameObject.scene);
                };
            }
        }
#endif

        private void OnEnable()
        {
            GameEvents.TaskCompleted += OnTaskCompleted;
            GameEvents.NeighborhoodCompleted += OnNeighborhoodCompleted;
            GameEvents.NeighborhoodProgress += OnNeighborhoodProgress;
        }

        private void OnDisable()
        {
            GameEvents.TaskCompleted -= OnTaskCompleted;
            GameEvents.NeighborhoodCompleted -= OnNeighborhoodCompleted;
            GameEvents.NeighborhoodProgress -= OnNeighborhoodProgress;
        }

        private void Start()
        {
            // Kök (GameStart) ilk açılışta bildirimle; kayıttan gelen eski ilerleme sessizce.
            Evaluate(true, onlyStart: true);
            Evaluate(false);
            started = true;
        }

        private void OnTaskCompleted(string taskId)
        {
            pendingTask = taskId;
            Evaluate(true);
            pendingTask = null;
        }

        private void OnNeighborhoodCompleted(string neighborhoodId)
        {
            pendingNeighborhood = neighborhoodId;
            Evaluate(true);
            pendingNeighborhood = null;
        }

        // Açılıştaki ilk yayın Start'tan önce gelebilir: o zaman sessiz eşitleme Start'ta yapılır.
        private void OnNeighborhoodProgress(string _, int __, int ___)
        {
            if (started) Evaluate(true);
        }

        // ---------- Değerlendirme ----------

        public bool IsUnlocked(string achievementId)
        {
            var data = Data;
            return data != null ? data.IsAchievementUnlocked(achievementId) : localUnlocked.Contains(achievementId);
        }

        private void Evaluate(bool notify, bool onlyStart = false)
        {
            bool any = false;
            // AllOf, aynı geçişte açılan başarımlara bağlı olabilir: değişiklik kalmayana kadar (en çok 4 tur).
            for (int pass = 0; pass < 4; pass++)
            {
                bool changed = false;
                for (int t = 0; t < trees.Count; t++)
                {
                    var tree = trees[t];
                    if (tree == null) continue;
                    var entries = tree.Entries;
                    for (int i = 0; i < entries.Count; i++)
                    {
                        var e = entries[i];
                        if (e == null || string.IsNullOrEmpty(e.Id) || IsUnlocked(e.Id)) continue;
                        if (onlyStart != (e.Trigger == AchievementTrigger.GameStart)) continue;
                        if (!IsSatisfied(e)) continue;
                        Unlock(tree, e, notify);
                        changed = true;
                    }
                }
                any |= changed;
                if (!changed) break;
            }
            if (any) SaveSystem.Instance?.Save();
        }

        private bool IsSatisfied(AchievementEntry e)
        {
            var data = Data;
            switch (e.Trigger)
            {
                case AchievementTrigger.GameStart:
                    return true;
                case AchievementTrigger.TaskCompleted:
                    return IsTaskDone(e.Target);
                case AchievementTrigger.NeighborhoodCompleted:
                    return !string.IsNullOrEmpty(e.Target)
                        && (e.Target == pendingNeighborhood || (data != null && data.IsNeighborhoodCompleted(e.Target)));
                case AchievementTrigger.TaskFlawless:
                    return IsTaskDone(e.Target) && (data == null || data.GetFailCount(e.Target) == 0);
                case AchievementTrigger.TaskWithoutHint:
                    return IsTaskDone(e.Target) && (data == null || !data.IsHintOpened(e.Target));
                case AchievementTrigger.AllOf:
                    return AllUnlocked(e.Target);
                default:
                    return false;
            }
        }

        private bool IsTaskDone(string taskId) =>
            !string.IsNullOrEmpty(taskId) && (taskId == pendingTask || (Data != null && Data.IsTaskCompleted(taskId)));

        // "a, b, c" → hepsi açık mı? (yalnızca olaylarda çağrılır; Split ayırması önemsiz)
        private bool AllUnlocked(string list)
        {
            if (string.IsNullOrWhiteSpace(list)) return false;
            var ids = list.Split(',');
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i].Trim();
                if (id.Length > 0 && !IsUnlocked(id)) return false;
            }
            return true;
        }

        private void Unlock(AchievementTree tree, AchievementEntry e, bool notify)
        {
            var data = Data;
            if (data != null) data.MarkAchievementUnlocked(e.Id);
            else localUnlocked.Add(e.Id);

            GameEvents.RaiseAchievementUnlocked(e.Id);
            if (!notify) return;
            var style = tree.StyleFor(e.Kind);
            ViewLocator.Get<IAchievementToastView>()?.ShowToast(
                new AchievementToastInfo(style.Header, e.Title, e.Description, tree.IconFor(e), style.Color, e.Kind));
        }

        // ---------- Esc menüsü ----------

        /// <summary>Bütün sekmelerin anlık görüntüsü. Menü açılırken bir kez çağrılır.</summary>
        public AchievementMenuInfo BuildMenuInfo()
        {
            var info = new AchievementMenuInfo();
            for (int t = 0; t < trees.Count; t++)
            {
                var tree = trees[t];
                if (tree == null) continue;
                var tab = BuildTab(tree);
                info.Tabs.Add(tab);
                info.UnlockedCount += tab.UnlockedCount;
                info.TotalCount += tab.Nodes.Count;
            }
            return info;
        }

        private AchievementTabInfo BuildTab(AchievementTree tree)
        {
            var tab = new AchievementTabInfo
            {
                Title = tree.TreeTitle,
                Icon = tree.TabIcon,
                Color = tree.AccentColor,
                ComingSoon = tree.ComingSoon,
                ComingSoonLabel = tree.ComingSoonLabel,
                LockedLabel = tree.LockedLabel
            };
            var entries = tree.Entries;
            int n = entries.Count;
            tree.GetParents(parentBuffer);
            var columns = new int[n];
            var rows = new int[n];
            tree.ComputeLayout(parentBuffer, columns, rows);

            for (int i = 0; i < n; i++)
            {
                var e = entries[i];
                bool unlocked = IsUnlocked(e.Id);
                var state = unlocked ? AchievementState.Unlocked : e.Secret ? AchievementState.Secret : AchievementState.Locked;
                if (unlocked) tab.UnlockedCount++;
                var style = tree.StyleFor(e.Kind);
                bool secret = state == AchievementState.Secret;
                tab.Nodes.Add(new AchievementNodeInfo(e.Id,
                    secret ? tree.SecretTitle : e.Title,
                    secret ? string.Empty : e.Description,
                    style.Header, secret ? null : tree.IconFor(e), style.Color,
                    e.Kind, state, parentBuffer[i], columns[i], rows[i]));
            }
            return tab;
        }
    }
}
