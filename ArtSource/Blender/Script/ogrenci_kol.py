# Bilgi Adası — Öğrencinin FPS kolları: tek deri kaplı mesh + iskelet + klipler.
# Referans ve kararlar: ArtSource/Referans/ogrenci_kol.md · Kurallar: 09-tema-ve-model-kurallari.md
# Önce ba_lib.py ve anim_ortak.py. Yarışma süresince yazıldı (0'dan üret).
#
# Uzay: iskeletin kökü = oyuncu kamerası (göz). Blender'da ileri -Y, yukarı +Z, karakterin sağı -X
# (FBX -Z ileri / Y yukarı ile Unity'de ileri +Z, sağ +X olur). Unity'de prefab kameranın çocuğu, yerel 0.
# Kol önce "kol uzayında" kurulur: omuz orijinde, +y parmaklara, +z elin sırtı, -x başparmak (sağ kol).
# Sol kol sağın X'e göre aynasıdır. Kafes → bir kademe Catmull-Clark → ağırlıklar kafesten taşınır.

import math
import bmesh
import bpy
from mathutils import Vector, Matrix, Euler

RIG = "Ogrenci_Kol_Rig"
MESH = "Ogrenci_Kol"
EXPORT_PATH = EXPORT_ROOT + r"\Animasyon\Ogrenci_Kol_Rig.fbx"

ELBOW_Y, WRIST_Y = 0.28, 0.53
SHOULDER_R = Vector((-0.19, 0.05, -0.27))      # sağ omuz, kameraya göre
REST_PITCH, REST_YAW = 30.0, 10.0              # dinlenme: aşağı ve içe

FINGERS = ("Isaret", "Orta", "Yuzuk", "Serce")
# avuç ön kenarındaki parmak sınırları (x), başparmak -x tarafında
KNUCKLE_XS = (-0.044, -0.021, 0.003, 0.025, 0.044)
FINGER_LEN = {"Isaret": (0.036, 0.025, 0.021), "Orta": (0.040, 0.027, 0.022),
              "Yuzuk": (0.037, 0.025, 0.021), "Serce": (0.029, 0.020, 0.018)}
FINGER_HZ = {"Isaret": 0.0122, "Orta": 0.0126, "Yuzuk": 0.0118, "Serce": 0.0104}
FINGER_FAN = {"Isaret": -4.0, "Orta": 0.0, "Yuzuk": 3.5, "Serce": 8.0}     # derece, +x'e açılır

# köşe etiketleri → renk
T_KUMAS, T_KIVRIM_IC, T_KIVRIM_DIS, T_MANSET, T_TEN, T_BOGUM, T_UC = range(1, 8)


# ---------------------------------------------------------------- kafes
class Cage:
    def __init__(self):
        self.bm = bmesh.new()
        self.dl = self.bm.verts.layers.deform.verify()
        self.tl = self.bm.verts.layers.int.new("etiket")
        self.groups = []
        self.creases = []          # (v1, v2, değer)

    def gi(self, name):
        if name not in self.groups:
            self.groups.append(name)
        return self.groups.index(name)

    def vert(self, co, w, tag):
        v = self.bm.verts.new(co)
        for n, val in w.items():
            v[self.dl][self.gi(n)] = val
        v[self.tl] = tag
        return v

    def ring(self, pts, w, tag):
        return [self.vert(p, w, tag) for p in pts]

    def bridge(self, a, b):
        n = len(a)
        for i in range(n):
            j = (i + 1) % n
            self.bm.faces.new((a[i], a[j], b[j], b[i]))

    def crease_ring(self, r, val):
        n = len(r)
        for i in range(n):
            self.creases.append((r[i], r[(i + 1) % n], val))


def ring_ellipse(y, rx, rz, cz=0.0):
    """12 köşe; sıra: üst 5 (x -→+), +x yan, alt 5 (x +→-), -x yan — el kafesiyle aynı düzen."""
    out = []
    for a in (150, 120, 90, 60, 30, 0, -30, -60, -90, -120, -150, 180):
        r = math.radians(a)
        out.append(Vector((rx * math.cos(r), y, cz + rz * math.sin(r))))
    return out


