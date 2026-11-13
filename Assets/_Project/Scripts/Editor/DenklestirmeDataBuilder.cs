using BilgiAdasi.Chemistry;
using BilgiAdasi.City;
using BilgiAdasi.Core;
using BilgiAdasi.Order;
using UnityEditor;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Denkleştirme köyünün 5 siparişini (05-icerik-prototip §2 + 04-gorev-plani §4 Seviye 1–5) SO olarak üretir.
    /// Var olan asset'lerin üstüne yazar (metin düzeltmeleri burada yapılır, sonra menü tekrar çalıştırılır).
    /// </summary>
    public static class DenklestirmeDataBuilder
    {
        private const string TaskFolder = "Assets/_Project/Data/Tasks";
        private const string NeighborhoodPath = "Assets/_Project/Data/Neighborhoods/Neighborhood_Denklestirme.asset";
        private const string TextsPath = "Assets/_Project/Data/GameTexts.asset";

        [MenuItem("BilgiAdasi/Seviye/1) Denkleştirme verisini üret")]
        public static void Build()
        {
            var texts = LoadOrCreate<GameTexts>(TextsPath);

            var d1 = Order("D-1", "Kuru Çeşme", "Muhtar Yusuf",
                request: "Çeşme kurudu evladım! Çocuklar dereye iniyor, dere de kurudu. Sen de bilirsin değil mi? Ustan suyu kazanda yapardı. İki su lazım bize, tastamam.",
                reminder: "Kazana ne koyacağını say, evladım. Atölyede seni bekleyen bir kazan var.",
                deliver: "Aktı! Gerçekten aktı!",
                after: "Çeşmenin başında çocuklar oynuyor yine. Sağ ol evladım.",
                hint: "Sudaki oksijenleri say. Bir su kaç oksijen ister?",
                realLife: "Hidrojen ile oksijen gerçekte çok hızlı ve gürültülü tepkimeye girer; yalnızca laboratuvarda, uzman gözetiminde yapılır.",
                question: "Peki çırak, neden iki su molekülü elde ettin?",
                options: new[]
                {
                    "Bir O₂'de iki oksijen atomu var; her su bir oksijen alıyor, o yüzden iki su oluşuyor.",
                    "Suyun formülü 2H₂O'dur.",
                    "İki hidrojen kavanozu koydum, oksijenin sayısı önemli değil."
                },
                correct: 0,
                replies: new[]
                {
                    "Aynen öyle. Atomları saydın, kazan da sana hak verdi.",
                    "Hayır çırak: 2 kaç tane su olduğunu söyler. Suyun formülü yine H₂O'dur; katsayı formülün parçası değildir.",
                    "Tek elemente bakma. Oksijen de sayılır: bir O₂ iki oksijen getirir, ikisi de bir suya gider."
                },
                discoveryId: "K-1",
                discovery: "Tepkimede atomların türü ve sayısı korunur; değişen, atomların birbirine bağlanışıdır. Su oluşurken her 2 H₂ için 1 O₂ harcanır.");
            SetOrder(d1, OrderMode.Exact, true, new[] { "H2", "O2" }, Step("R_water", 1));

            var d2 = Order("D-2", "İsli Fenerler", "Fenerci Demir",
                request: "Fenerler is yapıyor. Akşam çarşıda kimse kimseyi görmüyor. Gaz bende, hava sende. Ne kadar lazımsa o kadar ver.",
                reminder: "Fazla hava da az hava da işe yaramaz. Sayarak ver, çırak.",
                deliver: "Hah. Şimdi oldu. Sağ ol çırak.",
                after: "Bak şu ışığa! Çarşı gece bile gündüz gibi.",
                hint: "Sağ terazideki oksijenleri say: karbondioksitte kaç, suda kaç?",
                realLife: "Yakıt yeterli havayla yanmazsa is ve zehirli karbon monoksit çıkar; soba ve şofben olan odalar bu yüzden havalandırılır.",
                question: "Neden bir değil iki O₂ koydun?",
                options: new[]
                {
                    "CO₂'deki 2 ve iki suyun içindeki 2 oksijen eder 4; her O₂ iki oksijen getirir.",
                    "Metan çok yanıcı, fazladan hava iyi gelir.",
                    "Karbon iki oksijen istiyor, bir O₂ yeter."
                },
                correct: 0,
                replies: new[]
                {
                    "Aynen öyle. Oksijeni iki ürünün içinde ayrı ayrı saydın.",
                    "Sezgi güzel ama sayım yok. Fazla hava 'iyi gelmez', yalnızca artar. 4 oksijen gerekir, o kadar.",
                    "Suyu unuttun. Karbondioksit 2 oksijen alır, iki su da 2 oksijen daha: toplam 4, yani iki O₂."
                },
                discoveryId: "K-2",
                discovery: "Yanma tepkimelerinde oksijenin rolü vardır: yakıt O₂ ile birleşir. Karbon ve hidrojen içeren yakıtlar CO₂ ve H₂O verir. Hava yetmezse is (karbon) ve CO çıkar.");
            SetOrder(d2, OrderMode.Exact, true, new[] { "CH4", "O2" }, Step("R_methane", 1));

            var d3 = Order("D-3", "Soğuk Fırın", "Fırıncı Zehra",
                request: "Fenercinin fenerlerini yakmışsın, duydum. Benim fırın onun fenerinden büyük ama. Kasaba iki gündür peksimet yiyor.",
                reminder: "Fırın soğuk duruyor hâlâ. Propan bende, gerisi senin hesabın.",
                deliver: "İşte bu koku! Akşama ilk somun senin.",
                after: "Somunun hazır, sıcak sıcak. Ustana da götür.",
                hint: "Oksijeni sona bırak. Önce karbonu, sonra hidrojeni eşitle.",
                realLife: "Mutfaktaki tüp gazı da tam yanınca mavi alev verir; sarı ve isli alev hava yetmediğini gösterir.",
                question: null, options: null, correct: 0, replies: null,
                discoveryId: "K-3",
                discovery: "Dengesizliği bulmak için her elementi iki kefede ayrı say. Denkleştirirken önce tek yerde geçen elementler (C, H), oksijen en son.");
            SetOrder(d3, OrderMode.Exact, false, new[] { "C3H8", "O2" }, Step("R_propane", 1));

            var d4 = Order("D-4", "Yıkık Köprü", "Duvarcı Halil",
                request: "Harç için sönmüş kireç lazım. Kireç taşı var ama. Ustan taştan kireç çıkarırdı, iki adımda. Acele etme.",
                reminder: "Önce taştan kireç, sonra kireçten harç. İki adım, çırak.",
                deliver: "Tam kıvamında. Çırağı ustasını aratmıyor.",
                after: "Köprü taş gibi oldu. Artık arabalar da geçer.",
                hint: "Her denklem katsayı istemez. Saymadan kavanoz ekleme.",
                realLife: "Kireç söndürülürken çok ısı çıkar ve kireç cilde, göze zarar verir; ustalar eldiven ve gözlük takar.",
                question: "Kireç taşının içindeki karbon nereye gitti?",
                options: new[]
                {
                    "CO₂ olarak havaya karıştı; atomları kaybolmadı, başka bir maddede.",
                    "Isıyla yanıp yok oldu.",
                    "CaO'nun içinde saklı duruyor."
                },
                correct: 0,
                replies: new[]
                {
                    "Aynen öyle. Ne girerse o çıkar: karbon taştan havaya geçti.",
                    "Hiçbir atom yok olmaz, çırak. Karbon CO₂'nin içinde havaya karıştı.",
                    "CaO'ya bak: içinde Ca ve O var, karbon yok. Karbon CO₂ ile çıktı."
                },
                discoveryId: "K-4",
                discovery: "Bir tepkimenin ürünü başka bir tepkimenin gireni olabilir (ara ürün). Kireç: CaCO₃ → CaO → Ca(OH)₂.");
            SetOrder(d4, OrderMode.Exact, false, new[] { "CaCO3", "H2O" },
                Step("R_lime1", 1, "CaO"), Step("R_lime2", 1));

            var d5 = Order("D-5", "Gübre Ruhu", "Çiftçi Elif",
                request: "Dört kavanoz gübre ruhu istiyorum, ne eksik ne fazla. Parasını peşin veriyorum: kesede yirmi akçe. Artanın hesabı senin cebinden.",
                reminder: "Dört kavanoz, çırak. Sandıktan almadan önce hesabını yap.",
                deliver: "Dört kavanoz, tastamam. Hesabını bilen simyacı!",
                after: "Tarla yeşerdi! Hasatta seni de çağıracağım.",
                hint: "Önce bir kavanozluk denklemi kur, sonra ikiye katla.",
                realLife: "Amonyak en çok üretilen gübre hammaddelerindendir; keskin kokulu, zararlı bir gazdır ve kapalı fabrikalarda üretilir.",
                question: "Fazladan aldığın N₂ olsaydı neden kazanda kalırdı?",
                options: new[]
                {
                    "H₂ biterdi; N₂ eşleşecek hidrojen bulamaz, tepkime dururdu.",
                    "Kazan dolar, sığmazdı.",
                    "N₂ ağır olduğu için dibe çöker, karışmazdı."
                },
                correct: 0,
                replies: new[]
                {
                    "Aynen öyle. Biri biterse tepkime durur, fazla olan artar.",
                    "Hacim değil, sayı meselesi. Hidrojen biterse azot eşsiz kalır.",
                    "Karışmak tepkimeye girmek demek değil. Azot eşleşecek hidrojen bulamadığı için artar."
                },
                discoveryId: "K-5",
                discovery: "Tepkime girenleri katsayıları oranında harcar. Biri biterse tepkime durur; fazla olan artar.");
            SetOrder(d5, OrderMode.Purchase, false, new string[0], Step("R_ammonia", 2));
            SetPurchase(d5, 20, ("N2", 3), ("H2", 2));

            var hood = LoadOrCreate<NeighborhoodData>(NeighborhoodPath);
            var so = new SerializedObject(hood);
            so.FindProperty("id").stringValue = "denklestirme";
            so.FindProperty("displayName").stringValue = "Denkleştirme Köyü";
            so.FindProperty("masterIntro").stringValue =
                "Bu köyde herkesin derdi bir 'kaç tane' sorusu. Kazana ne girerse o çıkar; ne bir eksik, ne bir fazla. " +
                "Kavanozlar mühürlü, içlerini değiştiremezsin. Sen yalnızca kaç tane koyacağını seçersin.";
            var list = so.FindProperty("tasks");
            list.ClearArray();
            var all = new TaskData[] { d1, d2, d3, d4, d5 };
            for (int i = 0; i < all.Length; i++)
            {
                list.InsertArrayElementAtIndex(i);
                list.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            Debug.Log("[Denkleştirme] 5 sipariş + köy + ortak metinler üretildi.");
        }

        // ───────────── yardımcılar ─────────────

        private struct StepDef
        {
            public string Reaction;
            public int Multiplier;
            public string[] Drops;
        }

        private static StepDef Step(string reaction, int multiplier, params string[] drops) =>
            new StepDef { Reaction = reaction, Multiplier = multiplier, Drops = drops };

        private static OrderTask Order(string id, string title, string resident, string request, string reminder, string deliver,
            string after, string hint, string realLife, string question, string[] options, int correct, string[] replies,
            string discoveryId, string discovery)
        {
            var task = LoadOrCreate<OrderTask>($"{TaskFolder}/Order_{id}.asset");
            var so = new SerializedObject(task);
            so.FindProperty("taskId").stringValue = id;
            so.FindProperty("title").stringValue = title;
            so.FindProperty("residentName").stringValue = resident;
            so.FindProperty("requestLine").stringValue = Rich(request);
            so.FindProperty("reminderLine").stringValue = Rich(reminder);
            so.FindProperty("deliverLine").stringValue = Rich(deliver);
            so.FindProperty("afterLine").stringValue = Rich(after);
            so.FindProperty("hint").stringValue = Rich(hint);
            so.FindProperty("realLife").stringValue = Rich(realLife);
            so.FindProperty("discoveryId").stringValue = discoveryId;
            so.FindProperty("discoveryText").stringValue = Rich(discovery);

            var r = so.FindProperty("reasoning");
            r.FindPropertyRelative("question").stringValue = Rich(question ?? string.Empty);
            r.FindPropertyRelative("correctIndex").intValue = correct;
            SetStrings(r.FindPropertyRelative("options"), options);
            SetStrings(r.FindPropertyRelative("replies"), replies);
            so.ApplyModifiedPropertiesWithoutUndo();
            return task;
        }

        private static void SetOrder(OrderTask task, OrderMode mode, bool showReactants, string[] shelf, params StepDef[] steps)
        {
            var so = new SerializedObject(task);
            so.FindProperty("mode").enumValueIndex = (int)mode;
            so.FindProperty("showReactantsOnCard").boolValue = showReactants;

            var shelfProp = so.FindProperty("shelf");
            shelfProp.ClearArray();
            for (int i = 0; i < shelf.Length; i++)
            {
                shelfProp.InsertArrayElementAtIndex(i);
                shelfProp.GetArrayElementAtIndex(i).objectReferenceValue = Species(shelf[i]);
            }

            var stepsProp = so.FindProperty("steps");
            stepsProp.ClearArray();
            for (int i = 0; i < steps.Length; i++)
            {
                stepsProp.InsertArrayElementAtIndex(i);
                var s = stepsProp.GetArrayElementAtIndex(i);
                s.FindPropertyRelative("reaction").objectReferenceValue = Reaction(steps[i].Reaction);
                s.FindPropertyRelative("multiplier").intValue = steps[i].Multiplier;
                var drops = s.FindPropertyRelative("dropToShelf");
                drops.ClearArray();
                for (int d = 0; d < steps[i].Drops.Length; d++)
                {
                    drops.InsertArrayElementAtIndex(d);
                    drops.GetArrayElementAtIndex(d).objectReferenceValue = Species(steps[i].Drops[d]);
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetPurchase(OrderTask task, int budget, params (string formula, int price)[] prices)
        {
            var so = new SerializedObject(task);
            so.FindProperty("budget").intValue = budget;
            var p = so.FindProperty("prices");
            p.ClearArray();
            for (int i = 0; i < prices.Length; i++)
            {
                p.InsertArrayElementAtIndex(i);
                var e = p.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("species").objectReferenceValue = Species(prices[i].formula);
                e.FindPropertyRelative("price").intValue = prices[i].price;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetStrings(SerializedProperty array, string[] values)
        {
            array.ClearArray();
            if (values == null) return;
            for (int i = 0; i < values.Length; i++)
            {
                array.InsertArrayElementAtIndex(i);
                array.GetArrayElementAtIndex(i).stringValue = Rich(values[i]);
            }
        }

        /// <summary>Unicode alt indisleri (₀–₉) TMP &lt;sub&gt; etiketine çevirir: oyun fontunda bu glifler yok.</summary>
        private static string Rich(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new System.Text.StringBuilder(text.Length + 16);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c >= '₀' && c <= '₉')
                {
                    sb.Append("<sub>");
                    while (i < text.Length && text[i] >= '₀' && text[i] <= '₉')
                        sb.Append((char)('0' + (text[i++] - '₀')));
                    i--;
                    sb.Append("</sub>");
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        public static SpeciesData Species(string formula)
        {
            var s = AssetDatabase.LoadAssetAtPath<SpeciesData>($"Assets/_Project/Data/Species/Species_{formula}.asset");
            if (s == null) Debug.LogError("Tür bulunamadı: " + formula);
            return s;
        }

        private static ReactionData Reaction(string id)
        {
            var r = AssetDatabase.LoadAssetAtPath<ReactionData>($"Assets/_Project/Data/Reactions/{id}.asset");
            if (r == null) Debug.LogError("Tepkime bulunamadı: " + id);
            return r;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
