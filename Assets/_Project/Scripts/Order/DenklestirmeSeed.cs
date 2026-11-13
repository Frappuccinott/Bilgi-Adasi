#if UNITY_EDITOR
using System;
using BilgiAdasi.Chemistry;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// 05-icerik-prototip.md §2 (Denkleştirme, 5 sipariş) kod karşılığı. YALNIZCA editör veri üreticisi ve testler
    /// kullanır; oyun SO asset'lerini okur. Build'e girmez. İçerik değişirse önce 05 güncellenir.
    /// </summary>
    public static class DenklestirmeSeed
    {
        public static OrderTask[] Build(Func<string, SpeciesData> sp, Func<string, ReactionData> rx)
        {
            return new[]
            {
                OrderTask.Create("denk_1", "Muhtar Yusuf",
                    new[] { "Çeşme kurudu evladım! Çocuklar dereye iniyor, dere de kurudu. Sen de bilirsin değil mi?" },
                    new[] { "Aktı! Gerçekten aktı!" },
                    "Sudaki oksijenleri say. Bir su kaç oksijen ister?",
                    "Hidrojen ile oksijen gerçekte çok hızlı ve gürültülü tepkimeye girer; yalnızca laboratuvarda, uzman gözetiminde yapılır.",
                    OrderMode.Exact,
                    new[] { new OrderStep(rx("R_water"), new[] { A(sp("H2O"), 2) }, true) },
                    new[] { sp("H2"), sp("O2") }),

                OrderTask.Create("denk_2", "Fenerci Demir",
                    new[] { "Fenerler is yapıyor. Akşam çarşıda kimse kimseyi görmüyor. Gaz bende, hava sende. Ne kadar lazımsa o kadar ver." },
                    new[] { "Hah. Şimdi oldu. Sağ ol çırak." },
                    "Sağ terazideki oksijenleri say: karbondioksitte kaç, suda kaç?",
                    "Yakıt yeterli havayla yanmazsa is ve zehirli karbon monoksit çıkar; soba ve şofben olan odalar bu yüzden havalandırılır.",
                    OrderMode.Exact,
                    new[] { new OrderStep(rx("R_methane"), new[] { A(sp("CO2"), 1), A(sp("H2O"), 2) }, true) },
                    new[] { sp("CH4"), sp("O2") }),

                OrderTask.Create("denk_3", "Fırıncı Zehra",
                    new[] { "Fenercinin fenerlerini yakmışsın, duydum. Benim fırın onun fenerinden büyük ama. Kasaba iki gündür peksimet yiyor." },
                    new[] { "İşte bu koku! Akşama ilk somun senin." },
                    "Oksijeni sona bırak. Önce karbonu, sonra hidrojeni eşitle.",
                    "Mutfaktaki tüp gazı da tam yanınca mavi alev verir; sarı ve isli alev hava yetmediğini gösterir.",
                    OrderMode.Exact,
                    new[] { new OrderStep(rx("R_propane"), new[] { A(sp("CO2"), 3), A(sp("H2O"), 4) }, false) },
                    new[] { sp("C3H8"), sp("O2") }),

                OrderTask.Create("denk_4", "Duvarcı Halil",
                    new[] { "Harç için sönmüş kireç lazım. Kireç taşı var ama. Ustan taştan kireç çıkarırdı, iki adımda. Acele etme." },
                    new[] { "Tam kıvamında. Çırağı ustasını aratmıyor." },
                    "Her denklem katsayı istemez. Saymadan kavanoz ekleme.",
                    "Kireç söndürülürken çok ısı çıkar ve kireç cilde, göze zarar verir; ustalar eldiven ve gözlük takar.",
                    OrderMode.Exact,
                    new[]
                    {
                        new OrderStep(rx("R_lime1"), new[] { A(sp("CaO"), 1), A(sp("CO2"), 1) }, false),
                        new OrderStep(rx("R_lime2"), new[] { A(sp("Ca(OH)2"), 1) }, false),
                    },
                    new[] { sp("CaCO3"), sp("H2O") }),

                OrderTask.Create("denk_5", "Çiftçi Elif",
                    new[] { "Dört kavanoz gübre ruhu istiyorum, ne eksik ne fazla. Parasını peşin veriyorum. Artanın hesabı senin cebinden." },
                    new[] { "Dört kavanoz, tastamam. Hesabını bilen simyacı!" },
                    "Önce bir kavanozluk denklemi kur, sonra ikiye katla.",
                    "Amonyak en çok üretilen gübre hammaddelerindendir; keskin kokulu, zararlı bir gazdır ve kapalı fabrikalarda üretilir.",
                    OrderMode.Purchase,
                    new[] { new OrderStep(rx("R_ammonia"), new[] { A(sp("NH3"), 4) }, false) },
                    Array.Empty<SpeciesData>(),
                    budget: 20,
                    prices: new[] { new SpeciesPrice(sp("N2"), 3), new SpeciesPrice(sp("H2"), 2) }),
            };
        }

        private static SpeciesAmount A(SpeciesData s, int n) => new SpeciesAmount(s, n);
    }
}
#endif
