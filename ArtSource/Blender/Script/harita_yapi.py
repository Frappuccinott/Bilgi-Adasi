# Kasaba haritası — köy yapıları: evler, fırın, fenerler, meydan çeşmesi + çınar, köprüler, tarlalar,
# güvercin kulesi, lonca binası, ilan panosu, bayrak dizileri, kilitli kapılar + pus.
# Referans: ArtSource/Referans/yapilar.md. Önce harita.py + harita_doga.py (bu dosya onları yükler).
# Yarışma süresince yazıldı (0'dan üret). Blender 5.x.
#
#   p = r"<repo>/ArtSource/Blender/Script/harita_yapi.py"
#   g = {"__name__": "harita_yapi"}; exec(compile(open(p, encoding="utf-8").read(), p, "exec"), g)
#   g["build_yapilar"]()
#
# Adlandırma (yapilar.md §3): SM_* sabit · <Koy>_Once_* → Before · <Koy>_Sonra_* → After · <Koy>_P<n>_* → Part_n

import os
_dp = os.path.join(r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\ArtSource\Blender\Script", "harita_doga.py")
exec(compile(open(_dp, encoding="utf-8").read(), _dp, "exec"), globals())

YAPI_COL = "Harita_Yapilar"

# ------------------------------------------------------------------ malzemeler
def m_fener():
    return material_named("M_Fener", "#FFD23F", emission=4.0)

def m_duman():
    return material_named("M_Duman", "#DDDCC5", emission=0.3)

def m_bulut():
    return material_named("M_Bulut", "#BDEFFA", emission=0.6)

def m_su():
    return material_named("M_Su", "#FFFFFF")

# ------------------------------------------------------------------ yardımcılar
def chaikin(prof, it=2):
    """Profil köşe kesme (uçlar sabit) — tombul dönel siluet."""
    return [(p.x, p.y) for p in _chaikin2(prof, it)]

def grad(bm, group, lo=2, hi=0, seed=0, noise=0.05, axis=2):
    return paint_part(bm, group, lo=lo, hi=hi, seed=seed, noise=noise) if axis == 2 else bm

def tone(c, k):
    return (min(c[0] * k, 1), min(c[1] * k, 1), min(c[2] * k, 1), 1.0)

def M(x=0.0, y=0.0, z=0.0, yaw=0.0, rx=0.0, ry=0.0):
    return (Matrix.Translation((x, y, z)) @ Matrix.Rotation(yaw, 4, "Z") @
            Matrix.Rotation(ry, 4, "Y") @ Matrix.Rotation(rx, 4, "X"))

def ground_min(x, y, hw, hd, yaw):
    c, s = math.cos(yaw), math.sin(yaw)
    zs = []
    for u in (-hw, 0, hw):
        for v in (-hd, 0, hd):
            zs.append(height(x + u * c - v * s, y + u * s + v * c))
    return min(zs)

_PARENTS = {}

def put(bm, name, x, y, yaw=0.0, z=None, parent=None, mat=None, sharp=35, smooth=True):
    """bm yerel (ön = -Y, taban z=0) → dünyaya. Dönüş meshe uygulanır (09 §7), konum objede."""
    if z is None:
        z = height(x, y)
    transform_bm(bm, Matrix.Rotation(yaw, 4, "Z"))
    ob = to_object(bm, name, collection(YAPI_COL), loc=(x, y, z), sharp_angle=sharp, mat=mat, smooth=smooth)
    if parent is not None:
        parent_keep(ob, parent)
    return ob

def merge(parts):
    return bm_merge(*[(p, None) for p in parts])

def box(sx, sy, sz, x=0, y=0, z=0, bevel=0.0, yaw=0.0, rx=0.0, ry=0.0, origin="bottom"):
    b = bm_box(sx, sy, sz, bevel=bevel, origin=origin)
    transform_bm(b, M(x, y, z, yaw, rx, ry))
    return b

def prism_yz(poly, x0, x1):
    """(u=y, z) çokgeni X boyunca x0→x1 uzatır (beşik çatı alınlığı)."""
    b = bm_prism(poly, x0, x1)
    for v in b.verts:
        v.co.x, v.co.y = v.co.y, v.co.x
    bmesh.ops.recalc_face_normals(b, faces=b.faces)
    return b

def tube_pts(points, r, sides=6):
    pts = [Vector(p) for p in points]
    secs = []
    for i, p in enumerate(pts):
        t = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
        up = Vector((0, 0, 1)) if abs(t.z) < 0.9 else Vector((1, 0, 0))
        n = t.cross(up).normalized()
        b = t.cross(n).normalized()
        secs.append([p + n * math.cos(2 * math.pi * k / sides) * r + b * math.sin(2 * math.pi * k / sides) * r
                     for k in range(sides)])
    return bm_sweep(secs)

def colorize(bm, fn):
    paint_by(bm, fn)
    return bm

def flat(bm, c):
    paint_flat(bm, c)
    return bm

# ------------------------------------------------------------------ EV PARÇALARI
def roof_bm(W, D, base_z, seed, ov_x=0.5, ov_e=0.6, pitch_deg=None, rows=4, col_group="cati"):
    """Beşik çatı, mahya X boyunca. Kiremit sıraları saçakta basamaklı siluet verir; mahya hafif sarkık."""
    rnd = random.Random(seed)
    pitch = math.radians(pitch_deg if pitch_deg else rnd.uniform(40, 46))
    halfD = D / 2 + 0.1
    rise = halfD * math.tan(pitch)
    ridge = Vector((0, 0, base_z + rise + 0.12))
    Lx = W + 2 * ov_x
    L = halfD / math.cos(pitch) + ov_e
    th = 0.17
    parts = []
    for s in (-1, 1):
        X = Vector((1 if s > 0 else -1, 0, 0))
        d = Vector((0, s * math.cos(pitch), -math.sin(pitch)))
        n = Vector((0, s * math.sin(pitch), math.cos(pitch)))
        basis = Matrix((X, d, n)).transposed().to_4x4()
        for i in range(rows):
            a = L * i / rows
            b = L * (i + 1) / rows + (0.1 if i < rows - 1 else 0.0)
            st = bm_box(Lx + rnd.uniform(-0.06, 0.06), b - a, th, origin="center")
            for v in st.verts:
                if v.co.y > 0:
                    v.co.z += 0.07                       # sıranın alt kenarı kabarık
            k = 0.93 + 0.1 * rnd.random() + (0.04 if i % 2 else 0)
            lo, hi = pal(col_group, 2), pal(col_group, 1 if i < rows - 1 else 1)
            paint_by(st, lambda f, l, k=k, lo=lo, hi=hi: tone(mix(hi, lo, 0.35 + 0.3 * max(0, -f.normal.z) +
                                                                 (0.25 if f.normal.dot(Vector((0, 1, 0))) > 0.7 else 0)), k))
            transform_bm(st, Matrix.Translation(ridge + d * ((a + b) / 2) + n * (th / 2)) @ basis)
            parts.append(st)
    cap = bm_box(Lx + 0.12, 0.34, 0.34, origin="center")
    transform_bm(cap, Matrix.Translation(ridge + Vector((0, 0, 0.12))) @ Matrix.Rotation(math.pi / 4, 4, "X"))
    paint_by(cap, lambda f, l: tone(pal(col_group, 2), 0.95 + 0.1 * max(0, f.normal.z)))
    parts.append(cap)
    r = merge(parts)
    sag = rnd.uniform(0.07, 0.14)
    for v in r.verts:
        v.co.z -= sag * max(0.0, 1 - (2 * v.co.x / Lx) ** 2)
    return r, ridge.z, rise, pitch

def gable_bm(W, D, base_z, rise, timber):
    halfD = D / 2 + 0.02
    poly = [(-halfD, 0.0), (halfD, 0.0), (0.0, rise)]
    g = prism_yz(poly, -W / 2 + 0.02, W / 2 - 0.02)
    transform_bm(g, Matrix.Translation((0, 0, base_z)))
    if timber:
        paint_by(g, lambda f, l: tone(mix(pal("ahsap", 1), pal("ahsap", 0), 0.3), 1.0))
    else:
        grad(g, "siva", 1, 0)
    return g

def window_bm(w=0.8, h=0.95, shutters=False):
    """Yerel: pencere ön yüzü -Y'de, alt kenar z=0, merkez x=0. Kalın çerçeve + koyu cam + haç kayıt + denizlik."""
    parts = []
    fr = box(w + 0.2, 0.14, h + 0.2, z=-0.1, bevel=0.025)
    grad(fr, "ahsap", 2, 1)
    parts.append(fr)
    gl = box(w, 0.14, h, y=-0.012)
    flat(gl, mix(pal("dag", 2), pal("ahsap", 2), 0.45))
    parts.append(gl)
    mul_v = box(0.07, 0.16, h, y=-0.02)
    mul_h = box(w, 0.16, 0.07, y=-0.02, z=h * 0.55)
    for m_ in (mul_v, mul_h):
        flat(m_, pal("ahsap", 1))
        parts.append(m_)
    sill = box(w + 0.42, 0.3, 0.1, y=-0.08, z=-0.2, bevel=0.02)
    grad(sill, "kaya", 1, 0)
    parts.append(sill)
    return merge(parts)

def shutter_pair(w, h, open_=False):
    parts = []
    for s in (-1, 1):
        lf = box(w / 2, 0.06, h, origin="bottom")
        transform_bm(lf, Matrix.Translation((-s * w / 4, 0, 0)))          # menteşe x=0'da
        if open_:
            transform_bm(lf, Matrix.Rotation(s * math.radians(105), 4, "Z"))
        transform_bm(lf, Matrix.Translation((s * w / 2, -0.12, 0)))
        grad(lf, "ahsap", 2, 0, seed=7 + s)
        parts.append(lf)
    return merge(parts)

def door_bm(w=1.1, h_spring=1.75, rise=0.45, base=0.5):
    parts = []
    sur = bm_prism(pointed_arch(0, w + 0.38, base, base + h_spring, rise + 0.16), -0.07, 0.05)
    grad(sur, "kaya", 1, 0)
    parts.append(sur)
    dr = bm_prism(pointed_arch(0, w, base, base + h_spring, rise), -0.1, -0.03)
    paint_by(dr, lambda f, l: tone(mix(pal("ahsap", 2), pal("ahsap", 1), 0.4 + 0.4 * (int((l.vert.co.x + 2) / 0.22) % 2)), 1.0))
    parts.append(dr)
    for zz in (base + 0.45, base + 1.35):
        band = box(w * 0.92, 0.04, 0.07, y=-0.11, z=zz)
        flat(band, demir(0.9))
        parts.append(band)
    step = box(w + 0.6, 0.6, base + 0.25, y=-0.32, z=-0.25, bevel=0.04)
    jitter(step, 0.02, seed=3)
    grad(step, "kaya", 2, 0)
    parts.append(step)
    return merge(parts)

def timber_frame(W, D, z0, H, seed, sides=("front", "left", "right")):
    """Sıva üstünde kalın ahşap çatkı: köşe dikmeleri, ara kuşak, tepe kirişi, payandalar."""
    rnd = random.Random(seed)
    parts = []
    t = 0.2
    out = 0.035
    for sx in (-1, 1):
        for sy in (-1, 1):
            p = box(t, t, H, x=sx * (W / 2 - t / 2 + out), y=sy * (D / 2 - t / 2 + out), z=z0)
            parts.append(p)
    for zz in (z0 + H * 0.52, z0 + H - 0.14):
        parts.append(box(W + 0.12, t * 0.8, 0.16, y=-(D / 2 + out), z=zz))
        parts.append(box(W + 0.12, t * 0.8, 0.16, y=(D / 2 + out), z=zz))
        parts.append(box(t * 0.8, D + 0.12, 0.16, x=-(W / 2 + out), z=zz))
        parts.append(box(t * 0.8, D + 0.12, 0.16, x=(W / 2 + out), z=zz))
    # payandalar: ön yüzde iki köşeden, yanlarda birer
    zt = z0 + H * 0.52
    lb = math.hypot(1.1, zt - z0)
    ang = math.atan2(zt - z0, 1.1)
    for sx in (-1, 1):
        br = box(lb, 0.14, 0.14, origin="center")
        transform_bm(br, M(sx * (W / 2 - 0.65), -(D / 2 + out), (z0 + zt) / 2, ry=sx * ang))
        parts.append(br)
    for sx in (-1, 1):
        br = box(0.14, lb, 0.14, origin="center")
        transform_bm(br, M(sx * (W / 2 + out), 0.6 * rnd.choice((-1, 1)), (z0 + zt) / 2, rx=sx * ang * 0.0 + ang))
        parts.append(br)
    m_ = merge(parts)
    paint_by(m_, lambda f, l: tone(mix(pal("ahsap", 2), pal("ahsap", 1), 0.35 + 0.4 * max(0, f.normal.z)), 1.0))
    return m_

def house_bm(W, D, H, seed, timber=True, windows=2, door_x=None, chimney=True):
    rnd = random.Random(seed)
    parts = []
    fz = 0.5
    fnd = box(W + 0.36, D + 0.36, 0.95, z=-0.45, bevel=0.06)
    jitter(fnd, 0.03, seed=seed)
    grad(fnd, "kaya", 2, 1, seed=seed)
    parts.append(fnd)
    body = box(W, D, H, z=fz, bevel=0.03)
    k = 0.96 + 0.08 * rnd.random()
    paint_by(body, lambda f, l: tone(mix(pal("siva", 1), pal("siva", 0), smooth01((l.vert.co.z - fz) / H * 1.4)), k))
    parts.append(body)
    if timber:
        parts.append(timber_frame(W, D, fz, H, seed))
    roof, ridge_z, rise, pitch = roof_bm(W, D, fz + H, seed)
    parts.append(roof)
    parts.append(gable_bm(W, D, fz + H, rise, timber))
    # kapı
    dx = door_x if door_x is not None else rnd.choice((-1, 1)) * W * 0.18
    d = door_bm()
    transform_bm(d, Matrix.Translation((dx, -D / 2, 0)))
    parts.append(d)
    anchors = {"windows": [], "door": Vector((dx, -D / 2, 0)), "ridge": ridge_z}
    # pencereler: ön yüz (kapıdan uzak), yanlar
    xs = [x for x in (-W * 0.32, W * 0.32, 0.0) if abs(x - dx) > 1.25][:windows]
    for x in xs:
        wb = window_bm()
        transform_bm(wb, Matrix.Translation((x, -D / 2 - 0.02, fz + 1.0)))
        parts.append(wb)
        anchors["windows"].append(Vector((x, -D / 2 - 0.02, fz + 1.0)))
    for s in (-1, 1):
        wb = window_bm(0.7, 0.85)
        transform_bm(wb, Matrix.Translation((s * (W / 2 + 0.02), rnd.uniform(-0.4, 0.4), fz + 1.05)) @
                     Matrix.Rotation(s * math.pi / 2, 4, "Z"))
        parts.append(wb)
    if chimney:
        cx = rnd.choice((-1, 1)) * W * 0.27
        cy = rnd.choice((-1, 1)) * D * 0.16
        top = ridge_z + 0.9
        ch = box(0.7, 0.7, top - (fz + H - 0.2), x=cx, y=cy, z=fz + H - 0.2, bevel=0.03)
        jitter(ch, 0.02, seed=seed + 5)
        grad(ch, "kaya", 1, 0, seed=seed)
        parts.append(ch)
        cap = box(0.9, 0.9, 0.12, x=cx, y=cy, z=top, bevel=0.02)
        grad(cap, "kaya", 1, 0)
        parts.append(cap)
        anchors["chimney"] = Vector((cx, cy, top + 0.12))
    return merge(parts), anchors

# ------------------------------------------------------------------ FIRIN
def firin_bm(seed=611):
    W, D, H = 5.0, 4.4, 2.9
    hb, an = house_bm(W, D, H, seed, timber=False, windows=1, door_x=-0.9, chimney=False)
    parts = [hb]
    ox = W / 2 + 1.55
    pl = box(3.1, 3.1, 0.9, x=ox, z=-0.4, bevel=0.06)
    jitter(pl, 0.03, seed=seed)
    grad(pl, "kaya", 2, 1)
    parts.append(pl)
    dome = bm_lathe(chaikin([(1.35, 0.5), (1.38, 0.9), (1.22, 1.55), (0.85, 2.05), (0.35, 2.32), (0.0, 2.36)], 1), sides=14)
    transform_bm(dome, Matrix.Translation((ox, 0.1, 0)))
    grad(dome, "kil", 1, 0, seed=seed)
    parts.append(dome)
    mouth = bm_prism(pointed_arch(0, 0.8, 0.55, 0.95, 0.3), -0.1, 0.05)
    transform_bm(mouth, Matrix.Translation((ox, -1.15, 0)))
    flat(mouth, mix(pal("ahsap", 2), pal("kaya", 2), 0.5))
    parts.append(mouth)
    lip = bm_prism(pointed_arch(0, 1.15, 0.5, 0.95, 0.42), -0.12, 0.1)
    transform_bm(lip, Matrix.Translation((ox, -1.2, 0)))
    grad(lip, "kaya", 1, 0)
    parts.append(lip)
    # baca: ağzın üstünde (MOT, yapilar.md #5)
    ch = box(0.55, 0.55, 2.1, x=ox, y=-0.75, z=1.75, bevel=0.03)
    grad(ch, "kil", 2, 1)
    parts.append(ch)
    cap = box(0.75, 0.75, 0.1, x=ox, y=-0.75, z=3.85, bevel=0.02)
    grad(cap, "kaya", 1, 0)
    parts.append(cap)
    # odun yığını (fırının hikâyesi: yakıt)
    for i in range(7):
        lg = bm_cyl(0.1, 0.1, 1.0, sides=6)
        transform_bm(lg, M(ox - 1.0 + (i % 4) * 0.22 + (0.11 if i >= 4 else 0), 1.35, 0.55 + 0.1 + (0.19 if i >= 4 else 0), ry=math.pi / 2)
                     @ Matrix.Translation((0, 0, -0.5)))
        paint_by(lg, lambda f, l: tone(pal("ahsap", 1) if abs(f.normal.x) < 0.7 else pal("kil", 0), 1.0))
        parts.append(lg)
    anchors = {"mouth": Vector((ox, -1.32, 0.6)), "chimney": Vector((ox, -0.75, 3.95))}
    return merge(parts), anchors

def kor_bm(at):
    g = bm_prism(pointed_arch(0, 0.74, 0.55, 0.93, 0.27), -0.02, 0.0)
    transform_bm(g, Matrix.Translation(at + Vector((0, 0.05, -0.6))))
    flat(g, pal("altin", 0))
    return g

def duman_bm(at, seed):
    rnd = random.Random(seed)
    parts = []
    for i in range(4):
        b = _blob(at + Vector((rnd.uniform(-0.15, 0.15) + i * 0.18, rnd.uniform(-0.1, 0.1), 0.35 + i * 0.55)),
                  0.28 + i * 0.12, sub=1, squash=0.9, seed=seed + i, jit=0.15)
        flat(b, mix(pal("siva", 0), pal("gok", 0), 0.3 + 0.15 * i))
        parts.append(b)
    return merge(parts)

# ------------------------------------------------------------------ FENER DİREĞİ
def fener_direk_bm(seed):
    parts = []
    base = box(0.45, 0.45, 0.55, z=-0.2, bevel=0.05)
    jitter(base, 0.02, seed=seed)
    grad(base, "kaya", 2, 0)
    parts.append(base)
    post = box(0.17, 0.17, 2.85, z=0.3, bevel=0.02)
    grad(post, "ahsap", 2, 1)
    parts.append(post)
    arm = box(0.12, 0.85, 0.12, y=-0.38, z=2.95)
    grad(arm, "ahsap", 2, 1)
    parts.append(arm)
    brace = box(0.1, 0.55, 0.1, origin="center")
    transform_bm(brace, M(0, -0.2, 2.8, rx=math.radians(40)))
    flat(brace, pal("ahsap", 2))
    parts.append(brace)
    # fener: bakır kapak + alt tabla + 4 köşe dikmesi; cam ayrı
    lx, ly, lz = 0.0, -0.72, 2.32
    hook = box(0.03, 0.03, 0.2, x=lx, y=ly, z=lz + 0.5)
    flat(hook, demir(0.8))
    parts.append(hook)
    top = bm_cyl(0.27, 0.06, 0.2, sides=4)
    transform_bm(top, M(lx, ly, lz + 0.42, yaw=math.pi / 4))
    grad(top, "bakir", 2, 0)
    parts.append(top)
    bot = box(0.36, 0.36, 0.06, x=lx, y=ly, z=lz - 0.02, bevel=0.01)
    grad(bot, "bakir", 2, 1)
    parts.append(bot)
    for sx in (-1, 1):
        for sy in (-1, 1):
            c = box(0.04, 0.04, 0.42, x=lx + sx * 0.15, y=ly + sy * 0.15, z=lz)
            flat(c, pal("bakir", 2))
            parts.append(c)
    return merge(parts), Vector((lx, ly, lz + 0.04))

def fener_cam_bm(at, lit):
    s = 0.27 if lit else 0.25
    g = box(s, s, 0.34 if lit else 0.32, x=at.x, y=at.y, z=at.z + (0.0 if lit else 0.01), bevel=0.02)
    flat(g, pal("altin", 0) if lit else mix(pal("dag", 2), pal("ahsap", 2), 0.6))
    return g

# ------------------------------------------------------------------ ÇEŞME (meydan)
def cesme_bm(seed=721):
    parts = []
    W, Dp, H = 2.4, 0.8, 2.6
    body = box(W, Dp, H, y=0.25, z=-0.3, bevel=0.06)
    jitter(body, 0.02, seed=seed)
    grad(body, "kaya", 2, 0, seed=seed)
    parts.append(body)
    cor = box(W + 0.3, Dp + 0.3, 0.22, y=0.25, z=H - 0.3, bevel=0.05)
    grad(cor, "kaya", 1, 0)
    parts.append(cor)
    cap = bm_prism([(-W / 2 - 0.1, 0), (W / 2 + 0.1, 0), (0, 0.55)], -0.25, 0.75)
    transform_bm(cap, Matrix.Translation((0, 0.25, H - 0.08)))
    grad(cap, "cati", 2, 1)
    parts.append(cap)
    niche_fr = bm_prism(pointed_arch(0, 1.45, 0.4, 1.55, 0.5), -0.2, -0.13)
    grad(niche_fr, "kaya", 1, 0)
    parts.append(niche_fr)
    niche = bm_prism(pointed_arch(0, 1.1, 0.45, 1.5, 0.4), -0.22, -0.14)
    flat(niche, mix(pal("kaya", 2), pal("dag", 2), 0.4))
    parts.append(niche)
    # yazıt taşı (yazı TMP ile, 09 §7) ve lüle
    plate = box(0.8, 0.06, 0.35, y=-0.25, z=1.1, bevel=0.01)
    grad(plate, "siva", 1, 0)
    parts.append(plate)
    sp = tube_pts([(0, -0.2, 0.95), (0, -0.4, 0.93), (0, -0.48, 0.86)], 0.045, sides=6)
    grad(sp, "bakir", 2, 0)
    parts.append(sp)
    tas = bm_lathe([(0.0, 0.0), (0.09, 0.0), (0.12, 0.06), (0.11, 0.08), (0.0, 0.03)], sides=8)
    transform_bm(tas, Matrix.Translation((0.35, -0.3, 0.72)))
    grad(tas, "bakir", 2, 0)
    parts.append(tas)
    ch = tube_pts([(0.2, -0.24, 0.98), (0.3, -0.3, 0.86), (0.35, -0.3, 0.8)], 0.012, sides=4)
    flat(ch, pal("bakir", 2))
    parts.append(ch)
    # yalak (önünde, hayvanlar ve testi için)
    tw, td, th = 2.1, 0.85, 0.62
    for (sx, sy, x, y) in ((tw, 0.16, 0, -0.3 - td + 0.08), (tw, 0.16, 0, -0.38), (0.16, td, -tw / 2 + 0.08, -0.3 - td / 2 - 0.04),
                           (0.16, td, tw / 2 - 0.08, -0.3 - td / 2 - 0.04)):
        wb = box(sx, sy, th, x=x, y=y, z=-0.2, bevel=0.03)
        grad(wb, "kaya", 2, 0)
        parts.append(wb)
    floor = box(tw - 0.2, td - 0.2, 0.1, y=-0.3 - td / 2 - 0.04, z=-0.1)
    flat(floor, mix(pal("kil", 2), pal("kaya", 2), 0.5))
    parts.append(floor)
    return merge(parts), {"trough_z": 0.0, "trough_c": Vector((0, -0.3 - td / 2 - 0.04)), "tw": tw, "td": td}

def cesme_su_bm(an):
    w = box(an["tw"] - 0.24, an["td"] - 0.26, 0.02, x=an["trough_c"].x, y=an["trough_c"].y, z=0.3)
    flat(w, pal("su", 1))
    jet = tube_pts([(0, -0.48, 0.84), (0, -0.52, 0.6), (0, -0.53, 0.32)], 0.03, sides=6)
    flat(jet, pal("su", 0))
    return merge([w, jet])

def kuru_yaprak_bm(an, seed):
    rnd = random.Random(seed)
    parts = []
    for i in range(9):
        lf = box(0.16, 0.1, 0.015, x=an["trough_c"].x + rnd.uniform(-0.8, 0.8), y=an["trough_c"].y + rnd.uniform(-0.25, 0.25),
                 z=0.005, yaw=rnd.uniform(0, 3.14))
        flat(lf, mix(pal("altin", 2), pal("ahsap", 1), rnd.random()))
        parts.append(lf)
    return merge(parts)

# ------------------------------------------------------------------ KÖPRÜLER
BR_L, BR_W, BR_HUMP, BR_SPAN = 14.0, 3.8, 0.55, 7.4
BR_SPRING, BR_RISE, BR_FOOT = -0.95, 1.05, -2.6

def _deck(u):
    return 0.05 + BR_HUMP * (1 - (2 * u / BR_L) ** 2)

def _deck_pts(u0, u1, n=12):
    return [(u0 + (u1 - u0) * i / n, _deck(u0 + (u1 - u0) * i / n)) for i in range(n + 1)]

def _arch():
    return pointed_arch(0, BR_SPAN, -2.3, BR_SPRING, BR_RISE, n=7)

def _bridge_paint(bm, broken=False):
    rnd = random.Random(31)
    tones = {}
    def fn(f, l):
        c = f.calc_center_median()
        if f.normal.z > 0.75:
            return tone(mix(pal("kil", 1), pal("kaya", 0), 0.45), 1.0)
        if f.normal.z < -0.3 or (abs(f.normal.y) < 0.4 and abs(c.x) < BR_SPAN / 2 + 0.05):
            return tone(mix(pal("kaya", 2), pal("dag", 2), 0.3), 1.0)       # kemer içi
        ang = math.atan2(c.z - BR_SPRING, c.x)
        dist_arch = math.hypot(c.x * 0.95, (c.z - BR_SPRING) * 1.4)
        if abs(f.normal.y) > 0.6 and BR_SPAN * 0.5 - 0.1 < dist_arch < BR_SPAN * 0.5 + 0.75 and c.z > BR_SPRING - 0.2:
            k = int((ang + 4) / 0.2) % 2                                    # kemer taşları
            return pal("kaya", 0) if k else mix(pal("kaya", 1), pal("kaya", 0), 0.5)
        k = tones.setdefault(f.index, 0.92 + 0.14 * rnd.random())
        return tone(mix(pal("kaya", 1), pal("kaya", 0), 0.3 + 0.25 * smooth01((c.z + 2) / 3)), k)
    bm.faces.index_update()
    paint_by(bm, fn)
    return bm

def _parapets(u0, u1, w_in=0.0):
    parts = []
    for s in (-1, 1):
        secs = []
        for (u, z) in _deck_pts(u0, u1, 12):
            y0 = s * (BR_W / 2 - w_in)
            y1 = s * (BR_W / 2 - 0.36 - w_in)
            secs.append([Vector((u, y0, z - 0.05)), Vector((u, y1, z - 0.05)), Vector((u, y1, z + 0.6)), Vector((u, y0, z + 0.6))])
        p = bm_sweep(secs)
        paint_by(p, lambda f, l: tone(mix(pal("kaya", 1), pal("kaya", 0), 0.4 + 0.5 * max(0, f.normal.z)), 1.0))
        parts.append(p)
        # uçta kaba taş baş
        for u in (u0, u1):
            hd = box(0.5, 0.48, 0.85, x=u, y=s * (BR_W / 2 - 0.18 - w_in), z=_deck(u) - 0.1, bevel=0.05)
            grad(hd, "kaya", 1, 0)
            parts.append(hd)
    return parts

def kopru_tas_bm():
    outer = [(-BR_L / 2, BR_FOOT)] + _deck_pts(-BR_L / 2, BR_L / 2, 16) + [(BR_L / 2, BR_FOOT)]
    body = bm_wall(outer, [_arch()], BR_W)
    transform_bm(body, Matrix.Translation((0, -BR_W / 2, 0)))
    _bridge_paint(body)
    return merge([body] + _parapets(-BR_L / 2, BR_L / 2))

def kopru_yikik_bm(seed=41):
    """Onarılmış köprünün içinde kalacak kadar küçültülmüş iki ayak (yapilar.md §2 notu)."""
    rnd = random.Random(seed)
    arch = _arch()
    pieces = []
    for side in (-1, 1):
        cut = 2.6 + rnd.uniform(-0.2, 0.2)
        top = [(u, z - 0.04) for (u, z) in _deck_pts(-BR_L / 2 + 0.03, -cut, 8)]
        jag = [(-cut + 0.25, _deck(-cut) - 0.5), (-cut - 0.1, _deck(-cut) - 0.9), (-cut + 0.15, -0.2)]
        left_arc = [p for p in arch if p[0] <= -cut + 0.3 and p[1] > -2.31]
        left_arc = sorted(left_arc, key=lambda p: -p[1])        # yukarıdan aşağı
        poly = [(-BR_L / 2 + 0.03, BR_FOOT + 0.03)] + top + jag + [(x + 0.03, z) for (x, z) in left_arc] + \
               [(-BR_SPAN / 2 + 0.03, BR_FOOT + 0.03)]
        if side > 0:
            poly = [(-u, z) for (u, z) in reversed(poly)]
        w = bm_wall(poly, [], BR_W - 0.08)
        transform_bm(w, Matrix.Translation((0, -BR_W / 2 + 0.04, 0)))
        _bridge_paint(w, broken=True)
        pieces.append(w)
        pieces += _parapets(*(sorted((side * (BR_L / 2 - 0.03), side * (cut + 0.6)))), w_in=0.04)
    # dere yatağında düşmüş kemer taşları
    for i in range(6):
        r = make_rock("_tmp_tas", 900 + i, size=0.35 + 0.25 * rnd.random(), flat=0.55)
        me = r.data
        bm = bmesh.new()
        bm.from_mesh(me)
        bpy.data.objects.remove(r, do_unlink=True)
        bpy.data.meshes.remove(me)
        lay = bm.loops.layers.float_color.get("Col")
        transform_bm(bm, M(rnd.uniform(-2.0, 2.0), rnd.uniform(-1.6, 1.6), -1.55, yaw=rnd.uniform(0, 6)))
        pieces.append(bm)
    return merge(pieces)

def kopru_gecici_bm(length=11.0, seed=51):
    rnd = random.Random(seed)
    parts = []
    def zc(u):
        return 0.12 - 0.18 * (1 - (2 * u / length) ** 2)
    for s in (-1, 1):
        lg = tube_pts([(-length / 2 - 0.4, s * 0.55, zc(-length / 2) - 0.12), (0, s * 0.55, zc(0) - 0.14),
                       (length / 2 + 0.4, s * 0.55, zc(length / 2) - 0.12)], 0.14, sides=7)
        grad(lg, "ahsap", 2, 1)
        parts.append(lg)
    u = -length / 2
    while u < length / 2:
        pw = rnd.uniform(0.24, 0.32)
        pl = box(pw, 1.7 + rnd.uniform(-0.1, 0.1), 0.06, x=u + pw / 2, y=rnd.uniform(-0.05, 0.05), z=zc(u),
                 yaw=rnd.uniform(-0.05, 0.05), bevel=0.01)
        grad(pl, "ahsap", 1, 0, seed=int(u * 10))
        parts.append(pl)
        u += pw + 0.04
    for s in (-1, 1):
        posts = []
        for uu in (-length / 2, -length / 6, length / 6, length / 2):
            pst = box(0.12, 0.12, 1.25, x=uu, y=s * 0.82, z=zc(uu) - 0.25)
            grad(pst, "ahsap", 2, 1)
            parts.append(pst)
            posts.append(Vector((uu, s * 0.82, zc(uu) + 0.95)))
        rope = tube_pts([p + Vector((0, 0, -0.08 * (i % 2))) for i, p in enumerate(posts)], 0.025, sides=4)
        flat(rope, mix(pal("kil", 0), pal("altin", 2), 0.3))
        parts.append(rope)
    return merge(parts)

# ------------------------------------------------------------------ TARLA
def tarla_bm(cx, cy, w, d, yaw, crops=None, seed=0):
    """Sürülmüş tarla (sırtlar yerel X boyunca). crops: None | 'bugday' | 'sebze' → yalnız ekin döner."""
    rnd = random.Random(seed)
    c, s = math.cos(yaw), math.sin(yaw)
    def world(u, v, dz):
        x = cx + u * c - v * s
        y = cy + u * s + v * c
        return Vector((x - cx, y - cy, height(x, y) - height(cx, cy) + dz))
    pitch_v = 0.75
    nrows = int(d / pitch_v)
    if crops is None:
        bm = bmesh.new()
        lay = bm.loops.layers.float_color.new("Col")
        nu = 6
        nv = nrows * 4
        grid = []
        for j in range(nv + 1):
            v = -d / 2 + d * j / nv
            ridge = 0.5 + 0.5 * math.cos(2 * math.pi * (v + d / 2) / pitch_v)
            row = []
            for i in range(nu + 1):
                u = -w / 2 + w * i / nu
                edge = min(1.0, min(abs(u) - w / 2, abs(v) - d / 2) * -1.2)
                row.append((bm.verts.new(world(u, v, 0.03 + 0.14 * ridge * max(0.0, edge))), ridge))
            grid.append(row)
        for j in range(nv):
            for i in range(nu):
                f = bm.faces.new((grid[j][i][0], grid[j][i + 1][0], grid[j + 1][i + 1][0], grid[j + 1][i][0]))
                for l in f.loops:
                    r = next(rr for (vv, rr) in grid[j] + grid[j + 1] if vv == l.vert)
                    l[lay] = mix(mix(pal("ahsap", 2), pal("kil", 2), 0.4), pal("kil", 1), r * 0.85)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        for f in bm.faces:
            if f.normal.z < 0:
                f.normal_flip()
        # kenarda alçak çit: kazık + tek kol
        parts = [bm]
        corners = [(-w / 2 - 0.4, -d / 2 - 0.4), (w / 2 + 0.4, -d / 2 - 0.4), (w / 2 + 0.4, d / 2 + 0.4), (-w / 2 - 0.4, d / 2 + 0.4)]
        for k in range(4):
            if k == 0:
                continue                                  # yol tarafı açık
            (u0, v0), (u1, v1) = corners[k], corners[(k + 1) % 4]
            n = max(2, int(math.hypot(u1 - u0, v1 - v0) / 1.8))
            pts = []
            for i in range(n + 1):
                u = u0 + (u1 - u0) * i / n
                v = v0 + (v1 - v0) * i / n
                p = world(u, v, 0)
                ps = box(0.1, 0.1, 1.0, x=p.x, y=p.y, z=p.z - 0.2, yaw=rnd.uniform(0, 1))
                grad(ps, "ahsap", 2, 1)
                parts.append(ps)
                pts.append(p + Vector((0, 0, 0.62 + rnd.uniform(-0.04, 0.04))))
            rl = tube_pts(pts, 0.04, sides=4)
            grad(rl, "ahsap", 2, 1)
            parts.append(rl)
        return merge(parts)
    parts = []
    for j in range(nrows):
        v = -d / 2 + pitch_v * (j + 0.0) + 0.02
        if abs(v) > d / 2 - 0.3:
            continue
        segs = 5
        for k in range(segs):
            u0 = -w / 2 + 0.4 + (w - 0.8) * k / segs
            u1 = -w / 2 + 0.4 + (w - 0.8) * (k + 1) / segs - 0.05
            hgt = (0.55 if crops == "bugday" else 0.3) * rnd.uniform(0.8, 1.15)
            wid = 0.42 if crops == "bugday" else 0.36
            a0, a1 = world(u0, v - wid / 2, 0.12), world(u0, v + wid / 2, 0.12)
            b0, b1 = world(u1, v - wid / 2, 0.12), world(u1, v + wid / 2, 0.12)
            ta = world(u0, v, 0.12 + hgt)
            tb = world(u1, v, 0.12 + hgt * rnd.uniform(0.9, 1.1))
            pb = bmesh.new()
            vs = [pb.verts.new(p) for p in (a0, a1, ta, b0, b1, tb)]
            for f in ((0, 1, 2), (3, 5, 4), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)):
                pb.faces.new([vs[i] for i in f])
            bmesh.ops.recalc_face_normals(pb, faces=pb.faces)
            if crops == "bugday":
                paint_by(pb, lambda f_, l: mix(pal("altin", 1), pal("altin", 0), smooth01((l.vert.co.z - a0.z) / hgt)))
            else:
                paint_by(pb, lambda f_, l: mix(pal("yaprak", 2), pal("cimen", 0), smooth01((l.vert.co.z - a0.z) / hgt)))
            parts.append(pb)
    return merge(parts)

