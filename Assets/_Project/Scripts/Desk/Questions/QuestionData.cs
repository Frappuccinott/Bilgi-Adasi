using System;
using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using UnityEngine;
using Random = System.Random;

namespace BilgiAdasi.Desk
{
    /// <summary>
    /// Masa sorusu (03 §4). Her tip kâğıda bir PaperQuestion açar; kontrol mantığı orada (saf C#).
    /// Oyuncuya görünen metinler burada; formüller TMP alt indisli (&lt;sub&gt;).
    /// </summary>
    public abstract class QuestionData : ScriptableObject
    {
        [Tooltip("Benzersiz kimlik: tur_1_q1, lonca_mc_1 …")]
        [SerializeField] private string id = "";
        [TextArea(2, 5)] [SerializeField] private string prompt = "";
        [Tooltip("2 hatadan sonra açılan ustanın notu (03 §5). Boşsa görevin ipucu.")]
        [TextArea(2, 4)] [SerializeField] private string hint = "";

        public string Id => id;
        public string Prompt => prompt;
        public string Hint => hint;

        /// <summary>Kâğıda açar. rng: şık/kart sırası karıştırma (null = karıştırma yok).</summary>
        public abstract PaperQuestion Build(Random rng);

        protected void SetBase(string questionId, string questionPrompt, string questionHint)
        {
            id = questionId;
            prompt = questionPrompt;
            hint = questionHint;
            name = questionId;
        }
    }
}
