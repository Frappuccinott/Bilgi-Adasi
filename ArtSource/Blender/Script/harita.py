# Kasaba haritası (Town): arazi, yollar, dere, meydan, köy kökleri.
# Referans ve yerleşim: ArtSource/Referans/harita.md. Kurallar: 09-tema-ve-model-kurallari.md.
# Yarışma süresince yazıldı (0'dan üret). Blender 5.x.
#
# Kullanım (Blender içinden):
#   p = r"<repo>/ArtSource/Blender/Script/harita.py"
#   g = {"__name__": "harita"}; exec(compile(open(p, encoding="utf-8").read(), p, "exec"), g)
#   g["build_harita"]()
#
# Koordinat: X doğu, Y kuzey, Z yukarı (atölye ile aynı: atölye kapısı güneye, -Y).
# Dışa aktarımda Blender -Y → Unity +Z; yani Unity'de kuzey = -Z. Konumlar harita.md §2.

import os
import math
import random
import bmesh
import bpy
from mathutils import Vector, Matrix, noise

_HERE = r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\ArtSource\Blender\Script"
_lib = os.path.join(_HERE, "ba_lib.py")
exec(compile(open(_lib, encoding="utf-8").read(), _lib, "exec"), globals())

COL = "Harita"
EXPORT_DIR = r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\Assets\_Project\Art\Harita"

# ------------------------------------------------------------------ yerleşim (harita.md §2)
ATOLYE = Vector((0.0, 12.0))
ATOLYE_HALF = (4.45 + 2.0, 3.7 + 2.0)        # dış yarı ölçü + çevre şeridi
MEYDAN = Vector((0.0, -8.0))
MEYDAN_R = 11.0
DENK = Vector((52.0, -6.0))                   # Denkleştirme köyü merkezi
TEPK = Vector((-46.0, -2.0))                  # Tepkime Türleri köyü merkezi

# oyun alanı (süper-elips): bunun dışında arazi yükselir (vadi duvarları)
PLAY_C = Vector((8.0, -3.0))
PLAY_A = (84.0, 40.0)

def _chaikin2(pts, it=3):
    pts = [tuple(p) for p in pts]
    for _ in range(it):
        out = [pts[0]]
        for a, b in zip(pts, pts[1:]):
            out.append((a[0] * 0.75 + b[0] * 0.25, a[1] * 0.75 + b[1] * 0.25))
            out.append((a[0] * 0.25 + b[0] * 0.75, a[1] * 0.25 + b[1] * 0.75))
        out.append(pts[-1])
        pts = out
    return [Vector(p) for p in pts]

# yollar: (ad, nokta listesi, genişlik). Meydana giren uçlar döşemenin altında kalır.
ROADS = [
    ("Yol_Bati", [(-6, -7), (-16, -6.5), (-26, -4.5), (-36, -3.0), (-46, -2.0), (-56, -1.0)], 3.8),
    ("Yol_Dogu", [(6, -8.5), (16, -9.5), (26, -8.5), (33, -8.0), (42, -7.0), (52, -6.0), (60, -5.0)], 3.8),
    ("Yol_Atolye", [(-1.2, -1.0), (-1.4, 3.0), (-1.4, 7.6)], 2.8),
    # tarla yolu Bereket Tarlası'nın güney giriş kapısında (60, 15.5) biter (denetim 13.11: köşedeki çite çıkıyordu)
    ("Yol_Tarla", [(58, -5.4), (64.6, -0.2), (66.6, 6.8), (63.8, 12.6), (60.0, 15.2)], 2.6),
    ("Yol_Hesaplamalar", [(59, -6.0), (66, -14.0), (72, -24.0), (76, -32.0)], 3.0),   # kilitli
    ("Yol_Tepkimeler", [(-50, -1.6), (-56, 6.0), (-62, 15.0), (-66, 24.0)], 3.0),    # kilitli
    # kuleye kısa sapak: Tepkimeler yolundan ayrılır (eskiden ona paralel ikinci yol gibi görünüyordu)
    ("Yol_Kule", [(-56.4, 6.4), (-55.7, 8.5)], 2.2),
    # geçici tahta köprüye inen patikalar (Denkleştirme Part_4 bitene kadar dereyi buradan geçeriz)
    # yıkık taş köprünün ayaklarının (x 24.6…38.6, y −9.9…−6.1) dışından geçer (Unity yürüme testi, 13.11)
    ("Patika_Kopru_Bati", [(21.5, -9.0), (23.6, -5.2), (26.0, -2.4)], 1.5),
    ("Patika_Kopru_Dogu", [(36.2, -2.4), (38.8, -3.3), (40.3, -6.4)], 1.5),
    # kilitli şehirlere giden bisiklet yolları: tepeleri boğaz geçidiyle yarar, uçta şehir kapısı (harita_yon.py)
    ("Yol_Matematik", [(14.0, -9.3), (15.0, 0.0), (17.0, 14.0), (19.5, 30.0), (20.5, 48.0), (21.5, 66.0), (21.5, 98.0)], 3.4),
    ("Yol_Turkce", [(-21.0, -5.8), (-22.0, -18.0), (-24.0, -34.0), (-25.5, -50.0), (-24.5, -66.0), (-23.0, -101.0)], 3.4),
]
GECIT_YOLLARI = ("Yol_Matematik", "Yol_Turkce")
GECIT_KAPI = {"Yol_Matematik": (21.5, 84.0), "Yol_Turkce": (-23.6, -84.0)}
ROADS = [(n, _chaikin2(p, 3), w) for (n, p, w) in ROADS]