# ------------------------------------------------------------------ GÜVERCİN KULESİ
def kule_bm(seed=811):
    parts = []
    pl = bm_cyl(2.75, 2.65, 0.9, sides=16)
    transform_bm(pl, Matrix.Translation((0, 0, -0.5)))
    jitter(pl, 0.03, seed=seed)
    grad(pl, "kaya", 2, 1)
    parts.append(pl)
    zs = [0.4, 1.6, 2.8, 4.0] + [4.4 + 0.42 * i for i in range(11)] + [9.2, 9.4]
    prof = [(2.42 - 0.042 * (z - 0.4), z) for z in zs]          # içe eğik duvar (Isfahan)
    body = bm_lathe(prof, sides=24, close_top=False, close_bottom=False)
    body.faces.index_update()
    def bfn(f, l):
        c = f.calc_center_median()
        if 4.4 < c.z < 8.6:
            row = int((c.z - 4.4) / 0.42)
            ang = math.atan2(c.y, c.x)
            col_i = int((ang + math.pi) / (2 * math.pi) * 24 + 0.5)
            if (row + col_i) % 2 == 0:
                return mix(pal("ahsap", 2), pal("dag", 2), 0.5)     # yuva deliği
        t = smooth01(c.z / 9.4)
        return mix(pal("kil", 0), pal("siva", 0), 0.4 + 0.5 * t)
    paint_by(body, bfn)
    parts.append(body)
    for zr in (4.2, 6.55):                                       # tünek saçakları
        r0 = 2.42 - 0.042 * (zr - 0.4)
        rg = bm_lathe([(r0 - 0.05, zr - 0.08), (r0 + 0.22, zr - 0.04), (r0 + 0.24, zr + 0.06), (r0 - 0.05, zr + 0.1)], sides=24,
                      close_top=False, close_bottom=False)
        grad(rg, "kil", 1, 0)
        parts.append(rg)
    rt = 2.42 - 0.042 * 9.0
    cor = bm_lathe([(rt - 0.05, 9.35), (rt + 0.3, 9.4), (rt + 0.34, 9.7), (rt + 0.05, 9.8)], sides=24, close_top=False, close_bottom=False)
    grad(cor, "kil", 2, 0)
    parts.append(cor)
    dome = bm_lathe(chaikin([(rt + 0.05, 9.8), (rt, 10.5), (rt * 0.78, 11.25), (rt * 0.42, 11.85), (0.0, 12.05)], 1), sides=20)
    paint_by(dome, lambda f, l: mix(pal("siva", 1), pal("siva", 0), smooth01((l.vert.co.z - 9.8) / 2.2)))
    parts.append(dome)
    tur = bm_cyl(0.55, 0.5, 0.9, sides=10)
    transform_bm(tur, Matrix.Translation((0, 0, 11.75)))
    grad(tur, "siva", 1, 0)
    parts.append(tur)
    for k in range(5):                                            # taret giriş delikleri
        a = 2 * math.pi * k / 5
        hole = box(0.18, 0.08, 0.3, x=math.cos(a) * 0.52, y=math.sin(a) * 0.52, z=12.05, yaw=a + math.pi / 2)
        flat(hole, mix(pal("ahsap", 2), pal("dag", 2), 0.5))
        parts.append(hole)
    td = bm_lathe(chaikin([(0.62, 12.65), (0.55, 12.9), (0.3, 13.15), (0.0, 13.25)], 1), sides=10)
    grad(td, "bakir", 2, 0)
    parts.append(td)
    fin = bm_lathe([(0.0, 13.2), (0.06, 13.25), (0.09, 13.45), (0.0, 13.75)], sides=6)
    grad(fin, "bakir", 1, 0)
    parts.append(fin)
    d = door_bm(0.9, 1.35, 0.35, base=0.4)
    transform_bm(d, Matrix.Translation((0, -2.38, 0)))
    parts.append(d)
    return merge(parts)

