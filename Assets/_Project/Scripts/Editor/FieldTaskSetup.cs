using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.City;
using BilgiAdasi.Core;
using BilgiAdasi.Order;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Sv2 Tohum Nefes Alır (Bereket Tarlası, Matematik + Biyoloji; 15-bereket-tarlasi-mat-biyo §3.2):
    /// 1) Veri: tohum kesesi (alet gibi envanter maddesi), bereket_2 görevi (metinler, saha soruları), pazar satırı
    ///    (denk_1'den sonra açılır, görev bitince kalkar), ekonomi kataloğu, Denkleştirme köyünde 2. sıra.
    /// 2) Açık sahne (Harita): tarlaya FieldPlots, ekim sandığı (SeedBox), FieldTaskDirector; runner'a bağlar.
    /// Ders notu, sorgu ve defter LearningDataBuilder/LessonNotesData'da (bu menü onu da çalıştırır).
    /// Tekrar çalıştırılabilir; sahneyi kaydetmez.
    /// </summary>
    public static class FieldTaskSetup
    {
        private const string TaskFolder = "Assets/_Project/Data/Tasks/BereketTarlasi";
        private const string TaskPath = TaskFolder + "/bereket_2.asset";
        private const string SeedPath = "Assets/_Project/Data/Tools/Tool_Tohum_Kesesi.asset";
        private const string MarketPath = "Assets/_Project/Data/Market/Pazar.asset";
        private const string EconomyPath = "Assets/_Project/Data/Economy.asset";
        private const string NeighborhoodPath = "Assets/_Project/Data/Neighborhoods/Neighborhood_denklestirme.asset";
        private const string Denk1Path = "Assets/_Project/Data/Tasks/Denklestirme/denk_1.asset";
        private const string Denk5Path = "Assets/_Project/Data/Tasks/Denklestirme/denk_5.asset";
        private const string SeedPrefabPath = "Assets/_Project/Art/Prefablar/Esya_Tohum_Kesesi.prefab";
        private const string BoxModelPath = "Assets/_Project/Art/Tarla/Ekim_Sandigi.fbx";
        private const string SeedIconPath = "Assets/_Project/UI/Ikonlar/Esya_Tohum_Kesesi.png";
        private const string ThemePath = "Assets/_Project/Resources/UiTheme.asset";

        // Ekim sandığı: tarla kapısının DIŞINDA, solda (kapı açıklığı x −62.5…−58, z ≈ −3.7). İçeride traktörün yolunu
        // kesiyordu (14.11). Açık yüzü (kepçe) yola, kapağı çite bakar.
        private static readonly Vector3 BoxPosition = new Vector3(-64.8f, 0f, -2.0f);
        private const float BoxYaw = 0f;

        [MenuItem("BilgiAdasi/Kurulum/24 - Tarla: Tohum Nefes Alır (Sv2)", priority = 41)]
        public static void Run()
        {
            var seed = BuildSeedBag();
            var task = BuildTask(seed);
            AddToMarket(seed, task);
            AddToEconomy(seed);
            InsertIntoNeighborhood(task);
            LearningDataBuilder.Build();
            AssetDatabase.SaveAssets();
            SetupScene(task);
            Debug.Log("[BilgiAdasi] Tohum Nefes Alır (bereket_2) Sv2 olarak kuruldu: Çıraklık → denk_1 → bereket_2 → denk_2 … Sahne kaydedilmedi.");
        }

        // ---------- Veri ----------

        private static SpeciesData BuildSeedBag()
        {
            var s = AssetDatabase.LoadAssetAtPath<SpeciesData>(SeedPath);
            if (s == null)
            {
                s = ScriptableObject.CreateInstance<SpeciesData>();
                AssetDatabase.CreateAsset(s, SeedPath);
            }
            var so = new SerializedObject(s);
            so.FindProperty("formula").stringValue = "Tohum";
            so.FindProperty("displayName").stringValue = "Tohum kesesi (10 tohum)";
            so.FindProperty("composition").ClearArray();
            so.FindProperty("jarColor").colorValue = new Color(0.80f, 0.66f, 0.38f);
            so.FindProperty("jarPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(SeedPrefabPath);
            so.FindProperty("price").intValue = 2;
            so.FindProperty("isTool").boolValue = true;
            so.FindProperty("iconName").stringValue = "Esya_Tohum_Kesesi";
            so.ApplyModifiedPropertiesWithoutUndo();

            var icon = MakeSprite(SeedIconPath);
            var theme = AssetDatabase.LoadAssetAtPath<UI.UiTheme>(ThemePath);
            if (icon != null && theme != null)
            {
                var tso = new SerializedObject(theme);
                var icons = tso.FindProperty("speciesIcons");
                bool has = false;
                for (int i = 0; i < icons.arraySize; i++) if (icons.GetArrayElementAtIndex(i).objectReferenceValue == icon) has = true;
                if (!has)
                {
                    icons.InsertArrayElementAtIndex(icons.arraySize);
                    icons.GetArrayElementAtIndex(icons.arraySize - 1).objectReferenceValue = icon;
                    tso.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            return s;
        }

        private static FieldTask BuildTask(SpeciesData seed)
        {
            if (!AssetDatabase.IsValidFolder(TaskFolder)) AssetDatabase.CreateFolder("Assets/_Project/Data/Tasks", "BereketTarlasi");
            var task = AssetDatabase.LoadAssetAtPath<FieldTask>(TaskPath);
            if (task == null)
            {
                task = ScriptableObject.CreateInstance<FieldTask>();
                AssetDatabase.CreateAsset(task, TaskPath);
            }
            const string hesap = "Hesap Ustası";
            const string hayat = "Hayat Ustası";
            var questions = new List<FieldQuestion>
            {
                new FieldQuestion(FieldQuestionStage.Plan, hesap,
                    "Tohumun çimlenme olasılığı 0,8. Elif 120 filiz istiyor. En az kaç tohum ekilmeli?",
                    new[] { "96", "150", "120" }, 1,
                    new[]
                    {
                        "96, 120'nin yüzde 80'i. Ama 120 filiz istiyoruz: filizi olasılığa böl. 120 ÷ 0,8 = 150.",
                        "Doğru: 120 ÷ 0,8 = 150. Kontrol et: 150 × 0,8 = 120.",
                        "120 tohumun hepsi çimlenmez; yaklaşık 96'sı filiz verir. 150 tohum gerekir."
                    }),
                new FieldQuestion(FieldQuestionStage.Plan, hesap,
                    "Tohum 10'luk keselerde satılıyor. 150 tohum için kaç kese alınır?",
                    new[] { "12", "16", "15" }, 2,
                    new[]
                    {
                        "12 kese 120 tohum eder; onun da yalnızca 96'sı filiz verir. 15 kese gerekir.",
                        "16 kese 160 tohum eder; bir kese fazla. Tam bölünüyorsa yukarı yuvarlamaya gerek yok: 15.",
                        "Doğru: 150 ÷ 10 = 15 kese."
                    }),
                new FieldQuestion(FieldQuestionStage.AfterSowing, hayat,
                    "Toprağın altındaki tohum, çimlenirken gereken enerjiyi nasıl elde eder?",
                    new[] { "Depoladığı besini oksijenle kullanarak (oksijenli solunum)", "Topraktan hazır enerji emerek", "Güneş ışığıyla fotosentez yaparak" }, 0,
                    new[]
                    {
                        "Aynen öyle: besin + O₂ → CO₂ + H₂O + enerji. Hücrede adım adım olur, alev çıkmaz.",
                        "Toprak su ve mineral verir, enerji vermez. Enerji tohumun kendi besininden, oksijenle çıkar.",
                        "Toprağın altında yaprak yok, ışık yok. Fotosentez filiz yeşerince başlar; o zamana dek tohum besinini solunumla kullanır."
                    }),
                new FieldQuestion(FieldQuestionStage.BeforeSowing, hayat,
                    "Toprağı sürmeden ekseydik ne olurdu?",
                    new[] { "Hiç fark etmezdi", "Sıkışık toprakta hava az olur, daha az tohum çimlenirdi", "Tohumlar daha hızlı çimlenirdi" }, 1,
                    new[]
                    {
                        "Fark ederdi: tohum nefes alır. Sıkışık toprakta oksijen az, çimlenme düşük olur.",
                        "Doğru. Sürmek toprağı gevşetir; arasına giren hava tohumun solunumu için oksijen taşır.",
                        "Tersine: oksijen azalınca solunum, solunum azalınca çimlenme yavaşlar."
                    }),
            };

            task.EditorSetCommon("bereket_2", "Çiftçi Elif",
                new[]
                {
                    "Tarlayı sürdüm, toprak havalandı. Bu yıl 120 öbek filiz isterim.",
                    "Tohum pahalı: fazla alırsam kesem boşalır, az alırsam tarla boş kalır. Tohum parasının bir kısmı benden; tohumu Element ya da Alet Pazarı'nda bulursun."
                },
                "Ders notundaki kuralı kullan: tohum = filiz ÷ olasılık. Sonra 10'luk keselere böl.",
                "Çiftçiler ekimden önce tohumun bir kısmını ıslak bezde çimlendirip oranını ölçer (çimlenme testi); kaç tohum alacaklarını buna göre hesaplar.");
            task.EditorSet(seed, 10, 0.8f, 120, 20, "Surulmus", "Ekili",
                new[]
                {
                    "Çırak, Elif'ten haber var: tarlada ekim zamanı gelmiş.",
                    "Tarla benim kazanıma sığmaz. Kilitli şehirlerden iki dostum, Hesap Ustası ile Hayat Ustası, tarlada.",
                },
                "{0} kese, {1} tohum eder. Bunun yaklaşık {2} tanesi filiz verir; 120'ye yetmez. Pazardan biraz daha getir.",
                "Tam yetecek kadar! Bu tohumların yaklaşık 120'si filiz verir. Ustalar sana bir şey soracakmış.",
                "Tarla doldu ama {0} kese fazla geldi; o tohumlar boşa gitti. Neyse, ustalar sana bir şey soracakmış.",
                "Elinde tohum kesesi yok. Element ya da Alet Pazarı'ndan tohum al, keseyi elinde tutup sandığa dök.",
                new[] { "Ekim tamam! Toprak havalı, tohum nefes alacak.", "Filizler çıkınca seni yine çağırırım, çırak." },
                questions, "Bereket Tarlası", "120 filiz",
                new[] { "Pazardan tohum kesesi al", "Tarladaki ekim sandığına dök", "Traktörle tarlada 2 tur at", "Ustaların sorularını yanıtla" },
                "Tohumları sandığa dök");
            task.EditorSetTalk(new[] { "Çiftçi Elif", "Hesap Ustası", "Hayat Ustası" }, "{0} ile konuş",
                new[]
                {
                    "Toprak dinleniyor. Ekim zamanı gelince ustana haber yollarım.",
                    "Ustanın notunu okudun mu? 120 öbek filiz isterim; tohumun hepsi çimlenmez, ona göre hesapla.",
                    "Tohum keseleri Element ya da Alet Pazarı'nda. Aldığın keseleri elinde tutup şu sandığa dök.",
                    "Ekici traktörün arkasında. Tarlada iki tur at; tohum ekicinin geçtiği yere düşer.",
                    "Tohumlar toprakta. Hayat Ustası'nın sorusuna kulak ver.",
                    "Filizler çıkınca seni yine çağırırım, çırak. Bu yıl tarla boş kalmayacak."
                },
                new[]
                {
                    "Sayının da sırası var, çırak. Önce kazandaki işini bitir.",
                    "Beklenen sayı = olasılık × tohum. Bize filiz sayısı verildi; tersinden düşün.",
                    "Bir kese 10 tohum alır. Bulduğun tohum sayısını 10'a böl; artan kalırsa bir kese daha.",
                    "Tarlanın çevresini say: merkezin etrafında bir tam dönüş, bir tur. İki tur at, ekici gerisini halleder.",
                    "Hesap tuttu mu, tarla söyleyecek. Filizler çıkınca birlikte sayarız.",
                    "150 tohumun yaklaşık 120'si filiz verir. Yaklaşık dedim: olasılık kesinlik vermez."
                },
                new[]
                {
                    "Toprak da canlıdır, çırak. Sırası gelince birlikte bakarız.",
                    "Tohum da nefes alır. Ustanın notundaki solunum denklemine bak.",
                    "Bu toprak sürüldü; arasında hava var. Tohum o havadaki oksijeni kullanacak.",
                    "Ekim bitince sana bir şey soracağım: toprağın altında ışık yokken tohum enerjisini nereden alır?",
                    "Şimdi sor kendine: ışık görmeyen tohum nasıl büyür?",
                    "Tohum depoladığı besini oksijenle kullanır; filiz yeşerince fotosentez başlar."
                });
            task.EditorSetGuide(
                new[]
                {
                    "Ustaların hesabını yanıtla",
                    "Pazardan tohum kesesi al",
                    "Ekim sandığına dök",
                    "Traktörle tarlada 2 tur at",
                    "Hayat Ustası'nın sorusunu yanıtla"
                },
                new[]
                {
                    "Kaç tohum, kaç kese? Ders notundaki kural: tohum = filiz ÷ olasılık.",
                    "Element ya da Alet Pazarı'nda en başta: Tohum kesesi (10 tohum). Hesapladığın kadar al.",
                    "Tarla kapısının solunda. Döktüğün kese: {0}",
                    "Ekici traktörün arkasında; geçtiğin parsel ekilir. Tur {0} / {1}",
                    "Tohum toprakta: nasıl nefes alacak?"
                });
            task.EditorSetDriving(2,
                new[]
                {
                    "Tohumları ekiciye doldurdum, ekiciyi de traktörün arkasına taktım.",
                    "Traktöre bin, tarlada iki tur at. Ekici geçtiği yere tohumu kendi bırakır."
                },
                "Ekim: {0}. tur tamam ({0} / {1})",
                "İki tur tamam! Kalan köşeleri ekici bitiriyor, izle.");

            // Elif'in portresi denk_5'ten (aynı kişi).
            var denk5 = AssetDatabase.LoadAssetAtPath<TaskData>(Denk5Path);
            var so = new SerializedObject(task);
            if (denk5 != null) so.FindProperty("giverPortrait").objectReferenceValue = denk5.GiverPortrait;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(task);
            return task;
        }

        private static void AddToMarket(SpeciesData seed, FieldTask task)
        {
            // Hem Element Pazarı'nda hem Alet Pazarı'nda (oyuncular tohumu alet/eşya pazarında arıyor, Play 14.11).
            AddToCatalog(MarketPath, seed, task);
            AddToCatalog("Assets/_Project/Data/Market/Pazar_Aletler.asset", seed, task);
        }

        private static void AddToCatalog(string path, SpeciesData seed, FieldTask task)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<MarketCatalog>(path);
            if (catalog == null) { Debug.LogWarning("[Tarla] Pazar kataloğu yok: " + path); return; }
            catalog.AddIfMissing(seed);
            MoveToTop(catalog, seed); // 4 sütunlu pazar ekranında en sonda kalıyordu, kaydırmadan görünmüyordu (14.11)
            var so = new SerializedObject(catalog);
            var entries = so.FindProperty("entries");
            for (int i = 0; i < entries.arraySize; i++)
            {
                var e = entries.GetArrayElementAtIndex(i);
                if (e.FindPropertyRelative("species").objectReferenceValue != seed) continue;
                e.FindPropertyRelative("unlimitedStock").boolValue = true;
                e.FindPropertyRelative("availableAfter").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TaskData>(Denk1Path);
                e.FindPropertyRelative("removedAfter").objectReferenceValue = task;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void AddToEconomy(SpeciesData seed)
        {
            var economy = AssetDatabase.LoadAssetAtPath<EconomyData>(EconomyPath);
            if (economy == null) return;
            var so = new SerializedObject(economy);
            var list = so.FindProperty("catalog");
            for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == seed) return;
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = seed;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Denkleştirme köyünde 2. sıraya (denk_1'den sonra) koyar: seviye sırası Sv0 Çıraklık, Sv1 denk_1, Sv2 bereket_2.</summary>
        private static void InsertIntoNeighborhood(FieldTask task)
        {
            var n = AssetDatabase.LoadAssetAtPath<NeighborhoodData>(NeighborhoodPath);
            if (n == null) return;
            var so = new SerializedObject(n);
            var tasks = so.FindProperty("tasks");
            for (int i = 0; i < tasks.arraySize; i++)
                if (tasks.GetArrayElementAtIndex(i).objectReferenceValue == task)
                {
                    if (i == 1) return;
                    tasks.DeleteArrayElementAtIndex(i);
                    break;
                }
            int at = Mathf.Min(1, tasks.arraySize);
            tasks.InsertArrayElementAtIndex(at);
            tasks.GetArrayElementAtIndex(at).objectReferenceValue = task;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(n);
        }

        // ---------- Sahne ----------

        private static void SetupScene(FieldTask task)
        {
            var runner = Object.FindAnyObjectByType<NeighborhoodRunner>();
            var field = GameObject.Find("Bereket_Tarlasi");
            if (runner == null || field == null)
            {
                Debug.LogWarning("[Tarla] Harita sahnesi açık değil (NeighborhoodRunner / Bereket_Tarlasi yok); yalnızca veri kuruldu.");
                return;
            }

            var plots = field.GetComponent<FieldPlots>();
            if (plots == null) plots = Undo.AddComponent<FieldPlots>(field);
            var pso = new SerializedObject(plots);
            pso.FindProperty("seedMaterial").objectReferenceValue = ParticleMaterial("Tohum_Parcacik", false);
            pso.FindProperty("dustMaterial").objectReferenceValue = ParticleMaterial("Toz_Parcacik", true);
            pso.FindProperty("sowStep").floatValue = 0.36f; // 24 parsel ≈ 9 sn: oyuncu ekimi izleyebilsin
            pso.ApplyModifiedPropertiesWithoutUndo();

            // Ekim sandığı
            var oldBox = GameObject.Find("Ekim_Sandigi");
            if (oldBox != null) Undo.DestroyObjectImmediate(oldBox);
            var box = new GameObject("Ekim_Sandigi") { layer = ProjectSetup.InteractableLayer };
            Undo.RegisterCreatedObjectUndo(box, "Ekim sandığı");
            box.transform.SetParent(field.transform.parent, true);
            Vector3 p = BoxPosition;
            if (Physics.Raycast(p + Vector3.up * 5f, Vector3.down, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore)) p.y = hit.point.y;
            box.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, BoxYaw, 0f));

            Transform heap;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(BoxModelPath);
            if (model != null)
            {
                var m = (GameObject)PrefabUtility.InstantiatePrefab(model, box.transform);
                m.name = "Model";
                heap = FindDeep(m.transform, "Ekim_Sandigi_Tohum");
            }
            else heap = BuildPlaceholderBox(box.transform);

            var col = box.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.35f, 0f);
            col.size = new Vector3(1.3f, 0.7f, 0.8f);

            var director = Object.FindAnyObjectByType<FieldTaskDirector>();
            if (director == null)
            {
                var go = new GameObject("Tarla_Gorevleri");
                Undo.RegisterCreatedObjectUndo(go, "Tarla görevleri");
                go.transform.SetParent(runner.transform.parent, false);
                director = go.AddComponent<FieldTaskDirector>();
            }

            var seedBox = box.AddComponent<SeedBox>();
            var sb = new SerializedObject(seedBox);
            sb.FindProperty("director").objectReferenceValue = director;
            sb.FindProperty("seedHeap").objectReferenceValue = heap;
            sb.ApplyModifiedPropertiesWithoutUndo();

            var rso = new SerializedObject(runner);
            var texts = rso.FindProperty("texts").objectReferenceValue as NeighborhoodTexts;
            var dso = new SerializedObject(director);
            dso.FindProperty("dialogue").objectReferenceValue = runner.GetComponent<DialogueDirector>();
            dso.FindProperty("seedBox").objectReferenceValue = seedBox;
            dso.FindProperty("plots").objectReferenceValue = plots;
            var known = dso.FindProperty("knownTasks");
            known.ClearArray();
            known.InsertArrayElementAtIndex(0);
            known.GetArrayElementAtIndex(0).objectReferenceValue = task;
            // Traktörle ekim: sahnedeki sürülebilir traktör (Kurulum 23) ve ahır önündeki ekici.
            var tractor = Object.FindAnyObjectByType<Player.Tractor>();
            dso.FindProperty("tractor").objectReferenceValue = tractor;
            Transform seeder = null;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
                if (t.name == "Alet_Ekici" && t.GetComponent<MeshRenderer>() != null) { seeder = t; break; }
            dso.FindProperty("seeder").objectReferenceValue = seeder;
            if (tractor == null) Debug.LogWarning("[Tarla] Sürülebilir traktör yok (Kurulum → 23); tarla traktörsüz ekilecek.");
            if (texts != null)
            {
                dso.FindProperty("masterName").stringValue = texts.MasterName;
                dso.FindProperty("masterPortrait").objectReferenceValue = texts.MasterPortrait;
            }
            dso.ApplyModifiedPropertiesWithoutUndo();

            rso.FindProperty("fieldTasks").objectReferenceValue = director;
            rso.ApplyModifiedPropertiesWithoutUndo();

            // Tarlada bekleyenler: Elif (köylü kadın modeli) ve Hesap Ustası (Hesap Hocası modeli), sandığın yanında.
            // Gezinme (NpcAgent) ve fizik kaldırılır; yerinde durur, Animator varsayılan (bekleme) pozunda.
            PlaceNpc("NPC_KoyluKadin", "Tarla_Elif", new Vector3(-66.6f, 0f, -2.4f), box.transform.position, FieldNpcRole.Giver, director);
            PlaceNpc("NPC_HesapHocasi", "Tarla_HesapUstasi", new Vector3(-67.6f, 0f, -1.0f), box.transform.position, FieldNpcRole.Math, director);
            // Hayat Ustası'nın kendi modeli yok: köylü erkek modeli (yer tutucu; Mustafa'nın modeli gelince değişir).
            PlaceNpc("NPC_KoyluErkek", "Tarla_HayatUstasi", new Vector3(-68.4f, 0f, -2.6f), box.transform.position, FieldNpcRole.Biology, director);
            SetPortraits();

            EditorSceneManager.MarkSceneDirty(runner.gameObject.scene);
            Selection.activeGameObject = box;
        }

        /// <summary>Emin'in diyalog çerçevesi portreyi konuşanın adından bulur (PortraitBook): iki usta eklenir.</summary>
        private static void SetPortraits()
        {
            var book = AssetDatabase.LoadAssetAtPath<PortraitBook>("Assets/_Project/Data/Resources/PortraitBook.asset");
            if (book == null) return;
            var hesap = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Portreler/Portre_HesapHocasi.png");
            var hayat = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Portreler/Portre_KoyluErkek.png");
            if (hesap != null) book.Set("Hesap Ustası", hesap);
            if (hayat != null) book.Set("Hayat Ustası", hayat);
            EditorUtility.SetDirty(book);
        }

        private static void MoveToTop(MarketCatalog catalog, SpeciesData seed)
        {
            var so = new SerializedObject(catalog);
            var entries = so.FindProperty("entries");
            for (int i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("species").objectReferenceValue == seed)
                {
                    if (i > 0) entries.MoveArrayElement(i, 0);
                    break;
                }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PlaceNpc(string sourceName, string newName, Vector3 position, Vector3 lookAt, FieldNpcRole role, FieldTaskDirector director)
        {
            var old = GameObject.Find(newName);
            if (old != null) Undo.DestroyObjectImmediate(old);
            GameObject source = null;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
                if (t.name == sourceName && t.parent != null && t.parent.name == "NPC_Sanat") { source = t.gameObject; break; }
            if (source == null) { Debug.LogWarning("[Tarla] NPC modeli yok: " + sourceName); return; }
            var npc = Object.Instantiate(source, source.transform.parent);
            npc.name = newName;
            Undo.RegisterCreatedObjectUndo(npc, "Tarla NPC");
            foreach (var mb in npc.GetComponentsInChildren<MonoBehaviour>(true))
                if (mb.GetType().Name == "NpcAgent") Object.DestroyImmediate(mb);
            foreach (var rb in npc.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
            if (Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore))
                position.y = hit.point.y;
            Vector3 dir = lookAt - position;
            dir.y = 0f;
            npc.transform.SetPositionAndRotation(position, dir.sqrMagnitude > 0.01f ? Quaternion.LookRotation(dir) : Quaternion.identity);
            // Konuşulabilir: Interact (E / X) → aşamaya göre ipucu (FieldNpc). Bakma ışını için Interactable katmanı + collider.
            int layer = ProjectSetup.InteractableLayer;
            npc.layer = layer;
            if (npc.GetComponent<Collider>() == null)
            {
                var cap = npc.AddComponent<CapsuleCollider>();
                cap.center = new Vector3(0f, 0.9f, 0f);
                cap.height = 1.8f;
                cap.radius = 0.35f;
            }
            foreach (var c in npc.GetComponentsInChildren<Collider>(true)) c.gameObject.layer = layer;
            var talk = npc.AddComponent<FieldNpc>();
            var so = new SerializedObject(talk);
            so.FindProperty("director").objectReferenceValue = director;
            so.FindProperty("role").enumValueIndex = (int)role;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Model yoksa yer tutucu sandık (_Placeholder): Mustafa'nın modeli gelince değişir.</summary>
        private static Transform BuildPlaceholderBox(Transform parent)
        {
            var ph = new GameObject("_Placeholder").transform;
            ph.SetParent(parent, false);
            Cube(ph, "Govde", new Vector3(0f, 0.3f, 0f), new Vector3(1.2f, 0.6f, 0.7f), new Color(0.55f, 0.38f, 0.22f));
            var heap = Cube(ph, "Ekim_Sandigi_Tohum", new Vector3(0f, 0.55f, 0f), new Vector3(1.1f, 0.12f, 0.6f), new Color(0.82f, 0.68f, 0.40f));
            return heap;
        }

        private static Transform Cube(Transform parent, string name, Vector3 pos, Vector3 size, Color color)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = name;
            Object.DestroyImmediate(c.GetComponent<Collider>());
            c.transform.SetParent(parent, false);
            c.transform.localPosition = pos;
            c.transform.localScale = size;
            var r = c.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            r.sharedMaterial = mat;
            return c.transform;
        }

        /// <summary>Ekim efekti malzemesi (URP Particles/Unlit; tohum opak, toz saydam). Renk parçacıktan gelir.</summary>
        private static Material ParticleMaterial(string name, bool transparent)
        {
            const string dir = "Assets/_Project/Art/VFX/Tarla";
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/VFX")) AssetDatabase.CreateFolder("Assets/_Project/Art", "VFX");
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/_Project/Art/VFX", "Tarla");
            string path = dir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) { Debug.LogWarning("[Tarla] URP Particles/Unlit bulunamadı; ekim efekti kapalı."); return null; }
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor", Color.white);
            if (transparent)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_ZWrite", 0f);
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                mat.SetFloat("_Surface", 0f);
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Transform FindDeep(Transform t, string name)
        {
            foreach (var c in t.GetComponentsInChildren<Transform>(true)) if (c.name == name) return c;
            return null;
        }

        private static Sprite MakeSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter ti &&
                (ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single))
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
