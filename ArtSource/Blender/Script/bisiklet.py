# Bisiklet: öğrencinin köyler arası aracı (kasaba arabacısı yapımı "kemik sarsan" velosiped).
# Referans: ArtSource/Referans/bisiklet.md. Kurallar: 09-tema-ve-model-kurallari.md.
# Yarışma süresince yazıldı (0'dan üret). Blender 5.x.
#
# Kullanım (Blender içinden):
#   p = r"<repo>/ArtSource/Blender/Script/bisiklet.py"
#   exec(compile(open(p, encoding="utf-8").read(), p, "exec"), globals())
#   build_bisiklet()            # sahneyi kurar
#   export_bisiklet()           # Assets/_Project/Art/Bisiklet/Bisiklet.fbx
#
# Yön: bisikletin önü Blender -Y (Unity +Z). Zemin z=0. Teker ekseni X.

import os
import math
import random
import bmesh
import bpy
from mathutils import Vector, Matrix

_HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else \
    r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\ArtSource\Blender\Script"
_lib = os.path.join(_HERE, "ba_lib.py")
exec(compile(open(_lib, encoding="utf-8").read(), _lib, "exec"), globals())

COL = "Bisiklet"
EXPORT_PATH = r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\Assets\_Project\Art\Bisiklet\Bisiklet.fbx"

# ------------------------------------------------------------------ ölçüler (bisiklet.md §4)
R_ON, R_ARKA = 0.39, 0.30                     # teker yarıçapı (lastik dışı)
HUB_ON = Vector((0.0, -0.53, R_ON))
HUB_ARKA = Vector((0.0, 0.52, R_ARKA))
RAKE = math.radians(12)                        # direksiyon ekseni yatıklığı (üst arkaya)
STEER = Vector((0.0, math.sin(RAKE), math.cos(RAKE)))
CRANK = 0.13                                   # krank boyu
CRANK_X = 0.105                                # krankın göbekten yana uzaklığı
# Sele: sürücü (1,66 m öğrenci) ön tekerdeki pedala yetişsin diye öne ve omurgaya yakın (13.11, ogrenci_govde.md).
# Önceki: merkez y 0,13 / üst 0,80 → en uzak pedal 0,95 m, bacak 0,8 m'de kalıyordu.
SADDLE_Y = -0.08                               # sele merkezi (Blender y; + arka)
SADDLE_TOP = 0.82
SPINE_MID_Z = 0.48                             # omurganın sele altındaki çukuru (önceden 0,62)
BAR_Z = 1.00

# ------------------------------------------------------------------ renkler (yalnız paletten / türetilmiş)
def c_iron(light=0.5):
    return mix(demir(0.55), demir(0.95), light)

def c_leather(t=0.5):
    # deri: ahşap gölge + çatı gölge (aynı sıcak ailenin koyusu)
    return mix(mix(pal("ahsap", 2), pal("cati", 2), 0.25), pal("ahsap", 1), t * 0.6)

def c_wicker(t=0.5):
    return mix(mix(pal("altin", 2), pal("ahsap", 0), 0.55), pal("altin", 0), t * 0.35)

def c_wood(t=0.5, alt=False):
    # iki ahşap tonu (kiraz + yumuşak ağaç, bisiklet.md #7): alt=True biraz daha kızıl
    a, b = pal("ahsap", 2), pal("ahsap", 0)
    c = mix(a, b, t)
    return mix(c, pal("cati", 2), 0.12) if alt else c

def c_copper(t=0.5):
    return mix(pal("bakir", 2), pal("bakir", 0), t)

def c_accent(t=0.5):
    return mix(pal("cati", 2), pal("cati", 1), t)

def grad_paint(bm, fn_color, axis=2, noise=0.04, seed=0):
    """fn_color(t) → renk; t alttan üste 0..1 (09 §3 koyudan açığa)."""
    rnd = random.Random(seed)
    ks = {}
    vs = [v.co[axis] for v in bm.verts]
    lo, hi = min(vs), max(vs)
    span = max(hi - lo, 1e-6)
    def fn(f, l):
        k = ks.setdefault(f.index, 1.0 + rnd.uniform(-noise, noise))
        c = fn_color((l.vert.co[axis] - lo) / span)
        return (min(c[0] * k, 1), min(c[1] * k, 1), min(c[2] * k, 1), 1.0)
    bm.faces.index_update()
    paint_by(bm, fn)
    return bm