def guvercin_bm(seed):
    rnd = random.Random(seed)
    body = bm_lathe([(0.0, -0.13), (0.05, -0.1), (0.075, -0.02), (0.06, 0.06), (0.0, 0.11)], sides=6)
    transform_bm(body, Matrix.Rotation(math.pi / 2, 4, "X"))
    head = bm_lathe([(0.0, 0.0), (0.035, 0.01), (0.03, 0.05), (0.0, 0.06)], sides=5)
    transform_bm(head, Matrix.Translation((0, -0.1, 0.06)))
    tail = box(0.08, 0.1, 0.02, y=0.15, z=0.0, rx=0.3, origin="center")
    for b_, c in ((body, mix(pal("kaya", 1), pal("siva", 0), 0.5)), (head, pal("kaya", 2)), (tail, pal("kaya", 2))):
        grad(b_, "kaya", 1, 0) if b_ is body else flat(b_, c)
    g = merge([body, head, tail])
    transform_bm(g, Matrix.Translation((0, 0, 0.07)))
    return g

# ------------------------------------------------------------------ LONCA BİNASI
def lonca_bm(seed=911, shutters_open=None):
    """shutters_open None → bina; False/True → yalnız kepenk grubu döner."""
    W, D = 10.0, 6.6
    H1, H2 = 2.8, 2.5
    fz = 0.45
    up_z = fz + H1 + 0.3
    win_x = [-3.2, 0.0, 3.2]
    if shutters_open is not None:
        sh = []
        for x in win_x:
            s = shutter_pair(1.0, 1.05, open_=shutters_open)
            transform_bm(s, Matrix.Translation((x, -(D / 2 + 0.25) - 0.04, up_z + 0.7)))
            sh.append(s)
        return merge(sh)
    rnd = random.Random(seed)
    parts = []
    fl = box(W + 0.4, D + 0.4, fz + 0.45, z=-0.45, bevel=0.06)
    jitter(fl, 0.03, seed=seed)
    grad(fl, "kaya", 2, 1)
    parts.append(fl)
    tiles = box(W, D, 0.02, z=fz)
    paint_by(tiles, lambda f, l: tone(mix(pal("kaya", 1), pal("kaya", 0), 0.5), 1.0))
    parts.append(tiles)
    # arka yarı kapalı, ön yarı revak (taş kaide + ahşap dikme)
    back = box(W, D / 2, H1, y=D / 4, z=fz)
    paint_by(back, lambda f, l: mix(pal("siva", 1), pal("siva", 0), smooth01((l.vert.co.z - fz) / H1)))
    parts.append(back)
    d = door_bm(1.3, 1.6, 0.45, base=fz)
    transform_bm(d, Matrix.Translation((0, 0.0, 0)))
    parts.append(d)
    for x in (-W / 2 + 0.25, -W / 4, W / 4, W / 2 - 0.25):
        for y in (-D / 2 + 0.25,):
            pl = box(0.5, 0.5, 0.45, x=x, y=y, z=fz, bevel=0.04)
            grad(pl, "kaya", 1, 0)
            parts.append(pl)
            pst = box(0.26, 0.26, H1 - 0.45, x=x, y=y, z=fz + 0.45, bevel=0.02)
            grad(pst, "ahsap", 2, 1)
            parts.append(pst)
            for s in (-1, 1):
                if abs(x + s * 0.6) < W / 2:
                    br = box(0.85, 0.16, 0.16, origin="center")
                    transform_bm(br, M(x + s * 0.32, y, fz + H1 - 0.45, ry=s * math.radians(40)))
                    flat(br, pal("ahsap", 2))
                    parts.append(br)
    beam = box(W + 0.7, D + 0.7, 0.3, z=fz + H1, bevel=0.03)
    grad(beam, "ahsap", 2, 1)
    parts.append(beam)
    up = box(W + 0.5, D + 0.5, H2, z=up_z, bevel=0.03)
    paint_by(up, lambda f, l: mix(pal("siva", 1), pal("siva", 0), smooth01((l.vert.co.z - up_z) / H2 * 1.3)))
    parts.append(up)
    parts.append(timber_frame(W + 0.5, D + 0.5, up_z, H2, seed))
    for x in win_x:
        wb = window_bm(1.0, 1.05)
        transform_bm(wb, Matrix.Translation((x, -(D + 0.5) / 2 - 0.02, up_z + 0.7)))
        parts.append(wb)
    for s in (-1, 1):
        wb = window_bm(0.9, 1.0)
        transform_bm(wb, Matrix.Translation((s * ((W + 0.5) / 2 + 0.02), 0, up_z + 0.7)) @ Matrix.Rotation(s * math.pi / 2, 4, "Z"))
        parts.append(wb)
    roof, ridge_z, rise, pitch = roof_bm(W + 0.5, D + 0.5, up_z + H2, seed, ov_x=0.7, ov_e=0.8, pitch_deg=44, rows=5)
    parts.append(roof)
    parts.append(gable_bm(W + 0.5, D + 0.5, up_z + H2, rise, True))
    # mahyada küçük kule-saat yerine çan kulesi (simge, uzaktan okunur)
    bz = ridge_z - 0.2
    bt = box(1.1, 1.1, 1.6, z=bz, bevel=0.03)
    grad(bt, "ahsap", 2, 0)
    parts.append(bt)
    bo = box(1.14, 1.14, 0.7, z=bz + 0.6)
    flat(bo, mix(pal("ahsap", 2), pal("dag", 2), 0.5))
    parts.append(bo)
    br_, _, _, _ = roof_bm(1.3, 1.3, bz + 1.6, seed + 1, ov_x=0.2, ov_e=0.25, pitch_deg=55, rows=2)
    parts.append(br_)
    bell = bm_lathe([(0.0, 0.0), (0.24, 0.0), (0.2, 0.12), (0.13, 0.38), (0.0, 0.42)], sides=10)
    transform_bm(bell, Matrix.Translation((0, 0, bz + 0.7)))
    grad(bell, "bakir", 2, 0)
    parts.append(bell)
    anchors = {"front_y": -(D + 0.5) / 2, "banner_z": up_z - 0.05, "up_z": up_z}
    return merge(parts), anchors

