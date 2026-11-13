namespace BilgiAdasi.Order.Market
{
    /// <summary>
    /// Kuralların üzerinde çalıştığı tek satır (saf C#, Unity'ye bağımlı değil).
    /// Katalog satırından üretilir; kurallar satırı listeden çıkarır ya da alanlarını değiştirir.
    /// </summary>
    public sealed class MarketOffer
    {
        /// <summary>Madde kimliği (formül).</summary>
        public string Key { get; }
        /// <summary>Katalogdaki satırın sırası (tezgâh satırı maddeye geri bağlamak için kullanır).</summary>
        public int SourceIndex { get; }
        public int Price { get; set; }

        public bool UnlimitedStock { get; }
        /// <summary>Katalogdaki başlangıç stoku.</summary>
        public int Stock { get; }
        /// <summary>Kalan adet; sınırsızsa anlamsız. StockRule doldurur.</summary>
        public int Remaining { get; set; }

        /// <summary>Bu görev bitince açılır; boşsa baştan açık.</summary>
        public string AvailableAfterTaskId { get; }
        /// <summary>Bu görev bitince kalkar; boşsa hiç kalkmaz.</summary>
        public string RemovedAfterTaskId { get; }

        public MarketOffer(string key, int sourceIndex, int price, bool unlimitedStock, int stock,
            string availableAfterTaskId = null, string removedAfterTaskId = null)
        {
            Key = key ?? string.Empty;
            SourceIndex = sourceIndex;
            Price = price;
            UnlimitedStock = unlimitedStock;
            Stock = stock;
            Remaining = stock;
            AvailableAfterTaskId = availableAfterTaskId;
            RemovedAfterTaskId = removedAfterTaskId;
        }
    }
}
