# Câbir Usta'nın Atölyesi — iç mekân düzeni (ikinci geçiş)
# Amaç: steril boş oda yerine yaşanmış, sıcak, katmanlı bir zanaatkâr atölyesi.
# Her nesnenin gerekçesi: ArtSource/Referans/atolye.md §F (Râzî/İbn Sînâ/Libavius/Teniers) ve 09 §5.
# Önce: ba_lib.py, atolye_kabuk.py, atolye_esya.py (aynı globals).

import math
import random
from mathutils import Matrix, Vector

RI = random.Random(4242)
IC_COL = "Atolye_Ic"
FLAMES = []          # (bm, matris) — tüm alevler tek objede (M_Fener)

def _c():
    return collection(IC_COL)

def _o(bm, name, loc=(0, 0, 0), rot=(0, 0, 0), mat=None, sharp=40):
    return to_object(bm, name, _c(), loc=loc, rot=rot, sharp_angle=sharp, mat=mat)

def TR(x, y, z, rz=0.0):
    return Matrix.Translation((x, y, z)) @ Matrix.Rotation(rz, 4, "Z")

BOOK_COLS = (lambda: [pal("ahsap", 2), pal("dag", 2), pal("cati", 2), pal("yaprak", 2), mix(pal("ahsap", 1), pal("cati", 2), 0.5),
                      mix(pal("dag", 2), pal("ahsap", 2), 0.5), pal("bakir", 2)])()

# ================================================================== küçük nesne yapıcıları
def book_bm(t, d, h, col, seed=0):
    """Dik duran kitap: sırt -Y'de. Kapak tahtaları + sırt + iç sayfa bloğu (açık), köşeler pahlı."""
    parts = []
    pg = bm_box(t - 0.008, d - 0.008, h - 0.012, bevel=0.002)
    paint_flat(pg, mix(pal("siva", 0), pal("kil", 0), 0.35))
    parts.append((pg, Matrix.Translation((0, 0.004, 0.006))))
    for sx in (-1, 1):
        cv = bm_box(0.005, d, h, bevel=0.0015)
        paint_flat(cv, col)
        parts.append((cv, Matrix.Translation((sx * (t / 2 - 0.0025), 0, 0))))
    sp = bm_box(t, 0.008, h, bevel=0.002)
    paint_flat(sp, mix(col, pal("siva", 2), 0.15))
    parts.append((sp, Matrix.Translation((0, -d / 2 + 0.004, 0))))
    for zz in (h * 0.2, h * 0.78):   # sırtta kabartma bantlar
        bd = bm_box(t + 0.004, 0.012, 0.012, bevel=0.002)
        paint_flat(bd, mix(col, pal("altin", 1), 0.45))
        parts.append((bd, Matrix.Translation((0, -d / 2 + 0.002, zz))))
    return bm_merge(*parts)

def book_row(length, seed, hmin=0.18, hmax=0.27, lean_end=True):
    r = random.Random(seed)
    parts = []
    x = -length / 2
    while x < length / 2 - 0.03:
        t = r.uniform(0.025, 0.055)
        h = r.uniform(hmin, hmax)
        d = r.uniform(0.15, 0.2)
        b = book_bm(t, d, h, r.choice(BOOK_COLS), seed=r.randrange(9999))
        parts.append((b, Matrix.Translation((x + t / 2, r.uniform(-0.01, 0.01), 0)) @ Matrix.Rotation(r.uniform(-0.02, 0.02), 4, "Y")))
        x += t + 0.002
    if lean_end:
        t, h = 0.04, hmax * 0.95
        b = book_bm(t, 0.18, h, r.choice(BOOK_COLS), seed=r.randrange(9999))
        parts.append((b, Matrix.Translation((x + 0.06, 0, 0)) @ Matrix.Rotation(-0.42, 4, "Y")))
    return bm_merge(*parts)

def book_stack(n, seed):
    r = random.Random(seed)
    parts = []
    z = 0.0
    for i in range(n):
        t = r.uniform(0.03, 0.05)
        w, d = r.uniform(0.18, 0.26), r.uniform(0.14, 0.2)
        b = book_bm(t, d, w, r.choice(BOOK_COLS), seed=r.randrange(9999))
        transform_bm(b, Matrix.Rotation(math.pi / 2, 4, "Y"))
        parts.append((b, Matrix.Translation((r.uniform(-0.02, 0.02), r.uniform(-0.02, 0.02), z + t / 2)) @ Matrix.Rotation(r.uniform(-0.3, 0.3), 4, "Z")))
        z += t
    return bm_merge(*parts)

def open_book_bm(w, d, seed):
    """Açık kitap: iki kavisli sayfa yarısı + kapak."""
    parts = []
    cv = bm_box(w + 0.02, d + 0.02, 0.008, bevel=0.002)
    paint_flat(cv, pal("ahsap", 2))
    parts.append((cv, None))
    for side in (-1, 1):
        secs = []
        for k in range(6):
            t = k / 5
            x = side * (0.005 + t * w / 2)
            z = 0.008 + 0.022 * math.sin(math.pi * (0.15 + 0.7 * t)) * (1 - 0.3 * t)
            secs.append([Vector((x, -d / 2, z)), Vector((x, d / 2, z)), Vector((x, d / 2, z + 0.006)), Vector((x, -d / 2, z + 0.006))])
        pg = bm_sweep(secs)
        paint_by(pg, lambda f, l: mix(pal("siva", 0), pal("kil", 0), 0.25))
        parts.append((pg, None))
    # sayfada mürekkep satırları (kütle olarak ince koyu şeritler)
    for side in (-1, 1):
        for k in range(5):
            ln = bm_box(w * 0.32, 0.006, 0.001)
            paint_flat(ln, mix(pal("ahsap", 2), demir(0.7), 0.5))
            parts.append((ln, Matrix.Translation((side * w * 0.25, -d * 0.3 + k * d * 0.14, 0.034))))
    return bm_merge(*parts)

def candle(x, y, z, h=0.14, seed=0, holder=True):
    r = random.Random(seed)
    parts = []
    if holder:
        hd = bm_lathe([(0, 0), (0.06, 0), (0.065, 0.012), (0.03, 0.018), (0.022, 0.05), (0.03, 0.055), (0.0, 0.055)], sides=8)
        paint_part(hd, "bakir", lo=1, hi=0, seed=seed)
        parts.append((hd, None))
    z0 = 0.055 if holder else 0.0
    wx = bm_lathe([(0, 0), (0.019, 0), (0.019, h * 0.85), (0.016, h), (0.0, h - 0.01)], sides=7)
    jitter(wx, 0.002, seed=seed)
    paint_by(wx, lambda f, l: mix(pal("siva", 1), pal("siva", 0), min(1, l.vert.co.z / h)))
    parts.append((wx, Matrix.Translation((0, 0, z0))))
    for k in range(2):   # erimiş damlalar
        dr = bm_box(0.008, 0.008, r.uniform(0.02, 0.05), bevel=0.003)
        paint_flat(dr, pal("siva", 0))
        a = r.uniform(0, 6.28)
        parts.append((dr, Matrix.Translation((math.cos(a) * 0.019, math.sin(a) * 0.019, z0 + h - 0.05))))
    fl = bm_lathe([(0, 0), (0.012, 0.012), (0.009, 0.03), (0, 0.05)], sides=5)
    paint_by(fl, lambda f, l: mix(pal("altin", 0), pal("cati", 0), min(1, l.vert.co.z / 0.05)))
    FLAMES.append((fl, Matrix.Translation((x, y, z + z0 + h))))
    return bm_merge(*parts)

