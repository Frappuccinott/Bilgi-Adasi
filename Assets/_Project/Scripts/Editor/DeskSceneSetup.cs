using BilgiAdasi.City;
using BilgiAdasi.Desk;
using BilgiAdasi.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Masa görevlerini (Seviye 6–10) açık sahneye bağlar (harita sahnesi: Deneme_Mustafa_Harita):
    /// Station_Desk'e DeskController, masa sanatı (güvercin, zarflar, açık kâğıt) + ArtDeskView, HUD'a kâğıt view'ı,
    /// NeighborhoodRunner'a Tepkime Türleri köyü, Tepkimeler bariyeri Denkleştirme bitince kalkar.
    /// Tekrar çalıştırmak güvenlidir (var olanı günceller).
    /// </summary>
    public static class DeskSceneSetup
    {
        private const string ArtFolder = "Assets/_Project/Art/Atolye/MasaGorev/";
        private const string PropsRootName = "MasaGorev_Sanat";

        // Atölye ölçüleri (atolye_esya.py: masa, atolye_kabuk.py: masanın yanındaki pencere), Unity dünya konumu.
        private static readonly Vector3 PerchPosition = new Vector3(4.64f, 0.975f, -0.30f);   // dış pervaz (parmaklığın dışı)
        private static readonly Vector3 SkyPosition = new Vector3(9.5f, 4.2f, 2.5f);
        private static readonly Vector3 EnvelopePosition = new Vector3(3.58f, 0.815f, -0.52f);  // masadaki kâğıdın üstü

        [MenuItem("BilgiAdasi/Kurulum/16 - Masa görevleri (Seviye 6–10) sahneye bağla", priority = 16)]
        public static void Run()
        {
            var desk = GameObject.Find("Station_Desk");
            if (desk == null)
            {
                EditorUtility.DisplayDialog("Masa görevleri", "Açık sahnede Station_Desk yok.", "Tamam");
                return;
            }

            SetUpDesk(desk);
            SetUpProps();
            SetUpPaperView();
            SetUpRunner(desk.GetComponent<DeskController>());
            UnlockVillage();

            var scene = desk.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Masa] Masa görevleri sahneye bağlandı: " + scene.path);
        }

        private static void SetUpDesk(GameObject desk)
        {
            var controller = desk.GetComponent<DeskController>() ?? desk.AddComponent<DeskController>();
            var so = new SerializedObject(controller);
            so.FindProperty("texts").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DeskTexts>("Assets/_Project/Data/Texts/DeskTexts.asset");
            so.ApplyModifiedPropertiesWithoutUndo();

            var station = new SerializedObject(desk.GetComponent<DeskStation>());
            station.FindProperty("promptText").stringValue = "Masaya otur";
            station.ApplyModifiedPropertiesWithoutUndo();

            // Atölyenin sanat masası (SM_Masa) var: yer tutucu küp gizlenir.
            var placeholder = desk.transform.Find("Visual/_Placeholder");
            if (placeholder != null && GameObject.Find("SM_Masa") != null) placeholder.gameObject.SetActive(false);
        }

        private static void SetUpProps()
        {
            var parent = GameObject.Find("Atolye_Sanat");
            var rootT = parent != null ? parent.transform.Find(PropsRootName) : null;
            var root = rootT != null ? rootT.gameObject : GameObject.Find(PropsRootName);
            if (root == null)
            {
                root = new GameObject(PropsRootName);
                if (parent != null) root.transform.SetParent(parent.transform, false);
            }
            var view = root.GetComponent<ArtDeskView>() ?? root.AddComponent<ArtDeskView>();

            var perch = Child(root.transform, "Guvercin_Pervaz", PerchPosition, Quaternion.Euler(0f, -90f, 0f));
            var sky = Child(root.transform, "Guvercin_Gok", SkyPosition, Quaternion.identity);

            var pigeon = Instance(root.transform, "Guvercin", PerchPosition, Quaternion.Euler(0f, -90f, 0f));
            var envelope = Instance(root.transform, "Zarf", EnvelopePosition, Quaternion.Euler(0f, 78f, 0f));
            var guild = Instance(root.transform, "Zarf_Lonca", EnvelopePosition, Quaternion.Euler(0f, 78f, 0f));
            var paperPoint = GameObject.Find("Masa_Kagit_Noktasi");
            var paperPos = paperPoint != null ? paperPoint.transform.position + new Vector3(0f, 0.006f, 0f) : EnvelopePosition;
            var paper = Instance(root.transform, "Masa_Acik_Kagit", paperPos, Quaternion.Euler(0f, 86f, 0f));

            var so = new SerializedObject(view);
            so.FindProperty("pigeon").objectReferenceValue = pigeon.transform;
            so.FindProperty("wingLeft").objectReferenceValue = Find(pigeon.transform, "Kanat_Sol");
            so.FindProperty("wingRight").objectReferenceValue = Find(pigeon.transform, "Kanat_Sag");
            so.FindProperty("perch").objectReferenceValue = perch;
            so.FindProperty("skyPoint").objectReferenceValue = sky;
            so.FindProperty("envelope").objectReferenceValue = envelope;
            so.FindProperty("guildEnvelope").objectReferenceValue = guild;
            so.FindProperty("openPaper").objectReferenceValue = paper;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Sahnede başta görünmesinler (ArtDeskView Awake de gizler).
            pigeon.SetActive(false);
            envelope.SetActive(false);
            guild.SetActive(false);
            paper.SetActive(false);
        }

        private static void SetUpPaperView()
        {
            var hud = GameObject.Find("HUD");
            if (hud == null)
            {
                Debug.LogWarning("[Masa] HUD kanvası yok; kâğıt view'ı eklenmedi.");
                return;
            }
            if (hud.GetComponentInChildren<FigmaPaperView>(true) != null) return;
            var go = new GameObject("PaperView", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(hud.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();
            go.AddComponent<FigmaPaperView>();
        }

        private static void SetUpRunner(DeskController desk)
        {
            var runner = Object.FindAnyObjectByType<NeighborhoodRunner>(FindObjectsInactive.Include);
            if (runner == null) return;
            var tepkime = AssetDatabase.LoadAssetAtPath<NeighborhoodData>("Assets/_Project/Data/Neighborhoods/Neighborhood_tepkime_turleri.asset");
            var so = new SerializedObject(runner);
            var list = so.FindProperty("neighborhoods");
            bool has = false;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == tepkime) has = true;
            if (!has && tepkime != null)
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = tepkime;
            }
            so.FindProperty("desk").objectReferenceValue = desk;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void UnlockVillage()
        {
            var zone = GameObject.Find("LockedZone_Tepkimeler");
            if (zone == null || !zone.TryGetComponent<LockedZone>(out var lz)) return;
            var so = new SerializedObject(lz);
            so.FindProperty("unlockedBy").stringValue = "denklestirme";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- yardımcılar ----------

        private static Transform Child(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            var t = parent.Find(name);
            if (t == null)
            {
                t = new GameObject(name).transform;
                t.SetParent(parent, false);
            }
            t.SetPositionAndRotation(position, rotation);
            return t;
        }

        private static GameObject Instance(Transform parent, string fbx, Vector3 position, Quaternion rotation)
        {
            var existing = parent.Find(fbx);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ArtFolder + fbx + ".fbx");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.name = fbx;
            go.transform.SetPositionAndRotation(position, rotation);
            return go;
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root)
            {
                var f = Find(c, name);
                if (f != null) return f;
            }
            return null;
        }
    }
}
