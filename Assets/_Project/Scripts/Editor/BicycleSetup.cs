using BilgiAdasi.City;
using BilgiAdasi.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Bisiklet: ayar asset'i + küplerden yer tutucu model ile Bicycle prefab'ı; açık sahneye bir tane koyar (kaydetmez).
    /// Gerçek model gelince: prefab'da Visual/_Placeholder silinir, model Visual altına konur,
    /// BicycleRig'teki parça referansları (teker, gidon, aynakol, pedal) yeniden bağlanır. Bicycle'a dokunulmaz.
    /// Var olan asset ve prefab'ın üzerine yazmaz.
    /// </summary>
    public static class BicycleSetup
    {
        private const string SettingsPath = "Assets/_Project/Data/Player/BicycleSettings.asset";
        private const string PrefabPath = "Assets/_Project/Prefabs/Bicycle.prefab";
        private const string PlaceholderName = "_Placeholder";

        // Ölçüler (bisiklet yereli: +Z ileri, +X sağ, orijin iki teker arasında yerde).
        private const float WheelRadius = 0.34f;
        private const float WheelBase = 1.05f;
        private const float SteerTilt = 18f;            // furş borusu geriye yatık
        private static readonly Vector3 HeadTop = new Vector3(0f, 0.98f, 0.37f);
        private static readonly Vector3 BottomBracket = new Vector3(0f, 0.29f, -0.05f);
        private static readonly Vector3 SeatTop = new Vector3(0f, 0.86f, -0.22f);
        // Göz ≈ 1.40 m (1.56 − 0.16), selenin üstünde: gidon ekranın altında görünür.
        private static readonly Vector3 SeatAnchorPosition = new Vector3(0f, -0.16f, -0.25f);
        private const float SkinWidth = 0.03f;

        [MenuItem("BilgiAdasi/Kurulum/9 - Bisiklet (küp yer tutucu)", priority = 8)]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Data/Player")) AssetDatabase.CreateFolder("Assets/_Project/Data", "Player");

            var settings = AssetDatabase.LoadAssetAtPath<BicycleSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<BicycleSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) prefab = BuildPrefab(settings);
            else Debug.Log("[BilgiAdasi] Bicycle.prefab zaten var, yeniden kurulmadı (yeniden kurmak için önce sil).");

            PlaceInActiveScene(prefab);
            AssetDatabase.SaveAssets();
            Debug.Log("[BilgiAdasi] Bisiklet kuruldu. Sahne kaydedilmedi: konumu ayarlayıp kendin kaydet.");
        }

        // ---------- Prefab ----------

        private static GameObject BuildPrefab(BicycleSettings settings)
        {
            int interactable = ProjectSetup.InteractableLayer;
            var root = new GameObject("Bicycle") { layer = interactable };

            var cc = root.AddComponent<CharacterController>();
            cc.radius = 0.3f;
            cc.height = settings.ParkedHeight;
            cc.center = new Vector3(0f, settings.ParkedHeight * 0.5f, 0f);
            cc.skinWidth = SkinWidth;
            cc.slopeLimit = settings.SlopeLimit;
            cc.stepOffset = settings.StepOffset;

            // Bakma hacmi: ince kapsül tekerleri kapsamaz, crosshair bisikletin her yerinde tutsun.
            var volume = new GameObject("InteractVolume") { layer = interactable };
            volume.transform.SetParent(root.transform, false);
            var box = volume.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.55f, 0.05f);
            box.size = new Vector3(0.3f, 1.0f, 1.75f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, -SkinWidth, 0f); // kapsül deri payı kadar havada durmasın
            var rig = visual.AddComponent<BicycleRig>();

            var parts = BuildPlaceholderModel(visual.transform);

            var seat = new GameObject("SeatAnchor");
            seat.transform.SetParent(root.transform, false);
            seat.transform.localPosition = SeatAnchorPosition;

            var bicycle = root.AddComponent<Bicycle>();
            Set(bicycle, "settings", settings);
            Set(bicycle, "rig", rig);
            Set(bicycle, "seatAnchor", seat.transform);
            SetInt(bicycle, "obstacleMask", ~(1 << ProjectSetup.PlayerLayer));

            Set(rig, "frontWheel", parts.FrontWheel);
            Set(rig, "rearWheel", parts.RearWheel);
            Set(rig, "steering", parts.Steering);
            Set(rig, "crank", parts.Crank);
            Set(rig, "pedalLeft", parts.PedalLeft);
            Set(rig, "pedalRight", parts.PedalRight);
            Set(rig, "leanRoot", visual.transform);
            SetFloat(rig, "wheelRadius", WheelRadius);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private struct RigParts
        {
            public Transform FrontWheel, RearWheel, Steering, Crank, PedalLeft, PedalRight;
        }

        /// <summary>Küplerden bisiklet. Dönen her parçanın pivotu kendi dönme noktasında (göbek, furş, orta göbek).</summary>
        private static RigParts BuildPlaceholderModel(Transform visual)
        {
            var parts = new RigParts();
            var model = new GameObject(PlaceholderName).transform;
            model.SetParent(visual, false);

            // Furş ekseni (aşağı yönü) ve ön göbek.
            Vector3 steerDown = Quaternion.Euler(-SteerTilt, 0f, 0f) * Vector3.down;
            float forkLength = (HeadTop.y - WheelRadius) / -steerDown.y;
            Vector3 frontHub = HeadTop + steerDown * forkLength;
            Vector3 rearHub = new Vector3(0f, WheelRadius, frontHub.z - WheelBase);
            Vector3 headBottom = HeadTop + steerDown * 0.15f;
            Vector3 saddle = new Vector3(0f, 0.95f, -0.25f);

            // --- Kadro (sabit) ---
            const float tube = 0.035f;
            Bar(model, "TopTube", SeatTop, HeadTop, tube);
            Bar(model, "DownTube", BottomBracket, headBottom, tube * 1.2f);
            Bar(model, "SeatTube", BottomBracket, SeatTop + Vector3.up * 0.03f, tube);
            Bar(model, "HeadTube", HeadTop, headBottom, tube * 1.4f);
            Bar(model, "SeatPost", SeatTop, saddle + Vector3.down * 0.02f, tube * 0.8f);
            for (int s = -1; s <= 1; s += 2)
            {
                var x = new Vector3(0.06f * s, 0f, 0f);
                Bar(model, s < 0 ? "ChainStay_L" : "ChainStay_R", BottomBracket + x * 0.6f, rearHub + x, tube * 0.7f);
                Bar(model, s < 0 ? "SeatStay_L" : "SeatStay_R", SeatTop + x * 0.4f, rearHub + x, tube * 0.6f);
            }
            Box(model, "Saddle", saddle, new Vector3(0.14f, 0.05f, 0.26f), Quaternion.identity);

            // --- Arka teker ---
            parts.RearWheel = Pivot(model, "RearWheel", rearHub, Quaternion.identity);
            BuildWheel(parts.RearWheel);

            // --- Aynakol ve pedallar ---
            parts.Crank = Pivot(model, "Crank", BottomBracket, Quaternion.identity);
            Ring(parts.Crank, "Chainring", new Vector3(0.04f, 0f, 0f), 0.1f, 10, new Vector3(0.012f, 0.02f, 0.07f));
            Box(parts.Crank, "Axle", Vector3.zero, new Vector3(0.16f, 0.03f, 0.03f), Quaternion.identity);
            const float arm = 0.17f;
            Bar(parts.Crank, "Arm_R", new Vector3(0.08f, 0f, 0f), new Vector3(0.08f, -arm, 0f), 0.025f);
            Bar(parts.Crank, "Arm_L", new Vector3(-0.08f, 0f, 0f), new Vector3(-0.08f, arm, 0f), 0.025f);
            parts.PedalRight = Pivot(parts.Crank, "Pedal_R", new Vector3(0.14f, -arm, 0f), Quaternion.identity);
            parts.PedalLeft = Pivot(parts.Crank, "Pedal_L", new Vector3(-0.14f, arm, 0f), Quaternion.identity);
            Box(parts.PedalRight, "Pedal", Vector3.zero, new Vector3(0.1f, 0.02f, 0.07f), Quaternion.identity);
            Box(parts.PedalLeft, "Pedal", Vector3.zero, new Vector3(0.1f, 0.02f, 0.07f), Quaternion.identity);

            // --- Gidon grubu (furş ekseni etrafında döner): çatal, gidon, ön teker ---
            parts.Steering = Pivot(model, "Steering", HeadTop, Quaternion.Euler(-SteerTilt, 0f, 0f));
            var st = parts.Steering;
            for (int s = -1; s <= 1; s += 2)
                Bar(st, s < 0 ? "Fork_L" : "Fork_R", new Vector3(0.05f * s, -0.15f, 0f), new Vector3(0.05f * s, -forkLength, 0f), 0.03f);
            Box(st, "ForkCrown", new Vector3(0f, -0.15f, 0f), new Vector3(0.13f, 0.03f, 0.05f), Quaternion.identity);
            // Yüksek boğaz ve geriye kıvrık şehir gidonu: oturunca ekranın altında görünür.
            const float barY = 0.25f;
            Bar(st, "Stem", Vector3.zero, new Vector3(0f, barY, -0.03f), 0.03f);
            Bar(st, "Bar_Center", new Vector3(-0.2f, barY, -0.03f), new Vector3(0.2f, barY, -0.03f), 0.025f);
            for (int s = -1; s <= 1; s += 2)
            {
                var from = new Vector3(0.2f * s, barY, -0.03f);
                var to = new Vector3(0.29f * s, barY, -0.15f);
                Bar(st, s < 0 ? "Bar_L" : "Bar_R", from, to, 0.025f);
                Bar(st, s < 0 ? "Grip_L" : "Grip_R", to, to + (to - from).normalized * 0.11f, 0.04f);
            }
            parts.FrontWheel = Pivot(st, "FrontWheel", new Vector3(0f, -forkLength, 0f), Quaternion.identity);
            BuildWheel(parts.FrontWheel);

            return parts;
        }

        /// <summary>16 parçalı lastik + 4 jant teli (8 kol) + göbek. Teller dönüşü görünür kılar.</summary>
        private static void BuildWheel(Transform hub)
        {
            Ring(hub, "Tire", Vector3.zero, WheelRadius, 16, new Vector3(0.035f, 0.045f, 0.14f));
            for (int i = 0; i < 4; i++)
                Box(hub, $"Spoke_{i}", Vector3.zero, new Vector3(0.012f, WheelRadius * 1.9f, 0.012f), Quaternion.AngleAxis(i * 45f, Vector3.right));
            Box(hub, "Hub", Vector3.zero, new Vector3(0.1f, 0.05f, 0.05f), Quaternion.identity);
        }

        /// <summary>YZ düzleminde (X ekseni etrafında) halka: her parça teğet yönünde.</summary>
        private static void Ring(Transform parent, string name, Vector3 center, float radius, int segments, Vector3 segmentSize)
        {
            var ring = Pivot(parent, name, center, Quaternion.identity);
            for (int i = 0; i < segments; i++)
            {
                var rot = Quaternion.AngleAxis(360f * i / segments, Vector3.right);
                Box(ring, $"{name}_{i}", rot * new Vector3(0f, radius, 0f), segmentSize, rot);
            }
        }

        // ---------- Sahne ----------

        private static void PlaceInActiveScene(GameObject prefab)
        {
            if (Object.FindAnyObjectByType<Bicycle>() != null)
            {
                Debug.Log("[BilgiAdasi] Açık sahnede zaten bisiklet var, yenisi konmadı.");
                return;
            }

            Vector3 position;
            Quaternion rotation;
            var spawn = Object.FindAnyObjectByType<PlayerSpawn>();
            if (spawn != null)
            {
                position = spawn.transform.position + spawn.transform.right * 2.5f + spawn.transform.forward * 1.5f;
                rotation = Quaternion.Euler(0f, spawn.transform.eulerAngles.y, 0f);
            }
            else
            {
                var view = SceneView.lastActiveSceneView;
                position = view != null ? view.pivot : Vector3.zero;
                rotation = Quaternion.identity;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetPositionAndRotation(position, rotation);
            Undo.RegisterCreatedObjectUndo(instance, "Bisiklet ekle");
            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(instance.scene);
        }

        // ---------- Yardımcılar ----------

        private static Transform Pivot(Transform parent, string name, Vector3 localPosition, Quaternion localRotation)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.SetLocalPositionAndRotation(localPosition, localRotation);
            return t;
        }

        private static void Box(Transform parent, string name, Vector3 localPosition, Vector3 size, Quaternion localRotation)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.transform.SetParent(parent, false);
            cube.transform.SetLocalPositionAndRotation(localPosition, localRotation);
            cube.transform.localScale = size;
        }

        /// <summary>İki nokta arasında kare kesitli boru (ebeveynin yerel uzayında).</summary>
        private static void Bar(Transform parent, string name, Vector3 from, Vector3 to, float thickness)
        {
            Vector3 d = to - from;
            float length = d.magnitude;
            if (length < 0.0001f) return;
            Vector3 up = Mathf.Abs(Vector3.Dot(d / length, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            Box(parent, name, (from + to) * 0.5f, new Vector3(thickness, thickness, length), Quaternion.LookRotation(d, up));
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[BilgiAdasi] {target.GetType().Name}.{field} yok."); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[BilgiAdasi] {target.GetType().Name}.{field} yok."); return; }
            p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[BilgiAdasi] {target.GetType().Name}.{field} yok."); return; }
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
