using System.Collections.Generic;
using BilgiAdasi.City;
using BilgiAdasi.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Mustafa'nın entegrasyon notları (08 §9): kamerada post processing + global Volume, bisiklet modelini BicycleRig'e bağlama,
    /// atölye kapısı (Kapi_Rig) Animator'ını DoorStation'a bağlama. Tekrar çalıştırılabilir.
    /// </summary>
    public static class ArtIntegrationSetup
    {
        private const string PlayerRigPath = "Assets/_Project/Prefabs/PlayerRig.prefab";
        private const string VolumeProfilePath = "Assets/Settings/SampleSceneProfile.asset";
        private const string VolumeName = "PostProcessVolume";

        private const string BicyclePrefabPath = "Assets/_Project/Prefabs/Bicycle.prefab";
        private const string BicycleModelPath = "Assets/_Project/Art/Bisiklet/Bisiklet.fbx";
        private const string BicycleModelName = "Model_Bisiklet";
        // bisiklet.py: R_ON 0.39, R_ARKA 0.30; pedal ön tekerde (velosiped).
        private const float FrontWheelRadius = 0.39f;
        private const float RearWheelRadius = 0.30f;

        [MenuItem("BilgiAdasi/Kurulum/10 - Görüntü: post processing (PlayerRig)", priority = 9)]
        public static void SetupPostProcessing()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null) { Debug.LogError($"[BilgiAdasi] Profil yok: {VolumeProfilePath}"); return; }

            var root = PrefabUtility.LoadPrefabContents(PlayerRigPath);
            try
            {
                var cam = root.GetComponentInChildren<Camera>(true);
                if (cam == null) { Debug.LogError("[BilgiAdasi] PlayerRig'de kamera yok."); return; }
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

                // Global Volume oyuncuyla gelir: her sahnede (Town, Classroom) aynı tonlama.
                var t = root.transform.Find(VolumeName);
                var go = t != null ? t.gameObject : new GameObject(VolumeName);
                go.transform.SetParent(root.transform, false);
                var volume = go.GetComponent<Volume>();
                if (volume == null) volume = go.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 0f;
                volume.weight = 1f;
                volume.sharedProfile = profile;

                PrefabUtility.SaveAsPrefabAsset(root, PlayerRigPath);
                Debug.Log("[BilgiAdasi] PlayerRig: post processing açık, global Volume (SampleSceneProfile) eklendi.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [MenuItem("BilgiAdasi/Kurulum/11 - Bisiklet: Mustafa'nın modelini bağla", priority = 10)]
        public static void AttachBicycleModel()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(BicycleModelPath);
            if (model == null) { Debug.LogError($"[BilgiAdasi] Model yok: {BicycleModelPath}"); return; }

            var root = PrefabUtility.LoadPrefabContents(BicyclePrefabPath);
            try
            {
                var rig = root.GetComponentInChildren<BicycleRig>(true);
                if (rig == null) { Debug.LogError("[BilgiAdasi] Bicycle.prefab'da BicycleRig yok."); return; }
                var visual = rig.transform;

                // Eski görünüm: küp yer tutucu ve önceki model kopyası.
                for (int i = visual.childCount - 1; i >= 0; i--)
                {
                    var c = visual.GetChild(i);
                    if (c.name == "_Placeholder" || c.name == BicycleModelName) Object.DestroyImmediate(c.gameObject);
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, visual);
                instance.name = BicycleModelName;
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                Transform front = FindDeep(instance.transform, "Bisiklet_On_Teker");
                Transform rear = FindDeep(instance.transform, "Bisiklet_Arka_Teker");
                Transform steer = FindDeep(instance.transform, "Bisiklet_Gidon");
                Transform pedalL = FindDeep(instance.transform, "Bisiklet_Pedal_Sol");
                Transform pedalR = FindDeep(instance.transform, "Bisiklet_Pedal_Sag");
                if (front == null || rear == null || steer == null)
                {
                    Debug.LogError("[BilgiAdasi] Modelde teker/gidon parçası bulunamadı; adlar bisiklet.py ile aynı mı?");
                    return;
                }

                var so = new SerializedObject(rig);
                so.FindProperty("frontWheel").objectReferenceValue = front;
                so.FindProperty("rearWheel").objectReferenceValue = rear;
                so.FindProperty("steering").objectReferenceValue = steer;
                so.FindProperty("crank").objectReferenceValue = null; // aynakol ön tekere gömülü
                so.FindProperty("pedalLeft").objectReferenceValue = pedalL;
                so.FindProperty("pedalRight").objectReferenceValue = pedalR;
                so.FindProperty("leanRoot").objectReferenceValue = visual;
                so.FindProperty("crankOnFrontWheel").boolValue = true;
                so.FindProperty("wheelRadius").floatValue = FrontWheelRadius;
                so.FindProperty("rearWheelRadius").floatValue = RearWheelRadius;
                SetArray(so.FindProperty("steeringParts"), front);
                SetArray(so.FindProperty("crankParts"), pedalL, pedalR);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, BicyclePrefabPath);

                // Doğrulama çıktısı (eksen/ölçü kontrolü için).
                var b = new Bounds(instance.transform.position, Vector3.zero);
                foreach (var r in instance.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                Debug.Log($"[BilgiAdasi] Bisiklet modeli bağlandı. Sınırlar merkez {b.center} boyut {b.size}; " +
                          $"ön göbek {root.transform.InverseTransformPoint(front.position)}, arka göbek {root.transform.InverseTransformPoint(rear.position)}, " +
                          $"gidon yerel yukarı (kök uzayında) {root.transform.InverseTransformDirection(steer.up)}, " +
                          $"teker yerel sağ {root.transform.InverseTransformDirection(front.right)}");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [MenuItem("BilgiAdasi/Kurulum/12 - Atölye kapısını (Kapi_Rig) DoorStation'a bağla", priority = 11)]
        public static void LinkWorkshopDoor()
        {
            var door = Object.FindAnyObjectByType<DoorStation>(FindObjectsInactive.Include);
            if (door == null) { Debug.LogWarning("[BilgiAdasi] Açık sahnede DoorStation yok."); return; }

            Animator found = null;
            foreach (var a in Object.FindObjectsByType<Animator>(FindObjectsInactive.Include))
                if (a.gameObject.name.StartsWith("Kapi_Rig")) { found = a; break; }
            if (found == null) { Debug.LogWarning("[BilgiAdasi] Açık sahnede Kapi_Rig yok (önce Mustafa'nın atölye yerleştirme menüsü)."); return; }

            var so = new SerializedObject(door);
            so.FindProperty("doorAnimator").objectReferenceValue = found;
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(door.gameObject.scene);
            Debug.Log($"[BilgiAdasi] DoorStation → {found.name} bağlandı. Sahne kaydedilmedi.");
        }

        /// <summary>Batchmode: post processing + bisiklet (sahneye dokunmaz).</summary>
        public static void RunBatch()
        {
            SetupPostProcessing();
            AttachBicycleModel();
            AssetDatabase.SaveAssets();
        }

        private static void SetArray(SerializedProperty prop, params Transform[] items)
        {
            var list = new List<Transform>();
            foreach (var t in items) if (t != null) list.Add(t);
            prop.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var r = FindDeep(parent.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
