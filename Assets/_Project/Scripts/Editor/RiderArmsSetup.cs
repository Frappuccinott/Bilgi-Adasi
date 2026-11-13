using BilgiAdasi.Player;
using UnityEditor;
using UnityEngine;

namespace BilgiAdasi.EditorTools
{
    /// <summary>
    /// Bisiklette eller gidonda: Bicycle.prefab'da gidonun çocuğu olarak iki tutma noktası (Tutma_Sol / Tutma_Sag)
    /// ve PlayerRig'deki FPS kollarına RiderArmsIK. Tekrar çalıştırılabilir (noktalar yeniden hesaplanır).
    /// Tutma noktası gidon modelinin tutamak bölgesinden (köşe noktalarının ortası) bulunur; model değişirse yeniden çalıştır.
    /// </summary>
    public static class RiderArmsSetup
    {
        private const string BicyclePrefabPath = "Assets/_Project/Prefabs/Bicycle.prefab";
        private const string PlayerRigPath = "Assets/_Project/Prefabs/PlayerRig.prefab";
        private const string ArmsName = "Ogrenci_Kol_Rig";
        private const string GripLeftName = "Tutma_Sol";
        private const string GripRightName = "Tutma_Sag";

        // bisiklet.py: tutamak gidon ekseninin dış ucunda; gidon yerel uzayında |x| bu aralıkta (topuz hariç).
        private const float GripInnerX = 0.22f;
        private const float GripOuterX = 0.32f;
        // Model okunamazsa (yerel uzay, sağ tutamak): orta nokta ve dışa doğru eksen.
        private static readonly Vector3 FallbackCenter = new Vector3(0.27f, 0.636f, -0.088f);
        private static readonly Vector3 FallbackOutward = new Vector3(0.87f, 0.24f, -0.44f);
        // Parmaklar (el kemiği Y) ileriden bu kadar aşağı bakar: avuç tutamağın üstünde, parmaklar önden sarar.
        private const float FingerDownAngle = 25f;

        [MenuItem("BilgiAdasi/Kurulum/14 - Bisiklet: eller gidonda (kollar + tutma noktaları)", priority = 13)]
        public static void Setup()
        {
            SetupBicyclePrefab();
            SetupPlayerArms();
        }

