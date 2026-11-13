# Simya Traktörü + 6 alet, Bereket Tarlası (24 parsel × 6 durum), Ormancı Korusu (kesilebilir ağaç durumları, halkalı kütük,
# kızak yükü, fidan, kurdele). Görev tasarımı: BilgiAdasi_Docs/02_Mekanik/04-gorev-plani.md
# Önce harita_detay.py (bu dosya yükler). Yarışma süresince yazıldı (0'dan üret). Blender 5.x.
#
#   g = {"__name__": "harita_tarla"}; exec(compile(open(p, encoding="utf-8").read(), p, "exec"), g)
#   g["build_traktor"](); g["build_bereket"](); g["build_koru"](); g["export_tarla"]()

import os
_dp2 = os.path.join(r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\ArtSource\Blender\Script", "harita_detay.py")
exec(compile(open(_dp2, encoding="utf-8").read(), _dp2, "exec"), globals())

# ---- bisiklet.py'den aynı yardımcılar (o dosya COL vb. tanımladığı için exec edilmez)
def c_iron(light=0.5):
    return mix(demir(0.55), demir(0.95), light)

def c_leather(t=0.5):
    # deri: ahşap gölge + çatı gölge (aynı sıcak ailenin koyusu)
    return mix(mix(pal("ahsap", 2), pal("cati", 2), 0.25), pal("ahsap", 1), t * 0.6)

def c_wood(t=0.5, alt=False):
    # iki ahşap tonu (kiraz + yumuşak ağaç, bisiklet.md #7): alt=True biraz daha kızıl
    a, b = pal("ahsap", 2), pal("ahsap", 0)
    c = mix(a, b, t)
    return mix(c, pal("cati", 2), 0.12) if alt else c

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

TR_COL = "Traktor"
TARLA_COL = "Bereket_Tarlasi"
KORU_COL = "Ormanci_Korusu"
TR_EXPORT = r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\Assets\_Project\Art\Traktor"

# ================================================================== SİMYA TRAKTÖRÜ
# Yerel: ön = -Y (Unity +Z), zemin z=0. Arka aks y=+0.9, ön aks y=-1.55.
R_ARKA_T, R_ON_T = 0.88, 0.48
Y_ARKA, Y_ON = 0.9, -1.55
HITCH = Vector((0.0, 1.85, 0.55))

def _teker_bm(R, W, spokes, seed, cleats=True):
    """Geniş jantlı çekiş tekeri (Fowler: yumuşak toprakta batmasın) + çapraz tırnaklar + ahşap kollar + bakır göbek.
    Yerel: eksen X, merkez (0,0,0)."""
    rnd = random.Random(seed)
    parts = []
    n = 22
    # jant (demir bant, iç kenarlar pahlı)
    sec = [(-W / 2, -0.07), (-W / 2 - 0.02, -0.02), (-W / 2, 0.0), (W / 2, 0.0), (W / 2 + 0.02, -0.02), (W / 2, -0.07)]
    rim = ring(R - 0.0, sec, n, lambda x, rr, nrm: c_iron(0.35 + 0.35 * max(0, nrm.z) + (0.12 if rr > R - 0.01 else 0)))
    parts.append(rim)
    if cleats:
        for i in range(n):
            a = 2 * math.pi * (i + 0.5) / n
            cl = box(W * 0.95, 0.07, 0.06, origin="center")
            transform_bm(cl, Matrix.Rotation(a - math.pi / 2, 4, "X") @ Matrix.Translation((0, 0, R + 0.02)) @ Matrix.Rotation(0.5 * (1 if i % 2 else -1), 4, "Z"))
            flat(cl, c_iron(0.55))
            parts.append(cl)
    hub_r = 0.14 + R * 0.06
    for i in range(spokes):
        a = 2 * math.pi * i / spokes + 0.1
        d = Vector((0, math.cos(a), math.sin(a)))
        for sx in (-W * 0.22, W * 0.22):
            sp = tube([Vector((sx, 0, 0)) + d * hub_r, Vector((sx * 0.6, 0, 0)) + d * (R - 0.08)], [0.035, 0.025], sides=5, sx=1.0, sy=0.8)
            paint_by(sp, lambda f, l: c_wood(0.45))
            parts.append(sp)
    hub = bm_lathe([(0.05, -W * 0.35), (hub_r, -W * 0.3), (hub_r * 1.1, 0.0), (hub_r, W * 0.3), (0.05, W * 0.35)], sides=10)
    transform_bm(hub, Matrix.Rotation(math.pi / 2, 4, "Y"))
    grad(hub, "bakir", 2, 0)
    parts.append(hub)
    return merge(parts)

def traktor_govde_bm():
    parts = []
    # şasi: iki uzun ahşap kiriş + traversler
    for sx in (-0.5, 0.5):
        b = box(0.16, 3.5, 0.2, x=sx, y=-0.3, z=0.72, bevel=0.02)
        grad(b, "ahsap", 2, 1)
        parts.append(b)
    for y in (-1.5, -0.5, 0.5, 1.35):
        t = box(1.2, 0.14, 0.14, y=y, z=0.75, bevel=0.015)
        grad(t, "ahsap", 2, 1)
        parts.append(t)
    # kazan (yatay bakır silindir) + demir kuşaklar
    by0, by1, bz, br = -1.6, 0.35, 1.38, 0.5
    boil = bm_lathe([(0.0, by0 - 0.02), (br - 0.04, by0), (br, by0 + 0.05), (br, by1 - 0.05), (br - 0.03, by1), (0.0, by1 + 0.01)], sides=16)
    transform_bm(boil, Matrix.Translation((0, 0, bz)) @ Matrix.Rotation(-math.pi / 2, 4, "X"))
    paint_by(boil, lambda f, l: tone(mix(pal("bakir", 2), pal("bakir", 0), 0.35 + 0.5 * max(0, f.normal.z)), 1.0))
    parts.append(boil)
    for y in (by0 + 0.25, -0.75, 0.0):
        bd = bm_lathe([(br + 0.025, y - 0.05), (br + 0.035, y), (br + 0.025, y + 0.05)], sides=16, close_top=False, close_bottom=False)
        bd = bm_lathe([(br - 0.01, -0.05), (br + 0.03, -0.04), (br + 0.03, 0.04), (br - 0.01, 0.05)], sides=16, close_top=False, close_bottom=False)
        transform_bm(bd, Matrix.Translation((0, y, bz)) @ Matrix.Rotation(-math.pi / 2, 4, "X"))
        flat(bd, c_iron(0.55))
        parts.append(bd)
    # ön duman kutusu kapağı (yuvarlak kapı + perçinler) ve baca
    door = bm_lathe([(0.0, 0.0), (0.36, 0.0), (0.4, 0.04), (0.36, 0.08), (0.0, 0.1)], sides=16)
    transform_bm(door, Matrix.Translation((0, by0 - 0.02, bz)) @ Matrix.Rotation(math.pi / 2, 4, "X"))
    flat(door, c_iron(0.45))
    parts.append(door)
    for k in range(10):
        a = 2 * math.pi * k / 10
        rv = _blob(Vector((math.cos(a) * 0.3, by0 - 0.11, bz + math.sin(a) * 0.3)), 0.025, sub=1, jit=0.0)
        flat(rv, pal("bakir", 0))
        parts.append(rv)
    hd = box(0.18, 0.05, 0.05, y=by0 - 0.14, z=bz)
    flat(hd, pal("bakir", 0))
    parts.append(hd)
    ch = bm_lathe(chaikin([(0.16, 0.0), (0.15, 0.9), (0.17, 1.2), (0.28, 1.45), (0.3, 1.52), (0.24, 1.55)], 1), sides=12, close_top=False)
    transform_bm(ch, Matrix.Translation((0, by0 + 0.3, bz + br - 0.05)))
    paint_by(ch, lambda f, l: c_iron(0.4 + 0.3 * smooth01(l.vert.co.z / 3.0)) if l.vert.co.z < bz + br + 1.25 else pal("bakir", 0))
    parts.append(ch)
    # ocak (arka: kil tuğla kutu, küçük kemerli kapak)
    fb = box(0.9, 0.7, 1.05, y=0.68, z=0.85, bevel=0.04)
    paint_by(fb, lambda f, l: tone(mix(pal("kil", 2), pal("kil", 1), 0.4 + 0.4 * (int(l.vert.co.z / 0.18) % 2)), 1.0))
    parts.append(fb)
    fd = bm_prism(pointed_arch(0, 0.32, 0.0, 0.18, 0.12), -0.03, 0.02)
    transform_bm(fd, Matrix.Translation((0, 1.04, 1.0)) @ Matrix.Rotation(math.pi, 4, "Z"))
    flat(fd, c_iron(0.5))
    parts.append(fd)
    # basınç saati, emniyet valfı, düdük (bakır)
    gauge = bm_lathe([(0.0, 0.0), (0.11, 0.0), (0.12, 0.04), (0.1, 0.06), (0.0, 0.065)], sides=12)
    transform_bm(gauge, Matrix.Translation((0.22, 0.32, bz + 0.38)) @ Matrix.Rotation(-math.radians(70), 4, "X"))
    paint_by(gauge, lambda f, l: pal("siva", 0) if l.vert.co.z > 0.058 and False else pal("bakir", 0))
    parts.append(gauge)
    face = bm_cyl(0.09, 0.09, 0.012, sides=12)
    transform_bm(face, Matrix.Translation((0.22, 0.34, bz + 0.38)) @ Matrix.Rotation(-math.radians(70), 4, "X") @ Matrix.Translation((0, 0, 0.06)))
    flat(face, pal("siva", 0))
    parts.append(face)
    for k, (x, hgt) in enumerate(((-0.18, 0.28), (0.05, 0.22))):
        v = bm_lathe([(0.04, 0.0), (0.035, hgt * 0.7), (0.06, hgt * 0.75), (0.02, hgt), (0.0, hgt + 0.02)], sides=8)
        transform_bm(v, Matrix.Translation((x, -0.35, bz + br - 0.04)))
        grad(v, "bakir", 2, 0)
        parts.append(v)
    # simya yakıt deposu (yanda): bakır kap + mühürlü cam şişe simgesi (Atölye kimliği)
    tk = bm_lathe(chaikin([(0.0, 0.0), (0.22, 0.0), (0.25, 0.3), (0.2, 0.62), (0.08, 0.72), (0.08, 0.82), (0.0, 0.83)], 1), sides=12)
    transform_bm(tk, Matrix.Translation((0.72, -0.55, 0.82)))
    grad(tk, "bakir", 2, 0)
    parts.append(tk)
    em = simge_bm("sise", size=0.34, th=0.04, color=pal("altin", 0))
    transform_bm(em, M(0.97, -0.55, 1.15, yaw=math.pi / 2))
    parts.append(em)
    pipe = tube_pts([(0.72, -0.55, 1.62), (0.6, -0.6, 1.75), (0.35, -0.75, 1.72)], 0.03, sides=6)
    grad(pipe, "bakir", 2, 0)
    parts.append(pipe)
    # su sandığı (öbür yan)
    wt = box(0.36, 1.1, 0.5, x=-0.74, y=-0.4, z=0.82, bevel=0.03)
    paint_by(wt, lambda f, l: mix(pal("ahsap", 2), pal("ahsap", 1), 0.3 + 0.4 * (int((l.vert.co.y + 3) / 0.22) % 2)))
    parts.append(wt)
    for y in (-0.8, 0.0):
        bd = box(0.4, 0.05, 0.54, x=-0.74, y=y, z=0.8)
        flat(bd, c_iron(0.5))
        parts.append(bd)
    # sürücü platformu, koltuk, direksiyon mili, kollar, tente
    pf = box(1.5, 0.95, 0.08, y=1.45, z=0.62, bevel=0.02)
    paint_by(pf, lambda f, l: mix(pal("ahsap", 1), pal("ahsap", 0), 0.3 + 0.4 * (int((l.vert.co.x + 3) / 0.2) % 2)))
    parts.append(pf)
    seat = box(0.75, 0.42, 0.12, y=1.6, z=1.05, bevel=0.04)
    paint_by(seat, lambda f, l: c_leather(0.45))
    parts.append(seat)
    back = box(0.75, 0.1, 0.45, y=1.84, z=1.12, bevel=0.03)
    paint_by(back, lambda f, l: c_leather(0.35))
    parts.append(back)
    for sx in (-0.3, 0.3):
        lg = box(0.08, 0.08, 0.45, x=sx, y=1.6, z=0.66)
        flat(lg, c_iron(0.5))
        parts.append(lg)
    col_ = tube_pts([(0.0, 1.12, 0.7), (0.0, 1.02, 1.2), (0.0, 0.98, 1.38)], 0.035, sides=6)
    flat(col_, c_iron(0.5))
    parts.append(col_)
    for k, x in enumerate((-0.42, 0.42)):
        lv = tube_pts([(x, 1.15, 0.68), (x, 1.12, 1.05), (x, 1.08, 1.25)], 0.02, sides=5)
        flat(lv, pal("bakir", 1))
        kn = _blob(Vector((x, 1.08, 1.28)), 0.045, sub=1, jit=0.0)
        flat(kn, pal("cati", 1) if k == 0 else pal("altin", 0))
        parts += [lv, kn]
    for sx in (-0.7, 0.7):
        for sy in (1.0, 1.9):
            po = box(0.07, 0.07, 2.05, x=sx, y=sy, z=0.66)
            grad(po, "ahsap", 2, 1)
            parts.append(po)
    n = 7
    for i in range(n):
        x0 = -0.85 + 1.7 * i / n
        x1 = -0.85 + 1.7 * (i + 1) / n
        tb = bmesh.new()
        vs = [tb.verts.new(p) for p in ((x0, 0.85, 2.62), (x1, 0.85, 2.62), (x1, 2.1, 2.78), (x0, 2.1, 2.78))]
        tb.faces.new(vs)
        flat(tb, pal("cati", 1) if i % 2 == 0 else pal("siva", 0))
        parts.append(tb)
    # çamurluk (arka tekerler üstü)
    for sx in (-1, 1):
        fn = bm_lathe([(R_ARKA_T + 0.1, -0.24), (R_ARKA_T + 0.12, 0.24)], sides=18, close_top=False, close_bottom=False)
        for v in fn.verts:
            if v.co.y < -0.05 or math.atan2(v.co.y, v.co.x) < -0.3:
                pass
        fender = []
        for i in range(9):
            a0 = math.radians(20 + i * 15)
            a1 = math.radians(20 + (i + 1) * 15)
            fb_ = bmesh.new()
            p = [(sx * 0.98, math.cos(a0) * (R_ARKA_T + 0.1), math.sin(a0) * (R_ARKA_T + 0.1)),
                 (sx * 1.48, math.cos(a0) * (R_ARKA_T + 0.1), math.sin(a0) * (R_ARKA_T + 0.1)),
                 (sx * 1.48, math.cos(a1) * (R_ARKA_T + 0.1), math.sin(a1) * (R_ARKA_T + 0.1)),
                 (sx * 0.98, math.cos(a1) * (R_ARKA_T + 0.1), math.sin(a1) * (R_ARKA_T + 0.1))]
            vs = [fb_.verts.new(q) for q in p]
            fb_.faces.new(vs if sx > 0 else list(reversed(vs)))
            fender.append(fb_)
        fm = merge(fender)
        transform_bm(fm, Matrix.Translation((0, Y_ARKA, R_ARKA_T)))
        flat(fm, c_iron(0.45))
        parts.append(fm)
        fn.free()
    # arka bağlantı çatalı (alet pimi)
    hk = box(0.3, 0.45, 0.12, y=HITCH.y - 0.2, z=HITCH.z - 0.06)
    flat(hk, c_iron(0.5))
    parts.append(hk)
    pin = bm_cyl(0.035, 0.035, 0.3, sides=6)
    transform_bm(pin, Matrix.Translation((HITCH.x, HITCH.y - 0.05, HITCH.z - 0.15)))
    flat(pin, pal("bakir", 0))
    parts.append(pin)
    # aks yatakları
    for sx in (-0.62, 0.62):
        b = box(0.16, 0.2, 0.3, x=sx, y=Y_ARKA, z=R_ARKA_T - 0.15)
        flat(b, c_iron(0.45))
        parts.append(b)
    return merge(parts)

def traktor_volan_bm():
    """Yan volan (çark): eksen X; buhar makinesinin dönen kalbi."""
    parts = []
    sec = [(-0.05, -0.05), (-0.05, 0.03), (0.05, 0.03), (0.05, -0.05)]
    parts.append(ring(0.42, sec, 18, lambda x, rr, n: c_iron(0.45 + 0.3 * max(0, n.z))))
    for i in range(6):
        a = 2 * math.pi * i / 6
        sp = tube([Vector((0, math.cos(a) * 0.06, math.sin(a) * 0.06)), Vector((0, math.cos(a) * 0.4, math.sin(a) * 0.4))],
                  0.025, sides=5)
        flat(sp, c_iron(0.5))
        parts.append(sp)
    hb = bm_cyl(0.08, 0.08, 0.14, sides=8)
    transform_bm(hb, Matrix.Rotation(math.pi / 2, 4, "Y") @ Matrix.Translation((0, 0, -0.07)))
    grad(hb, "bakir", 2, 0)
    parts.append(hb)
    return merge(parts)

def traktor_direksiyon_bm():
    parts = []
    sec = [(-0.02, -0.02), (-0.02, 0.02), (0.02, 0.02), (0.02, -0.02)]
    rg = ring(0.22, sec, 14, lambda x, rr, n: c_iron(0.55))
    transform_bm(rg, Matrix.Rotation(math.pi / 2, 4, "Y"))
    parts.append(rg)
    for i in range(4):
        a = 2 * math.pi * i / 4
        sp = tube([Vector((0, 0, 0)), Vector((math.cos(a) * 0.21, math.sin(a) * 0.21, 0))], 0.012, sides=4)
        flat(sp, c_iron(0.55))
        parts.append(sp)
    kn = bm_cyl(0.03, 0.03, 0.12, sides=6)
    transform_bm(kn, Matrix.Translation((0.21, 0, 0.0)))
    flat(kn, pal("bakir", 0))
    parts.append(kn)
    return merge(parts)

def build_traktor(loc=(46.0, 14.8), yaw_deg=90.0):          # ön tarla girişine (doğu) bakar
    col = collection(TR_COL)
    clear_collection(TR_COL)
    for me in list(bpy.data.meshes):
        if me.users == 0:
            bpy.data.meshes.remove(me)
    x, y = loc
    z = ground_min(x, y, 1.4, 2.2, math.radians(yaw_deg))
    root = empty("Traktor", col, loc=(x, y, z), rot=(0, 0, yaw_deg), kind="ARROWS", size=0.8)
    bpy.context.view_layer.update()          # parent_keep yeni kökün dünya matrisini okur
    W = root.matrix_world
    def mk(bm, name, local_pivot, sharp=40):
        transform_bm(bm, Matrix.Translation(-Vector(local_pivot)))
        ob = to_object(bm, name, col, sharp_angle=sharp)
        ob.matrix_world = W @ Matrix.Translation(local_pivot)
        parent_keep(ob, root)
        return ob
    obs = [mk(traktor_govde_bm(), "SM_Traktor_Govde", (0, 0, 0))]
    rw = []
    for sx in (-1, 1):
        t = _teker_bm(R_ARKA_T, 0.46, 12, 5100 + sx)
        transform_bm(t, Matrix.Translation((sx * 1.23, Y_ARKA, R_ARKA_T)))
        rw.append(t)
    ax = tube_pts([(-1.0, Y_ARKA, R_ARKA_T), (1.0, Y_ARKA, R_ARKA_T)], 0.06, sides=8)
    flat(ax, c_iron(0.5))
    rw.append(ax)
    obs.append(mk(merge(rw), "Traktor_Arka_Tekerler", (0, Y_ARKA, R_ARKA_T)))
    fa = []
    beam = box(1.25, 0.16, 0.16, y=Y_ON, z=R_ON_T, origin="center", bevel=0.02)
    flat(beam, c_iron(0.5))
    fa.append(beam)
    king = bm_cyl(0.08, 0.08, 0.6, sides=8)
    transform_bm(king, Matrix.Translation((0, Y_ON, R_ON_T)))
    grad(king, "bakir", 2, 0)
    fa.append(king)
    obs.append(mk(merge(fa), "Traktor_On_Aks", (0, Y_ON, R_ON_T)))
    fw = []
    for sx in (-1, 1):
        t = _teker_bm(R_ON_T, 0.22, 8, 5200 + sx, cleats=False)
        transform_bm(t, Matrix.Translation((sx * 0.72, Y_ON, R_ON_T)))
        fw.append(t)
    obs.append(mk(merge(fw), "Traktor_On_Tekerler", (0, Y_ON, R_ON_T)))
    v = traktor_volan_bm()
    transform_bm(v, Matrix.Translation((0.82, -0.2, 1.25)))
    obs.append(mk(v, "Traktor_Volan", (0.82, -0.2, 1.25)))
    d = traktor_direksiyon_bm()
    piv = Vector((0.0, 0.98, 1.42))
    transform_bm(d, Matrix.Translation(piv) @ Matrix.Rotation(math.radians(-35), 4, "X") @ Matrix.Rotation(math.pi / 2, 4, "Y"))
    obs.append(mk(d, "Traktor_Direksiyon", tuple(piv)))
    for nm, p in (("Traktor_Koltuk_Kamera", (0.0, 1.55, 1.95)), ("Traktor_Duman_Noktasi", (0.0, -1.3, 3.45)),
                  ("Traktor_Baglanti", tuple(HITCH))):
        e = empty(nm, col, kind="SINGLE_ARROW", size=0.3)
        e.matrix_world = W @ Matrix.Translation(p) @ Matrix.Rotation(math.pi, 4, "Z")
        parent_keep(e, root)
    print("Traktör:", {o.name: tri_count([o]) for o in obs}, "toplam", tri_count(obs))
    return root

# ------------------------------------------------------------------ ALETLER (pivot = bağlantı pimi, yerel +Y geriye)
def alet_pulluk_bm():
    parts = []
    bm_ = tube_pts([(0, 0, 0), (0, 0.5, -0.05), (0, 1.9, -0.15)], 0.06, sides=6)
    flat(bm_, c_iron(0.5))
    parts.append(bm_)
    cr = box(1.4, 0.12, 0.1, y=1.2, z=-0.12)
    flat(cr, c_iron(0.5))
    parts.append(cr)
    for k, x in enumerate((-0.55, 0.0, 0.55)):
        y = 0.9 + k * 0.35
        st = box(0.06, 0.06, 0.4, x=x, y=y, z=-0.5)
        flat(st, c_iron(0.45))
        parts.append(st)
        # kulak (devirme yüzü): bükük levha
        mb = bmesh.new()
        pts = []
        for i in range(4):
            t = i / 3
            pts.append([Vector((x - 0.05 + 0.3 * t, y - 0.2 + 0.25 * t, -0.52 + 0.05 * t)), Vector((x - 0.05 + 0.32 * t, y - 0.2 + 0.3 * t, -0.2 - 0.02 * t))])
        vs = [[mb.verts.new(p) for p in row] for row in pts]
        for i in range(3):
            mb.faces.new((vs[i][0], vs[i + 1][0], vs[i + 1][1], vs[i][1]))
        paint_by(mb, lambda f, l: pal("bakir", 0) if l.vert.co.z < -0.45 else c_iron(0.7))
        parts.append(mb)
        sh = bm_prism([(-0.05, 0), (0.25, 0), (0.0, 0.1)], -0.03, 0.03)
        transform_bm(sh, M(x, y - 0.25, -0.58, yaw=0.0))
        flat(sh, c_iron(0.85))
        parts.append(sh)
    wh = _teker_bm(0.25, 0.08, 6, 77, cleats=False)
    transform_bm(wh, Matrix.Translation((0.7, 1.9, -0.3)))
    parts.append(wh)
    hd = tube_pts([(0, 1.9, -0.15), (0, 2.3, 0.4), (-0.25, 2.45, 0.55)], 0.03, sides=5)
    grad(hd, "ahsap", 2, 1)
    parts.append(hd)
    return merge(parts)

def alet_ekici_bm():
    parts = []
    dr = tube_pts([(0, 0, 0), (0, 0.6, -0.05)], 0.05, sides=6)
    flat(dr, c_iron(0.5))
    parts.append(dr)
    hp = bm_lathe([(0.0, 0.0), (0.2, 0.0), (0.55, 0.45), (0.55, 0.6), (0.0, 0.6)], sides=4)
    transform_bm(hp, M(0, 1.15, -0.25, yaw=math.pi / 4) @ Matrix.Scale(1.0, 4))
    transform_bm(hp, Matrix.Translation((0, 0, 0)))
    for v in hp.verts:
        v.co.x *= 2.0
    paint_by(hp, lambda f, l: mix(pal("ahsap", 2), pal("ahsap", 1), 0.3 + 0.4 * (int((l.vert.co.z + 3) / 0.15) % 2)))
    parts.append(hp)
    for k in range(6):
        x = -0.75 + k * 0.3
        tb = tube_pts([(x, 1.15, -0.25), (x, 1.3, -0.55), (x, 1.32, -0.75)], 0.025, sides=5)
        grad(tb, "bakir", 2, 0)
        parts.append(tb)
        ds = bm_cyl(0.13, 0.13, 0.03, sides=10)
        transform_bm(ds, M(x, 1.4, -0.72, ry=math.pi / 2) @ Matrix.Translation((0, 0, -0.015)))
        flat(ds, c_iron(0.7))
        parts.append(ds)
    for sx in (-1, 1):
        wh = _teker_bm(0.38, 0.1, 8, 79 + sx, cleats=False)
        transform_bm(wh, Matrix.Translation((sx * 1.15, 1.2, -0.5)))
        parts.append(wh)
    ax = tube_pts([(-1.15, 1.2, -0.5), (1.15, 1.2, -0.5)], 0.03, sides=6)
    flat(ax, c_iron(0.5))
    parts.append(ax)
    for k in range(2):
        cv = _cuval(-0.4 + k * 0.8, 1.15, 0.36, 81 + k, pal("altin", 1))
        parts.append(cv)
    return merge(parts)

def alet_su_tanki_bm():
    parts = []
    dr = tube_pts([(0, 0, 0), (0, 0.7, -0.05)], 0.05, sides=6)
    flat(dr, c_iron(0.5))
    parts.append(dr)
    cask = bm_lathe(chaikin([(0.0, -0.75), (0.45, -0.75), (0.55, -0.4), (0.58, 0.0), (0.55, 0.4), (0.45, 0.75), (0.0, 0.75)], 1), sides=14)
    paint_by(cask, lambda f, l: mix(pal("ahsap", 2), pal("ahsap", 1), 0.3 + 0.4 * (int((math.atan2(l.vert.co.y, l.vert.co.x) + 4) / 0.45) % 2)))
    transform_bm(cask, Matrix.Translation((0, 1.4, 0.2)) @ Matrix.Rotation(math.pi / 2, 4, "Y"))
    parts.append(cask)
    for xx in (-0.5, 0.0, 0.5):
        bd = bm_lathe([(0.5, -0.04), (0.6, -0.03), (0.6, 0.03), (0.5, 0.04)], sides=14, close_top=False, close_bottom=False)
        transform_bm(bd, Matrix.Translation((xx, 1.4, 0.2)) @ Matrix.Rotation(math.pi / 2, 4, "Y"))
        flat(bd, pal("bakir", 1))
        parts.append(bd)
    sp = tube_pts([(-1.0, 2.0, -0.3), (1.0, 2.0, -0.3)], 0.035, sides=6)
    grad(sp, "bakir", 2, 0)
    parts.append(sp)
    pp = tube_pts([(0, 1.6, -0.3), (0, 1.9, -0.32), (0, 2.0, -0.3)], 0.03, sides=5)
    grad(pp, "bakir", 2, 0)
    parts.append(pp)
    for sx in (-1, 1):
        wh = _teker_bm(0.42, 0.12, 8, 85 + sx, cleats=False)
        transform_bm(wh, Matrix.Translation((sx * 0.85, 1.4, -0.15)))
        parts.append(wh)
    return merge(parts)

def alet_serpici_bm():
    parts = []
    dr = tube_pts([(0, 0, 0), (0, 0.6, -0.05)], 0.05, sides=6)
    flat(dr, c_iron(0.5))
    parts.append(dr)
    hp = bm_lathe(chaikin([(0.08, -0.2), (0.15, 0.0), (0.55, 0.45), (0.58, 0.55)], 1), sides=10, close_bottom=True, close_top=False)
    transform_bm(hp, Matrix.Translation((0, 1.0, -0.05)))
    paint_by(hp, lambda f, l: mix(pal("bakir", 2), pal("bakir", 0), smooth01((l.vert.co.z + 0.25) / 0.8)))
    parts.append(hp)
    fill = bm_lathe([(0.52, 0.42), (0.3, 0.52), (0.0, 0.56)], sides=10, close_bottom=True)
    transform_bm(fill, Matrix.Translation((0, 1.0, -0.05)))
    flat(fill, mix(pal("siva", 0), pal("gok", 0), 0.3))
    parts.append(fill)
    dk = bm_cyl(0.3, 0.3, 0.03, sides=12)
    transform_bm(dk, Matrix.Translation((0, 1.0, -0.4)))
    flat(dk, c_iron(0.7))
    parts.append(dk)
    for k in range(4):
        vn = box(0.26, 0.02, 0.05, origin="center")
        transform_bm(vn, M(0, 1.0, -0.35, yaw=k * math.pi / 4) @ Matrix.Translation((0.13, 0, 0)))
        flat(vn, c_iron(0.5))
        parts.append(vn)
    for sx in (-1, 1):
        wh = _teker_bm(0.35, 0.1, 6, 88 + sx, cleats=False)
        transform_bm(wh, Matrix.Translation((sx * 0.75, 1.0, -0.35)))
        parts.append(wh)
    return merge(parts)

def alet_bicer_bm():
    parts = []
    dr = tube_pts([(0, 0, 0), (0, 0.5, -0.1), (0.9, 0.9, -0.3)], 0.05, sides=6)
    flat(dr, c_iron(0.5))
    parts.append(dr)
    bar = box(2.6, 0.18, 0.06, x=1.2, y=1.3, z=-0.55)
    flat(bar, c_iron(0.6))
    parts.append(bar)
    for k in range(13):
        tt = bm_prism([(-0.05, 0), (0.05, 0), (0.0, 0.18)], -0.02, 0.02)
        transform_bm(tt, M(0.0 + k * 0.2, 1.2, -0.55, rx=-math.pi / 2))
        flat(tt, c_iron(0.85))
        parts.append(tt)
    # döner tırmık çarkı (reel): 5 kanatlı ahşap
    for k in range(5):
        a = 2 * math.pi * k / 5
        bl = box(2.4, 0.05, 0.05, x=1.2, y=1.3 + math.cos(a) * 0.45, z=-0.05 + math.sin(a) * 0.45)
        flat(bl, pal("ahsap", 1))
        parts.append(bl)
        for sx in (0.1, 2.3):
            sp = tube_pts([(sx, 1.3, -0.05), (sx, 1.3 + math.cos(a) * 0.45, -0.05 + math.sin(a) * 0.45)], 0.02, sides=4)
            flat(sp, pal("ahsap", 2))
            parts.append(sp)
    wh = _teker_bm(0.4, 0.12, 8, 91, cleats=False)
    transform_bm(wh, Matrix.Translation((-0.25, 1.0, -0.2)))
    parts.append(wh)
    return merge(parts)

def alet_kizak_bm():
    parts = []
    for sx in (-0.55, 0.55):
        rn = tube_pts([(sx, 0.3, -0.35), (sx, 0.45, -0.52), (sx, 2.6, -0.52)], 0.06, sides=6)
        grad(rn, "ahsap", 2, 1)
        parts.append(rn)
    for y in (0.7, 1.4, 2.1):
        cb = box(1.3, 0.12, 0.1, y=y, z=-0.47)
        grad(cb, "ahsap", 2, 1)
        parts.append(cb)
    for sx in (-1, 1):
        ch = tube_pts([(0, 0, 0), (sx * 0.3, 0.25, -0.2), (sx * 0.55, 0.32, -0.38)], 0.015, sides=4)
        flat(ch, c_iron(0.7))
        parts.append(ch)
    for k in range(3):
        lg = bm_cyl(0.2 - k * 0.02, 0.18 - k * 0.02, 2.2, sides=8)
        transform_bm(lg, M(-0.28 + k * 0.28 + (0.14 if k == 2 else 0), 0.45, -0.18 + (0.3 if k == 2 else 0), rx=-math.pi / 2))
        paint_by(lg, lambda f, l: pal("ahsap", 1) if abs(f.normal.y) < 0.7 else _halka_renk(l.vert.co.x, l.vert.co.z, 0.2))
        parts.append(lg)
    return merge(parts)

def _halka_renk(u, v, r):
    """Kesit yüzünde yaş halkaları (B1 sorusunun görseli): merkezden dışa açık-koyu halkalar, dışta kabuk."""
    d = math.hypot(u, v) / max(r, 1e-4)
    if d > 0.9:
        return pal("ahsap", 2)
    k = int(d * 9)
    return mix(pal("kil", 0), pal("altin", 2), 0.25) if k % 2 == 0 else mix(pal("ahsap", 0), pal("kil", 1), 0.4)

def build_aletler(origin=(40.0, 10.5)):
    """Aletler ahır önünde yan yana (Unity'de traktör bağlantısına kod takar). Pivot = pim."""
    col = collection(TR_COL)
    fns = [("Alet_Pulluk", alet_pulluk_bm), ("Alet_Ekici", alet_ekici_bm), ("Alet_Su_Tanki", alet_su_tanki_bm),
           ("Alet_Serpici", alet_serpici_bm), ("Alet_Bicer", alet_bicer_bm), ("Alet_Kizak", alet_kizak_bm)]
    out = []
    for i, (nm, fn) in enumerate(fns):
        x, y = origin[0] + i * 3.4, origin[1]
        z = height(x, y)
        bm = fn()
        minz = min(v.co.z for v in bm.verts)
        ob = to_object(bm, nm, col, loc=(x, y, z - minz), sharp_angle=40)
        out.append(ob)
    yc = bpy.data.collections.get(YER_COL)
    if yc:                                                    # aletlerin ve traktörün altına düşen doğa yerleri
        pts = [Vector((o.location.x, o.location.y + 1.2)) for o in out] + [Vector((46.0, 14.8))]
        for o in list(yc.objects):
            if any((Vector((o.location.x, o.location.y)) - q).length < 3.2 for q in pts):
                bpy.data.objects.remove(o, do_unlink=True)
    print("Aletler:", {o.name: tri_count([o]) for o in out})
    return out

# ================================================================== BEREKET TARLASI (6 × 4 parsel, 6 durum)
BT_C = Vector((60.0, 24.0))
BT_W, BT_D, BT_NX, BT_NY = 24.0, 16.0, 6, 4
DURUMLAR = ["Nadas", "Surulmus", "Ekili", "Filiz", "Olgun", "Hasat"]

def parsel_bm(cx, cy, w, d, durum, seed):
    """Tek parsel (yerel, merkez 0). Sırtlar X boyunca. Durumlar ayrı mesh (kod aç/kapa yapar)."""
    rnd = random.Random(seed)
    parts = []
    nu, nv = 4, 16
    pitch = d / 5.0
    def gz(u, v):
        return height(cx + u, cy + v) - height(BT_C.x, BT_C.y)
    bm = bmesh.new()
    lay = bm.loops.layers.float_color.new("Col")
    grid = []
    ridged = durum != "Nadas"
    for j in range(nv + 1):
        v = -d / 2 + d * j / nv
        rr = 0.5 + 0.5 * math.cos(2 * math.pi * (v + d / 2) / pitch)
        row = []
        for i in range(nu + 1):
            u = -w / 2 + w * i / nu
            row.append((bm.verts.new((u, v, gz(u, v) + 0.03 + (0.13 * rr if ridged else 0.0))), rr))
        grid.append(row)
    for j in range(nv):
        for i in range(nu):
            q = (grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i])
            f = bm.faces.new([a[0] for a in q])
            for l in f.loops:
                r = next(rr for (vv, rr) in q if vv == l.vert)
                p = l.vert.co
                blob = 0.5 + 0.5 * n2(cx + p.x, cy + p.y, 2.0, seed * 0.1)
                if durum == "Nadas":
                    c = mix(mix(pal("cimen", 2), pal("kil", 1), 0.45), pal("cimen", 1), blob * 0.5)
                elif durum in ("Surulmus", "Ekili", "Filiz"):
                    c = mix(mix(pal("ahsap", 2), pal("kil", 2), 0.4), pal("kil", 1), r * 0.8)
                    if durum != "Surulmus":
                        c = mix(c, pal("ahsap", 2), 0.2)            # sulanmış/ekilmiş koyu
                elif durum == "Olgun":
                    c = mix(pal("kil", 1), pal("altin", 2), 0.3 + 0.3 * r)
                else:
                    c = mix(pal("kil", 0), pal("altin", 1), 0.35 + 0.3 * blob)
                l[lay] = c
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    for f in bm.faces:
        if f.normal.z < 0:
            f.normal_flip()
    parts.append(bm)
    rows = [(-d / 2 + pitch * k) for k in range(5)]
    for k, v in enumerate(rows):
        v = v + 0.0
        if durum == "Nadas":
            for t in range(3):
                g = cimen_bm(seed + k * 7 + t, n=6, hmax=0.35)
                transform_bm(g, Matrix.Translation((rnd.uniform(-w / 2 + 0.4, w / 2 - 0.4), rnd.uniform(-d / 2 + 0.3, d / 2 - 0.3), gz(0, 0))))
                parts.append(g)
        elif durum == "Ekili":
            for t in range(6):
                u = -w / 2 + 0.35 + t * (w - 0.7) / 5
                s = _blob(Vector((u, v, gz(u, v) + 0.17)), 0.05, sub=1, squash=0.5, jit=0.1, seed=seed + t)
                flat(s, mix(pal("kil", 0), pal("altin", 1), 0.4))
                parts.append(s)
        elif durum == "Filiz":
            for t in range(7):
                u = -w / 2 + 0.3 + t * (w - 0.6) / 6
                sp = cimen_bm(seed + k * 11 + t, n=3, hmax=0.22)
                paint_by(sp, lambda f, l: mix(pal("yaprak", 1), pal("cimen", 0), smooth01(l.vert.co.z / 0.22)))
                transform_bm(sp, Matrix.Translation((u, v, gz(u, v) + 0.14)))
                parts.append(sp)
        elif durum == "Olgun":
            for t in range(3):
                u0 = -w / 2 + 0.2 + t * (w - 0.4) / 3
                u1 = u0 + (w - 0.4) / 3 - 0.08
                hgt = 0.7 * rnd.uniform(0.9, 1.1)
                a0, a1 = Vector((u0, v - 0.22, gz(u0, v) + 0.12)), Vector((u0, v + 0.22, gz(u0, v) + 0.12))
                b0, b1 = Vector((u1, v - 0.22, gz(u1, v) + 0.12)), Vector((u1, v + 0.22, gz(u1, v) + 0.12))
                ta, tb = Vector((u0, v, a0.z + hgt)), Vector((u1, v, b0.z + hgt * rnd.uniform(0.9, 1.08)))
                pb = bmesh.new()
                vs = [pb.verts.new(p) for p in (a0, a1, ta, b0, b1, tb)]
                for fi in ((0, 1, 2), (3, 5, 4), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)):
                    pb.faces.new([vs[i] for i in fi])
                bmesh.ops.recalc_face_normals(pb, faces=pb.faces)
                z0 = a0.z
                paint_by(pb, lambda f, l, z0=z0, h=hgt: mix(pal("altin", 2), pal("altin", 0), smooth01((l.vert.co.z - z0) / h)))
                parts.append(pb)
                for e in range(3):                                         # başak tepecikleri
                    uu = u0 + (u1 - u0) * (e + 0.5) / 3
                    hb = _blob(Vector((uu, v, ta.z + 0.05)), 0.07, sub=1, squash=1.6, jit=0.1, seed=seed + e)
                    flat(hb, pal("altin", 0))
                    parts.append(hb)
        elif durum == "Hasat":
            for t in range(8):
                u = -w / 2 + 0.25 + t * (w - 0.5) / 7
                st = _blade(Vector((u, v, gz(u, v) + 0.1)), 0.12, 0.08, 0.02, rnd)
                flat(st, pal("altin", 1))
                parts.append(st)
    return merge(parts)

