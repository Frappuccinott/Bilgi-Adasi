using System;
using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// Hata Dedektifi (04-gorev-plani Sv 3): tahtada katsayısı hatalı bir denklem. Oyuncu dengesiz elementleri
    /// işaretler; doğru küme AtomMath ile hesaplanır (elle yazılmaz). Ardından "Neden?" sorusu.
    /// </summary>
    [Serializable]
    public sealed class ErrorHuntRound
    {
        [SerializeField] private List<SpeciesAmount> reactants = new List<SpeciesAmount>();
        [SerializeField] private List<SpeciesAmount> products = new List<SpeciesAmount>();
        [SerializeField] private ReasoningPrompt why = new ReasoningPrompt();

        public IReadOnlyList<SpeciesAmount> Reactants => reactants;
        public IReadOnlyList<SpeciesAmount> Products => products;
        public ReasoningPrompt Why => why;

        public ErrorHuntRound() { }

        public ErrorHuntRound(List<SpeciesAmount> reactants, List<SpeciesAmount> products, ReasoningPrompt why)
        {
            this.reactants = reactants;
            this.products = products;
            this.why = why;
        }
    }

    [Serializable]
    public sealed class ErrorHuntEntry
    {
        [SerializeField] private string taskId = "";
        [SerializeField] private List<ErrorHuntRound> rounds = new List<ErrorHuntRound>();

        public string TaskId => taskId;
        public IReadOnlyList<ErrorHuntRound> Rounds => rounds;
        public string FlagId => "hata:" + taskId;

        public ErrorHuntEntry() { }

        public ErrorHuntEntry(string taskId, List<ErrorHuntRound> rounds)
        {
            this.taskId = taskId;
            this.rounds = rounds;
        }
    }

    /// <summary>Hata Dedektifi kontrolü. Saf C# (UnityEngine API'si yok); EditMode testleri var.</summary>
    public static class ErrorHuntLogic
    {
        /// <summary>
        /// Denklemdeki elementler (görünme sırasıyla: önce girenler) ve her birinin sol/sağ sayısı.
        /// order, left, right çağıranındır (temizlenir).
        /// </summary>
        public static void Count(IReadOnlyList<SpeciesAmount> reactants, IReadOnlyList<SpeciesAmount> products,
            List<ElementData> order, Dictionary<ElementData, int> left, Dictionary<ElementData, int> right)
        {
            order.Clear();
            AddOrder(reactants, order);
            AddOrder(products, order);
            AtomMath.Count(reactants, left);
            AtomMath.Count(products, right);
        }

        /// <summary>İşaretlenen küme tam olarak dengesiz elementler mi?</summary>
        public static bool IsCorrect(IReadOnlyList<ElementData> order, IReadOnlyDictionary<ElementData, int> left,
            IReadOnlyDictionary<ElementData, int> right, ICollection<ElementData> marked)
        {
            for (int i = 0; i < order.Count; i++)
            {
                var e = order[i];
                left.TryGetValue(e, out int l);
                right.TryGetValue(e, out int r);
                if ((l != r) != marked.Contains(e)) return false;
            }
            return true;
        }

        private static void AddOrder(IReadOnlyList<SpeciesAmount> list, List<ElementData> order)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i].Species;
                if (s == null) continue;
                var comp = s.Composition;
                for (int c = 0; c < comp.Count; c++)
                    if (comp[c].Element != null && !order.Contains(comp[c].Element)) order.Add(comp[c].Element);
            }
        }
    }
}
