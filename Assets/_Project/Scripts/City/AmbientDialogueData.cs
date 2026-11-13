using System;
using System.Collections.Generic;
using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Yol sohbeti (10-gorev-pazar-npc §6): oyuncu tetik bölgesine girince iki sakinin kendi arasında konuştuğu
    /// 2–4 satır. Oyuncuya soru sorulmaz, durmak gerekmez; altyazı olarak akar. Koşul görev kimlikleriyle yazılır
    /// (görevler sırayla bittiği için "D≥2" = denk_2 bitti, "D&lt;1" = denk_1 bitmedi).
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Metin/Yol Sohbeti", fileName = "S-00")]
    public sealed class AmbientDialogueData : ScriptableObject
    {
        [Serializable]
        public struct Line
        {
            [SerializeField] private string speaker;
            [TextArea(1, 3)] [SerializeField] private string text;

            public string Speaker => speaker;
            public string Text => text;

            public Line(string speaker, string text)
            {
                this.speaker = speaker;
                this.text = text;
            }
        }

        [Tooltip("Kayıtta kullanılan kimlik: S-01 …")]
        [SerializeField] private string id = "S-00";
        [SerializeField] private Line[] lines = Array.Empty<Line>();

        [Header("Koşul (görev kimlikleri: denk_n, tur_n)")]
        [Tooltip("Hepsi bitmiş olmalı.")]
        [SerializeField] private string[] requireAll = Array.Empty<string>();
        [Tooltip("Boş değilse en az biri bitmiş olmalı (ör. iki köyden biri).")]
        [SerializeField] private string[] requireAny = Array.Empty<string>();
        [Tooltip("Hiçbiri bitmemiş olmalı (ör. çeşme kuruyken: denk_1).")]
        [SerializeField] private string[] blockedBy = Array.Empty<string>();
        [Tooltip("Bir kez çalar (kayda yazılır).")]
        [SerializeField] private bool playOnce = true;

        public string Id => id;
        public IReadOnlyList<Line> Lines => lines;
        public bool PlayOnce => playOnce;

        /// <summary>Koşul tutuyor mu? Kayıt yoksa hiçbir görev bitmemiş sayılır.</summary>
        public bool IsEligible(SaveData save)
        {
            if (lines == null || lines.Length == 0) return false;
            for (int i = 0; i < requireAll.Length; i++)
                if (!Done(save, requireAll[i])) return false;
            for (int i = 0; i < blockedBy.Length; i++)
                if (Done(save, blockedBy[i])) return false;
            if (requireAny.Length == 0) return true;
            for (int i = 0; i < requireAny.Length; i++)
                if (Done(save, requireAny[i])) return true;
            return false;
        }

        private static bool Done(SaveData save, string taskId) =>
            save != null && !string.IsNullOrEmpty(taskId) && save.IsTaskCompleted(taskId);

        /// <summary>Editör veri üreticisi için (AmbientDialogueBuilder).</summary>
        public void Initialize(string dialogueId, Line[] dialogueLines, string[] all, string[] any, string[] blocked, bool once = true)
        {
            id = dialogueId;
            lines = dialogueLines ?? Array.Empty<Line>();
            requireAll = all ?? Array.Empty<string>();
            requireAny = any ?? Array.Empty<string>();
            blockedBy = blocked ?? Array.Empty<string>();
            playOnce = once;
        }
    }
}
