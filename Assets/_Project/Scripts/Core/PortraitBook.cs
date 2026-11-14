using System;
using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Konuşan adı → portre (Art/Portreler). Diyalog satırı ya da ustanın sorusu portre getirmezse görünüm buradan bulur
    /// (Çıraklık, Sorgu, tahmin: hepsi "Câbir Usta"). Resources/PortraitBook (Data/Resources). Kurulum 21 doldurur.
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Metin/Portre Defteri", fileName = "PortraitBook")]
    public sealed class PortraitBook : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string speaker;
            public Sprite portrait;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => entries;

        /// <summary>Adın portresi; yoksa null. Büyük/küçük harf ve baş/son boşluk önemsiz.</summary>
        public Sprite Find(string speaker)
        {
            if (string.IsNullOrWhiteSpace(speaker)) return null;
            var key = speaker.Trim();
            for (int i = 0; i < entries.Count; i++)
                if (string.Equals(entries[i].speaker?.Trim(), key, StringComparison.OrdinalIgnoreCase)) return entries[i].portrait;
            return null;
        }

        public void Set(string speaker, Sprite portrait)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (!string.Equals(entries[i].speaker, speaker, StringComparison.OrdinalIgnoreCase)) continue;
                entries[i] = new Entry { speaker = speaker, portrait = portrait };
                return;
            }
            entries.Add(new Entry { speaker = speaker, portrait = portrait });
        }

        private static PortraitBook cached;

        /// <summary>Resources/PortraitBook; yoksa null.</summary>
        public static PortraitBook Load()
        {
            if (cached == null) cached = Resources.Load<PortraitBook>("PortraitBook");
            return cached;
        }

        /// <summary>Verilen portre varsa o, yoksa defterden.</summary>
        public static Sprite Resolve(string speaker, Sprite given)
        {
            if (given != null) return given;
            var book = Load();
            return book != null ? book.Find(speaker) : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => cached = null;
    }
}
