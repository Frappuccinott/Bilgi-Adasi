using BilgiAdasi.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Sürülebilir Simya Traktörü (16-traktor-tarla-mekanikleri, adım 2). Açık sahnedeki duran traktör kopyasının
    /// yerine Traktor.fbx'ten sürülebilir traktör kurar: CharacterController + Tractor + TractorRig, bakma hacmi,
    /// SeatAnchor. Eski duran kopya silinmez, kapatılır ("_Eski" adıyla). Sahneyi kaydetmez.
    /// Tekrar çalıştırılırsa önceki kurulumu silip yeniden kurar.
    /// </summary>
    public static class TractorSetup
    {
        private const string SettingsPath = "Assets/_Project/Data/Player/TractorSettings.asset";
        private const string ModelPath = "Assets/_Project/Art/Traktor/Traktor.fbx";
        private const string RootName = "Tractor";
        private const string OldName = "Traktor";
        private const float EyeHeight = 1.56f;   // CLAUDE.md §7, 10. sınıf
        private const float SkinWidth = 0.04f;

        [MenuItem("BilgiAdasi/Kurulum/23 - Traktör: sürülebilir (Sürüm 2)", priority = 40)]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Data/Player")) AssetDatabase.CreateFolder("Assets/_Project/Data", "Player");
            var settings = AssetDatabase.LoadAssetAtPath<TractorSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<TractorSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) { Debug.LogError("[BilgiAdasi] Traktor.fbx bulunamadı: " + ModelPath); return; }

            // Konum: önceki kurulum ya da sahnedeki duran kopya.
            Transform parent = null;
            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            var existing = FindInScene(RootName, t => t.GetComponent<Tractor>() != null);
            var old = FindInScene(OldName, t => t.GetComponent<Tractor>() == null && t.parent != null
                                               && t.parent.GetComponent<TractorRig>() == null && t.gameObject.activeSelf);
            if (existing != null)
            {
                parent = existing.parent;
                pos = existing.position;
                rot = existing.rotation;
                Undo.DestroyObjectImmediate(existing.gameObject);
            }
            else if (old != null)
            {
                parent = old.parent;
                pos = old.position;
                rot = Quaternion.Euler(0f, old.eulerAngles.y, 0f);
                Undo.RecordObject(old.gameObject, "Traktör kur");
                old.gameObject.name = OldName + "_Eski";
                old.gameObject.SetActive(false);
            }
            else Debug.LogWarning("[BilgiAdasi] Sahnede duran traktör bulunamadı; traktör orijine kuruldu.");

            int interactable = ProjectSetup.InteractableLayer;
            var root = new GameObject(RootName) { layer = interactable };
            Undo.RegisterCreatedObjectUndo(root, "Traktör kur");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(pos, rot);

            var cc = root.AddComponent<CharacterController>();
            cc.skinWidth = SkinWidth;
            cc.radius = settings.ColliderRadius;
            cc.height = settings.ColliderHeight;
            cc.center = new Vector3(0f, settings.ColliderHeight * 0.5f, settings.ColliderCenterZ);
            cc.slopeLimit = settings.SlopeLimit;
            cc.stepOffset = settings.StepOffset;

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            visual.name = "Visual";
            visual.transform.localPosition = new Vector3(0f, -SkinWidth, 0f);
            visual.transform.localRotation = Quaternion.identity;
            Transform v = visual.transform;

            Transform rear = v.Find("Traktor_Arka_Tekerler");
            Transform axle = v.Find("Traktor_On_Aks");
            Transform front = v.Find("Traktor_On_Tekerler");
            Transform steering = v.Find("Traktor_Direksiyon");
            Transform flywheel = v.Find("Traktor_Volan");
            Transform gripL = v.Find("Traktor_Tutma_Sol");
            Transform gripR = v.Find("Traktor_Tutma_Sag");
            Transform seat = v.Find("Traktor_Sele_Oturma");
            Transform footL = v.Find("Traktor_Ayak_Sol");
            Transform footR = v.Find("Traktor_Ayak_Sag");
            Transform cam = v.Find("Traktor_Koltuk_Kamera");
            // Ön tekerler aksla birlikte sapar; eller simitle birlikte döner (FBX'te köke bağlı gelirler).
            if (front != null && axle != null) front.SetParent(axle, true);
            if (steering != null)
            {
                if (gripL != null) { gripL.SetParent(steering, true); OrientGrip(gripL, steering, root.transform, false); }
                if (gripR != null) { gripR.SetParent(steering, true); OrientGrip(gripR, steering, root.transform, true); }
            }

            // Bakma + gövde hacmi: modelin sınırları (yürüyen oyuncu traktörün içinden geçmesin).
            var bounds = LocalBounds(root.transform, visual);
            var volume = new GameObject("InteractVolume") { layer = interactable };
            volume.transform.SetParent(root.transform, false);
            var box = volume.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;

            var anchor = new GameObject("SeatAnchor").transform;
            anchor.SetParent(root.transform, false);
            Vector3 eye = cam != null ? root.transform.InverseTransformPoint(cam.position) : new Vector3(0f, 1.95f, -1.55f);
            anchor.localPosition = new Vector3(eye.x, eye.y - EyeHeight, eye.z);

            var rig = visual.AddComponent<TractorRig>();
            var so = new SerializedObject(rig);
            so.FindProperty("rearWheels").objectReferenceValue = rear;
            so.FindProperty("frontWheels").objectReferenceValue = front;
            so.FindProperty("frontAxle").objectReferenceValue = axle;
            so.FindProperty("steeringWheel").objectReferenceValue = steering;
            so.FindProperty("flywheel").objectReferenceValue = flywheel;
            so.FindProperty("pitchRoot").objectReferenceValue = v;
            so.FindProperty("gripLeft").objectReferenceValue = gripL;
            so.FindProperty("gripRight").objectReferenceValue = gripR;
            so.FindProperty("seat").objectReferenceValue = seat;
            so.FindProperty("footLeft").objectReferenceValue = footL;
            so.FindProperty("footRight").objectReferenceValue = footR;
            so.ApplyModifiedPropertiesWithoutUndo();

            var tractor = root.AddComponent<Tractor>();
            var to = new SerializedObject(tractor);
            to.FindProperty("settings").objectReferenceValue = settings;
            to.FindProperty("rig").objectReferenceValue = rig;
            to.FindProperty("seatAnchor").objectReferenceValue = anchor;
            to.FindProperty("obstacleMask").intValue = ~(1 << ProjectSetup.PlayerLayer);
            to.ApplyModifiedPropertiesWithoutUndo();

            string missing = "";
            foreach (var (n, t) in new[] { ("arka teker", rear), ("ön aks", axle), ("ön teker", front), ("direksiyon", steering),
                         ("volan", flywheel), ("tutma sol", gripL), ("tutma sağ", gripR), ("koltuk kamerası", cam) })
                if (t == null) missing += n + ", ";
            if (missing.Length > 0) Debug.LogWarning("[BilgiAdasi] Traktör modelinde eksik parça: " + missing);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            AssetDatabase.SaveAssets();
            Debug.Log("[BilgiAdasi] Traktör kuruldu (sürülebilir). Sahne kaydedilmedi; denedikten sonra kendin kaydet.");
        }

        /// <summary>
        /// Tutma noktasının dönüşü = elin hedef dönüşü (RiderArmsIK). El kemiğinde Y parmaklara, Z elin sırtına;
        /// sağ elde +X, sol elde −X başparmak (RiderArmsSetup.Grip ile aynı kural). Simit saat 9 ve 3'ten tutulur:
        /// parmaklar simidi önden sarar (sürücüden uzağa, simit kenarına dik), elin sırtı simidin dışına bakar,
        /// başparmak simit boyunca uzanır (yönü elin sırtı dışa bakacak şekilde seçilir; el aynalanmaz).
        /// </summary>
        private static void OrientGrip(Transform grip, Transform steering, Transform root, bool rightHand)
        {
            Vector3 n = steering.TransformDirection(SteeringAxis).normalized;          // simit ekseni (sürücüye ve yukarı)
            Vector3 radial = Vector3.ProjectOnPlane(grip.position - steering.position, n).normalized; // merkezden ele
            Vector3 tangent = Vector3.Cross(n, radial).normalized;                      // simit kenarı boyunca
            Vector3 fingers = Vector3.ProjectOnPlane(-n * 0.8f - radial * 0.6f, tangent).normalized; // önden ve içe sarar
            for (int pass = 0; pass < 2; pass++)
            {
                Vector3 thumb = pass == 0 ? tangent : -tangent;
                Vector3 x = rightHand ? thumb : -thumb;
                Vector3 back = Vector3.Cross(x, fingers);
                if (pass == 0 && Vector3.Dot(back, radial + n * 0.3f) < 0f) continue;  // sırt içe bakıyor: başparmağı çevir
                grip.rotation = Quaternion.LookRotation(back, fingers);
                return;
            }
        }

        private static readonly Vector3 SteeringAxis = new Vector3(0f, 0.423f, -0.906f); // TractorRig ile aynı

        private static Transform FindInScene(string name, System.Func<Transform, bool> filter)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == name && filter(t)) return t;
            return null;
        }

        private static Bounds LocalBounds(Transform root, GameObject visual)
        {
            bool any = false;
            var b = new Bounds();
            foreach (var r in visual.GetComponentsInChildren<MeshRenderer>())
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = new Vector3((i & 1) == 0 ? mb.min.x : mb.max.x, (i & 2) == 0 ? mb.min.y : mb.max.y,
                        (i & 4) == 0 ? mb.min.z : mb.max.z);
                    var p = root.InverseTransformPoint(r.transform.TransformPoint(c));
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                    else b.Encapsulate(p);
                }
            }
            // Zemine sürtmesin: alt 0.25 m boş (CharacterController zemini taşır).
            var min = b.min;
            min.y = Mathf.Max(min.y, 0.25f);
            b.SetMinMax(min, b.max);
            return b;
        }
    }
}
