using UnityEngine;

namespace BilgiAdasi.Player
{
    /// <summary>
    /// Adım, zıplama ve iniş sesleri. PlayerMotor'un olaylarını dinler; ritim kafa sallanmasıyla aynıdır.
    /// Klip dizileri boşsa ses çalmaz (SFX sonra doldurulur; CC0 olmalı, LICENSES.md'ye yazılır).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class PlayerFootsteps : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private AudioSource source;

        [Header("Klipler (boş bırakılabilir)")]
        [SerializeField] private AudioClip[] footstepClips = new AudioClip[0];
        [SerializeField] private AudioClip[] jumpClips = new AudioClip[0];
        [SerializeField] private AudioClip[] landClips = new AudioClip[0];

        [Header("Ses")]
        [Range(0f, 1f)] [SerializeField] private float footstepVolume = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float jumpVolume = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float landVolume = 0.7f;
        [Tooltip("Koşarken adım sesi çarpanı.")]
        [SerializeField] private float sprintVolumeMultiplier = 1.25f;
        [Tooltip("Eğilirken adım sesi çarpanı.")]
        [SerializeField] private float crouchVolumeMultiplier = 0.5f;
        [Tooltip("Her adımda rastgele perde aralığı (tekrar hissini azaltır).")]
        [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);
        [Tooltip("Bu düşüş hızına (m/s) kadar iniş sesi orantılı yükselir.")]
        [SerializeField] private float landSpeedForFullVolume = 9f;

        private int lastFootstepIndex = -1;

        private void Awake()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (source == null) source = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (motor == null) return;
            motor.Footstep += OnFootstep;
            motor.Jumped += OnJumped;
            motor.Landed += OnLanded;
        }

        private void OnDisable()
        {
            if (motor == null) return;
            motor.Footstep -= OnFootstep;
            motor.Jumped -= OnJumped;
            motor.Landed -= OnLanded;
        }

        private void OnFootstep()
        {
            float volume = footstepVolume;
            if (motor.IsSprinting) volume *= sprintVolumeMultiplier;
            else if (motor.IsCrouching) volume *= crouchVolumeMultiplier;
            Play(footstepClips, Mathf.Clamp01(volume), ref lastFootstepIndex);
        }

        private void OnJumped()
        {
            int unused = -1;
            Play(jumpClips, jumpVolume, ref unused);
        }

        private void OnLanded(float impactSpeed)
        {
            int unused = -1;
            float t = landSpeedForFullVolume > 0f ? Mathf.Clamp01(impactSpeed / landSpeedForFullVolume) : 1f;
            Play(landClips, landVolume * Mathf.Lerp(0.3f, 1f, t), ref unused);
        }

        /// <summary>Diziden rastgele bir klip çalar; aynı klip art arda gelmez.</summary>
        private void Play(AudioClip[] clips, float volume, ref int lastIndex)
        {
            if (source == null || clips == null || clips.Length == 0) return;

            int index = Random.Range(0, clips.Length);
            if (clips.Length > 1 && index == lastIndex) index = (index + 1) % clips.Length;
            lastIndex = index;

            var clip = clips[index];
            if (clip == null) return;
            source.pitch = Random.Range(pitchRange.x, pitchRange.y);
            source.PlayOneShot(clip, volume);
        }
    }
}
