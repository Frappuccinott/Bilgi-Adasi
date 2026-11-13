using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.City;
using BilgiAdasi.Core;
using BilgiAdasi.Order;
using BilgiAdasi.UI;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Açık sahnedeki atölye sanatına (Atolye_Sanat) ve haritaya (Neighborhood_Denklestirme) sipariş düzeneğini bağlar:
    /// kazan istasyonu, teraziler, raf, tahta, ocak, köy sakinleri ve ham UI. Tekrar çalıştırılabilir (eski kökü siler).
    /// Önce: Sanat → Atölye/Harita/NPC yerleştir ve Seviye → 1) veri.
    /// </summary>
    public static class SiparisDuzenegiKurulum
    {
        private const string RootName = "_Oyun_Denklestirme";
        private const string UiName = "_Placeholder_UI_Oyun";
        private const string AtomPrefabFolder = "Assets/_Project/Prefabs/Atoms";

        [MenuItem("BilgiAdasi/Seviye/2) Atölyeye sipariş düzeneğini kur")]
        public static void Build()
        {
            var texts = AssetDatabase.LoadAssetAtPath<GameTexts>("Assets/_Project/Data/GameTexts.asset");
            var hood = AssetDatabase.LoadAssetAtPath<NeighborhoodData>("Assets/_Project/Data/Neighborhoods/Neighborhood_Denklestirme.asset");
            if (texts == null || hood == null)
            {
                Debug.LogError("[Kurulum] Önce 'Seviye → 1) Denkleştirme verisini üret' çalıştırın.");
                return;
            }
            if (GameObject.Find("Atolye_Sanat") == null)
            {
                Debug.LogError("[Kurulum] Sahnede Atolye_Sanat yok (Sanat → Atölyeyi yerleştir).");
                return;
            }

            DestroyIfExists(RootName);
            DestroyIfExists(UiName);
            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Sipariş düzeneği");

            // ── köy akışı
            var runnerGo = new GameObject("NeighborhoodRunner_Denklestirme");
            runnerGo.transform.SetParent(root.transform, false);
            var runner = runnerGo.AddComponent<NeighborhoodRunner>();
            Set(runner, "data", hood);
            Set(runner, "texts", texts);
            Set(runner, "view", FindNeighborhoodView("denklestirme"));

            // ── denetleyici
            var ctrlGo = new GameObject("OrderController");
            ctrlGo.transform.SetParent(root.transform, false);
            var controller = ctrlGo.AddComponent<OrderController>();
            Set(controller, "runner", runner);
            Set(controller, "texts", texts);

            BuildStation(root.transform, controller);
            BuildCauldronView(root.transform);
            BuildScales(root.transform);
            BuildShelf(root.transform);
            BuildChalkboard(root.transform);
            BuildStoveTarget(root.transform);
            BuildResidents(root.transform, runner, hood);
            BuildUi(texts);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("[Kurulum] Sipariş düzeneği kuruldu: " + RootName + " + " + UiName);
        }

        // ───────────────────────── istasyon ─────────────────────────

        private static void BuildStation(Transform root, OrderController controller)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Stations/Station_Cauldron.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            go.name = "Station_Cauldron";
            var kazan = Find("Kazan_Rig");
            go.transform.position = new Vector3(kazan.position.x, 0f, kazan.position.z);
            go.transform.rotation = Quaternion.identity;

            var placeholder = go.transform.Find("Visual/_Placeholder");
            if (placeholder != null) placeholder.gameObject.SetActive(false);

            // Kazanı saran etkileşim kutusu (sanat collider'larından biraz büyük, ışın önce buna çarpsın).
            var box = go.GetComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.75f, 0f);
            box.size = new Vector3(1.6f, 1.5f, 1.6f);
            go.layer = LayerMask.NameToLayer("Interactable");

            var cam = go.transform.Find("CameraPoint");
            cam.position = new Vector3(-0.85f, 2.05f, 0.75f);
            cam.rotation = Quaternion.LookRotation(new Vector3(-0.85f, 0.95f, -2.45f) - cam.position, Vector3.up);
            var cmc = cam.GetComponent<CinemachineCamera>();
            var lens = cmc.Lens;
            lens.FieldOfView = 55f;
            cmc.Lens = lens;

            var station = go.GetComponent<CauldronStation>();
            Set(station, "controller", controller);
            Set(station, "promptText", "Kazana geç");
            int mask = ~((1 << LayerMask.NameToLayer("Interactable")) | (1 << LayerMask.NameToLayer("Player")));
            SetInt(station, "targetMask", mask);
        }

        private static void BuildCauldronView(Transform root)
        {
            var go = new GameObject("WorkshopCauldronView");
            go.transform.SetParent(root, false);
            var view = go.AddComponent<WorkshopCauldronView>();
            Set(view, "cauldron", Find("Kazan_Rig").GetComponentInChildren<Animator>());
            Set(view, "fire", Find("Ocak_Ates_Rig").GetComponentInChildren<Animator>());
        }

        // ───────────────────────── teraziler ─────────────────────────

        private static void BuildScales(Transform root)
        {
            var go = new GameObject("WorkshopScalesView");
            go.transform.SetParent(root, false);
            var view = go.AddComponent<WorkshopScalesView>();
            var so = new SerializedObject(view);
            BindScale(so.FindProperty("left"), "Terazi_Giren", root);
            BindScale(so.FindProperty("right"), "Terazi_Cikan", root);

            var atoms = so.FindProperty("atomPrefabs");
            atoms.ClearArray();
            var elements = AssetDatabase.FindAssets("t:ElementData", new[] { "Assets/_Project/Data/Elements" });
            int i = 0;
            foreach (var guid in elements)
            {
                var e = AssetDatabase.LoadAssetAtPath<ElementData>(AssetDatabase.GUIDToAssetPath(guid));
                var prefab = AtomPrefab(e.Symbol);
                if (prefab == null) continue;
                atoms.InsertArrayElementAtIndex(i);
                var el = atoms.GetArrayElementAtIndex(i++);
                el.FindPropertyRelative("element").objectReferenceValue = e;
                el.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindScale(SerializedProperty p, string name, Transform root)
        {
            var scale = Find(name);
            var beam = Find(name + "_Kol");
            var panL = Find(name + "_Kefe_L");
            var panR = Find(name + "_Kefe_R");
            p.FindPropertyRelative("beam").objectReferenceValue = beam;
            p.FindPropertyRelative("panA").objectReferenceValue = panL;
            p.FindPropertyRelative("panB").objectReferenceValue = panR;
            p.FindPropertyRelative("slotA").objectReferenceValue = PanSlot(panL, root);
            p.FindPropertyRelative("slotB").objectReferenceValue = PanSlot(panR, root);

            var labelGo = new GameObject(name + "_Etiket");
            labelGo.transform.SetParent(root, false);
            labelGo.transform.position = scale.position + new Vector3(0f, 1.78f, 0f);
            var tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.fontSize = 1.1f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(1.4f, 0.45f);
            tmp.color = new Color(0.98f, 0.94f, 0.85f);
            tmp.richText = true;
            labelGo.AddComponent<FaceCamera>();
            p.FindPropertyRelative("label").objectReferenceValue = tmp;
        }

        /// <summary>Kefenin içine, tabanın hemen üstüne atom yuvası (kefe hareket edince atomlar da gider).</summary>
        private static Transform PanSlot(Transform pan, Transform root)
        {
            var r = pan.GetComponentInChildren<Renderer>();
            var slot = new GameObject(pan.name + "_AtomYuvasi").transform;
            slot.SetParent(pan, false);
            float y = r != null ? r.bounds.min.y + 0.025f : pan.position.y - 0.45f;
            var center = r != null ? r.bounds.center : pan.position;
            slot.position = new Vector3(center.x, y, center.z);
            slot.rotation = Quaternion.identity;
            return slot;
        }

        private static GameObject AtomPrefab(string symbol)
        {
            string fbx = $"Assets/_Project/Art/Atom/Atom_{symbol}.fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (model == null) return null;
            if (!AssetDatabase.IsValidFolder(AtomPrefabFolder)) AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Atoms");
            string path = $"{AtomPrefabFolder}/Atom_{symbol}.prefab";

            var stilize = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Prefablar/Malzeme_H2.prefab")
                .GetComponentInChildren<Renderer>().sharedMaterial;
            var wrapper = new GameObject("Atom_" + symbol);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            inst.transform.SetParent(wrapper.transform, false);
            inst.transform.localPosition = Vector3.zero;
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterial = stilize;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var saved = PrefabUtility.SaveAsPrefabAsset(wrapper, path);
            Object.DestroyImmediate(wrapper);
            return saved;
        }

        // ───────────────────────── raf, tahta, ocak ─────────────────────────

        private static void BuildShelf(Transform root)
        {
            var go = new GameObject("WorkshopShelfView");
            go.transform.SetParent(root, false);
            var view = go.AddComponent<WorkshopShelfView>();
            var so = new SerializedObject(view);

            // Kazana en yakın yuvadan başla (1_4 → 1_1).
            var slots = so.FindProperty("slots");
            string[] names = { "Raf_Yuva_1_4", "Raf_Yuva_1_3", "Raf_Yuva_1_2", "Raf_Yuva_1_1" };
            slots.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++) slots.GetArrayElementAtIndex(i).objectReferenceValue = Find(names[i]);

            var jars = so.FindProperty("jarPrefabs");
            jars.ClearArray();
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:SpeciesData", new[] { "Assets/_Project/Data/Species" }))
            {
                var s = AssetDatabase.LoadAssetAtPath<SpeciesData>(AssetDatabase.GUIDToAssetPath(guid));
                string file = s.Formula.Replace("(", "").Replace(")", "");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Art/Prefablar/Malzeme_{file}.prefab");
                if (prefab == null) continue;
                jars.InsertArrayElementAtIndex(n);
                var el = jars.GetArrayElementAtIndex(n++);
                el.FindPropertyRelative("species").objectReferenceValue = s;
                el.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // Raftaki süs mühürlü kavanoz ilk yuvayı kapatıyor → gizle.
            var decor = Find("Kavanoz_Muhurlu", false);
            if (decor != null) decor.gameObject.SetActive(false);
        }

        private static void BuildChalkboard(Transform root)
        {
            var board = Find("Karatahta");
            var r = board.GetComponent<Renderer>();
            var go = new GameObject("WorkshopChalkboardView");
            go.transform.SetParent(root, false);
            var textGo = new GameObject("Tahta_Yazi");
            textGo.transform.SetParent(go.transform, false);
            textGo.transform.position = new Vector3(r.bounds.max.x + 0.01f, r.bounds.center.y, r.bounds.center.z);
            textGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            var tmp = textGo.AddComponent<TextMeshPro>();
            tmp.fontSize = 1.1f;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.rectTransform.sizeDelta = new Vector2(r.bounds.size.z - 0.2f, r.bounds.size.y - 0.2f);
            tmp.color = new Color(0.93f, 0.93f, 0.88f);
            var view = go.AddComponent<WorkshopChalkboardView>();
            Set(view, "text", tmp);
            Set(view, "header", "Kural: ne girerse o çıkar.");
        }

        private static void BuildStoveTarget(Transform root)
        {
            var fire = Find("Ocak_Ates_Rig");
            var go = new GameObject("Hedef_Ocak");
            go.transform.SetParent(root, false);
            go.transform.position = fire.position + new Vector3(0f, 0.2f, 0.35f);
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(0.7f, 0.35f, 0.25f);
            box.isTrigger = true;
            go.AddComponent<StationTarget>().Setup(StationTargetKind.Stove, null, null);
        }

        // ───────────────────────── sakinler ─────────────────────────

        private struct ResidentPlace
        {
            public string Anchor;
            public Vector3 Offset;
            public bool Female;
        }

        private static void BuildResidents(Transform root, NeighborhoodRunner runner, NeighborhoodData hood)
        {
            // Görev sırasıyla: çeşme, fener, fırın, köprü, tarla (04-gorev-plani Seviye 1–5).
            var places = new[]
            {
                new ResidentPlace { Anchor = "Denk_P1_Cesme_Su", Offset = new Vector3(0f, 0f, -2.2f) },
                new ResidentPlace { Anchor = "Denk_P2_Fener_Isik", Offset = new Vector3(4f, 0f, -2.5f) },
                new ResidentPlace { Anchor = "Denk_P3_Firin_Kor", Offset = new Vector3(2.0f, 0f, 4.4f), Female = true },
                new ResidentPlace { Anchor = "Denk_P4_Kopru_Tas", Offset = new Vector3(9f, 0f, -1.5f) },
                new ResidentPlace { Anchor = "Denk_P5_Ekin", Offset = new Vector3(8.5f, 0f, 0f), Female = true },
            };

            var group = new GameObject("Sakinler");
            group.transform.SetParent(root, false);
            for (int i = 0; i < hood.Tasks.Count && i < places.Length; i++)
            {
                var task = hood.Tasks[i];
                var anchor = Find(places[i].Anchor, false);
                if (anchor == null) continue;
                var r = anchor.GetComponentInChildren<Renderer>(true);
                Vector3 basePos = (r != null ? r.bounds.center : anchor.position) + places[i].Offset;
                basePos.y = GroundY(basePos);

                string model = places[i].Female ? "NPC_KoyluKadin" : "NPC_KoyluErkek";
                string clipBase = places[i].Female ? "KoyluKadin" : "KoyluErkek";
                var go = new GameObject("Sakin_" + task.TaskId + "_" + task.ResidentName.Replace(' ', '_'));
                go.transform.SetParent(group.transform, false);
                go.transform.position = basePos;
                go.layer = LayerMask.NameToLayer("Interactable");

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Art/Prefablar/{model}.prefab");
                var body = (GameObject)PrefabUtility.InstantiatePrefab(prefab, go.transform);
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.identity;

                var cap = go.AddComponent<CapsuleCollider>();
                cap.center = new Vector3(0f, 0.85f, 0f);
                cap.height = 1.7f;
                cap.radius = 0.4f;

                var marker = new GameObject("Isaret");
                marker.transform.SetParent(go.transform, false);
                marker.transform.localPosition = new Vector3(0f, 2.15f, 0f);
                var tmp = marker.AddComponent<TextMeshPro>();
                tmp.text = "!";
                tmp.fontSize = 6f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.95f, 0.62f, 0.2f);
                tmp.rectTransform.sizeDelta = new Vector2(0.6f, 0.8f);
                marker.AddComponent<FaceCamera>();

                var resident = go.AddComponent<ResidentNpc>();
                resident.Configure(runner, task, body.GetComponentInChildren<Animator>(),
                    clipBase + "_Bekle_Dongu", clipBase + "_Konus_Dongu", marker);
                EditorUtility.SetDirty(resident);

                // Atölyeye (orijin) dönük dursun.
                var look = -basePos;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f) go.transform.rotation = Quaternion.LookRotation(look);
            }
        }

        private static float GroundY(Vector3 p)
        {
            var hits = Physics.RaycastAll(new Vector3(p.x, 60f, p.z), Vector3.down, 120f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.NegativeInfinity;
            foreach (var h in hits)
            {
                if (h.collider.GetComponentInParent<ResidentNpc>() != null) continue;
                if (h.point.y > best && h.point.y < p.y + 3f) best = h.point.y;
            }
            return float.IsNegativeInfinity(best) ? p.y : best;
        }

        // ───────────────────────── UI (ham yer tutucu) ─────────────────────────

        private static readonly Color PanelColor = new Color(0.12f, 0.09f, 0.07f, 0.88f);
        private static readonly Color Ink = new Color(0.97f, 0.93f, 0.84f);
        private static readonly Color Amber = new Color(0.95f, 0.68f, 0.3f);

        private static void BuildUi(GameTexts texts)
        {
            var canvasGo = new GameObject(UiName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "UI");
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var c = canvasGo.transform;

            // Sipariş kartı (sol üst) + yardım (alt)
            var cardRoot = Node(c, "OrderCard");
            var card = Panel(cardRoot, "Panel", new Vector2(0, 1), new Vector2(30, -30), new Vector2(600, 420));
            var cardTitle = Text(card, "Baslik", 34, Amber, new Vector2(24, -18), new Vector2(552, 50), FontStyles.Bold);
            var cardBody = Text(card, "Govde", 30, Ink, new Vector2(24, -74), new Vector2(552, 160));
            var cardCounts = Text(card, "Sayac", 32, Ink, new Vector2(24, -240), new Vector2(552, 170));
            var help = Text(cardRoot, "Yardim", 28, Ink, new Vector2(0, 30), new Vector2(1800, 44), FontStyles.Normal,
                new Vector2(0.5f, 0), TextAlignmentOptions.Bottom);
            var cardView = cardRoot.gameObject.AddComponent<PlaceholderOrderCardView>();
            Set(cardView, "panel", card.gameObject);
            Set(cardView, "titleLabel", cardTitle);
            Set(cardView, "bodyLabel", cardBody);
            Set(cardView, "countsLabel", cardCounts);
            Set(cardView, "helpLabel", help);

            // Görev hedefi (sağ üst)
            var objRoot = Node(c, "Objective");
            var objText = Text(objRoot, "Hedef", 30, Ink, new Vector2(-30, -30), new Vector2(760, 90), FontStyles.Bold,
                new Vector2(1, 1), TextAlignmentOptions.TopRight);
            Set(objRoot.gameObject.AddComponent<PlaceholderObjectiveView>(), "label", objText);

            // Kısa not (üst orta)
            var toastRoot = Node(c, "Toast");
            var toastPanel = Panel(toastRoot, "Panel", new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(1100, 130));
            var toastText = Text(toastPanel, "Metin", 30, Ink, new Vector2(20, -12), new Vector2(1060, 106), FontStyles.Normal,
                new Vector2(0, 1), TextAlignmentOptions.Center);
            var toast = toastRoot.gameObject.AddComponent<PlaceholderToastView>();
            Set(toast, "panel", toastPanel.gameObject);
            Set(toast, "label", toastText);

            // Konuşma (alt orta)
            var dlgRoot = Node(c, "Dialogue");
            var dlg = Panel(dlgRoot, "Panel", new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(1400, 440));
            var speaker = Text(dlg, "Konusan", 32, Amber, new Vector2(30, -20), new Vector2(1340, 46), FontStyles.Bold);
            var body = Text(dlg, "Metin", 32, Ink, new Vector2(30, -70), new Vector2(1340, 130));
            var options = new Button[3];
            for (int i = 0; i < 3; i++)
                options[i] = MakeButton(dlg, "Secenek_" + i, new Vector2(30, -205 - i * 68), new Vector2(1340, 60), TextAlignmentOptions.Left);
            var cont = MakeButton(dlg, "Devam", new Vector2(1130, -360), new Vector2(240, 60), TextAlignmentOptions.Center);
            var dlgView = dlgRoot.gameObject.AddComponent<PlaceholderDialogueView>();
            Set(dlgView, "panel", dlg.gameObject);
            Set(dlgView, "speakerLabel", speaker);
            Set(dlgView, "bodyLabel", body);
            var so = new SerializedObject(dlgView);
            var arr = so.FindProperty("optionButtons");
            arr.arraySize = 3;
            for (int i = 0; i < 3; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = options[i];
            so.FindProperty("continueButton").objectReferenceValue = cont;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Defter (orta)
            var bookRoot = Node(c, "HintBook");
            var book = Panel(bookRoot, "Panel", new Vector2(0.5f, 0.5f), new Vector2(-600, 380), new Vector2(1200, 760));
            var hintTitle = Text(book, "NotlarBaslik", 34, Amber, new Vector2(30, -24), new Vector2(540, 50), FontStyles.Bold);
            var hint = Text(book, "Notlar", 30, Ink, new Vector2(30, -84), new Vector2(540, 560));
            var discTitle = Text(book, "KesifBaslik", 34, Amber, new Vector2(630, -24), new Vector2(540, 50), FontStyles.Bold);
            var disc = Text(book, "Kesifler", 28, Ink, new Vector2(630, -84), new Vector2(540, 560));
            var close = MakeButton(book, "Kapat", new Vector2(930, -680), new Vector2(240, 60), TextAlignmentOptions.Center);
            close.GetComponentInChildren<TMP_Text>().text = "Kapat";
            var glow = Text(bookRoot, "Parlama", 28, Amber, new Vector2(30, 30), new Vector2(700, 44), FontStyles.Bold,
                new Vector2(0, 0), TextAlignmentOptions.BottomLeft);
            glow.text = texts.HintGlowNote;
            var bookView = bookRoot.gameObject.AddComponent<PlaceholderHintBookView>();
            Set(bookView, "panel", book.gameObject);
            Set(bookView, "hintTitleLabel", hintTitle);
            Set(bookView, "hintLabel", hint);
            Set(bookView, "discoveryTitleLabel", discTitle);
            Set(bookView, "discoveryLabel", disc);
            Set(bookView, "closeButton", close);
            Set(bookView, "glowIndicator", glow);

            // Gerçek hayatta (orta)
            var lifeRoot = Node(c, "RealLife");
            var life = Panel(lifeRoot, "Panel", new Vector2(0.5f, 0.5f), new Vector2(-480, 220), new Vector2(960, 440));
            var lifeTitle = Text(life, "Baslik", 34, Amber, new Vector2(30, -24), new Vector2(900, 50), FontStyles.Bold);
            var lifeBody = Text(life, "Metin", 30, Ink, new Vector2(30, -84), new Vector2(900, 260));
            var lifeClose = MakeButton(life, "Tamam", new Vector2(690, -360), new Vector2(240, 60), TextAlignmentOptions.Center);
            lifeClose.GetComponentInChildren<TMP_Text>().text = "Tamam";
            var lifeView = lifeRoot.gameObject.AddComponent<PlaceholderRealLifeCardView>();
            Set(lifeView, "panel", life.gameObject);
            Set(lifeView, "titleLabel", lifeTitle);
            Set(lifeView, "bodyLabel", lifeBody);
            Set(lifeView, "closeButton", lifeClose);
        }

        private static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>anchor = hem anchor hem pivot; pos = sol üst köşenin anchor'a göre yeri (pivot sol üst).</summary>
        private static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(anchor.x == 0.5f && anchor.y != 0.5f ? 0.5f : anchor.x == 1 ? 1 : 0, anchor.y == 0.5f ? 1 : anchor.y);
            if (anchor == new Vector2(0.5f, 0.5f)) rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = PanelColor;
            return rt;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, float size, Color color, Vector2 pos, Vector2 box,
            FontStyles style = FontStyles.Normal, Vector2? anchor = null, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            var a = anchor ?? new Vector2(0, 1);
            rt.anchorMin = rt.anchorMax = a;
            rt.pivot = a;
            rt.anchoredPosition = pos;
            rt.sizeDelta = box;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.richText = true;
            t.raycastTarget = false;
            return t;
        }

        private static Button MakeButton(Transform parent, string name, Vector2 pos, Vector2 size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.3f, 0.22f, 0.15f, 0.95f);
            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.55f, 0.38f, 0.2f);
            colors.selectedColor = new Color(0.55f, 0.38f, 0.2f);
            btn.colors = colors;
            var label = Text(rt, "Yazi", 28, Ink, new Vector2(18, 0), new Vector2(size.x - 36, size.y), FontStyles.Normal,
                new Vector2(0, 1), align);
            label.rectTransform.anchoredPosition = new Vector2(18, 0);
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            return btn;
        }

        // ───────────────────────── yardımcılar ─────────────────────────

        private static Transform Find(string name, bool required = true)
        {
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                var t = FindDeep(root.transform, name);
                if (t != null) return t;
            }
            if (required) Debug.LogError("[Kurulum] Sahnede bulunamadı: " + name);
            return null;
        }

        private static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var r = FindDeep(t.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        private static NeighborhoodView FindNeighborhoodView(string id)
        {
            foreach (var v in Object.FindObjectsByType<NeighborhoodView>(FindObjectsInactive.Include))
                if (v.NeighborhoodId == id) return v;
            Debug.LogWarning("[Kurulum] NeighborhoodView bulunamadı: " + id);
            return null;
        }

        private static void DestroyIfExists(string name)
        {
            var go = GameObject.Find(name);
            if (go != null) Undo.DestroyObjectImmediate(go);
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError($"[Kurulum] {target.GetType().Name}.{field} yok");
                return;
            }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