def build_bereket():
    col = collection(TARLA_COL)
    clear_collection(TARLA_COL)
    z0 = height(BT_C.x, BT_C.y)
    root = empty("Bereket_Tarlasi", col, loc=(BT_C.x, BT_C.y, z0), kind="CUBE", size=1.0)
    bpy.context.view_layer.update()          # parent_keep yeni kökün dünya matrisini okur
    pw, pd = BT_W / BT_NX, BT_D / BT_NY
    n = 0
    tot = 0
    for r in range(BT_NY):
        for c in range(BT_NX):
            cx = BT_C.x - BT_W / 2 + pw * (c + 0.5)
            cy = BT_C.y - BT_D / 2 + pd * (r + 0.5)
            for k, durum in enumerate(DURUMLAR):
                bm = parsel_bm(cx, cy, pw - 0.1, pd - 0.1, durum, 6000 + r * 100 + c * 10 + k)
                ob = to_object(bm, "Tarla_P%d%d_%s" % (r + 1, c + 1, durum), col, loc=(cx, cy, z0), sharp_angle=0)
                parent_keep(ob, root)
                if k > 0:
                    ob.hide_set(True)
                    ob.hide_render = True
                tot += tri_count([ob])
                n += 1
    # çevre: alçak çit + giriş (güney, Yol_Tarla tarafı) + parsel köşe kazıkları (kod için de referans)
    parts = []
    pts = [(-BT_W / 2 - 0.5, -BT_D / 2 - 0.5), (BT_W / 2 + 0.5, -BT_D / 2 - 0.5), (BT_W / 2 + 0.5, BT_D / 2 + 0.5),
           (-BT_W / 2 - 0.5, BT_D / 2 + 0.5)]
    rnd = random.Random(61)
    for k in range(4):
        (u0, v0), (u1, v1) = pts[k], pts[(k + 1) % 4]
        L = math.hypot(u1 - u0, v1 - v0)
        m = int(L / 2.0)
        rail = []
        for i in range(m + 1):
            t = i / m
            u, v = u0 + (u1 - u0) * t, v0 + (v1 - v0) * t
            if k == 0 and abs(u - 0.0) < 3.0:                       # giriş boşluğu (traktör)
                continue
            z = height(BT_C.x + u, BT_C.y + v) - z0
            ps = box(0.12, 0.12, 1.15, x=u, y=v, z=z - 0.25, yaw=rnd.uniform(0, 0.5))
            grad(ps, "ahsap", 2, 1)
            parts.append(ps)
            rail.append(Vector((u, v, z + 0.72)))
            if len(rail) >= 2 and (rail[-1] - rail[-2]).length < 2.6:
                rl = tube_pts([rail[-2], rail[-1]], 0.045, sides=4)
                grad(rl, "ahsap", 2, 1)
                parts.append(rl)
    for sx in (-1, 1):                                              # giriş direkleri + tarla levhası
        gp = box(0.22, 0.22, 2.2, x=sx * 3.0, y=-BT_D / 2 - 0.5, z=-0.3)
        grad(gp, "ahsap", 2, 1)
        parts.append(gp)
    sg = box(1.6, 0.08, 0.6, y=-BT_D / 2 - 0.55, z=1.45, bevel=0.02)
    grad(sg, "ahsap", 1, 0)
    parts.append(sg)
    em = simge_bm("basak", size=0.4, th=0.12, color=pal("cimen", 1))
    transform_bm(em, Matrix.Translation((0, -BT_D / 2 - 0.55, 1.75)))
    parts.append(em)
    cb = box(6.4, 0.14, 0.14, y=-BT_D / 2 - 0.5, z=1.9)
    grad(cb, "ahsap", 2, 1)
    parts.append(cb)
    ob = to_object(merge(parts), "SM_Bereket_Cit", col, loc=(BT_C.x, BT_C.y, z0), sharp_angle=40)
    parent_keep(ob, root)
    e = empty("Tabela_Yazi_Bereket_Tarlasi", col, loc=(BT_C.x, BT_C.y - BT_D / 2 - 0.62, z0 + 1.45), rot=(90, 0, 0), kind="SINGLE_ARROW", size=0.3)
    parent_keep(e, root)
    print("Bereket tarlası:", n, "parsel mesh,", tot, "üçgen (hepsi; aynı anda görünen ≈ 1/6)")
    # tarlanın altına düşen doğa yerlerini temizle
    yc = bpy.data.collections.get(YER_COL)
    if yc:
        k = 0
        for o in list(yc.objects):
            if abs(o.location.x - BT_C.x) < BT_W / 2 + 2.5 and abs(o.location.y - BT_C.y) < BT_D / 2 + 2.5:
                bpy.data.objects.remove(o, do_unlink=True)
                k += 1
        print("Tarla altındaki yerler silindi:", k)
    return root

