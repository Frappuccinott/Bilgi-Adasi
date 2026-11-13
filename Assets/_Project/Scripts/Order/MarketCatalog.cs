using System;
using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Bir pazar tezgâhının satış listesi. Her tezgâh kendi kataloğunu taşır; ikinci bir pazar = yeni bir katalog asset'i.
    /// Bu dosya yalnızca veridir; neyin o an satışta olduğuna kurallar (MarketRules) karar verir.
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Pazar Kataloğu", fileName = "MarketCatalog")]
    public sealed class MarketCatalog : ScriptableObject
    {
        [Tooltip("Tezgâhta satılabilecek maddeler. Sıra = ekrandaki sıra.")]
        [SerializeField] private List<MarketEntry> entries = new List<MarketEntry>();

        public IReadOnlyList<MarketEntry> Entries => entries;

        /// <summary>Editör kurulumu (geçiş) için: listeyi verilen maddelerle doldurur, var olanları bozmaz.</summary>
        public void AddIfMissing(SpeciesData species)
        {
            if (species == null) return;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] != null && entries[i].Species == species) return;
            entries.Add(new MarketEntry(species));
        }
    }

    /// <summary>Katalogdaki tek satır: madde, fiyat, stok ve görevle açılıp kalkma.</summary>
    [Serializable]
    public sealed class MarketEntry
    {
        [SerializeField] private SpeciesData species;

        [Tooltip("İşaretsizse maddenin kendi fiyatı (SpeciesData.price) kullanılır.")]
        [SerializeField] private bool overridePrice;
        [Min(0)] [SerializeField] private int price;

        [Tooltip("İşaretliyse stok sınırsızdır.")]
        [SerializeField] private bool unlimitedStock = true;
        [Min(0)] [SerializeField] private int stock = 5;

        [Tooltip("Bu görev bitince satışa çıkar. Boşsa baştan satılır.")]
        [SerializeField] private TaskData availableAfter;
        [Tooltip("Bu görev bitince tezgâhtan kalkar. Boşsa hiç kalkmaz.")]
        [SerializeField] private TaskData removedAfter;

        public MarketEntry() { }
        public MarketEntry(SpeciesData species) { this.species = species; }

        public SpeciesData Species => species;
        public int Price => overridePrice || species == null ? price : species.Price;
        public bool UnlimitedStock => unlimitedStock;
        public int Stock => stock;
        public TaskData AvailableAfter => availableAfter;
        public TaskData RemovedAfter => removedAfter;

        /// <summary>Kayıt ve stok için kimlik (formül).</summary>
        public string Key => species != null ? species.Formula : string.Empty;
    }
}