def herb_bundle(seed, length=0.42):
    """Baş aşağı asılı kurutulmuş ot demeti (İbn Sînâ: bitki analizi, ilaç). Pivot: asılma noktası."""
    r = random.Random(seed)
    parts = []
    st = bm_cyl(0.004, 0.004, 0.22, sides=4)                    # ip
    paint_flat(st, pal("kil", 0))
    parts.append((st, Matrix.Translation((0, 0, -0.22))))
    tie = bm_lathe([(0.03, -0.26), (0.034, -0.24), (0.03, -0.22)], sides=6, close_top=False, close_bottom=False)
    paint_flat(tie, pal("kil", 1))
    parts.append((tie, None))
    for k in range(9):   # saplar
        a = r.uniform(0, 6.28)
        rr = r.uniform(0.0, 0.03)
        L = r.uniform(0.14, 0.2)
        s = bm_cyl(0.004, 0.003, L, sides=3)
        paint_flat(s, mix(pal("yaprak", 2), pal("kil", 1), 0.4))
        parts.append((s, Matrix.Translation((math.cos(a) * rr, math.sin(a) * rr, -0.24)) @ Matrix.Rotation(math.pi + r.uniform(-0.25, 0.25), 4, "X")))
    tones = (pal("yaprak", 1), pal("yaprak", 2), mix(pal("yaprak", 1), pal("altin", 2), 0.4), mix(pal("yaprak", 0), pal("kil", 0), 0.4))
    hue = r.choice(tones)
    for k in range(8):   # yaprak kümeleri: aşağı doğru genişler
        t = k / 7
        rad = 0.025 + 0.05 * t
        a = r.uniform(0, 6.28)
        cl = bmesh.new()
        bmesh.ops.create_icosphere(cl, subdivisions=1, radius=r.uniform(0.035, 0.055))
        jitter(cl, 0.01, seed=seed + k)
        c = mix(hue, pal("yaprak", 2), r.uniform(0, 0.4))
        paint_flat(cl, c)
        parts.append((cl, Matrix.Translation((math.cos(a) * rad, math.sin(a) * rad, -0.27 - t * (length - 0.27)))))
    return bm_merge(*parts)

def lantern(seed):
    """Asılı fener: demir çerçeve, opak pastel cam, sivri başlık, halka. Pivot: halkanın üstü (asılma)."""
    parts = []
    H, W = 0.28, 0.17
    z0 = -0.12 - H - 0.06
    base = bm_box(W + 0.04, W + 0.04, 0.03, bevel=0.008)
    iron(base)
    parts.append((base, Matrix.Translation((0, 0, z0))))
    for sx in (-1, 1):
        for sy in (-1, 1):
            p = bm_box(0.018, 0.018, H, bevel=0.003)
            iron(p)
            parts.append((p, Matrix.Translation((sx * W / 2, sy * W / 2, z0 + 0.03))))
    gl = bm_box(W - 0.01, W - 0.01, H - 0.02)
    paint_by(gl, lambda f, l: mix(pal("altin", 0), pal("siva", 0), 0.35 + 0.3 * (l.vert.co.z - z0) / H))
    parts.append((gl, Matrix.Translation((0, 0, z0 + 0.04))))
    roof = bm_cyl(W * 0.85, 0.02, 0.12, sides=4)
    transform_bm(roof, Matrix.Rotation(math.pi / 4, 4, "Z"))
    iron(roof)
    parts.append((roof, Matrix.Translation((0, 0, z0 + 0.03 + H))))
    ring = bm_lathe([(0.03, -0.006), (0.038, 0), (0.03, 0.006), (0.022, 0)], sides=6, close_top=False, close_bottom=False)
    iron(ring)
    parts.append((ring, Matrix.Translation((0, 0, -0.04)) @ Matrix.Rotation(math.pi / 2, 4, "X")))
    ch = bm_cyl(0.005, 0.005, 0.10, sides=4)
    iron(ch)
    parts.append((ch, Matrix.Translation((0, 0, -0.10))))
    fl = bm_lathe([(0, 0), (0.022, 0.02), (0.016, 0.05), (0, 0.085)], sides=6)
    paint_by(fl, lambda f, l: mix(pal("altin", 0), pal("cati", 0), min(1, l.vert.co.z / 0.085)))
    return bm_merge(*parts), (fl, z0 + 0.06)

def crate(w, d, h, seed):
    parts = []
    r = random.Random(seed)
    for side in (-1, 1):
        for axis in ("x", "y"):
            L = w if axis == "x" else d
            for k in range(3):
                sl = bm_box(L - 0.06, 0.022, h / 3 - 0.02, bevel=0.006)
                jitter(sl, 0.004, seed=seed + k + side * 5)
                _wood(sl, seed + k * 7 + (1 if axis == "x" else 2), lo=1, hi=0)
                m = Matrix.Translation((0, side * (d / 2 - 0.011), 0.01 + k * h / 3))
                if axis == "y":
                    m = Matrix.Rotation(math.pi / 2, 4, "Z") @ Matrix.Translation((0, side * (w / 2 - 0.011), 0.01 + k * h / 3))
                parts.append((sl, m))
    for sx in (-1, 1):
        for sy in (-1, 1):
            p = bm_box(0.05, 0.05, h, bevel=0.008)
            _wood(p, seed + 30, lo=2, hi=1)
            parts.append((p, Matrix.Translation((sx * (w / 2 - 0.025), sy * (d / 2 - 0.025), 0))))
    bt = bm_box(w - 0.04, d - 0.04, 0.02)
    _wood(bt, seed + 31, lo=2, hi=2)
    parts.append((bt, None))
    straw = bm_box(w - 0.08, d - 0.08, 0.05)
    jitter(straw, 0.02, seed=seed + 32)
    paint_flat(straw, mix(pal("altin", 2), pal("kil", 0), 0.5))
    parts.append((straw, Matrix.Translation((0, 0, h - 0.07))))
    return bm_merge(*parts)

