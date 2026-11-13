using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.City;
using BilgiAdasi.Core;
using BilgiAdasi.Desk;
using UnityEditor;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Tepkime Türleri köyü (Seviye 6–10, 05 §3 + 04-gorev-plani §4) verisini üretir:
    /// glikoz + fotosentez/solunum, soru asset'leri, tur_1 … tur_5, köy asset'i (önkoşul: Denkleştirme),
    /// DeskTexts ve Sorgu + Keşif Defteri kayıtları (K-6 … K-10).
    /// Var olan asset'in üzerine yazar ama GUID'i korur (sahne/köy referansları kopmaz). Metin düzeltmesi burada yapılır.
    /// </summary>
    public static class DeskDataBuilder
    {
        private const string Data = "Assets/_Project/Data";
        private const string QuestionsFolder = Data + "/Questions/TepkimeTurleri";
        private const string TasksFolder = Data + "/Tasks/TepkimeTurleri";
        private const string NeighborhoodPath = Data + "/Neighborhoods/Neighborhood_tepkime_turleri.asset";
        private const string DenklestirmePath = Data + "/Neighborhoods/Neighborhood_denklestirme.asset";
        private const string TextsPath = Data + "/Texts/DeskTexts.asset";

        private static readonly string[] Boxes = { "Sentez", "Analiz", "Yanma" };

        // 05 §3 — içerik değişirse önce 05 güncellenir.
        private static readonly string[] Intro =
        {
            "Her tepkimenin bir huyu var. Kimi birleştirir, kimi ayırır, kimi yakar.",
            "Kasaba sana mektup yazacak; cevapları masada vereceksin. Güvercin pencereye konunca masaya otur."
        };

        [MenuItem("BilgiAdasi/Seviye/Tepkime Türleri (Seviye 6–10) verisini üret")]
        public static void Build()
        {
            EnsureFolder(Data, "Questions");
            EnsureFolder(Data + "/Questions", "TepkimeTurleri");
            EnsureFolder(Data + "/Tasks", "TepkimeTurleri");

            var glucose = EnsureGlucose();
            var reactions = LoadReactions(glucose);
            if (reactions == null) return;
            EnsureTexts();

            var q = BuildQuestions(reactions);
            var tasks = BuildTasks(q);
            BuildNeighborhood(tasks);
            LearningDataBuilder.Build();

            AssetDatabase.SaveAssets();
            Debug.Log($"[Masa] Tepkime Türleri verisi üretildi: {q.Count} soru, {tasks.Count} görev → {NeighborhoodPath}");
        }

        // ---------- kimya ----------

        private static SpeciesData EnsureGlucose()
        {
            const string path = Data + "/Species/Species_C6H12O6.asset";
            var existing = AssetDatabase.LoadAssetAtPath<SpeciesData>(path);
            if (existing != null) return existing;
            var c = Element("C");
            var h = Element("H");
            var o = Element("O");
            var s = SpeciesData.Create("C6H12O6", "Glikoz", new ElementCount(c, 6), new ElementCount(h, 12), new ElementCount(o, 6));
            var so = new SerializedObject(s);
            so.FindProperty("jarColor").colorValue = new Color(0.93f, 0.86f, 0.62f);
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(s, path);
            return s;
        }

        private static Dictionary<string, ReactionData> LoadReactions(SpeciesData glucose)
        {
            var map = new Dictionary<string, ReactionData>();
            foreach (var id in new[] { "R_water", "R_methane", "R_propane", "R_lime1", "R_ammonia", "R_co", "R_waterSplit" })
            {
                var r = AssetDatabase.LoadAssetAtPath<ReactionData>($"{Data}/Reactions/{id}.asset");
                if (r == null)
                {
                    EditorUtility.DisplayDialog("Tepkime Türleri", id + " bulunamadı. Önce 'Kurulum → 2' çalıştırılmalı.", "Tamam");
                    return null;
                }
                map[id] = r;
            }
            var co2 = Species("CO2");
            var h2o = Species("H2O");
            var o2 = Species("O2");
            map["R_photosynthesis"] = EnsureReaction("R_photosynthesis",
                new[] { new SpeciesAmount(co2, 6), new SpeciesAmount(h2o, 6) },
                new[] { new SpeciesAmount(glucose, 1), new SpeciesAmount(o2, 6) });
            map["R_respiration"] = EnsureReaction("R_respiration",
                new[] { new SpeciesAmount(glucose, 1), new SpeciesAmount(o2, 6) },
                new[] { new SpeciesAmount(co2, 6), new SpeciesAmount(h2o, 6) });
            return map;
        }

        private static ReactionData EnsureReaction(string id, SpeciesAmount[] reactants, SpeciesAmount[] products)
        {
            string path = $"{Data}/Reactions/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ReactionData>(path);
            if (existing != null) return existing;
            var r = ReactionData.Create(id, ReactionType.Other, reactants, products);
            AssetDatabase.CreateAsset(r, path);
            return r;
        }

        private static ElementData Element(string symbol) =>
            AssetDatabase.LoadAssetAtPath<ElementData>($"{Data}/Elements/Element_{symbol}.asset");

        private static SpeciesData Species(string formula) =>
            AssetDatabase.LoadAssetAtPath<SpeciesData>($"{Data}/Species/Species_{formula}.asset");

        private static void EnsureTexts()
        {
            if (AssetDatabase.LoadAssetAtPath<DeskTexts>(TextsPath) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<DeskTexts>(), TextsPath);
        }

        // ---------- sorular ----------

        private static Dictionary<string, QuestionData> BuildQuestions(Dictionary<string, ReactionData> r)
        {
            var q = new Dictionary<string, QuestionData>();

            // T-1 katmanı: gözlemden çıkarım (oyuncunun kendi siparişleri)
            Save(q, ClassifyQ.Create("tur_1_gozlem",
                T("Usta defterini açmadan önce masaya üç gözlem kartı bıraktı. Her gözlem hangi tepkime türünün kanıtı?"),
                T("Çeşmeyi, köprüyü ve feneri hatırla: neler birleşti, ne ayrıldı, ne yandı?"),
                Boxes, new List<ClassifyQ.Card>
                {
                    Card(T("Çeşme: iki gaz kazanda birleşti, tek ürün oldu."), 0),
                    Card(T("Köprü: kireç taşı ısıtıldı, iki maddeye ayrıldı."), 1),
                    Card(T("Fener: yakıt oksijenle birleşti, ısı ve ışık çıktı."), 2),
                }));

            Save(q, FillBlankQ.Create("tur_1_defter",
                T("Defterin ıslanan satırlarını kelime bankasıyla tamamla."),
                T("Kelimelere bak: birleşmek, ayrışmak, yanmak."),
                new[] { "sentez", "analiz", "yanma", "oksijen", "buharlaşma", "azot" },
                new List<FillBlankQ.Sentence>
                {
                    Sentence("İki ya da daha fazla maddenin birleşip tek bir ürün oluşturduğu tepkimeye {0} denir.", "sentez"),
                    Sentence("Tek bir maddenin daha basit maddelere ayrıştığı tepkimeye {0} denir.", "analiz"),
                    Sentence("Bir maddenin {0} ile tepkimeye girip genellikle ısı ve ışık verdiği tepkimeye {1} denir.", "oksijen", "yanma"),
                }));

            Save(q, ClassifyQ.Create("tur_2_etiket",
                T("Şişelerin etiketleri karışmış. Her şişeyi doğru kutuya koy."),
                T("Girenlere bak: tek madde mi var, oksijen var mı, ürün tek mi?"),
                Boxes, new List<ClassifyQ.Card>
                {
                    Card(r["R_water"], 0), Card(r["R_lime1"], 1), Card(r["R_methane"], 2),
                    Card(r["R_waterSplit"], 1), Card(r["R_ammonia"], 0), Card(r["R_propane"], 2),
                    // katman: formülsüz gözlem kartları
                    Card(T("Kireç taşı ısıtılınca gaz çıktı, beyaz toz kaldı."), 1),
                    Card(T("Fenerin alevi ısıttı, cam buğulandı."), 2),
                    Card(T("İki gaz kazanda birleşti, damla damla su oldu."), 0),
                }));

            Save(q, MultipleChoiceQ.Create("tur_3_propan",
                T("Propan tam yandığında ürünler hangisidir?"),
                T("C₃H₈ + 5O₂ → ? Tahtana bak; fenerdeki metan da aynı ürünleri vermişti (K-2)."),
                new[] { T("Karbondioksit ve su"), T("Hidrojen ve oksijen"), T("Yalnızca su"), T("Azot ve su") }, new[] { 0 }));

            Save(q, MultipleChoiceQ.Create("tur_3_kanit",
                T("Ekmek kabarıp kızarıyor. Kimyasal değişimin kanıtı olanları işaretle."),
                T("Yeni madde oluştu mu? Yalnız şekil ya da hâl değişimi fizikseldir."),
                new[]
                {
                    T("Hamurda gaz kabarcıkları (CO₂) oluşuyor."),
                    T("Kabuğun rengi değişiyor."),
                    T("Fırından yeni bir koku yayılıyor."),
                    T("Hamur yoğrulunca şekli değişiyor."),
                    T("Hamurdaki su buharlaşıyor.")
                }, new[] { 0, 1, 2 }, allowMultiple: true));

            Save(q, TrueFalseQ.Create("tur_4_dedikodu",
                T("Her ifadeye D ya da Y mührü bas. Y dediklerinin doğrusunu da seç."),
                T("Ustanın tek kuralını hatırla: kazana ne girerse o çıkar."),
                new List<TrueFalseQ.Statement>
                {
                    Statement("Analiz tepkimesinde tek bir giren birden fazla ürüne ayrışır.", true),
                    Statement("Yanma tepkimelerinde oksijen girenler arasındadır.", true),
                    Statement("Sentez tepkimesinde ürünlerin sayısı girenlerden fazladır.", false,
                        "Sentezde birden çok giren tek ürün oluşturur.",
                        "Sentezde hiç ürün oluşmaz.",
                        "Sentezde tek giren ikiye ayrılır."),
                    Statement("Bir tepkimede atomlar kaybolabilir.", false,
                        "Atomlar kaybolmaz; yalnızca yeniden dizilir.",
                        "Atomlar yalnızca yanmada kaybolur.",
                        "Atomlar kaybolur ama kütle değişmez."),
                }));

            // Lonca havuzuna ek sorular (05 §3 Görev 5 + 04 §4 Seviye 10 transfer)
            Save(q, MultipleChoiceQ.Create("lonca_mc_kirec", T("CaCO₃ → CaO + CO₂ hangi tür tepkimedir?"),
                T("Girenlere bak: kaç madde var?"),
                new[] { "Analiz", "Sentez", "Yanma", "Çözünme" }, new[] { 0 }));
            Save(q, MultipleChoiceQ.Create("lonca_mc_amonyak", T("N₂ + 3H₂ → 2NH₃ hangi tür tepkimedir?"),
                T("Ürünlere bak: kaç madde çıkıyor?"),
                new[] { "Sentez", "Analiz", "Yanma", "Nötrleşme" }, new[] { 0 }));
            Save(q, MultipleChoiceQ.Create("lonca_mc_yanma", T("Aşağıdakilerden hangisi bir yanma tepkimesinin girenidir?"),
                T("Yanma için yakıtın neyle birleşmesi gerekir?"),
                new[] { T("O₂"), T("N₂"), T("H₂O"), T("CO₂") }, new[] { 0 }));
            Save(q, MultipleChoiceQ.Create("lonca_mc_harc",
                T("Ca(OH)₂ + CO₂ → CaCO₃ + H₂O (harcın sertleşmesi) üç kutudan hangisine girer?"),
                T("Tek ürün var mı? Tek giren var mı? Oksijen yanıyor mu?"),
                new[] { T("Hiçbirine tam uymaz; türler bütün tepkimeleri kapsamaz."), "Sentez", "Analiz", "Yanma" }, new[] { 0 }));

            const string balancePrompt = "Denklemi denkleştir: katsayıları seç.";
            const string balanceHint = "Her elementi iki tarafta ayrı say. Önce tek yerde geçen elementler, oksijen en son.";
            Save(q, BalanceEquationQ.Create("lonca_be_su", balancePrompt, balanceHint, r["R_water"]));
            Save(q, BalanceEquationQ.Create("lonca_be_metan", balancePrompt, balanceHint, r["R_methane"]));
            Save(q, BalanceEquationQ.Create("lonca_be_co", balancePrompt, balanceHint, r["R_co"]));
            Save(q, BalanceEquationQ.Create("lonca_be_fotosentez", T("Bitkinin yaprağında (fotosentez) denklemi denkleştir."),
                T("Önce karbon (glikozda 6), sonra hidrojen, oksijen en son."), r["R_photosynthesis"]));
            Save(q, BalanceEquationQ.Create("lonca_be_solunum", T("Hücrede (solunum) denklemi denkleştir."),
                T("Fotosentezin tersine bak: aynı katsayılar."), r["R_respiration"]));

            Save(q, ClassifyQ.Create("lonca_cl_mini", T("Bu iki şişeyi doğru kutuya koy."),
                T("Girenlerde O₂ var mı? Tek giren mi var?"),
                Boxes, new List<ClassifyQ.Card> { Card(r["R_co"], 2), Card(r["R_waterSplit"], 1) }));

            return q;
        }

        // ---------- görevler ----------

        private static List<TaskData> BuildTasks(Dictionary<string, QuestionData> q)
        {
            var list = new List<TaskData>
            {
                SaveTask(DeskTask.Create("tur_1", "Câbir Usta",
                    L("Çırak, defterime su sıçramış; üç satırın kelimeleri silinmiş.",
                      "Önce masadaki gözlem kartlarına bak: kazanda kendi gördüklerin. Sonra satırları tamamla."),
                    L("Usta defteri kaldırdı: \"Satırlar yerine oturdu. Gördüğünü adlandırmayı öğrendin.\""),
                    T("Kelimelere bak: birleşmek, ayrışmak, yanmak."), "",
                    DeskWrapper.MasterNotebook, "Ustanın Defteri", Qs(q, "tur_1_gozlem", "tur_1_defter"))),

                SaveTask(DeskTask.Create("tur_2", "Câbir Usta",
                    L("Raftaki tepkime şişelerinin etiketleri karışmış.",
                      "Her şişeyi doğru kutuya koy: Sentez, Analiz, Yanma."),
                    L("Usta şişeleri rafa dizdi: \"Artık her şişe kendi kutusunda.\""),
                    T("Girenlere bak: tek madde mi var, oksijen var mı, ürün tek mi?"), "",
                    DeskWrapper.LabelTable, "Etiket Masası", Qs(q, "tur_2_etiket"))),

                SaveTask(DeskTask.Create("tur_3", "Fırıncı Zehra",
                    L("Çırak, fırında gaz yanınca neler çıkıyor? Usta bir keresinde söylemişti, unuttum.",
                      "Bir de: ekmek kabarıp kızarıyor; bu da mı kimyasal? Kanıt göster."),
                    L("Zehra'nın cevabı güvercinle geldi: \"Demek fırınım her sabah küçük bir kimya dersi!\""),
                    T("C₃H₈ + 5O₂ → ? Tahtana bak."), "",
                    DeskWrapper.Letter, "Fırıncının Mektubu", Qs(q, "tur_3_propan", "tur_3_kanit"))),

                SaveTask(DeskTask.Create("tur_4", "Muhtar Yusuf",
                    L("Kasabada dedikodu çok. Hangisi doğru, hangisi yanlış, işaretleyip gönder.",
                      "Yanlış olanların doğrusunu da yaz ki meydanda okuyayım."),
                    L("Muhtar panoya astı: \"Dedikoduya karşı en iyi ilaç: kanıt.\""),
                    T("Ustanın tek kuralını hatırla: kazana ne girerse o çıkar."), "",
                    DeskWrapper.Letter, "Muhtarın Mektubu", Qs(q, "tur_4_dedikodu"))),
            };

            var pool = Qs(q, "tur_1_defter", "tur_2_etiket", "tur_3_propan", "tur_4_dedikodu",
                "lonca_mc_kirec", "lonca_mc_amonyak", "lonca_mc_yanma", "lonca_mc_harc",
                "lonca_be_su", "lonca_be_metan", "lonca_be_co", "lonca_be_fotosentez", "lonca_be_solunum", "lonca_cl_mini");
            list.Add(SaveTask(DeskTask.Create("tur_5", "Kâtip Nevin",
                L("Simyacılar Loncası seni Tepkime Türleri sınavına çağırır.",
                  "Dört soru. Yanlış yazılan soru çekilir, yerine başkası gelir. Mühür ancak hepsi doğruysa basılır."),
                L("Kâtip Nevin sınav kâğıdını mühürledi: \"Lonca seni kayda geçirdi, çırak.\""),
                T("Her soruda önce türü düşün: birleşme mi, ayrışma mı, oksijenle yanma mı?"),
                T("Sindirim, paslanma, ekmeğin kabarması... Çevrendeki birçok değişim bu tepkime türlerinden biridir."),
                DeskWrapper.GuildExam, "Lonca Sınavı", pool, 4)));
            return list;
        }

        private static void BuildNeighborhood(List<TaskData> tasks)
        {
            var n = NeighborhoodData.Create("tepkime_turleri", "Tepkime Türleri", Intro, tasks);
            n.SetRequires(AssetDatabase.LoadAssetAtPath<NeighborhoodData>(DenklestirmePath));
            CreateOrUpdate(n, NeighborhoodPath);
        }

        // ---------- yardımcılar ----------

        private static void Save(Dictionary<string, QuestionData> map, QuestionData q)
        {
            map[q.Id] = CreateOrUpdate(q, $"{QuestionsFolder}/{q.Id}.asset");
        }

        private static TaskData SaveTask(DeskTask t) => CreateOrUpdate(t, $"{TasksFolder}/{t.Id}.asset");

        /// <summary>Yoksa oluşturur; varsa içeriği kopyalar (GUID ve dışarıdan gelen referanslar korunur).</summary>
        private static T CreateOrUpdate<T>(T fresh, string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(fresh, path);
                return fresh;
            }
            string keepName = existing.name;
            EditorUtility.CopySerialized(fresh, existing);
            existing.name = keepName;
            Object.DestroyImmediate(fresh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static List<QuestionData> Qs(Dictionary<string, QuestionData> map, params string[] ids)
        {
            var list = new List<QuestionData>(ids.Length);
            foreach (var id in ids) list.Add(map[id]);
            return list;
        }

        private static ClassifyQ.Card Card(string text, int box) => new ClassifyQ.Card { text = text, box = box };
        private static ClassifyQ.Card Card(ReactionData reaction, int box) => new ClassifyQ.Card { reaction = reaction, box = box };

        private static FillBlankQ.Sentence Sentence(string text, params string[] answers) =>
            new FillBlankQ.Sentence { text = T(text), answers = answers };

        private static TrueFalseQ.Statement Statement(string text, bool isTrue, params string[] corrections) =>
            new TrueFalseQ.Statement { text = T(text), isTrue = isTrue, corrections = LearningDataBuilder.RichAll(corrections), correctionIndex = 0 };

        private static string T(string text) => LearningDataBuilder.Rich(text);

        private static string[] L(params string[] lines) => LearningDataBuilder.RichAll(lines);

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
