# Câbir Usta'nın Atölyesi — kabuk (duvar, zemin, taşlar, kapı, çatı, cephe, baca, pencereler)
# Referans: ArtSource/Referans/atolye.md · Kurallar: BilgiAdasi_Docs/01_Tasarim/09-tema-ve-model-kurallari.md
# Koordinat (Blender): X doğu, Y kuzey, Z yukarı. Ön cephe (kapı) güneye (-Y) bakar.
# Taban (iç zemin üstü) z = 0. 1 birim = 1 m.
#
# Çalıştır: önce ba_lib.py, sonra bu dosya (aynı globals).

import math
import random
from mathutils import Matrix, Vector

R = random.Random(1307)

# ------------------------------------------------------------------ ölçüler
IX, IY = 4.0, 3.25          # iç yarı genişlik (X), iç yarı derinlik (Y) → 8 × 6.5 m
T = 0.45                    # duvar kalınlığı
OX, OY = IX + T, IY + T     # dış yarı ölçüler → 8.9 × 7.4 m
H = 3.4                     # duvar (saçak) yüksekliği
RISE = 4.0                  # çatı yüksekliği (saçak → mahya alt yüzü)
SLOPE = RISE / OX           # ≈ 0.90 (≈ 42°)
EAVE_X = OX + 0.55          # yan saçak taşması
JETTY = 0.30                # ön alınlık çıkması
ROOF_Y0 = -OY - JETTY - 0.55
ROOF_Y1 = OY + 0.50

# Kapı (ön duvar). Menteşe batı (sol, -X) tarafında; içeri açılır.
DOOR_CX, DOOR_W = -1.4, 1.20
DOOR_SPRING, DOOR_RISE = 2.25, 0.55
LEAF_W, LEAF_H, LEAF_T = 1.30, 2.30, 0.08
HINGE_X = DOOR_CX - LEAF_W / 2          # -2.05
HINGE_Y = -IY                            # iç yüz düzlemi
# Pencereler: (duvar, merkez u, genişlik, denizlik z, kemer başlangıcı, kemer yüksekliği)
WINDOWS = [
    ("S", 2.0, 1.00, 1.00, 1.95, 0.30),   # ön, kapının sağı
    ("W", 0.6, 1.10, 0.95, 1.95, 0.35),   # batı: çalışma masası + güvercin penceresi
    ("E", -1.2, 0.70, 1.55, 2.30, 0.25),  # doğu: yüksek küçük pencere
]

def roof_z(x):
    """Çatı alt yüzü (kaplama tahtası altı) z — mahyadan saçağa doğrusal."""
    return H + RISE - SLOPE * abs(x)

# ------------------------------------------------------------------ boyama
def stone_wall_painter(inner_dir_world, height=H):
    def fn(f, l):
        z = l.vert.co.z
        t = max(0.0, min(1.0, z / height))
        n = f.normal
        k = 1.0 + ((hash((round(f.calc_center_median().x, 1), round(f.calc_center_median().z, 1))) % 7) - 3) * 0.006
        if n.dot(inner_dir_world) > 0.6:
            c = mix(pal("siva", 1), pal("siva", 0), 0.35 + 0.65 * t)
        else:
            c = mix(pal("kaya", 2), pal("kaya", 0), 0.15 + 0.85 * t)
        return (min(c[0] * k, 1), min(c[1] * k, 1), min(c[2] * k, 1), 1.0)
    return fn

def tone(group, lo=2, hi=0, t0=0.0, t1=1.0, noise=0.06, seed=0):
    """Tek parça için alttan üste ton geçişi + parça başına küçük ton farkı."""
    r = random.Random(seed)
    k = 1.0 + r.uniform(-noise, noise)
    a, b = pal(group, lo), pal(group, hi)
    def fn(f, l, zlo=[None], zhi=[None]):
        return None
    return a, b, k

def paint_part(bm, group, lo=2, hi=0, noise=0.06, seed=0, bias=0.0):
    r = random.Random(seed)
    k = 1.0 + r.uniform(-noise, noise)
    zs = [v.co.z for v in bm.verts]
    z0, z1 = min(zs), max(zs)
    span = max(z1 - z0, 1e-6)
    a, b = pal(group, lo), pal(group, hi)
    def fn(f, l):
        t = (l.vert.co.z - z0) / span
        c = mix(a, b, max(0.0, min(1.0, t * (1 - bias) + bias)))
        return (min(c[0] * k, 1), min(c[1] * k, 1), min(c[2] * k, 1), 1.0)
    paint_by(bm, fn)
    return bm

def iron(bm):
    paint_by(bm, lambda f, l: mix(demir(0.5), demir(0.8), max(0.0, f.normal.z) * 0.5 + 0.25))
    return bm

# ------------------------------------------------------------------ duvarlar
def wall_matrix(side):
    """Yerel (u, y, z) → dünya. Yerel y=0 dış yüz, y=T iç yüz."""
    if side == "S":
        return Matrix.Translation((0, -OY, 0))
    if side == "N":   # u ekseni -X'e bakar ki dış yüz +Y'de kalsın
        return Matrix.Translation((0, OY, 0)) @ Matrix.Rotation(math.pi, 4, "Z")
    if side == "W":
        return Matrix.Translation((-OX, 0, 0)) @ Matrix.Rotation(-math.pi / 2, 4, "Z")
    if side == "E":
        return Matrix.Translation((OX, 0, 0)) @ Matrix.Rotation(math.pi / 2, 4, "Z")

def opening_poly(side, u, w, z0, spring, rise):
    # N ve E duvarlarında yerel u dünya ekseninin tersidir
    return pointed_arch(u, w, z0, spring, rise, n=5)

def build_walls(col):
    parts = []
    # güney (ön): tam genişlik, kapı + pencere
    # Kapı, zemine değdiği için delik değil dış çevreye açılmış oyuktur: delik alt kenarla aynı çizgide
    # buluşunca sıfır alanlı üçgenler oluşuyordu (Unity'de atılan yüz).
    door = pointed_arch(DOOR_CX, DOOR_W, 0.0, DOOR_SPRING, DOOR_RISE, n=6)
    outer = [(-OX, 0)] + door + [(OX, 0), (OX, H), (-OX, H)]
    holes = []
    for s, u, w, z0, sp, ri in WINDOWS:
        if s == "S":
            holes.append(list(reversed(pointed_arch(u, w, z0, sp, ri, n=5))))
    bm = bm_wall(outer, holes, T)
    parts.append((bm, wall_matrix("S")))
    # kuzey (arka): tam genişlik + alınlık üçgeni (taş)
    bm = bm_wall([(-OX, 0), (OX, 0), (OX, H), (0.0, roof_z(0.0)), (-OX, H)], [], T)
    parts.append((bm, wall_matrix("N")))
    # batı / doğu: iç derinlik kadar (köşeler ön/arka duvarlarda)
    for side in ("W", "E"):
        holes = []
        for s, u, w, z0, sp, ri in WINDOWS:
            if s == side:
                uu = -u if side == "W" else u   # W: yerel u = -dünya y ... (dönüş -90°: u→-y?)
                holes.append(list(reversed(pointed_arch(uu, w, z0, sp, ri, n=5))))
        bm = bm_wall([(-IY, 0), (IY, 0), (IY, H), (-IY, H)], holes, T)
        parts.append((bm, wall_matrix(side)))
    out = bm_merge(*parts)
    # büyük yüzleri böl: köşe rengiyle yumuşak ton lekeleri taşınabilsin
    bmesh.ops.subdivide_edges(out, edges=[e for e in out.edges if e.calc_length() > 0.9],
                              cuts=2, use_grid_fill=True)
    paint_by(out, _wall_color)
    return to_object(out, "SM_Atolye_Duvar", col, sharp_angle=30)

