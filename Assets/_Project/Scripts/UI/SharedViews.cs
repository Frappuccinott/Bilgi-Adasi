using System.Collections.Generic;

namespace BilgiAdasi.UI
{
    // Sipariş ve masa görevlerinin ortak kullandığı görünüm arayüzleri.

    /// <summary>Karatahta: çözülen denklemler (TMP zengin metin, alt indisli).</summary>
    public interface IChalkboardView
    {
        void ShowEquations(IReadOnlyList<string> richEquations);
    }

    /// <summary>Ustanın defteri: 2 hatadan sonra parlar; açılınca ipucu metni.</summary>
    public interface IHintBookView
    {
        void SetGlowing(bool glowing);
        void ShowHint(string text);
        void HideHint();
        bool IsHintOpen { get; }
    }

    /// <summary>"Gerçek hayatta" kartı. Kod belli bir süre sonra ya da yeni bir etkileşimde kapatır.</summary>
    public interface IRealLifeCardView
    {
        void Show(string text);
        void Hide();
    }

    /// <summary>Kısa bilgi notu (HUD): "Envanter dolu", "Kese yetmiyor", mühür notu… Birkaç saniye görünür.</summary>
    public interface INoticeView
    {
        void ShowNotice(string text);
    }
}