def flama_bm(x, y, z, h=1.6, w=0.55, seed=0):
    """Asılı flama: V kesik uçlu kumaş, şerit; çubuk."""
    poly = [(-w / 2, 0), (-w / 2, -h), (0, -h + 0.28), (w / 2, -h), (w / 2, 0)]
    cl = bm_prism(poly, -0.02, 0.02)
    def fn(f, l):
        u = l.vert.co.x
        return pal("altin", 0) if abs(u) < w * 0.16 else pal("cati", 1)
    paint_by(cl, fn)
    rod = tube_pts([(-w / 2 - 0.08, 0, 0.02), (w / 2 + 0.08, 0, 0.02)], 0.03, sides=5)
    flat(rod, pal("ahsap", 2))
    m_ = merge([cl, rod])
    transform_bm(m_, Matrix.Translation((x, y, z)))
    return m_

# ------------------------------------------------------------------ PANO, SAKSI, BAYRAK DİZİSİ, SANCAK
def pano_bm():
    parts = []
    for s in (-1, 1):
        p = box(0.14, 0.14, 2.2, x=s * 0.85, z=-0.3)
        grad(p, "ahsap", 2, 1)
        parts.append(p)
    bd = box(1.8, 0.1, 1.05, z=0.9, bevel=0.02)
    grad(bd, "ahsap", 1, 0)
    parts.append(bd)
    rf = bm_prism([(-1.15, 0), (1.15, 0), (0, 0.32)], -0.25, 0.25)
    transform_bm(rf, Matrix.Translation((0, 0, 2.0)))
    grad(rf, "cati", 2, 1)
    parts.append(rf)
    return merge(parts)

