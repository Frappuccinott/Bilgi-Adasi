using System;
using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Bir NPC'nin oyuncuya görünen metinleri (kodda sabit Türkçe metin olmaz): adı, portresi, etkileşim ipucu
    /// ve sırayla açılan sohbetleri. Her Interact bir sonraki sohbeti oynatır, sona gelince başa döner.
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Metin/NPC Profili", fileName = "NpcProfile")]
    public sealed class NpcProfile : ScriptableObject
    {
        [Serializable]
        public sealed class Conversation
        {
            [TextArea(1, 3)] [SerializeField] private string[] lines = Array.Empty<string>();
            public IReadOnlyList<string> Lines => lines;

            public Conversation() { }
            public Conversation(string[] lines) { this.lines = lines; }
        }

        [SerializeField] private string displayName = "Sakin";
        [Tooltip("Konuşma balonundaki 2D portre. Boş bırakılabilir.")]
        [SerializeField] private Sprite portrait;
        [Tooltip("Bakınca tuş simgesinin yanında görünen metin. {0} = ad.")]
        [SerializeField] private string promptFormat = "{0} ile konuş";
        [SerializeField] private Conversation[] conversations = Array.Empty<Conversation>();

        public string DisplayName => displayName;
        public Sprite Portrait => portrait;
        public string PromptFormat => promptFormat;
        public IReadOnlyList<Conversation> Conversations => conversations;

        /// <summary>Kurulum aracı (NpcYerlestir) ilk kez oluştururken doldurur; sonra metinler Inspector'dan düzenlenir.</summary>
        public void Initialize(string name, string prompt, Conversation[] convs)
        {
            displayName = name;
            promptFormat = prompt;
            conversations = convs;
        }
    }
}