# dere: kuzeyde vadi basamağının dibindeki pınar havuzundan doğar, vadiyi geçer (doğu yolunu x≈32'de keser, köprü),
# güneyde basamağın dibindeki sazlık gölette biter. Eskiden iki ucu tepeye tırmanıyordu (su yokuş yukarı, denetim
# 13.11); basamaktaki çağlayan şeridi de Play'de yamaçta düz beyaz levha gibi duruyordu → kaldırıldı.
STREAM = _chaikin2([(36.2, 35.4), (34.6, 26.0), (33, 18), (30.5, 2), (31.5, -8), (34, -22), (31.6, -35.5)], 3)
STREAM_HALF = 4.6        # kıyıdan kıyıya yarı genişlik
STREAM_DEPTH = 1.5
WATER_Z = 0.55           # yatak tabanından su yüzü
# göletler: (merkez, yarıçap). Kuzeyde çağlayan havuzu, güneyde derenin bittiği sazlık gölet.
GOLLER = [(Vector((36.2, 35.4)), 3.4), (Vector((31.4, -37.2)), 5.0)]
GOL_DEPTH = 1.6

KOPRU = Vector((31.6, -8.0))

# ------------------------------------------------------------------ yardımcılar
def smooth01(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)

def seg_dist(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / max(ab.length_squared, 1e-9)))
    return (p - (a + ab * t)).length, t

def poly_dist(p, pts):
    best = 1e9
    for a, b in zip(pts, pts[1:]):
        d, _ = seg_dist(p, a, b)
        if d < best:
            best = d
    return best

def n2(x, y, s, seed=0.0):
    return noise.noise(Vector((x / s + seed, y / s - seed * 0.7, seed * 1.3)))

def play_outside(x, y):
    """Oyun alanının dışına uzaklık (m); içeride ≤ 0."""
    dx = abs(x - PLAY_C.x) / PLAY_A[0]
    dy = abs(y - PLAY_C.y) / PLAY_A[1]
    d = (dx ** 4 + dy ** 4) ** 0.25
    return (d - 1.0) * (PLAY_A[1] * 1.25)

# ------------------------------------------------------------------ yükseklik
def height(x, y, with_stream=True):
    p = Vector((x, y))
    # taban: çok yumuşak dalga (yol eğimi ≤ %5)
    h = 0.35 * n2(x, y, 28.0, 1.7) + 0.15 * n2(x, y, 11.0, 4.2)

    # vadi duvarları: dışarı çıktıkça hızla dikleşen tombul tepeler
    d = play_outside(x, y)
    if d > 0:
        south = 0.6 if y < PLAY_C.y else 1.0
        # 1) dipte kısa, dik kaya basamağı (> 45°, yürünmez sınır; tema: düz yüzlü iri kayalar)
        ledge = (3.6 + 1.2 * n2(x, y, 14.0, 5.5)) * smooth01(d / 3.2)
        # 2) üstte tombul yeşil kubbeler (tek tek tepeler: büyük ölçekli gürültüyle)
        dd = max(d - 2.5, 0.0)
        dome = 30.0 * (1 - math.exp(-(dd / 26.0) ** 1.6))
        lump = 0.62 + 0.38 * (0.5 + 0.5 * n2(x, y, 31.0, 7.1)) + 0.12 * n2(x, y, 11.0, 2.3)
        h += (ledge + dome * lump) * south

    # atölye düzlüğü: tam 0
    ax = abs(x - ATOLYE.x) - ATOLYE_HALF[0]
    ay = abs(y - ATOLYE.y) - ATOLYE_HALF[1]
    da = max(ax, ay, 0.0) if (ax > 0 or ay > 0) else 0.0
    h *= smooth01(da / 7.0)

    # meydan: düz
    dm = (p - MEYDAN).length - MEYDAN_R
    h *= 0.15 + 0.85 * smooth01(dm / 8.0)

    # şehir yolları: tepeyi boğaz gibi yar (taban düz, yanlar dik kaya → yürünmez duvar, görünmez duvar gerekmez)
    if d > -2:
        for n_, pts, w in ROADS:
            if n_ not in GECIT_YOLLARI:
                continue
            dr = poly_dist(p, pts) - w * 0.5
            if dr < 13.0:
                k = smooth01((dr - 2.2) / 8.5)
                floor = 0.25 * n2(x, y, 9.0, 3.3)
                h = floor * (1 - k) + h * k

    # yollar: gürültüyü bastır (yol düz, kıyılar yumuşak)
    for _, pts, w in ROADS:
        dr = poly_dist(p, pts) - w * 0.5
        if dr < 6.0:
            h *= 0.25 + 0.75 * smooth01(dr / 6.0)

    # dere yatağı + göletler (ikisi birden derinleştirmesin: en derin olan geçerli)
    if with_stream:
        cut = 0.0
        ds = poly_dist(p, STREAM)
        if ds < STREAM_HALF:
            k = smooth01(1.0 - (ds - 1.6) / (STREAM_HALF - 1.6)) if ds > 1.6 else 1.0
            cut = STREAM_DEPTH * k
        for gc, gr in GOLLER:
            dg = (p - gc).length
            if dg < gr + 1.2:
                k = smooth01(1.0 - (dg - (gr - 2.0)) / 3.2) if dg > gr - 2.0 else 1.0
                cut = max(cut, GOL_DEPTH * k)
        h -= cut
    return h

