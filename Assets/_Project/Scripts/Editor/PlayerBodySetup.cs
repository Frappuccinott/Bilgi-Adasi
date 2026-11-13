using BilgiAdasi.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Oyuncunun görünen gövdesi (ogrenci_govde.py → Ogrenci_Govde_Rig.fbx): oyuncu Animator controller'ı
    /// (yer karışımı + havada + bisiklet), PlayerRig'de Player'ın çocuğu "Govde" + PlayerBodyView, bisiklette
    /// sele noktası (Visual/Sele_Oturma) ve SeatAnchor (oturan sürücünün gözü). Tekrar çalıştırılabilir.
    /// Kol rig'ine (Ogrenci_Kol_Rig) dokunmaz.
    /// </summary>
    public static class PlayerBodySetup
    {
        private const string FbxPath = "Assets/_Project/Art/Animasyon/Ogrenci_Govde_Rig.fbx";
        private const string BodyPrefabPath = "Assets/_Project/Art/Prefablar/Ogrenci_Govde_Rig.prefab";
        private const string ControllerPath = "Assets/_Project/Art/Animasyon/Kontrol/Ogrenci_Govde_Oyuncu.controller";
        private const string PlayerRigPath = "Assets/_Project/Prefabs/PlayerRig.prefab";
        private const string BicyclePrefabPath = "Assets/_Project/Prefabs/Bicycle.prefab";
        private const string BodyName = "Govde";
        private const string SeatName = "Sele_Oturma";

        // bisiklet.py: sele merkezi y −0,08 (Blender, + arka), üst 0,82; kalça eklemi oturma yüzeyinin 0,08 üstünde,
        // selenin geniş arka kısmının üstünde (+0,07). Unity: z = −y.
        private static readonly Vector3 SeatInModel = new Vector3(0f, 0.90f, 0.01f);
        // Oturuş pozunda (Govde_Bisiklet_Dongu, gövde ~9° öne) göz kalçaya göre: yukarı 0,69, ileri 0,08.
        private static readonly Vector3 EyeFromHip = new Vector3(0f, 0.69f, 0.08f);
        private const float EyeHeight = 1.56f; // ayakta göz (CLAUDE.md §7, 10. sınıf)

        [MenuItem("BilgiAdasi/Kurulum/15 - Oyuncu gövdesi (bacaklar, bisiklette sele/pedal)", priority = 14)]
        public static void Setup()
        {
            var controller = BuildController();
            if (controller == null) return;
            SetupPlayerRig(controller);
            SetupBicycle();
        }

        private static AnimationClip Clip(string name)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(FbxPath))
                if (o is AnimationClip c && c.name == name) return c;
            Debug.LogError($"[BilgiAdasi] {FbxPath} içinde klip yok: {name}");
            return null;
        }

        private static AnimatorController BuildController()
        {
            var idle = Clip("Govde_Bekle_Dongu");
            var walk = Clip("Govde_Yuru_Dongu");
            var crouch = Clip("Govde_Egil_Dongu");
            var crouchWalk = Clip("Govde_Egil_Yuru_Dongu");
            var air = Clip("Govde_Havada_Dongu");
            var bike = Clip("Govde_Bisiklet_Dongu");
            if (idle == null || walk == null || crouch == null || crouchWalk == null || air == null || bike == null) return null;

            AssetDatabase.DeleteAsset(ControllerPath); // her seferinde temiz kurulur
            var c = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            c.AddParameter("Hiz", AnimatorControllerParameterType.Float);
            c.AddParameter("Comel", AnimatorControllerParameterType.Float);
            c.AddParameter("Tempo", AnimatorControllerParameterType.Float);
            var ps = c.parameters; // kopya döner: değiştirip geri yazılır
            for (int i = 0; i < ps.Length; i++) if (ps[i].name == "Tempo") ps[i].defaultFloat = 1f;
            c.parameters = ps;

            var sm = c.layers[0].stateMachine;
            var tree = new BlendTree
            {
                name = "Yer",
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = "Hiz",
                blendParameterY = "Comel",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(tree, c);
            tree.AddChild(idle, new Vector2(0f, 0f));
            tree.AddChild(walk, new Vector2(1f, 0f));
            tree.AddChild(crouch, new Vector2(0f, 1f));
            tree.AddChild(crouchWalk, new Vector2(1f, 1f));

            var ground = sm.AddState("Yer");
            ground.motion = tree;
            ground.speedParameter = "Tempo";
            ground.speedParameterActive = true;
            ground.writeDefaultValues = true;
            var airState = sm.AddState("Havada");
            airState.motion = air;
            airState.writeDefaultValues = true;
            var bikeState = sm.AddState("Bisiklet");
            bikeState.motion = bike;
            bikeState.writeDefaultValues = true;
            sm.defaultState = ground;
            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
            return c;
        }

        private static void SetupPlayerRig(RuntimeAnimatorController controller)
        {
            var bodyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BodyPrefabPath);
            if (bodyPrefab == null) { Debug.LogError($"[BilgiAdasi] Gövde prefab'ı yok: {BodyPrefabPath} (FBX içe aktarıldı mı?)"); return; }

            var root = PrefabUtility.LoadPrefabContents(PlayerRigPath);
            try
            {
                var motor = root.GetComponentInChildren<PlayerMotor>(true);
                if (motor == null) { Debug.LogError("[BilgiAdasi] PlayerRig'de PlayerMotor yok."); return; }
                var old = motor.transform.Find(BodyName);
                if (old != null) Object.DestroyImmediate(old.gameObject);

                var body = (GameObject)PrefabUtility.InstantiatePrefab(bodyPrefab, motor.transform);
                body.name = BodyName;
                body.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                var animator = body.GetComponentInChildren<Animator>(true);
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                Renderer bodyRenderer = null;
                foreach (var r in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    r.updateWhenOffscreen = true; // kamera gövdenin içinde: sınır kutusu yüzünden kaybolmasın
                    if (r.name == "Ogrenci_Golge_Bas")
                    {
                        r.shadowCastingMode = ShadowCastingMode.ShadowsOnly; // gölge başsız olmasın, kamerada görünmez
                    }
                    else
                    {
                        r.shadowCastingMode = ShadowCastingMode.On;
                        bodyRenderer = r;
                    }
                }

                var view = body.GetComponent<PlayerBodyView>() ?? body.AddComponent<PlayerBodyView>();
                var so = new SerializedObject(view);
                so.FindProperty("animator").objectReferenceValue = animator;
                so.FindProperty("motor").objectReferenceValue = motor;
                so.FindProperty("look").objectReferenceValue = motor.GetComponent<PlayerLook>();
                so.FindProperty("hips").objectReferenceValue = Find(body.transform, "Kalca");
                so.FindProperty("thighLeft").objectReferenceValue = Find(body.transform, "Bacak_Ust_Sol");
                so.FindProperty("shinLeft").objectReferenceValue = Find(body.transform, "Bacak_Alt_Sol");
                so.FindProperty("footLeft").objectReferenceValue = Find(body.transform, "Ayak_Sol");
                so.FindProperty("thighRight").objectReferenceValue = Find(body.transform, "Bacak_Ust_Sag");
                so.FindProperty("shinRight").objectReferenceValue = Find(body.transform, "Bacak_Alt_Sag");
                so.FindProperty("footRight").objectReferenceValue = Find(body.transform, "Ayak_Sag");
                var rs = so.FindProperty("bodyRenderers");
                rs.arraySize = bodyRenderer != null ? 1 : 0;
                if (bodyRenderer != null) rs.GetArrayElementAtIndex(0).objectReferenceValue = bodyRenderer;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerRigPath);
                Debug.Log("[BilgiAdasi] PlayerRig: Player/Govde (öğrenci gövdesi + PlayerBodyView) eklendi.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void SetupBicycle()
        {
            var root = PrefabUtility.LoadPrefabContents(BicyclePrefabPath);
            try
            {
                var rig = root.GetComponentInChildren<BicycleRig>(true);
                if (rig == null) { Debug.LogError("[BilgiAdasi] Bicycle.prefab'da BicycleRig yok."); return; }
                var visual = rig.transform;
                var model = visual.Find("Model_Bisiklet");
                Transform space = model != null ? model : visual;

                var seat = visual.Find(SeatName);
                if (seat == null)
                {
                    seat = new GameObject(SeatName).transform;
                    seat.SetParent(visual, false);
                }
                seat.position = space.TransformPoint(SeatInModel);
                seat.localRotation = Quaternion.identity;
                var so = new SerializedObject(rig);
                so.FindProperty("seat").objectReferenceValue = seat;
                so.ApplyModifiedPropertiesWithoutUndo();

                // Kamera: oturan sürücünün gözü (SeatAnchor = göz − ayakta göz yüksekliği; yatmaz, Visual dışında).
                var anchor = root.transform.Find("SeatAnchor");
                if (anchor != null)
                {
                    Vector3 hip = root.transform.InverseTransformPoint(seat.position);
                    anchor.localPosition = hip + EyeFromHip - Vector3.up * EyeHeight;
                }
                PrefabUtility.SaveAsPrefabAsset(root, BicyclePrefabPath);
                Debug.Log($"[BilgiAdasi] Bicycle.prefab: {SeatName} {root.transform.InverseTransformPoint(seat.position)}, " +
                          $"SeatAnchor {(anchor != null ? anchor.localPosition.ToString() : "yok")}.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static Transform Find(Transform parent, string name)
        {
            foreach (var t in parent.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }
    }
}