# ------------------------------------------------------------------ geometri yardımcıları
def _frames(points):
    """Polyline boyunca paralel taşımalı çerçeveler (t, n, b)."""
    pts = [Vector(p) for p in points]
    tans = []
    for i in range(len(pts)):
        a = pts[max(i - 1, 0)]
        b = pts[min(i + 1, len(pts) - 1)]
        tans.append((b - a).normalized())
    up = Vector((1, 0, 0)) if abs(tans[0].x) < 0.9 else Vector((0, 0, 1))
    n = tans[0].cross(up).normalized()
    out = []
    for t in tans:
        n = (n - t * n.dot(t)).normalized()
        out.append((t, n, t.cross(n).normalized()))
    return pts, out

def tube(points, radii, sides=8, sx=1.0, sy=1.0, cap=True):
    """Kesiti elips olan boru. radii: her nokta için yarıçap (ya da tek sayı).
    sx: kesitin 'n' yönündeki, sy: 'b' yönündeki ölçeği."""
    pts, fr = _frames(points)
    if not isinstance(radii, (list, tuple)):
        radii = [radii] * len(pts)
    secs = []
    for p, (t, n, b), r in zip(pts, fr, radii):
        sec = []
        for i in range(sides):
            a = 2 * math.pi * (i + 0.5) / sides
            sec.append(p + n * (math.cos(a) * r * sx) + b * (math.sin(a) * r * sy))
        secs.append(sec)
    return bm_sweep(secs, cap=cap)

def chaikin(prof, it=2, keep_ends=True):
    """Profil köşe kesme (esya_tasinir.py ile aynı fikir; bu script bağımsız çalışsın diye burada)."""
    pts = list(prof)
    for _ in range(it):
        out = [pts[0]] if keep_ends else []
        for a, b in zip(pts, pts[1:]):
            out.append((a[0] * 0.75 + b[0] * 0.25, a[1] * 0.75 + b[1] * 0.25))
            out.append((a[0] * 0.25 + b[0] * 0.75, a[1] * 0.25 + b[1] * 0.75))
        if keep_ends:
            out.append(pts[-1])
        pts = out
    return pts

def bezier(p0, p1, p2, p3, n):
    p0, p1, p2, p3 = map(Vector, (p0, p1, p2, p3))
    out = []
    for i in range(n + 1):
        t = i / n
        u = 1 - t
        out.append(p0 * u ** 3 + p1 * 3 * u * u * t + p2 * 3 * u * t * t + p3 * t ** 3)
    return out

