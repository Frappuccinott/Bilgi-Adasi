namespace BilgiAdasi.UI
{
    /// <summary>
    /// Hikâye kartı ve açılış sinematiği. İki hâli var:
    /// • Kart (Show/ShowImmediate): göz kapakları kapalı, karanlıkta ortada tek cümle (sınıfta uyuyakalma).
    /// • Kare (ShowShot): göz kapakları sinema şeridine kadar açık, arkada oyun dünyası; sol altta yer adı + cümle.
    /// Göz kapakları SetLids ile açılıp kapanır (kareler arası kesme, uyanış). Hide her şeyi kaldırır.
    /// Açıkken HUD'un konuşmada çekilen parçaları da çekilir.
    /// </summary>
    public interface IStoryCardView
    {
        /// <summary>
        /// Bir dizi başlar: altta <paramref name="count"/> adımlık ilerleme ve sağ altta ipuçları (kareyi geç / atla).
        /// Sonraki her Show/ShowImmediate/ShowShot bir adım ilerler.
        /// </summary>
        void BeginSequence(int count, string nextWord, string skipWord);
        void Show(string text);
        /// <summary>Kartı solmadan, bu karede tamamen açar (sahne yüklenirken hiçbir şey görünmesin).</summary>
        void ShowImmediate(string text);
        /// <summary>Sinematik kare yazısı (sol alt). Göz kapaklarını açmaz; SetLids ile birlikte kullanılır.</summary>
        void ShowShot(string kicker, string text);
        /// <summary>Göz kapakları: 0 = kapalı (karanlık), 1 = sinema şeridi kadar açık. Süre sn (gerçek zaman).</summary>
        void SetLids(float open, float seconds);
        /// <summary>Kapaklar tam açılır, yazılar söner; görünüm kapanır.</summary>
        void Hide();
    }
}
