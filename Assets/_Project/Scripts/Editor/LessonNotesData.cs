using BilgiAdasi.Core;
using BilgiAdasi.Order;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Ders Notu içeriği (13-ders-notu-plani, araştırma: 04_Icerik/12-ders-notu-arastirma.md). Her görev iki açılış:
    /// 1) sol: konu, sağ: yöntem + kavram yanılgısı; 2) sol: çözümlü örnek (görevdekinden farklı tepkime), sağ: "bu görevde".
    /// "Bu görevde" sayfası cevabı (katsayıları) vermez; yalnızca ne yapılacağını söyler.
    /// Unicode alt indisler (₂) LearningDataBuilder.Rich ile TMP &lt;sub&gt;'a çevrilir.
    /// </summary>
    internal static class LessonNotesData
    {
        public static LessonNote For(string taskId)
        {
            switch (taskId)
            {
                case "denk_1": return Denk1();
                case "denk_2": return Denk2();
                case "denk_3": return Denk3();
                case "denk_4": return Denk4();
                case "denk_5": return Denk5();
                case "bereket_2": return Bereket2();
                default: return new LessonNote();
            }
        }

        // ---------- D-1 Kuru Çeşme: kütlenin korunumu, katsayı ↔ alt indis ----------

        private static LessonNote Denk1() => new LessonNote("Atomlar kaybolmaz",
            Page(
                K("Ders notu", "Denkleştirme Köyü, 1. görev"),
                T("Atomlar kaybolmaz"),
                B("Kimyasal tepkimede atomlar yok olmaz, yoktan da var olmaz. Yalnızca aralarındaki bağlar kopar ve yeni bağlar kurulur. Bu yüzden tepkimeden önce ve sonra her elementin atom sayısı aynıdır."),
                R(),
                S("Kazanın terazisi de bunu ölçer"),
                C(("H", 4, 4), ("O", 2, 2)),
                E("2H₂ + O₂ → 2H₂O"),
                F("Lavoisier, 1789: kütlenin korunumu kanunu. Câbir’e atfedilen yazılar bu işe terazi ilmi der.")),
            Page(
                T("Nasıl denkleştirilir?"),
                St("Girenleri (tepkenleri) sola, ürünleri sağa yaz.",
                    "Her elementin atomlarını iki tarafta ayrı ayrı say.",
                    "Eksik kalan tarafta maddenin önüne katsayı yaz.",
                    "Bütün elementler eşitlenince dur. Katsayılar en küçük tam sayılar olsun."),
                R(),
                W("Dikkat", "2H₂O’daki büyük 2 katsayıdır, kaç su molekülü olduğunu söyler. Küçük 2 alt indistir ve maddenin kendisidir. Alt indisi değiştirirsen madde değişir: H₂O₂ su değildir.")),
            Page(
                K("Çözümlü örnek"),
                T("H₂ + Cl₂ → HCl"),
                C(("H", 2, 1), ("Cl", 2, 1)),
                B("Sağda hem H hem Cl birer atom eksik. Alt indise dokunmadan HCl’nin önüne 2 yaz:"),
                T("H₂ + Cl₂ → 2HCl"),
                C(("H", 2, 2), ("Cl", 2, 2))),
            Page(
                K("Bu görevde", "Muhtar Yusuf, meydan çeşmesi"),
                T("Çeşmeye su"),
                B("Sipariş kartı neyin girip neyin çıkacağını söyler. Kaç kavanoz gerektiğini sen bulacaksın."),
                St("Pazardan ya da raftan H₂ ve O₂ kavanozlarını al.",
                    "Kazana birer birer dök. Her döküşte teraziye bak: H ve O iki kefede ayrı ayrı eşit mi?",
                    "Terazi dengeye gelince ocağı yak."),
                R(),
                N("Unutma", "Dökülen kavanoz geri alınmaz. Dökmeden önce say.")));

        // ---------- D-2 İsli Fenerler: yanma, oksijen en son ----------

        private static LessonNote Denk2() => new LessonNote("Yanma",
            Page(
                K("Ders notu", "Denkleştirme Köyü, 2. görev"),
                T("Yanma"),
                B("Yanma, bir maddenin oksijenle tepkimeye girip ısı ve ışık vermesidir. Karbon ve hidrojenden oluşan yakıtlar yeterli oksijenle yanınca karbondioksit ve su verir."),
                E("yakıt + O₂ → CO₂ + H₂O"),
                R(),
                S("Hava yetmezse"),
                B("Yanma eksik kalır: alev sarılaşır, is (karbon) ve karbon monoksit çıkar. Fenerlerin islenmesinin sebebi budur."),
                F("Evlere gelen doğal gazın büyük kısmı metandır; ocakta mavi alevle yanar.")),
            Page(
                T("Yakıt nasıl denkleştirilir?"),
                St("Önce karbonu eşitle: CO₂’nin önüne yakıttaki C sayısını yaz.",
                    "Sonra hidrojeni eşitle: her H₂O iki H alır.",
                    "En son oksijeni say: sağdaki bütün O’ları topla, O₂ ile karşıla.",
                    "Oksijen en sona kalır, çünkü O₂ başka bir elementi bozmaz."),
                R(),
                W("Dikkat", "Oksijeni yalnızca CO₂’de sayma. Sudaki oksijen de sağ tarafa dahildir.")),
            Page(
                K("Çözümlü örnek"),
                T("C₂H₄ + O₂ → CO₂ + H₂O"),
                St("C: solda 2 var. Sağa 2CO₂ yaz.",
                    "H: solda 4 var. Sağa 2H₂O yaz.",
                    "O: sağda 2·2 + 2 = 6 oldu. Sola 3O₂ yaz."),
                E("C₂H₄ + 3O₂ → 2CO₂ + 2H₂O"),
                C(("C", 2, 2), ("H", 4, 4), ("O", 6, 6))),
            Page(
                K("Bu görevde", "Fenerci Demir, çarşı fenerleri"),
                T("Fenerlere temiz alev"),
                B("Fenercinin gazı metandır: CH₄. Oksijeni ne eksik ne fazla vereceksin."),
                St("Raftan ya da pazardan CH₄ ve O₂ al.",
                    "Önce C ve H’yi iki kefede eşitle.",
                    "Sonra O₂’yi teker teker dök, O sayısını izle.",
                    "Terazi dengeye gelince ocağı yak."),
                R(),
                N("Unutma", "Fazla O₂ de dengeyi bozar; denge yoksa kazan köpürür.")));

        // ---------- D-3 Soğuk Fırın: büyük molekülde sayma sırası ----------

        private static LessonNote Denk3() => new LessonNote("Sırayla say",
            Page(
                K("Ders notu", "Denkleştirme Köyü, 3. görev"),
                T("Sırayla say"),
                B("Molekül büyüdükçe atom sayısı artar ama yöntem değişmez. Karışmasın diye hep aynı sırayı izle: önce karbon, sonra hidrojen, en son oksijen."),
                R(),
                S("Neden oksijen en sonda?"),
                B("Oksijen hem CO₂’de hem H₂O’da var. Karbon ve hidrojen yerine oturmadan oksijeni sayarsan her değişiklikte baştan saymak zorunda kalırsın. O₂ yalnız oksijen taşır; en son onunla tamamlamak kolaydır."),
                F("Tüp gaz çoğunlukla propan ve bütandır. Tam yanınca alevi mavi olur.")),
            Page(
                T("Katsayı bütün molekülü çarpar"),
                St("Katsayı ile alt indisi çarp: 3CO₂ içinde 3 C ve 6 O var.",
                    "4H₂O içinde 8 H ve 4 O var.",
                    "Oksijen sayısı tek çıkarsa bütün katsayıları 2 ile çarpıp yeniden say."),
                R(),
                W("Dikkat", "Katsayıyı yalnızca ilk atoma uygulama. 3CO₂, 3 C ve 6 O demektir; 3 C ve 2 O değil.")),
            Page(
                K("Çözümlü örnek"),
                T("C₂H₆ + O₂ → CO₂ + H₂O"),
                St("C: 2 karbon için sağa 2CO₂.",
                    "H: 6 hidrojen için sağa 3H₂O.",
                    "O: sağda 4 + 3 = 7 oksijen, tek sayı. Hepsini 2 ile çarp."),
                E("2C₂H₆ + 7O₂ → 4CO₂ + 6H₂O"),
                C(("C", 4, 4), ("H", 12, 12), ("O", 14, 14))),
            Page(
                K("Bu görevde", "Fırıncı Zehra, kasaba fırını"),
                T("Fırını yeniden yak"),
                B("Fırının yakıtı propandır: C₃H₈. Bu kez sipariş kartında yalnızca ürünler yazıyor; girenleri sen kuracaksın."),
                St("Sağ kefeye bak: C, H ve O’dan kaçar tane var?",
                    "Önce C₃H₈ dök. C ve H iki kefede eşit mi?",
                    "En son O₂’yi teker teker ekle, oksijeni iki kefede topla.",
                    "Terazi dengeye gelince ocağı yak."),
                R(),
                N("Unutma", "Katsayı kavanoz sayısıdır: 3O₂ üç kavanoz, ama altı oksijen atomu.")));

        // ---------- D-4 Yıkık Köprü: analiz, sentez, ara ürün ----------

        private static LessonNote Denk4() => new LessonNote("Ara ürün",
            Page(
                K("Ders notu", "Denkleştirme Köyü, 4. görev"),
                T("Ara ürün"),
                B("Bazı ürünler tek tepkimede elde edilmez. Önce bir ara ürün yapılır, sonra o ara ürün yeni bir tepkimeye girer."),
                R(),
                S("Analiz ve sentez"),
                B("Analizde (ayrışma) tek bir madde daha basit maddelere ayrılır. Sentezde (oluşum) birden çok madde birleşip tek bir ürün verir."),
                F("Kireçli harç duvarda havadaki CO₂’yi yavaş yavaş geri alır ve taşa döner; bu yüzden sertleşir.")),
            Page(
                T("Her denklem katsayı istemez"),
                St("Denklemi yaz ve hemen say: belki zaten denktir.",
                    "Denkse bütün katsayılar 1’dir; 1 yazılmaz.",
                    "Birinci adımın ürünü, ikinci adımın gireni olur. Onu kaybetme."),
                R(),
                W("Dikkat", "Gaz çıkınca kütle kaybolmaz. Kireç taşı ısıtılınca hafifler, çünkü CO₂ havaya karışır; atomları başka bir maddededir.")),
            Page(
                K("Çözümlü örnek"),
                S("1. adım, sentez"),
                E("C + O₂ → CO₂"),
                S("2. adım, CO₂ ara ürün"),
                E("CO₂ + H₂O → H₂CO₃"),
                C(("C", 1, 1), ("H", 2, 2), ("O", 3, 3)),
                B("İki denklem de zaten denk: katsayı yok. Gazozdaki karbonik asit böyle oluşur.")),
            Page(
                K("Bu görevde", "Duvarcı Halil, yıkık köprü"),
                T("Taştan harca"),
                B("Halil sönmüş kireç istiyor: Ca(OH)₂. Elinde kireç taşı (CaCO₃) ve su var. İş iki adımda biter."),
                St("Birinci adım: kireç taşını ayrıştır. Taş, sönmemiş kirece (CaO) ve CO₂’ye ayrılır.",
                    "İkinci adım: ara ürün CaO’yu suyla birleştir.",
                    "Her adımda önce say, sonra dök."),
                R(),
                N("Unutma", "Saymadan kavanoz ekleme. Fazla madde de dengeyi bozar.")));

        // ---------- D-5 Gübre Ruhu: oran, katlama, artan madde ----------

        private static LessonNote Denk5() => new LessonNote("Oranla hesap",
            Page(
                K("Ders notu", "Denkleştirme Köyü, 5. görev"),
                T("Oranla hesap"),
                B("Denklemdeki katsayılar bir tarif gibidir: maddelerin hangi oranda harcandığını söyler."),
                E("2H₂ + O₂ → 2H₂O"),
                B("Her 2 H₂ için 1 O₂ harcanır ve 2 H₂O çıkar. Oran 2 : 1 : 2."),
                R(),
                S("Biri biterse"),
                B("Tepkime, girenlerden biri bitince durur. Önce biten maddeye sınırlayıcı bileşen denir. Öteki madde fazla kalır; buna artan madde denir."),
                F("Havadaki azottan amonyak yapmak, bugün gübrenin temel kaynağıdır.")),
            Page(
                T("İstenen kadar üret"),
                St("Önce denklemi en küçük katsayılarla denkleştir.",
                    "İstenen ürün kaç katıysa bütün katsayıları o sayıyla çarp.",
                    "Tam o kadar al. Artan madde boşa gider, parası da gider."),
                R(),
                W("Dikkat", "Katlarken yalnızca ürünü çarpma. Bütün katsayılar aynı sayıyla çarpılır, yoksa denge bozulur.")),
            Page(
                K("Çözümlü örnek"),
                T("6 su gerekiyor"),
                B("2H₂ + O₂ → 2H₂O iki su verir. Altı su için bütün katsayıları 3 ile çarp:"),
                E("6H₂ + 3O₂ → 6H₂O"),
                C(("H", 12, 12), ("O", 6, 6)),
                S("Artan madde"),
                B("Kazana 7 H₂ ve 3 O₂ koysaydın 3 O₂ yalnızca 6 H₂ harcardı; 1 H₂ artardı. Burada O₂ sınırlayıcıdır.")),
            Page(
                K("Bu görevde", "Çiftçi Elif, tarla"),
                T("Dört kavanoz gübre ruhu"),
                B("Gübre ruhu amonyaktır: NH₃. Elif parayı peşin verdi; pazarda N₂ ve H₂’nin fiyatına bak."),
                St("Bir kavanoz NH₃ için denklemi kur: N₂ ve H₂’den NH₃.",
                    "Dört kavanoza göre katsayıları katla.",
                    "Pazardan tam o kadar al, kazana dök.",
                    "Terazi dengeye gelince ocağı yak."),
                R(),
                N("Unutma", "İki giren her zaman birlikte tükenmez. Fazla aldığın kavanoz kazanda artar, boşa gider; kesen de boşalır.")));

        // ---------- Sv2 Tohum Nefes Alır (Bereket Tarlası): Biyoloji + Matematik (15-bereket §3.2) ----------
        // TYMM: BİY.10.1.7 hücresel solunum, BİY.10.1.8 oksijenli solunum; Matematik Tema 7 Veriden Olasılığa (deneysel olasılık).
        // "Bu görevde" sayfası cevabı (150 tohum / 15 kese) vermez.

        private static LessonNote Bereket2() => new LessonNote("Tohum nefes alır",
            Page(
                K("Ders notu · Biyoloji + Matematik", "Bereket Tarlası, Hayat Ustası"),
                T("Tohum nefes alır"),
                B("Tohumun içinde depolanmış besin vardır. Tohum çimlenirken bu besini oksijenli solunumla kullanır ve büyümek için gereken enerjiyi elde eder."),
                E("besin (glikoz) + O₂ → CO₂ + H₂O + enerji"),
                R(),
                S("Toprağı neden süreriz?"),
                B("Sürülmüş toprak gevşektir; arasında hava (oksijen) ve su tutulur. Sıkışmış toprakta tohum yeterince oksijen bulamaz, çimlenme azalır."),
                F("Bu denklemi tanıyorsun: yanmaya benzer (K-2). Ama hücrede yavaş ve adım adım olur; alev çıkmaz.")),
            Page(
                K("Hesap Ustası"),
                T("Kaç tanesi çimlenir?"),
                B("Çimlenme olasılığı 0,8 ise her 10 tohumdan yaklaşık 8'i çimlenir."),
                St("Beklenen sayı = olasılık × tohum sayısı.",
                    "Tersinden: istenen filiz biliniyorsa tohum = filiz ÷ olasılık.",
                    "Tohum kesede satılıyorsa kese sayısını yukarı yuvarla."),
                R(),
                W("Dikkat", "Filiz sayısını olasılıkla çarpma, böl. Çarparsan daha az tohum bulursun, tarla boş kalır. Sonucu kendine sor: bu kadar tohumun yüzde 80'i istediğim sayı mı?")),
            Page(
                K("Çözümlü örnek"),
                T("Bostanda karpuz"),
                B("Bostancı Osman 45 karpuz fidesi istiyor. Karpuz tohumunun çimlenme olasılığı 0,9; tohum 10'luk paketlerde."),
                St("Tohum = 45 ÷ 0,9 = 50.",
                    "Kontrol: 50 × 0,9 = 45.",
                    "Paket: 50 ÷ 10 = 5 paket."),
                N("Unutma", "Beklenen sayı ortalamadır. 50 tohumdan 44 de çıkabilir, 47 de.")),
            Page(
                K("Bu görevde", "Çiftçi Elif, Bereket Tarlası"),
                T("Tarla boş kalmasın"),
                B("Elif 120 öbek filiz istiyor. Tohumun çimlenme olasılığı 0,8; pazarda tohum 10'luk keselerde."),
                St("Kaç tohum gerektiğini hesapla.",
                    "Pazardan tam o kadar kese al.",
                    "Tarladaki ekim sandığına dök."),
                R(),
                N("Unutma", "Dökülen tohum geri alınmaz; fazla kese akçeni boşa harcar.")));

        // ---------- Defter sayfası (Keşiflerim) ve şeritler ----------

        /// <summary>Kaydın defter sayfası: görev adı, sakin ve yer, denklem, ikon, kullanım notu, öteki derslerdeki saha notları
        /// (04-gorev-plani §4 "Saha notları").</summary>
        public static void ApplyPage(LearningEntry e)
        {
            switch (e.TaskId)
            {
                case "cirak_0":
                    e.WithPage("Çıraklık: İlk Kural", "Câbir Usta, atölye", Rx("2H₂ → 2H₂"), "H2", "");
                    break;
                case "denk_1":
                    e.WithPage("Kuru Çeşme", "Muhtar Yusuf, meydan çeşmesi", Rx("2H₂ + O₂ → 2H₂O"), "H2O", "",
                        Fn("Matematik", "Matematik, oran", "6 su için 6 H₂ ve 3 O₂ gerekir."),
                        Fn("Biyoloji", "Biyoloji, su döngüsü", "Dere kurudu: döngüde yağış eksik."));
                    break;
                case "denk_2":
                    e.WithPage("İsli Fenerler", "Fenerci Demir, çarşı fenerleri", Rx("CH₄ + 2O₂ → CO₂ + 2H₂O"), "CH4",
                        "Bu sayfa Fırıncının Mektubu’nda işe yarayacak.",
                        Fn("Biyoloji", "Biyoloji, hücresel solunum", "Hücre de besini O₂ ile kullanır; CO₂ ve su çıkar, ama yavaş ve adım adım."),
                        Fn("Fizik", "Fizik, enerji biçimleri", "Fenerde kimyasal enerji ısıya ve ışığa dönüşür."));
                    break;
                case "denk_3":
                    e.WithPage("Soğuk Fırın", "Fırıncı Zehra, kasaba fırını", Rx("C₃H₈ + 5O₂ → 3CO₂ + 4H₂O"), "C3H8", "",
                        Fn("Matematik", "Matematik, orantı", "Bir propan 5 O₂ ister; 3 propan 15 O₂ ister."),
                        Fn("Biyoloji", "Biyoloji, fermantasyon", "Hamur, mayanın çıkardığı CO₂ ile kabarır."));
                    break;
                case "denk_4":
                    e.WithPage("Yıkık Köprü", "Duvarcı Halil, taş köprü", Rx("CaO + H₂O → Ca(OH)₂"), "CaOH2", "",
                        Fn("Matematik", "Matematik, iki adımlı işlem", "İlk adımın ürünü ikinci adımın gireni oldu."),
                        Fn("Biyoloji", "Biyoloji, karbon döngüsü", "Kireç taşındaki karbon CO₂ olarak havaya karıştı."),
                        Fn("Fizik", "Fizik, enerji biçimleri", "Kireç söndürülürken kimyasal enerji ısı olarak çıkar."));
                    break;
                case "denk_5":
                    e.WithPage("Gübre Ruhu", "Çiftçi Elif, tarla", Rx("N₂ + 3H₂ → 2NH₃"), "NH3", "",
                        Fn("Matematik", "Matematik, doğrusal maliyet", "Maliyet = 3·(N₂ sayısı) + 2·(H₂ sayısı). Fazla alınan kavanoz maliyeti boşuna artırır."),
                        Fn("Biyoloji", "Biyoloji, azot döngüsü", "Bitki havadaki N₂’yi doğrudan kullanamaz; gübre bağlanmış azotu toprağa verir."));
                    break;
                case "bereket_2":
                    e.WithPage("Tohum Nefes Alır", "Çiftçi Elif, Bereket Tarlası", Rx("besin + O₂ → CO₂ + H₂O + enerji"), "", "",
                        Fn("Matematik", "Matematik, olasılık", "Tohum = filiz ÷ olasılık: 120 ÷ 0,8 = 150 tohum = 15 kese."),
                        Fn("Biyoloji", "Biyoloji, oksijenli solunum", "Çimlenen tohum besinini O₂ ile kullanır; sıkışmış toprakta çimlenme azalır."));
                    break;
                case "tur_1":
                    e.WithPage("Ustanın Defteri", "Câbir Usta’nın defteri, çalışma masası", "", "", "");
                    break;
                case "tur_2":
                    e.WithPage("Etiket Masası", "Güvercinle gelen mektup, çalışma masası", "", "", "",
                        Fn("Matematik", "Matematik, sıklık tablosu", "Kasabanın tepkimeleri türlere göre sayıldı: en az görülen analiz."),
                        Fn("Biyoloji", "Biyoloji, hücresel solunum", "Glikozun O₂ ile tepkimesi yanmaya benzer, ama hücrede enzimlerle adım adım olur."));
                    break;
                case "tur_3":
                    e.WithPage("Fırıncının Mektubu", "Fırıncı Zehra’nın mektubu, çalışma masası", "", "", "");
                    break;
                case "tur_4":
                    e.WithPage("Muhtarın Mektubu", "Muhtar Yusuf’un mektubu, çalışma masası", "", "", "",
                        Fn("Matematik", "Matematik, karşı örnek", "Her yanma CO₂ vermez: 2H₂ + O₂ → 2H₂O."),
                        Fn("Biyoloji", "Biyoloji, fotosentez", "Bitki besinini fotosentezle kendi üretir; topraktan su ve mineral alır."));
                    break;
                case "tur_5":
                    e.WithPage("Lonca Sınavı", "Kâtip Nevin, lonca", "", "", "");
                    break;
            }
        }

        /// <summary>Ders şeritleri (renkler 11-gorev-ve-basarim-listesi ve Figma Defter/4) ve köy başlıkları.</summary>
        public static void ApplyNotebookTexts(NotebookTexts texts)
        {
            texts.SetSubjects(new[]
            {
                new NotebookSubject("Kimya", Hex(0xE8602A), true, false),
                new NotebookSubject("Matematik", Hex(0x6FA9B8), false, false),
                new NotebookSubject("Fizik", Hex(0xA3B1BE), false, false),
                new NotebookSubject("Biyoloji", Hex(0x3FA83A), false, false),
                new NotebookSubject("Türkçe", Hex(0xB5431C), false, true),
                new NotebookSubject("Sosyal", Hex(0x8A5A3A), false, true)
            });
            texts.SetGroupTitles(new[]
            {
                new NotebookGroupTitle("ciraklik", "Atölye"),
                new NotebookGroupTitle("denklestirme", "Denkleştirme Köyü"),
                new NotebookGroupTitle("tepkime_turleri", "Tepkime Türleri Köyü")
            });
        }

        private static FieldNote Fn(string subject, string heading, string text) => new FieldNote(subject, heading, Rx(text));

        private static UnityEngine.Color Hex(int rgb) =>
            new UnityEngine.Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        // ---------- kısa yazım ----------

        private static LessonPage Page(params LessonBlock[] blocks) => new LessonPage(blocks);

        private static string Rx(string s) => LearningDataBuilder.Rich(s);

        private static LessonBlock K(string label, string detail = "") => new LessonBlock(LessonBlockKind.Kicker, label, detail);
        private static LessonBlock T(string s) => new LessonBlock(LessonBlockKind.Title, Rx(s));
        private static LessonBlock S(string s) => new LessonBlock(LessonBlockKind.Subhead, Rx(s));
        private static LessonBlock B(string s) => new LessonBlock(LessonBlockKind.Body, Rx(s));
        private static LessonBlock E(string s) => new LessonBlock(LessonBlockKind.Equation, Rx(s));
        private static LessonBlock F(string s) => new LessonBlock(LessonBlockKind.Footnote, Rx(s));
        private static LessonBlock R() => new LessonBlock(LessonBlockKind.Rule, string.Empty);
        private static LessonBlock N(string head, string s) => new LessonBlock(LessonBlockKind.Note, Rx(s), head);
        private static LessonBlock W(string head, string s) => new LessonBlock(LessonBlockKind.Warning, Rx(s), head);

        private static LessonBlock St(params string[] items) =>
            new LessonBlock(LessonBlockKind.Steps, string.Empty, string.Empty, LearningDataBuilder.RichAll(items));

        private static LessonBlock C(params (string element, int left, int right)[] rows)
        {
            var counts = new LessonCount[rows.Length];
            for (int i = 0; i < rows.Length; i++) counts[i] = new LessonCount(rows[i].element, rows[i].left, rows[i].right);
            return new LessonBlock(LessonBlockKind.Counts, string.Empty, string.Empty, null, counts);
        }
    }
}
