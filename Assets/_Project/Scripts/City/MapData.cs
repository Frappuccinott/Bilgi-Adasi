using System;
using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.City
{
    public enum MapPointKind { Workshop, Neighborhood, Market, LockedCity, Landmark }

    /// <summary>Haritadaki bir yer: dünyadaki konumu (x, z), adı, rengi; köyse durumu NeighborhoodData'dan okunur.</summary>
    [Serializable]
    public struct MapPoint
    {
        public string label;
        public MapPointKind kind;
        [Tooltip("Dünya konumu (x, z).")]
        public Vector2 world;
        [Tooltip("Kimlik rengi (09 paleti): köy, şehir, atölye.")]
        public Color color;
        [Tooltip("Yalnızca Neighborhood türünde: durum ve 5 nokta buradan.")]
        public NeighborhoodData neighborhood;
        [Tooltip("Mini haritada da gösterilsin mi?")]
        public bool onMinimap;
    }

    /// <summary>
    /// Harita ekranı ve mini haritanın verisi: üstten pişirilmiş görüntü (Kurulum → 17), görüntünün kapladığı dünya
    /// karesi ve yerler. Oyuncuya görünen bütün kelimeler burada (kodda metin yok).
    /// </summary>
    [CreateAssetMenu(menuName = "Bilgi Adası/Harita", fileName = "MapData")]
    public sealed class MapData : ScriptableObject
    {
        [Header("Görüntü (üstten, +Z yukarı)")]
        [SerializeField] private Texture2D image;
        [Tooltip("Görüntünün ortası (dünya x, z).")]
        [SerializeField] private Vector2 worldCenter = new Vector2(-5f, 12f);
        [Tooltip("Görüntünün kenarı (m). Kare.")]
        [SerializeField] private float worldSize = 196f;
        [Tooltip("Mini haritanın çapında görünen dünya genişliği (m).")]
        [SerializeField] private float minimapSpan = 64f;

        [Header("Yerler")]
        [SerializeField] private List<MapPoint> points = new List<MapPoint>();

        [Header("Metinler")]
        [SerializeField] private string title = "Kimya Şehri";
        [SerializeField] private string subtitle = "Etkileşim İlçesi";
        [SerializeField] private string legendTitle = "Köyler";
        [SerializeField] private string citiesTitle = "Şehirler";
        [SerializeField] private string statusOpen = "{0}/{1} görev";
        [SerializeField] private string statusDone = "Tamamlandı";
        [SerializeField] private string statusLocked = "Kilitli";
        [Tooltip("{0}: önce bitmesi gereken köy.")]
        [SerializeField] private string statusRequires = "Önce {0} bitmeli";
        [SerializeField] private string statusSoon = "Yakında";
        [SerializeField] private string currentWord = "Sıradaki";
        [SerializeField] private string youAreHere = "Buradasın";
        [SerializeField] private string closeWord = "Kapat";
        [SerializeField] private string mapWord = "Harita";
        [Tooltip("Mini haritada kuzeyi gösteren harf.")]
        [SerializeField] private string northLetter = "K";

        public Texture2D Image => image;
        public Vector2 WorldCenter => worldCenter;
        public float WorldSize => worldSize;
        public float MinimapSpan => minimapSpan;
        public IReadOnlyList<MapPoint> Points => points;
        public string Title => title;
        public string Subtitle => subtitle;
        public string LegendTitle => legendTitle;
        public string CitiesTitle => citiesTitle;
        public string StatusOpen => statusOpen;
        public string StatusDone => statusDone;
        public string StatusLocked => statusLocked;
        public string StatusRequires => statusRequires;
        public string StatusSoon => statusSoon;
        public string CurrentWord => currentWord;
        public string YouAreHere => youAreHere;
        public string CloseWord => closeWord;
        public string MapWord => mapWord;
        public string NorthLetter => northLetter;

        /// <summary>Dünya konumu → görüntü UV'si (0–1; dışarıdaysa taşar).</summary>
        public Vector2 WorldToUv(Vector3 p) => WorldToUv(new Vector2(p.x, p.z));

        public Vector2 WorldToUv(Vector2 xz) =>
            new Vector2((xz.x - worldCenter.x) / worldSize + 0.5f, (xz.y - worldCenter.y) / worldSize + 0.5f);

#if UNITY_EDITOR
        public void EditorSetImage(Texture2D tex, Vector2 center, float size)
        {
            image = tex;
            worldCenter = center;
            worldSize = size;
        }

        public void EditorSetPoints(List<MapPoint> list) => points = list;
#endif
    }
}
