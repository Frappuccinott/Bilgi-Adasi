using System;
using System.Collections.Generic;
using BilgiAdasi.Player;
using BilgiAdasi.UI;
using UnityEngine;

namespace BilgiAdasi.City
{
    public enum NpcActivity
    {
        /// <summary>Durur, nefes alır, bakınır (Bekle_Dongu).</summary>
        Idle,
        /// <summary>Önündeki işle uğraşır: tahtaya yazar, tezgâhta tartar, malı seçer (Calis_Dongu).</summary>
        Work,
    }

    public enum NpcPathMode
    {
        /// <summary>Yürümez; ilk durakta işini yapar.</summary>
        Static,
        /// <summary>Uçtan uca gider, döner gelir.</summary>
        PingPong,
        /// <summary>Son noktadan ilk noktaya bağlanan halka.</summary>
        Loop,
    }

    /// <summary>Rotadaki bir durak: hangi düğümde, ne kadar, ne yaparak ve nereye bakarak bekler.</summary>
    [Serializable]
    public struct NpcStop
    {
        [SerializeField] private int node;
        [SerializeField] private Vector2 waitSeconds;
        [SerializeField] private NpcActivity activity;
        [SerializeField] private bool faceTarget;
        [SerializeField] private Vector3 facePoint;

        public int Node => node;
        public Vector2 WaitSeconds => waitSeconds;
        public NpcActivity Activity => activity;
        public bool FaceTarget => faceTarget;
        public Vector3 FacePoint => facePoint;

        public NpcStop(int node, Vector2 waitSeconds, NpcActivity activity, bool faceTarget, Vector3 facePoint)
        {
            this.node = node;
            this.waitSeconds = waitSeconds;
            this.activity = activity;
            this.faceTarget = faceTarget;
            this.facePoint = facePoint;
        }
    }

