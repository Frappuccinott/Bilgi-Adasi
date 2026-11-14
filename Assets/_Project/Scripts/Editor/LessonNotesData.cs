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
                case "tur_1": return Tur1();
                case "tur_2": return Tur2();
                case "tur_3": return Tur3();
                case "tur_4": return Tur4();
                case "tur_5": return Tur5();
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
                K("Ders notu", "Biyoloji + Matematik · Hayat Ustası"),
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

        // ---------- Masa görevleri (Tepkime Türleri, 13-ders-notu-plani §6) ----------
        // Türler oyundaki üç kutuyla aynı: Sentez · Analiz · Yanma (DeskDataBuilder.Boxes). Yanma = karbonlu yakıt + O₂;
        // H₂ + O₂ → H₂O oyunda Sentez kutusunda (tur_2_etiket), not bununla çelişmez. "Bu görevde" sayfası cevap vermez.

        private static LessonNote Tur1() => new LessonNote("Gözlemden türe",
            Page(
                K("Ders notu", "Tepkime Türleri Köyü, 1. görev"),
                T("Gözlem kanıttır"),
                B("Bir tepkimenin türünü anlamak için önce ne gördüğüne bak: kaç madde girdi, kaç madde çıktı, ısı ya da ışık var mı? Gözlem, türün kanıtıdır."),
                R(),
                S("Her olayda üç soru sor"),
                St("Girenler kaç tane: tek madde mi, birden çok mu?",
                    "Ürünler kaç tane: tek ürün mü, birden çok mu?",
                    "Bir yakıt oksijenle yanıyor mu, ısı ve ışık çıkıyor mu?"),
                F("Bu köyde üç türle çalışacaksın: sentez, analiz, yanma.")),
            Page(
                T("Üç tür"),
                S("Sentez (birleşme)"),
                B("Birden çok madde birleşir, tek bir ürün oluşur."),
                S("Analiz (ayrışma)"),
                B("Tek bir madde daha basit maddelere ayrılır."),
                S("Yanma"),
                B("Bir madde oksijenle tepkimeye girer; genellikle ısı ve ışık çıkar."),
                R(),
                W("Dikkat", "Isınan her kap yanma değildir. Kireç söndürülürken kova ısınır ama ortada alev ve oksijen yoktur: sönmemiş kireç suyla birleşip tek ürün verir.")),
            Page(
                K("Çözümlü örnek"),
                T("Gümüş kaşık karardı"),
                B("Gözlem: parlak gümüş kaşık, havadaki kükürtle birleşti; yüzeyinde tek bir siyah madde oluştu."),
                St("Girenler: gümüş ve kükürt, iki madde.",
                    "Ürün: tek bir siyah madde.",
                    "Alev yok, oksijen yanmıyor."),
                E("2Ag + S → Ag₂S"),
                B("Birden çok giren, tek ürün: sentez.")),
            Page(
                K("Bu görevde", "Câbir Usta’nın defteri, çalışma masası"),
                T("Defteri tamamla"),
                B("Usta masaya, kazanda senin gördüğün olayların gözlem kartlarını bıraktı. Defterinin bazı satırları da ıslanıp silinmiş."),
                St("Her kartta üç soruyu sor: kaç giren, kaç ürün, yanma var mı?",
                    "Kartı kanıtı olduğu türün kutusuna koy.",
                    "Sonra defterin boş satırlarını kelime bankasıyla doldur."),
                R(),
                N("Unutma", "Kelime bankasında fazladan kelimeler var; hepsini kullanmak zorunda değilsin.")));

        private static LessonNote Tur2() => new LessonNote("Denklemden türe",
            Page(
                K("Ders notu", "Tepkime Türleri Köyü, 2. görev"),
                T("Denklemden türü oku"),
                B("Denklemde girenler okun solunda, ürünler sağındadır. Türü bulmak için iki tarafta kaç farklı madde olduğunu say."),
                R(),
                S("Bakma sırası"),
                St("Solda tek madde, sağda birden çok madde: analiz.",
                    "Solda karbonlu bir yakıt ve O₂, sağda CO₂ (çoğu zaman H₂O da): yanma.",
                    "Solda birden çok madde, sağda tek ürün: sentez."),
                F("Formülsüz bir gözlem kartında da aynı sırayı izle: ayrılan mı var, yanan mı, birleşen mi?")),
            Page(
                T("Katsayı madde sayısı değildir"),
                B("Tür için kaç farklı madde olduğuna bak, katsayıya değil. 2H₂O yine tek bir maddedir: su."),
                E("2H₂ + O₂ → 2H₂O"),
                B("Solda iki farklı madde (H₂ ve O₂), sağda tek madde (H₂O)."),
                R(),
                W("Dikkat", "Katsayı 2 diye “iki ürün var” deme. Sağ tarafta kaç farklı formül olduğunu say.")),
            Page(
                K("Çözümlü örnek"),
                T("Üç şişe, üç kutu"),
                E("2HgO → 2Hg + O₂"),
                B("Solda tek madde, sağda iki madde: analiz."),
                E("C + O₂ → CO₂"),
                B("Karbon (kömür) oksijenle yanıyor: yanma."),
                E("Fe + S → FeS"),
                B("İki madde birleşti, tek ürün: sentez."),
                F("Kömürün yanması da tek ürün verir; ama yakıt oksijenle yandığı için önce yanmayı düşün.")),
            Page(
                K("Bu görevde", "Güvercinle gelen mektup, çalışma masası"),
                T("Etiketleri düzelt"),
                B("Şişelerin etiketleri karışmış. Bazı şişede denklem yazıyor, bazısında yalnızca bir gözlem."),
                St("Her şişede iki tarafı say: kaç giren, kaç ürün?",
                    "Bakma sırasını izle: önce ayrışma, sonra yanma, en son birleşme.",
                    "Şişeyi kutusuna koy: Sentez, Analiz ya da Yanma."),
                R(),
                N("Unutma", "Kasabanın kazanında yaptığın tepkimeleri tanıyacaksın. Defterindeki kayıtlara bakabilirsin.")));

        private static LessonNote Tur3() => new LessonNote("Kimyasal değişimin kanıtı",
            Page(
                K("Ders notu", "Tepkime Türleri Köyü, 3. görev"),
                T("Fiziksel mi, kimyasal mı?"),
                B("Fiziksel değişimde madde aynı kalır; yalnızca şekli ya da hâli değişir. Kimyasal değişimde yeni bir madde oluşur."),
                R(),
                S("Yeni madde oluştuğunu nereden anlarız?"),
                St("Gaz çıkışı, kabarcık",
                    "Renk değişimi",
                    "Yeni bir koku",
                    "Isı ya da ışık çıkması"),
                F("Bir kanıt tek başına her zaman yetmez; ne kadar çok kanıt varsa o kadar emin olursun.")),
            Page(
                T("Hâl değişimi tepkime değildir"),
                B("Buz eriyince su olur, su kaynayınca buhar olur. Üçü de aynı maddedir: H₂O. Yeni madde yoktur, değişim fizikseldir."),
                R(),
                W("Dikkat", "Kesmek, ezmek, yoğurmak, eritmek ve buharlaştırmak maddenin kendisini değiştirmez. Görünüş değişti diye kimyasal deme.")),
            Page(
                K("Çözümlü örnek"),
                T("Buz ve odun"),
                S("Buzun erimesi"),
                B("Katı su sıvı su oldu. Madde yine H₂O: fiziksel değişim."),
                S("Odunun yanması"),
                B("Alev ve ısı çıktı, duman yükseldi, geriye siyah kül kaldı. Yeni maddeler oluştu: kimyasal değişim.")),
            Page(
                K("Bu görevde", "Fırıncı Zehra’nın mektubu, çalışma masası"),
                T("Zehra’ya kanıt göster"),
                B("Zehra fırınının yakıtını ve ekmeğini soruyor."),
                St("Önce yakıtın yanınca ne verdiğini seç. İsli Fenerler’de yanmayı görmüştün; defterindeki kaydı hatırla.",
                    "Sonra ekmekte olanlardan kimyasal değişimin kanıtlarını işaretle.",
                    "Her seçenekte kendine sor: yeni bir madde oluştu mu?"),
                R(),
                N("Unutma", "Birden çok doğru seçenek olabilir.")));

        private static LessonNote Tur4() => new LessonNote("Yanlış iddiayı düzelt",
            Page(
                K("Ders notu", "Tepkime Türleri Köyü, 4. görev"),
                T("Bir iddia nasıl sınanır?"),
                B("Bir cümlenin doğru olduğunu söylemek için bildiğin kurallarla karşılaştır. Yanlış olduğunu göstermek için tek bir karşı örnek yeter."),
                R(),
                S("Üç adım"),
                St("İddiayı dikkatle oku: hangi tür ya da hangi kural hakkında?",
                    "Bildiğin tanımla ve bir örnek denklemle karşılaştır.",
                    "Yanlışsa doğrusunu söyle ve kanıtını göster."),
                F("Dedikoduya karşı en iyi ilaç kanıttır.")),
            Page(
                T("Sık duyulan yanılgılar"),
                W("Yanılgı", "“Yanan odun hafifliyor, demek ki kütle kayboldu.” Kaybolmadı: CO₂ ve su buharı havaya karıştı. Atomlar başka maddelerde."),
                W("Yanılgı", "“2H₂O’daki 2, suyun formülüne aittir.” Hayır: katsayı kaç molekül olduğunu söyler. Suyun formülü H₂O’dur."),
                R(),
                N("Unutma", "Atomlar tepkimede kaybolmaz, yoktan da var olmaz (K-0).")),
            Page(
                K("Çözümlü örnek"),
                T("“Her yanma CO₂ verir.”"),
                B("Karşı örnek ara: karbon içermeyen bir yakıt var mı?"),
                E("2H₂ + O₂ → 2H₂O"),
                B("Hidrojen oksijenle yanar ama karbon yoktur, CO₂ de çıkmaz. Tek örnek iddiayı çürüttü."),
                S("Doğrusu"),
                B("Karbon içeren yakıtlar yanınca CO₂ verir.")),
            Page(
                K("Bu görevde", "Muhtar Yusuf’un mektubu, çalışma masası"),
                T("Kasabadaki dedikodular"),
                B("Muhtar kasabada dolaşan cümleleri gönderdi. Hangisi doğru, hangisi yanlış, mühürle işaretleyeceksin."),
                St("Her cümleyi tanımlarla karşılaştır.",
                    "Doğruysa D, yanlışsa Y mührü bas.",
                    "Y dediğin cümlenin doğrusunu da seç."),
                R(),
                N("Unutma", "Defterindeki kayıtlar kanıttır. Sentez, analiz ve atomların korunumu için önceki sayfalarına bak.")));

        private static LessonNote Tur5() => new LessonNote("Lonca sınavı: tekrar",
            Page(
                K("Ders notu", "Tepkime Türleri Köyü, 5. görev"),
                T("Bu ünitede öğrendiklerin"),
                St("Tepkimede atomlar kaybolmaz, yoktan var olmaz.",
                    "Denkleştirirken katsayı yazılır; alt indis değişmez.",
                    "Önce tek yerde geçen elementleri, oksijeni en son say.",
                    "Sentez: birden çok giren, tek ürün. Analiz: tek giren, birden çok ürün. Yanma: yakıt + O₂."),
                R(),
                F("Bu sayfada yeni bilgi yok; hepsi defterinde yazılı.")),
            Page(
                T("Kontrol listesi"),
                S("Tür sorusunda"),
                St("Girenleri say.", "Ürünleri say.", "Yakıt ve O₂ ara."),
                S("Denkleştirme sorusunda"),
                St("Her elementi iki tarafta ayrı say.", "Katsayıyla alt indisi çarp.", "Bütün elementler eşitse dur."),
                R(),
                W("Dikkat", "Bazı tepkimeler üç kutunun hiçbirine tam uymaz. Türler bütün tepkimeleri kapsamaz.")),
            Page(
                K("Hatırla"),
                T("Döngü"),
                B("Fotosentezde bitki CO₂ ve suyu glikoza bağlar; solunumda glikoz yeniden CO₂ ve suya döner."),
                E("6CO₂ + 6H₂O → C₆H₁₂O₆ + 6O₂"),
                B("Aynı atomlar (C, H, O) yakıtta, havada, bitkide ve bizde dolaşır."),
                F("Glikozun formülü C₆H₁₂O₆; denkleştirirken önce karbonu say.")),
            Page(
                K("Bu görevde", "Kâtip Nevin, lonca"),
                T("Lonca mührü"),
                B("Kâtip Nevin karışık sorular soracak: türler, denkleştirme ve yeni tepkimeler."),
                St("Her soruda önce soruyu tanı: tür mü, denkleştirme mi?",
                    "Kontrol listesini uygula.",
                    "Yanlış yazdığın soru çekilir, yerine başkası gelir. Mühür hepsi doğru olunca basılır."),
                R(),
                N("Unutma", "Acele etme; her soruyu saydıktan sonra gönder.")));

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
