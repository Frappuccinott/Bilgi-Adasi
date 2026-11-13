using System;
using System.Collections.Generic;
using UnityEngine;

namespace BilgiAdasi.City
{
    public enum MapPointState { Open, Current, Done, Locked, Soon }

    /// <summary>Haritada çizilecek bir yer (MapController hazırlar; view yalnızca çizer).</summary>
    public sealed class MapPointInfo
    {
        public string Label;
        public MapPointKind Kind;
        public Vector2 Uv;
        public Color Color;
        public MapPointState State;
        /// <summary>Durum satırı: "2/5 görev", "Kilitli", "Tamamlandı" …</summary>
        public string Status;
        /// <summary>Köyde biten görev / toplam görev (köy değilse 0/0).</summary>
        public int Done, Total;
        public bool OnMinimap;
    }

    public sealed class MapScreenInfo
    {
        public Texture Image;
        public string Title, Subtitle, LegendTitle, CitiesTitle, CurrentWord, YouAreHere, CloseWord, MapWord, NorthLetter;
        /// <summary>Mini haritanın çapına düşen görüntü oranı (span / worldSize).</summary>
        public float MinimapUvSpan;
        public readonly List<MapPointInfo> Points = new List<MapPointInfo>();
    }

    /// <summary>Büyük harita ekranı (M · Tab · Select). Açıkken girdi UI modunda.</summary>
    public interface IMapView
    {
        bool IsOpen { get; }
        void Open(MapScreenInfo info, Action onCloseRequested);
        void Close();
        /// <summary>Oyuncunun yeri (UV) ve bakış yönü (derece, kuzeyden saat yönünde).</summary>
        void SetPlayer(Vector2 uv, float yawDegrees);
    }

    /// <summary>Sol alttaki mini harita: oyuncu ortada, bakış yönü yukarıda.</summary>
    public interface IMinimapView
    {
        void Setup(MapScreenInfo info);
        void SetVisible(bool visible);
        void SetPlayer(Vector2 uv, float yawDegrees);
    }
}
