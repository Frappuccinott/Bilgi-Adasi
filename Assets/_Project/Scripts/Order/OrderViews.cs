using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>Bir element satırı: H 4 / 4.</summary>
    public readonly struct AtomCountRow
    {
        public AtomCountRow(ElementData element, int left, int right)
        {
            Element = element;
            Left = left;
            Right = right;
        }

        public ElementData Element { get; }
        public int Left { get; }
        public int Right { get; }
        public bool Equal => Left == Right;
    }

    /// <summary>Raftaki bir kavanoz: tür, kazandaki adet, alt etiket (fiyat vb.).</summary>
    public readonly struct ShelfItem
    {
        public ShelfItem(SpeciesData species, int count, string label)
        {
            Species = species;
            Count = count;
            Label = label;
        }

        public SpeciesData Species { get; }
        public int Count { get; }
        public string Label { get; }
    }

    /// <summary>Sipariş kartı + sayaç tablosu + kontrol yardımı (ekran).</summary>
    public interface IOrderCardView
    {
        void Show(string title, string body);
        void SetCounts(IReadOnlyList<AtomCountRow> rows, bool balanced);
        void SetHelp(string text);
        void Hide();
    }

    /// <summary>İki terazi: GİREN (kazandakiler) ve ÇIKAN (hedef). Atomlar kefelerde element başına sıra hâlinde.</summary>
    public interface IScalesView
    {
        void ShowAtoms(IReadOnlyList<AtomCountRow> rows, bool balanced);
        /// <summary>Köpürmede: fazla (+) atomlar kırmızı yanıp söner, eksik (−) yerler boş halka.</summary>
        void FlashDifference(IReadOnlyDictionary<ElementData, int> difference);
        void SetTitles(string left, string right);
        void Clear();
    }

    /// <summary>Kazan + ocak canlandırmaları (Kazan_Rig, Ocak_Ates_Rig).</summary>
    public interface ICauldronView
    {
        void PlayStir();
        void SetBoiling(bool boiling);
        void SetFire(bool lit);
        void PlayFoam();
        void PlaySuccess();
        float SuccessDuration { get; }
    }

    /// <summary>Raf: kavanozları dizer; her kavanozda StationTarget olur.</summary>
    public interface IShelfView
    {
        void SetItems(IReadOnlyList<ShelfItem> items);
        void Shake(SpeciesData species);
        void Pour(SpeciesData species);
    }

    /// <summary>Karatahta: biten görevlerin denklemleri (TMP zengin metin).</summary>
    public interface IChalkboardView
    {
        void SetEquations(IReadOnlyList<string> richEquations);
    }

    /// <summary>İstasyonda tıklanabilen/seçilebilen nesne (kavanoz, ocak, defter).</summary>
    public enum StationTargetKind
    {
        Jar,
        Stove,
        HintBook
    }
}