def gol_su_z(gc):
    """Gölet su yüzü: oyulmuş taban + dere ile aynı su payı."""
    return height(gc.x, gc.y) + WATER_Z + 0.05

# ------------------------------------------------------------------ gerçek arazi ağı yüksekliği
# height() sürekli fonksiyondur; SM_Arazi ise 2.5 m ızgara + eğime göre köşegen seçimli üçgen ağıdır. Üstüne serilen
# katmanlar (yol, köy zemini, bahçe, tarla, meydan) fonksiyona göre konunca ağ yer yer onların içinden çıkıyordu
# (Unity çakışma taraması 13.11: köy zemini–arazi 656, yol–köy zemini 575 nokta). Katmanlar artık ağın kendisine oturur.
_hizgara = {}

def _hg(i, j):
    k = (i, j)
    v = _hizgara.get(k)
    if v is None:
        v = height(X0 + i * STEP, Y0 + j * STEP)
        _hizgara[k] = v
    return v

def arazi_z(x, y):
    """SM_Arazi ağının (build_terrain ile aynı üçgenleme) bu noktadaki yüksekliği."""
    fi, fj = (x - X0) / STEP, (y - Y0) / STEP
    i, j = int(math.floor(fi)), int(math.floor(fj))
    u, v = fi - i, fj - j
    a, b, c, d = _hg(i, j), _hg(i + 1, j), _hg(i + 1, j + 1), _hg(i, j + 1)
    if abs(a - c) < abs(b - d):                    # köşegen a–c: (a,b,c) + (a,c,d)
        if u >= v:
            return a + (b - a) * u + (c - b) * v
        return a + (d - a) * v + (c - d) * u
    if u + v <= 1.0:                               # köşegen b–d: (a,b,d) + (b,c,d)
        return a + (b - a) * u + (d - a) * v
    return c + (d - c) * (1.0 - u) + (b - c) * (1.0 - v)

def zemin_ust(x, y, r=0.5):
    """Katman serme yüksekliği: noktada ve çevresinde (±r) arazi ağının ve fonksiyonun en yükseği.
    Katman üçgeni ağın sırt çizgisini geçse bile ağ katmanın içinden çıkmaz."""
    return max(height(x, y), arazi_z(x, y), arazi_z(x + r, y), arazi_z(x - r, y), arazi_z(x, y + r), arazi_z(x, y - r))

def road_info(p):
    """(yol kenarına uzaklık, yol adı) — boyama için."""
    best, nm = 1e9, None
    for n, pts, w in ROADS:
        d = poly_dist(p, pts) - w * 0.5
        if d < best:
            best, nm = d, n
    return best, nm

# ------------------------------------------------------------------ ARAZİ
X0, X1, Y0, Y1, STEP = -136.0, 150.0, -104.0, 100.0, 2.5

def terrain_color(x, y, z, nz):
    p = Vector((x, y))
    # çimen: büyük lekeler (09 §3 "zemin büyük lekelerle renk değiştirir")
    blob = 0.5 + 0.5 * n2(x, y, 17.0, 3.3)
    blob2 = 0.5 + 0.5 * n2(x, y, 6.0, 9.1)
    c = mix(pal("cimen", 1), pal("cimen", 0), smooth01(blob * 1.3 - 0.25))
    c = mix(c, pal("cimen", 2), 0.35 * smooth01((0.45 - blob2) * 3.0))
    # tepelerde açık, çukurda koyu (alttan üste)
    c = mix(c, pal("yaprak", 0), 0.25 * smooth01(z / 20.0))
    c = mix(c, pal("cimen", 2), 0.45 * smooth01(-z / 1.2))
    # dik yamaç → kaya
    if nz < 0.72:
        t = smooth01((0.72 - nz) / 0.15)
        rock = mix(pal("kaya", 1), pal("kaya", 0), 0.5 + 0.5 * n2(x, y, 5.0, 1.1))
        c = mix(c, rock, t * 0.85)
    # dere yatağı: kaya + su kıyısı
    ds = poly_dist(p, STREAM)
    for gc, gr in GOLLER:
        ds = min(ds, max((p - gc).length - (gr - 2.0) + 1.6, 0.0))
    if ds < STREAM_HALF + 0.5:
        t = smooth01(1.0 - (ds - 1.5) / (STREAM_HALF - 1.0))
        bed = mix(pal("kaya", 1), pal("dag", 1), 0.4)
        c = mix(c, bed, t)
    # yol kenarı: toprağa kayan aşınmış çimen
    dr, _ = road_info(p)
    if dr < 1.6:
        c = mix(c, mix(pal("kil", 1), pal("cimen", 2), 0.4), 0.55 * smooth01(1.0 - dr / 1.6))
    # meydan çevresi: ayak izi aşınması
    dm = (p - MEYDAN).length - MEYDAN_R
    if dm < 2.5:
        c = mix(c, mix(pal("kil", 1), pal("cimen", 2), 0.3), 0.5 * smooth01(1.0 - dm / 2.5))
    return c

ATOLYE_DELIK = (4.2, 3.45)     # duvar iç yüzü 4.0 / 3.25, dış yüzü 4.45 / 3.70 → delik kenarı duvarın içinde kalır

