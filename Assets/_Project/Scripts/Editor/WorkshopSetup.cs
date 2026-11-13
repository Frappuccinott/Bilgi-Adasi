using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.City;
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
    /// Kurulum 8 (karar 13.11 — atölye): ekonomi (kese, fiyat, ödeme), pazar kataloğu (eski Economy listesinden geçiş),
    /// kavanoz/atom/raf (boş yerde yeşil işaret)/pazar prefab'ları,
    /// Station_Cauldron'u FPS kazana çevirir (istasyon kamerası ve eski 3D panel kalkar; ocak, boşaltma kolu,
    /// sıvı/köpük/ateş, 3D terazi eklenir), oyuncuya envanter, tahtaya 3D yazı, Town'a raf + pazar + HUD.
    /// Var olanın üzerine yazmaz; tekrar çalıştırmak güvenlidir. Yer tutucular renkli küp/silindir/küredir.
    /// </summary>
    public static class WorkshopSetup
    {
        private const string Data = "Assets/_Project/Data";
        private const string EconomyPath = Data + "/Economy.asset";
        private const string MarketFolder = Data + "/Market";
        // Dosya adı = stok kaydındaki tezgâh kimliği; değiştirilirse satılan adetler sıfırlanır.
        private const string MarketCatalogPath = MarketFolder + "/Pazar.asset";
        private const string PrefabsFolder = "Assets/_Project/Prefabs";
        private const string WorkshopFolder = PrefabsFolder + "/Workshop";
        private const string JarPath = PrefabsFolder + "/Jars/Jar_Placeholder.prefab";
        private const string AtomPath = WorkshopFolder + "/Atom_Placeholder.prefab";
        private const string ShelfPath = WorkshopFolder + "/Shelf.prefab";
        private const string MarketPath = WorkshopFolder + "/MarketStall.prefab";
        private const string CauldronPath = PrefabsFolder + "/Stations/Station_Cauldron.prefab";
        private const string DoorPath = PrefabsFolder + "/Stations/Station_Door.prefab";
        private const string BoardPath = PrefabsFolder + "/Stations/Station_Chalkboard.prefab";
        private const string RigPath = PrefabsFolder + "/PlayerRig.prefab";
        private const string TownPath = "Assets/_Project/Scenes/Town.unity";

        // 05 §2'ye taslak: pazar fiyatları ve sipariş ödemeleri (oynanarak ayarlanacak, 06 açık işler).
        private static readonly Dictionary<string, int> Prices = new Dictionary<string, int>
        {
            { "H2", 2 }, { "O2", 1 }, { "CH4", 3 }, { "C3H8", 5 }, { "CaCO3", 4 }, { "H2O", 1 }, { "N2", 3 },
        };
        private static readonly string[] MarketOrder = { "H2", "O2", "H2O", "CH4", "C3H8", "CaCO3", "N2" };
        private static readonly int[] Rewards = { 10, 12, 15, 15, 25 };

        [MenuItem("BilgiAdasi/Kurulum/8 - Atölye (envanter, raf, pazar, FPS kazan, 3D terazi)", priority = 7)]
        public static void Run()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CauldronPath) == null)
            {
                EditorUtility.DisplayDialog("Atölye", "Önce 'Kurulum → 3' çalıştırılmalı.", "Tamam");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var texts = OrderSetup.EnsureTexts();
            if (OrderSetup.EnsureDenklestirmeTasks() == null) return;
            EnsureEconomy();
            EnsureMarketCatalog();
            EnsureFolder(PrefabsFolder, "Workshop");
            EnsureFolder(PrefabsFolder, "Jars");
            EnsureJar();
            EnsureAtom();
            EnsureShelf();
            EnsureMarket();
            RebuildCauldron();
            CleanDoor();
            AddBoardText();
            AddInventoryToRig();
            AssetDatabase.SaveAssets();

            SetUpTown();
            AssetDatabase.SaveAssets();
            Debug.Log("[BilgiAdasi] Atölye kurulumu tamam: ekonomi, raf, pazar, FPS kazan + 3D terazi, envanter, HUD.");
        }

        // ---------- Veri ----------

        private static EconomyData EnsureEconomy()
        {
            var existing = AssetDatabase.LoadAssetAtPath<EconomyData>(EconomyPath);
            if (existing != null) return existing;

            var species = OrderSetup.LoadAll<SpeciesData>(Data + "/Species", s => s.Formula);
            foreach (var pair in species)
            {
                var so = new SerializedObject(pair.Value);
                so.FindProperty("price").intValue = Prices.TryGetValue(pair.Key, out int p) ? p : 3;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(pair.Value);
            }

            for (int i = 0; i < Rewards.Length; i++)
            {
                var task = AssetDatabase.LoadAssetAtPath<OrderTask>($"{Data}/Tasks/Denklestirme/denk_{i + 1}.asset");
                if (task == null) continue;
                var so = new SerializedObject(task);
                so.FindProperty("reward").intValue = Rewards[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(task);
            }

            var economy = ScriptableObject.CreateInstance<EconomyData>();
            var eso = new SerializedObject(economy);
            var catalog = eso.FindProperty("catalog");
            foreach (var s in species.Values)
            {
                catalog.arraySize++;
                catalog.GetArrayElementAtIndex(catalog.arraySize - 1).objectReferenceValue = s;
            }
            eso.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(economy, EconomyPath);
            return economy;
        }

        /// <summary>
        /// Pazar kataloğu. Geçiş: eski Economy.asset'teki "marketItems" listesi (alan koddan kalktı, YAML'da duruyor)
        /// kataloğa taşınır, sonra Economy.asset yeniden yazılıp eski alan silinir. Yeni kurulumda MarketOrder kullanılır.
        /// Var olan satırlara dokunmaz; tekrar çalıştırmak güvenlidir.
        /// </summary>
        private static MarketCatalog EnsureMarketCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<MarketCatalog>(MarketCatalogPath);
            bool created = catalog == null;
            if (created)
            {
                EnsureFolder(Data, "Market");
                catalog = ScriptableObject.CreateInstance<MarketCatalog>();
                AssetDatabase.CreateAsset(catalog, MarketCatalogPath);
            }

            var legacy = ReadLegacyMarketItems();
            if (legacy.Count > 0)
            {
                foreach (var s in legacy) catalog.AddIfMissing(s);
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
                AssetDatabase.ForceReserializeAssets(new[] { EconomyPath });
                Debug.Log($"[BilgiAdasi] Pazar listesi Economy'den {MarketCatalogPath} dosyasına taşındı ({legacy.Count} madde).");
            }
            else if (created)
            {
                var species = OrderSetup.LoadAll<SpeciesData>(Data + "/Species", s => s.Formula);
                for (int i = 0; i < MarketOrder.Length; i++)
                    if (species.TryGetValue(MarketOrder[i], out var s)) catalog.AddIfMissing(s);
                EditorUtility.SetDirty(catalog);
            }
            return catalog;
        }

        /// <summary>Economy.asset YAML'ındaki eski "marketItems" listesinden maddeleri okur (sırayla). Yoksa boş.</summary>
        private static List<SpeciesData> ReadLegacyMarketItems()
        {
            var result = new List<SpeciesData>();
            if (!System.IO.File.Exists(EconomyPath)) return result;

            var lines = System.IO.File.ReadAllLines(EconomyPath);
            bool inList = false;
            foreach (var line in lines)
            {
                if (!inList)
                {
                    if (line.TrimStart().StartsWith("marketItems:")) inList = true;
                    continue;
                }
                if (!line.TrimStart().StartsWith("- ")) break;
                var match = System.Text.RegularExpressions.Regex.Match(line, "guid: ([0-9a-f]{32})");
                if (!match.Success) continue;
                var path = AssetDatabase.GUIDToAssetPath(match.Groups[1].Value);
                var s = AssetDatabase.LoadAssetAtPath<SpeciesData>(path);
                if (s != null && !result.Contains(s)) result.Add(s);
            }
            return result;
        }

        private static EconomyData Economy => AssetDatabase.LoadAssetAtPath<EconomyData>(EconomyPath);
        private static MarketCatalog Catalog => AssetDatabase.LoadAssetAtPath<MarketCatalog>(MarketCatalogPath);
        private static OrderTexts Texts => OrderSetup.EnsureTexts();

        // ---------- Prefab'lar ----------

        private static void EnsureJar()
        {
            if (AssetDatabase.LoadAssetAtPath<JarProp>(JarPath) != null) return;

            var root = new GameObject("Jar_Placeholder");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            body.transform.localScale = new Vector3(0.12f, 0.16f, 0.12f);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            var label = WorldText(root.transform, "Label", new Vector3(0f, 0.26f, 0f), new Vector2(0.3f, 0.14f), 0.6f,
                TextAlignmentOptions.Bottom, new Color(0.98f, 0.95f, 0.88f));
            label.text = "H<sub>2</sub>O";

            var prop = root.AddComponent<JarProp>();
            var so = new SerializedObject(prop);
            so.FindProperty("label").objectReferenceValue = label;
            var tint = so.FindProperty("tintRenderers");
            tint.arraySize = 1;
            tint.GetArrayElementAtIndex(0).objectReferenceValue = body.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
            Save(root, JarPath);
        }

        private static void EnsureAtom()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AtomPath) != null) return;
            var atom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            atom.name = "Atom_Placeholder";
            Object.DestroyImmediate(atom.GetComponent<Collider>());
            Save(atom, AtomPath);
        }

        private static void EnsureShelf()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ShelfPath) != null)
            {
                AddShelfMarkers();
                return;
            }

            // Önü -Z: oyuncu -Z tarafından bakar. 2 sıra × 4 yer.
            var root = new GameObject("Shelf");
            var visual = Child(root.transform, "Visual", Vector3.zero);
            Block(visual, "SM_Raf_Arka", new Vector3(0f, 1.0f, 0.14f), new Vector3(1.3f, 2.0f, 0.04f), Wood, false);
            Block(visual, "SM_Raf_Alt", new Vector3(0f, 0.85f, 0f), new Vector3(1.3f, 0.04f, 0.3f), Wood, false);
            Block(visual, "SM_Raf_Ust", new Vector3(0f, 1.3f, 0f), new Vector3(1.3f, 0.04f, 0.3f), Wood, false);

            int index = 0;
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 4; col++)
                {
                    var slot = Child(root.transform, "Slot_" + (++index),
                        new Vector3(-0.45f + col * 0.3f, row == 0 ? 0.87f : 1.32f, 0f));
                    slot.layer = ProjectSetup.InteractableLayer;
                    var box = slot.AddComponent<BoxCollider>();
                    box.center = new Vector3(0f, 0.14f, 0f);
                    box.size = new Vector3(0.26f, 0.28f, 0.28f);
                    AddPlaceMarker(slot.AddComponent<ShelfSlot>());
                }

            var shelf = root.AddComponent<Shelf>();
            var so = new SerializedObject(shelf);
            so.FindProperty("economy").objectReferenceValue = Economy;
            so.FindProperty("texts").objectReferenceValue = Texts;
            so.FindProperty("defaultJarPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<JarProp>(JarPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            Save(root, ShelfPath);
        }

        /// <summary>Var olan raf prefab'ında işareti olmayan yerlere yeşil işaret ekler.</summary>
        private static void AddShelfMarkers()
        {
            var root = PrefabUtility.LoadPrefabContents(ShelfPath);
            try
            {
                bool changed = false;
                foreach (var slot in root.GetComponentsInChildren<ShelfSlot>(true))
                    changed |= AddPlaceMarker(slot);
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, ShelfPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Raf yerine "buraya koyabilirsin" işareti: rafın üstünde ince yeşil levha (yer tutucu, collider yok).
        /// Alan zaten doluysa (Mustafa kendi işaretini verdiyse) dokunmaz.
        /// </summary>
        private static bool AddPlaceMarker(ShelfSlot slot)
        {
            var so = new SerializedObject(slot);
            var prop = so.FindProperty("placeMarker");
            if (prop.objectReferenceValue != null) return false;

            var existing = slot.transform.Find("PlaceMarker");
            var marker = existing != null
                ? existing.gameObject
                : Block(slot.gameObject, "PlaceMarker", new Vector3(0f, 0.004f, 0f), new Vector3(0.22f, 0.008f, 0.22f), MarkerGreen, false);
            marker.SetActive(false);
            prop.objectReferenceValue = marker.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static void EnsureMarket()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MarketPath) != null)
            {
                WireMarketCatalog();
                return;
            }

            var root = new GameObject("MarketStall") { layer = ProjectSetup.InteractableLayer };
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.5f, 0f);
            box.size = new Vector3(1.8f, 1.0f, 0.8f);
            var visual = Child(root.transform, "Visual", Vector3.zero);
            Block(visual, "SM_Tezgah", new Vector3(0f, 0.5f, 0f), new Vector3(1.8f, 1.0f, 0.8f), Wood, false);
            Block(visual, "SM_Tente", new Vector3(0f, 2.2f, 0.2f), new Vector3(2.0f, 0.08f, 1.2f), Roof, false);
            Block(visual, "SM_Direk_Sol", new Vector3(-0.9f, 1.1f, 0.35f), new Vector3(0.08f, 2.2f, 0.08f), Wood, false);
            Block(visual, "SM_Direk_Sag", new Vector3(0.9f, 1.1f, 0.35f), new Vector3(0.08f, 2.2f, 0.08f), Wood, false);

            var stall = root.AddComponent<MarketStall>();
            var so = new SerializedObject(stall);
            so.FindProperty("catalog").objectReferenceValue = Catalog;
            so.FindProperty("economy").objectReferenceValue = Economy;
            so.FindProperty("texts").objectReferenceValue = Texts;
            so.ApplyModifiedPropertiesWithoutUndo();
            Save(root, MarketPath);
        }

        /// <summary>Var olan tezgâh prefab'ına kataloğu bağlar (boşsa; elle başka katalog seçildiyse dokunmaz).</summary>
        private static void WireMarketCatalog()
        {
            var root = PrefabUtility.LoadPrefabContents(MarketPath);
            try
            {
                var stall = root.GetComponent<MarketStall>();
                if (stall == null) return;
                var so = new SerializedObject(stall);
                var prop = so.FindProperty("catalog");
                if (prop.objectReferenceValue != null) return;
                prop.objectReferenceValue = Catalog;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, MarketPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RebuildCauldron()
        {
            var root = PrefabUtility.LoadPrefabContents(CauldronPath);
            try
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                DestroyChild(root, "CameraPoint");
                DestroyChild(root, "Props");

                if (!root.TryGetComponent<OrderController>(out var controller)) controller = root.AddComponent<OrderController>();
                var cso = new SerializedObject(controller);
                cso.FindProperty("texts").objectReferenceValue = Texts;
                cso.ApplyModifiedPropertiesWithoutUndo();

                if (root.transform.Find("Effects") == null)
                {
                    // Yer tutucu yerleşim: Visual'daki 1 m küp kazan (üstü y=1), önü -Z.
                    var fx = Child(root.transform, "Effects", Vector3.zero);
                    var liquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    liquid.name = "Liquid";
                    liquid.transform.SetParent(fx.transform, false);
                    liquid.transform.localPosition = new Vector3(0f, 1.005f, 0f);
                    liquid.transform.localScale = new Vector3(0.8f, 0.005f, 0.8f);
                    Object.DestroyImmediate(liquid.GetComponent<Collider>());

                    var foam = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    foam.name = "Foam";
                    foam.transform.SetParent(fx.transform, false);
                    foam.transform.localPosition = new Vector3(0f, 1.05f, 0f);
                    foam.transform.localScale = new Vector3(0.9f, 0.35f, 0.9f);
                    Object.DestroyImmediate(foam.GetComponent<Collider>());
                    Tint(foam, new Color(0.93f, 0.95f, 0.97f));

                    var fire = Block(fx, "Fire", new Vector3(0f, 0.15f, -0.69f), new Vector3(0.34f, 0.16f, 0.04f), FireColor, false);

                    var visuals = fx.AddComponent<CauldronVisuals>();
                    var vso = new SerializedObject(visuals);
                    vso.FindProperty("liquid").objectReferenceValue = liquid.GetComponent<Renderer>();
                    vso.FindProperty("foam").objectReferenceValue = foam;
                    vso.FindProperty("fire").objectReferenceValue = fire;
                    vso.ApplyModifiedPropertiesWithoutUndo();
                }

                if (root.transform.Find("Stove") == null)
                {
                    var stove = Block(root, "Stove", new Vector3(0f, 0.15f, -0.6f), new Vector3(0.5f, 0.3f, 0.15f), Stone, true);
                    stove.layer = ProjectSetup.InteractableLayer;
                    stove.AddComponent<CauldronStove>();
                }

                if (root.transform.Find("Drain") == null)
                {
                    var drain = Child(root.transform, "Drain", new Vector3(0.75f, 0.45f, 0f));
                    drain.layer = ProjectSetup.InteractableLayer;
                    var box = drain.AddComponent<BoxCollider>();
                    box.center = new Vector3(0f, 0.2f, 0f);
                    box.size = new Vector3(0.15f, 0.45f, 0.15f);
                    var lever = Child(drain.transform, "Lever", Vector3.zero);
                    Block(lever, "SM_Kol", new Vector3(0f, 0.2f, 0f), new Vector3(0.05f, 0.4f, 0.05f), Brass, false);
                    var d = drain.AddComponent<CauldronDrain>();
                    var dso = new SerializedObject(d);
                    dso.FindProperty("lever").objectReferenceValue = lever.transform;
                    dso.ApplyModifiedPropertiesWithoutUndo();
                }

                if (root.transform.Find("Scale") == null) BuildScale(root);

                PrefabUtility.SaveAsPrefabAsset(root, CauldronPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>3D terazi, kazanın soluna. Kol y=1.45'te; kefeler kolun uçlarından sarkar.</summary>
        private static void BuildScale(GameObject cauldron)
        {
            var scale = Child(cauldron.transform, "Scale", new Vector3(-1.25f, 0f, 0f));
            var visual = Child(scale.transform, "Visual", Vector3.zero);
            Block(visual, "SM_Ayak", new Vector3(0f, 0.02f, 0f), new Vector3(0.35f, 0.04f, 0.35f), Wood, false);
            Block(visual, "SM_Direk", new Vector3(0f, 0.73f, 0f), new Vector3(0.05f, 1.42f, 0.05f), Brass, false);

            var beam = Child(scale.transform, "Beam", new Vector3(0f, 1.45f, 0f));
            Block(beam, "SM_Kol", Vector3.zero, new Vector3(1.0f, 0.03f, 0.04f), Brass, false);
            var leftHook = Child(beam.transform, "LeftHook", new Vector3(-0.47f, 0f, 0f));
            var rightHook = Child(beam.transform, "RightHook", new Vector3(0.47f, 0f, 0f));
            var leftPan = Child(leftHook.transform, "LeftPan", new Vector3(0f, -0.55f, 0f));
            var rightPan = Child(rightHook.transform, "RightPan", new Vector3(0f, -0.55f, 0f));
            Block(leftPan, "SM_Kefe", new Vector3(0f, -0.01f, 0f), new Vector3(0.42f, 0.02f, 0.18f), Brass, false);
            Block(rightPan, "SM_Kefe", new Vector3(0f, -0.01f, 0f), new Vector3(0.42f, 0.02f, 0.18f), Brass, false);
            Block(leftHook, "SM_Ip", new Vector3(0f, -0.27f, 0f), new Vector3(0.01f, 0.54f, 0.01f), Brass, false);
            Block(rightHook, "SM_Ip", new Vector3(0f, -0.27f, 0f), new Vector3(0.01f, 0.54f, 0.01f), Brass, false);

            var status = WorldText(scale.transform, "Status", new Vector3(0f, 0.35f, -0.2f), new Vector2(1.2f, 0.12f), 0.7f,
                TextAlignmentOptions.Center, Color.white);
            var leftTitle = WorldText(scale.transform, "LeftTitle", new Vector3(-0.47f, 1.62f, 0f), new Vector2(0.5f, 0.08f), 0.45f,
                TextAlignmentOptions.Center, new Color(0.98f, 0.95f, 0.88f));
            leftTitle.text = "Giren (kazan)";
            var rightTitle = WorldText(scale.transform, "RightTitle", new Vector3(0.47f, 1.62f, 0f), new Vector2(0.5f, 0.08f), 0.45f,
                TextAlignmentOptions.Center, new Color(0.98f, 0.95f, 0.88f));
            rightTitle.text = "Çıkan (sipariş)";

            var model = scale.AddComponent<ScaleModel>();
            var so = new SerializedObject(model);
            so.FindProperty("beam").objectReferenceValue = beam.transform;
            so.FindProperty("leftPan").objectReferenceValue = leftPan.transform;
            so.FindProperty("rightPan").objectReferenceValue = rightPan.transform;
            so.FindProperty("atomPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(AtomPath).GetComponent<Renderer>();
            so.FindProperty("statusLabel").objectReferenceValue = status;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CleanDoor()
        {
            var root = PrefabUtility.LoadPrefabContents(DoorPath);
            try
            {
                if (root.transform.Find("CameraPoint") == null) return;
                DestroyChild(root, "CameraPoint");
                PrefabUtility.SaveAsPrefabAsset(root, DoorPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AddBoardText()
        {
            var root = PrefabUtility.LoadPrefabContents(BoardPath);
            try
            {
                if (root.GetComponentInChildren<ChalkboardText>(true) != null) return;
                var text = WorldText(root.transform, "BoardText", new Vector3(0f, 0.55f, -0.51f), new Vector2(0.95f, 0.9f), 0.55f,
                    TextAlignmentOptions.TopLeft, new Color(0.95f, 0.96f, 0.92f));
                text.textWrappingMode = TextWrappingModes.Normal;
                var board = text.gameObject.AddComponent<ChalkboardText>();
                var so = new SerializedObject(board);
                so.FindProperty("text").objectReferenceValue = text;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, BoardPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AddInventoryToRig()
        {
            var root = PrefabUtility.LoadPrefabContents(RigPath);
            try
            {
                var interactor = root.GetComponentInChildren<PlayerInteractor>(true);
                if (interactor == null)
                {
                    Debug.LogError("[BilgiAdasi] PlayerRig'de PlayerInteractor yok; envanter eklenmedi.");
                    return;
                }
                if (!interactor.TryGetComponent<PlayerInventory>(out var inv)) inv = interactor.gameObject.AddComponent<PlayerInventory>();
                var so = new SerializedObject(inv);
                so.FindProperty("economy").objectReferenceValue = Economy;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, RigPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------- Sahne ----------

        private static void SetUpTown()
        {
            var scene = EditorSceneManager.OpenScene(TownPath);

            if (Object.FindAnyObjectByType<Shelf>(FindObjectsInactive.Include) == null)
                Place(ShelfPath, "Shelf", new Vector3(-5.0f, 0f, 3.0f));
            if (Object.FindAnyObjectByType<MarketStall>(FindObjectsInactive.Include) == null)
                Place(MarketPath, "MarketStall", new Vector3(5.0f, 0f, 9.0f));

            var hud = GameObject.Find("_Placeholder/HUD");
            if (hud != null)
            {
                foreach (var t in hud.GetComponentsInChildren<Transform>(true))
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                var oldOrder = hud.transform.Find("OrderView");
                if (oldOrder != null) Object.DestroyImmediate(oldOrder.gameObject);

                if (hud.GetComponentInChildren<PlaceholderWorkshopHud>(true) == null)
                {
                    var go = new GameObject("WorkshopHud", typeof(RectTransform));
                    var rt = (RectTransform)go.transform;
                    rt.SetParent(hud.transform, false);
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = rt.offsetMax = Vector2.zero;
                    go.AddComponent<PlaceholderWorkshopHud>();
                    var dialogue = hud.GetComponentInChildren<PlaceholderDialogueView>(true);
                    if (dialogue != null) dialogue.transform.SetAsLastSibling(); // konuşma en üstte
                }
            }
            else
            {
                Debug.LogWarning("[BilgiAdasi] _Placeholder/HUD bulunamadı; atölye HUD'u eklenmedi.");
            }

            // Runner'ın kapı bağlantısı (DoorStation aynı sınıf; yine de kontrol).
            var runner = Object.FindAnyObjectByType<NeighborhoodRunner>(FindObjectsInactive.Include);
            var door = Object.FindAnyObjectByType<DoorStation>(FindObjectsInactive.Include);
            if (runner != null && door != null)
            {
                var so = new SerializedObject(runner);
                if (so.FindProperty("door").objectReferenceValue == null)
                {
                    so.FindProperty("door").objectReferenceValue = door;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void Place(string prefabPath, string name, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.position = position;
        }

        // ---------- Yardımcılar ----------

        private static readonly Color Wood = new Color(0.54f, 0.35f, 0.23f);   // 09 palet: ahşap orta
        private static readonly Color Roof = new Color(0.91f, 0.38f, 0.16f);   // çatı orta
        private static readonly Color Stone = new Color(0.43f, 0.50f, 0.56f);  // kaya gölge
        private static readonly Color Brass = new Color(0.97f, 0.66f, 0.0f);   // altın vurgu
        private static readonly Color FireColor = new Color(1.0f, 0.48f, 0.24f);
        private static readonly Color MarkerGreen = new Color(0.247f, 0.659f, 0.227f); // yeşil yaprak orta #3FA83A

        private static GameObject Child(Transform parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go;
        }

        private static GameObject Block(GameObject parent, string name, Vector3 localPosition, Vector3 scale, Color color, bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            Tint(go, color);
            return go;
        }

        private static TextMeshPro WorldText(Transform parent, string name, Vector3 localPosition, Vector2 size, float fontSize,
            TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            ((RectTransform)go.transform).sizeDelta = size;
            var t = go.AddComponent<TextMeshPro>();
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = color;
            t.fontStyle = FontStyles.Bold;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.text = string.Empty;
            return t;
        }

        private static void DestroyChild(GameObject root, string name)
        {
            var t = root.transform.Find(name);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        /// <summary>Yer tutucu renk: varsayılan malzemenin kopyası _Placeholder/Materials'a asset olarak (renk başına bir tane).</summary>
        private static void Tint(GameObject go, Color color)
        {
            const string folder = "Assets/_Project/_Placeholder/Materials";
            EnsureFolder("Assets/_Project", "_Placeholder");
            EnsureFolder("Assets/_Project/_Placeholder", "Materials");

            string path = $"{folder}/M_Placeholder_{ColorUtility.ToHtmlStringRGB(color)}.mat";
            var r = go.GetComponent<Renderer>();
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(r.sharedMaterial) { color = color };
                AssetDatabase.CreateAsset(mat, path);
            }
            r.sharedMaterial = mat;
        }

        private static void Save(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}")) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
