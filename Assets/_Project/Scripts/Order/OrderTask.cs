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
        /// <summary>Malzeme akçeyle alınır; her girenden en az gereken kadar. Fazlası "artan" (Görev 5).</summary>
        Purchase
    }

    /// <summary>Bir denge döngüsü. Zincir görevde birden fazla adım olur (Görev 4).</summary>
    [Serializable]
    public sealed class OrderStep
    {
        [SerializeField] private ReactionData reaction;
        [Tooltip("Sağ teraziye konan hedef ürünler ve adetleri (ör. 2 H2O).")]
        [SerializeField] private List<SpeciesAmount> targetProducts = new List<SpeciesAmount>();
        [Tooltip("Sipariş kartında girenler de yazsın mı? (Görev 1–2: evet)")]
        [SerializeField] private bool cardShowsReactants;

        public ReactionData Reaction => reaction;
        public IReadOnlyList<SpeciesAmount> TargetProducts => targetProducts;
        public bool CardShowsReactants => cardShowsReactants;

        public OrderStep() { }

        public OrderStep(ReactionData reaction, IEnumerable<SpeciesAmount> targets, bool cardShowsReactants)
        {
            this.reaction = reaction;
            targetProducts = new List<SpeciesAmount>(targets);
            this.cardShowsReactants = cardShowsReactants;
        }
    }

    [Serializable]
    public struct SpeciesPrice
    {
        [SerializeField] private SpeciesData species;
        [SerializeField] private int price;

        public SpeciesData Species => species;
        public int Price => price;

        public SpeciesPrice(SpeciesData species, int price)
        {
            this.species = species;
            this.price = price;
        }
    }

    /// <summary>Sipariş görevi (02-siparis-mekanigi.md, 04 §3).</summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Görev/Sipariş", fileName = "Order_")]
    public sealed class OrderTask : TaskData
    {
        [SerializeField] private OrderMode mode = OrderMode.Exact;
        [SerializeField] private List<OrderStep> steps = new List<OrderStep>();
        [Tooltip("Raftaki mühürlü kavanozlar. Purchase modunda boş; malzeme sandıktan alınır.")]
        [SerializeField] private List<SpeciesData> shelf = new List<SpeciesData>();
        [Header("Yalnızca Purchase")]
        [SerializeField] private int budget;
        [SerializeField] private List<SpeciesPrice> prices = new List<SpeciesPrice>();

        public OrderMode Mode => mode;
        public IReadOnlyList<OrderStep> Steps => steps;
        public IReadOnlyList<SpeciesData> Shelf => shelf;
        public int Budget => budget;
        public IReadOnlyList<SpeciesPrice> Prices => prices;

        public static OrderTask Create(string id, string giver, string[] lines, string[] delivery, string hint, string realLife,
            OrderMode mode, IEnumerable<OrderStep> steps, IEnumerable<SpeciesData> shelf, int budget = 0,
            IEnumerable<SpeciesPrice> prices = null)
        {
            var t = CreateInstance<OrderTask>();
            t.SetCommon(id, giver, lines, delivery, hint, realLife);
            t.mode = mode;
            t.steps = new List<OrderStep>(steps);
            t.shelf = new List<SpeciesData>(shelf);
            t.budget = budget;
            t.prices = prices != null ? new List<SpeciesPrice>(prices) : new List<SpeciesPrice>();
            t.name = id;
            return t;
        }
    }
}