def _wall_color(f, l):
    """İç yüz (odaya bakan, iç sınırlar içinde) sıva; geri kalanı taş."""
    c = f.calc_center_median()
    n = f.normal
    # iç yüz: dört iç düzlemden birinin üstünde ve odaya bakıyor
    # Not: x/y sınırı konmaz — köşede iç düzlem komşu duvarın içine uzanır; oraya taşan üçgenin merkezi
    # sınır dışına düşüp taş rengi alıyor, görünen ucu köşede mavi üçgen olarak çıkıyordu.
    on_y = abs(abs(c.y) - IY) < 0.02 and n.y * (-c.y) > 0.5
    on_x = abs(abs(c.x) - IX) < 0.02 and n.x * (-c.x) > 0.5
    to_center = Vector((-c.x, -c.y, 0.0))
    inside = on_x or on_y
    co = l.vert.co
    z = co.z
    t = max(0.0, min(1.0, z / H))
    # düşük frekanslı leke: el yapımı taş/sıva hissi (köşe başına, dokusuz)
    from mathutils import noise as _nz
    k = 1.0 + 0.09 * _nz.noise(co * 0.55) + 0.03 * _nz.noise(co * 2.1)
    if inside:
        col = mix(pal("siva", 1), pal("siva", 0), 0.55 + 0.45 * min(t, 1.0) ** 0.55)
        col = mix(col, pal("kil", 0), 0.18)          # sıcak, kireç badanalı his
    else:
        col = mix(pal("kaya", 2), pal("kaya", 1), min(z / (H + RISE * 0.6), 1.0) ** 0.9)
        col = mix(col, pal("kaya", 0), 0.35 * min(z / (H + RISE), 1.0))
    return (min(col[0] * k, 1), min(col[1] * k, 1), min(col[2] * k, 1), 1.0)

# ------------------------------------------------------------------ zemin (taş döşeme)
def build_floor(col):
    parts = []
    y = -IY
    row = 0
    # derz (taşların arasındaki koyu harç)
    grout = bm_box(2 * IX, 2 * IY, 0.04)
    paint_flat(grout, mix(pal("kil", 2), pal("kaya", 2), 0.4))
    parts.append((grout, Matrix.Translation((0, 0, -0.07))))
    while y < IY - 0.05:
        h = R.uniform(0.55, 0.85)
        y1 = min(IY, y + h)
        if IY - y1 < 0.35:
            y1 = IY
        x = -IX + (0.0 if row % 2 == 0 else -R.uniform(0.2, 0.4))
        while x < IX - 0.05:
            w = R.uniform(0.50, 0.95)
            x1 = min(IX, x + w)
            if IX - x1 < 0.3:
                x1 = IX
            xa, xb = max(x, -IX), x1
            g = 0.018
            sx, sy = (xb - xa) - 2 * g, (y1 - y) - 2 * g
            bm = bm_box(sx, sy, 0.08, bevel=0.025, seg=1)
            jitter(bm, 0.008, seed=R.randrange(9999), axes=(1, 1, 0))
            # sıcak gri taş: kaya ile sıva karışımı, taş başına ton farkı
            rr = random.Random(R.randrange(9999))
            base = mix(pal("kil", 1), pal("kaya", 1), rr.uniform(0.10, 0.35))
            base = mix(base, pal("kil", 0), rr.uniform(0.0, 0.25))
            base = mix(base, pal("kil", 2), rr.uniform(0.05, 0.25))
            paint_by(bm, lambda f, l, b=base: b if f.normal.z > 0.5 else mix(b, pal("kaya", 2), 0.5))
            m = Matrix.Translation(((xa + xb) / 2, (y + y1) / 2, -0.08 + R.uniform(-0.004, 0.0)))
            m = m @ Matrix.Rotation(math.radians(R.uniform(-0.8, 0.8)), 4, "Z")
            parts.append((bm, m))
            x = x1
        y = y1
        row += 1
    # eşik taşı (kapı altı): üstü 0.04, dışarı taşar
    bm = bm_box(DOOR_W + 0.30, T + 0.25, 0.12, bevel=0.03)
    paint_part(bm, "kaya", lo=2, hi=0, seed=5, bias=0.3)
    parts.append((bm, Matrix.Translation((DOOR_CX, -OY + T / 2 - 0.10, -0.08))))
    out = bm_merge(*parts)
    return to_object(out, "SM_Atolye_Zemin", col, sharp_angle=40)

# ------------------------------------------------------------------ taş ayrıntıları
def quoin_blocks():
    parts = []
    for sx in (-1, 1):
        for sy in (-1, 1):
            cx, cy = sx * OX, sy * OY
            z = 0.22
            i = 0
            while z < H - 0.2:
                h = R.uniform(0.30, 0.38)
                if i % 2 == 0:
                    lx, ly = R.uniform(0.55, 0.68), R.uniform(0.30, 0.36)
                else:
                    lx, ly = R.uniform(0.30, 0.36), R.uniform(0.55, 0.68)
                bm = bm_box(lx, ly, h - 0.025, bevel=0.035, seg=1)
                jitter(bm, 0.012, seed=R.randrange(9999))
                paint_part(bm, "kaya", lo=1, hi=0, noise=0.07, seed=R.randrange(9999), bias=0.2)
                p = (cx + sx * (0.035 - lx / 2), cy + sy * (0.035 - ly / 2), z)
                parts.append((bm, Matrix.Translation(p) @ Matrix.Rotation(math.radians(R.uniform(-1.5, 1.5)), 4, "Z")))
                z += h
                i += 1
    return parts