def ilanlar_bm(seed):
    rnd = random.Random(seed)
    parts = []
    for i in range(6):
        pw, ph = rnd.uniform(0.3, 0.42), rnd.uniform(0.35, 0.48)
        pp = box(pw, 0.012, ph, x=-0.6 + (i % 3) * 0.6 + rnd.uniform(-0.05, 0.05), y=-0.06,
                 z=1.0 + (i // 3) * 0.48 + rnd.uniform(-0.03, 0.03), ry=rnd.uniform(-0.12, 0.12))
        flat(pp, mix(pal("siva", 0), pal("altin", 0), 0.2 * rnd.random()))
        parts.append(pp)
        pin = box(0.04, 0.02, 0.04, x=-0.6 + (i % 3) * 0.6, y=-0.075, z=1.0 + (i // 3) * 0.48 + ph - 0.06)
        flat(pin, pal("cati", 1))
        parts.append(pin)
    return merge(parts)

def saksi_bm(seed):
    rnd = random.Random(seed)
    parts = []
    bx = box(0.95, 0.24, 0.2, y=-0.1, z=-0.15, bevel=0.02)
    grad(bx, "ahsap", 2, 1)
    parts.append(bx)
    for i in range(6):
        c = Vector((-0.38 + i * 0.15 + rnd.uniform(-0.03, 0.03), -0.1 + rnd.uniform(-0.04, 0.04), 0.08 + rnd.uniform(0, 0.06)))
        lf = _blob(c, 0.09, sub=1, squash=0.8, seed=seed + i)
        flat(lf, pal("yaprak", 1))
        parts.append(lf)
        if i % 2 == 0 or rnd.random() < 0.4:
            fl = _blob(c + Vector((0, -0.03, 0.08)), 0.055, sub=1, seed=seed + 20 + i)
            flat(fl, pal("cati", 0) if rnd.random() < 0.5 else pal("altin", 0))
            parts.append(fl)
    return merge(parts)

def bayrak_dizisi_bm(p0, p1, seed, sag=0.55, h=4.2):
    """İki direk arasında bayrak dizisi (After). p0/p1 dünya XY; dönen mesh dünya koordinatında (z dahil)."""
    rnd = random.Random(seed)
    parts = []
    tops = []
    for p in (p0, p1):
        z = height(p.x, p.y)
        po = box(0.16, 0.16, h + 0.4, x=p.x, y=p.y, z=z - 0.4)
        grad(po, "ahsap", 2, 1)
        parts.append(po)
        tops.append(Vector((p.x, p.y, z + h)))
    n = 14
    pts = []
    for i in range(n + 1):
        t = i / n
        q = tops[0].lerp(tops[1], t) - Vector((0, 0, sag * 4 * t * (1 - t)))
        pts.append(q)
    rope = tube_pts(pts, 0.02, sides=4)
    flat(rope, mix(pal("kil", 0), pal("altin", 2), 0.3))
    parts.append(rope)
    for i in range(1, n):
        a, b = pts[i] , pts[i + 1] if i + 1 <= n else pts[i]
        mid = a.lerp(b, 0.5)
        dirv = (b - a)
        tri = bmesh.new()
        v0 = tri.verts.new(a - dirv * 0.05)
        v1 = tri.verts.new(b + dirv * -0.25)
        v2 = tri.verts.new(mid + Vector((0, 0, -0.38)) - dirv * 0.12)
        tri.faces.new((v0, v1, v2))
        w0, w1, w2 = (tri.verts.new(v.co.copy()) for v in (v0, v1, v2))
        tri.faces.new((w2, w1, w0))                      # iki yüzlü bayrak
        flat(tri, pal("cati", 1) if i % 2 else pal("altin", 0))
        parts.append(tri)
    return merge(parts)

def sancak_bm():
    parts = []
    base = box(0.7, 0.7, 0.5, z=-0.2, bevel=0.05)
    grad(base, "kaya", 2, 0)
    parts.append(base)
    pole = bm_cyl(0.08, 0.06, 7.0, sides=8)
    transform_bm(pole, Matrix.Translation((0, 0, 0.3)))
    grad(pole, "ahsap", 2, 0)
    parts.append(pole)
    knob = _blob(Vector((0, 0, 7.35)), 0.12, sub=1)
    flat(knob, pal("bakir", 0))
    parts.append(knob)
    fl = bmesh.new()
    pts = []
    for i in range(6):
        u = i / 5
        pts.append(((u * 2.2), 6.9 - 0.08 * math.sin(u * 3), -0.12 * math.sin(u * 4.0)))
    for i in range(6):
        u = i / 5
        pts.append((u * 2.2, 5.6 - 0.15 * u - 0.08 * math.sin(u * 3), -0.12 * math.sin(u * 4.0)))
    for side in (0, 1):                                   # iki yüzlü: ayrı köşeler
        vs = [fl.verts.new((x + 0.06, w, z)) for (x, z, w) in pts]
        for i in range(5):
            q = (vs[i], vs[i + 1], vs[6 + i + 1], vs[6 + i])
            fl.faces.new(q if side == 0 else tuple(reversed(q)))
    paint_by(fl, lambda f, l: pal("altin", 0) if 6.0 < l.vert.co.z < 6.45 and False else (pal("cati", 1) if l.vert.co.z > 6.15 or l.vert.co.z < 5.85 else pal("altin", 0)))
    parts.append(fl)
    return merge(parts)

# ------------------------------------------------------------------ KİLİTLİ KAPI + PUS
def kilitli_kapi_bm(seed):
    rnd = random.Random(seed)
    parts = []
    for s in (-1, 1):
        p = box(0.85, 0.85, 3.1, x=s * 2.0, z=-0.3, bevel=0.06)
        jitter(p, 0.03, seed=seed + s)
        grad(p, "kaya", 2, 0)
        parts.append(p)
        c = bm_cyl(0.62, 0.05, 0.55, sides=4)
        transform_bm(c, M(s * 2.0, 0, 2.8, yaw=math.pi / 4))
        grad(c, "kaya", 1, 0)
        parts.append(c)
    for s in (-1, 1):
        lf = box(1.55, 0.14, 2.45, x=s * 0.79, z=0.0, bevel=0.02)
        paint_by(lf, lambda f, l: tone(mix(pal("ahsap", 2), pal("ahsap", 1), 0.3 + 0.4 * (int((l.vert.co.x + 5) / 0.26) % 2)), 1.0))
        parts.append(lf)
        for zz in (0.35, 1.25, 2.1):
            b = box(1.45, 0.04, 0.12, x=s * 0.79, y=-0.09, z=zz)
            flat(b, demir(0.8))
            parts.append(b)
    bar = box(1.9, 0.12, 0.16, y=-0.15, z=1.2)
    flat(bar, pal("ahsap", 2))
    parts.append(bar)
    lock = box(0.18, 0.1, 0.24, y=-0.24, z=1.1, bevel=0.02)
    flat(lock, demir(0.9))
    parts.append(lock)
    lin = box(4.9, 0.5, 0.35, z=2.5, bevel=0.04)
    grad(lin, "ahsap", 2, 1)
    parts.append(lin)
    rf, _, _, _ = roof_bm(4.8, 0.7, 2.85, seed, ov_x=0.25, ov_e=0.35, pitch_deg=40, rows=2)
    parts.append(rf)
    # iki yana örme çit (8 m)
    for s in (-1, 1):
        pts = []
        for i in range(6):
            x = s * (2.5 + i * 1.5)
            ps = box(0.12, 0.12, 1.35, x=x, z=-0.25, yaw=rnd.uniform(0, 0.6))
            grad(ps, "ahsap", 2, 1)
            parts.append(ps)
        for zz in (0.35, 0.75):
            rl = tube_pts([(s * 2.45, 0, zz), (s * 5.5, 0, zz + 0.03), (s * 10.0, 0, zz)], 0.05, sides=4)
            grad(rl, "ahsap", 2, 1)
            parts.append(rl)
    return merge(parts)

def pus_bm(seed, n=7, spread=11.0, depth=9.0):
    rnd = random.Random(seed)
    parts = []
    for i in range(n):
        c = Vector((rnd.uniform(-spread, spread), rnd.uniform(2.0, depth), rnd.uniform(0.5, 2.5)))
        b = _blob(c, rnd.uniform(3.0, 4.8), sub=2, squash=0.55, seed=seed + i, jit=0.08)
        paint_by(b, lambda f, l: mix(pal("gok", 0), pal("su", 0), 0.3 + 0.3 * smooth01(l.vert.co.z / 4)))
        parts.append(b)
    return merge(parts)

# ================================================================== KURULUM
DENK_EVLER = [  # (x, y, yaw, W, D, H, seed) — aralarında 3-5 m bahçe/geçit
    (42.5, 1.4, 0.06, 5.6, 4.6, 3.0, 1001),
    (61.2, 3.6, 0.32, 5.0, 4.4, 2.9, 1003),
    (41.5, -14.0, math.pi + 0.05, 6.0, 4.8, 3.2, 1004),
    (50.5, -14.3, math.pi - 0.06, 5.2, 4.4, 2.8, 1005),
    (59.0, -16.5, math.pi + 0.18, 5.4, 4.6, 3.1, 1006),
]
FIRIN = (50.0, 1.8, -0.04)
FENERLER = [(39.5, -4.2), (44.0, -9.6), (48.5, -3.6), (53.0, -9.0), (57.2, -2.6)]
TARLALAR = [(75.5, 6.0, 0.35, 8.5, 7.0, "bugday", 2001), (76.5, 16.0, 0.2, 8.0, 6.5, "sebze", 2002),
            (60.5, 11.0, 0.5, 7.5, 6.0, "bugday", 2003)]
CESME = (6.6, -2.8, math.radians(-52))
CINAR = (-5.5, -4.2)

TEPK_EVLER = [
    (-37.5, 3.6, 0.05, 5.4, 4.6, 3.0, 1101),
    (-36.5, -11.6, math.pi + 0.08, 5.8, 4.6, 3.1, 1102),
    (-56.0, -9.2, math.pi + 0.1, 5.2, 4.4, 2.9, 1103),
    (-44.8, 10.6, -0.2, 5.0, 4.2, 2.8, 1104),
]
LONCA = (-48.0, -10.6, math.pi)
KULE = (-55.0, 12.5, math.radians(27))
PANO = (-43.5, -5.6, math.pi)

KAPILAR = [("Hesaplamalar", Vector((76.0, -32.0)), Vector((4.0, -8.0))),
           ("Tepkimeler", Vector((-66.0, 24.0)), Vector((-4.0, 9.0)))]

def _road_yaw(p, q):
    d = (q - p).normalized()
    return math.atan2(d.y, d.x)

def build_yapilar():
    col = collection(YAPI_COL)
    clear_collection(YAPI_COL)
    for nm in ("Denk_Once", "Denk_Sonra", "Tepk_Once", "Tepk_Sonra"):
        o = bpy.data.objects.get(nm)
        if o:
            bpy.data.objects.remove(o, do_unlink=True)
    for me in list(bpy.data.meshes):
        if me.users == 0:
            bpy.data.meshes.remove(me)
    KD = bpy.data.objects["Koy_Denk"]
    KT = bpy.data.objects["Koy_Tepk"]
    made = []

    # ---------------- MEYDAN (çeşme Denk P1'e bağlı)
    cb, can = cesme_bm()
    x, y, yaw = CESME
    z = ground_min(x, y, 1.3, 1.3, yaw) + 0.05
    made.append(put(cb, "SM_Meydan_Cesme", x, y, yaw, z=z))
    made.append(put(cesme_su_bm(can), "Denk_P1_Cesme_Su", x, y, yaw, z=z, parent=KD, mat=m_su(), sharp=0))
    made.append(put(kuru_yaprak_bm(can, 5), "Denk_Once_Cesme_Yaprak", x, y, yaw, z=z, parent=KD))
    cin = make_tree("SM_Meydan_Cinar", 777, height=12.0, crown=5.4, trunk=2.0, col=col)
    cin.location = (CINAR[0], CINAR[1], height(*CINAR) - 0.1)
    made.append(cin)

    # ---------------- DENKLEŞTİRME
    for i, (x, y, yaw, W, D, H, sd) in enumerate(DENK_EVLER):
        hb, an = house_bm(W, D, H, sd, timber=(i % 2 == 0))
        made.append(put(hb, "SM_Denk_Ev_%d" % (i + 1), x, y, yaw, z=ground_min(x, y, W / 2, D / 2, yaw), parent=KD))
    x, y, yaw = FIRIN
    fb, fan = firin_bm()
    fz = ground_min(x, y, 4.5, 2.4, yaw)
    made.append(put(fb, "SM_Denk_Firin", x, y, yaw, z=fz, parent=KD))
    made.append(put(kor_bm(fan["mouth"]), "Denk_P3_Firin_Kor", x, y, yaw, z=fz, parent=KD, mat=m_fener(), sharp=0))
    made.append(put(duman_bm(fan["chimney"], 31), "Denk_P3_Firin_Duman", x, y, yaw, z=fz, parent=KD, mat=m_duman(), sharp=0))
    # fenerler: direkler + isli camlar sabit; yanan camlar P2
    stat, lit = [], []
    for i, (fx, fy) in enumerate(FENERLER):
        d, _ = road_info(Vector((fx, fy)))
        rp = min(resample(ROADS[1][1], 1.0), key=lambda q: (q - Vector((fx, fy))).length)   # Yol_Dogu
        to_road = (rp - Vector((fx, fy))).normalized()
        yaw = math.atan2(-to_road.x, to_road.y)       # kol (yerel -Y) yola uzansın
        yaw = math.atan2(to_road.y, to_road.x) + math.pi / 2
        pb, at = fener_direk_bm(300 + i)
        pb = merge([pb, fener_cam_bm(at, False)])
        gb = fener_cam_bm(at, True)
        z = height(fx, fy)
        for b_ in (pb, gb):
            transform_bm(b_, M(fx - DENK.x, fy - DENK.y, z - KD.location.z, yaw))
        stat.append(pb)
        lit.append(gb)
    made.append(put(merge(stat), "SM_Denk_Fener_Direkleri", DENK.x, DENK.y, 0, z=KD.location.z, parent=KD))
    made.append(put(merge(lit), "Denk_P2_Fener_Isik", DENK.x, DENK.y, 0, z=KD.location.z, parent=KD, mat=m_fener(), sharp=0))
    # köprüler
    p_a, p_b = Vector((KOPRU.x - 3, KOPRU.y - 0.3)), Vector((KOPRU.x + 3, KOPRU.y + 0.3))
    byaw = _road_yaw(Vector((26, -8.5)), Vector((38, -7.5)))
    bz = (height(KOPRU.x - 7 * math.cos(byaw), KOPRU.y - 7 * math.sin(byaw)) +
          height(KOPRU.x + 7 * math.cos(byaw), KOPRU.y + 7 * math.sin(byaw))) / 2
    made.append(put(kopru_tas_bm(), "Denk_P4_Kopru_Tas", KOPRU.x, KOPRU.y, byaw, z=bz, parent=KD))
    made.append(put(kopru_yikik_bm(), "Denk_Once_Kopru_Yikik", KOPRU.x, KOPRU.y, byaw, z=bz, parent=KD))
    gp = min(STREAM, key=lambda q: abs(q.y + 2.0))
    i_ = STREAM.index(gp)
    sd_ = (STREAM[min(i_ + 1, len(STREAM) - 1)] - STREAM[max(i_ - 1, 0)]).normalized()
    gyaw = math.atan2(sd_.y, sd_.x) + math.pi / 2
    gz = (height(gp.x - 5.5 * math.cos(gyaw), gp.y - 5.5 * math.sin(gyaw)) +
          height(gp.x + 5.5 * math.cos(gyaw), gp.y + 5.5 * math.sin(gyaw))) / 2
    made.append(put(kopru_gecici_bm(), "SM_Denk_Kopru_Gecici", gp.x, gp.y, gyaw, z=gz, parent=KD))
    # tarlalar
    soil, crops = [], []
    for (tx, ty, tyaw, w, d, kind, sd) in TARLALAR:
        b0 = tarla_bm(tx, ty, w, d, tyaw, None, sd)
        b1 = tarla_bm(tx, ty, w, d, tyaw, kind, sd)
        for b_ in (b0, b1):
            transform_bm(b_, Matrix.Translation((tx - DENK.x, ty - DENK.y, height(tx, ty) - KD.location.z)))
        soil.append(b0)
        crops.append(b1)
    made.append(put(merge(soil), "SM_Denk_Tarlalar", DENK.x, DENK.y, 0, z=KD.location.z, parent=KD))
    made.append(put(merge(crops), "Denk_P5_Ekin", DENK.x, DENK.y, 0, z=KD.location.z, parent=KD))
    # köy bitince: yol üstünde bayrak dizileri
    bd = merge([bayrak_dizisi_bm(Vector((46.0, -3.4)), Vector((46.4, -10.4)), 61),
                bayrak_dizisi_bm(Vector((54.5, -2.6)), Vector((55.0, -9.8)), 62)])
    transform_bm(bd, Matrix.Translation((-DENK.x, -DENK.y, -KD.location.z)))
    made.append(put(bd, "Denk_Sonra_Bayraklar", DENK.x, DENK.y, 0, z=KD.location.z, parent=KD, sharp=0))

    # ---------------- TEPKİME TÜRLERİ
    pots = []
    for i, (x, y, yaw, W, D, H, sd) in enumerate(TEPK_EVLER):
        hb, an = house_bm(W, D, H, sd, timber=(i % 2 == 1))
        z = ground_min(x, y, W / 2, D / 2, yaw)
        made.append(put(hb, "SM_Tepk_Ev_%d" % (i + 1), x, y, yaw, z=z, parent=KT))
        for wp in an["windows"][:1]:
            sb = saksi_bm(500 + i)
            transform_bm(sb, Matrix.Translation(wp + Vector((0, -0.12, -0.05))))
            transform_bm(sb, M(x - TEPK.x, y - TEPK.y, z - KT.location.z, yaw))
            pots.append(sb)
    made.append(put(merge(pots), "Tepk_P3_Saksilar", TEPK.x, TEPK.y, 0, z=KT.location.z, parent=KT))
    x, y, yaw = LONCA
    lb, lan = lonca_bm()
    lz = ground_min(x, y, 5.4, 3.6, yaw)
    made.append(put(lb, "SM_Tepk_Lonca", x, y, yaw, z=lz, parent=KT))
    made.append(put(lonca_bm(shutters_open=False), "Tepk_Once_Kepenk", x, y, yaw, z=lz, parent=KT))
    made.append(put(lonca_bm(shutters_open=True), "Tepk_Sonra_Kepenk_Acik", x, y, yaw, z=lz, parent=KT))
    fl = merge([flama_bm(fx_, lan["front_y"] - 0.3, lan["banner_z"], seed=k) for k, fx_ in enumerate((-4.6, -1.6, 1.6, 4.6))])
    made.append(put(fl, "Tepk_P2_Flamalar", x, y, yaw, z=lz, parent=KT, sharp=0))
    sn = sancak_bm()
    made.append(put(sn, "Tepk_P5_Sancak", x + 3.0, y + 5.6, yaw, parent=KT))
    x, y, yaw = KULE
    made.append(put(kule_bm(), "SM_Tepk_Guvercin_Kulesi", x, y, yaw, z=ground_min(x, y, 2.6, 2.6, 0), parent=KT))
    kz = ground_min(x, y, 2.6, 2.6, 0)
    gv = []
    rnd = random.Random(88)
    rt = 2.42 - 0.042 * 9.0
    for k in range(9):
        a = rnd.uniform(0, 2 * math.pi)
        zz, rr = (9.8, rt + 0.2) if k < 6 else (6.65, 2.42 - 0.042 * 6.25 + 0.12)
        g_ = guvercin_bm(k)
        transform_bm(g_, M(math.cos(a) * rr, math.sin(a) * rr, zz, yaw=a + math.pi / 2 + rnd.uniform(-0.5, 0.5)))
        gv.append(g_)
    made.append(put(merge(gv), "Tepk_P1_Guvercinler", x, y, 0, z=kz, parent=KT))
    x, y, yaw = PANO
    made.append(put(pano_bm(), "SM_Tepk_Pano", x, y, yaw, parent=KT))
    made.append(put(ilanlar_bm(9), "Tepk_P4_Ilanlar", x, y, yaw, parent=KT))
    bd = merge([bayrak_dizisi_bm(Vector((-40.0, 0.4)), Vector((-40.4, -6.6)), 71),
                bayrak_dizisi_bm(Vector((-51.0, 1.4)), Vector((-51.2, -5.6)), 72)])
    transform_bm(bd, Matrix.Translation((-TEPK.x, -TEPK.y, -KT.location.z)))
    made.append(put(bd, "Tepk_Sonra_Bayraklar", TEPK.x, TEPK.y, 0, z=KT.location.z, parent=KT, sharp=0))

    # ---------------- KİLİTLİ YOL AĞIZLARI
    for nm, p, d in KAPILAR:
        yaw = math.atan2(d.y, d.x) - math.pi / 2          # kapı ön yüzü oyuncuya (içeri) bakar
        dn = d.normalized()
        z = height(p.x, p.y)
        made.append(put(kilitli_kapi_bm(hash(nm) % 1000), "SM_Kapi_" + nm, p.x, p.y, yaw, z=z))
        pb = pus_bm(len(nm) * 17)
        transform_bm(pb, Matrix.Rotation(yaw, 4, "Z"))            # pus kapının dışında
        ob = to_object(pb, "Pus_" + nm, col, loc=(p.x, p.y, z), sharp_angle=0, mat=m_bulut())
        for v in ob.data.vertices:
            pass
        made.append(ob)
        lz_ = empty("LockedZone_" + nm, col, loc=(p.x + dn.x * 2.0, p.y + dn.y * 2.0, z + 1.5), rot=(0, 0, math.degrees(yaw)),
                    kind="CUBE", size=1.0)
        lz_.scale = (6.0, 1.0, 2.5)

    # doğa yerlerinden yapıların altına düşenleri sil
    clear_yerler_under(made)
    tot = sum(tri_count([o]) for o in made if o.type == "MESH")
    print("Yapılar:", len(made), "obje,", tot, "üçgen")
    return made

def clear_yerler_under(objs):
    boxes = []
    for o in objs:
        if o.type != "MESH" or o.name.startswith("Pus_"):
            continue
        bb = [o.matrix_world @ Vector(c) for c in o.bound_box]
        xs = [v.x for v in bb]; ys = [v.y for v in bb]
        if (max(xs) - min(xs)) * (max(ys) - min(ys)) > 400:      # köy çapındaki gruplar (fener, tarla) hariç
            continue
        boxes.append((min(xs) - 1.5, max(xs) + 1.5, min(ys) - 1.5, max(ys) + 1.5))
    for (tx, ty, tyaw, w, d, kind, sd) in TARLALAR:
        r = max(w, d) / 2 + 2.0
        boxes.append((tx - r, tx + r, ty - r, ty + r))
    yc = bpy.data.collections.get(YER_COL)
    if not yc:
        return
    n = 0
    for y in list(yc.objects):
        p = y.location
        if any(a <= p.x <= b and c <= p.y <= d for (a, b, c, d) in boxes):
            bpy.data.objects.remove(y, do_unlink=True)
            n += 1
    print("Yapı altındaki doğa yerleri silindi:", n)
