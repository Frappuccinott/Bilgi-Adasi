using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using BilgiAdasi.City;
using BilgiAdasi.Order;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Kurulum → 22: atölye önü pazarı (karar 14.11). Açık sahnede (Harita):
    /// 1) Atolye_Pazar prefab'ı kapının batısına konur; köküne ShopCounter + tezgâh kutusu (layer Interactable).
    /// 2) Pazar_Musteriler: yol noktaları, müşteri gövdeleri (sipariş verenler + alıcılar), CustomerQueue.
    /// 3) Alıcı istekleri verisi (Data/Market/MusteriIstekleri.asset) yoksa yazılır.
    /// 4) NeighborhoodRunner.counter bağlanır: sipariş/teslim artık tezgâhta.
    /// 5) Tabela yazısı (TMP, Alegreya Bold, mürekkep rengi).
    /// Tekrar çalıştırılabilir: var olanı bulur, eksiği tamamlar.
    /// </summary>
    public static class MarketFrontSetup
    {
        private const string StallPrefab = "Assets/_Project/Art/Prefablar/Atolye_Pazar.prefab";
        private const string PrefabDir = "Assets/_Project/Art/Prefablar/";
        private const string RequestsPath = "Assets/_Project/Data/Market/MusteriIstekleri.asset";
        private const string SpeciesDir = "Assets/_Project/Data/Species/";
        private const string SignFontPath = "Assets/_Project/Fonts/Alegreya-Bold SDF.asset";
        private const string SignText = "Câbir'in Tezgâhı";

        private static readonly Vector3 StallPos = new Vector3(-2.0f, 0f, 5.3f);
        private static readonly Vector3 Street = new Vector3(-7.5f, 0f, -1.5f);

        // (obje adı, prefab, sipariş veren adı — OrderTask.GiverName, kadın mı)
        private static readonly (string name, string prefab, string giver, bool female)[] Bodies =
        {
            ("Musteri_Yusuf", "NPC_MusteriYusuf", "Muhtar Yusuf", false),
            ("Musteri_Demir", "NPC_MusteriDemir", "Fenerci Demir", false),
            ("Musteri_Zehra", "NPC_MusteriZehra", "Fırıncı Zehra", true),
            ("Musteri_Halil", "NPC_KoyluErkek", "Duvarcı Halil", false),
            ("Musteri_Elif", "NPC_KoyluKadin", "Çiftçi Elif", true),
            ("Alici_Kadin", "NPC_KoyluKadin", "", true),
            ("Alici_Erkek", "NPC_KoyluErkek", "", false),
        };

        [MenuItem("BilgiAdasi/Kurulum/22 - Atölye önü pazarı (tezgâh, müşteriler, satış)", priority = 23)]
        public static void Run()
        {
            var stall = SetupStall(out var counter);
            var queue = SetupQueue(counter, stall);
            Link(counter, "queue", queue);
            var runner = Object.FindAnyObjectByType<NeighborhoodRunner>(FindObjectsInactive.Include);
            if (runner != null) Link(runner, "counter", counter);
            else Debug.LogWarning("[Kurulum 22] NeighborhoodRunner bulunamadı; tezgâh bağlanmadı.");
            SetupSign(stall);
            SetupDisplay(stall);
            EditorSceneManager.MarkSceneDirty(stall.scene);
            Debug.Log("[Kurulum 22] Atölye önü pazarı kuruldu. Sahneyi kaydet.");
        }

        // ---------- 1) tezgâh ----------
        private static GameObject SetupStall(out ShopCounter counter)
        {
            GameObject stall = null;
            foreach (var r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                if (r.name == "Atolye_Pazar") stall = r;
            if (stall == null)
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(StallPrefab);
                stall = (GameObject)PrefabUtility.InstantiatePrefab(pf);
                stall.transform.SetPositionAndRotation(StallPos, Quaternion.identity);
                Undo.RegisterCreatedObjectUndo(stall, "Atölye pazarı");
            }
            counter = GetOrAdd<ShopCounter>(stall);
            stall.layer = LayerMask.NameToLayer("Interactable");
            var box = GetOrAdd<BoxCollider>(stall);
            box.isTrigger = false;
            box.center = new Vector3(0f, 0.47f, 0f);
            box.size = new Vector3(2.46f, 0.94f, 0.78f);

            // Direkler ve bank: sabit çarpışma (oyuncu içinden geçmesin), etkileşimsiz.
            foreach (var mf in stall.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.name == "SM_Pazar_Tezgah") continue;   // tezgâhı kutu tutuyor
                var mc = GetOrAdd<MeshCollider>(mf);
                mc.sharedMesh = mf.sharedMesh;
                mf.gameObject.isStatic = true;
            }
            return stall;
        }

        // ---------- 2) müşteriler ----------
        private static CustomerQueue SetupQueue(ShopCounter counter, GameObject stall)
        {
            var root = GameObject.Find("Pazar_Musteriler");
            if (root == null)
            {
                root = new GameObject("Pazar_Musteriler");
                Undo.RegisterCreatedObjectUndo(root, "Pazar müşterileri");
            }
            var street = Point(root, "Musteri_Sokak", Street);
            var a1 = Point(root, "Musteri_Yaklasma_1", new Vector3(-6.4f, 0f, 3.9f));
            var a2 = Point(root, "Musteri_Yaklasma_2", new Vector3(-5.2f, 0f, 7.5f));
            var wait = Point(root, "Musteri_Bekleme", new Vector3(-4.65f, 0f, 5.75f));   // bankın batısı: tezgâha giden yolun dışında
            var front = Deep(stall.transform, "Musteri_Nokta_1");

            var queue = GetOrAdd<CustomerQueue>(root);
            var so = new SerializedObject(queue);
            so.FindProperty("runner").objectReferenceValue = Object.FindAnyObjectByType<NeighborhoodRunner>(FindObjectsInactive.Include);
            so.FindProperty("orderController").objectReferenceValue = Object.FindAnyObjectByType<OrderController>(FindObjectsInactive.Include);
            so.FindProperty("counter").objectReferenceValue = counter;
            so.FindProperty("street").objectReferenceValue = street;
            so.FindProperty("counterPoint").objectReferenceValue = front;
            so.FindProperty("benchPoint").objectReferenceValue = wait;
            var ap = so.FindProperty("approach");
            ap.arraySize = 2;
            ap.GetArrayElementAtIndex(0).objectReferenceValue = a1;
            ap.GetArrayElementAtIndex(1).objectReferenceValue = a2;

            var list = so.FindProperty("bodies");
            list.arraySize = Bodies.Length;
            for (int i = 0; i < Bodies.Length; i++)
            {
                var b = Bodies[i];
                var t = root.transform.Find(b.name);
                if (t == null)
                {
                    var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + b.prefab + ".prefab");
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, root.transform);
                    go.name = b.name;
                    t = go.transform;
                }
                t.position = Street;
                var el = list.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("giverName").stringValue = b.giver;
                el.FindPropertyRelative("animator").objectReferenceValue = t.GetComponentInChildren<Animator>(true);
                el.FindPropertyRelative("clipPrefix").stringValue = "";
                el.FindPropertyRelative("female").boolValue = b.female;
            }
            so.FindProperty("requests").objectReferenceValue = EnsureRequests();
            so.ApplyModifiedPropertiesWithoutUndo();
            return queue;
        }

        private static Transform Point(GameObject root, string name, Vector3 pos)
        {
            var t = root.transform.Find(name);
            if (t == null)
            {
                t = new GameObject(name).transform;
                t.SetParent(root.transform, false);
            }
            t.position = pos;
            return t;
        }

        // ---------- 3) alıcı istekleri ----------
        private static CustomerRequestSet EnsureRequests()
        {
            var set = AssetDatabase.LoadAssetAtPath<CustomerRequestSet>(RequestsPath);
            if (set != null) return set;
            set = ScriptableObject.CreateInstance<CustomerRequestSet>();
            AssetDatabase.CreateAsset(set, RequestsPath);
            var so = new SerializedObject(set);
            var list = so.FindProperty("requests");
            // Fiyatlar 10-gorev-pazar-npc §2.2 "Takasçı alır" sütunu (H2O için 1). Satıcı gündelik adı söyler.
            var rows = new (string who, bool female, string species, int price, string ask, string thanks)[]
            {
                ("Aşçı Melek", true, "H2O", 1,
                 "Kuyunun suyu bulandı, çorbaya temiz su lazım. Bir kavanoz {0} verir misin? {1} akçe öderim.",
                 "Tertemiz! Bugünkü çorba senden sayılır."),
                ("Ambarcı Kenan", false, "CO2", 1,
                 "Ambarda kıvılcım sıçradı. Ağır hava ateşi boğarmış derler; sende {0} var mı? {1} akçe.",
                 "Bunu kapının yanına koyarım. Ateş sönsün, ambar yanmasın."),
                ("Badanacı Rukiye", true, "Ca(OH)2", 2,
                 "Evin duvarları karardı. Bir kavanoz badana kireci, yani {0} lazım. {1} akçe veririm.",
                 "Duvarlar bembeyaz olacak. Eline sağlık."),
                ("Bostancı Osman", false, "NH3", 3,
                 "Elif'in tarlası yeşerdi, benim bostan bakıyor. Bir kavanoz gübre ruhu ({0}) olur mu? {1} akçe.",
                 "Domatesler sana teşekkür edecek."),
                ("Duvarcı Tamer", false, "CaO", 2,
                 "Harç tutmuyor. Biraz diri kireç, {0}, lazım. Eldivenim de var merak etme. {1} akçe.",
                 "Bununla harç taş gibi olur."),
            };
            list.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                var el = list.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("customerName").stringValue = r.who;
                el.FindPropertyRelative("female").boolValue = r.female;
                el.FindPropertyRelative("product").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<SpeciesData>(SpeciesDir + "Species_" + r.species + ".asset");
                el.FindPropertyRelative("price").intValue = r.price;
                el.FindPropertyRelative("askLine").stringValue = r.ask;
                el.FindPropertyRelative("thanksLine").stringValue = r.thanks;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            return set;
        }

        // ---------- 6) sergi: atölyenin ürettikleri sokağa dönük (yalnız süs; envantere girmez) ----------
        private static readonly string[] DisplayJars = { "Malzeme_H2O", "Malzeme_CO2", "Malzeme_NH3", "Malzeme_CaOH2" };

        private static void SetupDisplay(GameObject stall)
        {
            for (int i = 0; i < DisplayJars.Length; i++)
            {
                var anchor = Deep(stall.transform, "Sergi_" + (i + 1));
                if (anchor == null) continue;
                for (int k = anchor.childCount - 1; k >= 0; k--) Object.DestroyImmediate(anchor.GetChild(k).gameObject);
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + DisplayJars[i] + ".prefab");
                if (pf == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, anchor);
                go.transform.localPosition = Vector3.zero;
                go.transform.rotation = Quaternion.Euler(0f, 180f + (i * 37 % 30) - 15f, 0f);
                // Sergideki kavanoz alınmaz: collider'ları kapat (raf/envanter etkileşimine karışmasın).
                foreach (var col in go.GetComponentsInChildren<Collider>(true)) col.enabled = false;
            }
        }

        // ---------- 5) tabela ----------
        private static void SetupSign(GameObject stall)
        {
            var anchor = Deep(stall.transform, "Tabela_Yazi_Pazar");
            if (anchor == null) return;
            for (int i = anchor.childCount - 1; i >= 0; i--) Object.DestroyImmediate(anchor.GetChild(i).gameObject);
            var go = new GameObject("Yazi_On");
            go.transform.SetParent(anchor, false);
            // Levha sokağa (+Z) bakar; TMP önü -forward'dan okunur.
            go.transform.SetPositionAndRotation(anchor.position + stall.transform.forward * 0.01f, Quaternion.LookRotation(-stall.transform.forward, Vector3.up));
            var tmp = go.AddComponent<TextMeshPro>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SignFontPath);
            if (font != null) tmp.font = font;
            tmp.text = SignText;
            tmp.color = new Color(0.290f, 0.204f, 0.153f);   // mürekkep orta #4A3427
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.5f;
            tmp.fontSizeMax = 1.6f;
            tmp.rectTransform.sizeDelta = new Vector2(0.9f, 0.16f);
        }

        private static Transform Deep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        private static T GetOrAdd<T>(Component c) where T : Component => GetOrAdd<T>(c.gameObject);

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var x = go.GetComponent<T>();
            return x != null ? x : go.AddComponent<T>();
        }

        private static void Link(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[Kurulum 22] {target.GetType().Name}.{field} yok."); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
