using System;
using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using UnityEngine;
using Random = System.Random;

namespace BilgiAdasi.Desk
{
    /// <summary>Boşluk doldurma (Ustanın Defteri): kelime bankası + 1–2 çeldirici; cümlede {0}, {1} boşluklar.</summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Masa/Soru: Boşluk doldurma", fileName = "Q_FB_")]
    public sealed class FillBlankQ : QuestionData
    {
        [Serializable]
        public sealed class Sentence
        {
            [Tooltip("Boşluklar {0}, {1} …")]
            [TextArea(1, 3)] public string text = "";
            [Tooltip("Her boşluğun doğru kelimesi (bankadaki yazılışıyla).")]
            public string[] answers = new string[0];
        }

        [Tooltip("Doğru kelimeler + çeldiriciler. Her açılışta karışık sırada.")]
        [SerializeField] private string[] wordBank = new string[0];
        [SerializeField] private List<Sentence> sentences = new List<Sentence>();

        public override PaperQuestion Build(Random rng)
        {
            var q = new FillBlankPaper(Id, Prompt, Hint);
            var order = MultipleChoiceQ.Order(wordBank.Length, rng);
            var bank = new string[order.Count];
            for (int i = 0; i < order.Count; i++) bank[i] = wordBank[order[i]];

            for (int s = 0; s < sentences.Count; s++)
            {
                var answers = new int[sentences[s].answers.Length];
                for (int k = 0; k < answers.Length; k++) answers[k] = Array.IndexOf(bank, sentences[s].answers[k]);
                q.AddSentence(sentences[s].text, answers, bank);
            }
            return q;
        }

#if UNITY_EDITOR
        public static FillBlankQ Create(string id, string prompt, string hint, string[] wordBank, List<Sentence> sentences)
        {
            var q = CreateInstance<FillBlankQ>();
            q.SetBase(id, prompt, hint);
            q.wordBank = wordBank;
            q.sentences = sentences;
            return q;
        }
#endif
    }
}
