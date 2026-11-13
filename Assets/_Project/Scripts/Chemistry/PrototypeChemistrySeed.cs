#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.Chemistry
{
    /// <summary>
    /// 05-icerik-prototip.md §1 tablosunun kod karşılığı. YALNIZCA editör veri üreticisi ve testler kullanır;
    /// oyun verinin kendisini SO asset'lerinden okur. Build'e girmez.
    /// </summary>
    public sealed class PrototypeChemistrySeed
    {
        public readonly Dictionary<string, ElementData> Elements = new Dictionary<string, ElementData>();
        public readonly Dictionary<string, SpeciesData> Species = new Dictionary<string, SpeciesData>();
        public readonly Dictionary<string, ReactionData> Reactions = new Dictionary<string, ReactionData>();

        public static PrototypeChemistrySeed Build()
        {
            var s = new PrototypeChemistrySeed();

            s.AddElement("H", "Hidrojen", Color.white);
            s.AddElement("O", "Oksijen", new Color(0.86f, 0.18f, 0.18f));
            s.AddElement("C", "Karbon", new Color(0.25f, 0.25f, 0.25f));
            s.AddElement("N", "Azot", new Color(0.2f, 0.4f, 0.9f));
            s.AddElement("Ca", "Kalsiyum", new Color(0.6f, 0.9f, 0.55f));

            s.AddSpecies("H2", "Hidrojen", ("H", 2));
            s.AddSpecies("O2", "Oksijen", ("O", 2));
            s.AddSpecies("H2O", "Su", ("H", 2), ("O", 1));
            s.AddSpecies("CH4", "Metan", ("C", 1), ("H", 4));
            s.AddSpecies("CO2", "Karbondioksit", ("C", 1), ("O", 2));
            s.AddSpecies("C3H8", "Propan", ("C", 3), ("H", 8));
            s.AddSpecies("CaCO3", "Kireç taşı", ("Ca", 1), ("C", 1), ("O", 3));
            s.AddSpecies("CaO", "Sönmemiş kireç", ("Ca", 1), ("O", 1));
            s.AddSpecies("Ca(OH)2", "Sönmüş kireç", ("Ca", 1), ("O", 2), ("H", 2));
            s.AddSpecies("N2", "Azot", ("N", 2));
            s.AddSpecies("NH3", "Amonyak", ("N", 1), ("H", 3));
            s.AddSpecies("CO", "Karbon monoksit", ("C", 1), ("O", 1));
            s.AddSpecies("C6H12O6", "Glikoz", ("C", 6), ("H", 12), ("O", 6));

            s.AddReaction("R_water", ReactionType.Synthesis, new[] { (2, "H2"), (1, "O2") }, new[] { (2, "H2O") });
            s.AddReaction("R_methane", ReactionType.Combustion, new[] { (1, "CH4"), (2, "O2") }, new[] { (1, "CO2"), (2, "H2O") });
            s.AddReaction("R_propane", ReactionType.Combustion, new[] { (1, "C3H8"), (5, "O2") }, new[] { (3, "CO2"), (4, "H2O") });
            s.AddReaction("R_lime1", ReactionType.Analysis, new[] { (1, "CaCO3") }, new[] { (1, "CaO"), (1, "CO2") });
            s.AddReaction("R_lime2", ReactionType.Synthesis, new[] { (1, "CaO"), (1, "H2O") }, new[] { (1, "Ca(OH)2") });
            s.AddReaction("R_ammonia", ReactionType.Synthesis, new[] { (1, "N2"), (3, "H2") }, new[] { (2, "NH3") });
            s.AddReaction("R_co", ReactionType.Combustion, new[] { (2, "CO"), (1, "O2") }, new[] { (2, "CO2") });
            s.AddReaction("R_waterSplit", ReactionType.Analysis, new[] { (2, "H2O") }, new[] { (2, "H2"), (1, "O2") });
            // Lonca Sınavı transferi (04 §4 Seviye 10): tür kutularına tam uymaz.
            s.AddReaction("R_photosynthesis", ReactionType.Other, new[] { (6, "CO2"), (6, "H2O") }, new[] { (1, "C6H12O6"), (6, "O2") });
            s.AddReaction("R_respiration", ReactionType.Other, new[] { (1, "C6H12O6"), (6, "O2") }, new[] { (6, "CO2"), (6, "H2O") });

            return s;
        }

        private void AddElement(string symbol, string name, Color color) =>
            Elements[symbol] = ElementData.Create(symbol, name, color);

        private void AddSpecies(string formula, string name, params (string symbol, int count)[] parts)
        {
            var comp = new ElementCount[parts.Length];
            for (int i = 0; i < parts.Length; i++) comp[i] = new ElementCount(Elements[parts[i].symbol], parts[i].count);
            Species[formula] = SpeciesData.Create(formula, name, comp);
        }

        private void AddReaction(string id, ReactionType type, (int coef, string formula)[] reactants, (int coef, string formula)[] products)
        {
            Reactions[id] = ReactionData.Create(id, type, ToTerms(reactants), ToTerms(products));
        }

        private SpeciesAmount[] ToTerms((int coef, string formula)[] terms)
        {
            var result = new SpeciesAmount[terms.Length];
            for (int i = 0; i < terms.Length; i++) result[i] = new SpeciesAmount(Species[terms[i].formula], terms[i].coef);
            return result;
        }

        /// <summary>Oluşturulan geçici (asset olmayan) örnekleri yok eder. Testlerde TearDown'da çağrılır.</summary>
        public void DestroyAll()
        {
            foreach (var r in Reactions.Values) Object.DestroyImmediate(r);
            foreach (var sp in Species.Values) Object.DestroyImmediate(sp);
            foreach (var e in Elements.Values) Object.DestroyImmediate(e);
        }
    }
}
#endif
