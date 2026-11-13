using System;
using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using UnityEngine;
using Random = System.Random;

namespace BilgiAdasi.Desk
{
    /// <summary>Sınıflandırma (Etiket Masası): her kart bir kutuya. Kart bir tepkime (denklem) ya da gözlem cümlesi.</summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Masa/Soru: Sınıflandırma", fileName = "Q_CL_")]
    public sealed class ClassifyQ : QuestionData
    {
        [Serializable]
        public sealed class Card
        {
            [Tooltip("Doluysa kartta bu tepkimenin denklemi yazar.")]
            public ReactionData reaction;
            [Tooltip("Tepkime yoksa kartın metni (formülsüz gözlem).")]
            [TextArea(1, 3)] public string text = "";
            public int box;
        }

        [SerializeField] private string[] boxes = { "Sentez", "Analiz", "Yanma" };
        [SerializeField] private List<Card> cards = new List<Card>();

        public override PaperQuestion Build(Random rng)
        {
            var q = new PaperQuestion(Id, Prompt, Hint);
            var order = MultipleChoiceQ.Order(cards.Count, rng);
            for (int i = 0; i < order.Count; i++)
            {
                var c = cards[order[i]];
                string label = c.reaction != null ? FormulaFormatter.Equation(c.reaction) : c.text;
                q.Rows.Add(new PaperRow(label, boxes, c.box, RowStyle.Chips));
            }
            return q;
        }

#if UNITY_EDITOR
        public static ClassifyQ Create(string id, string prompt, string hint, string[] boxes, List<Card> cards)
        {
            var q = CreateInstance<ClassifyQ>();
            q.SetBase(id, prompt, hint);
            q.boxes = boxes;
            q.cards = cards;
            return q;
        }
#endif
    }
}
