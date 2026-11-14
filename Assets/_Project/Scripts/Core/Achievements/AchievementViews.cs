using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>Ağaçta bir başarımın durumu.</summary>
    public enum AchievementState
    {
        /// <summary>Açıldı.</summary>
        Unlocked,
        /// <summary>Açılmadı; adı ve açıklaması görünür.</summary>
        Locked,
        /// <summary>Açılmadı ve gizli: "???".</summary>
        Secret
    }

    /// <summary>Sağ alttaki bildirim için veri. Renk ve simge sekme asset'inin tür görünümünden gelir.</summary>
    public readonly struct AchievementToastInfo
    {
        public readonly string Header;      // "Öğrenildi!" / "Başarım kazanıldı!"
        public readonly string Title;
        public readonly string Description;
        public readonly Sprite Icon;        // boş olabilir
        public readonly Color Color;        // türün rengi
        public readonly AchievementKind Kind;
        /// <summary>Görev sonu özeti: öğrenilen konu ("Konu: Yanma tepkimeleri"). Boş = özet yok.</summary>
        public readonly string Topic;
        /// <summary>Görev sonu özeti: hata ve defter kullanımı ("Hatasız · Defter açılmadan"). Boş olabilir.</summary>
        public readonly string Stats;

        public bool HasSummary => !string.IsNullOrEmpty(Topic) || !string.IsNullOrEmpty(Stats);

        public AchievementToastInfo(string header, string title, string description, Sprite icon, Color color, AchievementKind kind,
            string topic = "", string stats = "")
        {
            Header = header;
            Title = title;
            Description = description;
            Icon = icon;
            Color = color;
            Kind = kind;
            Topic = topic ?? string.Empty;
            Stats = stats ?? string.Empty;
        }
    }

    /// <summary>Ağaçta tek düğüm. Column/Row: otomatik yerleşim ya da asset'teki elle konum.</summary>
    public readonly struct AchievementNodeInfo
    {
        public readonly string Id;
        public readonly string Title;       // gizliyse "???"
        public readonly string Description; // gizliyse boş
        public readonly string Header;      // türün başlığı
        public readonly Sprite Icon;        // gizliyse null
        public readonly Color Color;        // türün rengi
        public readonly AchievementKind Kind;
        public readonly AchievementState State;
        public readonly int ParentIndex;    // -1 = kök (aynı sekmede)
        public readonly int Column;
        public readonly int Row;
        /// <summary>Görev başarımıysa ve açıldıysa öğrenilen konu; değilse boş.</summary>
        public readonly string Topic;
        /// <summary>Açılmış görev başarımında hata ve defter kullanımı; değilse boş.</summary>
        public readonly string Stats;

        public AchievementNodeInfo(string id, string title, string description, string header, Sprite icon, Color color,
            AchievementKind kind, AchievementState state, int parentIndex, int column, int row, string topic = "", string stats = "")
        {
            Topic = topic ?? string.Empty;
            Stats = stats ?? string.Empty;
            Id = id;
            Title = title;
            Description = description;
            Header = header;
            Icon = icon;
            Color = color;
            Kind = kind;
            State = state;
            ParentIndex = parentIndex;
            Column = column;
            Row = row;
        }
    }

    /// <summary>Bir sekme (ders): Kimya, Matematik, Fizik, Biyoloji…</summary>
    public sealed class AchievementTabInfo
    {
        public string Title;
        public Sprite Icon;
        public Color Color;
        public bool ComingSoon;
        public string ComingSoonLabel;
        public string LockedLabel;
        public int UnlockedCount;
        public readonly List<AchievementNodeInfo> Nodes = new List<AchievementNodeInfo>();
    }

    /// <summary>Esc menüsündeki "Başarımlar" bölümünün tamamı.</summary>
    public sealed class AchievementMenuInfo
    {
        public readonly List<AchievementTabInfo> Tabs = new List<AchievementTabInfo>();
        public int UnlockedCount;
        public int TotalCount;
    }

    /// <summary>
    /// Sağ alttan kayan başarım bildirimi. Aynı anda birden çok gelebilir (görev + köy): view sıraya alır.
    /// Oyun duraklatılmışken de çalışmalı (unscaled zaman).
    /// </summary>
    public interface IAchievementToastView
    {
        void ShowToast(AchievementToastInfo info);
    }

    /// <summary>Duraklatma (Esc) menüsü. Denetleyici: PauseMenuController.</summary>
    public interface IPauseMenuView
    {
        /// <summary>Menü açılır; başarım verisi her açılışta tazedir.</summary>
        void Open(AchievementMenuInfo achievements);
        void Close();
        /// <summary>Şu anki görev kartı (Open'dan hemen sonra çağrılır). null = görev yok, kart gizli.</summary>
        void ShowCurrentTask(CurrentTaskInfo task);
        /// <summary>Alt sayfadaysa (başarım ağacı) ana sayfaya döner ve true; zaten ana sayfadaysa false (menü kapanır).</summary>
        bool TryGoBack();
    }

    /// <summary>View'ın düğmelerinin çağırdığı komutlar. Menü açıkken kayıtlıdır.</summary>
    public interface IPauseMenuCommands
    {
        void Resume();
        /// <summary>Ana menüye döner (kayıt yazılmaz; "Devam Et" son otomatik kayda döner).</summary>
        void ReturnToMainMenu();
        void QuitGame();
    }
}
