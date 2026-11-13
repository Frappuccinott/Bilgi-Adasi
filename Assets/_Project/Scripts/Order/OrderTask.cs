using System;
using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.Order
{
    public enum OrderMode
    {
        /// <summary>Tüm elementlerde giren == çıkan (Görev 1–4).</summary>
        Exact,
        /// <summary>Her giren için kazandaki ≥ gereken; fazlası artan madde (Görev 5).</summary>
        Purchase
    }

    /// <summary>Zincir görevin bir adımı (02 §3.3). Hedef = tepkimenin ürünleri × çarpan.</summary>
    [Serializable]
    public sealed class OrderStep
    {
        [SerializeField] private ReactionData reaction;
        [Tooltip("Hedef ürünler = tepkime ürünleri × bu sayı (D-5: 4 NH₃ için 2).")]
        [Min(1)] [SerializeField] private int multiplier = 1;
        [Tooltip("Adım bitince rafa düşen ürünler (D-4: CaO). Boşsa hiçbiri.")]
        [SerializeField] private List<SpeciesData> dropToShelf = new List<SpeciesData>();

        public ReactionData Reaction => reaction;
        public int Multiplier => Mathf.Max(1, multiplier);
        public IReadOnlyList<SpeciesData> DropToShelf => dropToShelf;
    }

    /// <summary>Purchase modunda sandıktaki tür ve fiyatı (akçe).</summary>
    [Serializable]
    public struct SpeciesPrice
    {
        [SerializeField] private SpeciesData species;
        [Min(0)] [SerializeField] private int price;

        public SpeciesData Species => species;
        public int Price => price;
    }

    /// <summary>Sipariş görevi (kazan + teraziler). 02-siparis-mekanigi.md, 05-icerik-prototip.md §2.</summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Görev/Sipariş", fileName = "Order_D-")]
    public sealed class OrderTask : TaskData
    {
        [Header("Sipariş")]
        [SerializeField] private OrderMode mode = OrderMode.Exact;
        [SerializeField] private List<OrderStep> steps = new List<OrderStep>();
        [Tooltip("Exact modunda raftaki kavanozlar.")]
        [SerializeField] private List<SpeciesData> shelf = new List<SpeciesData>();
        [Tooltip("Sipariş kartı girenleri de yazsın mı? (Görev 1–2)")]
        [SerializeField] private bool showReactantsOnCard = true;

        [Header("Purchase")]
        [SerializeField] private int budget = 20;
        [SerializeField] private List<SpeciesPrice> prices = new List<SpeciesPrice>();

        public OrderMode Mode => mode;
        public IReadOnlyList<OrderStep> Steps => steps;
        public IReadOnlyList<SpeciesData> Shelf => shelf;
        public bool ShowReactantsOnCard => showReactantsOnCard;
        public int Budget => budget;
        public IReadOnlyList<SpeciesPrice> Prices => prices;

        public int PriceOf(SpeciesData species)
        {
            for (int i = 0; i < prices.Count; i++)
                if (prices[i].Species == species) return prices[i].Price;
            return 0;
        }
    }
}