def atolye_deligi(bm):
    """Arazi atölyenin içinden geçiyor, döşemeyle aynı yükseklikte (0.000 / −0.001) titriyordu; çimen döşemeden
    görünüyordu (Unity çakışma taraması 13.11, 151 nokta). Atölye tabanının altında arazide dikdörtgen delik açılır;
    kenarı duvarın içinde kalır, içeride yalnız atölye döşemesi (kendi MeshCollider'ı) olur."""
    hx, hy = ATOLYE_DELIK
    cx, cy = ATOLYE.x, ATOLYE.y
    def yakin():
        fs = [f for f in bm.faces if all(abs(v.co.x - cx) < hx + 3.0 and abs(v.co.y - cy) < hy + 3.0 for v in f.verts)]
        es = list({e for f in fs for e in f.edges})
        vs = list({v for f in fs for v in f.verts})
        return vs + es + fs
    for co, no in (((cx - hx, 0, 0), (1, 0, 0)), ((cx + hx, 0, 0), (1, 0, 0)), ((0, cy - hy, 0), (0, 1, 0)), ((0, cy + hy, 0), (0, 1, 0))):
        bmesh.ops.bisect_plane(bm, geom=yakin(), plane_co=co, plane_no=no, dist=1e-5)
    ic = []
    for f in bm.faces:
        c = f.calc_center_median()
        if abs(c.x - cx) < hx and abs(c.y - cy) < hy:
            ic.append(f)
    bmesh.ops.delete(bm, geom=ic, context="FACES")

def build_terrain(col):
    nx = int((X1 - X0) / STEP) + 1
    ny = int((Y1 - Y0) / STEP) + 1
    bm = bmesh.new()
    grid = []
    for j in range(ny):
        row = []
        y = Y0 + j * STEP
        for i in range(nx):
            x = X0 + i * STEP
            row.append(bm.verts.new((x, y, height(x, y))))
        grid.append(row)
    for j in range(ny - 1):
        for i in range(nx - 1):
            a, b, c, d = grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i]
            # köşegeni eğime göre seç (kırık sırt çizgisi olmasın)
            if abs(a.co.z - c.co.z) < abs(b.co.z - d.co.z):
                bm.faces.new((a, b, c)); bm.faces.new((a, c, d))
            else:
                bm.faces.new((a, b, d)); bm.faces.new((b, c, d))
    atolye_deligi(bm)
    bm.normal_update()
    lay = bm.loops.layers.float_color.new("Col")
    vcol = {}
    bm.verts.index_update()
    for v in bm.verts:
        vcol[v.index] = terrain_color(v.co.x, v.co.y, v.co.z, v.normal.z)
    for f in bm.faces:
        for l in f.loops:
            l[lay] = vcol[l.vert.index]
    ob = to_object(bm, "SM_Arazi", col, sharp_angle=0)
    return ob

# ------------------------------------------------------------------ YOLLAR (ayrı ince şerit; keskin kenar, çarpışmasız)
def resample(pts, step):
    out = [pts[0]]
    acc = 0.0
    for a, b in zip(pts, pts[1:]):
        seg = (b - a).length
        t = step - acc
        while t <= seg:
            out.append(a.lerp(b, t / seg))
            t += step
        acc = seg - (t - step)
    if (out[-1] - pts[-1]).length > step * 0.3:
        out.append(pts[-1])
    return out

# Yol katman sırası (Play'de kavşakta yan yolun kare ucu ana yolun üstünde duruyordu, 13.11): ana yollar en üstte,
# yan yollar ve patikalar altında → yan yolun ucu ana yolun altında kalır, kavşak düz okunur. Köy zemini 0.03'te.
def _yol_lift(name):
    if name in ("Yol_Bati", "Yol_Dogu"):
        return 0.075
    if name.startswith("Patika") or name == "Yol_Kule":
        return 0.045
    return 0.06

def _cikmaz_uc(name, p):
    """Uç başka bir yolun şeridinde ya da meydanda değilse çıkmaz uçtur: sivrilip toprağa iner (kare uç kalmaz).
    Köprüye inen patika uçları ve atölye kapısındaki uç çıkmaz sayılmaz (köprüye/eşiğe kadar tam genişlik)."""
    if (p - MEYDAN).length < MEYDAN_R + 0.5:
        return False
    if poly_dist(p, STREAM) < STREAM_HALF + 2.5 or (p - ATOLYE).length < 7.0:
        return False
    for n_, pts_, w_ in ROADS:
        if n_ != name and poly_dist(p, pts_) < w_ * 0.5 + 0.3:
            return False
    return True

