using System.Collections.Generic;
using BilgiAdasi.Chemistry;
using UnityEngine;

namespace BilgiAdasi.Order
{
    /// <summary>
    /// İstasyonda fareyle tıklanan ya da gamepad'le seçilen nesne. Collider bu objede ya da çocuğunda olur.
    /// Açık hedefler statik listede tutulur (gamepad sırası ekran X konumuna göre).
    /// </summary>
    public sealed class StationTarget : MonoBehaviour
    {
        [SerializeField] private StationTargetKind kind = StationTargetKind.Jar;
        [SerializeField] private SpeciesData species;
        [Tooltip("Seçilince büyüyen görsel (boşsa bu obje).")]
        [SerializeField] private Transform visual;

        private static readonly List<StationTarget> ActiveTargets = new List<StationTarget>();
        public static IReadOnlyList<StationTarget> Active => ActiveTargets;

        private Vector3 baseScale;
        private bool focused;

        public StationTargetKind Kind => kind;
        public SpeciesData Species => species;

        public void Setup(StationTargetKind targetKind, SpeciesData targetSpecies, Transform targetVisual)
        {
            kind = targetKind;
            species = targetSpecies;
            visual = targetVisual;
            baseScale = Visual.localScale;
        }

        private Transform Visual => visual != null ? visual : transform;

        private void Awake() => baseScale = Visual.localScale;

        private void OnEnable() => ActiveTargets.Add(this);

        private void OnDisable()
        {
            ActiveTargets.Remove(this);
            SetFocused(false);
        }

        public void SetFocused(bool value)
        {
            if (focused == value) return;
            focused = value;
            Visual.localScale = value ? baseScale * 1.12f : baseScale;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ActiveTargets.Clear();
    }
}