        private static void SetupBicyclePrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(BicyclePrefabPath);
            try
            {
                var rig = root.GetComponentInChildren<BicycleRig>(true);
                if (rig == null) { Debug.LogError("[BilgiAdasi] Bicycle.prefab'da BicycleRig yok."); return; }
                var steering = new SerializedObject(rig).FindProperty("steering").objectReferenceValue as Transform;
                if (steering == null) { Debug.LogError("[BilgiAdasi] BicycleRig'te gidon (steering) bağlı değil; önce Kurulum → 11."); return; }
                CreateGrips(rig, steering, root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, BicyclePrefabPath);
                Debug.Log("[BilgiAdasi] Bicycle.prefab: gidona Tutma_Sol / Tutma_Sag eklendi, BicycleRig'e bağlandı.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        /// <summary>Gidonun çocuğu iki tutma noktası kurar ve BicycleRig'e bağlar. ArtIntegrationSetup (11) de çağırır.</summary>
        public static void CreateGrips(BicycleRig rig, Transform steering, Transform bikeRoot)
        {
            Vector3 center = FallbackCenter, outward = FallbackOutward;
            var mf = steering.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) TryMeasureGrip(mf.sharedMesh, ref center, ref outward);

            var left = Grip(steering, bikeRoot, GripLeftName, new Vector3(-center.x, center.y, center.z),
                new Vector3(-outward.x, outward.y, outward.z), false);
            var right = Grip(steering, bikeRoot, GripRightName, center, outward, true);

            var so = new SerializedObject(rig);
            so.FindProperty("gripLeft").objectReferenceValue = left;
            so.FindProperty("gripRight").objectReferenceValue = right;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Sağ tutamak bölgesindeki köşe noktalarından orta nokta ve dışa eksen (gidon yerel uzayı).</summary>
        private static void TryMeasureGrip(Mesh mesh, ref Vector3 center, ref Vector3 outward)
        {
            var verts = mesh.vertices; // editörde okunamaz mesh'te de çalışır
            if (verts == null || verts.Length == 0) return;
            Vector3 inner = Vector3.zero, outer = Vector3.zero, sum = Vector3.zero;
            int ni = 0, no = 0, n = 0;
            const float band = 0.02f;
            foreach (var v in verts)
            {
                if (v.x < GripInnerX || v.x > GripOuterX) continue;
                sum += v; n++;
                if (v.x < GripInnerX + band) { inner += v; ni++; }
                else if (v.x > GripOuterX - band) { outer += v; no++; }
            }
            if (n < 10 || ni == 0 || no == 0) return;
            center = sum / n;
            Vector3 dir = outer / no - inner / ni;
            if (dir.sqrMagnitude > 1e-6f) outward = dir.normalized;
        }

        private static Transform Grip(Transform steering, Transform bikeRoot, string name, Vector3 localPos, Vector3 localOutward, bool rightHand)
        {
            var t = steering.Find(name);
            if (t == null)
            {
                t = new GameObject(name).transform;
                t.SetParent(steering, false);
            }
            t.localPosition = localPos;
            t.localScale = Vector3.one;

            // Bisiklet uzayında: parmaklar ileri-aşağı, tutamağa dik; elin sırtı yukarı.
            // El kemiği: Y parmaklara, Z elin sırtı, sağ elde +X başparmak (içe), sol elde -X başparmak (içe).
            Vector3 barOut = steering.TransformDirection(localOutward).normalized;
            Vector3 forward = bikeRoot.forward, upBike = bikeRoot.up;
            Vector3 fingers = Quaternion.AngleAxis(FingerDownAngle, bikeRoot.right) * forward;
            fingers = Vector3.ProjectOnPlane(fingers, barOut).normalized;
            Vector3 x = rightHand ? -barOut : barOut;
            Vector3 back = Vector3.Cross(x, fingers);
            if (Vector3.Dot(back, upBike) < 0f) back = -back;
            t.rotation = Quaternion.LookRotation(back, fingers);
            return t;
        }

        private static void SetupPlayerArms()
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerRigPath);
            try
            {
                var cam = root.GetComponentInChildren<Camera>(true);
                Transform arms = cam != null ? cam.transform.Find(ArmsName) : null;
                if (arms == null) { Debug.LogError("[BilgiAdasi] PlayerRig'de kameranın altında Ogrenci_Kol_Rig yok."); return; }

                Transform kok = FindDeep(arms, "Kok");
                if (kok == null) { Debug.LogError("[BilgiAdasi] Kol iskeletinde Kok kemiği yok."); return; }

                var ik = arms.GetComponent<RiderArmsIK>();
                if (ik == null) ik = arms.gameObject.AddComponent<RiderArmsIK>();
                var so = new SerializedObject(ik);
                so.FindProperty("root").objectReferenceValue = kok;
                so.FindProperty("cameraSpace").objectReferenceValue = arms;
                bool ok = Arm(so, "left", kok, "_L") & Arm(so, "right", kok, "_R");
                so.ApplyModifiedPropertiesWithoutUndo();
                if (!ok) { Debug.LogError("[BilgiAdasi] Kol kemikleri bulunamadı (Kol_Ust / Kol_On / El / Tutma)."); return; }

                PrefabUtility.SaveAsPrefabAsset(root, PlayerRigPath);
                Debug.Log("[BilgiAdasi] PlayerRig: FPS kollarına RiderArmsIK eklendi (eller bisiklet gidonunu tutar).");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static bool Arm(SerializedObject so, string field, Transform kok, string side)
        {
            Transform upper = FindDeep(kok, "Kol_Ust" + side);
            Transform lower = FindDeep(kok, "Kol_On" + side);
            Transform hand = FindDeep(kok, "El" + side);
            Transform grip = FindDeep(kok, "Tutma" + side);
            so.FindProperty(field + ".upper").objectReferenceValue = upper;
            so.FindProperty(field + ".lower").objectReferenceValue = lower;
            so.FindProperty(field + ".hand").objectReferenceValue = hand;
            so.FindProperty(field + ".gripPoint").objectReferenceValue = grip;
            return upper != null && lower != null && hand != null;
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            foreach (var t in parent.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }
    }
}
