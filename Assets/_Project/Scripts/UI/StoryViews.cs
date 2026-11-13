namespace BilgiAdasi.UI
{
    /// <summary>
    /// Hikâye kartı: kararmış ekranın üstünde ortada tek cümle (açılıştaki sınıf, kapanış). Ekranı tamamen örter;
    /// Show ilk çağrıda kartı yumuşakça açar, Hide kapatır.
    /// </summary>
    public interface IStoryCardView
    {
        void Show(string text);
        void Hide();
    }
}
