using System.Text;
using BilgiAdasi.Order;
using UnityEditor;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Ustanın Sorgusu + Keşif Defteri verisini (04-gorev-plani §4, Seviye 1–5) üretir:
    /// Assets/_Project/Data/Resources/LearningData.asset. Metin düzeltmeleri burada yapılıp menü yeniden çalıştırılır.
    /// Görev kimlikleri DenklestirmeSeed ile aynı (denk_1 … denk_5).
    /// </summary>
    public static class LearningDataBuilder
    {
        private const string Folder = "Assets/_Project/Data/Resources";
        private const string Path = Folder + "/" + LearningData.ResourcePath + ".asset";

        [MenuItem("BilgiAdasi/Seviye/Sorgu + Keşif Defteri verisini üret")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project/Data", "Resources");
            var data = AssetDatabase.LoadAssetAtPath<LearningData>(Path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<LearningData>();
                AssetDatabase.CreateAsset(data, Path);
            }

            data.SetEntries(new[]
            {
                Entry("denk_1",
                    "Peki çırak, neden iki su molekülü elde ettin?",
                    new[]
                    {
                        "Bir O₂'de iki oksijen atomu var; her su bir oksijen alıyor, o yüzden iki su oluşuyor.",
                        "Suyun formülü 2H₂O'dur.",
                        "İki hidrojen kavanozu koydum, oksijenin sayısı önemli değil."
                    }, 0,
                    new[]
                    {
                        "Aynen öyle. Atomları saydın, kazan da sana hak verdi.",
                        "Hayır çırak: 2, kaç tane su olduğunu söyler. Suyun formülü yine H₂O'dur; katsayı formülün parçası değildir.",
                        "Tek elemente bakma. Oksijen de sayılır: bir O₂ iki oksijen getirir, ikisi de bir suya gider."
                    },
                    "K-1", "Tepkimede atomların türü ve sayısı korunur; değişen, atomların birbirine bağlanışıdır. Su oluşurken her 2 H₂ için 1 O₂ harcanır."),

                Entry("denk_2",
                    "Neden bir değil iki O₂ koydun?",
                    new[]
                    {
                        "CO₂'deki 2 ve iki suyun içindeki 2 oksijen eder 4; her O₂ iki oksijen getirir.",
                        "Metan çok yanıcı, fazladan hava iyi gelir.",
                        "Karbon iki oksijen istiyor, bir O₂ yeter."
                    }, 0,
                    new[]
                    {
                        "Aynen öyle. Oksijeni iki ürünün içinde ayrı ayrı saydın.",
                        "Sezgi güzel ama sayım yok. Fazla hava iyi gelmez, yalnızca artar. 4 oksijen gerekir, o kadar.",
                        "Suyu unuttun. Karbondioksit 2 oksijen alır, iki su da 2 oksijen daha: toplam 4, yani iki O₂."
                    },
                    "K-2", "Yanma tepkimelerinde oksijenin rolü vardır: yakıt O₂ ile birleşir. Karbon ve hidrojen içeren yakıtlar CO₂ ve H₂O verir. Hava yetmezse is (karbon) ve CO çıkar."),

                // Seviye 3: sorgu yok (bu seviyenin düşünme aşaması Hata Dedektifi, P2).
                Entry("denk_3", null, null, 0, null,
                    "K-3", "Dengesizliği bulmak için her elementi iki kefede ayrı say. Denkleştirirken önce tek yerde geçen elementler (C, H), oksijen en son."),

                Entry("denk_4",
                    "Kireç taşının içindeki karbon nereye gitti?",
                    new[]
                    {
                        "CO₂ olarak havaya karıştı; atomları kaybolmadı, başka bir maddede.",
                        "Isıyla yanıp yok oldu.",
                        "CaO'nun içinde saklı duruyor."
                    }, 0,
                    new[]
                    {
                        "Aynen öyle. Ne girerse o çıkar: karbon taştan havaya geçti.",
                        "Hiçbir atom yok olmaz, çırak. Karbon CO₂'nin içinde havaya karıştı.",
                        "CaO'ya bak: içinde Ca ve O var, karbon yok. Karbon CO₂ ile çıktı."
                    },
                    "K-4", "Bir tepkimenin ürünü başka bir tepkimenin gireni olabilir (ara ürün). Kireç: CaCO₃ → CaO → Ca(OH)₂."),

                Entry("denk_5",
                    "Fazladan N₂ alsaydın neden kazanda kalırdı?",
                    new[]
                    {
                        "H₂ biterdi; N₂ eşleşecek hidrojen bulamaz, tepkime dururdu.",
                        "Kazan dolar, sığmazdı.",
                        "N₂ ağır olduğu için dibe çöker, karışmazdı."
                    }, 0,
                    new[]
                    {
                        "Aynen öyle. Biri biterse tepkime durur, fazla olan artar.",
                        "Hacim değil, sayı meselesi. Hidrojen biterse azot eşsiz kalır.",
                        "Karışmak tepkimeye girmek demek değil. Azot eşleşecek hidrojen bulamadığı için artar."
                    },
                    "K-5", "Tepkime girenleri katsayıları oranında harcar. Biri biterse tepkime durur; fazla olan artar."),
            });

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log("[Öğrenme] Sorgu + Keşif Defteri verisi üretildi: " + Path);
        }

        private static LearningEntry Entry(string taskId, string question, string[] options, int correct, string[] replies,
            string discoveryId, string discovery)
        {
            ReasoningPrompt prompt = question == null
                ? new ReasoningPrompt()
                : new ReasoningPrompt(Rich(question), RichAll(options), correct, RichAll(replies));
            return new LearningEntry(taskId, prompt, discoveryId, Rich(discovery));
        }

        private static string[] RichAll(string[] values)
        {
            var result = new string[values.Length];
            for (int i = 0; i < values.Length; i++) result[i] = Rich(values[i]);
            return result;
        }

        /// <summary>Unicode alt indisleri (₀–₉) TMP &lt;sub&gt; etiketine çevirir: oyun fontunda bu glifler yok.</summary>
        private static string Rich(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new StringBuilder(text.Length + 16);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c < '₀' || c > '₉')
                {
                    sb.Append(c);
                    continue;
                }
                sb.Append("<sub>");
                while (i < text.Length && text[i] >= '₀' && text[i] <= '₉')
                    sb.Append((char)('0' + (text[i++] - '₀')));
                i--;
                sb.Append("</sub>");
            }
            return sb.ToString();
        }
    }
}
