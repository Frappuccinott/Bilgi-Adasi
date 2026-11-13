using System;
using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using UnityEngine;
using Random = System.Random;

namespace BilgiAdasi.Desk
{
    /// <summary>
    /// Doğru / yanlış: her ifade için D ya da Y mührü. İfadenin düzeltmesi varsa Y seçilince altında
    /// düzeltme satırı açılır (T-4 katmanı: yanlış iddiayı düzelt).
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Masa/Soru: Doğru-Yanlış", fileName = "Q_TF_")]
    public sealed class TrueFalseQ : QuestionData
    {
        [Serializable]
        public sealed class Statement
        {
            [TextArea(1, 3)] public string text = "";
            public bool isTrue;
            [Tooltip("Boşsa düzeltme sorulmaz.")]
            public string[] corrections = new string[0];
            public int correctionIndex;
        }

        [SerializeField] private List<Statement> statements = new List<Statement>();
        [SerializeField] private string trueLabel = "D";
        [SerializeField] private string falseLabel = "Y";
        [SerializeField] private string correctionLabel = "Doğrusu:";

        public override PaperQuestion Build(Random rng)
        {
            var q = new PaperQuestion(Id, Prompt, Hint);
            var labels = new[] { trueLabel, falseLabel };
            for (int i = 0; i < statements.Count; i++)
            {
                var s = statements[i];
                int row = q.Rows.Count;
                q.Rows.Add(new PaperRow(s.text, labels, s.isTrue ? 0 : 1, RowStyle.Chips));
                if (s.corrections == null || s.corrections.Length == 0) continue;

                var order = MultipleChoiceQ.Order(s.corrections.Length, rng);
                var shown = new string[order.Count];
                int right = 0;
                for (int k = 0; k < order.Count; k++)
                {
                    shown[k] = s.corrections[order[k]];
                    if (order[k] == s.correctionIndex) right = k;
                }
                q.Rows.Add(new PaperRow(correctionLabel, shown, right, RowStyle.List) { ParentRow = row, ParentOption = 1 });
            }
            return q;
        }

#if UNITY_EDITOR
        public static TrueFalseQ Create(string id, string prompt, string hint, List<Statement> statements)
        {
            var q = CreateInstance<TrueFalseQ>();
            q.SetBase(id, prompt, hint);
            q.statements = statements;
            return q;
        }
#endif
    }
}
