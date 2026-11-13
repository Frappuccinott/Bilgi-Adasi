using System;
using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using UnityEngine;
using Random = System.Random;

namespace BilgiAdasi.Desk
{
    /// <summary>Denkleştirme: ReactionData'dan kendiliğinden üretilir (soru yazmaya gerek yok, 03 §4).</summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Masa/Soru: Denkleştirme", fileName = "Q_BE_")]
    public sealed class BalanceEquationQ : QuestionData
    {
        [SerializeField] private ReactionData reaction;

        public ReactionData Reaction => reaction;

        public override PaperQuestion Build(Random rng) => new BalancePaper(Id, Prompt, Hint, reaction);

#if UNITY_EDITOR
        public static BalanceEquationQ Create(string id, string prompt, string hint, ReactionData reaction)
        {
            var q = CreateInstance<BalanceEquationQ>();
            q.SetBase(id, prompt, hint);
            q.reaction = reaction;
            return q;
        }
#endif
    }
}