def ring_box(y, xs, zt, zb, side, arch):
    top = [Vector((x, y, zt + arch * (1 - (x / side) ** 2))) for x in xs]
    bot = [Vector((x, y, zb)) for x in reversed(xs)]
    mid = (zt + zb) * 0.5
    return top + [Vector((side, y, mid))] + bot + [Vector((-side, y, mid))]


def _frame(d, up):
    d = d.normalized()
    u = (up - d * up.dot(d)).normalized()
    s = d.cross(u).normalized()
    return d, u, s


def digit(cage, base, d, up, hx, hz, stations, cap_bone, bones_out, names):
    """Parmak: base = avuçtaki 4 köşe (döngü sırası). stations: [(uzaklık, ölçek, ağırlık, etiket)].
    Kesit köşeleri base köşelerinin (s, u) işaretine göre eşlenir → burulma yok."""
    d, u, s = _frame(d, up)
    c0 = sum((v.co for v in base), Vector()) / 4
    signs = []
    for v in base:
        rel = v.co - c0
        signs.append((1 if rel.dot(s) >= 0 else -1, 1 if rel.dot(u) >= 0 else -1))
    assert len(set(signs)) == 4, "parmak köşe eşlemesi belirsiz: %s" % (signs,)
    prev = base
    for dist, sc, w, tag in stations:
        c = c0 + d * dist
        pts = [c + s * (hx * sc * ss) + u * (hz * sc * su) for ss, su in signs]
        cur = cage.ring(pts, w, tag)
        cage.bridge(prev, cur)
        prev = cur
    cage.bm.faces.new(prev)
    # kemikler: eklem uzaklıkları names sırasıyla
    return c0, d, u