def basket(rad, h, seed, content=None):
    r = random.Random(seed)
    parts = []
    n = int(h / 0.035)
    for k in range(n):
        z = k * 0.035
        rr = rad * (0.85 + 0.15 * (k / max(1, n - 1)))
        ring = bm_lathe([(rr - 0.012, z), (rr + 0.008 * (1 if k % 2 else -0.4), z + 0.0175), (rr - 0.012, z + 0.035)], sides=10,
                        close_top=False, close_bottom=False)
        paint_flat(ring, mix(pal("altin", 2), pal("kil", 1), 0.45 + (0.15 if k % 2 else -0.05)))
        parts.append((ring, None))
    bt = bm_cyl(rad * 0.85, rad * 0.85, 0.02, sides=10)
    paint_flat(bt, pal("kil", 1))
    parts.append((bt, None))
    rim = bm_lathe([(rad - 0.01, h), (rad + 0.012, h + 0.012), (rad - 0.01, h + 0.024), (rad - 0.025, h + 0.012)], sides=10,
                   close_top=False, close_bottom=False)
    paint_flat(rim, pal("kil", 1))
    parts.append((rim, None))
    if content == "tomar":
        for k in range(5):
            sc = scroll_bm(r.uniform(0.32, 0.42), r.uniform(0.022, 0.03), seed + k)
            transform_bm(sc, Matrix.Rotation(math.pi / 2, 4, "Y"))
            parts.append((sc, Matrix.Translation((r.uniform(-0.05, 0.05), r.uniform(-0.05, 0.05), h * 0.4))
                          @ Matrix.Rotation(r.uniform(-0.35, 0.35), 4, "X") @ Matrix.Rotation(1.3 + r.uniform(-0.2, 0.2), 4, "Y")))
    elif content == "ot":
        for k in range(6):
            cl = bmesh.new()
            bmesh.ops.create_icosphere(cl, subdivisions=1, radius=r.uniform(0.05, 0.08))
            jitter(cl, 0.015, seed=seed + k)
            paint_flat(cl, mix(pal("yaprak", 1), pal("yaprak", 2), r.random()))
            parts.append((cl, Matrix.Translation((r.uniform(-rad * 0.5, rad * 0.5), r.uniform(-rad * 0.5, rad * 0.5), h - 0.02))))
    elif content == "kor":       # kömür
        for k in range(8):
            cb = bm_box(r.uniform(0.04, 0.07), r.uniform(0.04, 0.06), r.uniform(0.03, 0.05), bevel=0.01, origin="center")
            jitter(cb, 0.008, seed=seed + k)
            paint_flat(cb, mix(demir(0.55), pal("ahsap", 2), r.uniform(0.1, 0.4)))
            parts.append((cb, Matrix.Translation((r.uniform(-rad * 0.6, rad * 0.6), r.uniform(-rad * 0.6, rad * 0.6), h - 0.02))))
    return bm_merge(*parts)

def scroll_bm(L, rad, seed):
    """Tomar: kâğıt silindiri + iki ahşap uç topuzu + bağ. Uzunluk Z ekseninde."""
    parts = []
    p = bm_cyl(rad, rad, L, sides=7)
    paint_flat(p, mix(pal("siva", 0), pal("kil", 0), 0.3))
    parts.append((p, None))
    for z in (-0.015, L):
        k = bm_cyl(rad * 0.55, rad * 0.55, 0.02, sides=6)
        _wood(k, seed, lo=1, hi=0)
        parts.append((k, Matrix.Translation((0, 0, z))))
    bd = bm_cyl(rad + 0.003, rad + 0.003, 0.012, sides=7)
    paint_flat(bd, pal("cati", 2))
    parts.append((bd, Matrix.Translation((0, 0, L / 2))))
    return bm_merge(*parts)

def log_bm(L, rad, seed):
    r = random.Random(seed)
    lg = bm_cyl(rad, rad * r.uniform(0.85, 1.0), L, sides=6)
    jitter(lg, rad * 0.12, seed=seed)
    paint_by(lg, lambda f, l: mix(pal("kil", 0), pal("altin", 2), 0.3) if abs(f.normal.z) > 0.8 else
             mix(pal("ahsap", 2), pal("kil", 2), r.uniform(0.2, 0.6)))
    return lg

# ================================================================== iç mimari
def build_wall_timbers():
    parts = []
    beam_ys = (-2.30, -0.75, 0.80, 2.35)
    for y in beam_ys:
        for s in (-1, 1):
            # konsol: kiriş ucunun altında, duvara yaslı
            cb = bm_prism([(0, 0), (0.55, 0), (0.55, -0.10), (0.30, -0.32), (0.0, -0.46)], -0.11, 0.11)
            bevel_all(cb, 0.012)
            _wood(cb, 7000 + int(y * 10) + s, lo=2, hi=1)
            # yerel +x odaya doğru: batı duvarında +X, doğuda -X
            m = Matrix.Translation((s * IX, y, H - 0.27)) @ Matrix.Rotation(0.0 if s < 0 else math.pi, 4, "Z")
            parts.append((cb, m))
            # payanda (diz): duvardan kiriş altına
            a = Vector((s * (IX - 0.06), y, 2.38))
            b = Vector((s * (IX - 0.80), y, H - 0.27))
            d = b - a
            br = bm_box(0.10, 0.10, d.length, bevel=0.015)
            _wood(br, 7100 + int(y * 10) + s, lo=2, hi=1)
            q = Vector((0, 0, 1)).rotation_difference(d.normalized())
            parts.append((br, Matrix.Translation(a) @ q.to_matrix().to_4x4()))
    # duvar kuşağı (sıvanın üst sınırı), ocak davlumbazı hizasında kesik
    def rail(a, b):
        a, b = Vector(a), Vector(b)
        d = b - a
        bm = bm_box(d.length, 0.10, 0.14, bevel=0.015)
        jitter(bm, 0.006, seed=int(a.x * 13 + a.y * 7))
        _wood(bm, 7200 + int(a.x * 10 + a.y * 10), lo=2, hi=1)
        ang = math.atan2(d.y, d.x)
        return (bm, Matrix.Translation((a + b) / 2) @ Matrix.Rotation(ang, 4, "Z"))
    z = 2.95
    parts.append(rail((-IX, -IY + 0.05, z), (-IX, IY - 0.05, z)))
    parts.append(rail((IX, -IY + 0.05, z), (IX, IY - 0.05, z)))
    parts.append(rail((-IX, -IY, z), (IX, -IY, z)))
    parts.append(rail((-IX, IY, z), (HEARTH_X - 0.95, IY, z)))
    parts.append(rail((HEARTH_X + 0.95, IY, z), (IX, IY, z)))
    return _o(bm_merge(*parts), "SM_Ic_Iskelet")

