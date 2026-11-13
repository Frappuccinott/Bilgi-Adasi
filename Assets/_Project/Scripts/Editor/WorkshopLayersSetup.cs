using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.Order;
using BilgiAdasi.Player;
using BilgiAdasi.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Atölye ekleri (04-gorev-plani Sv 1–5 katmanları + elde taşıma):
    /// veri → Data/Resources/{PredictionData, ErrorHuntData, WorkshopTexts};
    /// sahne → oyuncuya ItemPlacer + HeldItemDisplay, kazanın üstüne Atom Görüşü, tahtaya Hata Dedektifi yazısı.
    /// Tekrar çalıştırmak güvenli (var olanı günceller). Metin düzeltmeleri burada yapılıp menü yeniden çalıştırılır.
    /// </summary>
    public static class WorkshopLayersSetup
    {
        private const string Data = "Assets/_Project/Data";
        private const string Resources = Data + "/Resources";
        private const string MatFolder = "Assets/_Project/Prefabs/Atolye";

        [MenuItem("BilgiAdasi/Kurulum/18 - Atölye ekleri: veri (tahmin, hata dedektifi, metinler)", priority = 18)]
        public static void BuildData()
        {
            if (!AssetDatabase.IsValidFolder(Resources)) AssetDatabase.CreateFolder(Data, "Resources");
            BuildTexts();
            BuildPredictions();
            BuildErrorHunt();
            AssetDatabase.SaveAssets();
            Debug.Log("[Atölye ekleri] Veri üretildi: PredictionData, ErrorHuntData, WorkshopTexts.");
        }

        [MenuItem("BilgiAdasi/Kurulum/18b - Atölye ekleri: açık sahneye bağla", priority = 19)]
        public static void BuildScene()
        {
            BuildData();
            var report = new List<string>();
            SetupPlayer(report);
            SetupAtomVision(report);
            SetupChalkboard(report);
            SetupCabinet(report);
            SetupCauldronFixes(report);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Atölye ekleri] Sahne:\n" + string.Join("\n", report));
        }

        // ---------- veri ----------

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            // Betiği kayıp (eski sürümde sınıf başka dosyadaydı) bir asset kaldıysa yeniden üretilir.
            if (AssetDatabase.LoadMainAssetAtPath(path) != null || System.IO.File.Exists(path)) AssetDatabase.DeleteAsset(path);
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        private static void BuildTexts()
        {
            var t = LoadOrCreate<WorkshopTexts>(Resources + "/" + WorkshopTexts.ResourcePath + ".asset");
            // Sayaç satırında element harfi zaten solda yazılı: yalnızca "sol / sağ".
            var so = new SerializedObject(t);
            so.FindProperty("huntCount").stringValue = "{1} / {2}";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SpeciesData Sp(string formula)
        {
            var s = AssetDatabase.LoadAssetAtPath<SpeciesData>($"{Data}/Species/Species_{formula}.asset");
            if (s == null) Debug.LogError("[Atölye ekleri] Tür yok: " + formula);
            return s;
        }

        private static SpeciesAmount A(int n, string formula) => new SpeciesAmount(Sp(formula), n);

        private static ReasoningPrompt Q(string question, int correct, string[] options, string[] replies) =>
            new ReasoningPrompt(question, options, correct, replies);

        private static void BuildPredictions()
        {
            var data = LoadOrCreate<PredictionData>(Resources + "/" + PredictionData.ResourcePath + ".asset");
            data.SetEntries(new List<PredictionEntry>
            {
                // Sv 1 — D-1 Kuru Çeşme (tahmin)
                new PredictionEntry("denk_1", 0, Q(
                    "Muhtar su istiyor. Kazana başlamadan söyle çırak: su yapmak için kazana ne koymalıyız?", 1,
                    new[] { "Yalnızca H₂", "H₂ ve O₂", "H₂ ve CO₂" },
                    new[]
                    {
                        "Suyun içinde oksijen de var: H₂O. Yalnız hidrojenle su olmaz.",
                        "Aynen öyle. Suyun içinde ne varsa kazana o girer: hidrojen ve oksijen.",
                        "CO₂ karbon getirir, suda karbon yok. Gereken oksijen O₂'den gelir."
                    })),

                // Sv 2 — D-2 İsli Fenerler (tahmin: yanma için ne gerekir)
                new PredictionEntry("denk_2", 0, Q(
                    "Fenerler isli yanıyor. Bir fener temiz yansın diye neler gerekir?", 2,
                    new[] { "Yalnızca CH₄ ve bir kıvılcım", "CH₄ ve H₂O", "CH₄ (yakıt), O₂ (hava) ve kıvılcım" },
                    new[]
                    {
                        "Havasız ateş söner, yarım yanarsa is çıkar. Yanma oksijen ister.",
                        "Su yangını söndürür. Yakıtın yanında hava ve bir kıvılcım gerekir.",
                        "Doğru. Yakıt, hava, ısı: üçü bir arada olunca fener temiz yanar."
                    })),

                // Sv 4 — D-4 Yıkık Köprü, 2. adım başlarken (ara ürün)
                new PredictionEntry("denk_4", 1, Q(
                    "Taşı ısıttın, kazandan sönmemiş kireç çıktı. Harç için su ile neyi birleştireceksin?", 0,
                    new[] { "CaO, az önce yaptığım sönmemiş kireç", "CaCO₃, kireç taşının kendisi", "CO₂, taştan çıkan gaz" },
                    new[]
                    {
                        "Evet. Birinci adımın ürünü ikinci adımın malzemesi olur: buna ara ürün denir.",
                        "Taş zaten ısıtıldı, artık CaO oldu. Tahtadaki ilk satıra bak: ürün neydi?",
                        "CO₂ havaya karıştı. Harcı su ile CaO yapar."
                    })),

                // Sv 5 — D-5 Gübre Ruhu (tahmin: bütçe)
                new PredictionEntry("denk_5", 0, Q(
                    "Elif 4 NH₃ istiyor. Pazarda N₂ 3 akçe, H₂ 2 akçe. Kesende 30 akçe var. En az kaç akçe harcarsın?", 0,
                    new[] { "18 akçe", "9 akçe", "30 akçe, hepsini" },
                    new[]
                    {
                        "Doğru hesap. 2 N₂ + 6 H₂ = 6 + 12 = 18 akçe. Fazlası boşa gider.",
                        "9 akçe yalnızca 2 NH₃ eder: 1 N₂ + 3 H₂. Elif dört istedi, denklemi ikiyle çarp.",
                        "Kesenin hepsini harcamak gerekmez. Denklemin istediği kadarını al: fazlası artar."
                    })),
            });
            EditorUtility.SetDirty(data);
        }

        private static void BuildErrorHunt()
        {
            var data = LoadOrCreate<ErrorHuntData>(Resources + "/" + ErrorHuntData.ResourcePath + ".asset");
            data.SetEntries(new List<ErrorHuntEntry>
            {
                new ErrorHuntEntry("denk_3", new List<ErrorHuntRound>
                {
                    // Tur 1: dünkü fener denkleminin hatalı kopyası → H ve O dengesiz, C dengeli.
                    new ErrorHuntRound(
                        new List<SpeciesAmount> { A(1, "CH4"), A(1, "O2") },
                        new List<SpeciesAmount> { A(1, "CO2"), A(1, "H2O") },
                        Q("Bu kopyada ne yanlış gitmiş?", 1,
                            new[] { "Metanın formülü yanlış yazılmış.", "Suyun ve oksijenin katsayısı unutulmuş.", "Karbon fazla, bir karbon silinmeli." },
                            new[]
                            {
                                "Formüle dokunulmaz: CH₄ hep CH₄'tür. Değişen yalnızca önündeki sayı, katsayı.",
                                "Aynen öyle. 2 H₂O ve 2 O₂ yazılınca H 4/4, O 4/4 olur.",
                                "Karbon zaten iki tarafta birer tane. Atom silinmez; katsayıyla sayılar eşitlenir."
                            })),

                    // Tur 2: bugünün yakıtı, farklı örüntü → C ve O dengesiz, H dengeli.
                    new ErrorHuntRound(
                        new List<SpeciesAmount> { A(1, "C3H8"), A(5, "O2") },
                        new List<SpeciesAmount> { A(1, "CO2"), A(4, "H2O") },
                        Q("Bu denklemi nasıl düzeltirsin?", 0,
                            new[] { "CO₂'nin önüne 3 yazarım; karbonla birlikte oksijen de 10 olur.", "C₃H₈'i CH₈ yaparım, karbon tek kalır.", "O₂'nin katsayısını artırırım." },
                            new[]
                            {
                                "Doğru. 3 CO₂ → C 3/3; O 6 + 4 = 10, soldaki 5 O₂ ile eşit.",
                                "Formül değişirse madde değişir: o artık propan olmaz. Katsayıyla düzeltilir.",
                                "Solda oksijen zaten fazla (10'a 6). Eksik olan sağda: karbonla başla."
                            })),
                }),
            });
            EditorUtility.SetDirty(data);
        }

        // ---------- sahne ----------

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[Atölye ekleri] Alan yok: {target.GetType().Name}.{field}"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Object GetRef(Object target, string field)
        {
            var p = new SerializedObject(target).FindProperty(field);
            return p != null ? p.objectReferenceValue : null;
        }

        private static JarProp DefaultJar()
        {
            var shelf = Object.FindAnyObjectByType<Shelf>(FindObjectsInactive.Include);
            return shelf != null ? GetRef(shelf, "defaultJarPrefab") as JarProp : null;
        }

        private const string PlayerPrefab = "Assets/_Project/Prefabs/PlayerRig.prefab";

        /// <summary>Oyuncu sahnede değil, PlayerSpawn'ın prefab'ından gelir: prefab düzenlenir.</summary>
        private static void SetupPlayer(List<string> report)
        {
            var jar = DefaultJar();
            var markerMat = MarkerMaterial();
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                var interactor = root.GetComponentInChildren<PlayerInteractor>(true);
                if (interactor == null) { report.Add("! PlayerRig prefab'ında PlayerInteractor yok"); return; }
                var inventory = interactor.GetComponentInParent<PlayerInventory>();

                var placer = interactor.GetComponent<ItemPlacer>();
                if (placer == null) placer = interactor.gameObject.AddComponent<ItemPlacer>();
                Set(placer, "interactor", interactor);
                Set(placer, "inventory", inventory);
                Set(placer, "defaultJarPrefab", jar);
                Set(placer, "markerMaterial", markerMat);

                var rayOrigin = GetRef(interactor, "rayOrigin") as Transform;
                if (rayOrigin == null) { report.Add("! rayOrigin yok, el görünümü kurulmadı"); }
                else
                {
                    var t = rayOrigin.Find("ElEsyasi");
                    var heldGo = t != null ? t.gameObject : new GameObject("ElEsyasi");
                    heldGo.transform.SetParent(rayOrigin, false);
                    var held = heldGo.GetComponent<HeldItemDisplay>();
                    if (held == null) held = heldGo.AddComponent<HeldItemDisplay>();
                    Set(held, "inventory", inventory);
                    Set(held, "interactor", interactor);
                    Set(held, "cameraRoot", rayOrigin);
                    Set(held, "defaultJarPrefab", jar);
                }
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
                report.Add($"✓ PlayerRig prefab: ItemPlacer + HeldItemDisplay (kavanoz: {(jar != null ? jar.name : "YOK")})");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Material PlaqueMaterial()
        {
            string path = MatFolder + "/Mat_Terazi_Levha.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetColor("_BaseColor", Color.white);   // renk ScaleModel'den (plaqueColor, 09 palet kâğıt)
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static Material MarkerMaterial()
        {
            string path = MatFolder + "/Mat_BirakmaHalkasi.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            if (!AssetDatabase.IsValidFolder(MatFolder)) AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Atolye");
            m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            var c = new Color(0.247f, 0.659f, 0.227f, 1f); // 09 palet: yeşil yaprak orta #3FA83A
            m.SetColor("_BaseColor", c);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static void SetupAtomVision(List<string> report)
        {
            var controller = Object.FindAnyObjectByType<OrderController>(FindObjectsInactive.Include);
            var scale = Object.FindAnyObjectByType<ScaleModel>(FindObjectsInactive.Include);
            if (controller == null || scale == null) { report.Add("! Kazan ya da terazi yok, Atom Görüşü kurulmadı"); return; }
            var root = controller.transform.Find("AtomGorusu");
            if (root == null)
            {
                root = new GameObject("AtomGorusu").transform;
                root.SetParent(controller.transform, false);
            }
            root.localPosition = new Vector3(0f, 1.45f, 0f);
            var view = root.GetComponent<AtomVisionDisplay>();
            if (view == null) view = root.gameObject.AddComponent<AtomVisionDisplay>();

            // Atom görünümü terazidekiyle aynı: atomPrefab + elementShapes kopyalanır.
            var src = new SerializedObject(scale);
            var dst = new SerializedObject(view);
            dst.FindProperty("atomPrefab").objectReferenceValue = src.FindProperty("atomPrefab").objectReferenceValue;
            dst.CopyFromSerializedProperty(src.FindProperty("elementShapes"));
            dst.FindProperty("titleLabel").objectReferenceValue = Label(root, "Baslik", new Vector3(0f, 0.42f, 0f), 0.8f);
            dst.FindProperty("counterLabel").objectReferenceValue = Label(root, "Sayac", new Vector3(0f, 0.22f, 0f), 0.55f);
            // Ölçüler (bileşen önceden eklendiyse eski varsayılanlar sahnede kalmasın).
            dst.FindProperty("towardViewer").floatValue = 0.85f;
            dst.FindProperty("atomSize").floatValue = 0.085f;
            dst.FindProperty("bondLength").floatValue = 0.09f;
            dst.FindProperty("moleculeGap").floatValue = 0.26f;
            dst.FindProperty("sideOffset").floatValue = 0.5f;
            dst.ApplyModifiedPropertiesWithoutUndo();
            report.Add("✓ Atom Görüşü: kazanın 1.45 m üstünde");
        }

        private static TMP_Text Label(Transform parent, string name, Vector3 pos, float size)
        {
            var t = parent.Find(name);
            var go = t != null ? t.gameObject : new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.identity;
            var tmp = go.GetComponent<TextMeshPro>();
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.color = new Color(0.98f, 0.95f, 0.86f);
            tmp.outlineWidth = 0.18f;
            tmp.outlineColor = new Color32(40, 26, 14, 255);
            // Kök +Z'si kameradan uzağa bakar (LookRotation(dir)); TMP de +Z'ye bakan kameradan okunur: döndürme yok.
            tmp.rectTransform.sizeDelta = new Vector2(2.4f, 0.6f);
            return tmp;
        }

        private static void SetupChalkboard(List<string> report)
        {
            var board = Object.FindAnyObjectByType<ChalkboardText>(FindObjectsInactive.Include);
            if (board == null) { report.Add("! ChalkboardText yok, Hata Dedektifi yazısı kurulmadı"); return; }
            var boardTmp = GetRef(board, "text") as TMP_Text;
            if (boardTmp == null) { report.Add("! ChalkboardText.text boş"); return; }
            var parent = boardTmp.transform.parent;
            var root = parent.Find("HataDedektifi");
            if (root == null)
            {
                root = new GameObject("HataDedektifi", typeof(RectTransform)).transform;
                root.SetParent(parent, false);
            }
            var srcRt = boardTmp.rectTransform;
            var rt = (RectTransform)root;
            rt.localPosition = srcRt.localPosition;
            rt.localRotation = srcRt.localRotation;
            rt.localScale = srcRt.localScale;
            rt.sizeDelta = srcRt.sizeDelta;
            var size = srcRt.rect.size;

            var view = root.GetComponent<ChalkboardHuntView>();
            if (view == null) view = root.gameObject.AddComponent<ChalkboardHuntView>();
            var so = new SerializedObject(view);
            so.FindProperty("title").objectReferenceValue = BoardText(root, "Baslik", boardTmp, size, 0.40f, 0.14f, 1.5f, TextAlignmentOptions.Top);
            so.FindProperty("body").objectReferenceValue = BoardText(root, "Govde", boardTmp, size, 0.0f, 0.66f, 1.15f, TextAlignmentOptions.TopLeft);
            so.FindProperty("footer").objectReferenceValue = BoardText(root, "Alt", boardTmp, size, -0.44f, 0.1f, 0.75f, TextAlignmentOptions.Bottom);
            so.FindProperty("boardText").objectReferenceValue = boardTmp.gameObject;
            var mark = BoardText(root, "Bekleyen", boardTmp, size, 0.3f, 0.3f, 4f, TextAlignmentOptions.Right);
            mark.text = "?";
            so.FindProperty("pendingMark").objectReferenceValue = mark.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();
            // İstasyon kamerası tahtanın ortasına baksın (yer tutucu kamera aşağı bakıyordu, üst satırlar kadraj dışıydı).
            var station = board.GetComponentInParent<ChalkboardStation>();
            var cam = station != null ? station.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>(true) : null;
            if (cam != null)
            {
                var dir = root.position - cam.transform.position;
                if (dir.sqrMagnitude > 0.0001f) cam.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
            report.Add("✓ Tahta: Hata Dedektifi yazısı (" + parent.name + "), kamera tahtaya çevrildi: " + (cam != null));
        }

        private const string CabinetModel = "Assets/_Project/Art/Esya/Kilitli_Dolap.fbx";

        /// <summary>
        /// Kilitli dolap (Blender: ArtSource/Blender/Script/kilitli_dolap.py): atölyenin arka duvarında, malzeme rafının
        /// sağındaki boşlukta. Kök: LockedCabinet + kutu collider (layer Interactable); kapaklar ve kilit sarsılır.
        /// </summary>
        private static void SetupCabinet(List<string> report)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(CabinetModel);
            if (model == null) { report.Add("! Kilitli_Dolap.fbx yok (Blender betiğini çalıştır)"); return; }
            var go = GameObject.Find("Kilitli_Dolap");
            if (go == null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(model);
                go.name = "Kilitli_Dolap";
                var art = GameObject.Find("Atolye_Sanat");
                if (art != null) go.transform.SetParent(art.transform, true);
            }
            go.transform.position = new Vector3(2.6f, 0f, -3.0f);
            go.transform.rotation = Quaternion.identity;
            int layer = LayerMask.NameToLayer("Interactable");
            if (layer >= 0) go.layer = layer;
            var box = go.GetComponent<BoxCollider>();
            if (box == null) box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.82f, 0f);
            box.size = new Vector3(0.9f, 1.64f, 0.46f);
            var cab = go.GetComponent<LockedCabinet>();
            if (cab == null) cab = go.AddComponent<LockedCabinet>();
            var so = new SerializedObject(cab);
            var doors = so.FindProperty("doors");
            doors.arraySize = 2;
            doors.GetArrayElementAtIndex(0).objectReferenceValue = go.transform.Find("Dolap_Kapak_Sol");
            doors.GetArrayElementAtIndex(1).objectReferenceValue = go.transform.Find("Dolap_Kapak_Sag");
            so.FindProperty("padlock").objectReferenceValue = go.transform.Find("Dolap_Kilit");
            so.ApplyModifiedPropertiesWithoutUndo();
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) GameObjectUtility.SetStaticEditorFlags(r.gameObject,
                r.name.StartsWith("SM_") ? StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic : 0);
            report.Add("✓ Kilitli dolap: arka duvar (2.6, 0, -3.0)");
        }

        private const string LeverModel = "Assets/_Project/Art/Esya/Kazan_Bosaltma_Kolu.fbx";

        /// <summary>
        /// Kazan çevresi düzeltmeleri (karar 14.11): ocak algılama kutusu ocağın ağzına ve büyüğe; kazan kutusu yalnızca
        /// kazanı kaplar (ocağın önünü kesmez); boşaltma kolu modeli; terazi okunurluğu. Sanat terazisi statik (batching)
        /// olduğu için mantık kolu eğilmez: atomlar sanat kefelerinden kaymasın.
        /// </summary>
        private static void SetupCauldronFixes(List<string> report)
        {
            var controller = Object.FindAnyObjectByType<OrderController>(FindObjectsInactive.Include);
            if (controller == null) { report.Add("! Kazan yok"); return; }
            var root = controller.transform;

            var box = root.GetComponent<BoxCollider>();
            if (box != null) { box.center = new Vector3(0f, 0.82f, 0f); box.size = new Vector3(1.1f, 0.76f, 1.1f); }

            var stove = root.GetComponentInChildren<CauldronStove>(true);
            if (stove != null)
            {
                var t = stove.transform;
                // Station_Cauldron sahnede 180° dönük: yerel -Z oyuncu tarafı. Ocağın ağzı (ön yüz kazan merkezinden 0.45).
                t.localPosition = new Vector3(0f, 0.27f, -0.5f);
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
                var sb = stove.GetComponent<BoxCollider>();
                if (sb != null) { sb.center = Vector3.zero; sb.size = new Vector3(0.95f, 0.52f, 0.24f); }
                var mr = stove.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;               // yer tutucu küp ocağın önünde görünmesin
            }

            var drain = root.GetComponentInChildren<CauldronDrain>(true);
            var leverModel = AssetDatabase.LoadAssetAtPath<GameObject>(LeverModel);
            if (drain != null && leverModel != null)
            {
                var dt = drain.transform;
                // ocağın sağ ön köşesi (oyuncuya göre), kazanın yanı; istasyon 180° dönük olduğu için yerel -Z öndür
                dt.localPosition = new Vector3(0.56f, 0.55f, -0.36f);
                dt.localRotation = Quaternion.identity;
                var old = dt.Find("Lever");
                if (old != null) old.gameObject.SetActive(false);     // eski yer tutucu
                var inst = dt.Find("Kazan_Kolu");
                GameObject go = inst != null ? inst.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(leverModel, dt);
                go.name = "Kazan_Kolu";
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // tutamak oyuncuya kıvrık
                go.transform.localScale = Vector3.one * 1.5f;          // tutamak ~0.75 m: ocak üstünde rahat görünür
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                var db = drain.GetComponent<BoxCollider>();
                if (db != null) { db.center = new Vector3(0f, 0.32f, 0.02f); db.size = new Vector3(0.2f, 0.66f, 0.22f); }
                Set(drain, "lever", go.transform.Find("Kazan_Kolu_Cubuk"));
                report.Add("✓ Boşaltma kolu: model + animasyon (" + (go.transform.Find("Kazan_Kolu_Cubuk") != null) + ")");
            }
            else report.Add("! Boşaltma kolu kurulmadı (model: " + (leverModel != null) + ")");

            var scale = Object.FindAnyObjectByType<ScaleModel>(FindObjectsInactive.Include);
            if (scale != null)
            {
                var so = new SerializedObject(scale);
                so.FindProperty("atomSize").floatValue = 0.055f;
                so.FindProperty("maxPerRow").intValue = 6;
                so.FindProperty("labelFontSize").floatValue = 0.6f;
                so.FindProperty("labelForward").floatValue = 0.16f;
                so.FindProperty("tiltPerAtom").floatValue = 0f;
                so.FindProperty("maxTilt").floatValue = 0f;
                so.FindProperty("plaqueMaterial").objectReferenceValue = PlaqueMaterial();
                so.FindProperty("statusLocal").vector3Value = new Vector3(0f, 1.85f, 0f);
                var status = so.FindProperty("statusLabel").objectReferenceValue as TMP_Text;
                so.ApplyModifiedPropertiesWithoutUndo();
                if (status != null)
                {
                    status.transform.localPosition = new Vector3(0f, 1.82f, 0f);   // kolun üstünde, başlıkların arasında
                    status.fontSize = Mathf.Max(status.fontSize, 0.7f);
                    status.alignment = TextAlignmentOptions.Center;
                }
                // İkinci sanat terazisi (Terazi_Cikan) mantığa bağlı değil: boş kefeleriyle "kaymış" görünüyordu → gizli.
                var extra = GameObject.Find("Terazi_Cikan");
                if (extra != null) extra.SetActive(false);
                report.Add("✓ Terazi: levhalı etiketler, durum kolun üstünde, eğilme kapalı; boş Terazi_Cikan gizlendi: " + (extra != null));
            }
        }

        /// <summary>Tahta yazısının kopyası: aynı font/renk, tahta alanının bir şeridi (yMid: merkez oranı, h: yükseklik oranı).</summary>
        private static TMP_Text BoardText(Transform root, string name, TMP_Text src, Vector2 area, float yMid, float h, float sizeK,
            TextAlignmentOptions align)
        {
            var t = root.Find(name);
            var go = t != null ? t.gameObject : new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
            go.transform.SetParent(root, false);
            var tmp = go.GetComponent<TextMeshPro>();
            tmp.font = src.font;
            tmp.fontSharedMaterial = src.fontSharedMaterial;
            tmp.color = src.color;
            tmp.fontSize = src.fontSize * sizeK;
            tmp.enableAutoSizing = false;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.richText = true;
            var rt = tmp.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(area.x * 0.94f, area.y * h);
            rt.localPosition = new Vector3(0f, area.y * yMid, 0f);
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            return tmp;
        }
    }
}
