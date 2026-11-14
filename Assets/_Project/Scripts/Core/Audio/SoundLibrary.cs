using System;
using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.Core
{
    /// <summary>
    /// Ses kütüphanesi: her <see cref="SoundId"/> için klipler, ses düzeyi ve perde aralığı; müzik ve ortam döngüleri.
    /// Klipler ArtSource/Audio/ses_sentez.py ile sıfırdan üretilir (Assets/_Project/Audio).
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Ses Kütüphanesi", fileName = "SoundLibrary")]
    public sealed class SoundLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private SoundId id;
            [Tooltip("Birden çok klip varsa rastgele biri çalar (art arda aynısı gelmez).")]
            [SerializeField] private AudioClip[] clips = new AudioClip[0];
            [Range(0f, 1f)] [SerializeField] private float volume = 0.8f;
            [SerializeField] private Vector2 pitch = new Vector2(0.96f, 1.04f);
            [Tooltip("Aynı ses bu süreden sık çalmaz (sn). Gamepad çubuğu gibi tekrarlayan girdiler için.")]
            [SerializeField] private float minInterval = 0.03f;

            public SoundId Id => id;
            public IReadOnlyList<AudioClip> Clips => clips;
            public float Volume => volume;
            public Vector2 Pitch => pitch;
            public float MinInterval => minInterval;

            public Entry(SoundId id, AudioClip[] clips, float volume, Vector2 pitch, float minInterval)
            {
                this.id = id;
                this.clips = clips;
                this.volume = volume;
                this.pitch = pitch;
                this.minInterval = minInterval;
            }
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        [Header("Döngüler")]
        [SerializeField] private AudioClip music;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.32f;
        [Tooltip("Duraklatma menüsünde müzik bu çarpanla kısılır.")]
        [Range(0f, 1f)] [SerializeField] private float pausedMusicMultiplier = 0.45f;
        [SerializeField] private AudioClip ambience;
        [Range(0f, 1f)] [SerializeField] private float ambienceVolume = 0.45f;

        [Header("3B ses")]
        [Tooltip("Konumlu seslerin tam duyulduğu uzaklık (m).")]
        [SerializeField] private float spatialMinDistance = 2f;
        [Tooltip("Konumlu seslerin söndüğü uzaklık (m).")]
        [SerializeField] private float spatialMaxDistance = 22f;

        public AudioClip Music => music;
        public float MusicVolume => musicVolume;
        public float PausedMusicMultiplier => pausedMusicMultiplier;
        public AudioClip Ambience => ambience;
        public float AmbienceVolume => ambienceVolume;
        public float SpatialMinDistance => spatialMinDistance;
        public float SpatialMaxDistance => spatialMaxDistance;
        public IReadOnlyList<Entry> Entries => entries;

        /// <summary>Kurulum aracı için: kaydı değiştirir ya da ekler.</summary>
        public void SetEntry(Entry entry)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Id != entry.Id) continue;
                entries[i] = entry;
                return;
            }
            entries.Add(entry);
        }

        public void SetLoops(AudioClip musicClip, AudioClip ambienceClip)
        {
            music = musicClip;
            ambience = ambienceClip;
        }
    }
}
