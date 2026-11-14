using System.Collections.Generic;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Duraklatma menüsündeki "Şu anki görev" kartının verisi (Figma: Ekran/Duraklatma → Görev parşömeni).
    /// Metinler görev/köy verisinden gelir; kartın etiketleri ("İstenen" vb.) view'dadır.
    /// </summary>
    public sealed class CurrentTaskInfo
    {
        public readonly struct Step
        {
            /// <summary>TMP zengin metin (alt indisli formül).</summary>
            public readonly string Text;
            public readonly bool Done;

            public Step(string text, bool done)
            {
                Text = text;
                Done = done;
            }
        }

        /// <summary>Görevi veren (başlık).</summary>
        public string Title;
        /// <summary>Verenin ilk cümlesi (italik alıntı). Boş olabilir.</summary>
        public string Quote;
        /// <summary>İstenen ürün(ler), zengin metin: "2 H₂O".</summary>
        public string WantedRich;
        public readonly List<Step> Steps = new List<Step>();
        /// <summary>Ürün hazır, kapıda teslim bekliyor.</summary>
        public bool ReadyToDeliver;
        /// <summary>Görevin teslim adımı var mı (tarla görevinde yok: sandığa dökülür).</summary>
        public bool HasDelivery = true;
        /// <summary>Köyün adı.</summary>
        public string Place;
        /// <summary>Köyde biten görev sayısı ve toplam.</summary>
        public int Done, Total;
    }

    /// <summary>Şu anki görevi verir (yoksa null). NeighborhoodRunner uygular, ViewLocator'a kaydolur.</summary>
    public interface ICurrentTaskSource
    {
        CurrentTaskInfo GetCurrentTask();
    }
}