def build_wainscot():
    """Lambri: duvar dibinde 3 sıra yatay tahta + üst pervaz (0.92 m)."""
    parts = []
    segs = [  # (başlangıç, bitiş, sabit, eksen, iç yön)
        ((-IY, IY), -IX, "y", 1), ((-IY, IY), IX, "y", -1),
        ((-IX, HEARTH_X - 0.85), IY, "x", -1), ((HEARTH_X + 0.85, IX), IY, "x", -1),
        ((-IX, DOOR_CX - 0.75), -IY, "x", 1), ((DOOR_CX + 0.75, IX), -IY, "x", 1),
    ]
    rows = ((0.04, 0.30), (0.30, 0.58), (0.58, 0.86))
    sd = 7300
    for (u0, u1), fixed, axis, inward in segs:
        for ri, (z0, z1) in enumerate(rows):
            u = u0 + RI.uniform(0.0, 0.4) * (ri % 2)
            if ri % 2:
                u = u0
            while u < u1 - 0.05:
                L = min(RI.uniform(1.0, 1.8), u1 - u)
                if u1 - (u + L) < 0.35:
                    L = u1 - u
                sd += 1
                bm = bm_box(L - 0.008, 0.028, z1 - z0 - 0.008, bevel=0.007)
                jitter(bm, 0.003, seed=sd)
                paint_part(bm, "ahsap", lo=2 if ri == 0 else 1, hi=1 if ri == 0 else 0, noise=0.09, seed=sd, bias=0.3)
                c = u + L / 2
                off = inward * 0.016
                if axis == "x":
                    m = Matrix.Translation((c, fixed + off, z0))
                else:
                    m = Matrix.Translation((fixed + off, c, z0)) @ Matrix.Rotation(math.pi / 2, 4, "Z")
                parts.append((bm, m))
                u += L
        cap = bm_box(u1 - u0, 0.06, 0.06, bevel=0.015)
        _wood(cap, sd + 50, lo=1, hi=0)
        c = (u0 + u1) / 2
        off = inward * 0.03
        if axis == "x":
            m = Matrix.Translation((c, fixed + off, 0.86))
        else:
            m = Matrix.Translation((fixed + off, c, 0.86)) @ Matrix.Rotation(math.pi / 2, 4, "Z")
        parts.append((cap, m))
    return _o(bm_merge(*parts), "SM_Ic_Lambri")

LOFT_Y = -1.85          # asma kat ön kenarı
LOFT_Z = 3.42           # asma kat zemin üstü
def build_loft():
    parts = []
    # ön kiriş + iki dikme + diz payandaları
    fb = bm_box(2 * IX + 0.1, 0.18, 0.24, bevel=0.02)
    jitter(fb, 0.008, seed=7400)
    _wood(fb, 7401, lo=2, hi=1)
    parts.append((fb, Matrix.Translation((0, LOFT_Y, LOFT_Z - 0.04 - 0.24))))
    for x in (-2.6, 2.6):
        p = _leg(LOFT_Z - 0.28, 0.18, 7410 + int(x))
        parts.append((p, Matrix.Translation((x, LOFT_Y, 0))))
        base = _stone_block(0.30, 0.30, 0.10, 7415 + int(x), warm=0.5)
        parts.append((base, Matrix.Translation((x, LOFT_Y, 0))))
        for s in (-1, 1):
            a = Vector((x, LOFT_Y, LOFT_Z - 0.95))
            b = Vector((x + s * 0.65, LOFT_Y, LOFT_Z - 0.30))
            d = b - a
            br = bm_box(0.10, 0.10, d.length, bevel=0.015)
            _wood(br, 7420 + int(x) + s, lo=2, hi=1)
            q = Vector((0, 0, 1)).rotation_difference(d.normalized())
            parts.append((br, Matrix.Translation(a) @ q.to_matrix().to_4x4()))
    # kirişler (güney duvarından ön kirişe)
    x = -IX + 0.35
    while x < IX - 0.3:
        j = bm_box(0.13, (LOFT_Y + 0.09) - (-IY), 0.18, bevel=0.015)
        _wood(j, 7430 + int(x * 10), lo=2, hi=1)
        parts.append((j, Matrix.Translation((x, (-IY + LOFT_Y + 0.09) / 2, LOFT_Z - 0.04 - 0.18))))
        x += 0.75
    # döşeme tahtaları (X boyunca), uçları düzensiz
    y = -IY
    k = 0
    while y < LOFT_Y + 0.08:
        bw = min(0.21, LOFT_Y + 0.09 - y)
        L = 2 * IX - 0.02
        pl = bm_box(L, bw - 0.006, 0.04, bevel=0.006)
        jitter(pl, 0.004, seed=7450 + k)
        paint_part(pl, "ahsap", lo=1, hi=0, noise=0.1, seed=7450 + k, bias=0.4)
        parts.append((pl, Matrix.Translation((RI.uniform(-0.01, 0.01), y + bw / 2, LOFT_Z - 0.04))))
        y += bw
        k += 1
    # korkuluk: dikmeler + iki kuşak (çatı eğimi içinde)
    for xp in (-2.8, -1.4, 0.0, 1.4, 2.8):
        p = bm_box(0.08, 0.08, 0.88, bevel=0.012)
        _wood(p, 7470 + int(xp * 10), lo=1, hi=0)
        parts.append((p, Matrix.Translation((xp, LOFT_Y + 0.02, LOFT_Z))))
        cap = bm_lathe([(0, 0), (0.05, 0), (0.055, 0.03), (0.03, 0.06), (0, 0.07)], sides=6)
        _wood(cap, 7475, lo=1, hi=0)
        parts.append((cap, Matrix.Translation((xp, LOFT_Y + 0.02, LOFT_Z + 0.88))))
    for zz, th in ((0.84, 0.07), (0.42, 0.05)):
        rl = bm_box(5.8, 0.06, th, bevel=0.012)
        jitter(rl, 0.006, seed=int(zz * 100))
        _wood(rl, 7480 + int(zz * 10), lo=1, hi=0)
        parts.append((rl, Matrix.Translation((0, LOFT_Y + 0.02, LOFT_Z + zz - th / 2))))
    for i in range(24):   # parmaklıklar (iki dikme arasında, ritmik ama hafif düzensiz)
        xp = -2.75 + i * 0.24
        if any(abs(xp - q) < 0.08 for q in (-2.8, -1.4, 0.0, 1.4, 2.8)):
            continue
        bl = bm_box(0.035, 0.035, 0.40, bevel=0.006)
        _wood(bl, 7490 + i, lo=1, hi=0)
        parts.append((bl, Matrix.Translation((xp + RI.uniform(-0.01, 0.01), LOFT_Y + 0.02, LOFT_Z + 0.42))))
    out = bm_merge(*parts)
    _o(out, "SM_Asma_Kat")
    # merdiven (batı, asma kata yaslı)
    lad = []
    top = Vector((-3.35, LOFT_Y - 0.02, LOFT_Z + 0.55))
    bot = Vector((-3.35, -1.05, 0.0))
    d = top - bot
    q = Vector((0, 0, 1)).rotation_difference(d.normalized())
    R4 = q.to_matrix().to_4x4()
    for sx in (-0.21, 0.21):
        rl = bm_box(0.07, 0.05, d.length, bevel=0.012)
        jitter(rl, 0.005, seed=7500 + int(sx * 10))
        _wood(rl, 7501 + int(sx * 10), lo=2, hi=1)
        lad.append((rl, Matrix.Translation(bot + Vector((sx, 0, 0))) @ R4))
    n = int(d.length / 0.30)
    for k in range(1, n + 1):
        rg = bm_cyl(0.022, 0.022, 0.42, sides=6)
        transform_bm(rg, Matrix.Rotation(math.pi / 2, 4, "Y"))
        _wood(rg, 7510 + k, lo=1, hi=0)
        lad.append((rg, Matrix.Translation(bot + d * (k / (n + 0.6)) + Vector((-0.21, 0, 0)))))
    _o(bm_merge(*lad), "SM_Merdiven")
    # asma kattaki eşyalar (aşağıdan görünür)
    st = []
    for i, (x, y) in enumerate(((-2.4, -2.75), (-1.95, -2.5), (1.9, -2.7))):
        sack = bm_lathe([(0, 0), (0.18, 0.0), (0.22, 0.12), (0.2, 0.32), (0.13, 0.42), (0.07, 0.46), (0.09, 0.52), (0, 0.52)], sides=8)
        jitter(sack, 0.02, seed=7520 + i)
        paint_part(sack, "kil", lo=1, hi=0, seed=7521 + i, bias=0.2)
        st.append((sack, Matrix.Translation((x, y, LOFT_Z)) @ Matrix.Rotation(RI.uniform(0, 3), 4, "Z") @ Matrix.Rotation(RI.uniform(-0.1, 0.1), 4, "X")))
    st.append((crate(0.6, 0.45, 0.38, 7530), TR(-0.7, -2.7, LOFT_Z, 0.1)))
    st.append((crate(0.5, 0.4, 0.32, 7531), TR(-0.75, -2.65, LOFT_Z + 0.38, -0.25)))
    st.append((crate(0.55, 0.45, 0.36, 7532), TR(0.6, -2.75, LOFT_Z, -0.08)))
    st.append((basket(0.20, 0.30, 7533, content="tomar"), TR(1.25, -2.4, LOFT_Z)))
    rug = bm_cyl(0.11, 0.11, 1.4, sides=8)            # dürülmüş yedek kilim
    paint_by(rug, lambda f, l: pal("cati", 2) if abs(f.normal.z) < 0.5 else pal("altin", 1))
    st.append((rug, Matrix.Translation((2.3, -2.25, LOFT_Z + 0.11)) @ Matrix.Rotation(math.pi / 2, 4, "Y") @ Matrix.Rotation(0.1, 4, "X")))
    for k in range(3):
        j = jar_bm("kup", "tuz", seed=7540 + k)
        transform_bm(j, Matrix.Scale(0.7, 4))
        st.append((j, TR(-1.4 + k * 0.32, -3.0, LOFT_Z)))
    _o(bm_merge(*st), "SM_Asma_Kat_Esya")

