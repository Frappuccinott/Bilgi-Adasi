using System;
using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Ders Notu sayfasındaki bir parçanın türü (Figma: Keşif Defteri ve Başarılar / Ders Notu/1–2).
    /// View her türü kendi biçimiyle çizer; içerik yalnızca metindir.
    /// </summary>
    public enum LessonBlockKind
    {
        /// <summary>Küçük bakır üst etiket + soluk ayrıntı: "Ders notu  Denkleştirme Köyü, 1. görev".</summary>
        Kicker,
        /// <summary>Sayfanın büyük başlığı.</summary>
        Title,
        /// <summary>Bakır ara başlık.</summary>
        Subhead,
        /// <summary>Gövde paragrafı.</summary>
        Body,
        /// <summary>Denklem satırı (TMP alt indis: H&lt;sub&gt;2&lt;/sub&gt;O).</summary>
        Equation,
        /// <summary>Terazi sayım tablosu: element, sol kefe, sağ kefe, eşit / eşit değil.</summary>
        Counts,
        /// <summary>Numaralı adımlar (items).</summary>
        Steps,
        /// <summary>Sol çizgili not (bakır): "Unutma".</summary>
        Note,
        /// <summary>Sol çizgili uyarı (mühür kırmızısı): "Dikkat", kavram yanılgısı.</summary>
        Warning,
        /// <summary>İnce ayırıcı çizgi.</summary>
        Rule,
        /// <summary>Soluk küçük dipnot (kaynak, tarih).</summary>
        Footnote
    }

    /// <summary>Sayım tablosunun bir satırı: bir elementin iki kefedeki atom sayısı.</summary>
    [Serializable]
    public struct LessonCount
    {
        [SerializeField] private string element;
        [SerializeField] private int left;
        [SerializeField] private int right;

        public string Element => element;
        public int Left => left;
        public int Right => right;
        public bool Balanced => left == right;

        public LessonCount(string element, int left, int right)
        {
            this.element = element;
            this.left = left;
            this.right = right;
        }
    }

    [Serializable]
    public sealed class LessonBlock
    {
        [SerializeField] private LessonBlockKind kind;
        [Tooltip("Başlık, paragraf, denklem; Note/Warning için not gövdesi; Kicker için bakır etiket.")]
        [TextArea(1, 5)] [SerializeField] private string text = "";
        [Tooltip("Kicker: soluk ayrıntı. Note/Warning: not başlığı (Dikkat, Unutma).")]
        [SerializeField] private string detail = "";
        [Tooltip("Steps: her satır bir adım.")]
        [TextArea(1, 3)] [SerializeField] private string[] items = new string[0];
        [Tooltip("Counts: sayım tablosu satırları.")]
        [SerializeField] private LessonCount[] counts = new LessonCount[0];

        public LessonBlockKind Kind => kind;
        public string Text => text;
        public string Detail => detail;
        public IReadOnlyList<string> Items => items;
        public IReadOnlyList<LessonCount> Counts => counts;

        public LessonBlock() { }

        public LessonBlock(LessonBlockKind kind, string text, string detail = "", string[] items = null, LessonCount[] counts = null)
        {
            this.kind = kind;
            this.text = text ?? string.Empty;
            this.detail = detail ?? string.Empty;
            this.items = items ?? new string[0];
            this.counts = counts ?? new LessonCount[0];
        }
    }

    /// <summary>Defterin bir sayfası (sol ya da sağ). Sayfalar ikişer ikişer açık defterde gösterilir.</summary>
    [Serializable]
    public sealed class LessonPage
    {
        [SerializeField] private List<LessonBlock> blocks = new List<LessonBlock>();

        public IReadOnlyList<LessonBlock> Blocks => blocks;

        public LessonPage() { }

        public LessonPage(params LessonBlock[] blocks) => this.blocks = new List<LessonBlock>(blocks);
    }

    /// <summary>
    /// Görevden önceki konu anlatımı (13-ders-notu-plani): 1. açılış konu + yöntem, 2. açılış çözümlü örnek + "bu görevde".
    /// Sayfa yoksa not gösterilmez.
    /// </summary>
    [Serializable]
    public sealed class LessonNote
    {
        [Tooltip("Menüdeki ve kayıttaki kısa ad: \"Atomlar kaybolmaz\".")]
        [SerializeField] private string title = "";
        [SerializeField] private List<LessonPage> pages = new List<LessonPage>();

        public string Title => title;
        public IReadOnlyList<LessonPage> Pages => pages;
        public bool HasPages => pages != null && pages.Count > 0;

        public LessonNote() { }

        public LessonNote(string title, params LessonPage[] pages)
        {
            this.title = title;
            this.pages = new List<LessonPage>(pages);
        }
    }

    /// <summary>Ders Notu ekranının sabit kelimeleri (kodda metin yok).</summary>
    [Serializable]
    public sealed class LessonTexts
    {
        [SerializeField] private string tabWord = "Ustanın Notları";
        [Tooltip("{0} = açılış no, {1} = açılış sayısı.")]
        [SerializeField] private string pageFormat = "Sayfa {0} / {1}";
        [SerializeField] private string turnWord = "Sayfa";
        [SerializeField] private string nextWord = "Devam";
        [SerializeField] private string startWord = "Göreve başla";
        [SerializeField] private string closeWord = "Kapat";
        [SerializeField] private string elementHeader = "Element";
        [SerializeField] private string leftHeader = "Sol kefe";
        [SerializeField] private string rightHeader = "Sağ kefe";
        [SerializeField] private string equalWord = "eşit";
        [SerializeField] private string notEqualWord = "eşit değil";
        [Tooltip("Kapı konuşmasının sonunda ustanın satırı; ardından ders notu açılır.")]
        [TextArea(1, 2)] [SerializeField] private string introLine = "Dur çırak, kazana koşmadan önce şu notu bir oku.";

        public string TabWord => tabWord;
        public string PageFormat => pageFormat;
        public string TurnWord => turnWord;
        public string NextWord => nextWord;
        public string StartWord => startWord;
        public string CloseWord => closeWord;
        public string ElementHeader => elementHeader;
        public string LeftHeader => leftHeader;
        public string RightHeader => rightHeader;
        public string EqualWord => equalWord;
        public string NotEqualWord => notEqualWord;
        public string IntroLine => introLine;
    }
}
