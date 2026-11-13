using System.Collections.Generic;

namespace BilgiAdasi.Order.Market
{
    /// <summary>
    /// Pazar kuralı. Yeni kural = bu arayüzü uygulayan yeni bir sınıf; ötekilere dokunulmaz.
    /// Kural, listeden satır çıkarabilir ya da satırın alanlarını (fiyat, kalan) değiştirebilir.
    /// </summary>
    public interface IMarketRule
    {
        void Apply(List<MarketOffer> offers, IMarketState state);
    }

    /// <summary>Kuralların okuduğu oyun durumu. Oyunda tezgâh, testte sahte bir sınıf sağlar.</summary>
    public interface IMarketState
    {
        /// <summary>Bu kimlikteki görev bitti mi?</summary>
        bool IsTaskCompleted(string taskId);
        /// <summary>Bu madde, alınmış (sürmekte olan) bir siparişin istenen ürünü mü?</summary>
        bool IsActiveOrderProduct(string key);
        /// <summary>Bu tezgâhta bu maddeden şimdiye dek kaç tane satıldı?</summary>
        int SoldCount(string key);
    }
}
