using System.Text;
using BilgiAdasi.Order;
using UnityEditor;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Ustanın Sorgusu + Keşif Defteri verisini (04-gorev-plani §4, Seviye 0–10) üretir:
    /// Assets/_Project/Data/Resources/LearningData.asset. Metin düzeltmeleri burada yapılıp menü yeniden çalıştırılır.
    /// Görev kimlikleri DenklestirmeSeed (denk_1 … denk_5) ve DeskDataBuilder (tur_1 … tur_5) ile aynı.
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
                // ---------- Çıraklık (Seviye 0) ----------
                Entry("cirak_0",
                    "Terazinin iki kefesi neden eşit kaldı?",
                    new[]
                    {
                        "Atomlar yok olmaz, yoktan da var olmaz.",
                        "Kazan kapalı olduğu için hava girmedi.",
                        "Hidrojen hafif olduğu için tartılmaz."
                    }, 0,
                    new[]
                    {
                        "Aynen öyle. Ne girdiyse o çıktı; deftere ilk kuralı yaz.",
                        "Kapak tek başına açıklamaz. Isıtsan da açık bıraksan da bu kazanda atom ne kaybolur ne türer.",
                        "Hafif de olsa her atom sayılır. Dört H girdi, dört H çıktı."
                    },
                    "K-0", "Kazana ne girerse o çıkar: tepkimede atomlar kaybolmaz, yoktan var olmaz."),

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

                // ---------- Bereket Tarlası: Tohum Nefes Alır (Sv2, Matematik + Biyoloji; 15-bereket §3.2) ----------
                Entry("bereket_2",
                    "150 tohum ektin. Tarlada kesin 120 filiz mi çıkar?",
                    new[]
                    {
                        "Hayır; 120 beklenen sayıdır, biraz az ya da çok çıkabilir.",
                        "Evet, hesap doğruysa tam 120 çıkar.",
                        "Hayır; olasılık 0,8 olduğu için hep 80 çıkar."
                    }, 0,
                    new[]
                    {
                        "Aynen öyle. Beklenen sayı ortalamadır: 150 tohumdan 114 de çıkabilir, 126 da. Çok kez ekersen ortalaması 120'ye yaklaşır.",
                        "Olasılık kesinlik vermez, çırak. 120 beklediğimiz ortalamadır; bu yıl biraz az ya da çok çıkabilir.",
                        "0,8 bir oran: her 10 tohumdan yaklaşık 8'i. 150 tohumda bu 120 eder, 80 değil."
                    },
                    "K-12", "Beklenen sayı = olasılık × deneme; tersinden tohum = filiz ÷ olasılık. Çimlenen tohum besinini oksijenle kullanır (oksijenli solunum), bu yüzden toprak havalı olmalı."),

                // ---------- Tepkime Türleri (Seviye 6–10, masa) ----------
                Entry("tur_1", null, null, 0, null,
                    "K-6", "Sentez: birden çok madde → tek ürün. Analiz: tek madde → daha basit maddeler. Yanma: madde + O₂ → ısı ve ışık. (Kanıtlarım: çeşme, kireç taşı, fener.)"),

                Entry("tur_2", null, null, 0, null,
                    "K-7", "Türü anlamak için: tek giren varsa analiz, tek ürün varsa sentez, girenlerde O₂ var ve ısı-ışık çıkıyorsa yanma."),

                Entry("tur_3",
                    "Kazandan çıkan su buharı neden kimyasal değişimin kanıtı değil?",
                    new[]
                    {
                        "Su hâlâ su; yeni madde oluşmadı, yalnızca hâli değişti.",
                        "Çünkü buhar görünmez.",
                        "Çünkü ısı yok."
                    }, 0,
                    new[]
                    {
                        "Aynen öyle. Hâl değişimi fizikseldir; kazandan yeni bir madde çıkmadı.",
                        "Görünmek ölçüt değil, çırak. Buhar da sudur; yeni madde oluşmadı.",
                        "Buharlaşma ısı ister aslında. Ölçüt ısı değil, yeni madde oluşup oluşmadığı."
                    },
                    "K-8", "Kimyasal değişimin kanıtları: yeni madde, gaz çıkışı, renk değişimi, ısı-ışık, koku. Yalnız şekil ya da hâl değişimi fizikseldir."),

                Entry("tur_4", null, null, 0, null,
                    "K-9", "Bir iddiayı çürütmek için tek karşı örnek yeter; düzeltirken kanıt göster."),

                Entry("tur_5",
                    "Fotosentezle solunum arasında nasıl bir ilişki gördün?",
                    new[]
                    {
                        "Biri diğerinin tersi gibi: biri CO₂ ve suyu glikoza bağlar, öteki glikozu CO₂ ve suya geri çevirir.",
                        "İkisi aynı tepkimedir.",
                        "Fotosentez yalnız gece olur."
                    }, 0,
                    new[]
                    {
                        "Aynen öyle. Aynı atomlar bir yönde bağlanıyor, öbür yönde çözülüyor.",
                        "Girenleri ve ürünleri karşılaştır: biri ötekinin tersi, aynısı değil.",
                        "Fotosentez ışık ister, gündüz olur. Solunum ise gece gündüz sürer."
                    },
                    "K-10", "Tür, atomların nasıl yeniden dizildiğini anlatır. Aynı atomlar (C, H, O) doğada döngü hâlinde dolaşır: yakıtta, havada, bitkide, bizde."),
            });

            // Defter sayfası alanları (görev adı, yer, denklem, saha notları) ve şeritler: LessonNotesData.
            foreach (var e in data.Entries) LessonNotesData.ApplyPage(e);
            LessonNotesData.ApplyNotebookTexts(data.NotebookTexts);

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log("[Öğrenme] Sorgu + Keşif Defteri verisi üretildi: " + Path);
        }

        /// <summary>
        /// Yalnızca masa görevlerinin (tur_1–5) Ders Notu'nu yazar; asset'teki öteki görevlerin Inspector düzeltmelerine
        /// ve sorgu/defter alanlarına dokunmaz. Tam üretim yukarıdaki menüdür.
        /// </summary>
        [MenuItem("BilgiAdasi/Seviye/Masa görevlerinin ders notlarını yaz (tur_1–5)")]
        public static void WriteDeskLessons()
        {
            var data = AssetDatabase.LoadAssetAtPath<LearningData>(Path);
            if (data == null)
            {
                Debug.LogWarning("[Öğrenme] LearningData yok; önce tam üretimi çalıştırın: " + Path);
                return;
            }
            int written = 0;
            foreach (var id in new[] { "tur_1", "tur_2", "tur_3", "tur_4", "tur_5" })
            {
                var entry = data.Find(id);
                if (entry == null)
                {
                    Debug.LogWarning("[Öğrenme] LearningData'da kayıt yok: " + id);
                    continue;
                }
                entry.WithLesson(LessonNotesData.For(id));
                written++;
            }
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log("[Öğrenme] Masa ders notları yazıldı: " + written + " görev.");
        }

        private static LearningEntry Entry(string taskId, string question, string[] options, int correct, string[] replies,
            string discoveryId, string discovery)
        {
            ReasoningPrompt prompt = question == null
                ? new ReasoningPrompt()
                : new ReasoningPrompt(Rich(question), RichAll(options), correct, RichAll(replies));
            // Ders Notu (görevden önce): LessonNotesData; notu olmayan görevde boş kalır.
            return new LearningEntry(taskId, prompt, discoveryId, Rich(discovery), LessonNotesData.For(taskId))
                .WithTopic(TopicFor(taskId));
        }

        /// <summary>
        /// Öğrenilen konu (müfredat dili): görev sonu bildiriminde "Konu: …" ve başarım kartının en altında görünür.
        /// Öğretmen/veli özetinin temeli; 10. sınıf Kimya "Kimyasal Tepkimeler" ünitesi.
        /// </summary>
        internal static string TopicFor(string taskId)
        {
            switch (taskId)
            {
                case "cirak_0": return "Kütlenin korunumu";
                case "denk_1": return "Kütlenin korunumu ve denklem denkleştirme";
                case "denk_2": return "Yanma tepkimeleri";
                case "denk_3": return "Denklem denkleştirme yöntemi";
                case "denk_4": return "Ardışık tepkimeler ve ara ürün";
                case "denk_5": return "Mol oranı ve sınırlayıcı bileşen";
                case "tur_1": return "Tepkime türleri: sentez, analiz, yanma";
                case "tur_2": return "Tepkime türünü denklemden tanıma";
                case "tur_3": return "Fiziksel ve kimyasal değişim";
                case "tur_4": return "Tepkime türlerinde kanıt ve karşı örnek";
                case "tur_5": return "Tepkime türleri ve maddenin döngüsü";
                case "bereket_2": return "Olasılık (beklenen sayı) ve oksijenli solunum";
                default: return string.Empty;
            }
        }

        internal static string[] RichAll(string[] values)
        {
            var result = new string[values.Length];
            for (int i = 0; i < values.Length; i++) result[i] = Rich(values[i]);
            return result;
        }

        /// <summary>Unicode alt indisleri (₀–₉) TMP &lt;sub&gt; etiketine çevirir: oyun fontunda bu glifler yok.</summary>
        internal static string Rich(string text)
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
