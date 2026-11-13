using UnityEngine;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// İki kemik IK (üst + alt kemik, uç hedefe). Generic iskelette Unity'nin hazır IK'sı çalışmadığı için saf trigonometri.
    /// Animator'ın pozunun üstüne LateUpdate'te çağrılır. Bacak (uyluk-baldır-ayak bileği) için kullanılır.
    /// </summary>
    public static class TwoBoneIK
    {
        /// <param name="hint">Diz/dirsek bu noktaya doğru bükülür (dünya).</param>
        /// <param name="weight">0 = animasyon pozu, 1 = uç tam hedefte.</param>
        public static void Solve(Transform upper, Transform lower, Transform end, Vector3 target, Vector3 hint, float weight)
        {
            if (weight <= 0f) return;
            Vector3 a = upper.position, b = lower.position, c = end.position;
            float lab = (b - a).magnitude, lbc = (c - b).magnitude;
            if (lab < 1e-4f || lbc < 1e-4f) return;
            Vector3 goal = Vector3.Lerp(c, target, weight);
            float lat = Mathf.Clamp((goal - a).magnitude, 0.01f, (lab + lbc) * 0.999f);

            // Orta eklem açısı: kosinüs teoremi.
            Vector3 ba = a - b, bc = c - b;
            float current = Vector3.Angle(ba, bc);
            float desired = Mathf.Acos(Mathf.Clamp((lab * lab + lbc * lbc - lat * lat) / (2f * lab * lbc), -1f, 1f)) * Mathf.Rad2Deg;
            Vector3 axis = Vector3.Cross(ba, bc);
            if (axis.sqrMagnitude < 1e-8f) axis = Vector3.Cross(goal - a, hint - a);
            if (axis.sqrMagnitude > 1e-8f)
                lower.rotation = Quaternion.AngleAxis(desired - current, axis.normalized) * lower.rotation;

            // Zincir hedefe döner, sonra orta eklem ipucuna doğru (kök-hedef ekseni etrafında).
            upper.rotation = Quaternion.FromToRotation(end.position - a, goal - a) * upper.rotation;
            Vector3 chain = goal - a;
            if (chain.sqrMagnitude < 1e-6f) return;
            Vector3 mid = Vector3.ProjectOnPlane(lower.position - a, chain);
            Vector3 toHint = Vector3.ProjectOnPlane(hint - a, chain);
            if (mid.sqrMagnitude > 1e-6f && toHint.sqrMagnitude > 1e-6f)
                upper.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(mid, toHint, chain) * weight, chain.normalized) * upper.rotation;
        }
    }

    /// <summary>
    /// Animator sabit (sıkıştırılmış) eğrili kemikleri her karede yazmaz; IK'nın yazdığı poz bir sonraki karede
    /// "animasyon pozu" sanılırsa düzeltmeler üst üste biner. Bu sınıf kemiklerin animasyon pozunu saklar, Animator o
    /// karede yazmadıysa geri yükler. Konum ve dönüş ayrı denetlenir.
    /// </summary>
    public sealed class AnimatedPoseCache
    {
        private readonly Transform[] bones;
        private readonly Vector3[] animPos, writtenPos;
        private readonly Quaternion[] animRot, writtenRot;
        private bool hasWritten;

        public AnimatedPoseCache(Transform[] bones)
        {
            this.bones = bones;
            animPos = new Vector3[bones.Length];
            writtenPos = new Vector3[bones.Length];
            animRot = new Quaternion[bones.Length];
            writtenRot = new Quaternion[bones.Length];
        }

        /// <summary>IK'dan önce: temiz animasyon pozu.</summary>
        public void Sync()
        {
            for (int i = 0; i < bones.Length; i++)
            {
                var t = bones[i];
                t.GetLocalPositionAndRotation(out var p, out var r);
                if (hasWritten && p == writtenPos[i]) p = animPos[i];
                else animPos[i] = p;
                if (hasWritten && r == writtenRot[i]) r = animRot[i];
                else animRot[i] = r;
                t.SetLocalPositionAndRotation(p, r);
            }
            hasWritten = false;
        }

        /// <summary>IK'dan sonra: yazılan pozu not et.</summary>
        public void Record()
        {
            for (int i = 0; i < bones.Length; i++)
                bones[i].GetLocalPositionAndRotation(out writtenPos[i], out writtenRot[i]);
            hasWritten = true;
        }

        public bool HasWritten => hasWritten;
    }
}