def build_roads(col):
    parts = []
    rnd = random.Random(77)
    for k, (name, pts, w) in enumerate(ROADS):
        cik0, cik1 = _cikmaz_uc(name, pts[0]), _cikmaz_uc(name, pts[-1])
        pts = resample(pts, 1.25)
        lift = _yol_lift(name)
        cum = [0.0]
        for a_, b_ in zip(pts, pts[1:]):
            cum.append(cum[-1] + (b_ - a_).length)
        L = cum[-1]
        secs = []
        for i, p in enumerate(pts):
            a = pts[max(i - 1, 0)]
            b = pts[min(i + 1, len(pts) - 1)]
            t = (b - a).normalized()
            nrm = Vector((-t.y, t.x))
            uc = 1.0
            if cik0:
                uc = min(uc, smooth01(cum[i] / 3.0))
            if cik1:
                uc = min(uc, smooth01((L - cum[i]) / 3.0))
            hw = w * 0.5 * (1.0 + 0.08 * n2(p.x, p.y, 4.0, k + 0.5)) * (0.35 + 0.65 * uc)
            li = lift * uc - 0.02 * (1.0 - uc)
            offs = [-(hw + 0.45), -hw, -hw * 0.35, hw * 0.35, hw, hw + 0.45]
            row = []
            for o in offs:
                q = p + nrm * o
                if abs(o) <= hw + 1e-4:
                    z = zemin_ust(q.x, q.y) + li
                else:
                    z = min(height(q.x, q.y), arazi_z(q.x, q.y)) - 0.06      # kenar eteği ağın altına iner
                row.append((Vector((q.x, q.y, z)), abs(o) / (hw + 0.45)))
            secs.append(row)
        bm = bmesh.new()
        lay = bm.loops.layers.float_color.new("Col")
        vs = [[bm.verts.new(c) for (c, _) in row] for row in secs]
        # dere yatağında yol çizilmez (köprü geçer); köprü ayağı/rampası üstünde de çizilmez (yıkık köprüyle çakışıyordu)
        wet = [poly_dist(p, STREAM) < STREAM_HALF - 0.6 or (name == "Yol_Dogu" and (p - KOPRU).length < 7.2) for p in pts]
        # meydan döşemesinin altında yol çizilmez (derz tabanıyla 1 cm içinde çakışıyordu)
        mey = [(p - MEYDAN).length < MEYDAN_R - 0.3 for p in pts]
        for i in range(len(vs) - 1):
            if wet[i] or wet[i + 1] or (mey[i] and mey[i + 1]):
                continue
            for j in range(len(vs[0]) - 1):
                f = bm.faces.new((vs[i][j], vs[i][j + 1], vs[i + 1][j + 1], vs[i + 1][j]))
                for l in f.loops:
                    jj = vs[i].index(l.vert) if l.vert in vs[i] else vs[i + 1].index(l.vert)
                    edge = jj in (0, 5)
                    mid = jj in (2, 3)
                    pp = l.vert.co
                    blob = 0.5 + 0.5 * n2(pp.x, pp.y, 3.0, 5.5 + k)
                    if edge:
                        c = mix(pal("cimen", 2), pal("kil", 1), 0.45)
                    elif mid:
                        c = mix(pal("kil", 0), mix(pal("kil", 0), pal("altin", 0), 0.25), blob * 0.6)
                    else:
                        c = mix(pal("kil", 1), pal("kil", 0), 0.35 + 0.4 * blob)
                    l[lay] = c
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        parts.append((bm, None))
    ob = to_object(bm_merge(*parts), "SM_Yollar", col, sharp_angle=0)
    return ob

# ------------------------------------------------------------------ MEYDAN DÖŞEMESİ (iri, düzensiz taş levhalar)
def build_meydan(col):
    """Şaşırtmalı (yarım kaydırmalı) iri taş levhalar; her levha ayrı köşeli → keskin ton, araları koyu derz."""
    rnd = random.Random(88)
    bm = bmesh.new()
    lay = bm.loops.layers.float_color.new("Col")
    cell = 1.15
    gap = 0.05
    R = MEYDAN_R
    def zat(x, y, lift):
        return (zemin_ust(x, y, 0.4) if lift > 0 else min(height(x, y), arazi_z(x, y))) + lift
    # 1) derz tabanı (koyu disk) + dış bordür halkası aşağı iner
    segs = 40
    cen = bm.verts.new((MEYDAN.x, MEYDAN.y, zat(MEYDAN.x, MEYDAN.y, 0.035)))
    ring_in, ring_out = [], []
    for i in range(segs):
        a = 2 * math.pi * i / segs
        rr = R * (1.0 + 0.03 * n2(math.cos(a) * 6, math.sin(a) * 6, 2.0, 1.0))
        x, y = MEYDAN.x + math.cos(a) * rr, MEYDAN.y + math.sin(a) * rr
        ring_in.append(bm.verts.new((x, y, zat(x, y, 0.035))))
        x2, y2 = MEYDAN.x + math.cos(a) * (rr + 0.5), MEYDAN.y + math.sin(a) * (rr + 0.5)
        ring_out.append(bm.verts.new((x2, y2, zat(x2, y2, -0.08))))
    joint = mix(pal("kaya", 2), pal("cimen", 2), 0.25)
    for i in range(segs):
        j = (i + 1) % segs
        for f in (bm.faces.new((cen, ring_in[i], ring_in[j])), bm.faces.new((ring_in[i], ring_out[i], ring_out[j], ring_in[j]))):
            for l in f.loops:
                l[lay] = joint
    # 2) levhalar
    n = int(R / cell) + 2
    # Sıra boyunca levhalar uç uca dizilir. Eski sürümde 1.5 kat geniş seçilen levha komşusunun üstüne biniyordu;
    # aralarındaki birkaç mm'lik yükseklik farkı meydanda benekli titreme yapıyordu (katman taraması 13.11).
    hucreler = []
    for j in range(-n, n + 1):
        u = -n * cell + ((cell * 0.5) if j % 2 else 0.0)
        while u < n * cell:
            w = cell * rnd.choice((1.0, 1.0, 1.5))
            hucreler.append((u, w, j * cell))
            u += w
    for (u0, w, v0) in hucreler:
        if True:
            cx, cy = MEYDAN.x + u0 + w / 2, MEYDAN.y + v0 + cell / 2
            if math.hypot(cx - MEYDAN.x, cy - MEYDAN.y) > R - 0.9:
                continue
            k = 0.94 + 0.12 * rnd.random()
            t = rnd.random()
            c = mix(pal("kaya", 1), pal("kaya", 0), 0.15 + 0.75 * t)
            r2 = rnd.random()
            if r2 < 0.22:
                c = mix(c, pal("kil", 0), 0.5)
            elif r2 < 0.36:
                c = mix(c, pal("siva", 1), 0.45)
            c = tone(c, k) if "tone" in globals() else c
            lift = 0.09 + rnd.uniform(-0.01, 0.015)
            pts = [(MEYDAN.x + u0 + gap, MEYDAN.y + v0 + gap), (MEYDAN.x + u0 + w - gap, MEYDAN.y + v0 + gap),
                   (MEYDAN.x + u0 + w - gap, MEYDAN.y + v0 + cell - gap), (MEYDAN.x + u0 + gap, MEYDAN.y + v0 + cell - gap)]
            # kenardaki levha bordür taşlarının (R−0.8…R−0.1) altına taşıyordu (çakışma taraması 13.11):
            # köşesi R−0.86'yı geçen levha merkeze doğru kırpılır
            kir = []
            for (x, y) in pts:
                dx, dy = x - MEYDAN.x, y - MEYDAN.y
                rr = math.hypot(dx, dy)
                if rr > R - 0.86:
                    s_ = (R - 0.86) / rr
                    x, y = MEYDAN.x + dx * s_, MEYDAN.y + dy * s_
                kir.append((x, y))
            pts = kir
            vs = [bm.verts.new((x, y, zat(x, y, lift))) for (x, y) in pts]
            f = bm.faces.new(vs)
            for l in f.loops:
                l[lay] = c
    # 3) bordür taşları (halka boyunca uzun taşlar)
    m = 34
    for i in range(m):
        a0 = 2 * math.pi * i / m + 0.01
        a1 = 2 * math.pi * (i + 1) / m - 0.01
        r0, r1 = R - 0.8, R - 0.1
        pts = [(math.cos(a0) * r0, math.sin(a0) * r0), (math.cos(a0) * r1, math.sin(a0) * r1),
               (math.cos(a1) * r1, math.sin(a1) * r1), (math.cos(a1) * r0, math.sin(a1) * r0)]
        vs = [bm.verts.new((MEYDAN.x + x, MEYDAN.y + y, zat(MEYDAN.x + x, MEYDAN.y + y, 0.11))) for (x, y) in pts]
        f = bm.faces.new(vs)
        c = mix(pal("kaya", 2), pal("kaya", 1), 0.35 + 0.4 * rnd.random())
        for l in f.loops:
            l[lay] = c
    bm.normal_update()
    for f in bm.faces:
        if f.normal.z < 0:
            f.normal_flip()
    ob = to_object(bm, "SM_Meydan_Doseme", col, sharp_angle=0)
    for p in ob.data.polygons:
        p.use_smooth = False
    return ob

