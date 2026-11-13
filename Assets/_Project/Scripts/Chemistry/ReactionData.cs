using System;
using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.Chemistry
{
    public enum ReactionType
    {
        Synthesis,   // Sentez (oluşum)
        Analysis,    // Analiz (ayrışma)
        Combustion   // Yanma
    }

    /// <summary>Bir tür + adet. Hem tepkime katsayısı hem kazandaki kavanoz sayısı için kullanılır.</summary>
    [Serializable]
    public struct SpeciesAmount
    {
        [SerializeField] private SpeciesData species;
        [SerializeField] private int amount;

        public SpeciesData Species => species;
        public int Amount => amount;

        public SpeciesAmount(SpeciesData species, int amount)
        {
            this.species = species;
            this.amount = amount;
        }
    }

    /// <summary>Denkleşmiş bir tepkime: 2H₂ + O₂ → 2H₂O.</summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Kimya/Tepkime", fileName = "R_")]
    public sealed class ReactionData : ScriptableObject
    {
        [SerializeField] private string id = "R_";
        [SerializeField] private List<SpeciesAmount> reactants = new List<SpeciesAmount>();
        [SerializeField] private List<SpeciesAmount> products = new List<SpeciesAmount>();
        [SerializeField] private ReactionType type;

        public string Id => id;
        public IReadOnlyList<SpeciesAmount> Reactants => reactants;
        public IReadOnlyList<SpeciesAmount> Products => products;
        public ReactionType Type => type;

        /// <summary>Bu türün tepkimedeki katsayısı (giren ya da ürün); yoksa 0.</summary>
        public int CoefficientOf(SpeciesData species)
        {
            for (int i = 0; i < reactants.Count; i++)
                if (reactants[i].Species == species) return reactants[i].Amount;
            for (int i = 0; i < products.Count; i++)
                if (products[i].Species == species) return products[i].Amount;
            return 0;
        }

        public static ReactionData Create(string id, ReactionType type, SpeciesAmount[] reactants, SpeciesAmount[] products)
        {
            var r = CreateInstance<ReactionData>();
            r.id = id;
            r.type = type;
            r.reactants = new List<SpeciesAmount>(reactants);
            r.products = new List<SpeciesAmount>(products);
            r.name = id;
            return r;
        }
    }
}
