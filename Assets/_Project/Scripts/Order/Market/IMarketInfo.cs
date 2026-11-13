namespace BilgiAdasi.Order
{
    /// <summary>
    /// Açık tezgâhın başlığı (IMarketCommands'ı kaydeden tezgâh bunu da uygulayabilir). Pazar ekranı açılınca okunur;
    /// boş dönen alan için ekranın kendi varsayılanı kalır. Birden çok tezgâh: "Element Pazarı", "Alet Pazarı".
    /// </summary>
    public interface IMarketInfo
    {
        string MarketTitle { get; }
        string MarketSubtitle { get; }
    }
}