# ================================================================== ORMANCI KORUSU (B zinciri)
KORU_C = Vector((40.5, 24.0))

def kuru_agac_bm(seed):
    """Yaşlı kuru ağaç (B1'de kesilecek olan): yapraksız çatallı dallar, koyu gövde."""
    rnd = random.Random(seed)
    parts = []
    tr = bm_lathe([(0.55, 0.0), (0.42, 0.4), (0.34, 2.5), (0.26, 4.2)], sides=8, close_bottom=False)
    jitter(tr, 0.03, seed=seed)
    paint_part(tr, "ahsap", lo=2, hi=1, seed=seed)
    parts.append(tr)
    def dal(p, d, L, r, depth):
        e = p + d * L
        b = tube([p, p.lerp(e, 0.5) + Vector((rnd.uniform(-0.1, 0.1), rnd.uniform(-0.1, 0.1), 0.05)), e], [r, r * 0.8, r * 0.55], sides=5)
        paint_part(b, "ahsap", lo=2, hi=1, seed=int(L * 100))
        parts.append(b)
        if depth > 0:
            for s in (-1, 1):
                nd = (d + Vector((rnd.uniform(-0.5, 0.5), rnd.uniform(-0.5, 0.5), 0.25)) * 0.8 + Vector((s * 0.3, -s * 0.2, 0))).normalized()
                dal(e, nd, L * 0.65, r * 0.6, depth - 1)
    for k in range(3):
        a = 2 * math.pi * k / 3 + rnd.uniform(-0.3, 0.3)
        dal(Vector((0, 0, 3.6 + k * 0.3)), Vector((math.cos(a) * 0.6, math.sin(a) * 0.6, 0.8)).normalized(), 1.6, 0.16, 2)
    return merge(parts)