# ------------------------------------------------------------------ SU
def _gol_seviyeleri():
    """Gölet su yüzü = arazi MESH'inin kıyı halkasındaki en alçak nokta − 8 cm (dere ağızları hariç).
    height() fonksiyonu 2.5 m ızgaranın yakalayamadığı derin çanak verir; su toprağın altında kalıyordu."""
    from mathutils.bvhtree import BVHTree
    ar = bpy.data.objects.get("SM_Arazi")
    if ar is None:
        return [gol_su_z(gc) for gc, gr in GOLLER]
    mw = ar.matrix_world
    bvh = BVHTree.FromPolygons([mw @ v.co for v in ar.data.vertices], [tuple(p.vertices) for p in ar.data.polygons])
    out = []
    for gc, gr in GOLLER:
        zs = []
        for k in range(36):
            a = 2 * math.pi * k / 36
            q = gc + Vector((math.cos(a), math.sin(a))) * (gr - 0.4)
            if poly_dist(q, STREAM) < 2.6:
                continue
            hit = bvh.ray_cast(Vector((q.x, q.y, 300.0)), Vector((0, 0, -1)), 600.0)
            if hit[0] is not None:
                zs.append(hit[0].z)
        out.append(min(zs) - 0.08 if zs else gol_su_z(gc))
    return out