def build_arm(cage, side, M, bones):
    """Bir kol (kol uzayında kurulur, M ile kameraya taşınır). bones listesine kemik ekler."""
    S = "_" + side
    UST, ON, EL = "Kol_Ust" + S, "Kol_On" + S, "El" + S
    first_vert = len(cage.bm.verts)

    # --- kol: sweatshirt (sıvanmış) + çıplak ön kol
    spec = [
        (-0.03, 0.056, 0.054, {UST: 1}, T_KUMAS),
        (0.15, 0.057, 0.054, {UST: 1}, T_KUMAS),
        (ELBOW_Y, 0.052, 0.050, {UST: .5, ON: .5}, T_KUMAS),
        (0.33, 0.056, 0.053, {ON: 1}, T_KUMAS),
        (0.352, 0.063, 0.058, {ON: 1}, T_KIVRIM_DIS),
        (0.369, 0.054, 0.050, {ON: 1}, T_KIVRIM_IC),
        (0.386, 0.061, 0.057, {ON: 1}, T_KIVRIM_DIS),
        (0.401, 0.053, 0.049, {ON: 1}, T_KIVRIM_IC),
        (0.420, 0.060, 0.056, {ON: 1}, T_MANSET),
        (0.441, 0.058, 0.054, {ON: 1}, T_MANSET),
        (0.444, 0.044, 0.037, {ON: 1}, T_TEN),
        (0.488, 0.040, 0.031, {ON: 1}, T_TEN),
        (WRIST_Y, 0.035, 0.024, {ON: .5, EL: .5}, T_TEN),
    ]
    rings = []
    for k, (y, rx, rz, w, tag) in enumerate(spec):
        r = cage.ring(ring_ellipse(y, rx, rz, cz=0.004 if tag == T_TEN else 0.0), w, tag)
        if rings:
            cage.bridge(rings[-1], r)
        rings.append(r)
    cage.bm.faces.new(list(reversed(rings[0])))
    cage.crease_ring(rings[9], 0.9)      # manşet ön kenarı
    cage.crease_ring(rings[10], 1.0)      # kumaş → ten basamağı

    # --- avuç
    h1 = cage.ring(ring_box(0.565, (-0.038, -0.019, 0, 0.019, 0.038), 0.018, -0.016, 0.043, 0.004), {EL: 1}, T_TEN)
    h2 = cage.ring(ring_box(0.600, (-0.043, -0.021, 0, 0.021, 0.043), 0.019, -0.015, 0.047, 0.004), {EL: 1}, T_TEN)
    h3 = cage.ring(ring_box(0.635, KNUCKLE_XS, 0.017, -0.013, 0.046, 0.003), {EL: 1}, T_BOGUM)
    cage.bridge(rings[-1], h1)
    # başparmak tabanı yüzü (h1[11], h1[0], h2[0], h2[11]) köprüden çıkarılır
    n = 12
    for i in range(n):
        j = (i + 1) % n
        if (i, j) == (11, 0):
            continue
        cage.bm.faces.new((h1[i], h1[j], h2[j], h2[i]))
    cage.bridge(h2, h3)
    cage.bm.faces.new((h3[4], h3[5], h3[6]))
    cage.bm.faces.new((h3[10], h3[11], h3[0]))
    # boğum halkası: avuç %50, komşu parmaklar %50
    for idx in range(12):
        v = h3[idx]
        if idx in (5, 11):
            continue
        xi = idx if idx < 5 else 10 - idx          # üst/alt köşenin sınır numarası 0..4
        adj = [FINGERS[k] for k in (xi - 1, xi) if 0 <= k < 4]
        v[cage.dl][cage.gi(EL)] = 0.5
        for f in adj:
            v[cage.dl][cage.gi("Parmak_%s_1%s" % (f, S))] = 0.5 / len(adj)

    # --- dört parmak
    hand_up = Vector((0, 0, 1))
    for i, f in enumerate(FINGERS):
        base = [h3[i], h3[i + 1], h3[9 - i], h3[10 - i]]
        l1, l2, l3 = FINGER_LEN[f]
        L = l1 + l2 + l3
        fan = math.radians(FINGER_FAN[f])
        droop = math.radians(6)
        d = Vector((math.sin(fan), math.cos(fan) * math.cos(droop), -math.sin(droop)))
        hx = (KNUCKLE_XS[i + 1] - KNUCKLE_XS[i]) * 0.40
        hz = FINGER_HZ[f]
        b1, b2, b3 = ("Parmak_%s_%d%s" % (f, k, S) for k in (1, 2, 3))
        st = [
            (l1 * 0.5, 1.00, {b1: 1}, T_TEN),
            (l1, 0.95, {b1: .5, b2: .5}, T_BOGUM),
            (l1 + l2 * 0.5, 0.92, {b2: 1}, T_TEN),
            (l1 + l2, 0.88, {b2: .5, b3: .5}, T_TEN),
            (L - 0.004, 0.80, {b3: 1}, T_UC),
            (L + 0.004, 0.45, {b3: 1}, T_UC),
        ]
        c0, dd, uu = digit(cage, base, d, hand_up, hx, hz, st, b3, bones, None)
        bones += [(b1, c0, c0 + dd * l1, EL, uu, True),
                  (b2, c0 + dd * l1, c0 + dd * (l1 + l2), b1, uu, True),
                  (b3, c0 + dd * (l1 + l2), c0 + dd * (L + 0.004), b2, uu, True)]

    # --- başparmak
    base = [h1[11], h1[0], h2[0], h2[11]]
    d = Vector((-0.55, 0.78, -0.40))
    up = Vector((-0.45, 0.0, 1.0))
    t1, t2, t3 = ("Parmak_Bas_%d%s" % (k, S) for k in (1, 2, 3))
    st = [
        (0.016, 1.00, {t1: 1}, T_TEN),
        (0.030, 0.95, {t1: .5, t2: .5}, T_BOGUM),
        (0.040, 0.92, {t2: 1}, T_TEN),
        (0.051, 0.88, {t2: .5, t3: .5}, T_TEN),
        (0.069, 0.78, {t3: 1}, T_UC),
        (0.075, 0.42, {t3: 1}, T_UC),
    ]
    c0, dd, uu = digit(cage, base, d, up, 0.0125, 0.0115, st, t3, bones, None)
    bones += [(t1, c0, c0 + dd * 0.030, EL, uu, True),
              (t2, c0 + dd * 0.030, c0 + dd * 0.051, t1, uu, True),
              (t3, c0 + dd * 0.051, c0 + dd * 0.075, t2, uu, True)]

    # --- kol kemikleri + tutma noktası (deform yok)
    up = Vector((0, 0, 1))
    bones[:0] = [(UST, Vector((0, 0, 0)), Vector((0, ELBOW_Y, 0)), "Kok", up, True),
                 (ON, Vector((0, ELBOW_Y, 0)), Vector((0, WRIST_Y, 0)), UST, up, True),
                 (EL, Vector((0, WRIST_Y, 0)), Vector((0, 0.635, 0.002)), ON, up, True)]
    bones.append(("Tutma" + S, Vector((-0.004, 0.603, -0.046)), Vector((-0.004, 0.653, -0.046)), EL, up, False))

    # --- kameraya taşı
    cage.bm.verts.ensure_lookup_table()
    for v in list(cage.bm.verts)[first_vert:]:
        v.co = M @ v.co
    R3 = M.to_3x3()
    out = []
    for name, h, t, parent, upv, deform in bones:
        out.append((name, M @ h, M @ t, parent, (R3 @ upv).normalized(), deform))
    return out