def kutuk_bm(r=0.42, h=0.55, cut="duz"):
    """Kütük: üst kesit yüzünde yaş halkaları (köşe rengi), kabuk yanlarda."""
    prof = [(r * 1.25, -0.2), (r * 1.08, 0.08), (r, 0.2), (r, h)]
    k = bm_lathe(prof, sides=14, close_top=False, close_bottom=False)
    paint_part(k, "ahsap", lo=2, hi=1, seed=3)
    top = bmesh.new()
    lay = top.loops.layers.float_color.new("Col")
    rings_ = [0.0, 0.18, 0.36, 0.54, 0.72, 0.9, 1.0]
    vs = []
    for rr in rings_:
        n = 1 if rr == 0 else 14
        vs.append([top.verts.new((math.cos(2 * math.pi * i / 14) * r * rr, math.sin(2 * math.pi * i / 14) * r * rr, h)) for i in range(n)])
    for a in range(len(rings_) - 1):
        A, B = vs[a], vs[a + 1]
        for i in range(14):
            j = (i + 1) % 14
            f = top.faces.new((A[0], B[i], B[j])) if len(A) == 1 else top.faces.new((A[i], B[i], B[j], A[j]))
            col = _halka_renk(0, (rings_[a] + rings_[a + 1]) / 2 * r, r) if rings_[a + 1] < 0.95 else pal("ahsap", 2)
            for l in f.loops:
                l[lay] = col
    for f in top.faces:
        if f.normal.z < 0:
            f.normal_flip()
    return merge([k, top])

