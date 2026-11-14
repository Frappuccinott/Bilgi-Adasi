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

        [Header("Mektup ışığı (zarf masada beklerken)")]
        [Tooltip("Zarfın çevresinde kameraya dönen yumuşak hale (Kurulum 26). Boşsa yalnızca zarf ışır.")]
        [SerializeField] private Renderer letterHalo;
        [Tooltip("Zarftan yukarı yükselen ince ışık sütunu (yalnızca dikey eksende kameraya döner). Boş bırakılabilir.")]
        [SerializeField] private Renderer letterBeam;
        [Tooltip("09 paleti, yeşil yaprak açık ton (#7ED63B): koyu ahşap masada en iyi seçilen.")]
        [SerializeField] private Color glowColor = new Color(0.494f, 0.839f, 0.231f, 1f);
        [Tooltip("Nabız hızı (atım/sn). Sakin kalsın: göz yormadan fark edilsin.")]
        [SerializeField] private float pulseHz = 0.7f;
        [SerializeField] [Range(0f, 1f)] private float haloAlphaMin = 0.45f;
        [SerializeField] [Range(0f, 1f)] private float haloAlphaMax = 1f;
        [Tooltip("Hale boyu (m); nabızla bu ile ×1.15 arasında büyür.")]
        [SerializeField] private float haloSize = 1.4f;
        [Tooltip("Hale zarftan kameraya doğru bu kadar öne alınır (m): masaya gömülmez.")]
        [SerializeField] private float haloForward = 0.35f;
        [Tooltip("Sütun: genişlik × yükseklik (m). Alt ucu zarfta.")]
        [SerializeField] private Vector2 beamSize = new Vector2(0.45f, 1.8f);
        [SerializeField] [Range(0f, 1f)] private float beamAlphaMax = 0.9f;
        [Tooltip("Zarfın ışıma gücü (Stilize _Isima) nabızda en çok.")]
        [SerializeField] private float envelopeGlow = 1.6f;
        [Tooltip("Zarfın rengi bu oranda yeşile kayar (nabzın tepesinde).")]
        [SerializeField] [Range(0f, 1f)] private float envelopeTint = 0.45f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int GlowId = Shader.PropertyToID("_Isima");
        private static readonly int TintId = Shader.PropertyToID("_Renk");
        private Renderer[] envelopeRenderers;
        private MaterialPropertyBlock block;
        private Transform cameraTransform;
        private Vector3 glowCenter;
        private bool glowing;

        private Quaternion wingLeftRest, wingRightRest;
        private Coroutine flight;
        private bool envelopeWanted, envelopeGuild, landing;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            envelopeRenderers = CollectRenderers(envelope, guildEnvelope);
            if (letterHalo != null) letterHalo.gameObject.SetActive(false);
            if (letterBeam != null) letterBeam.gameObject.SetActive(false);
            glowCenter = envelope != null ? envelope.transform.position : transform.position;
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
            SetGlowing(show);
        }

        // ---------- Mektup ışığı ----------

        private void SetGlowing(bool on)
        {
            if (glowing == on) return;
            glowing = on;
            if (letterHalo != null) letterHalo.gameObject.SetActive(on);
            if (letterBeam != null) letterBeam.gameObject.SetActive(on);
            if (!on) ClearEnvelopeGlow();
        }

        private void Update()
        {
            if (!glowing) return;
            // 0…1 yumuşak nabız (sinüs, en altta biraz bekler).
            float p = 0.5f - 0.5f * Mathf.Cos(Time.time * pulseHz * Mathf.PI * 2f);
            p *= p * (3f - 2f * p);

            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            Vector3 toCam = cameraTransform != null ? cameraTransform.position - glowCenter : Vector3.back;

            if (letterHalo != null)
            {
                var t = letterHalo.transform;
                Vector3 dir = toCam.sqrMagnitude > 1e-4f ? toCam.normalized : Vector3.back;
                t.position = glowCenter + Vector3.up * 0.05f + dir * haloForward;
                t.rotation = Quaternion.LookRotation(-dir, Vector3.up);
                t.localScale = Vector3.one * (haloSize * (1f + 0.15f * p));
                Tint(letterHalo, Mathf.Lerp(haloAlphaMin, haloAlphaMax, p));
            }

            if (letterBeam != null)
            {
                // Yalnızca Y ekseninde döner: sütun hep dik kalır.
                var t = letterBeam.transform;
                Vector3 flat = new Vector3(toCam.x, 0f, toCam.z);
                if (flat.sqrMagnitude > 1e-4f) t.rotation = Quaternion.LookRotation(-flat.normalized, Vector3.up);
                float h = beamSize.y * (0.9f + 0.1f * p);
                t.position = glowCenter + Vector3.up * (h * 0.5f);
                t.localScale = new Vector3(beamSize.x, h, 1f);
                Tint(letterBeam, beamAlphaMax * Mathf.Lerp(0.5f, 1f, p));
            }

            for (int i = 0; i < envelopeRenderers.Length; i++)
            {
                var r = envelopeRenderers[i];
                if (r == null || !r.gameObject.activeInHierarchy) continue;
                r.GetPropertyBlock(block);
                block.SetFloat(GlowId, envelopeGlow * p);
                block.SetColor(TintId, Color.Lerp(Color.white, glowColor, envelopeTint * p));
                r.SetPropertyBlock(block);
            }
        }

        private void Tint(Renderer r, float alpha)
        {
            var c = glowColor;
            c.a = alpha;
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            r.SetPropertyBlock(block);
        }

        private void ClearEnvelopeGlow()
        {
            if (envelopeRenderers == null) return;
            for (int i = 0; i < envelopeRenderers.Length; i++)
                if (envelopeRenderers[i] != null) envelopeRenderers[i].SetPropertyBlock(null);
        }

        private static Renderer[] CollectRenderers(GameObject a, GameObject b)
        {
            var list = new System.Collections.Generic.List<Renderer>();
            if (a != null) list.AddRange(a.GetComponentsInChildren<Renderer>(true));
            if (b != null) list.AddRange(b.GetComponentsInChildren<Renderer>(true));
            return list.ToArray();
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