# ================================================================== kilim (Anadolu düz dokuma)
def build_kilim(cx, cy, W, D, rot, name, seed):
    """Hücre ızgarası + köşe rengiyle motif: kenar bantları, eli belinde/göz motifli ortada üç baklava."""
    nx, ny = int(W / 0.08), int(D / 0.08)
    bm = bmesh.new()
    lay = bm.loops.layers.float_color.new("Col")
    red, red2 = pal("cati", 2), mix(pal("cati", 2), pal("bakir", 2), 0.5)
    gold, cream = pal("altin", 1), mix(pal("siva", 0), pal("kil", 0), 0.3)
    blue, dark = pal("dag", 2), mix(pal("ahsap", 2), demir(0.8), 0.4)
    def motif(i, j):
        u = (i + 0.5) / nx * 2 - 1
        v = (j + 0.5) / ny * 2 - 1
        bu, bv = abs(u) * W / 2, abs(v) * D / 2
        edge = min(W / 2 - bu, D / 2 - bv)
        if edge < 0.08:
            return dark
        if edge < 0.16:
            return gold if (i + j) % 4 < 2 else red2
        if edge < 0.24:
            return cream
        if edge < 0.30:
            return blue
        # alan: üç baklava (madalyon)
        for k, mx in enumerate((-0.33, 0.0, 0.33)):
            du = abs(u - mx) * W / 2
            dv = abs(v) * D / 2
            dd = du / 0.42 + dv / 0.52
            if dd < 0.32:
                return gold
            if dd < 0.55:
                return dark if k == 1 else blue
            if dd < 0.72:
                return cream
            if dd < 0.85:
                return red2
        # küçük serpme motifler
        if (i % 9 == 4 and j % 7 == 3):
            return cream
        return red
    verts = {}
    for j in range(ny + 1):
        for i in range(nx + 1):
            verts[i, j] = bm.verts.new((-W / 2 + W * i / nx, -D / 2 + D * j / ny, 0.012 + 0.002 * math.sin(i * 0.7 + j * 0.4)))
    for j in range(ny):
        for i in range(nx):
            f = bm.faces.new((verts[i, j], verts[i + 1, j], verts[i + 1, j + 1], verts[i, j + 1]))
            c = motif(i, j)
            for l in f.loops:
                l[lay] = c
    # kenar kalınlığı: alt yüzü kapatmaya gerek yok (zemine yatar); çevreye dar yan bant
    out = [(bm, None)]
    for side in (-1, 1):   # püsküller (kısa kenarlarda)
        for k in range(int(D / 0.06)):
            fr = bm_box(0.06, 0.012, 0.006)
            paint_flat(fr, cream)
            out.append((fr, Matrix.Translation((side * (W / 2 + 0.03), -D / 2 + 0.03 + k * 0.06, 0.004))))
    m = bm_merge(*out)
    return _o(m, name, loc=(cx, cy, 0.0), rot=(0, 0, rot), sharp=0)