def arm_matrix(side):
    base = Matrix.Rotation(math.pi, 4, "Z")                        # kol +y → kamera -Y (ileri)
    M = (Matrix.Translation(SHOULDER_R) @ Matrix.Rotation(math.radians(REST_YAW), 4, "Z")
         @ Matrix.Rotation(math.radians(REST_PITCH), 4, "X") @ base)
    if side == "L":
        M = Matrix.Scale(-1, 4, Vector((1, 0, 0))) @ M
    return M


# ---------------------------------------------------------------- renk
def grad3(group, t, lo=0.35, hi=0.5):
    if t < 0.5:
        return mix(pal(group, 1), pal(group, 2), (0.5 - t) * 2 * lo)
    return mix(pal(group, 1), pal(group, 0), (t - 0.5) * 2 * hi)


def paint_cage(cage):
    bm = cage.bm
    lay = bm.loops.layers.float_color.get("Col") or bm.loops.layers.float_color.new("Col")
    bm.normal_update()
    for f in bm.faces:
        for l in f.loops:
            v = l.vert
            t = (v.normal.z + 1) * 0.5
            tag = v[cage.tl]
            if tag in (T_KUMAS, T_KIVRIM_IC, T_KIVRIM_DIS, T_MANSET):
                c = grad3("dag", t, 0.4, 0.45)
                if tag == T_KIVRIM_IC:
                    c = mix(c, pal("dag", 2), 0.5)
                elif tag == T_KIVRIM_DIS:
                    c = mix(c, pal("dag", 0), 0.18)
                elif tag == T_MANSET:
                    c = mix(c, pal("dag", 2), 0.2)
            else:
                c = grad3("ten", t, 0.45, 0.55)
                if tag == T_BOGUM:
                    c = mix(c, pal("cati", 0), 0.10)
                elif tag == T_UC:
                    c = mix(c, pal("cati", 0), 0.14)
            l[lay] = c


# ---------------------------------------------------------------- iskelet
def make_skin_rig(bones):
    old = bpy.data.objects.get(RIG)
    if old:
        for c in list(old.children):
            me = c.data
            bpy.data.objects.remove(c, do_unlink=True)
            if me and me.users == 0:
                bpy.data.meshes.remove(me)
        bpy.data.objects.remove(old, do_unlink=True)
    ad = bpy.data.armatures.new(RIG)
    arm = bpy.data.objects.new(RIG, ad)
    anim_col().objects.link(arm)
    arm.show_in_front = True
    vl = bpy.context.view_layer
    for o in list(bpy.context.selected_objects):
        o.select_set(False)
    vl.objects.active = arm
    arm.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    eb = ad.edit_bones
    k = eb.new("Kok")
    k.head, k.tail = (0, 0, 0), (0, -0.08, 0)
    k.align_roll(Vector((0, 0, 1)))
    k.use_deform = False
    for name, h, t, parent, upv, deform in bones:
        b = eb.new(name)
        b.head, b.tail = h, t
        b.align_roll(upv)
        b.use_deform = deform
        b.parent = eb[parent]
        b.use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    arm.select_set(False)
    return arm