def build_koru():
    """B zinciri sahnesi: 3 işaretli ağaç (genç sağlıklı · yaşlı kuru · yuvalı), kesim durumları, kızak yükü, 3 fidan."""
    col = collection(KORU_COL)
    clear_collection(KORU_COL)
    z0 = height(KORU_C.x, KORU_C.y)
    root = empty("Ormanci_Korusu", col, loc=(KORU_C.x, KORU_C.y, z0), kind="CUBE", size=1.0)
    bpy.context.view_layer.update()          # parent_keep yeni kökün dünya matrisini okur
    def add(bm, name, x, y, hide=False, mat=None):
        ob = to_object(bm, name, col, loc=(x, y, height(x, y)), sharp_angle=0 if "Agac" in name else 40, mat=mat)
        parent_keep(ob, root)
        if hide:
            ob.hide_set(True)
            ob.hide_render = True
        return ob
    # 1) genç sağlıklı (korunur)
    g = make_tree("Koru_Agac_Genc", 8801, height=4.4, crown=2.5, col=col)
    g.location = (KORU_C.x - 5.0, KORU_C.y + 1.0, height(KORU_C.x - 5.0, KORU_C.y + 1.0))
    parent_keep(g, root)
    # 2) yaşlı kuru (kesilecek): ayakta / çentikli / devrik + kütük
    kx, ky = KORU_C.x, KORU_C.y - 1.0
    add(kuru_agac_bm(8802), "Koru_Agac_Kuru_Ayakta", kx, ky)
    ct = kuru_agac_bm(8802)
    for v in ct.verts:                                              # yön çentiği: güney yüzde kama boşluğu
        if 0.45 < v.co.z < 0.85 and v.co.y < -0.15:
            v.co.y *= 0.45
    add(ct, "Koru_Agac_Kuru_Centikli", kx, ky, hide=True)
    fallen = kuru_agac_bm(8802)
    transform_bm(fallen, Matrix.Translation((0, -0.4, 0.45)) @ Matrix.Rotation(math.radians(-86), 4, "X") @ Matrix.Translation((0, 0, -0.55)))
    add(fallen, "Koru_Agac_Kuru_Devrik", kx, ky, hide=True)
    add(kutuk_bm(), "Koru_Kutuk_Halkali", kx, ky, hide=True)
    # 3) yuvalı ağaç (korunur): dalda kuş yuvası
    yv = make_tree("Koru_Agac_Yuvali", 8803, height=5.6, crown=3.0, col=col)
    yv.location = (KORU_C.x + 5.5, KORU_C.y + 0.5, height(KORU_C.x + 5.5, KORU_C.y + 0.5))
    parent_keep(yv, root)
    nest = bm_lathe(chaikin([(0.0, 0.0), (0.22, 0.02), (0.3, 0.14), (0.24, 0.18), (0.16, 0.12), (0.0, 0.1)], 1), sides=10)
    jitter(nest, 0.02, seed=5)
    paint_by(nest, lambda f, l: mix(pal("ahsap", 2), pal("altin", 2), 0.35 + 0.3 * (int(l.vert.co.z / 0.04) % 2)))
    eggs = [_blob(Vector((dx, dy, 0.14)), 0.045, sub=1, squash=1.25, jit=0.0) for (dx, dy) in ((0.05, 0.0), (-0.05, 0.04))]
    for e_ in eggs:
        flat(e_, mix(pal("gok", 0), pal("siva", 0), 0.5))
    nb = merge([nest] + eggs)
    transform_bm(nb, Matrix.Translation((0.55, 0.3, 3.0)))
    add(nb, "Koru_Kus_Yuvasi", KORU_C.x + 5.5, KORU_C.y + 0.5)
    # işaret kurdeleleri (üçünde de; kesilecekte kırmızımsı çatı tonu, korunanlarda yeşil)
    for (x, y, kim) in ((KORU_C.x - 5.0, KORU_C.y + 1.0, "yaprak"), (kx, ky, "cati"), (KORU_C.x + 5.5, KORU_C.y + 0.5, "yaprak")):
        rb = bm_lathe([(0.0, 1.35), (0.0, 1.35)], sides=3) if False else None
        band = bm_lathe([(0.47, 1.3), (0.5, 1.33), (0.5, 1.47), (0.47, 1.5)], sides=12, close_top=False, close_bottom=False)
        tail = bm_prism([(0, 0), (0.08, -0.45), (0.16, -0.4), (0.1, 0)], -0.01, 0.01)
        transform_bm(tail, Matrix.Translation((0.45, -0.1, 1.42)))
        b2 = merge([band, tail])
        flat(b2, pal(kim, 1))
        add(b2, "Koru_Kurdele_%s" % ("Kes" if kim == "cati" else "Koru_%d" % int(x)), x, y)
    # kızağa yüklenecek kütük destesi (B4) — kesimden sonra görünür
    ld = []
    for k in range(4):
        lg = bm_cyl(0.2, 0.18, 1.8, sides=8)
        transform_bm(lg, M(-0.3 + (k % 3) * 0.3 + (0.15 if k == 3 else 0), 0, 0.2 + (0.33 if k == 3 else 0), ry=math.pi / 2) @ Matrix.Translation((0, 0, -0.9)))
        paint_by(lg, lambda f, l: pal("ahsap", 1) if abs(f.normal.x) < 0.7 else _halka_renk(l.vert.co.y, l.vert.co.z - (0.2 + (0.33 if k == 3 else 0)), 0.2))
        ld.append(lg)
    add(merge(ld), "Koru_Odun_Destesi", kx + 2.5, ky - 3.0, hide=True)
    # fidanlar (B5): 3 fidan, destek kazığı + ip
    fd = []
    for k, (dx, dy) in enumerate(((-1.2, 0.8), (0.0, 1.6), (1.2, 0.8))):
        st = bm_cyl(0.025, 0.02, 0.9, sides=5)
        transform_bm(st, Matrix.Translation((dx, dy, -0.05)))
        paint_part(st, "ahsap", lo=2, hi=1)
        lf = _blob(Vector((dx, dy, 0.95)), 0.28, sub=2, squash=1.2, jit=0.12, seed=70 + k)
        paint_by(lf, lambda f, l: mix(pal("yaprak", 1), pal("yaprak", 0), 0.5))
        pk = box(0.04, 0.04, 1.1, x=dx + 0.12, y=dy, z=-0.1)
        flat(pk, pal("ahsap", 2))
        mound = bm_lathe([(0.35, -0.02), (0.25, 0.05), (0.0, 0.07)], sides=8, close_bottom=True)
        transform_bm(mound, Matrix.Translation((dx, dy, 0)))
        flat(mound, mix(pal("ahsap", 2), pal("kil", 2), 0.4))
        fd += [st, lf, pk, mound]
    add(merge(fd), "Koru_Fidanlar", kx, ky + 1.5, hide=True)
    # baltalık kütüğü + balta (sahne anlatısı: ormancının yeri)
    bk = kutuk_bm(r=0.32, h=0.45)
    ax_ = box(0.05, 0.05, 0.8, origin="center")
    transform_bm(ax_, M(0.05, 0.0, 0.75, rx=math.radians(-25)))
    flat(ax_, pal("ahsap", 1))
    hd = bm_prism([(0, 0), (0.22, -0.06), (0.24, 0.1), (0.0, 0.08)], -0.02, 0.02)
    transform_bm(hd, M(0.05, 0.0, 1.05, rx=math.radians(-25), yaw=math.pi / 2))
    flat(hd, c_iron(0.85))
    add(merge([bk, ax_, hd]), "SM_Koru_Baltalik", kx - 2.6, ky - 2.4)
    # koru alanını temizle (doğa yerleri)
    yc = bpy.data.collections.get(YER_COL)
    if yc:
        for o in list(yc.objects):
            if (Vector((o.location.x, o.location.y)) - KORU_C).length < 8.5:
                bpy.data.objects.remove(o, do_unlink=True)
    print("Koru kuruldu:", len(col.objects), "obje")
    return root

