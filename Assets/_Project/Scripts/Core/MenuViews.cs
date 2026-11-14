using System;

namespace BilgiAdasi.Core
{
    /// <summary>Ana menü görünümü. Denetleyici (MainMenuController) düğmelerin ne yapacağını verir.</summary>
    public interface IMainMenuView
    {
        /// <summary>Menüyü kurar. canContinue false ise "Devam et" kapalı görünür.</summary>
        void Show(MenuTexts texts, bool canContinue, Action onPlay, Action onContinue, Action onQuit);

        /// <summary>Kayıt varken yeni oyun onayı: onaylanırsa confirm, vazgeçilirse cancel.</summary>
        void AskNewGame(Action confirm, Action cancel);

        /// <summary>Düğmeleri kilitler (sahne yüklenirken çift basış olmasın).</summary>
        void SetBusy(bool busy);
    }

    /// <summary>Sağ üstte, kesenin altında kısa "Otomatik kaydedildi…" bildirimi (sağdan gelir, sağa gider).</summary>
    public interface IAutoSaveNoticeView
    {
        void ShowSaved(string text);
    }
}