def build_mesh(arm, cage):
    me = bpy.data.meshes.new(MESH + "_Kafes")
    ec = cage.bm.edges.layers.float.get("crease_edge") or cage.bm.edges.layers.float.new("crease_edge")
    for a, b, val in cage.creases:
        e = cage.bm.edges.get((a, b))
        if e:
            e[ec] = val
    bmesh.ops.recalc_face_normals(cage.bm, faces=cage.bm.faces)
    paint_cage(cage)
    cage.bm.to_mesh(me)
    cage.bm.free()
    kafes = bpy.data.objects.new(MESH + "_Kafes", me)
    anim_col().objects.link(kafes)
    for g in cage.groups:
        kafes.vertex_groups.new(name=g)
    sub = kafes.modifiers.new("Yumusat", "SUBSURF")
    sub.levels = sub.render_levels = 1
    sub.use_creases = True
    dg = bpy.context.evaluated_depsgraph_get()
    ev = kafes.evaluated_get(dg)
    sm = bpy.data.meshes.new_from_object(ev, preserve_all_data_layers=True, depsgraph=dg)
    sm.name = MESH
    ob = bpy.data.objects.new(MESH, sm)
    anim_col().objects.link(ob)
    for g in cage.groups:                     # new_from_object grupları taşır; eksik kalırsa ekle
        if ob.vertex_groups.get(g) is None:
            ob.vertex_groups.new(name=g)
    bpy.data.objects.remove(kafes, do_unlink=True)
    bpy.data.meshes.remove(me)
    for p in sm.polygons:
        p.use_smooth = True
    sm.materials.clear()
    sm.materials.append(material_stilize())
    ob.parent = arm
    mod = ob.modifiers.new("Iskelet", "ARMATURE")
    mod.object = arm
    return ob


# ---------------------------------------------------------------- pozlar
def _eul(r):
    return Euler([math.radians(a) for a in r], "XYZ").to_quaternion()


def set_pose(arm, pose):
    """pose: {kemik: (rx, ry, rz)} ya da {"Kok_loc": (x, y, z)}; kemik yerel uzayı (X ekseni = bükme)."""
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.location = (0, 0, 0)
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.scale = (1, 1, 1)
    for k, v in pose.items():
        if k == "Kok_loc":
            arm.pose.bones["Kok"].location = v
        else:
            arm.pose.bones[k].rotation_quaternion = _eul(v)
    bpy.context.view_layer.update()


def bone_head(arm, name):
    return arm.matrix_world @ arm.pose.bones[name].head


def mirror_rot(r):
    return (r[0], -r[1], -r[2])


def hand_pose(side, curl, thumb=(0, 0, 0), spread=1.0):
    """curl: (k1, k2, k3) derece büküm (negatif = avuca). Serçe biraz daha bükük."""
    S = "_" + side
    p = {}
    for i, f in enumerate(FINGERS):
        extra = (0, 4, 8, 14)[i] * (1 if curl[0] < -5 else 0.3)
        sp = (-3, 0, 2, 5)[i] * spread
        p["Parmak_%s_1%s" % (f, S)] = (curl[0] - extra, 0, -sp if side == "R" else sp)
        p["Parmak_%s_2%s" % (f, S)] = (curl[1] - extra * 0.6, 0, 0)
        p["Parmak_%s_3%s" % (f, S)] = (curl[2], 0, 0)
    t1 = thumb
    p["Parmak_Bas_1" + S] = t1 if side == "R" else mirror_rot(t1)
    p["Parmak_Bas_2" + S] = (thumb[0] * 0.8, 0, 0)
    p["Parmak_Bas_3" + S] = (thumb[0] * 0.6, 0, 0)
    return p


