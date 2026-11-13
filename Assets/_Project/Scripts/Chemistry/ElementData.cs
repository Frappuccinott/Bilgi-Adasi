using UnityEngine;

namespace BilgiAdasi.Chemistry
{
    /// <summary>Bir element (H, O, C, N, Ca). Atom küresi rengi ve harfi buradan gelir.</summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Kimya/Element", fileName = "Element_")]
    public sealed class ElementData : ScriptableObject
    {
        [SerializeField] private string symbol = "H";
        [SerializeField] private string displayName = "Hidrojen";
        [SerializeField] private Color color = Color.white;

        public string Symbol => symbol;
        public string DisplayName => displayName;
        public Color Color => color;

        /// <summary>Testler ve editör veri üretici için çalışma zamanında örnek oluşturur.</summary>
        public static ElementData Create(string symbol, string displayName, Color color)
        {
            var e = CreateInstance<ElementData>();
            e.symbol = symbol;
            e.displayName = displayName;
            e.color = color;
            e.name = "Element_" + symbol;
            return e;
        }
    }
}
