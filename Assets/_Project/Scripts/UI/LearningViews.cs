using System;
using System.Collections.Generic;

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
}