def solve_arm(arm, side, target, end_bone, base_pose, wrist=(0, 0, 0), twist=0.0, fore_dir=None, iters=120):
    """Üst kol (x, y, z) + ön kol (x) açılarıyla end_bone başını hedefe getirir (kamera uzayı, m).
    4 serbestlik + ön kol yönü cezası: dirsek çemberinden ön kolu istenen yöne en yakın çözüm seçilir."""
    S = "_" + side
    ust, on, el = "Kol_Ust" + S, "Kol_On" + S, "El" + S
    p = [-20.0, 0.0, 0.0, 40.0]
    tgt = Vector(target)
    want = Vector(fore_dir).normalized() if fore_dir else None

    def cost(q):
        pose = dict(base_pose)
        r_ust = (q[0], q[1], q[2])
        r_on = (q[3], twist, 0)
        pose[ust] = r_ust if side == "R" else mirror_rot(r_ust)
        pose[on] = r_on if side == "R" else mirror_rot(r_on)
        pose[el] = wrist if side == "R" else mirror_rot(wrist)
        set_pose(arm, pose)
        c = (bone_head(arm, end_bone) - tgt).length_squared
        if want is not None:
            pb = arm.pose.bones[on]
            d = (arm.matrix_world.to_3x3() @ (pb.tail - pb.head)).normalized()
            c += 0.0006 * (d - want).length_squared
        return c, pose

    best, pose = cost(p)
    step = 16.0
    while step > 0.05 and iters > 0:
        moved = False
        for i in range(4):
            for sgn in (1, -1):
                q = list(p)
                q[i] += sgn * step
                c, ps = cost(q)
                if c < best:
                    best, p, pose, moved = c, q, ps, True
        if not moved:
            step *= 0.5
        iters -= 1
    set_pose(arm, pose)
    err = (bone_head(arm, end_bone) - tgt).length
    return {k: v for k, v in pose.items() if k in (ust, on, el)}, err


def mirror_pt(p):
    return (-p[0], p[1], p[2])


# Hedefler (kamera uzayı, Blender: x sola(+)/sağa(-), y ileri(-), z yukarı). Unity FOV 70 dikey, 16:9.
POSE_TARGETS = {
    # (hedef, uç kemik, bilek (x bük, y, z yan), ön kol burulması, ön kol yönü)
    # bekle: bilekler alt köşelerde, eller kadrajın altından yarım görünür, avuç aşağı
    "BEKLE": {"R": ((-0.20, -0.34, -0.265), "El_R", (-16, 0, -6), 14.0, (0.18, -1, 0.18)),
              "L": ((0.21, -0.32, -0.275), "El_L", (-20, 0, -4), 10.0, (-0.14, -1, 0.15))},
    # uzan: sağ el öne ve merkeze, avuç aşağı-ileri (bir şeye dokunma / kapı / kol)
    "UZAN": {"R": ((-0.11, -0.46, -0.15), "El_R", (-14, 0, 4), 6.0, (0.3, -1, 0.3)),
             "L": ((0.22, -0.28, -0.31), "El_L", (4, 0, -2), 8.0, (-0.12, -1, 0.1))},
    # tut: sağ el, el_testi.py ile aynı nokta (sağ +0,22 / aşağı 0,19 / ileri 0,42); avuç içe dönük
    "TUT": {"R": ((-0.22, -0.42, -0.19), "Tutma_R", (10, 0, 0), 68.0, (0.12, -1, 0.25)),
            "L": ((0.24, -0.15, -0.50), "El_L", (0, 0, 0), 0.0, (0, -0.3, -1))},
}

CURL_RELAX = (-24, -30, -18)
CURL_OPEN = (-2, -4, -3)
CURL_GRIP = (-62, -74, -46)


