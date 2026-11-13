using TMPro;
using UnityEngine;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Arayüz teması — Figma "Bilgi Adası — Marka Kimliği" dosyasından (Renk + Tipografi + Arayüz dili sayfaları).
    /// Tüm Figma view'ları renk, font ve sprite'ı buradan okur; değer değişince kod değişmez.
    /// Palet 09-tema-ve-model-kurallari §3'ten türetilmiştir: saf siyah/beyaz yok, gölgeler kahve/maviye kayar.
    /// En küçük yazı 28 px (1920×1080 referans).
    /// </summary>
    [CreateAssetMenu(menuName = "BilgiAdasi/UI/Tema", fileName = "UiTheme")]
    public sealed class UiTheme : ScriptableObject
    {
        [Header("Kâğıt (UI)")]
        [SerializeField] private Color paper = Hex(0xE2DBB1);
        [SerializeField] private Color paperMid = Hex(0xD6CCB2);
        [SerializeField] private Color paperShade = Hex(0x9E8B7E);

        [Header("Mürekkep (UI)")]
        [SerializeField] private Color ink = Hex(0x5A4437);
        [SerializeField] private Color inkDark = Hex(0x4A3427);
        [SerializeField] private Color inkDeep = Hex(0x3E281B);

        [Header("Bakır (defter başlıkları, numaralar)")]
        [SerializeField] private Color copper = Hex(0x9F4E2B);

        [Header("Ahşap çerçeve")]
        [SerializeField] private Color wood = Hex(0x8A5A3A);
        [SerializeField] private Color woodDark = Hex(0x5C3A26);

        [Header("Mühür mumu")]
        [SerializeField] private Color wax = Hex(0xC4532A);
        [SerializeField] private Color waxMid = Hex(0xA2411E);
        [SerializeField] private Color waxDark = Hex(0x7A2F15);

        [Header("Altın vurgu (yalnızca bakılacak yerde)")]
        [SerializeField] private Color goldLight = Hex(0xFFD23F);
        [SerializeField] private Color gold = Hex(0xF7A800);
        [SerializeField] private Color goldDark = Hex(0xD07A00);

        [Header("Su (meydan okuma)")]
        [SerializeField] private Color water = Hex(0x35BDEB);
        [SerializeField] private Color waterDark = Hex(0x1E86C4);

        [Header("Oyun görüntüsü üstü (scrim)")]
        [Tooltip("Ekran karartması: mürekkep koyusu, saydam.")]
        [SerializeField] private Color scrim = new Color(0.243f, 0.157f, 0.106f, 0.62f);

        [Header("Fontlar (Alegreya + Alegreya Sans, SIL OFL 1.1). Boş = TMP varsayılanı")]
        [Tooltip("Başlık / usta konuşması: Alegreya.")]
        [SerializeField] private TMP_FontAsset headingFont;
        [Tooltip("Gövde / etiket / formül: Alegreya Sans.")]
        [SerializeField] private TMP_FontAsset bodyFont;

        [Header("Sprite'lar (Figma'dan dışa aktarılmış)")]
        [Tooltip("UI/Mühür — tam mühür (çeşme simgeli).")]
        [SerializeField] private Sprite seal;
        [Tooltip("UI/Mühür'ün yalnızca mum gövdesi; üstüne başarım simgesi biner.")]
        [SerializeField] private Sprite sealWax;
        [Tooltip("UI/Akçe — gümüş sikke, ortada terazi (kese göstergesi).")]
        [SerializeField] private Sprite coin;
        [Tooltip("Envanter ikonları (Mustafa: UI/Ikonlar/Malzeme_*.png). Ad eşleşmesi: Malzeme_<formül, parantezsiz>.")]
        [SerializeField] private Sprite[] speciesIcons;

        public Color Paper => paper;
        public Color PaperMid => paperMid;
        public Color PaperShade => paperShade;
        public Color Ink => ink;
        public Color InkDark => inkDark;
        public Color InkDeep => inkDeep;
        public Color Copper => copper;
        public Color Wood => wood;
        public Color WoodDark => woodDark;
        public Color Wax => wax;
        public Color WaxMid => waxMid;
        public Color WaxDark => waxDark;
        public Color GoldLight => goldLight;
        public Color Gold => gold;
        public Color GoldDark => goldDark;
        public Color Water => water;
        public Color WaterDark => waterDark;
        public Color Scrim => scrim;
        public TMP_FontAsset HeadingFont => headingFont;
        public TMP_FontAsset BodyFont => bodyFont;
        public Sprite Seal => seal;
        public Sprite SealWax => sealWax;
        public Sprite Coin => coin;

        private System.Collections.Generic.Dictionary<string, Sprite> iconMap;

        /// <summary>"Ca(OH)2" → Malzeme_CaOH2. Yoksa null (view kavanoz rengiyle disk çizer).</summary>
        public Sprite IconFor(string formula)
        {
            if (string.IsNullOrEmpty(formula)) return null;
            return IconNamed("Malzeme_" + formula.Replace("(", string.Empty).Replace(")", string.Empty));
        }

        /// <summary>Maddenin kendi simge adı varsa (aletler: Alet_Havan) onu, yoksa Malzeme_&lt;formül&gt;'ü döner.</summary>
        public Sprite IconFor(Chemistry.SpeciesData species)
        {
            if (species == null) return null;
            return !string.IsNullOrEmpty(species.IconName) ? IconNamed(species.IconName) : IconFor(species.Formula);
        }

        private Sprite IconNamed(string iconName)
        {
            if (string.IsNullOrEmpty(iconName) || speciesIcons == null) return null;
            if (iconMap == null)
            {
                iconMap = new System.Collections.Generic.Dictionary<string, Sprite>();
                foreach (var s in speciesIcons)
                    if (s != null) iconMap[s.name] = s;
            }
            return iconMap.TryGetValue(iconName, out var icon) ? icon : null;
        }

        private static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        private static UiTheme fallback;

        /// <summary>Inspector'da tema atanmadıysa: Resources'taki "UiTheme" ya da varsayılan değerlerle geçici tema.</summary>
        public static UiTheme OrDefault(UiTheme theme)
        {
            if (theme != null) return theme;
            if (fallback == null)
            {
                fallback = Resources.Load<UiTheme>("UiTheme");
                if (fallback == null) fallback = CreateInstance<UiTheme>();
            }
            return fallback;
        }
    }
}
