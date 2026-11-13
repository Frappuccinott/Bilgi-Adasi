using System.Collections;
using BilgiAdasi.Core;
using UnityEngine;

namespace BilgiAdasi.Desk
{
    /// <summary>
    /// Masa çevresinin sanatı (IDeskPropsView): ulak güvercin dışarıdan kavisle uçup pencere pervazına konar,
    /// zarf konunca masada belirir; görev bitince güvercin cevabı alıp aynı yoldan uçar.
    /// Uçuş ve kanat çırpma kodla (Animator yok): kanatlar Kanat_Sol/Kanat_Sag menteşesinde döner.
    /// Model: Art/Atolye/Masa_Gorev.fbx (ArtSource/Blender/Script/masa_gorev.py).
    /// </summary>
    public sealed class ArtDeskView : RegisteredView<IDeskPropsView>, IDeskPropsView
    {
        [Header("Güvercin")]
        [SerializeField] private Transform pigeon;
        [SerializeField] private Transform wingLeft;
        [SerializeField] private Transform wingRight;
        [Tooltip("Kanat çırpma ekseni (kanadın yerel uzayında; gövdenin ileri ekseni).")]
        [SerializeField] private Vector3 wingAxis = Vector3.forward;
        [Tooltip("Tünerken kanatlar bu eksende geriye katlanır (yukarı ekseni).")]
        [SerializeField] private Vector3 foldAxis = Vector3.up;
        [SerializeField] private float foldAngle = 80f;
        [Tooltip("Katlanınca kanat hafif aşağı iner (gövdeye yatsın).")]
        [SerializeField] private float foldDroop = 12f;
        [Tooltip("Pencere pervazı: güvercin buraya konar (ileri ekseni odaya bakar).")]
        [SerializeField] private Transform perch;
        [Tooltip("Uçuşun başladığı/bittiği nokta (dışarıda, gökyüzünde).")]
        [SerializeField] private Transform skyPoint;
        [SerializeField] private float flightSeconds = 2.6f;
        [SerializeField] private float arcHeight = 1.6f;
        [SerializeField] private float flapHz = 7f;
        [SerializeField] private float flapAngle = 60f;

        [Header("Masa")]
        [SerializeField] private GameObject envelope;
        [Tooltip("Lonca Sınavı zarfı (altın şeritli). Boşsa normal zarf.")]
        [SerializeField] private GameObject guildEnvelope;
        [Tooltip("İstasyondayken masada açık duran kâğıt. Boş bırakılabilir.")]
        [SerializeField] private GameObject openPaper;

        private Quaternion wingLeftRest, wingRightRest;
        private Coroutine flight;
        private bool envelopeWanted, envelopeGuild, landing;

        private void Awake()
        {
            if (wingLeft != null) wingLeftRest = wingLeft.localRotation;
            if (wingRight != null) wingRightRest = wingRight.localRotation;
            if (pigeon != null) pigeon.gameObject.SetActive(false);
            Pose(0f, 1f);
            ApplyEnvelope();
            if (openPaper != null) openPaper.SetActive(false);
        }

        public void PlayArrival(bool guild)
        {
            if (pigeon == null || perch == null || skyPoint == null) return;
            if (flight != null) StopCoroutine(flight);
            landing = true;
            flight = StartCoroutine(Fly(skyPoint, perch, true));
        }

        public void PlayDeparture()
        {
            if (pigeon == null || perch == null || skyPoint == null || !pigeon.gameObject.activeSelf) return;
            if (flight != null) StopCoroutine(flight);
            flight = StartCoroutine(Fly(perch, skyPoint, false));
        }

        public void SetEnvelope(bool visible, bool guild)
        {
            envelopeWanted = visible;
            envelopeGuild = guild;
            ApplyEnvelope();
        }

        public void SetPaperOpen(bool open)
        {
            if (openPaper != null) openPaper.SetActive(open);
        }

        private void ApplyEnvelope()
        {
            // Güvercin yoldayken zarf henüz masada değil.
            bool show = envelopeWanted && !landing;
            bool guildShown = show && envelopeGuild && guildEnvelope != null;
            if (envelope != null) envelope.SetActive(show && !guildShown);
            if (guildEnvelope != null) guildEnvelope.SetActive(guildShown);
        }

        private IEnumerator Fly(Transform from, Transform to, bool arriving)
        {
            pigeon.gameObject.SetActive(true);
            Vector3 a = from.position, b = to.position;
            Vector3 c = (a + b) * 0.5f + Vector3.up * arcHeight;
            float t = 0f;
            Vector3 prev = a;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.1f, flightSeconds);
                float k = Mathf.Clamp01(t);
                // varışta yavaşla, kalkışta hızlan
                float e = arriving ? 1f - (1f - k) * (1f - k) : k * k;
                Vector3 p = Bezier(a, c, b, e);
                pigeon.position = p;
                Vector3 v = p - prev;
                if (v.sqrMagnitude > 1e-6f) pigeon.rotation = Quaternion.LookRotation(v.normalized, Vector3.up);
                prev = p;
                // varışın sonunda kanatlar yavaşlayıp katlanır; kalkışta önce açılır
                float fold = arriving ? Mathf.InverseLerp(0.82f, 1f, k) : 1f - Mathf.InverseLerp(0f, 0.15f, k);
                Pose(Mathf.Sin(Time.time * flapHz * Mathf.PI * 2f) * flapAngle * (1f - fold), fold);
                yield return null;
            }

            Pose(0f, 1f);
            flight = null;
            if (arriving)
            {
                pigeon.SetPositionAndRotation(to.position, to.rotation);
                landing = false;
                ApplyEnvelope();
            }
            else
            {
                pigeon.gameObject.SetActive(false);
            }
        }

        /// <summary>flap: çırpma açısı; fold 0 açık (uçuş) … 1 katlı (tünemiş). Sol kanat -X, sağ +X yanında.</summary>
        private void Pose(float flap, float fold)
        {
            if (wingLeft != null)
                wingLeft.localRotation = wingLeftRest * Quaternion.AngleAxis(-fold * foldAngle, foldAxis)
                    * Quaternion.AngleAxis(-flap + fold * foldDroop, wingAxis);
            if (wingRight != null)
                wingRight.localRotation = wingRightRest * Quaternion.AngleAxis(fold * foldAngle, foldAxis)
                    * Quaternion.AngleAxis(flap - fold * foldDroop, wingAxis);
        }

        private static Vector3 Bezier(Vector3 a, Vector3 c, Vector3 b, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * c + t * t * b;
        }
    }
}