def build_water(col):
    gol_z = _gol_seviyeleri()
    pts = resample(STREAM, 1.5)
    zcs = [height(p.x, p.y) + WATER_Z for p in pts]
    secs = []
    for i, p in enumerate(pts):
        a = pts[max(i - 1, 0)]
        b = pts[min(i + 1, len(pts) - 1)]
        t = (b - a).normalized()
        nrm = Vector((-t.y, t.x))
        zc = zcs[i]
        row = []
        for o in (-2.9, -1.0, 1.0, 2.9):
            q = p + nrm * o
            # yalnız dış kenar: kıyı (arazi ağı) su yüzünden alçaksa kenar kıyının 4 cm ALTINA iner → su yüzü araziyi
            # temiz bir çizgide keser; eskiden kıyının 3 cm üstünde kalıp arazi ağıyla çakışıyordu (13.11)
            z = zc
            if abs(o) > 2.0:
                kiyi = arazi_z(q.x, q.y)
                if kiyi < zc + 0.02:
                    z = min(zc, kiyi - 0.04)
            for (gc, gr), gz in zip(GOLLER, gol_z):   # göletin iç kısmında şerit gölet yüzünün 3 cm altında (titreşmesin)
                if (q - gc).length < gr - 0.6:
                    z = min(z, gz - 0.03)
            row.append(Vector((q.x, q.y, z)))
        secs.append(row)
    bm = bmesh.new()
    lay = bm.loops.layers.float_color.new("Col")
    vs = [[bm.verts.new(c) for c in row] for row in secs]
    for i in range(len(vs) - 1):
        # dik bölüm (çağlayan): köpük rengi
        drop = (zcs[max(i - 1, 0)] - zcs[min(i + 2, len(zcs) - 1)]) / 4.5
        foam = smooth01((drop - 0.08) / 0.25)
        for j in range(3):
            f = bm.faces.new((vs[i][j], vs[i][j + 1], vs[i + 1][j + 1], vs[i + 1][j]))
            for l in f.loops:
                mid = l.vert in (vs[i][1], vs[i][2], vs[i + 1][1], vs[i + 1][2])
                c = pal("su", 1) if mid else pal("su", 0)
                l[lay] = mix(c, pal("su", 0), foam)
    # göletler: halkalı disk, kenar kıyının altında kalır
    for (gc, gr), zw in zip(GOLLER, gol_z):
        segs = 32
        rings = [0.0, 0.45, 0.75, 1.0]
        ring_vs = []
        for r in rings:
            n = 1 if r == 0 else segs
            ring = []
            for k in range(n):
                a = 2 * math.pi * k / segs
                x, y = gc.x + math.cos(a) * (gr - 0.3) * r, gc.y + math.sin(a) * (gr - 0.3) * r
                z = zw
                # dere ağzında dış halka şerit seviyesinin 1 cm altına iner: halka kanalın üstünde levha gibi
                # kenar veriyordu (Play 13.11), şerit üstte kalır ve kesintisiz akar
                if r >= 0.75 and poly_dist(Vector((x, y)), STREAM) < 3.0:
                    z = min(zw, height(x, y) + WATER_Z - 0.01)
                ring.append(bm.verts.new((x, y, z)))
            ring_vs.append(ring)
        for k in range(len(rings) - 1):
            A, B = ring_vs[k], ring_vs[k + 1]
            for i in range(segs):
                j = (i + 1) % segs
                f = bm.faces.new((A[0], B[i], B[j])) if len(A) == 1 else bm.faces.new((A[i], B[i], B[j], A[j]))
                for l in f.loops:
                    l[lay] = pal("su", 1)                     # dere şeridinin ortasıyla aynı renk (ağızda renk kırığı yok)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    for f in bm.faces:
        if f.normal.z < 0:
            f.normal_flip()
    m = material_named("M_Su", "#FFFFFF")
    return to_object(bm, "Su_Dere", col, sharp_angle=0, mat=m)

# ------------------------------------------------------------------ KÖKLER (08 §2)
def build_roots(col):
    out = {}
    # Unity'de Mehmet'in Neighborhood_* prefab'ı bu noktaya konur; buradaki <Kisa>_Once / _Sonra
    # grupları onun Before / After çocuklarının altına sürüklenir (Blender'da ad tekil olmalı).
    for kisa, c in (("Denk", DENK), ("Tepk", TEPK)):
        z = height(c.x, c.y)
        root = empty("Koy_" + kisa, col, loc=(c.x, c.y, z), kind="CUBE", size=1.0)
        for ch in ("Once", "Sonra"):
            parent_keep(empty(kisa + "_" + ch, col, loc=(c.x, c.y, z)), root)
        out[kisa] = root
    # oyuncu doğuş önerisi: atölye kapısının önü, meydana bakar (Mehmet'in PlayerSpawn prefab'ı buraya)
    empty("_PlayerSpawn_Oneri", col, loc=(-1.4, 6.0, 0.0), rot=(0, 0, 180), kind="SINGLE_ARROW", size=1.0)
    return out

# ------------------------------------------------------------------ SINIR DUVARI (görünmez, yalnız çarpışma)
SINIR_D = 0.8            # play_outside değeri: kaya basamağının daha dibinde
SINIR_ALT, SINIR_UST = -2.0, 8.0
GECIT_YAN = 4.4          # boğaz duvarı: yol ekseninden (düz taban w/2 + 2.2'nin hemen dışı)

def _duvar_seridi(bm, pts, kalin=0.5):
    """XY nokta dizisi boyunca içi dolu dikey duvar (zemin −2 … +8 m, kalınlık 0.5 m), kapalı hacim, normaller
    dışa. Tek ya da iki yüzlü ince şerit işe yaramadı: Unity çarpışma pişirmesi çakışan ters yüzleri temizliyor,
    karakter kontrolcüsü arka yüzden geçiyordu (Play testi 13.11)."""
    if len(pts) < 2:
        return
    ring = []
    for i, p in enumerate(pts):
        a, b = pts[max(i - 1, 0)], pts[min(i + 1, len(pts) - 1)]
        t = (b - a).normalized()
        n = Vector((-t.y, t.x)) * (kalin * 0.5)
        z = height(p.x, p.y)
        q0, q1 = p - n, p + n
        ring.append([bm.verts.new((q0.x, q0.y, z + SINIR_ALT)), bm.verts.new((q1.x, q1.y, z + SINIR_ALT)),
                     bm.verts.new((q1.x, q1.y, z + SINIR_UST)), bm.verts.new((q0.x, q0.y, z + SINIR_UST))])
    yuz = []
    for A, B in zip(ring, ring[1:]):
        for k in range(4):
            j = (k + 1) % 4
            yuz.append(bm.faces.new((A[k], A[j], B[j], B[k])))
    yuz.append(bm.faces.new(tuple(ring[0])))
    yuz.append(bm.faces.new(tuple(reversed(ring[-1]))))
    bmesh.ops.recalc_face_normals(bm, faces=yuz)