def plinth_blocks():
    """Temel taşı: dış yüzden 0.1 taşar, kapı önünde boşluk."""
    parts = []
    def run(a, b, fixed, axis):
        u = a
        while u < b - 0.05:
            L = R.uniform(0.9, 1.6)
            u1 = min(b, u + L)
            if b - u1 < 0.5:
                u1 = b
            bm = bm_box(u1 - u - 0.02, 0.36, 0.50, bevel=0.05, seg=1)
            jitter(bm, 0.015, seed=R.randrange(9999))
            paint_part(bm, "kaya", lo=2, hi=1, noise=0.07, seed=R.randrange(9999))
            c = (u + u1) / 2
            if axis == "x":
                m = Matrix.Translation((c, fixed, -0.28))
            else:
                m = Matrix.Translation((fixed, c, -0.28)) @ Matrix.Rotation(math.pi / 2, 4, "Z")
            parts.append((bm, m))
            u = u1
    door_a = DOOR_CX - DOOR_W / 2 - 0.15
    door_b = DOOR_CX + DOOR_W / 2 + 0.15
    run(-OX - 0.1, door_a, -OY + 0.08, "x")
    run(door_b, OX + 0.1, -OY + 0.08, "x")
    run(-OX - 0.1, OX + 0.1, OY - 0.08, "x")
    run(-OY + 0.2, OY - 0.2, -OX + 0.08, "y")
    run(-OY + 0.2, OY - 0.2, OX - 0.08, "y")
    return parts