def ring(radius, section, segs, color_fn):
    """Teker halkası: X ekseni etrafında kapalı süpürme. section: [(x, dr)] kapalı kesit.
    color_fn(face_center_x, face_center_radius, normal) → renk."""
    bm = bmesh.new()
    rings_v = []
    for i in range(segs):
        a = 2 * math.pi * i / segs
        ca, sa = math.cos(a), math.sin(a)
        rings_v.append([bm.verts.new((x, (radius + dr) * ca, (radius + dr) * sa)) for (x, dr) in section])
    m = len(section)
    for i in range(segs):
        A, B = rings_v[i], rings_v[(i + 1) % segs]
        for j in range(m):
            k = (j + 1) % m
            bm.faces.new((A[j], A[k], B[k], B[j]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.normal_update()
    lay = bm.loops.layers.float_color.get("Col") or bm.loops.layers.float_color.new("Col")
    for f in bm.faces:
        c = f.calc_center_median()
        rr = math.hypot(c.y, c.z)
        col = color_fn(c.x, rr, f.normal)
        for l in f.loops:
            l[lay] = col
    return bm

def place(bm, loc=(0, 0, 0), rot=None, scale=None):
    m = Matrix.Translation(loc)
    if rot is not None:
        m = m @ rot
    if scale is not None:
        m = m @ Matrix.Diagonal((*scale, 1.0))
    transform_bm(bm, m)
    return bm

def _ob(bm, name, pivot_matrix, col, sharp=62):
    # 6-8 kenarlı borular (45-60°) yumuşak kalsın; 90° köşeler (sepet ağzı, pedal) keskin (09 §3)
    """bm dünya koordinatında; obje pivot_matrix'e yerleşir, mesh yerele çevrilir."""
    transform_bm(bm, pivot_matrix.inverted())
    ob = to_object(bm, name, col, sharp_angle=sharp)
    ob.matrix_world = pivot_matrix
    return ob

# ------------------------------------------------------------------ TEKER
def wheel_bm(R, segs, spokes, seed, hub_len=0.16):
    """Göbek merkezde (0,0,0), eksen X. Ahşap jant + demir lastik + ahşap teller + bakır göbek."""
    parts = []
    tyre_t = 0.028                     # demir lastik kalınlığı (radyal)
    fel_in = R - tyre_t - 0.055         # jant (felloe) iç yarıçap
    fel_out = R - tyre_t
    fel_mid = (fel_in + fel_out) / 2

    # demir lastik (yuvarlatılmış kesit)
    tyre_sec = [(-0.026, 0.0), (-0.029, 0.016), (0.0, 0.031), (0.029, 0.016), (0.026, 0.0)]
    tyre_sec = [(x, dr) for (x, dr) in tyre_sec]
    def tyre_col(x, rr, n):
        up = max(0.0, n.z) * 0.4
        return c_iron(0.35 + up + (0.15 if abs(x) < 0.02 else 0.0))
    parts.append(ring(fel_out - 0.002, tyre_sec, segs, tyre_col))

    # ahşap jant: yanlarda vurgu şeridi (bisiklet.md §3)
    hw = 0.024
    w = fel_out - fel_in
    fel_sec = [(-hw, 0.0), (-hw - 0.004, w * 0.42), (-hw - 0.004, w * 0.58), (-hw, w),
               (hw, w), (hw + 0.004, w * 0.58), (hw + 0.004, w * 0.42), (hw, 0.0)]
    rnd = random.Random(seed)
    def fel_col(x, rr, n):
        side = abs(n.x) > 0.6
        rel = (rr - fel_in) / w
        if side and 0.40 < rel < 0.60:
            return c_accent(0.55 + rnd.uniform(-0.08, 0.08))
        return c_wood(0.35 + 0.4 * rel + rnd.uniform(-0.05, 0.05))
    parts.append(ring(fel_in, fel_sec, segs, fel_col))

    # teller: göbekte kalın, jantta ince, hafif oval kesit; her biri azıcık farklı
    hub_r = 0.055
    for i in range(spokes):
        a = 2 * math.pi * (i + 0.5) / spokes + rnd.uniform(-0.01, 0.01)
        d = Vector((0, math.cos(a), math.sin(a)))
        p0 = d * (hub_r * 0.9)
        p1 = d * (fel_in + 0.006)
        sp = tube([p0, p1], [0.022, 0.014], sides=4, sx=1.0, sy=0.8)
        grad_paint(sp, lambda t, s=rnd.uniform(-0.06, 0.06): c_wood(0.35 + 0.35 * t + s),
                   axis=2, noise=0.0)
        # radyal boyama: göbek koyu, jant açık (grad_paint z'ye göre; burada radyal daha doğru)
        lay = sp.loops.layers.float_color.get("Col")
        for f in sp.faces:
            for l in f.loops:
                t = (l.vert.co.yz.length - hub_r) / (fel_in - hub_r)
                l[lay] = c_wood(0.3 + 0.4 * max(0, min(1, t)))
        parts.append(sp)

    # göbek: tombul ahşap gövde + iki bakır bilezik + demir aks başı
    prof = [(0.02, -hub_len / 2), (0.045, -hub_len / 2), (0.052, -hub_len / 2 + 0.012),
            (0.06, -0.03), (0.064, 0.0), (0.06, 0.03), (0.052, hub_len / 2 - 0.012),
            (0.045, hub_len / 2), (0.02, hub_len / 2)]
    hub = bm_lathe(prof, sides=10)
    transform_bm(hub, Matrix.Rotation(math.pi / 2, 4, "Y"))     # lathe Z → X
    lay = hub.loops.layers.float_color.get("Col") or hub.loops.layers.float_color.new("Col")
    for f in hub.faces:
        cx = f.calc_center_median().x
        for l in f.loops:
            band = abs(abs(l.vert.co.x) - (hub_len / 2 - 0.012)) < 0.014
            l[lay] = c_copper(0.55) if band else c_wood(0.5, alt=True)
    parts.append(hub)
    return bm_merge(*[(p, None) for p in parts])

def crank_bm(side, angle):
    """Ön göbekte krank kolu (demir) — ön tekerle birlikte döner. side: -1 sol, +1 sağ.
    angle: kolun yönü (0 = aşağı). Göbek merkezli yerel koordinat."""
    x = side * CRANK_X
    d = Vector((0, -math.sin(angle), -math.cos(angle)))
    p0 = Vector((x, 0, 0))
    p1 = p0 + d * CRANK
    arm = tube([p0, p0.lerp(p1, 0.5), p1], [0.020, 0.016, 0.015], sides=6, sx=0.6, sy=1.0)
    paint_by(arm, lambda f, l: c_iron(0.55 + 0.3 * max(0, f.normal.z)))
    boss = bm_cyl(0.026, 0.026, 0.025, sides=8)
    transform_bm(boss, Matrix.Translation((x - side * 0.012, 0, 0)) @ Matrix.Rotation(math.pi / 2, 4, "Y")
                 @ Matrix.Translation((0, 0, -0.0125)))
    paint_by(boss, lambda f, l: c_copper(0.45))
    return bm_merge((arm, None), (boss, None)), p1

def pedal_bm(side):
    """Pedal: mil + ahşap blok. Pivot pedal milinde (yerel 0)."""
    blk = bm_box(0.085, 0.07, 0.032, bevel=0.008, origin="center")
    transform_bm(blk, Matrix.Translation((side * 0.06, 0, 0)))
    grad_paint(blk, lambda t: c_wood(0.35 + 0.45 * t), seed=11)
    axle = tube([(0, 0, 0), (side * 0.03, 0, 0), (side * 0.11, 0, 0)], 0.007, sides=6)
    paint_by(axle, lambda f, l: c_iron(0.5))
    return bm_merge((blk, None), (axle, None))

# ------------------------------------------------------------------ GÖVDE (sabit)
def frame_bm():
    parts = []
    head_lo = HUB_ON + STEER * 0.44
    head_hi = HUB_ON + STEER * 0.56

    # omurga: baş borusundan sele altına iner, arka tekere doğru kalkar (bisiklet.md #4, #5)
    p0 = head_lo + STEER * 0.04 + Vector((0, 0.03, 0))
    spine_pts = bezier(p0, p0 + Vector((0, 0.22, -0.10)), Vector((0, 0.18, SPINE_MID_Z)), Vector((0, 0.50, 0.58)), 10)
    radii = [0.052, 0.055, 0.056, 0.055, 0.053, 0.050, 0.047, 0.044, 0.041, 0.038, 0.035]
    sp = tube(spine_pts, radii, sides=8, sx=0.78, sy=1.0)
    grad_paint(sp, lambda t: c_wood(0.3 + 0.55 * t), seed=21)
    parts.append(sp)
    spine_end = spine_pts[-1]

    # bakır bilezikler omurgada (bağlantı yerleri, #9)
    def band(center, axis, r, w=0.022):
        b = bm_cyl(r, r, w, sides=10)
        transform_bm(b, Matrix.Translation((0, 0, -w / 2)))
        q = Vector((0, 0, 1)).rotation_difference(axis).to_matrix().to_4x4()
        transform_bm(b, Matrix.Translation(center) @ q)
        grad_paint(b, lambda t: c_copper(0.35 + 0.4 * t), noise=0.02)
        return b
    for idx in (1, 6, 10):
        a = spine_pts[max(idx - 1, 0)]
        b = spine_pts[min(idx + 1, len(spine_pts) - 1)]
        parts.append(band(spine_pts[idx], (b - a).normalized(), radii[idx] * 1.12))

    # baş borusu (ahşap kovan + iki bakır bilezik): direksiyon ekseni çevresinde
    hb = tube([head_lo, head_lo.lerp(head_hi, 0.5), head_hi], [0.040, 0.038, 0.040], sides=10)
    grad_paint(hb, lambda t: c_wood(0.35 + 0.4 * t, alt=True), seed=22)
    parts.append(hb)
    parts.append(band(head_lo + STEER * 0.01, STEER, 0.045, 0.02))
    parts.append(band(head_hi - STEER * 0.01, STEER, 0.045, 0.02))

    # arka çatal: omurga ucundan arka göbeğe iki demir kol (yanlara açılır)
    for s in (-1, 1):
        q0 = spine_end + Vector((s * 0.012, -0.02, 0.0))
        q1 = HUB_ARKA + Vector((s * 0.085, 0, 0))
        mid = q0.lerp(q1, 0.5) + Vector((s * 0.03, 0.0, 0.02))
        st = tube(bezier(q0, q0.lerp(mid, 0.6), mid.lerp(q1, 0.4), q1, 5), [0.018, 0.017, 0.016, 0.015, 0.014, 0.014],
                  sides=6, sx=0.7, sy=1.0)
        paint_by(st, lambda f, l: c_iron(0.45 + 0.35 * max(0, f.normal.z)))
        parts.append(st)
        # aks başı somunu
        nut = bm_cyl(0.018, 0.016, 0.018, sides=6)
        transform_bm(nut, Matrix.Translation(HUB_ARKA + Vector((s * 0.09, 0, 0))) @
                     Matrix.Rotation(s * math.pi / 2, 4, "Y"))
        paint_by(nut, lambda f, l: c_copper(0.5))
        parts.append(nut)

    # sele yaprak yayı: omurga önünden kalkıp selenin altına uzanan demir şerit (#6)
    s0 = spine_pts[2] + Vector((0, 0, 0.035))
    s3 = Vector((0, SADDLE_Y + 0.02, SADDLE_TOP - 0.065))
    spring_pts = bezier(s0, s0 + Vector((0, 0.10, 0.07)), s3 + Vector((0, -0.12, 0.0)), s3, 7)
    spr = tube(spring_pts, 0.012, sides=6, sx=2.6, sy=0.55)
    paint_by(spr, lambda f, l: c_iron(0.4 + 0.45 * max(0, f.normal.z)))
    parts.append(spr)
    # arka destek dikmesi (omurgadan selenin arkasına)
    sp_back = spine_pts[7]
    post = tube([sp_back, sp_back.lerp(s3 + Vector((0, 0.04, 0)), 0.5), s3 + Vector((0, 0.04, 0.0))], 0.012, sides=6)
    paint_by(post, lambda f, l: c_iron(0.5))
    parts.append(post)

    # sele: deri, tombul, önü daralan (üstten armut)
    sad = bmesh.new()
    prof = [(-0.10, 0.03), (-0.05, 0.055), (0.0, 0.08), (0.05, 0.095), (0.09, 0.09), (0.11, 0.06), (0.115, 0.0)]
    secs = []
    for (yy, hw) in prof:
        # kesit: üst kubbe, alt düz
        sec = []
        for i in range(8):
            a = math.pi * i / 7
            sec.append((math.cos(a) * hw, yy, 0.012 + math.sin(a) * 0.035 * (0.6 + 0.4 * hw / 0.095)))
        sec += [(-hw * 0.85, yy, 0.0), (hw * 0.85, yy, 0.0)]
        secs.append(sec)
    # kesiti kapalı halka sırasına sok (sağ→üst→sol→alt sol→alt sağ)
    secs = [[s[i] for i in list(range(8)) + [8, 9]] for s in secs]
    sad = bm_sweep(secs, cap=True)
    transform_bm(sad, Matrix.Translation((0, SADDLE_Y, SADDLE_TOP - 0.05)))
    lay = sad.loops.layers.float_color.get("Col") or sad.loops.layers.float_color.new("Col")
    for f in sad.faces:
        for l in f.loops:
            t = (l.vert.co.z - (SADDLE_TOP - 0.05)) / 0.05
            l[lay] = c_leather(max(0.0, min(1.0, t)))
    parts.append(sad)

    # ayak dayaması: ön çatal yanında değil, omurgada iki küçük ahşap takoz (inişte ayak koymak için)
    for s in (-1, 1):
        peg = tube([spine_pts[4] + Vector((s * 0.03, 0, -0.01)), spine_pts[4] + Vector((s * 0.10, 0.0, -0.025))],
                   [0.016, 0.014], sides=6)
        grad_paint(peg, lambda t: c_wood(0.4 + 0.3 * t, alt=True))
        parts.append(peg)
    return bm_merge(*[(p, None) for p in parts])

# ------------------------------------------------------------------ GİDON (direksiyonla döner)
def steer_bm():
    parts = []
    crown = HUB_ON + STEER * 0.40
    # ön çatal: iki demir kol, aks başında incelir
    for s in (-1, 1):
        a = HUB_ON + Vector((s * 0.085, 0, 0))
        b = crown + Vector((s * 0.07, 0, 0))
        blade = tube([a, a.lerp(b, 0.5) + Vector((s * 0.006, -0.01, 0)), b], [0.014, 0.018, 0.021],
                     sides=6, sx=0.75, sy=1.0)
        paint_by(blade, lambda f, l: c_iron(0.4 + 0.4 * max(0, f.normal.z)))
        parts.append(blade)
        nut = bm_cyl(0.02, 0.017, 0.02, sides=6)
        transform_bm(nut, Matrix.Translation(HUB_ON + Vector((s * 0.09, 0, 0))) @ Matrix.Rotation(s * math.pi / 2, 4, "Y"))
        paint_by(nut, lambda f, l: c_copper(0.5))
        parts.append(nut)
    # taç: iki kolu birleştiren demir köprü
    cr = tube([crown + Vector((-0.085, 0, 0)), crown, crown + Vector((0.085, 0, 0))], 0.022, sides=8, sx=1.0, sy=0.8)
    paint_by(cr, lambda f, l: c_iron(0.45 + 0.4 * max(0, f.normal.z)))
    parts.append(cr)
    # direksiyon mili (baş borusunun içinden geçip gidona çıkar)
    top = HUB_ON + STEER * 0.63
    stem = tube([crown, crown.lerp(top, 0.5), top], 0.022, sides=8)
    paint_by(stem, lambda f, l: c_iron(0.55))
    parts.append(stem)
    cap = bm_cyl(0.03, 0.022, 0.025, sides=8)
    transform_bm(cap, Matrix.Translation(top) @ Vector((0, 0, 1)).rotation_difference(STEER).to_matrix().to_4x4())
    grad_paint(cap, lambda t: c_copper(0.4 + 0.45 * t))
    parts.append(cap)

    # gidon: ahşap, uçlara doğru sürücüye kıvrılır (FPS'te en çok görülen parça)
    hub = top + Vector((0, 0.0, 0.01))
    bar_pts = []
    for i in range(-6, 7):
        u = i / 6.0
        x = u * 0.29
        y = hub.y + 0.10 * (u * u) ** 1.2          # uçlar geriye
        z = BAR_Z - 0.035 * (1 - u * u) + 0.0      # ortası hafif aşağı
        bar_pts.append(Vector((x, y, z)))
    # orta kısım dikmenin tepesine iner
    bar_pts[6] = Vector((0, hub.y, hub.z + 0.005))
    radii = [0.017 if abs(i) > 3 else 0.020 for i in range(-6, 7)]
    bar = tube(bar_pts, radii, sides=8)
    grad_paint(bar, lambda t: c_wood(0.35 + 0.5 * t), seed=31)
    parts.append(bar)
    # orta bakır kelepçe
    cl = tube([Vector((-0.04, hub.y, hub.z + 0.005)), Vector((0, hub.y, hub.z + 0.005)), Vector((0.04, hub.y, hub.z + 0.005))],
              0.026, sides=8)
    grad_paint(cl, lambda t: c_copper(0.35 + 0.5 * t))
    parts.append(cl)
    # deri sargılı tombul tutamaklar (sargı bantları köşe rengiyle)
    for s in (-1, 1):
        g0 = bar_pts[6 + s * 4]
        g1 = bar_pts[6 + s * 6] + Vector((s * 0.035, 0.012, 0.0))
        gp = [g0.lerp(g1, k / 6) for k in range(7)]
        gr = tube(gp, [0.022, 0.026, 0.027, 0.027, 0.027, 0.026, 0.024], sides=8)
        lay = gr.loops.layers.float_color.get("Col") or gr.loops.layers.float_color.new("Col")
        for f in gr.faces:
            c = f.calc_center_median()
            k = int(abs(c.x - g0.x) / 0.012) % 2
            col = c_leather(0.55 if k else 0.3)
            for l in f.loops:
                l[lay] = col
        parts.append(gr)
        # uç topuzu (bakır)
        knob = bm_lathe([(0.0, 0.0), (0.02, 0.0), (0.026, 0.008), (0.022, 0.018), (0.0, 0.022)], sides=8)
        transform_bm(knob, Matrix.Translation(g1) @ Vector((0, 0, 1)).rotation_difference((g1 - g0).normalized()).to_matrix().to_4x4())
        paint_by(knob, lambda f, l: c_copper(0.5 + 0.3 * max(0, f.normal.z)))
        parts.append(knob)

    # zil: sol tutamağın içinde, bakır kubbe + demir kelepçe
    bp = bar_pts[6 - 3] + Vector((0, -0.012, 0.022))
    bell = bm_lathe([(0.0, 0.0), (0.032, 0.0), (0.034, 0.006), (0.028, 0.022), (0.016, 0.034), (0.006, 0.04), (0.0, 0.041)], sides=10)
    transform_bm(bell, Matrix.Translation(bp))
    grad_paint(bell, lambda t: c_copper(0.35 + 0.6 * t))
    parts.append(bell)

    # sepet: taca ve gidon ortasına bağlı hasır sepet (bisiklet.md §1)
    parts.extend(basket_parts(crown, hub))
    return bm_merge(*[(p, None) for p in parts])

def basket_parts(crown, hub):
    parts = []
    bw, bd, bh = 0.36, 0.27, 0.21         # üst ağız ölçüleri
    base = Vector((0.0, crown.y - 0.20, crown.z + 0.005))
    # yuvarlatılmış dikdörtgen kesitler, alttan üste genişler (hasır sepet)
    def rrect(w, d, r, n=2):
        pts = []
        cs = [(w / 2 - r, d / 2 - r, 0), (-w / 2 + r, d / 2 - r, 1), (-w / 2 + r, -d / 2 + r, 2), (w / 2 - r, -d / 2 + r, 3)]
        for (cx, cy, q) in cs:
            for i in range(n + 1):
                a = math.pi / 2 * (q + i / n)
                pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
        return pts
    rows = 5
    secs = []
    for k in range(rows + 1):
        t = k / rows
        w = bw * (0.82 + 0.18 * t)
        d = bd * (0.82 + 0.18 * t)
        bulge = 1.0 + 0.04 * math.sin(math.pi * t)
        pts = rrect(w * bulge, d * bulge, 0.05)
        secs.append([Vector((x, y, 0)) + base + Vector((0, 0, bh * t)) for (x, y) in pts])
    body = bm_sweep(secs, cap=False)
    # taban
    bot = bmesh.new()
    vs = [bot.verts.new(p) for p in secs[0]]
    bot.faces.new(list(reversed(vs)))
    body = bm_merge((body, None), (bot, None))
    # örgü: sıra sıra açık/koyu, dikey çubuk hizasında koyu (köşe rengiyle örgü bandı, geometri değil)
    lay = body.loops.layers.float_color.get("Col") or body.loops.layers.float_color.new("Col")
    rnd = random.Random(41)
    body.faces.index_update()
    for f in body.faces:
        c = f.calc_center_median()
        row = int((c.z - base.z) / (bh / rows) + 1e-4)
        col_i = f.index % 2
        t = 0.35 + 0.45 * ((c.z - base.z) / bh)
        v = c_wicker(t + (0.32 if (row + col_i) % 2 else -0.22) + rnd.uniform(-0.04, 0.04))
        for l in f.loops:
            l[lay] = v
    # iç yüz: sepetin içi görülür (FPS'te yukarıdan) → ince et kalınlığı
    inner = bm_sweep([[base + (p - base) * 1.0 + Vector((0, 0, 0)) for p in s] for s in secs], cap=False)
    for v in inner.verts:
        rel = v.co - Vector((base.x, base.y, v.co.z))
        v.co = Vector((base.x, base.y, v.co.z)) + rel * 0.93
    bmesh.ops.reverse_faces(inner, faces=inner.faces)
    grad_paint(inner, lambda t: c_wicker(0.1 + 0.35 * t), noise=0.03)
    parts.append(body)
    parts.append(inner)
    # ağız kenarı: kalın hasır halka
    top_pts = secs[-1] + [secs[-1][0]]
    rim = tube(top_pts, 0.017, sides=5)
    grad_paint(rim, lambda t: c_wicker(0.55 + 0.3 * t), noise=0.05)
    parts.append(rim)
    # kurdele (vurgu): ön yüzde düğüm — kütleyle verilen küçük ayrıntı
    fy = base.y - bd * 0.5 - 0.012
    rb = tube([Vector((-0.06, fy, base.z + bh * 0.62)), Vector((0, fy - 0.01, base.z + bh * 0.66)),
               Vector((0.06, fy, base.z + bh * 0.62))], 0.012, sides=6, sx=0.6, sy=1.6)
    grad_paint(rb, lambda t: c_accent(0.4 + 0.5 * t))
    parts.append(rb)
    knot = bm_lathe(chaikin([(0.0, 0.0), (0.018, 0.0), (0.022, 0.012), (0.016, 0.024), (0.0, 0.026)], 1), sides=8)
    transform_bm(knot, Matrix.Translation((0, fy - 0.012, base.z + bh * 0.66)) @ Matrix.Rotation(math.pi / 2, 4, "X"))
    grad_paint(knot, lambda t: c_accent(0.45 + 0.4 * t), axis=2)
    parts.append(knot)
    # bağlantı: sepet arkasından gidon ortasına iki demir askı, alttan taca bir destek
    for s in (-1, 1):
        a = Vector((s * 0.09, base.y + bd * 0.45, base.z + bh * 0.95))
        b = Vector((s * 0.035, hub.y - 0.01, hub.z - 0.005))
        h = tube([a, a.lerp(b, 0.5) + Vector((0, 0, 0.02)), b], 0.008, sides=6)
        paint_by(h, lambda f, l: c_iron(0.55))
        parts.append(h)
    sup = tube([Vector((0, base.y + bd * 0.3, base.z + 0.004)), Vector((0, crown.y - 0.02, crown.z))], 0.012, sides=6)
    paint_by(sup, lambda f, l: c_iron(0.45))
    parts.append(sup)
    # içinde: ustanın ayak işi — iple bağlı keten bohça (sebepli nesne)
    bun = bm_lathe(chaikin([(0.0, 0.0), (0.07, 0.0), (0.09, 0.03), (0.08, 0.07), (0.04, 0.095), (0.0, 0.1)], 1), sides=10)
    transform_bm(bun, Matrix.Translation(base + Vector((0.035, 0.015, 0.02))) @ Matrix.Diagonal((1.45, 1.1, 1.9, 1.0)))
    jitter(bun, 0.004, seed=42)
    grad_paint(bun, lambda t: mix(pal("kil", 1), mix(pal("kil", 0), pal("siva", 0), 0.5), 0.2 + 0.8 * t), noise=0.04)
    parts.append(bun)
    return parts

# ------------------------------------------------------------------ KURULUM
def _pivot(loc, rot_x=0.0):
    return Matrix.Translation(loc) @ Matrix.Rotation(rot_x, 4, "X")

def build_bisiklet():
    col = collection(COL)
    clear_collection(COL)
    root = empty("Bisiklet", col, loc=(0, 0, 0), kind="ARROWS", size=0.4)

    obs = []
    obs.append(_ob(frame_bm(), "SM_Bisiklet_Govde", _pivot((0, 0, 0)), col))

    # gidon: pivot ön göbekte, yerel Z = direksiyon ekseni (X etrafında -RAKE)
    steer_piv = _pivot(HUB_ON, -RAKE)
    obs.append(_ob(steer_bm(), "Bisiklet_Gidon", steer_piv, col))

    # ön teker + krank (krank sağda aşağı-önde, solda tersi)
    crank_a = math.radians(35)
    on = wheel_bm(R_ON, 24, 12, seed=51, hub_len=0.17)
    ck_r, pr = crank_bm(+1, crank_a)
    ck_l, pl = crank_bm(-1, crank_a + math.pi)
    on = bm_merge((on, None), (ck_r, None), (ck_l, None))
    transform_bm(on, Matrix.Translation(HUB_ON))
    obs.append(_ob(on, "Bisiklet_On_Teker", _pivot(HUB_ON), col))

    arka = wheel_bm(R_ARKA, 20, 10, seed=52, hub_len=0.16)
    transform_bm(arka, Matrix.Translation(HUB_ARKA))
    obs.append(_ob(arka, "Bisiklet_Arka_Teker", _pivot(HUB_ARKA), col))

    for side, p, nm in ((-1, pl, "Bisiklet_Pedal_Sol"), (+1, pr, "Bisiklet_Pedal_Sag")):
        world = HUB_ON + p
        pb = pedal_bm(side)
        transform_bm(pb, Matrix.Translation(world))
        obs.append(_ob(pb, nm, _pivot(world), col))

    for o in obs:
        parent_keep(o, root)
    print("Bisiklet üçgen:", tri_count(obs), {o.name: tri_count([o]) for o in obs})
    return root

def preview_scene():
    """Önizleme: zemin + güneş + gök (dışa aktarılmaz)."""
    col = collection("_Onizleme")
    clear_collection("_Onizleme")
    g = bm_cyl(2.2, 2.2, 0.02, sides=24)
    transform_bm(g, Matrix.Translation((0, 0, -0.02)))
    grad_paint(g, lambda t: mix(pal("cimen", 2), pal("cimen", 1), t))
    to_object(g, "Onizleme_Zemin", col)
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

def export_bisiklet(path=EXPORT_PATH):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    for o in list(bpy.context.selected_objects):
        o.select_set(False)
    col = bpy.data.collections[COL]
    for o in col.all_objects:
        o.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH", "EMPTY"},
        apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        mesh_smooth_type="OFF", use_mesh_modifiers=True, colors_type="LINEAR", bake_anim=False, add_leaf_bones=False)
    for o in list(bpy.context.selected_objects):
        o.select_set(False)
    return path
