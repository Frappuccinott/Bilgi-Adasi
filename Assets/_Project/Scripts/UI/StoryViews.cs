namespace BilgiAdasi.UI
{
    /// <summary>
    /// Hikâye kartı: kararmış ekranın üstünde ortada tek cümle (açılıştaki sınıf, kapanış). Ekranı tamamen örter;
    /// Show ilk çağrıda kartı yumuşakça açar, Hide kapatır.
    /// </summary>
    public interface IStoryCardView
    {
        void Show(string text);
        /// <summary>Kartı solmadan, bu karede tamamen açar (sahne yüklenirken hiçbir şey görünmesin).</summary>
        void ShowImmediate(string text);
        void Hide();
    }
}