# ================================================================== orta tezgâh + üstü
TBL = (-0.30, -0.45, 2.0, 0.92, 0.86)     # x, y, w, d, h
def build_center():
    x, y, w, d, h = TBL
    _o(table_bm(w, d, h, 7600, top_t=0.08), "SM_Orta_Tezgah", loc=(x, y, 0))
    # sıra (oturak)
    bench = []
    top = _plank(1.5, 0.30, 0.06, 7610, lo=1, hi=0)
    bench.append((top, Matrix.Translation((0, 0, 0.40))))
    for sx in (-1, 1):
        leg = bm_prism([(-0.13, 0), (0.13, 0), (0.09, 0.40), (-0.09, 0.40)], -0.03, 0.03)
        transform_bm(leg, Matrix.Rotation(math.pi / 2, 4, "Z"))
        bevel_all(leg, 0.01)
        _wood(leg, 7611 + sx, lo=2, hi=1)
        bench.append((leg, Matrix.Translation((sx * 0.6, 0, 0))))
    _o(bm_merge(*bench), "SM_Sira", loc=(x + 0.35, y - 0.80, 0), rot=(0, 0, 2))   # kapıdan giriş yolunu kesmez
    # 2. geçiş: tezgâhın üstü "simya tezgâhı" aletlerine ayrıldı (simya_tezgahi.py).
    # Eski eşyalar anlamlı yerlerine dağıtıldı:
    #   rahle + açık tarif defteri → Râzî rafının önünde, yerde, minderle (rahle yerde okunur)
    #   el terazisi, ağırlık kutusu, şişe sehpası, mum → doğu tezgâhı (tartım tezgâhı)
    #   tomarlar, notlar → çalışma masasının yanındaki sepette (build_corners)
    zt = h
    items = []
    # rahle (X ayaklı kitap sehpası) + açık kitap — Râzî/Câbir'in tarif defteri
    rh = []
    for s in (-1, 1):
        bd = bm_box(0.30, 0.02, 0.36, bevel=0.006)
        _wood(bd, 7620 + s, lo=1, hi=0)
        rh.append((bd, Matrix.Rotation(s * 0.62, 4, "X")))
    rah = bm_merge(*rh)
    ob = open_book_bm(0.36, 0.26, 7622)
    rah = bm_merge((rah, Matrix.Translation((0, 0, 0.0))), (ob, Matrix.Translation((0, -0.045, 0.27)) @ Matrix.Rotation(-0.62, 4, "X")))
    # minder: kilim renkli, kenarı biyeli yer minderi
    cush = bm_box(0.55, 0.45, 0.10, bevel=0.04, seg=2)
    jitter(cush, 0.008, seed=7625)
    paint_by(cush, lambda f, l: pal("cati", 2) if f.normal.z > 0.6 else mix(pal("altin", 1), pal("cati", 2), 0.4))
    rx, ry = -IX + 0.36 + 0.55, 2.30          # Râzî rafının önü
    _o(bm_merge((rah, TR(0, 0, 0, -math.pi / 2)), (cush, TR(0.55, 0, 0))), "SM_Rahle_Minder", loc=(rx, ry, 0))
    tartim = []   # doğu tezgâhı (x≈3.6, y -2.35…-0.75, üst 0.86)
    # el terazisi (küçük, ölçü kutusu ile)
    hb = []
    post = bm_cyl(0.012, 0.01, 0.30, sides=6)
    paint_part(post, "altin", lo=2, hi=1, seed=7630)
    hb.append((post, None))
    bs = bm_box(0.14, 0.10, 0.03, bevel=0.008)
    _wood(bs, 7631, lo=2, hi=1)
    hb.append((bs, None))
    arm = bm_box(0.34, 0.012, 0.012, bevel=0.003, origin="center")
    iron(arm)
    hb.append((arm, Matrix.Translation((0, 0, 0.30))))
    for s in (-1, 1):
        pn = bm_lathe([(0, 0), (0.05, 0.004), (0.055, 0.014), (0.0, 0.006)], sides=8, close_top=False)
        paint_part(pn, "altin", lo=1, hi=0, seed=7632)
        hb.append((pn, Matrix.Translation((s * 0.16, 0, 0.13))))
        for k in range(3):
            a = k * 2.09
            rd = bm_cyl(0.002, 0.002, 0.17, sides=3)
            iron(rd)
            hb.append((rd, Matrix.Translation((s * 0.16 + math.cos(a) * 0.03, math.sin(a) * 0.03, 0.135))))
    tartim.append((bm_merge(*hb), TR(3.62, -1.02, 0.0, math.pi / 2)))
    # ağırlık kutusu: içi oyuk, kademeli pirinç ağırlıklar
    wb = []
    box = bm_box(0.22, 0.14, 0.05, bevel=0.008)
    _wood(box, 7640, lo=2, hi=1)
    wb.append((box, None))
    lid = bm_box(0.22, 0.14, 0.015, bevel=0.005)
    _wood(lid, 7641, lo=1, hi=0)
    wb.append((lid, Matrix.Translation((0, 0.08, 0.05)) @ Matrix.Rotation(-1.2, 4, "X")))
    for k, rr in enumerate((0.026, 0.022, 0.018, 0.014, 0.011, 0.009)):
        wt = bm_lathe([(0, 0), (rr, 0), (rr, rr * 1.4), (rr * 0.5, rr * 1.6), (rr * 0.5, rr * 2.0), (0, rr * 2.1)], sides=7)
        paint_part(wt, "altin", lo=1, hi=0, seed=7642 + k)
        wb.append((wt, Matrix.Translation((-0.08 + (k % 3) * 0.075, -0.03 + (k // 3) * 0.06, 0.03))))
    tartim.append((bm_merge(*wb), TR(3.55, -1.45, 0.0, 1.6)))
    # şişe sehpası: 3 delikli tahta + cam şişeler (göztaşı, yeşil zaç, kezzap)
    sr = []
    rack = bm_box(0.42, 0.12, 0.10, bevel=0.01)
    _wood(rack, 7660, lo=1, hi=0)
    sr.append((rack, None))
    for k, cont in enumerate(("goztasi", "yesilzac", "kezzap")):
        j = jar_bm("sise", cont, fill=0.6, seed=7661 + k)
        sr.append((j, Matrix.Translation((-0.13 + k * 0.13, 0, 0.04))))
    tartim.append((bm_merge(*sr), TR(3.62, -1.95, 0.0, math.pi / 2)))
    tartim.append((candle(3.78, -2.22, zt, h=0.13, seed=7681), TR(3.78, -2.22, 0.0)))
    tartim.append((book_stack(3, 7682), TR(3.50, -2.20, 0.0, 0.4)))
    out = bm_merge(*tartim)
    _o(out, "SM_Tartim_Tezgahi_Ustu", loc=(0, 0, zt))

# ================================================================== asılı otlar + fenerler
def build_hanging():
    beam_bottom = H - 0.27
    parts = []
    spots = [(-3.1, 0.80), (-2.75, 0.80), (-2.4, 0.80), (-3.0, -0.75), (-2.6, -0.75), (2.75, -0.75), (3.1, -0.75),
             (3.3, 0.80), (-1.0, 2.35), (-1.35, 2.35)]
    for k, (x, y) in enumerate(spots):
        hb = herb_bundle(7700 + k, length=RI.uniform(0.38, 0.5))
        parts.append((hb, Matrix.Translation((x, y, beam_bottom)) @ Matrix.Rotation(RI.uniform(0, 6), 4, "Z")))
    _o(bm_merge(*parts), "SM_Asili_Otlar")
    for k, (x, y, drop) in enumerate(((-0.30, -0.75, 0.55), (2.95, 0.80, 0.45), (-2.2, 2.35, 0.5))):
        lb, (fl, fz) = lantern(7800 + k)
        chain = []
        n = int(drop / 0.06)
        for i in range(n):
            lk = bm_lathe([(0.012, -0.004), (0.016, 0), (0.012, 0.004), (0.008, 0)], sides=5, close_top=False, close_bottom=False)
            iron(lk)
            chain.append((lk, Matrix.Translation((0, 0, -i * 0.06 - 0.03)) @ Matrix.Rotation(math.pi / 2, 4, "X") @ Matrix.Rotation(i * math.pi / 2, 4, "Y")))
        lb = bm_merge((lb, Matrix.Translation((0, 0, -drop))), *chain)
        _o(lb, "Fener_%d" % (k + 1), loc=(x, y, beam_bottom))
        FLAMES.append((fl, Matrix.Translation((x, y, beam_bottom - drop + fz))))
        e = empty("Fener_%d_Isik" % (k + 1), _c(), loc=(x, y, beam_bottom - drop - 0.25), size=0.1)

# ================================================================== duvar rafları, ecza dolabı, askılık
def wall_shelf(L, seed, depth=0.24):
    parts = []
    bd = _plank(L, depth, 0.05, seed, lo=1, hi=0)
    parts.append((bd, Matrix.Translation((0, 0, 0))))
    for s in (-1, 1):
        br = bm_prism([(0, 0), (0.0, -0.26), (0.05, -0.26), (depth - 0.03, -0.02), (depth - 0.03, 0)], -0.025, 0.025)
        transform_bm(br, Matrix.Rotation(-math.pi / 2, 4, "Z"))
        bevel_all(br, 0.006)
        _wood(br, seed + 3 + s, lo=2, hi=1)
        parts.append((br, Matrix.Translation((s * (L / 2 - 0.12), depth / 2, 0))))
    return bm_merge(*parts)

def build_wall_storage():
    # 1) Ecza dolabı (güney duvarı, kapı ile pencere arası): çok gözlü çekmeceler (Râzî: simya → tıp)
    W, Hc, D = 1.30, 1.80, 0.42
    parts = []
    body = bm_box(W, D, Hc, bevel=0.02)
    _wood(body, 7900, lo=2, hi=1)
    parts.append((body, None))
    crown = bm_box(W + 0.12, D + 0.08, 0.08, bevel=0.02)
    _wood(crown, 7901, lo=1, hi=0)
    parts.append((crown, Matrix.Translation((0, -0.03, Hc))))
    plinth = bm_box(W + 0.06, D + 0.04, 0.10, bevel=0.015)
    _wood(plinth, 7902, lo=2, hi=2)
    parts.append((plinth, Matrix.Translation((0, -0.01, 0))))
    cols, rows = 5, 6
    for i in range(cols):
        for j in range(rows):
            dw = (W - 0.10) / cols
            dh = (Hc - 0.55) / rows
            fr = bm_box(dw - 0.02, 0.03, dh - 0.02, bevel=0.008)
            paint_part(fr, "ahsap", lo=1, hi=0, noise=0.1, seed=7910 + i * 11 + j, bias=0.2)
            cx = -W / 2 + 0.05 + dw * (i + 0.5)
            cz = 0.45 + dh * j
            parts.append((fr, Matrix.Translation((cx, -D / 2 - 0.012, cz))))
            kn = bm_cyl(0.012, 0.012, 0.02, sides=6)
            paint_part(kn, "altin", lo=1, hi=0, seed=7920)
            parts.append((kn, Matrix.Translation((cx, -D / 2 - 0.03, cz + dh / 2 - 0.01)) @ Matrix.Rotation(math.pi / 2, 4, "X")))
            lb = bm_box(0.06, 0.004, 0.03)
            paint_flat(lb, pal("siva", 0))
            parts.append((lb, Matrix.Translation((cx, -D / 2 - 0.028, cz + dh - 0.06))))
    for s in (-1, 1):   # alt kapaklar
        dr = bm_box(W / 2 - 0.08, 0.03, 0.30, bevel=0.01)
        _wood(dr, 7930 + s, lo=1, hi=0)
        parts.append((dr, Matrix.Translation((s * W / 4, -D / 2 - 0.012, 0.12))))
        rg = bm_lathe([(0.018, -0.003), (0.022, 0), (0.018, 0.003), (0.014, 0)], sides=6, close_top=False, close_bottom=False)
        iron(rg)
        parts.append((rg, Matrix.Translation((s * 0.06, -D / 2 - 0.035, 0.30)) @ Matrix.Rotation(math.pi / 2, 4, "X")))
    # üstünde: kitap yığını, küçük küp, sepet
    parts.append((book_stack(4, 7940), Matrix.Translation((-0.40, -0.02, Hc + 0.08))))
    parts.append((jar_bm("testi", "sirke", seed=7941), Matrix.Translation((0.05, 0.0, Hc + 0.08))))
    parts.append((basket(0.13, 0.16, 7942, content="ot"), Matrix.Translation((0.42, 0.0, Hc + 0.08))))
    out = bm_merge(*parts)
    transform_bm(out, Matrix.Rotation(math.pi, 4, "Z"))    # ön yüz +Y (odaya)
    _o(out, "SM_Ecza_Dolabi", loc=(0.30, -IY + D / 2 + 0.02, 0))
    # 2) Masanın yanında duvar rafı (batı duvarı): kitap sırası + kavanozlar
    ws = [(wall_shelf(1.0, 7950), None), (book_row(0.55, 7951), Matrix.Translation((-0.2, -0.02, 0.05))),
          (jar_bm("kavanoz", "kukurt", seed=7952), Matrix.Translation((0.30, 0, 0.05))),
          (jar_bm("sise", "civa", seed=7953), Matrix.Translation((0.42, 0.02, 0.05)))]
    sh = bm_merge(*ws)
    transform_bm(sh, Matrix.Rotation(math.pi / 2, 4, "Z"))
    _o(sh, "SM_Duvar_Rafi_Bati", loc=(-IX + 0.14, -0.55, 1.70))
    ws2 = [(wall_shelf(0.95, 7955), None), (book_row(0.5, 7956, hmin=0.16, hmax=0.24), Matrix.Translation((0.18, -0.02, 0.05))),
           (candle(0, 0, 0, h=0.12, seed=7957), Matrix.Translation((-0.32, 0, 0.05)))]
    FLAMES.pop()   # duvar rafındaki mum sönük (yalnızca masadakiler yanar)
    sh2 = bm_merge(*ws2)
    _o(sh2, "SM_Duvar_Rafi_Kuzey", loc=(-0.95, IY - 0.14, 2.30))       # dirsekler +Y (duvar), sırtlar -Y (oda)
    # malzeme rafının uçları: mekanik yuvaları (orta) boş kalır, uçlar dolu
    sx, sy, w = -0.95, IY - 0.34 / 2 - 0.02, 1.55
    zs = [0.18 + (2.0 - 0.30) * i / 3 + 0.045 for i in range(4)]
    fill = [(book_row(0.30, 7980, hmin=0.16, hmax=0.24, lean_end=False), Matrix.Translation((sx - w / 2 + 0.22, sy, zs[0])))]
    fill.append((book_stack(3, 7981), Matrix.Translation((sx + w / 2 - 0.20, sy, zs[0]))))
    fill.append((basket(0.10, 0.12, 7982, content="ot"), Matrix.Translation((sx - w / 2 + 0.14, sy, zs[1]))))
    fill.append((jar_bm("kavanoz", "boraks", seed=7983), Matrix.Translation((sx + w / 2 - 0.13, sy, zs[1]))))
    fill.append((book_stack(2, 7984), Matrix.Translation((sx - w / 2 + 0.16, sy, zs[2]))))
    fill.append((jar_bm("sise", "kralsuyu", seed=7985), Matrix.Translation((sx + w / 2 - 0.13, sy, zs[2]))))
    fill.append((crate(0.32, 0.24, 0.16, 7986), Matrix.Translation((sx + 0.25, sy, 2.07))))
    _o(bm_merge(*fill), "SM_Raf_Malzeme_Uclar")
    # 3) Askılık (doğu duvarı, pencere ile dolap arası değil → güney ucu): kancalar + asılı aletler
    pr = []
    rail = _plank(1.15, 0.06, 0.10, 7960, lo=1, hi=0)
    pr.append((rail, None))
    for k in range(5):
        pg = bm_cyl(0.015, 0.012, 0.10, sides=6)
        _wood(pg, 7961 + k, lo=1, hi=0)
        pr.append((pg, Matrix.Translation((-0.48 + k * 0.24, -0.03, 0.05)) @ Matrix.Rotation(math.pi / 2 + 0.25, 4, "X")))
    # elek (ahşap çemberli), bez kese, huni, ot demeti
    sv = bm_lathe([(0.16, -0.05), (0.17, -0.05), (0.17, 0.05), (0.16, 0.05)], sides=12, close_top=False, close_bottom=False)
    _wood(sv, 7966, lo=1, hi=0)
    mesh_disc = bm_cyl(0.16, 0.16, 0.004, sides=12)
    paint_flat(mesh_disc, mix(pal("kil", 0), pal("siva", 1), 0.5))
    pr.append((sv, Matrix.Translation((-0.48, -0.10, -0.12)) @ Matrix.Rotation(math.pi / 2, 4, "X")))
    pr.append((mesh_disc, Matrix.Translation((-0.48, -0.10, -0.12)) @ Matrix.Rotation(math.pi / 2, 4, "X")))
    bag = bm_lathe([(0, 0), (0.08, 0.02), (0.10, 0.10), (0.07, 0.2), (0.03, 0.24), (0.04, 0.27), (0, 0.27)], sides=8)
    jitter(bag, 0.01, seed=7967)
    paint_part(bag, "kil", lo=1, hi=0, seed=7968)
    pr.append((bag, Matrix.Translation((-0.24, -0.08, -0.30))))
    pr.append((herb_bundle(7969, 0.4), Matrix.Translation((0.0, -0.08, 0.04))))
    pr.append((herb_bundle(7970, 0.36), Matrix.Translation((0.24, -0.08, 0.04))))
    out = bm_merge(*pr)
    transform_bm(out, Matrix.Rotation(-math.pi / 2, 4, "Z"))
    _o(out, "SM_Askilik_Dogu", loc=(IX - 0.03, -2.75, 1.85))

# ================================================================== ocak çevresi
def build_hearth_dressing():
    parts = []
    # odun yığını (athanor önü): üç kat, uçları açık renkli
    k = 0
    for layer in range(3):
        n = 4 - layer
        for i in range(n):
            lg = log_bm(0.62 + RI.uniform(-0.06, 0.06), 0.06, 8000 + k)
            transform_bm(lg, Matrix.Rotation(math.pi / 2, 4, "Y"))
            parts.append((lg, Matrix.Translation((3.25 + RI.uniform(-0.03, 0.03), 1.62 + i * 0.13 + layer * 0.065, 0.06 + layer * 0.11))
                          @ Matrix.Rotation(RI.uniform(-0.08, 0.08), 4, "Z")))
            k += 1
    parts.append((basket(0.17, 0.26, 8020, content="kor"), Matrix.Translation((2.62, 1.05, 0))))
    _o(bm_merge(*parts), "SM_Odun_Yigini")
    # ocak kirişi üstü: küçük küp, mum, kroze, havan
    mt = []
    zt = 2.06
    mt.append((jar_bm("kavanoz", "goztasi", seed=8030), Matrix.Translation((HEARTH_X - 0.70, IY - 0.93, zt))))
    mt.append((jar_bm("sise", "zirnik", seed=8031), Matrix.Translation((HEARTH_X - 0.55, IY - 0.92, zt))))
    mt.append((candle(HEARTH_X + 0.62, IY - 0.92, zt, h=0.13, seed=8032), Matrix.Translation((HEARTH_X + 0.62, IY - 0.92, zt))))
    FLAMES.pop()
    mt.append((jar_bm("testi", "sirke", seed=8033), Matrix.Translation((HEARTH_X + 0.35, IY - 0.93, zt))))
    _o(bm_merge(*mt), "SM_Ocak_Kirisi_Ustu")

# ================================================================== depo köşesi + masa çevresi zenginleştirme
def build_corners():
    parts = []
    parts.append((crate(0.55, 0.42, 0.40, 8100), TR(1.55, -2.95, 0.0, 0.05)))
    parts.append((crate(0.45, 0.40, 0.34, 8101), TR(1.50, -2.90, 0.40, -0.2)))
    parts.append((basket(0.18, 0.30, 8102, content="tomar"), TR(2.95, -2.35, 0.0)))
    parts.append((basket(0.15, 0.22, 8103, content="ot"), TR(-3.70, -1.75, 0.0)))
    _o(bm_merge(*parts), "SM_Depo_Kosesi")
    # çalışma masası üstünü zenginleştir: kitap yığını, mum, tomar sepeti yanında
    x, y = -IX + 0.75 / 2 + 0.06, 0.60
    dm = []
    dm.append((book_stack(5, 8110), TR(x + 0.05, y + 0.38, 0.80)))
    dm.append((candle(x + 0.15, y - 0.28, 0.80, h=0.15, seed=8111), TR(x + 0.15, y - 0.28, 0.80)))
    dm.append((basket(0.16, 0.34, 8112, content="tomar"), TR(x + 0.55, y - 0.70, 0.0)))
    _o(bm_merge(*dm), "SM_Masa_Cevresi")

# ================================================================== alevler + önizleme ışıkları
def build_flames_and_preview_lights():
    if FLAMES:
        _o(bm_merge(*FLAMES), "SM_Alevler", mat=material_named("M_Fener", "#FFB13D", emission=4.0), sharp=0)
    # önizleme ışıkları (Unity'ye gitmez; Unity'de ışıklar sahnede kurulur)
    pc = collection("_Onizleme")
    for o in list(pc.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    def pl(name, loc, energy, color, radius=0.1):
        ld = bpy.data.lights.new(name, "POINT")
        ld.energy = energy
        ld.color = color
        ld.shadow_soft_size = radius
        ob = bpy.data.objects.new(name, ld)
        ob.location = loc
        pc.objects.link(ob)
    pl("Isik_Ocak", (HEARTH_X, HEARTH_Y - 0.5, 0.55), 450, (1.0, 0.55, 0.25), 0.3)
    for k, (x, y, d) in enumerate(((-0.30, -0.75, 0.55), (2.95, 0.80, 0.45), (-2.2, 2.35, 0.5))):
        pl("Isik_Fener_%d" % (k + 1), (x, y, H - 0.27 - d - 0.25), 160, (1.0, 0.72, 0.42), 0.15)
    pl("Isik_Mum", (TBL[0] - 0.35, TBL[1] - 0.25, TBL[4] + 0.3), 25, (1.0, 0.7, 0.4), 0.05)

def build_interior():
    clear_collection(IC_COL)
    FLAMES.clear()
    build_wall_timbers()
    build_wainscot()
    build_loft()
    build_kilim(TBL[0], TBL[1], 3.0, 2.0, 0, "SM_Kilim_Orta", 8200)
    build_kilim(-2.6, 1.0, 1.1, 0.8, 90, "SM_Kilim_Masa", 8201)
    build_center()
    build_hanging()
    build_wall_storage()
    build_hearth_dressing()
    build_corners()
    build_flames_and_preview_lights()
