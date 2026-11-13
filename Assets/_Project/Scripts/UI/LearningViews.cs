using System;
using System.Collections.Generic;
using BilgiAdasi.Core;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Ustanın Sorgusu: soru + seçenekler; seçimden sonra ustanın cevabı ve (varsa) defter notu "Devam" ile geçilir.
    /// Klavye/gamepad: seçenekler EventSystem ile seçilir, onay Submit.
    /// </summary>
    public interface IReasoningView
    {
        void ShowQuestion(string speaker, string question, IReadOnlyList<string> options, Action<int> onChosen);
        void ShowMessage(string speaker, string text, string continueWord, Action onContinue);
        void Hide();
    }

    /// <summary>Keşif Defteri: oyuncunun kendi yazdığı sayfalar (K-1, K-2 …).</summary>
    public interface IDiscoveryBookView
    {
        bool IsOpen { get; }
        void Open(string title, IReadOnlyList<string> pages, string emptyText, string closeWord, Action onClosed);
        void Close();
    }

    /// <summary>View → kod: Keşif Defteri'ni aç (Esc menüsündeki düğme).</summary>
    public interface IDiscoveryBookCommands
    {
        void OpenBook(Action onClosed);
    }

    /// <summary>
    /// Ders Notu (görevden önce konu anlatımı): açık defter, sayfalar ikişer ikişer. Sayfa çevirme view'ın işidir
    /// (UI Navigate ← →, Submit ileri); son açılışta Submit notu kapatır ve onFinished çağrılır.
    /// </summary>
    public interface ILessonView
    {
        bool IsOpen { get; }
        /// <param name="finishWord">Son açılıştaki onay kelimesi: "Göreve başla" ya da (menüden) "Kapat".</param>
        void Open(LessonNote note, LessonTexts texts, string finishWord, Action onFinished);
        void Close();
    }

    /// <summary>
    /// Defter (Figma: Keşif Defteri ve Başarılar / Defter/1–7): üç ayraç, ders şeritleri, açılış canlandırması.
    /// Ayraç Q / E (LB / RB), seçim ↑ ↓, okuma Enter / A, kapatma Esc / B. Görev öncesi Ders Notu da bu defterde açılır
    /// (aynı view ILessonView'ı da uygular).
    /// </summary>
    public interface INotebookView
    {
        bool IsOpen { get; }
        void OpenBook(NotebookModel model, NotebookTab tab, Action onClosed);
        void Close();
    }

    /// <summary>Kod → view: defterin anlık görüntüsünü kurar (NotebookDirector).</summary>
    public interface INotebookSource
    {
        NotebookModel BuildModel();
    }

    /// <summary>HUD'daki defter sırtı: sol altta "N Defter"; yeni kayıtta "Deftere yeni sayfa / K-1 Kuru Çeşme" + parlama.</summary>
    public interface INotebookHudView
    {
        void ShowNewPage(string code, string title);
    }

    /// <summary>View → kod: şu anki görevin ders notunu yeniden aç (Esc menüsündeki düğme).</summary>
    public interface ILessonCommands
    {
        /// <summary>Alınmış bir görevin notu varsa true.</summary>
        bool HasCurrentLesson { get; }
        void OpenCurrentLesson(Action onClosed);
    }
}

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Hata Dedektifi tahtası (dünya uzayı, karatahtanın üstünde): başlık, gövde (denklem + seçenekler, zengin metin),
    /// alt satır (tuş ipuçları). Denetleyici metni hazır verir; view yalnızca yazar ve normal tahta yazısını gizler.
    /// </summary>
    public interface IErrorHuntView
    {
        void ShowHunt(string title, string body, string footer);
        void HideHunt();
        /// <summary>Tahtada bekleyen bir dedektif işi var (işaret, "?" gibi).</summary>
        void SetPending(bool pending);
    }
}