    /// <summary>
    /// Şehirde dolaşan / atölyede çalışan NPC. Takılmaz, çünkü fizikle yürümez: kurulum aracının
    /// (Art/Editor/NpcYerlestir) yolların üstünde çizip kapsül taramasıyla doğruladığı, zemin yüksekliği
    /// fırınlanmış sık noktalı rotayı (≈0,5 m) birebir izler. Çalışma anında ışın/navmesh yok.
    ///
    /// Durumlar: Moving (rota boyunca) → Waiting (durakta iş/bekleme) → Moving …
    /// Önünde oyuncu ya da başka NPC varsa durur (Yielding); uzun sürerse geri döner (kilitlenme olmaz).
    /// Interact (E / X): durur, oyuncuya döner, konuşur (DialogueDirector, oyuncu hareketi durur);
    /// konuşma bitince kısa bir süre sonra kaldığı işe devam eder.
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class NpcAgent : WorldInteractable
    {
        private enum State { Waiting, Moving, Yielding, Talking }

        private const float CrossFade = 0.2f;
        private const float BlockCheckInterval = 0.15f;
        private const float MaxDeltaTime = 0.05f;
        private const float TurnInPlaceAngle = 50f;

        [Header("Kimlik")]
        [SerializeField] private NpcProfile profile;
        [Tooltip("Boş bırakılırsa sahnedeki ilk DialogueDirector kullanılır.")]
        [SerializeField] private DialogueDirector dialogue;
        [SerializeField] private Animator animator;
        [Tooltip("Animator durum adlarının öneki: <Önek>_Bekle_Dongu, _Yuru_Dongu, _Calis_Dongu, _Konus_Dongu.")]
        [SerializeField] private string clipPrefix = "";

        [Header("Hareket")]
        [SerializeField] private float walkSpeed = 1.0f;
        [Tooltip("Yürüme klibinin ayak kaymadan oynadığı hız (m/s); npc.py WALK_REF_SPEED.")]
        [SerializeField] private float clipWalkSpeed = 1.1f;
        [SerializeField] private float turnSpeed = 220f;
        [Tooltip("Önündeki oyuncu/NPC bu mesafedeyse durur (m).")]
        [SerializeField] private float personalSpace = 1.4f;
        [Tooltip("Yol verme bu kadar sürerse geri döner (sn).")]
        [SerializeField] private float giveUpSeconds = 5f;
        [Tooltip("Konuşma bitince işe dönmeden önce oyuncuya bakmaya devam ettiği süre (sn).")]
        [SerializeField] private float afterTalkPause = 1.2f;

        [Header("Rota (kurulum aracı doldurur)")]
        [SerializeField] private NpcPathMode pathMode = NpcPathMode.Static;
        [SerializeField] private Vector3[] path = Array.Empty<Vector3>();
        [SerializeField] private NpcStop[] stops = Array.Empty<NpcStop>();

        private static readonly List<NpcAgent> Active = new List<NpcAgent>();

        private readonly List<DialogueLine> lineBuffer = new List<DialogueLine>();
        private int[] stopAtNode;
        private State state;
        private int node, dir = 1;
        private float segDistance, timer, blockTimer, clearTime, yieldTime;
        private int currentStop = -1;
        private int conversationIndex;
        private bool talking;
        private Transform talkTarget;
        private Transform viewer;
        private float viewerRefresh;
        private string prompt;
        private int hIdle, hWork, hWalk, hTalk, hCurrent;

        public override bool CanInteract =>
            base.CanInteract && !talking && profile != null && profile.Conversations.Count > 0 &&
            dialogue != null && !dialogue.IsPlaying;

        public override string GetPrompt(PlayerInteractor interactor) => prompt;

        // ------------------------------------------------------------------ yaşam döngüsü
        protected override void Awake()
        {
            base.Awake();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (dialogue == null) dialogue = FindAnyObjectByType<DialogueDirector>();
            prompt = profile != null ? string.Format(profile.PromptFormat, profile.DisplayName) : string.Empty;

            hIdle = StateHash("_Bekle_Dongu", 0);
            hWork = StateHash("_Calis_Dongu", hIdle);
            hWalk = StateHash("_Yuru_Dongu", hIdle);
            hTalk = StateHash("_Konus_Dongu", hIdle);

            stopAtNode = new int[Mathf.Max(path.Length, 1)];
            for (int i = 0; i < stopAtNode.Length; i++) stopAtNode[i] = -1;
            for (int i = 0; i < stops.Length; i++)
                if (stops[i].Node >= 0 && stops[i].Node < path.Length) stopAtNode[stops[i].Node] = i;

            if (path.Length < 2) pathMode = NpcPathMode.Static;
        }

        private void OnEnable()
        {
            Active.Add(this);
            node = 0;
            dir = 1;
            segDistance = 0f;
            if (path.Length > 0) transform.position = path[0];
            // ilk düğüm bir duraksa orada başlar; değilse yola çıkar
            int s = path.Length > 0 ? stopAtNode[0] : -1;
            if (s >= 0 || pathMode == NpcPathMode.Static) EnterStop(Mathf.Max(s, 0), firstFrame: true);
            else EnterMoving();
        }

        private void OnDisable()
        {
            Active.Remove(this);
            if (talking && dialogue != null) dialogue.Finish();
            talking = false;
        }

        private int StateHash(string suffix, int fallback)
        {
            int h = Animator.StringToHash(clipPrefix + suffix);
            return animator != null && animator.HasState(0, h) ? h : fallback;
        }

        // ------------------------------------------------------------------ kare
        private void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, MaxDeltaTime);
            if (dt <= 0f) return;                        // duraklatma menüsü