def export_tarla():
    os.makedirs(TR_EXPORT, exist_ok=True)
    out = []
    tr = [o for o in bpy.data.collections[TR_COL].all_objects if o.name.startswith("Traktor") or o.name.startswith("SM_Traktor")]
    root = bpy.data.objects["Traktor"]
    keep = root.matrix_world.copy()
    root.matrix_world = Matrix.Identity(4)                    # traktör kendi orijininde dışa aktarılır (prefab)
    bpy.context.view_layer.update()
    try:
        out.append(_fbx(os.path.join(TR_EXPORT, "Traktor.fbx"), tr))
    finally:
        root.matrix_world = keep
    al = [o for o in bpy.data.collections[TR_COL].objects if o.name.startswith("Alet_")]
    keepl = {o.name: o.location.copy() for o in al}
    for o in al:
        o.location = (0, 0, 0)
    try:
        out.append(_fbx(os.path.join(TR_EXPORT, "Traktor_Aletler.fbx"), al))
    finally:
        for o in al:
            o.location = keepl[o.name]
    for cn, fn in ((TARLA_COL, "Bereket_Tarlasi.fbx"), (KORU_COL, "Ormanci_Korusu.fbx")):
        objs = list(bpy.data.collections[cn].all_objects)
        hidden = [o for o in objs if o.hide_get()]
        for o in hidden:
            o.hide_set(False)
        try:
            out.append(_fbx(os.path.join(EXPORT_DIR, fn), objs))
        finally:
            for o in hidden:
                o.hide_set(True)
    return out