def arch_ring(cx, w, z0, spring, rise, n_blocks, depth_out, y_face, width=0.28, protrude=0.045, jamb=True):
    """Kemer taşları + söve taşları (ön yüze yapışık, protrude kadar dışarı)."""
    parts = []
    inner = pointed_arch(cx, w, z0, spring, rise, n=24)[1:-1]   # yalnız kemer + söve üst noktası
    # kemer kısmı: spring üstündeki noktalar
    arc = [p for p in inner if p[1] >= spring - 1e-6]
    # yay uzunluğu ile böl
    seg = [0.0]
    for a, b in zip(arc, arc[1:]):
        seg.append(seg[-1] + math.dist(a, b))
    total = seg[-1]
    def at(s):
        for i in range(1, len(seg)):
            if seg[i] >= s:
                t = (s - seg[i - 1]) / max(seg[i] - seg[i - 1], 1e-9)
                a, b = arc[i - 1], arc[i]
                return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t), (b[0] - a[0], b[1] - a[1])
        return arc[-1], (arc[-1][0] - arc[-2][0], arc[-1][1] - arc[-2][1])
    def outward(pt, tan):
        # Kemer merkezinden radyal yön: tepe noktasındaki teğet kırılması taşın dış kenarlarını
        # çaprazlamasın (kendini kesen çokgen → Unity'de atılan yüz). Teğet yalnız geri dönüş için.
        nx, nz = pt[0] - cx, pt[1] - (spring - (rise * 0.6))
        L = math.hypot(nx, nz)
        if L < 1e-6:
            nx, nz = -tan[1], tan[0]
            L = math.hypot(nx, nz) or 1
        return nx / L, nz / L
    for k in range(n_blocks):
        s0 = total * k / n_blocks + 0.006
        s1 = total * (k + 1) / n_blocks - 0.006
        pts_in = []
        steps = 3
        for j in range(steps + 1):
            p, tan = at(s0 + (s1 - s0) * j / steps)
            pts_in.append((p, outward(p, tan)))
        wk = width * (1.12 if k == n_blocks // 2 else 1.0) * R.uniform(0.95, 1.06)
        poly = [p for p, _ in pts_in] + [(p[0] + n[0] * wk, p[1] + n[1] * wk) for p, n in reversed(pts_in)]
        bm = bm_prism(poly, -protrude, 0.06)
        bevel_all(bm, 0.018)
        paint_part(bm, "kaya", lo=1, hi=0, noise=0.06, seed=R.randrange(9999), bias=0.3)
        parts.append((bm, Matrix.Translation((0, y_face, 0))))
    if jamb:
        for side in (-1, 1):
            z = z0 + (0.0 if z0 > 0.01 else 0.04)
            i = 0
            x_in = cx + side * w / 2
            while z < spring - 0.12:
                h = min(R.uniform(0.38, 0.52), spring - z)
                ww = (0.34 if i % 2 == 0 else 0.24) * R.uniform(0.95, 1.08)
                poly = [(x_in, z), (x_in + side * ww, z), (x_in + side * ww, z + h - 0.02), (x_in, z + h - 0.02)]
                if side < 0:
                    poly = list(reversed(poly))
                bm = bm_prism(poly, -protrude, 0.06)
                bevel_all(bm, 0.018)
                paint_part(bm, "kaya", lo=1, hi=0, noise=0.06, seed=R.randrange(9999), bias=0.3)
                parts.append((bm, Matrix.Translation((0, y_face, 0))))
                z += h
                i += 1
    return parts

def _wall_stone(w, h, seed):
    """Tek moloz taşı: yalnız ön kenarları pahlı, hafif yamuk."""
    bm = bm_box(w, 0.07, h)
    front = [e for e in bm.edges if all(v.co.y < -0.03 for v in e.verts)]
    bmesh.ops.bevel(bm, geom=front, offset=0.03, segments=1, affect="EDGES", clamp_overlap=True)
    jitter(bm, 0.018, seed=seed, axes=(1, 0.3, 1))
    r = random.Random(seed)
    g = r.random()
    c = mix(pal("kaya", 2), pal("kaya", 1), 0.25 + 0.6 * g)
    c = mix(c, pal("siva", 1), r.uniform(0.0, 0.18))     # bazı taşlar hafif sıcak
    paint_by(bm, lambda f, l, c=c: c if f.normal.y < -0.5 else mix(c, pal("kaya", 2), 0.4))
    return bm

# Duvar başına açıklık bölgeleri (yerel u, z0, u1, z1) — taş konmaz
def _openings_local(side):
    rects = []
    if side == "S":
        rects.append((DOOR_CX - DOOR_W / 2 - 0.42, -1, DOOR_CX + DOOR_W / 2 + 0.42, 9))
    if side == "N":
        rects.append((-CHIM_X - 0.75, -1, -CHIM_X + 0.75, 9))
    for s, u, w, z0, sp, ri in WINDOWS:
        if s == side:
            uu = -u if side == "W" else u
            rects.append((uu - w / 2 - 0.35, z0 - 0.25, uu + w / 2 + 0.35, sp + ri + 0.35))
    return rects

def _free(rects, u0, u1, z0, z1):
    for a, b, c, d in rects:
        if u1 > a and u0 < c and z1 > b and z0 < d:
            return False
    return True

def rubble_stones():
    parts = []
    walls = (("S", OX - 0.62), ("N", OX - 0.62), ("W", OY - 0.66), ("E", OY - 0.66))
    for side, half in walls:
        rects = _openings_local(side)
        M = wall_matrix(side)
        # alt şerit: 2 tam sıra + düzensiz 3. sıra
        z = 0.24
        for row in range(3):
            h = R.uniform(0.22, 0.30)
            u = -half + R.uniform(0, 0.25)
            while u < half - 0.2:
                w = R.uniform(0.34, 0.68)
                u1 = min(half, u + w)
                keep = row < 2 or R.random() < 0.45
                if keep and _free(rects, u, u1, z, z + h):
                    bm = _wall_stone(u1 - u - 0.025, h - 0.025, R.randrange(99999))
                    parts.append((bm, M @ Matrix.Translation(((u + u1) / 2, 0.015, z))))
                u = u1
            z += h
        # sıvası dökülmüş taş öbekleri
        for _ in range(3):
            for _try in range(12):
                cu = R.uniform(-half + 0.6, half - 0.6)
                cz = R.uniform(1.4, H - 0.6)
                if _free(rects, cu - 0.6, cu + 0.6, cz - 0.4, cz + 0.4):
                    break
            else:
                continue
            for k in range(R.randint(3, 6)):
                w, h = R.uniform(0.28, 0.5), R.uniform(0.18, 0.26)
                du, dz = R.uniform(-0.45, 0.45), R.choice((-0.25, 0.0, 0.25)) + R.uniform(-0.04, 0.04)
                bm = _wall_stone(w, h, R.randrange(99999))
                parts.append((bm, M @ Matrix.Translation((cu + du, 0.03, cz + dz))))
    return parts

def build_stones(col):
    parts = quoin_blocks() + plinth_blocks() + rubble_stones()
    # kapı kemeri (ön yüz y = -OY)
    parts += arch_ring(DOOR_CX, DOOR_W, 0.0, DOOR_SPRING, DOOR_RISE, 7, 0.0, -OY, width=0.30)
    # pencere kemerleri + denizlik
    for s, u, w, z0, sp, ri in WINDOWS:
        ring = arch_ring(u, w, z0, sp, ri, 5, 0.0, 0.0, width=0.22, protrude=0.035)
        sill = bm_box(w + 0.30, 0.16, 0.09, bevel=0.025)
        paint_part(sill, "kaya", lo=1, hi=0, seed=R.randrange(999), bias=0.3)
        ring.append((sill, Matrix.Translation((u, -0.06, z0 - 0.09))))
        M = wall_matrix(s)
        if s == "W":
            M = M @ Matrix.Scale(-1, 4, (1, 0, 0))  # yerel u yönü batıda ters
        for bm, m in ring:
            parts.append((bm, M @ m))
    out = bm_merge(*parts)
    bmesh.ops.recalc_face_normals(out, faces=out.faces)
    return to_object(out, "SM_Atolye_Tas", col, sharp_angle=40)

# ------------------------------------------------------------------ kapı kanadı (hareketli)
def build_door_leaf(col):
    """Tek mesh. Origin: menteşe ekseni, tabanda. Kapalı hâlde kanat +X yönünde uzanır,
    dış yüzü y=0'da (duvar iç yüzü düzlemi), iç yüzü +y'de. +Z etrafında pozitif dönüş = içeri açılır."""
    parts = []
    wood_seed = R.randrange(9999)
    # dikey tahtalar (4)
    widths = [0.31, 0.34, 0.33, 0.32]
    scale = (LEAF_W - 0.02) / sum(widths)
    x = 0.01
    for i, w in enumerate(widths):
        w *= scale
        h = LEAF_H - R.uniform(0.0, 0.025)
        bm = bm_box(w - 0.008, LEAF_T * 0.62, h, bevel=0.012)
        jitter(bm, 0.004, seed=wood_seed + i, axes=(1, 0, 1))
        paint_part(bm, "ahsap", lo=1, hi=0, noise=0.07, seed=wood_seed + 10 + i, bias=0.15)
        m = Matrix.Translation((x + w / 2, LEAF_T * 0.31, 0.01)) @ Matrix.Rotation(math.radians(R.uniform(-0.4, 0.4)), 4, "Y")
        parts.append((bm, m))
        x += w
    # iç yüzde 3 yatay kuşak (Z kuşak yok: 19. yy)
    for j, zc in enumerate((0.32, 1.16, 1.98)):
        bm = bm_box(LEAF_W - 0.10, LEAF_T * 0.45, 0.17, bevel=0.018)
        jitter(bm, 0.005, seed=wood_seed + 40 + j)
        paint_part(bm, "ahsap", lo=2, hi=1, noise=0.05, seed=wood_seed + 50 + j, bias=0.3)
        parts.append((bm, Matrix.Translation((LEAF_W / 2, LEAF_T * 0.62 + LEAF_T * 0.22, zc - 0.085))
                       @ Matrix.Rotation(math.radians(R.uniform(-0.8, 0.8)), 4, "Y")))
    # dış yüzde etek tahtası
    bm = bm_box(DOOR_W - 0.06, 0.035, 0.14, bevel=0.012)   # boşluğa sığar, söveye girmez
    paint_part(bm, "ahsap", lo=2, hi=1, seed=wood_seed + 60)
    parts.append((bm, Matrix.Translation((LEAF_W / 2, -0.012, 0.012))))
    # dış yüzde 3 şerit menteşe (uç kısmı genişleyen dövme demir) + menteşe halkası eksende
    for j, zc in enumerate((0.32, 1.16, 1.98)):
        L = LEAF_W * 0.66
        poly = [(0.0, -0.028), (L * 0.85, -0.022), (L, -0.034), (L + 0.03, 0.0),
                (L, 0.034), (L * 0.85, 0.022), (0.0, 0.028)]
        bm = bm_prism([(px, pz) for px, pz in poly], -0.012, 0.0)
        bevel_all(bm, 0.004)
        iron(bm)
        parts.append((bm, Matrix.Translation((0.0, 0.0, zc))))
        # ucunda çivi başları
        for xx in (0.18, 0.42, L - 0.02):
            nb = bm_cyl(0.017, 0.012, 0.012, sides=6)
            iron(nb)
            parts.append((nb, Matrix.Translation((xx, -0.012, zc)) @ Matrix.Rotation(math.pi / 2, 4, "X")))
        # menteşe göbeği: eksen üzerinde dikey silindir (pivot ile hizalı)
        kn = bm_cyl(0.026, 0.026, 0.11, sides=8)
        iron(kn)
        parts.append((kn, Matrix.Translation((0.0, 0.0, zc - 0.055))))
    # halka tokmak + arka plaka (dış yüz, kilit tarafı)
    hx, hz = LEAF_W - 0.22, 1.05
    plate = bm_cyl(0.07, 0.065, 0.014, sides=8)
    iron(plate)
    parts.append((plate, Matrix.Translation((hx, -0.012, hz)) @ Matrix.Rotation(math.pi / 2, 4, "X")))
    ring = bmesh.new()
    bmesh.ops.create_cone(ring, cap_ends=False, segments=6, radius1=0.016, radius2=0.016, depth=0.01)
    ring.free()
    tor = bm_lathe([(0.075, -0.012), (0.09, 0.0), (0.075, 0.012), (0.06, 0.0), (0.075, -0.012)], sides=10,
                   close_top=False, close_bottom=False)
    iron(tor)
    parts.append((tor, Matrix.Translation((hx, -0.05, hz - 0.085)) @ Matrix.Rotation(math.pi / 2, 4, "Y")))
    # iç yüzde ahşap kulp + mandal çubuğu
    grip = bm_box(0.05, 0.05, 0.26, bevel=0.015)
    paint_part(grip, "ahsap", lo=1, hi=0, seed=wood_seed + 70)
    parts.append((grip, Matrix.Translation((hx, LEAF_T + 0.06, hz - 0.13))))
    for dz in (-0.09, 0.09):
        st = bm_box(0.04, 0.06, 0.04, bevel=0.01)
        paint_part(st, "ahsap", lo=2, hi=1, seed=wood_seed + 71)
        parts.append((st, Matrix.Translation((hx, LEAF_T + 0.025, hz + dz - 0.02))))
    latch = bm_box(0.34, 0.025, 0.035, bevel=0.008)
    iron(latch)
    parts.append((latch, Matrix.Translation((LEAF_W - 0.12, LEAF_T + 0.02, 1.32))))
    out = bm_merge(*parts)
    ob = to_object(out, "Kapi_Kanat", col, sharp_angle=40)
    ob.location = (HINGE_X, HINGE_Y, 0.0)   # pivot: menteşe ekseni, taban
    return ob

def build_door_frame(col):
    """Sabit: kemer alınlığı (kanadın üstü) + duvardaki menteşe pimleri."""
    parts = []
    # alınlık: kemerin kanat üstünde kalan kısmı, iç düzlemde ahşap pano
    arch = pointed_arch(DOOR_CX, DOOR_W + 0.10, 0.0, DOOR_SPRING, DOOR_RISE + 0.05, n=10)
    top = [(x, z) for (x, z) in arch if z >= LEAF_H + 0.01]
    poly = [(DOOR_CX - (DOOR_W + 0.10) / 2, LEAF_H + 0.012)] + top + [(DOOR_CX + (DOOR_W + 0.10) / 2, LEAF_H + 0.012)]
    bm = bm_prism(poly, -IY + 0.0, -IY + 0.06)
    bevel_all(bm, 0.01)
    paint_part(bm, "ahsap", lo=2, hi=1, seed=81)
    parts.append((bm, Matrix.Identity(4)))
    # alınlıkta 3 dikey çıta + ortada yuvarlak göbek (kütle ayrıntısı)
    for dx in (-0.30, 0.0, 0.30):
        hh = _arch_top_at(DOOR_CX + dx, DOOR_CX, DOOR_W + 0.10, DOOR_SPRING, DOOR_RISE + 0.05) - LEAF_H - 0.04
        b = bm_box(0.05, 0.03, max(0.05, hh), bevel=0.01)
        paint_part(b, "ahsap", lo=1, hi=0, seed=82)
        parts.append((b, Matrix.Translation((DOOR_CX + dx, -IY + 0.07, LEAF_H + 0.01))))
    boss = bm_cyl(0.09, 0.07, 0.04, sides=10)
    paint_part(boss, "altin", lo=2, hi=1, seed=83)
    parts.append((boss, Matrix.Translation((DOOR_CX, -IY + 0.08, LEAF_H + 0.24)) @ Matrix.Rotation(-math.pi / 2, 4, "X")))
    # menteşe pimleri (duvar iç yüzüne çakılı), kanadın göbeğinin hemen altında
    for zc in (0.32, 1.16, 1.98):
        pin = bm_box(0.09, 0.05, 0.05, bevel=0.01)
        iron(pin)
        parts.append((pin, Matrix.Translation((HINGE_X - 0.06, HINGE_Y + 0.025, zc - 0.11))))
    out = bm_merge(*parts)
    return to_object(out, "SM_Atolye_Kapi_Kasa", col, sharp_angle=40)

# ------------------------------------------------------------------ çatı
DECK = 0.05          # kaplama tahtası kalınlığı (iç mekândan görünen tavan)
TILE_ROWS = 13

def _nrm(side):
    n = Vector((side * SLOPE, 0.0, 1.0))
    return n.normalized()

def _sag(y, v):
    """Mahya ortası hafif çöker, saçak uçları hafif kalkar (Tiny Glade referansı)."""
    ty = (y - ROOF_Y0) / (ROOF_Y1 - ROOF_Y0)
    s = -0.11 * math.sin(math.pi * ty) * (0.35 + 0.65 * v)
    lift = 0.17 * (abs(ty - 0.5) * 2) ** 3 * (1 - v) ** 1.5
    return s + lift

def roof_pt(side, v, o, y):
    """side ±1; v: 0 saçak → 1 mahya; o: kaplama üstünden normal yönde mesafe."""
    x = side * EAVE_X * (1 - v)
    p = Vector((x, y, roof_z(x) + DECK * 1.35))
    p += _nrm(side) * o
    p.z += _sag(y, v)
    return p

def build_roof(col):
    parts = []
    ys = [ROOF_Y0 + (ROOF_Y1 - ROOF_Y0) * i / 12 for i in range(13)]
    # kaplama (deck): içeriden ahşap tavan
    for side in (-1, 1):
        secs = []
        for y in ys:
            a = roof_pt(side, 0.0, -DECK * 1.35, y)
            b = roof_pt(side, 1.0, -DECK * 1.35, y)
            c = roof_pt(side, 1.0, 0.0, y)
            d = roof_pt(side, 0.0, 0.0, y)
            b.x = c.x = 0.0
            secs.append([a, b, c, d])
        bm = bm_sweep(secs)
        paint_by(bm, lambda f, l: mix(pal("ahsap", 1), pal("ahsap", 0), 0.35))
        parts.append((bm, None))
    # kiremit sıraları: her sıra hafif eğik bir kama, alt kenarı bir öncekinin üstüne biner
    for side in (-1, 1):
        for i in range(TILE_ROWS):
            v0 = i / TILE_ROWS + R.uniform(-0.004, 0.004)
            v1 = min(1.0, (i + 1.35) / TILE_ROWS)
            th = 0.24 if i == 0 else 0.13       # saçak sırası kalın: çatı kenarı tombul okunur
            jit_end0 = R.uniform(-0.08, 0.02)
            jit_end1 = R.uniform(-0.02, 0.08)
            secs = []
            for j, y in enumerate(ys):
                yy = y + (jit_end0 if j == 0 else jit_end1 if j == len(ys) - 1 else 0.0)
                wob = 0.012 * math.sin(j * 1.7 + i * 2.3)
                vm = v0 + 0.18 / TILE_ROWS
                secs.append([roof_pt(side, v0, th - 0.11 + wob, yy), roof_pt(side, v1, -0.03, yy),
                             roof_pt(side, v1, 0.03, yy), roof_pt(side, vm, th + 0.01 + wob, yy),
                             roof_pt(side, v0, th - 0.03 + wob, yy)])
            bm = bm_sweep(secs)
            k = 1.0 + R.uniform(-0.05, 0.05)
            t_row = i / (TILE_ROWS - 1)
            base = mix(pal("cati", 2), pal("cati", 1), min(1.0, t_row * 1.6))
            base = mix(base, pal("cati", 0), max(0.0, t_row - 0.55) * 0.9)
            c = (min(base[0] * k, 1), min(base[1] * k, 1), min(base[2] * k, 1), 1.0)
            # sıranın alt yüzü (gölge) koyu
            paint_by(bm, lambda f, l, c=c: c if f.normal.z > -0.2 else mix(c, pal("cati", 2), 0.6))
            parts.append((bm, None))
    # mahya kapağı: yuvarlak kesit
    secs = []
    for y in ys:
        top = roof_pt(1, 1.0, 0.0, y)
        cz = top.z + 0.05
        ring = []
        for a in range(7):
            ang = math.pi * a / 6
            ring.append(Vector((math.cos(ang) * 0.19, y, cz + math.sin(ang) * 0.14)))
        secs.append(ring)
    secs_ext = [[Vector((p.x, p.y - 0.06 if k == 0 else p.y, p.z)) for p in s] for k, s in enumerate(secs)]
    bm = bm_sweep(secs_ext)
    paint_by(bm, lambda f, l: mix(pal("cati", 2), pal("cati", 1), 0.4))
    parts.append((bm, None))
    # ön/arka kenar rulosu (verge): çatı ucunu kalın ve yumuşak gösterir
    for yb, dy in ((ROOF_Y0, 0.04), (ROOF_Y1, -0.04)):
        for side in (-1, 1):
            secs = []
            for k in range(9):
                v = k / 8
                c = roof_pt(side, v, 0.10, yb + dy)
                nrm = _nrm(side)
                tan = Vector((-side * EAVE_X, 0, SLOPE * EAVE_X)).normalized()
                ring = []
                for a in range(6):
                    ang = 2 * math.pi * a / 6
                    off = nrm * math.cos(ang) * 0.12 + Vector((0, 1, 0)) * math.sin(ang) * 0.10
                    ring.append(c + off)
                secs.append(ring)
            bm = bm_sweep(secs)
            paint_by(bm, lambda f, l: mix(pal("cati", 2), pal("cati", 1), 0.55))
            parts.append((bm, None))
    out = bm_merge(*parts)
    return to_object(out, "SM_Atolye_Cati", col, sharp_angle=50)

def build_timbers(col):
    """İç makaslar, mahya kirişi, mertekler (saçakta uçları görünür), duvar başlığı, alınlık tahtaları."""
    parts = []
    def beam(p0, p1, w, h, tone=(1, 0), seed=0, up=Vector((0, 0, 1)), sag=0.0, n=2):
        p0, p1 = Vector(p0), Vector(p1)
        d = (p1 - p0)
        L = d.length
        dn = d.normalized()
        side = dn.cross(up)
        if side.length < 1e-4:
            side = dn.cross(Vector((1, 0, 0)))
        side.normalize()
        upv = side.cross(dn).normalized()
        secs = []
        for i in range(n + 1):
            t = i / n
            c = p0 + d * t - upv * (sag * math.sin(math.pi * t))
            secs.append([c + side * (-w / 2) + upv * (-h / 2), c + side * (w / 2) + upv * (-h / 2),
                         c + side * (w / 2) + upv * (h / 2), c + side * (-w / 2) + upv * (h / 2)])
        bm = bm_sweep(secs)
        bevel_all(bm, min(w, h) * 0.12)
        jitter(bm, 0.006, seed=seed)
        paint_part(bm, "ahsap", lo=2 if tone[0] == 2 else 1, hi=tone[1], noise=0.07, seed=seed, bias=0.2)
        return bm
    sd = 300
    # bağ kirişleri + dikme + payandalar
    for k, y in enumerate((-2.30, -0.75, 0.80, 2.35)):
        sd += 7
        parts.append((beam((-IX - 0.2, y, H - 0.14), (IX + 0.2, y, H - 0.14), 0.24, 0.26, (2, 1), sd, sag=0.03, n=6), None))
        ztop = roof_z(0.0) - 0.26
        parts.append((beam((0, y, H - 0.02), (0, y, ztop), 0.20, 0.20, (1, 0), sd + 1, up=Vector((0, 1, 0))), None))
        for s in (-1, 1):
            xa = s * 1.9
            parts.append((beam((0, y, H + 1.05), (xa, y, roof_z(xa) - 0.18), 0.14, 0.16, (1, 0), sd + 2 + s,
                               up=Vector((0, 1, 0))), None))
    # mahya kirişi
    parts.append((beam((0, -OY - 0.1, roof_z(0) - 0.13), (0, OY + 0.1, roof_z(0) - 0.13), 0.24, 0.26, (1, 0), 401, sag=0.04, n=6), None))
    # mertekler: içeride + saçakta görünen uçlar
    nraf = 9
    for i in range(nraf):
        y = -IY + 0.25 + (2 * IY - 0.5) * i / (nraf - 1)
        for s in (-1, 1):
            x0 = s * (EAVE_X - 0.05)
            a = Vector((x0, y, roof_z(x0) - 0.09))
            b = Vector((s * 0.13, y, roof_z(0.13) - 0.09))
            parts.append((beam(a, b, 0.11, 0.15, (1, 0), 500 + i * 3 + s, up=Vector((0, 1, 0))), None))
    # duvar başlığı kaması: iç yüz ile çatı arasındaki boşluğu kapatır (dışta saçak altında kalır)
    for s in (-1, 1):
        xi = s * (IX - 0.08)
        poly = [(xi, H - 0.01), (s * OX, H - 0.01), (xi, roof_z(xi) - 0.005)]
        if s < 0:
            poly = list(reversed(poly))
        bm = bmesh.new()
        a = [bm.verts.new((px, -OY, pz)) for px, pz in poly]
        b = [bm.verts.new((px, OY, pz)) for px, pz in poly]
        bm.faces.new(a); bm.faces.new(list(reversed(b)))
        for i in range(3):
            j = (i + 1) % 3
            bm.faces.new((a[i], b[i], b[j], a[j]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        paint_part(bm, "ahsap", lo=1, hi=1, seed=600 + s)
        parts.append((bm, None))
    # alınlık (rüzgâr) tahtaları: ön ve arka çatı kenarı
    for yb in (ROOF_Y0 + 0.08, ROOF_Y1 - 0.08):
        for s in (-1, 1):
            a = roof_pt(s, -0.01, -0.12, yb)
            b = roof_pt(s, 1.0, -0.12, yb)
            b.x = 0.0
            parts.append((beam(a, b, 0.07, 0.30, (2, 1), 700 + s + int(yb), up=Vector((0, 1, 0))), None))
    out = bm_merge(*parts)
    return to_object(out, "SM_Atolye_Kiris", col, sharp_angle=45)

# ------------------------------------------------------------------ ön alınlık (cumba/çıkma + kafes)
def build_gable(col):
    parts = []
    yf = -OY - JETTY            # alınlık dış yüzü
    zb = H + 0.22               # taban kirişi üstü
    # taban (çıkma) kirişi
    bm = bm_box(2 * 4.22, JETTY + 0.55, 0.24, bevel=0.03)   # uçlar çatının altında kalır
    jitter(bm, 0.008, seed=801)
    paint_part(bm, "ahsap", lo=2, hi=1, seed=802)
    parts.append((bm, Matrix.Translation((0, yf + (JETTY + 0.55) / 2 - 0.05, H - 0.02))))
    # konsollar
    for i, x in enumerate((-3.75, -0.25, 1.35, 3.75)):
        poly = [(0.0, H), (-JETTY - 0.04, H), (-JETTY + 0.02, H - 0.14), (-0.16, H - 0.42), (0.0, H - 0.60)]
        bm = bmesh.new()
        a = [bm.verts.new((x - 0.08, -OY + py, pz)) for py, pz in poly]
        b = [bm.verts.new((x + 0.08, -OY + py, pz)) for py, pz in poly]
        bm.faces.new(a); bm.faces.new(list(reversed(b)))
        n = len(poly)
        for k in range(n):
            j = (k + 1) % n
            bm.faces.new((a[k], b[k], b[j], a[j]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bevel_all(bm, 0.015)
        paint_part(bm, "ahsap", lo=2, hi=1, seed=810 + i)
        parts.append((bm, None))
    # sıva dolgu üçgeni + pencere boşluğu
    xr = (RISE - (zb - H)) / SLOPE
    WX, WZ0, WZ1 = 0.95, zb + 0.40, zb + 1.75
    outer = [(-xr, zb), (xr, zb), (0.0, roof_z(0.0) + 0.02)]
    hole = [(-WX, WZ0), (-WX, WZ1), (WX, WZ1), (WX, WZ0)]
    bm = bm_wall(outer, [hole], 0.16)
    paint_by(bm, lambda f, l: mix(pal("siva", 1), pal("siva", 0), 0.55 + 0.45 * _nz_s(l.vert.co)))
    parts.append((bm, Matrix.Translation((0, yf, 0))))
    # dış yüz ahşap iskelet (dolgudan 3 cm önde)
    def tb(x0, z0, x1, z1, w=0.17, seed=0):
        a, b = Vector((x0, yf - 0.035, z0)), Vector((x1, yf - 0.035, z1))
        d = b - a
        L = d.length
        dn = d.normalized()
        side = Vector((0, 1, 0)).cross(dn).normalized()
        secs = []
        for t in (0.0, 1.0):
            c = a + d * t
            secs.append([c - side * w / 2 + Vector((0, -0.05, 0)), c + side * w / 2 + Vector((0, -0.05, 0)),
                         c + side * w / 2 + Vector((0, 0.06, 0)), c - side * w / 2 + Vector((0, 0.06, 0))])
        bmx = bm_sweep(secs)
        bevel_all(bmx, 0.02)
        jitter(bmx, 0.007, seed=seed)
        paint_part(bmx, "ahsap", lo=2, hi=1, noise=0.08, seed=seed, bias=0.25)
        return bmx
    def zr(x, pad=0.10):
        return roof_z(abs(x)) - pad
    sd = 900
    for x in (-2.55, -WX - 0.09, WX + 0.09, 2.55):
        sd += 3
        parts.append((tb(x, zb, x + R.uniform(-0.03, 0.03), zr(x), seed=sd), None))
    parts.append((tb(-2.55, WZ0 - 0.09, 2.55, WZ0 - 0.09, seed=951), None))           # denizlik kuşağı
    xh = (RISE - (WZ1 + 0.09 - H)) / SLOPE - 0.1
    parts.append((tb(-xh, WZ1 + 0.09, xh, WZ1 + 0.09, seed=952), None))                # lento kuşağı
    xc = (RISE - (zb + 2.75 - H)) / SLOPE - 0.08
    parts.append((tb(-xc, zb + 2.75, xc, zb + 2.75, w=0.15, seed=953), None))          # üst kuşak
    # payandalar: dış bölmelerde çapraz, pencere üstünde ters V
    xo = xr - 0.35
    parts.append((tb(-xo, zb + 0.05, -2.62, zb + 1.35, w=0.15, seed=961), None))
    parts.append((tb(xo, zb + 0.05, 2.62, zb + 1.30, w=0.15, seed=962), None))
    parts.append((tb(-WX - 0.05, WZ1 + 0.18, 0.0, zb + 2.68, w=0.14, seed=963), None))
    parts.append((tb(WX + 0.05, WZ1 + 0.18, 0.0, zb + 2.68, w=0.14, seed=964), None))
    # kafes (mashrabiya esinli, iri ızgara): çerçeve + çubuklar + düğümler
    yk = yf + 0.05
    for (x0, z0, x1, z1) in ((-WX, WZ0, WX, WZ0), (-WX, WZ1, WX, WZ1), (-WX, WZ0, -WX, WZ1), (WX, WZ0, WX, WZ1)):
        bmf = bm_box(abs(x1 - x0) + 0.09 if z0 == z1 else 0.09, 0.10, abs(z1 - z0) + 0.09 if x0 == x1 else 0.09, bevel=0.015, origin="center")
        paint_part(bmf, "ahsap", lo=2, hi=1, seed=971)
        parts.append((bmf, Matrix.Translation(((x0 + x1) / 2, yk, (z0 + z1) / 2))))
    nx, nz = 5, 4
    for i in range(1, nx):
        x = -WX + 2 * WX * i / nx
        bb = bm_box(0.045, 0.05, WZ1 - WZ0, bevel=0.008, origin="center")
        paint_part(bb, "ahsap", lo=1, hi=0, seed=972 + i)
        parts.append((bb, Matrix.Translation((x, yk, (WZ0 + WZ1) / 2))))
    for k in range(1, nz):
        z = WZ0 + (WZ1 - WZ0) * k / nz
        bb = bm_box(2 * WX, 0.05, 0.045, bevel=0.008, origin="center")
        paint_part(bb, "ahsap", lo=1, hi=0, seed=980 + k)
        parts.append((bb, Matrix.Translation((0, yk, z))))
        for i in range(1, nx):
            x = -WX + 2 * WX * i / nx
            nb = bm_box(0.085, 0.07, 0.085, bevel=0.02, origin="center")
            paint_part(nb, "ahsap", lo=1, hi=0, seed=990 + i + k)
            parts.append((nb, Matrix.Translation((x, yk - 0.01, z)) @ Matrix.Rotation(math.pi / 4, 4, "Y")))
    out = bm_merge(*parts)
    return to_object(out, "SM_Atolye_Cephe", col, sharp_angle=40)

def _nz_s(co):
    from mathutils import noise as _n
    return max(0.0, min(1.0, 0.5 + 0.5 * _n.noise(co * 0.8)))

# ------------------------------------------------------------------ baca (arka duvar, ocağın üstü)
CHIM_X = 1.40
def build_chimney(col):
    parts = []
    top = roof_z(0.0) + 1.05
    z = 0.22
    w, d = 1.05, 0.72
    i = 0
    yc = OY + d / 2 - 0.08
    while z < top - 0.05:
        h = min(R.uniform(0.40, 0.55), top - z)
        ww = w - 0.12 * (z / top) + R.uniform(-0.025, 0.025)
        dd = d - 0.08 * (z / top) + R.uniform(-0.02, 0.02)
        bm = bm_box(ww, dd, h - 0.02, bevel=0.03)
        t = z / top
        sootish = max(0.0, (t - 0.82) / 0.18)
        c = mix(pal("kaya", 2), pal("kaya", 1), min(1.0, t * 1.4))
        c = mix(c, pal("kaya", 2), sootish * 0.7)
        k = 1 + R.uniform(-0.05, 0.05)
        paint_flat(bm, (c[0] * k, c[1] * k, c[2] * k, 1.0))
        parts.append((bm, Matrix.Translation((CHIM_X + R.uniform(-0.015, 0.015), yc, z))))
        z += h
        i += 1
    cap = bm_box(w + 0.12, d + 0.12, 0.10, bevel=0.03)
    paint_part(cap, "kaya", lo=1, hi=0, seed=1101)
    parts.append((cap, Matrix.Translation((CHIM_X, yc, top))))
    for dx in (-0.2, 0.2):
        pot = bm_lathe([(0.0, 0.0), (0.13, 0.0), (0.11, 0.18), (0.12, 0.30), (0.09, 0.30), (0.0, 0.29)], sides=8)
        paint_part(pot, "cati", lo=2, hi=1, seed=1102)
        parts.append((pot, Matrix.Translation((CHIM_X + dx, yc, top + 0.10))))
    out = bm_merge(*parts)
    ob = to_object(out, "SM_Atolye_Baca", col, sharp_angle=40)
    ob.rotation_euler = (math.radians(0.8), math.radians(-1.0), 0)
    return ob

# ------------------------------------------------------------------ pencere kafesleri ve kepenkler
def _arch_top_at(u, cx, w, spring, rise):
    pts = pointed_arch(cx, w, 0.0, spring, rise, n=16)[1:-1]
    best = spring
    for a, b in zip(pts, pts[1:]):
        if (a[0] - u) * (b[0] - u) <= 0 and a[0] != b[0]:
            t = (u - a[0]) / (b[0] - a[0])
            best = a[1] + (b[1] - a[1]) * t
    return best

def build_windows(col):
    parts = []
    for wi, (s, u, w, z0, sp, ri) in enumerate(WINDOWS):
        M = wall_matrix(s)
        if s == "W":
            M = M @ Matrix.Scale(-1, 4, (1, 0, 0))
        loc = []
        # iç denizlik tahtası (güvercin/eşya için geniş)
        sill_in = bm_box(w + 0.12, 0.30 if s == "W" else 0.18, 0.05, bevel=0.012)
        paint_part(sill_in, "ahsap", lo=1, hi=0, seed=1200 + wi)
        loc.append((sill_in, Matrix.Translation((u, T - 0.02, z0 - 0.05))))
        # kafes: dikey ve yatay çubuklar
        nv = 3 if w >= 0.9 else 2
        for i in range(1, nv + 1):
            x = u - w / 2 + w * i / (nv + 1)
            ztop = _arch_top_at(x, u, w, sp, ri)
            bb = bm_box(0.045, 0.05, ztop - z0, bevel=0.008)
            paint_part(bb, "ahsap", lo=1, hi=0, seed=1210 + i)
            loc.append((bb, Matrix.Translation((x, T * 0.55, z0))))
        for k in range(1, 3):
            z = z0 + (sp - z0) * k / 3
            bb = bm_box(w, 0.05, 0.045, bevel=0.008, origin="center")
            paint_part(bb, "ahsap", lo=1, hi=0, seed=1220 + k)
            loc.append((bb, Matrix.Translation((u, T * 0.55, z))))
        # dış kepenkler (S ve W): açık, duvara yaslı; boyalı ahşap (palet: dağ mavisi)
        if s in ("S", "W"):
            sh_h = sp - z0 + ri * 0.4
            sh_w = w / 2 + 0.04
            for side in (-1, 1):
                pl = []
                for j in range(3):
                    pw = sh_w / 3
                    bb = bm_box(pw - 0.01, 0.035, sh_h - R.uniform(0, 0.02), bevel=0.008)
                    paint_part(bb, "dag", lo=2, hi=1, noise=0.06, seed=1230 + j + wi * 10 + side)
                    pl.append((bb, Matrix.Translation((-sh_w / 2 + pw * (j + 0.5), 0, 0))))
                for zz in (0.18, sh_h - 0.25):
                    bb = bm_box(sh_w - 0.06, 0.03, 0.10, bevel=0.01)
                    paint_part(bb, "dag", lo=2, hi=2, seed=1240 + wi)
                    pl.append((bb, Matrix.Translation((0, -0.03, zz))))
                shut = bm_merge(*pl)
                cxs = u + side * (w / 2 + 0.26 + sh_w / 2)
                loc.append((shut, Matrix.Translation((cxs, -0.07, z0 + 0.02)) @
                            Matrix.Rotation(math.radians(R.uniform(-1.5, 1.5)), 4, "Y")))
        for bm, m in loc:
            parts.append((bm, M @ m))
    out = bm_merge(*parts)
    bmesh.ops.recalc_face_normals(out, faces=out.faces)
    return to_object(out, "SM_Atolye_Pencere", col, sharp_angle=40)

def build_stage_b():
    col = collection("Atolye")
    return [build_roof(col), build_timbers(col), build_gable(col), build_chimney(col), build_windows(col)]

def build_all():
    objs = build_stage_a() + build_stage_b()
    root = bpy.data.objects.get("Atolye")
    if root is None:
        root = empty("Atolye", collection("Atolye"), kind="ARROWS", size=1.0)
    for o in objs:
        o.parent = root
    return objs

# ------------------------------------------------------------------ çalıştır (A aşaması)
def build_stage_a():
    col = collection("Atolye")
    clear_collection("Atolye")
    objs = [build_walls(col), build_floor(col), build_stones(col), build_door_frame(col), build_door_leaf(col)]
    return objs