def build_sinir(col):
    """Oyuncu kaya basamağına zıplayıp tepeden arazinin kenarına yürüyor, boşluğa düşüyordu (Play testi 13.11,
    72 yönün 72'sinde kaçış). Oyun alanını çevreleyen görünmez duvar + Matematik/Türkçe boğazlarında kapıya kadar
    iki yan duvar. Unity'de COL_ önekli obje: MeshCollider, renderer yok (HaritaYerlestir.cs)."""
    bm = bmesh.new()
    gecit = [(n, pts, w) for (n, pts, w) in ROADS if n in GECIT_YOLLARI]
    r = 1.0 + SINIR_D / (PLAY_A[1] * 1.25)
    halka, acik = [], []
    N = 900
    for i in range(N + 1):
        t = 2 * math.pi * i / N
        c, s = math.cos(t), math.sin(t)
        p = Vector((PLAY_C.x + PLAY_A[0] * r * math.copysign(abs(c) ** 0.5, c),
                    PLAY_C.y + PLAY_A[1] * r * math.copysign(abs(s) ** 0.5, s)))
        kapali = all(poly_dist(p, pts) > GECIT_YAN for (_, pts, _) in gecit)
        if kapali:
            halka.append(p)
        else:
            if len(halka) > 1:
                acik.append(halka)
            halka = []
    if len(halka) > 1:
        acik.append(halka)
    for parca in acik:
        _duvar_seridi(bm, parca)
    # boğaz yan duvarları: halkanın içinden başlar, şehir kapısının 1.5 m önünde biter
    for n, pts, w in gecit:
        gx, gy = GECIT_KAPI[n]
        kapi = Vector((gx, gy))
        rs = resample(pts, 0.8)
        for side in (-1, 1):
            seri = []
            for i, p in enumerate(rs):
                if play_outside(p.x, p.y) < -0.5:
                    continue
                if (p - kapi).length < 1.5:
                    break
                a, b = rs[max(i - 1, 0)], rs[min(i + 1, len(rs) - 1)]
                d = (b - a).normalized()
                seri.append(p + Vector((-d.y, d.x)) * side * GECIT_YAN)
            _duvar_seridi(bm, seri)
    ob = to_object(bm, "COL_Sinir_Duvari", col, sharp_angle=0)
    ob.display_type = "WIRE"
    ob.hide_render = True
    return ob

def atolye_marker(col):
    bm = bm_box(ATOLYE_HALF[0] * 2 - 4.0, ATOLYE_HALF[1] * 2 - 4.0, 3.4)
    paint_flat(bm, pal("cati", 1))
    ob = to_object(bm, "_Atolye_Isaret", col, loc=(ATOLYE.x, ATOLYE.y, 0.0), smooth=False)
    ob.display_type = "WIRE"
    return ob

# ------------------------------------------------------------------ KURULUM
def build_harita():
    col = collection(COL)
    clear_collection(COL)
    prev = collection("_Onizleme_Harita")
    clear_collection("_Onizleme_Harita")
    obs = [build_terrain(col), build_roads(col), build_meydan(col), build_water(col), build_sinir(col)]
    build_roots(col)
    atolye_marker(prev)
    print("Harita üçgen:", {o.name: tri_count([o]) for o in obs})

# ------------------------------------------------------------------ önizleme (dışa aktarılmaz)
def preview_scene():
    col = collection("_Onizleme_Isik")
    clear_collection("_Onizleme_Isik")
    sun = bpy.data.lights.get("Onizleme_Gunes") or bpy.data.lights.new("Onizleme_Gunes", "SUN")
    sun.energy = 3.5
    sun.color = (1.0, 0.96, 0.88)
    so = bpy.data.objects.new("Onizleme_Gunes", sun)
    so.rotation_euler = (math.radians(50), 0, math.radians(35))
    col.objects.link(so)
    w = bpy.context.scene.world or bpy.data.worlds.new("World")
    bpy.context.scene.world = w
    w.use_nodes = True
    bg = next(n for n in w.node_tree.nodes if n.type == "BACKGROUND")
    bg.inputs[0].default_value = pal("gok", 0)
    bg.inputs[1].default_value = 0.9
    m = material_named("M_Su", "#FFFFFF")
    nt = m.node_tree
    if not any(n.type == "VERTEX_COLOR" for n in nt.nodes):
        b = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
        vc = nt.nodes.new("ShaderNodeVertexColor")
        vc.layer_name = "Col"
        nt.links.new(vc.outputs[0], b.inputs["Base Color"])
    def cam(name, loc, rot, lens):
        cd = bpy.data.cameras.get(name) or bpy.data.cameras.new(name)
        cd.lens = lens
        cd.clip_end = 1200
        ob = bpy.data.objects.new(name, cd)
        ob.location = loc
        ob.rotation_euler = [math.radians(a) for a in rot]
        col.objects.link(ob)
        return ob
    c1 = cam("Kam_Kus", (-20, -75, 55), (55, 0, -12), 24)
    # göz hizası (bisiklette ~1.45 m): atölye kapısından meydana bakış
    cam("Kam_Goz_Atolye", (-1.4, 6.0, 1.5), (88, 0, 180), 20)
    # meydandan doğuya (köprü ve Denkleştirme)
    cam("Kam_Goz_Meydan_Dogu", (6.0, -8.0, 1.5), (88, 0, -90), 20)
    # meydandan batıya (güvercin kulesi)
    cam("Kam_Goz_Meydan_Bati", (-6.0, -7.0, 1.5), (88, 0, 90), 20)
    bpy.context.scene.camera = c1
    bpy.context.scene.render.resolution_x = 1600
    bpy.context.scene.render.resolution_y = 900