def build_poses(arm):
    poses = {}
    fingers = {
        "BEKLE": (hand_pose("R", CURL_RELAX, (-10, 0, 14), spread=0.4), hand_pose("L", (-30, -34, -20), (-12, 0, 16), spread=0.3)),
        "UZAN": (hand_pose("R", CURL_OPEN, (4, 0, -6), spread=1.6), hand_pose("L", CURL_RELAX, (-6, 0, 8))),
        "TUT": (hand_pose("R", CURL_GRIP, (-34, 10, 34)), hand_pose("L", CURL_RELAX, (-6, 0, 8))),
    }
    for key, spec in POSE_TARGETS.items():
        fr, fl = fingers[key]
        pose = {**fr, **fl}
        for side in ("R", "L"):
            tgt, end, wrist, twist, fdir = spec[side]
            part, err = solve_arm(arm, side, tgt, end, pose, wrist, twist, fdir)
            pose.update(part)
            print("poz %s %s hata=%.3f m" % (key, side, err))
        poses[key] = pose
    # gizli: kök aşağı, üst kollar sarkık (kadrajın tamamen altında)
    g = dict(poses["BEKLE"])
    g["Kok_loc"] = (0, 0, -0.42)
    poses["GIZLI"] = g
    # nefes (bekle/tut): sağ/sol farklı genlik (asimetri)
    for key in ("BEKLE", "TUT"):
        b = dict(poses[key])
        b["Kok_loc"] = (0, 0, 0.006)
        for S, k in (("_R", 1.0), ("_L", 1.6)):
            r = b["Kol_On" + S]
            b["Kol_On" + S] = (r[0] + 1.6 * k, r[1], r[2])
            e = b["El" + S]
            b["El" + S] = (e[0] - 1.0 * k, e[1], e[2])
        poses[key + "_NEFES"] = b
    # bas: uzan pozundan 3 cm ileri itme
    u = dict(poses["UZAN"])
    r = u["Kol_On_R"]
    u["Kol_On_R"] = (r[0] + 7, r[1], r[2])
    e = u["El_R"]
    u["El_R"] = (e[0] - 10, e[1], e[2])
    poses["UZAN_BAS"] = u
    set_pose(arm, {})
    return poses


def key_full(arm, frame, pose):
    set_pose(arm, pose)
    for pb in arm.pose.bones:
        pb.keyframe_insert("location", frame=frame)
        pb.keyframe_insert("rotation_quaternion", frame=frame)
        pb.keyframe_insert("scale", frame=frame)


def build_clips(arm, P):
    if arm.animation_data:
        for t in list(arm.animation_data.nla_tracks):
            arm.animation_data.nla_tracks.remove(t)
    for a in list(bpy.data.actions):
        if a.name.startswith("Kol_"):
            bpy.data.actions.remove(a)

    def seq(keys):
        return lambda a: [key_full(a, f, P[k]) for f, k in keys]

    new_clip(arm, "Kol_Bosta", 2, seq([(1, "GIZLI"), (2, "GIZLI")]))
    new_clip(arm, "Kol_Kaldir", 12, seq([(1, "GIZLI"), (12, "BEKLE")]))
    new_clip(arm, "Kol_Bekle_Dongu", 91, seq([(1, "BEKLE"), (46, "BEKLE_NEFES"), (91, "BEKLE")]), loop=True)
    new_clip(arm, "Kol_Uzan", 24, seq([(1, "BEKLE"), (8, "UZAN"), (12, "UZAN_BAS"), (15, "UZAN_BAS"), (24, "BEKLE")]))
    new_clip(arm, "Kol_Tut_Dongu", 91, seq([(1, "TUT"), (46, "TUT_NEFES"), (91, "TUT")]), loop=True)
    new_clip(arm, "Kol_Indir", 10, seq([(1, "BEKLE"), (10, "GIZLI")]))


# ---------------------------------------------------------------- önizleme kamerası
def fps_camera():
    cam = bpy.data.objects.get("Kam_FPS")
    if cam is None:
        cam = bpy.data.objects.new("Kam_FPS", bpy.data.cameras.new("Kam_FPS"))
        bpy.context.scene.collection.objects.link(cam)
    cam.location = (0, 0, 0)
    cam.rotation_euler = (math.radians(90), 0, math.radians(180))
    cam.data.sensor_fit = "VERTICAL"
    cam.data.lens_unit = "FOV"
    cam.data.angle_y = math.radians(70)
    cam.data.clip_start = 0.01
    sc = bpy.context.scene
    sc.camera = cam
    sc.render.resolution_x, sc.render.resolution_y = 1600, 900
    return cam


# ---------------------------------------------------------------- ana
def build_ogrenci_kol():
    cage = Cage()
    bones = []
    for side in ("R", "L"):
        bones += build_arm(cage, side, arm_matrix(side), [])
    arm = make_skin_rig(bones)
    ob = build_mesh(arm, cage)
    P = build_poses(arm)
    build_clips(arm, P)
    fps_camera()
    print("OGRENCI_KOL ucgen=%d kemik=%d" % (tri_count([ob]), len(arm.data.bones)))
    return arm, ob, P


def export_ogrenci_kol(path=EXPORT_PATH):
    arm = bpy.data.objects[RIG]
    return export_rig(arm, path)
