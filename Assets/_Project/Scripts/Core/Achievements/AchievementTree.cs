using System;
using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>Başarımın türü: bildirimde başlık, ağaçta renk ve varsayılan simge buna göre değişir.</summary>
    public enum AchievementKind
    {
        /// <summary>Bir konu öğrenildi (görev bitti).</summary>
        Learned,
        /// <summary>Büyük adım (köy bitti, çıraklık başladı).</summary>
        Milestone,
        /// <summary>Meydan okuma (hatasız, defter açmadan).</summary>
        Challenge
    }

    /// <summary>Başarımın açılma koşulu. Koşullar kayıttan okunur; geç gelen kayıtlar da yakalanır.</summary>
    public enum AchievementTrigger
    {
        /// <summary>Oyun ilk açıldığında (ağacın kökü).</summary>
        GameStart,
        /// <summary>target = görev kimliği (denk_1).</summary>
        TaskCompleted,
        /// <summary>target = köy kimliği (denklestirme).</summary>
        NeighborhoodCompleted,
        /// <summary>target = görev kimliği; görev hiç köpürme/hata olmadan bitti.</summary>
        TaskFlawless,
        /// <summary>target = görev kimliği; görev ustanın defteri açılmadan bitti.</summary>
        TaskWithoutHint,
        /// <summary>target = virgülle ayrılmış başarım kimlikleri; hepsi açılınca (herhangi bir sekmeden).</summary>
        AllOf
    }

    /// <summary>Ağaçtaki tek başarım. Oyuncuya görünen metinler burada.</summary>
    [Serializable]
    public sealed class AchievementEntry
    {
        [Tooltip("Kayıtta tutulan benzersiz kimlik (bütün sekmelerde): ach_denk_1 …")]
        [SerializeField] private string id = "ach_";
        [Tooltip("Ağaçta bağlı olduğu başarımın kimliği (aynı sekmede, listede önce). Boş = kök.")]
        [SerializeField] private string parentId = "";
        [SerializeField] private AchievementKind kind = AchievementKind.Learned;
        [SerializeField] private AchievementTrigger trigger = AchievementTrigger.TaskCompleted;
        [Tooltip("Görev / köy kimliği ya da (AllOf) virgüllü başarım kimlikleri.")]
        [SerializeField] private string target = "";
        [SerializeField] private string title = "";
        [TextArea(2, 5)] [SerializeField] private string description = "";
        [Tooltip("Boş bırakılırsa türün varsayılan simgesi kullanılır.")]
        [SerializeField] private Sprite icon;
        [Tooltip("Açılana kadar ağaçta '???' görünür.")]
        [SerializeField] private bool secret;
        [Tooltip("Açıksa düğüm otomatik yerleşim yerine aşağıdaki sütun/satıra konur.")]
        [SerializeField] private bool manualPosition;
        [SerializeField] private Vector2Int position;

        public string Id => id;
        public string ParentId => parentId;
        public AchievementKind Kind => kind;
        public AchievementTrigger Trigger => trigger;
        public string Target => target;
        public string Title => title;
        public string Description => description;
        public Sprite Icon => icon;
        public bool Secret => secret;
        public bool ManualPosition => manualPosition;
        public Vector2Int Position => position;

        public AchievementEntry() { }

        /// <summary>Editör veri üreticisi ve testler için.</summary>
        public AchievementEntry(string id, string parentId, AchievementKind kind, AchievementTrigger trigger,
            string target, string title, string description, bool secret = false)
        {
            this.id = id;
            this.parentId = parentId ?? "";
            this.kind = kind;
            this.trigger = trigger;
            this.target = target ?? "";
            this.title = title;
            this.description = description;
            this.secret = secret;
        }
    }

    /// <summary>Türün görünümü: ağaçtaki çerçeve rengi ve simgesi boş başarımların simgesi. View'lar bunu kullanır.</summary>
    [Serializable]
    public sealed class AchievementKindStyle
    {
        [SerializeField] private string header = "";
        [SerializeField] private Color color = Color.white;
        [SerializeField] private Sprite defaultIcon;

        public string Header => header;
        public Color Color => color;
        public Sprite DefaultIcon => defaultIcon;

        public AchievementKindStyle() { }
        public AchievementKindStyle(string header, Color color)
        {
            this.header = header;
            this.color = color;
        }
    }

    /// <summary>
    /// Bir başarım sekmesi (Esc menüsünde dallanan ağaç): ders başına bir asset (Kimya, Matematik, Fizik, Biyoloji…).
    /// Görünüm verisi (sekme adı/simgesi, tür renkleri ve başlıkları) de burada: tasarım kod değişmeden düzenlenir.
    /// Ebeveyn listede çocuktan önce gelmeli; ilk çocuk ana dal olarak düz devam eder.
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Başarım Ağacı (sekme)", fileName = "AchievementTree_")]
    public sealed class AchievementTree : ScriptableObject
    {
        [Header("Sekme")]
        [SerializeField] private string treeTitle = "Kimya";
        [SerializeField] private Sprite tabIcon;
        [Tooltip("Sürüm 2 bölümleri: sekmede 'Yakında' etiketi görünür.")]
        [SerializeField] private bool comingSoon;
        [SerializeField] private string comingSoonLabel = "Yakında";
        [Tooltip("Menüdeki sekme sırası (küçük önce).")]
        [SerializeField] private int tabOrder;
        [Tooltip("Dersin rengi (09 paleti): seçili sekme ve sekme şeridi.")]
        [SerializeField] private Color accentColor = new Color(0.62f, 0.31f, 0.17f);

        [Header("Tür görünümü (bildirim başlığı, renk, varsayılan simge)")]
        [SerializeField] private AchievementKindStyle learned = new AchievementKindStyle("Öğrenildi!", new Color(0.30f, 0.55f, 0.30f));
        [SerializeField] private AchievementKindStyle milestone = new AchievementKindStyle("Başarım kazanıldı!", new Color(0.97f, 0.66f, 0f));
        [SerializeField] private AchievementKindStyle challenge = new AchievementKindStyle("Meydan okuma tamamlandı!", new Color(0.62f, 0.25f, 0.55f));

        [Header("Ağaç")]
        [Tooltip("Açılmamış gizli başarımın ağaçtaki adı.")]
        [SerializeField] private string secretTitle = "???";
        [SerializeField] private string lockedLabel = "Henüz açılmadı";
        [SerializeField] private List<AchievementEntry> entries = new List<AchievementEntry>();

        public string TreeTitle => treeTitle;
        public Sprite TabIcon => tabIcon;
        public bool ComingSoon => comingSoon;
        public string ComingSoonLabel => comingSoonLabel;
        public int TabOrder => tabOrder;
        public Color AccentColor => accentColor;
        public string SecretTitle => secretTitle;
        public string LockedLabel => lockedLabel;
        public IReadOnlyList<AchievementEntry> Entries => entries;

        public AchievementKindStyle StyleFor(AchievementKind kind)
        {
            switch (kind)
            {
                case AchievementKind.Milestone: return milestone;
                case AchievementKind.Challenge: return challenge;
                default: return learned;
            }
        }

        public string HeaderFor(AchievementKind kind) => StyleFor(kind).Header;

        /// <summary>Başarımın simgesi; boşsa türün varsayılanı (o da boş olabilir).</summary>
        public Sprite IconFor(AchievementEntry e) => e.Icon != null ? e.Icon : StyleFor(e.Kind).DefaultIcon;

        public int IndexOf(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId)) return -1;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] != null && entries[i].Id == achievementId) return i;
            return -1;
        }

        /// <summary>Ebeveyn indeksleri (-1 = kök; listede sonra gelen ebeveyn kök sayılır). Editör önizlemesi de kullanır.</summary>
        public void GetParents(List<int> result)
        {
            result.Clear();
            for (int i = 0; i < entries.Count; i++)
            {
                int p = entries[i] != null ? IndexOf(entries[i].ParentId) : -1;
                result.Add(p < i ? p : -1);
            }
        }

        /// <summary>Yerleşim: otomatik (AchievementLayout), sonra elle konumlananlar üzerine yazılır.</summary>
        public void ComputeLayout(List<int> parents, int[] columns, int[] rows)
        {
            AchievementLayout.Compute(parents, columns, rows);
            for (int i = 0; i < entries.Count && i < columns.Length; i++)
            {
                if (entries[i] == null || !entries[i].ManualPosition) continue;
                columns[i] = Mathf.Max(0, entries[i].Position.x);
                rows[i] = Mathf.Max(0, entries[i].Position.y);
            }
        }

        /// <summary>Editör veri üreticisi için.</summary>
        public void Configure(string title, int order, Color accent, bool isComingSoon, IEnumerable<AchievementEntry> list)
        {
            treeTitle = title;
            tabOrder = order;
            accentColor = accent;
            comingSoon = isComingSoon;
            entries = new List<AchievementEntry>(list);
        }
    }
}