            switch (state)
            {
                case State.Talking:
                    if (talkTarget != null) TurnTowards(talkTarget.position, dt);
                    if (!talking)
                    {
                        timer -= dt;
                        if (timer <= 0f) ResumeAfterTalk();
                    }
                    break;

                case State.Waiting:
                    if (currentStop >= 0 && stops[currentStop].FaceTarget) TurnTowards(stops[currentStop].FacePoint, dt);
                    if (pathMode == NpcPathMode.Static) break;
                    timer -= dt;
                    if (timer <= 0f) EnterMoving();
                    break;

                case State.Yielding:
                    yieldTime += dt;
                    if (BlockedThrottled(dt)) clearTime = 0f;
                    else clearTime += dt;
                    if (clearTime >= 0.5f) EnterMoving();
                    else if (yieldTime >= giveUpSeconds) { Reverse(); EnterMoving(); }
                    break;

                case State.Moving:
                    if (BlockedThrottled(dt)) { EnterYielding(); break; }
                    Move(dt);
                    break;
            }
        }

        private void Move(float dt)
        {
            int next = PeekNext(node, dir);
            if (next < 0) { Reverse(); next = PeekNext(node, dir); if (next < 0) { EnterStop(-1); return; } }

            Vector3 a = path[node], b = path[next];
            float len = Vector3.Distance(a, b);

            // bakış: ~1 m ileriye (köşeleri yumuşak alır); açı büyükse yerinde döner, sonra yürür
            Vector3 ahead = PointAhead(next, 2);
            float angle = TurnTowards(ahead, dt);
            float factor = angle > TurnInPlaceAngle ? 0f : 1f - angle / 120f;
            Play(hWalk, Mathf.Max(0.35f, walkSpeed * factor / Mathf.Max(0.1f, clipWalkSpeed)));

            segDistance += walkSpeed * factor * dt;
            // en fazla rota uzunluğu kadar düğüm atlanır (sıfır uzunluklu düğümler sonsuz döngü yapamaz)
            for (int guard = 0; guard <= path.Length && (len <= 0.0001f || segDistance >= len); guard++)
            {
                segDistance = Mathf.Max(0f, segDistance - len);
                node = next;
                int s = stopAtNode[node];
                if (s >= 0)
                {
                    segDistance = 0f;
                    transform.position = path[node];
                    EnterStop(s);
                    return;
                }
                next = PeekNext(node, dir);
                if (next < 0) { Reverse(); next = PeekNext(node, dir); if (next < 0) { EnterStop(-1); return; } }
                a = path[node];
                b = path[next];
                len = Vector3.Distance(a, b);
            }
            transform.position = len > 0.0001f ? Vector3.Lerp(a, b, Mathf.Clamp01(segDistance / len)) : a;
        }

        // ------------------------------------------------------------------ rota
        /// <summary>Gidiş yönündeki sonraki düğüm; uç noktada -1 (PingPong) ya da başa sarar (Loop).</summary>
        private int PeekNext(int from, int d)
        {
            int n = from + d;
            if (n >= 0 && n < path.Length) return n;
            if (pathMode == NpcPathMode.Loop) return n < 0 ? path.Length - 1 : 0;
            return -1;
        }

        private Vector3 PointAhead(int next, int count)
        {
            int n = next, d = dir;
            for (int i = 0; i < count; i++)
            {
                int k = PeekNext(n, d);
                if (k < 0) break;
                n = k;
            }
            return path[n];
        }

        /// <summary>Yön değiştirir; segmentin ortasındaysa kaldığı yerden geri yürür.</summary>
        private void Reverse()
        {
            int next = PeekNext(node, dir);
            if (next >= 0 && segDistance > 0f)
            {
                float len = Vector3.Distance(path[node], path[next]);
                node = next;
                segDistance = Mathf.Max(0f, len - segDistance);
            }
            dir = -dir;
        }

        // ------------------------------------------------------------------ durumlar
        private void EnterMoving()
        {
            if (pathMode == NpcPathMode.Static) { EnterStop(Mathf.Max(currentStop, 0)); return; }
            state = State.Moving;
            currentStop = -1;
            blockTimer = 0f;
        }

        private void EnterStop(int stopIndex, bool firstFrame = false)
        {
            state = State.Waiting;
            currentStop = stopIndex >= 0 && stopIndex < stops.Length ? stopIndex : -1;
            if (currentStop >= 0)
            {
                var st = stops[currentStop];
                timer = UnityEngine.Random.Range(st.WaitSeconds.x, Mathf.Max(st.WaitSeconds.x, st.WaitSeconds.y));
                if (firstFrame) timer *= UnityEngine.Random.value;        // herkes aynı anda yola çıkmasın
                if (firstFrame && st.FaceTarget) SnapFacing(st.FacePoint);
                Play(st.Activity == NpcActivity.Work ? hWork : hIdle, 1f);
            }
            else
            {
                timer = 1.5f;
                Play(hIdle, 1f);
            }
        }

        private void EnterYielding()
        {
            state = State.Yielding;
            yieldTime = 0f;
            clearTime = 0f;
            Play(hIdle, 1f);
        }

        // ------------------------------------------------------------------ konuşma
        public override void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract) return;
            var conv = profile.Conversations[conversationIndex % profile.Conversations.Count];
            conversationIndex++;
            lineBuffer.Clear();
            for (int i = 0; i < conv.Lines.Count; i++)
                lineBuffer.Add(new DialogueLine(profile.DisplayName, conv.Lines[i], profile.Portrait));
            if (lineBuffer.Count == 0) return;

            talking = true;
            talkTarget = interactor.transform;
            state = State.Talking;
            Play(hTalk, 1f);
            dialogue.Play(lineBuffer, OnTalkFinished, interactor);
        }

        private void OnTalkFinished()
        {
            talking = false;
            timer = afterTalkPause;
            Play(hIdle, 1f);
        }

        private void ResumeAfterTalk()
        {
            talkTarget = null;
            if (currentStop >= 0 || pathMode == NpcPathMode.Static)
            {
                // durağındaydı: işine geri döner, kalan süre en az birkaç saniye
                state = State.Waiting;
                timer = Mathf.Max(timer, 3f);
                int st = Mathf.Max(currentStop, 0);
                Play(st < stops.Length && stops[st].Activity == NpcActivity.Work ? hWork : hIdle, 1f);
            }
            else EnterMoving();
        }

        // ------------------------------------------------------------------ yol verme
        private bool lastBlocked;

        private bool BlockedThrottled(float dt)
        {
            blockTimer -= dt;
            if (blockTimer > 0f) return lastBlocked;
            blockTimer = BlockCheckInterval;
            lastBlocked = IsBlocked();
            return lastBlocked;
        }

        private bool IsBlocked()
        {
            Vector3 pos = transform.position;
            Vector3 fwd = MoveDirection();

            viewerRefresh -= BlockCheckInterval;
            if (viewer == null || viewerRefresh <= 0f)
            {
                var cam = Camera.main;                       // oyuncu kamerası (Play'de doğar); seyrek yenilenir
                viewer = cam != null ? cam.transform : null;
                viewerRefresh = 2f;
            }
            if (viewer != null && Blocks(pos, fwd, viewer.position)) return true;

            for (int i = 0; i < Active.Count; i++)
            {
                var o = Active[i];
                if (o == this || !o.isActiveAndEnabled) continue;
                if (Blocks(pos, fwd, o.transform.position)) return true;
            }
            return false;
        }

        private bool Blocks(Vector3 pos, Vector3 fwd, Vector3 other)
        {
            Vector3 to = other - pos;
            if (Mathf.Abs(to.y) > 2.2f) return false;
            to.y = 0f;
            float d = to.magnitude;
            if (d < 0.8f) return true;                         // çok yakın: hangi yandan olursa olsun
            return d < personalSpace && Vector3.Dot(fwd, to / d) > 0.3f;
        }

        private Vector3 MoveDirection()
        {
            int next = PeekNext(node, dir);
            if (next < 0) return transform.forward;
            Vector3 v = path[next] - path[node];
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : transform.forward;
        }

        // ------------------------------------------------------------------ yardımcılar
        /// <summary>Yataydaki hedefe döner; kalan açıyı (derece) döndürür.</summary>
        private float TurnTowards(Vector3 target, float dt)
        {
            Vector3 v = target - transform.position;
            v.y = 0f;
            if (v.sqrMagnitude < 0.0004f) return 0f;
            var want = Quaternion.LookRotation(v);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * dt);
            return Quaternion.Angle(transform.rotation, want);
        }

        private void SnapFacing(Vector3 target)
        {
            Vector3 v = target - transform.position;
            v.y = 0f;
            if (v.sqrMagnitude > 0.0004f) transform.rotation = Quaternion.LookRotation(v);
        }

        private void Play(int hash, float speed)
        {
            if (animator == null || hash == 0) return;
            animator.speed = speed;
            if (hash == hCurrent) return;
            hCurrent = hash;
            animator.CrossFadeInFixedTime(hash, CrossFade, 0);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (path == null || path.Length < 2) return;
            Gizmos.color = new Color(1f, 0.6f, 0.2f);
            for (int i = 0; i + 1 < path.Length; i++) Gizmos.DrawLine(path[i] + Vector3.up * 0.05f, path[i + 1] + Vector3.up * 0.05f);
            if (pathMode == NpcPathMode.Loop) Gizmos.DrawLine(path[path.Length - 1] + Vector3.up * 0.05f, path[0] + Vector3.up * 0.05f);
            Gizmos.color = new Color(0.3f, 0.8f, 1f);
            foreach (var s in stops)
                if (s.Node >= 0 && s.Node < path.Length) Gizmos.DrawWireSphere(path[s.Node] + Vector3.up * 0.1f, 0.25f);
        }

        /// <summary>Kurulum aracı (NpcYerlestir) çağırır.</summary>
        public void EditorSetup(NpcProfile npcProfile, DialogueDirector director, Animator anim, string prefix, float speed,
                                NpcPathMode mode, Vector3[] bakedPath, NpcStop[] npcStops)
        {
            profile = npcProfile;
            dialogue = director;
            animator = anim;
            clipPrefix = prefix;
            walkSpeed = speed;
            pathMode = mode;
            path = bakedPath;
            stops = npcStops;
        }
#endif
    }
}
