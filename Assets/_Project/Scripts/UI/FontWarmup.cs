using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BilgiAdasi.UI
{
    /// <summary>
    /// Font atlas ön ısıtması. Font asset'leri Dynamic ve "Clear Dynamic Data On Build" açık: build'de atlas boş başlar,
    /// her yeni harf ilk göründüğü karede rasterize edilip atlasa yazılır (anlık takılma). Burada sahne yüklenince
    /// yüklü tüm TMP fontlarına Türkçe karakter seti bir kerede eklenir; sonra oyun sırasında atlas güncellenmez.
    /// Sahneye bir şey koymak gerekmez.
    /// </summary>
    public static class FontWarmup
    {
        // Oyunda görülen tüm karakterler: ASCII, Türkçe, şapkalı harfler, noktalama ve formül işaretleri.
        private const string Charset =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "çÇğĞıİöÖşŞüÜâÂîÎûÛêÊ" +
            "“”‘’«»…–—−•·×÷±°→←↑↓⇌₀₁₂₃₄₅₆₇₈₉⁰¹²³⁴⁵⁶⁷⁸⁹⁺⁻✓✗★☆";

        private static readonly List<TMP_FontAsset> Fonts = new List<TMP_FontAsset>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            WarmLoadedFonts();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => WarmLoadedFonts();

        /// <summary>Bellekteki tüm TMP fontlarına (fallback'ler dahil) karakter setini ekler. Yalnızca sahne yüklenince çalışır.</summary>
        public static void WarmLoadedFonts()
        {
            Fonts.Clear();
            Fonts.AddRange(Resources.FindObjectsOfTypeAll<TMP_FontAsset>());
            for (int i = 0; i < Fonts.Count; i++)
            {
                var font = Fonts[i];
                if (font == null || font.atlasPopulationMode == AtlasPopulationMode.Static) continue;
                font.TryAddCharacters(Charset, out _);
            }
            Fonts.Clear();
        }
    }
}
