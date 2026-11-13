using System;
using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// FPS kollarını bisiklet gidonuna bağlayan görünüm (IRiderArmsView). Kol iskeletine (Ogrenci_Kol_Rig) eklenir.
    /// Animator'ın pozunun üstüne her karede çalışır:
    /// 1) Kol kökü kameradan alınıp gövdeye (bisikletin yatay yönü) bağlanır: kafa serbest bakınca eller gidonda kalır.
    /// 2) Eller tutma noktalarına iki kemik IK ile uzanır (üst kol + ön kol, dirsek ipucu dışa-aşağı).
    ///    Kol yetmezse omuzlar öne gelir (sürücü gidona eğilir).
    /// 3) Parmaklar gidonu kavrar (animasyondaki parmak pozuna eklenir).
    /// Generic iskelette Unity'nin hazır IK'sı çalışmadığı için çözücü burada, saf trigonometri (paket yok).
    /// CinemachineBrain kamerayı LateUpdate'te taşır; bu bileşen ondan sonra çalışır (sıra 10000), kollar titremez.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class RiderArmsIK : RegisteredView<IRiderArmsView>, IRiderArmsView
    {
        [Serializable]
        private struct ArmBones
        {
            public Transform upper;
            public Transform lower;
            public Transform hand;
            [Tooltip("Avuç içindeki tutma noktası (gidon bu noktadan geçer).")]
            public Transform gripPoint;
        }

        private struct ArmState
        {
            public float upperLength;
            public float lowerLength;
            public Vector3 gripInHand;     // tutma noktasının el kemiği uzayındaki konumu
            public Transform[] fingers;    // Parmak_* kemikleri
            public int[] phalanx;          // 1, 2, 3 (boğum)
            public bool[] thumb;
        }

        [Header("İskelet")]
        [Tooltip("Kol kökü (Kok). Sürerken kameradan alınıp gövdeye bağlanır.")]
        [SerializeField] private Transform root;
        [Tooltip("Kolların yazıldığı kamera uzayı: kameranın çocuğu olan sarmalayıcı (Ogrenci_Kol_Rig). Boşsa bu obje.")]
        [SerializeField] private Transform cameraSpace;
        [SerializeField] private ArmBones left;
        [SerializeField] private ArmBones right;

        [Header("Uzanma")]
        [Tooltip("Binince ellerin gidona uzanma süresi (sn).")]
        [SerializeField] private float reachDuration = 0.45f;
        [Tooltip("İnince bırakma süresi (sn).")]
        [SerializeField] private float releaseDuration = 0.3f;
        [Tooltip("Uzanırken elin çizdiği yayın yüksekliği (m).")]
        [SerializeField] private float reachArc = 0.06f;

        [Header("IK")]
        [Tooltip("Dirsek ipucu: omuza göre gövde uzayında (sağ kol; sol aynalanır). X dışa, Y yukarı, Z ileri (m).")]
        [SerializeField] private Vector3 elbowHint = new Vector3(0.45f, -0.35f, -0.1f);
        [Tooltip("Kolun en fazla ne kadarı açılır (1 = tam düz). Daha uzağa omuzlar öne gelir.")]
        [Range(0.85f, 1f)] [SerializeField] private float reachSlack = 0.97f;
        [Tooltip("Kol yetmeyince omuzların en fazla öne/aşağı kayması (m).")]
        [SerializeField] private float maxShoulderShift = 0.2f;

        [Header("Kavrama (animasyon pozuna eklenir, ° — yerel X ekseninde; eksi = avuca doğru)")]
        [Tooltip("Parmakların 1., 2., 3. boğumuna eklenen bükülme.")]
        [SerializeField] private Vector3 fingerCurl = new Vector3(-40f, -45f, -30f);
        [Tooltip("Başparmağın 1., 2., 3. boğumuna eklenen bükülme.")]
        [SerializeField] private Vector3 thumbCurl = new Vector3(-10f, -20f, -15f);

        private ArmState leftState;
        private ArmState rightState;
        private Transform gripLeft;
        private Transform gripRight;
        private Transform body;
        private bool gripping;
        private float weight;
        private bool ready;

        // Animator sabit (eğrisi sıkıştırılmış) kemikleri her karede yeniden yazmaz. IK'nın yazdığı poz bir sonraki
        // karede "animasyon pozu" sanılırsa düzeltmeler üst üste biner (kollar kayar, parmaklar dolanır).
        // Bu yüzden dokunulan her kemiğin animasyon pozu saklanır; Animator yazmadıysa o geri yüklenir.
        private Transform[] bones;
        private Vector3[] animPos, writtenPos;
        private Quaternion[] animRot, writtenRot;
        private bool hasWritten;

        private void Awake()
        {
            if (cameraSpace == null) cameraSpace = transform;
            ready = root != null && Init(left, out leftState) && Init(right, out rightState);
            if (!ready) { Debug.LogWarning("[RiderArmsIK] Kemik referansları eksik; kollar gidonu tutmaz.", this); return; }

            int n = 7 + leftState.fingers.Length + rightState.fingers.Length;
            bones = new Transform[n];
            int i = 0;
            bones[i++] = root;
            bones[i++] = left.upper; bones[i++] = left.lower; bones[i++] = left.hand;
            bones[i++] = right.upper; bones[i++] = right.lower; bones[i++] = right.hand;
            foreach (var f in leftState.fingers) bones[i++] = f;
            foreach (var f in rightState.fingers) bones[i++] = f;
            animPos = new Vector3[n];
            writtenPos = new Vector3[n];
            animRot = new Quaternion[n];
            writtenRot = new Quaternion[n];
        }

        protected override void OnDisable()
        {
            if (ready) SyncAnimatedPose(); // kapanırken kollar animasyon pozunda kalsın
            hasWritten = false;
            base.OnDisable(); // ViewLocator kaydını siler (yoksa kapalı görünüm kayıtlı kalıyordu)
        }

        /// <summary>Animator bu karede yazmadıysa animasyon pozunu geri yükler, yazdıysa yenisini saklar.</summary>
        private void SyncAnimatedPose()
        {
            for (int i = 0; i < bones.Length; i++)
            {
                var t = bones[i];
                // Konum ve dönüş ayrı denetlenir: Animator birini yazıp ötekini yazmayabilir (ör. kökte yalnızca konum).
                t.GetLocalPositionAndRotation(out var p, out var r);
                if (hasWritten && p == writtenPos[i]) p = animPos[i];
                else animPos[i] = p;
                if (hasWritten && r == writtenRot[i]) r = animRot[i];
                else animRot[i] = r;
                t.SetLocalPositionAndRotation(p, r);
            }
            hasWritten = false;
        }

        private void RecordWrittenPose()
        {
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].GetLocalPositionAndRotation(out var p, out var r);
                writtenPos[i] = p;
                writtenRot[i] = r;
            }
            hasWritten = true;
        }

        private static bool Init(ArmBones bones, out ArmState state)
        {
            state = default;
            if (bones.upper == null || bones.lower == null || bones.hand == null) return false;
            state.upperLength = Vector3.Distance(bones.upper.position, bones.lower.position);
            state.lowerLength = Vector3.Distance(bones.lower.position, bones.hand.position);
            state.gripInHand = bones.gripPoint != null ? bones.hand.InverseTransformPoint(bones.gripPoint.position) : Vector3.zero;

            // Parmak kemikleri adlarından bulunur: Parmak_<Ad>_<boğum>_<taraf> (Bas = başparmak).
            var all = bones.hand.GetComponentsInChildren<Transform>(true);
            int count = 0;
            foreach (var t in all) if (t.name.StartsWith("Parmak_", StringComparison.Ordinal) && Phalanx(t.name) > 0) count++;
            state.fingers = new Transform[count];
            state.phalanx = new int[count];
            state.thumb = new bool[count];
            int i = 0;
            foreach (var t in all)
            {
                int p = t.name.StartsWith("Parmak_", StringComparison.Ordinal) ? Phalanx(t.name) : 0;
                if (p == 0) continue;
                state.fingers[i] = t;
                state.phalanx[i] = p;
                state.thumb[i] = t.name.StartsWith("Parmak_Bas", StringComparison.Ordinal);
                i++;
            }
            return true;
        }

        /// <summary>"Parmak_Orta_2_R" → 2; boğum numarası yoksa 0.</summary>
        private static int Phalanx(string boneName)
        {
            for (int i = boneName.Length - 1; i > 0; i--)
            {
                char c = boneName[i];
                if (c >= '1' && c <= '3' && boneName[i - 1] == '_') return c - '0';
            }
            return 0;
        }

        // ---------- IRiderArmsView ----------

        public void BeginGrip(Transform leftGrip, Transform rightGrip, Transform bodyTransform, bool instant)
        {
            gripLeft = leftGrip;
            gripRight = rightGrip;
            body = bodyTransform;
            gripping = gripLeft != null && gripRight != null && body != null;
            if (gripping && instant) weight = 1f;
        }

        public void EndGrip() => gripping = false;

        // ---------- Döngü ----------

        private void LateUpdate()
        {
            if (!ready) return;
            if (hasWritten) SyncAnimatedPose(); // önce temiz animasyon pozu (inince kollar da buna döner)
            float dt = Time.deltaTime;
            if (gripLeft == null || gripRight == null || body == null)
            {
                gripping = false;
                weight = 0f;
                return;
            }
            float duration = gripping ? reachDuration : releaseDuration;
            weight = Mathf.MoveTowards(weight, gripping ? 1f : 0f, dt / Mathf.Max(0.01f, duration));
            if (weight <= 0f)
            {
                gripLeft = gripRight = body = null;
                return;
            }
            float w = Mathf.SmoothStep(0f, 1f, weight);
            SyncAnimatedPose(); // ilk karede animasyon pozunu saklar

            // 1) Kök: kamera uzayındaki animasyon pozu, gövde çerçevesine taşınır (kamera bakışı kolları döndürmez).
            Quaternion bodyRot = Quaternion.Euler(0f, body.eulerAngles.y, 0f);
            Quaternion camInv = Quaternion.Inverse(cameraSpace.rotation);
            Vector3 eye = cameraSpace.position;
            Vector3 rootRel = camInv * (root.position - eye);
            Quaternion rootRotRel = camInv * root.rotation;
            root.SetPositionAndRotation(
                Vector3.Lerp(root.position, eye + bodyRot * rootRel, w),
                Quaternion.Slerp(root.rotation, bodyRot * rootRotRel, w));

            // Hedefler (el kemiği konumu + dönüşü): tutma noktası gidon noktasına otursun.
            Quaternion rotL = gripLeft.rotation;
            Quaternion rotR = gripRight.rotation;
            Vector3 targetL = gripLeft.position - rotL * leftState.gripInHand;
            Vector3 targetR = gripRight.position - rotR * rightState.gripInHand;

            // Kol yetmiyorsa omuzlar hedeflere doğru kayar (gidona eğilme).
            float deficit = Mathf.Max(Deficit(left, leftState, targetL), Deficit(right, rightState, targetR));
            if (deficit > 0f)
            {
                Vector3 shoulders = (left.upper.position + right.upper.position) * 0.5f;
                Vector3 dir = ((targetL + targetR) * 0.5f - shoulders).normalized;
                root.position += dir * (Mathf.Min(deficit, maxShoulderShift) * w);
            }

            Vector3 up = bodyRot * Vector3.up;
            float arc = reachArc * Mathf.Sin(Mathf.PI * w); // uzanma/bırakma ortasında el yukarı yay çizer
            Solve(left, ref leftState, targetL, rotL, -1f, bodyRot, up * arc, w);
            Solve(right, ref rightState, targetR, rotR, 1f, bodyRot, up * arc, w);
            RecordWrittenPose();
        }

        private float Deficit(ArmBones bones, ArmState state, Vector3 target)
        {
            float reach = (state.upperLength + state.lowerLength) * reachSlack;
            return Vector3.Distance(bones.upper.position, target) - reach;
        }

        private void Solve(ArmBones bones, ref ArmState state, Vector3 target, Quaternion targetRot, float side,
            Quaternion bodyRot, Vector3 arcOffset, float w)
        {
            Transform upper = bones.upper, lower = bones.lower, hand = bones.hand;
            Quaternion animHandRot = hand.rotation;
            Vector3 goal = Vector3.Lerp(hand.position, target, w) + arcOffset;
            Quaternion goalRot = Quaternion.Slerp(animHandRot, targetRot, w);

            Vector3 a = upper.position;
            Vector3 b = lower.position;
            Vector3 c = hand.position;
            float lab = state.upperLength, lbc = state.lowerLength;
            float lat = Mathf.Clamp((goal - a).magnitude, 0.01f, (lab + lbc) * 0.999f);

            // Dirsek açısı: kosinüs teoremi ile |a-c| = |a-goal| olacak şekilde ön kol bükülür.
            Vector3 ba = a - b, bc = c - b;
            float current = Vector3.Angle(ba, bc);
            float desired = Mathf.Acos(Mathf.Clamp((lab * lab + lbc * lbc - lat * lat) / (2f * lab * lbc), -1f, 1f)) * Mathf.Rad2Deg;
            Vector3 hintWorld = a + bodyRot * new Vector3(elbowHint.x * side, elbowHint.y, elbowHint.z);
            Vector3 bendAxis = Vector3.Cross(ba, bc);
            if (bendAxis.sqrMagnitude < 1e-8f) bendAxis = Vector3.Cross(goal - a, hintWorld - a);
            if (bendAxis.sqrMagnitude > 1e-8f)
                lower.rotation = Quaternion.AngleAxis(desired - current, bendAxis.normalized) * lower.rotation;

            // Kol zinciri hedefe döner.
            upper.rotation = Quaternion.FromToRotation(hand.position - a, goal - a) * upper.rotation;

            // Dirsek ipucuna doğru zincir, omuz-hedef ekseni etrafında döner.
            Vector3 axis = goal - a;
            if (axis.sqrMagnitude > 1e-6f)
            {
                Vector3 elbow = Vector3.ProjectOnPlane(lower.position - a, axis);
                Vector3 hint = Vector3.ProjectOnPlane(hintWorld - a, axis);
                if (elbow.sqrMagnitude > 1e-6f && hint.sqrMagnitude > 1e-6f)
                    upper.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(elbow, hint, axis) * w, axis.normalized) * upper.rotation;
            }

            hand.rotation = goalRot;

            // Kavrama: animasyondaki parmak pozuna bükülme eklenir.
            var fingers = state.fingers;
            for (int i = 0; i < fingers.Length; i++)
            {
                Vector3 curl = state.thumb[i] ? thumbCurl : fingerCurl;
                float angle = state.phalanx[i] == 1 ? curl.x : state.phalanx[i] == 2 ? curl.y : curl.z;
                fingers[i].localRotation *= Quaternion.AngleAxis(angle * w, Vector3.right);
            }
        }
    }
}
