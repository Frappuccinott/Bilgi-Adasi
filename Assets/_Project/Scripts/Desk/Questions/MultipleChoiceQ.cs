using System;
using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using UnityEngine;
using Random = System.Random;

namespace BilgiAdasi.Desk
{
    /// <summary>
    /// Çoktan seçmeli. Tek doğru: şıklar alt alta, her açılışta karışık.
    /// allowMultiple: her şık bir kanıt kartı, oyuncu "Kanıt / Değil" işaretler (T-3 kanıt katmanı).
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Masa/Soru: Çoktan seçmeli", fileName = "Q_MC_")]
    public sealed class MultipleChoiceQ : QuestionData
    {
        [SerializeField] private string[] options = new string[4];
        [Tooltip("Doğru şıkların sırası. Tek seçimde yalnızca ilki geçerli.")]
        [SerializeField] private int[] correct = { 0 };
        [SerializeField] private bool allowMultiple;
        [SerializeField] private string yesLabel = "Kanıt";
        [SerializeField] private string noLabel = "Değil";

        public override PaperQuestion Build(Random rng)
        {
            var q = new PaperQuestion(Id, Prompt, Hint);
            if (!allowMultiple)
            {
                var order = Order(options.Length, rng);
                var shown = new string[options.Length];
                int right = 0;
                for (int i = 0; i < order.Count; i++)
                {
                    shown[i] = options[order[i]];
                    if (correct.Length > 0 && order[i] == correct[0]) right = i;
                }
                q.Rows.Add(new PaperRow(string.Empty, shown, right, RowStyle.List));
                return q;
            }

            var labels = new[] { yesLabel, noLabel };
            var cards = Order(options.Length, rng);
            for (int i = 0; i < cards.Count; i++)
            {
                bool isEvidence = Array.IndexOf(correct, cards[i]) >= 0;
                q.Rows.Add(new PaperRow(options[cards[i]], labels, isEvidence ? 0 : 1, RowStyle.Chips));
            }
            return q;
        }

        internal static List<int> Order(int count, Random rng)
        {
            var list = new List<int>(count);
            for (int i = 0; i < count; i++) list.Add(i);
            PaperQuestion.Shuffle(list, rng);
            return list;
        }

#if UNITY_EDITOR
        public static MultipleChoiceQ Create(string id, string prompt, string hint, string[] options, int[] correct,
            bool allowMultiple = false)
        {
            var q = CreateInstance<MultipleChoiceQ>();
            q.SetBase(id, prompt, hint);
            q.options = options;
            q.correct = correct;
            q.allowMultiple = allowMultiple;
            return q;
        }
#endif
    }
}
