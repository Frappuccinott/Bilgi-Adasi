using System;

namespace BilgiAdasi.Core
{
    /// <summary>Ana menüde "Devam Et"in altındaki kayıt özeti: sıradaki köy ve biten görev sayısı.</summary>
    public sealed class MainMenuSaveSummary
    {
        /// <summary>Köyün adı (biçimlenmemiş, NeighborhoodData.Title).</summary>
        public string Place;
        public int Done;
        public int Total;
    }

    /// <summary>
    /// Ana menü görünümü. Denetleyici (MainMenuController) düğmelerin ne yapacağını verir; Ayarlar ve Emeği Geçenler
    /// görünümün kendi sayfalarıdır (oyun durumu değiştirmez).
    /// </summary>
    public interface IMainMenuView
    {
        /// <summary>Menüyü kurar. canContinue false ise "Devam Et" pasif görünür; summary null ise özet gizlenir.</summary>
        void Show(MenuTexts texts, bool canContinue, MainMenuSaveSummary summary, Action onNewGame, Action onContinue,
            Action onQuit);

        /// <summary>Kayıt varken yeni oyun onayı: onaylanırsa confirm, vazgeçilirse cancel.</summary>
        void AskNewGame(Action confirm, Action cancel);

        /// <summary>Düğmeleri kilitler (sahne yüklenirken çift basış olmasın).</summary>
        void SetBusy(bool busy);

        /// <summary>UI/Cancel (Esc · B): açık sayfa ya da onay varsa kapatır ve true döner; ana sayfadaysa false.</summary>
        bool TryGoBack();
    }

    /// <summary>Sağ üstte, kesenin altında kısa "Otomatik kaydedildi…" bildirimi (sağdan gelir, sağa gider).</summary>
    public interface IAutoSaveNoticeView
    {
        void ShowSaved(string text);
    }
}
