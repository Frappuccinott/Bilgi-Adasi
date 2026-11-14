using System;
using System.Collections.Generic;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Prototip kapanışı (05-icerik-prototip §4): önce ustanın veda mektubu (Figma Ekran/Masa kâğıdı şablonu), sonra
    /// Emeği Geçenler (Figma Ekran/Emeği Geçenler; yalnızca isimler). Denetleyici: City.EndingDirector.
    /// </summary>
    public interface IEndingView
    {
        /// <summary>Mektup: üstte küçük gönderen etiketi, başlık, metin, sağ altta imza + mühür; altta tuş ipucu. Onay: onContinue.</summary>
        void ShowLetter(string fromLabel, string title, string body, string signature, string continueWord, Action onContinue);

        /// <summary>Jenerik: logo, isimler, "Devam edecek"; onay ya da geri: onDone.</summary>
        void ShowCredits(string logo, IReadOnlyList<string> names, string teaser, string doneWord, Action onDone);

        void Hide();
    }
}
