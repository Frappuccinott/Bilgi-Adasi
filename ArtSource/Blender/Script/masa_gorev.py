# Bilgi Adası — masa görevleri (Tepkime Türleri, Seviye 6–10) eşyaları
# Ulak güvercin (kanatları ayrı, menteşeli), mühürlü mektup (zarf), Lonca zarfı, masada açık kâğıt.
# Referans: kaya güvercini (Columba livia) — mavi-gri gövde, boyunda yeşil-mor yanardöner bant, kanatta iki koyu bant,
#   koyu kuyruk ucu, turuncu göz, koyu gaga + beyaz burun tümseği (cere), pembe ayak; boy ~32 cm, kanat açıklığı ~65 cm.
#   Ortaçağ mektubu: zarf değil katlanmış parşömen + ip + balmumu mühür ("letterlocking").
# Kurallar: 09-tema-ve-model-kurallari.md — renkler yalnızca paletten (pal/mix), köşe rengi, M_Stilize.
# Önce ba_lib.py çalıştırılmış olmalı.
#
# Eksenler (Blender): güvercin gagası -Y, yukarı +Z; sol kanat +X, sağ kanat -X.
# Unity sözleşmesi (ArtDeskView):
#   Guvercin                 boş kök, pivot ayakların arası (pervaza konar)
#   ├── Guvercin_Govde       gövde + baş + kuyruk + ayak (tek mesh)
#   ├── Kanat_Sol / Kanat_Sag   açık (süzülme) pozda; pivot omuz menteşesi
#   Zarf, Zarf_Lonca, Masa_Acik_Kagit   pivot taban ortası
# Her eşya ayrı FBX: Assets/_Project/Art/Atolye/MasaGorev/<Ad>.fbx (dünya kökünde).

import math
import os
import random
import bmesh
import bpy
from mathutils import Matrix, Vector

MG_COL = "Masa_Gorev"
MG_ORIGIN = Vector((14.0, 4.0, 0.0))   # atölyenin dışında, inceleme için dizilir
EXPORT_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(bpy.data.filepath))) if bpy.data.filepath else "",
                          "Assets", "_Project", "Art", "Atolye", "MasaGorev")


def _mg():
    return collection(MG_COL)


def _face_paint(bm, fn):
    """Yüz merkezine göre düz renk (kanat bantları keskin kalsın)."""
    layer = _color_layer(bm)
    for f in bm.faces:
        c = fn(f.calc_center_median(), f.normal)
        for l in f.loops:
            l[layer] = c
    return bm


def _sphere(r, sub=1):
    g = bmesh.new()
    bmesh.ops.create_icosphere(g, subdivisions=sub, radius=r)
    return g


# ------------------------------------------------------------------ renkler (palet)
def C_GOVDE():   return mix(pal("kaya", 1), pal("dag", 1), 0.25)          # mavi-gri
def C_ACIK():    return mix(pal("kaya", 0), pal("dag", 0), 0.30)          # göğüs / kanat örtüsü
def C_KOYU():    return mix(pal("kaya", 2), pal("dag", 2), 0.35)          # baş, birincil tüyler
def C_BANT():    return mix(demir(0.75), pal("kaya", 2), 0.25)            # kanat bandı, kuyruk ucu
def C_YESIL():   return mix(pal("yaprak", 2), pal("gok", 2), 0.35)       # boyun yanardöner
def C_MOR():     return mix(pal("cati", 2), pal("gok", 2), 0.55)          # boyun alt (mor-pas)
def C_AYAK():    return mix(pal("cati", 0), pal("ten", 0), 0.55)          # pembe ayak
def C_GOZ():     return pal("cati", 0)                                    # turuncu iris
def C_GAGA():    return mix(demir(0.6), pal("ten", 2), 0.2)
def C_CERE():    return pal("siva", 0)


# ------------------------------------------------------------------ GÜVERCİN: gövde
def guvercin_govde_bm():
    parts = []
    # gövde: damla (lathe Z ekseninde), sonra gaga -Y'ye bakacak şekilde yatırılır
    body = bm_lathe([(0.0, 0.0), (0.028, 0.012), (0.052, 0.045), (0.066, 0.095), (0.068, 0.14),
                     (0.058, 0.18), (0.036, 0.208), (0.0, 0.222)], sides=10)
    bmesh.ops.scale(body, vec=(0.92, 0.86, 1.0), verts=body.verts)        # biraz yassı (yanlardan)
    # lathe ekseni (kuyruk z=0 → göğüs z=0.222) -Y'ye; göğüs hafif yukarıda (tünemiş duruş ~12°)
    transform_bm(body, Matrix.Translation((0, 0.11, 0.10)) @ Matrix.Rotation(math.radians(90 - 12), 4, "X"))
    _face_paint(body, lambda p, n: C_ACIK() if (p.y < -0.05 and p.z < 0.12) else
                (C_GOVDE() if n.z > -0.4 else mix(C_GOVDE(), C_KOYU(), 0.4)))
    parts.append((body, None))

    # boyun: göğüsten başa kalın konik (yanardöner)
    neck = bm_cyl(0.036, 0.027, 0.075, sides=8)
    transform_bm(neck, Matrix.Translation((0, -0.088, 0.14)) @ Matrix.Rotation(math.radians(-28), 4, "X"))
    _face_paint(neck, lambda p, n: C_YESIL() if p.z > 0.165 else C_MOR())
    parts.append((neck, None))

    head = _sphere(0.036, 2)
    bmesh.ops.scale(head, vec=(0.9, 1.15, 0.95), verts=head.verts)
    transform_bm(head, Matrix.Translation((0, -0.125, 0.225)))
    _face_paint(head, lambda p, n: C_KOYU())
    parts.append((head, None))

    beak = bm_cyl(0.009, 0.003, 0.03, sides=6)
    transform_bm(beak, Matrix.Translation((0, -0.155, 0.218)) @ Matrix.Rotation(math.radians(98), 4, "X"))
    paint_flat(beak, C_GAGA())
    parts.append((beak, None))
    cere = _sphere(0.008, 1)
    bmesh.ops.scale(cere, vec=(1.1, 1.3, 0.7), verts=cere.verts)
    transform_bm(cere, Matrix.Translation((0, -0.152, 0.227)))
    paint_flat(cere, C_CERE())
    parts.append((cere, None))

    for sx in (-1, 1):   # göz: turuncu iris + koyu bebek
        iris = bm_cyl(0.009, 0.009, 0.004, sides=8)
        transform_bm(iris, Matrix.Translation((sx * 0.030, -0.135, 0.232)) @ Matrix.Rotation(math.radians(90 * sx), 4, "Y"))
        paint_flat(iris, C_GOZ())
        parts.append((iris, None))
        pup = bm_cyl(0.004, 0.004, 0.002, sides=6)
        transform_bm(pup, Matrix.Translation((sx * 0.0335, -0.135, 0.232)) @ Matrix.Rotation(math.radians(90 * sx), 4, "Y"))
        paint_flat(pup, demir(0.4))
        parts.append((pup, None))

    # kuyruk: yelpaze, ucu koyu bant
    tail = bmesh.new()
    pts = []
    for i in range(7):
        a = math.radians(-26 + i * (52 / 6))
        pts.append((math.sin(a) * 0.055, 0.0 + math.cos(a) * 0.11))
    verts_top = [tail.verts.new((0, 0.0, 0)), *[tail.verts.new((x, y, 0)) for x, y in pts]]
    for i in range(1, len(verts_top) - 1):
        tail.faces.new((verts_top[0], verts_top[i], verts_top[i + 1]))
    # tüy uçları: dilim aralarında küçük çentik (uç kenarı ikiye bölünüp ortası içeri çekilir)
    outer = [e for e in tail.edges if not any(v is verts_top[0] for v in e.verts) and len(e.link_faces) == 1]
    res_sub = bmesh.ops.subdivide_edges(tail, edges=outer, cuts=1)
    for v in [g for g in res_sub["geom_inner"] if isinstance(g, bmesh.types.BMVert)]:
        v.co *= 0.96
    res = bmesh.ops.extrude_face_region(tail, geom=list(tail.faces))
    bmesh.ops.translate(tail, vec=(0, 0, 0.008), verts=[g for g in res["geom"] if isinstance(g, bmesh.types.BMVert)])
    transform_bm(tail, Matrix.Translation((0, 0.07, 0.095)) @ Matrix.Rotation(math.radians(10), 4, "X"))
    _face_paint(tail, lambda p, n: C_BANT() if p.y > 0.155 else C_GOVDE())
    parts.append((tail, None))

    # bacak + parmaklar (pembe)
    for sx in (-1, 1):
        leg = bm_cyl(0.006, 0.005, 0.05, sides=5)
        transform_bm(leg, Matrix.Translation((sx * 0.022, -0.005, 0.0)))
        paint_flat(leg, C_AYAK())
        parts.append((leg, None))
        for k, ang in enumerate((-35, 0, 35, 180)):
            toe = bm_box(0.006, 0.03 if ang != 180 else 0.018, 0.005, origin="bottom")
            transform_bm(toe, Matrix.Translation((sx * 0.022, -0.005, 0.0)) @ Matrix.Rotation(math.radians(ang), 4, "Z")
                         @ Matrix.Translation((0, -0.014 if ang != 180 else -0.008, 0)))
            paint_flat(toe, C_AYAK())
            parts.append((toe, None))
    return bm_merge(*parts)


# ------------------------------------------------------------------ GÜVERCİN: kanat (açık poz)
def kanat_bm(side):
    """side +1 sol (+X), -1 sağ (-X). Izgara: u açıklık (omuz→uç), v kord (hücum kenarı→firar kenarı)."""
    span, nu, nv = 0.30, 16, 6

    def le(u):   # hücum kenarı (y, - ileri)
        return -0.045 + 0.075 * u ** 2.2

    def te(u):   # firar kenarı + birincil tüy çentikleri
        base = 0.075 + 0.02 * math.sin(u * math.pi) - 0.035 * u ** 3
        jag = 0.014 * abs(math.sin(u * math.pi * 5.0)) if u > 0.42 else 0.006 * abs(math.sin(u * math.pi * 8.0))
        return base + jag

    bm = bmesh.new()
    grid = []
    for i in range(nu + 1):
        u = i / nu
        row = []
        for j in range(nv + 1):
            v = j / nv
            y = le(u) + (te(u) - le(u)) * v
            # hafif kavis: kordda kambur, açıklıkta yukarı (dihedral)
            z = 0.010 * math.sin(v * math.pi) * (1 - u) + 0.03 * u ** 2
            row.append(bm.verts.new((side * u * span, y, z)))
        grid.append(row)
    for i in range(nu):
        for j in range(nv):
            q = (grid[i][j], grid[i + 1][j], grid[i + 1][j + 1], grid[i][j + 1])
            bm.faces.new(q if side > 0 else tuple(reversed(q)))
    res = bmesh.ops.extrude_face_region(bm, geom=list(bm.faces))
    bmesh.ops.translate(bm, vec=(0, 0, -0.012), verts=[g for g in res["geom"] if isinstance(g, bmesh.types.BMVert)])
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))

    def color(p, n):
        u = abs(p.x) / span
        vv = (p.y - le(u)) / max(1e-4, te(u) - le(u))
        if n.z < -0.5:
            return mix(C_ACIK(), C_GOVDE(), 0.3)            # kanat altı açık
        if u > 0.55:
            return C_KOYU() if vv > 0.2 else mix(C_GOVDE(), C_KOYU(), 0.5)   # birincil tüyler
        if 0.50 < vv < 0.64 or 0.76 < vv < 0.90:
            return C_BANT()                                  # iki koyu kanat bandı
        return C_GOVDE() if vv < 0.3 else C_ACIK()
    _face_paint(bm, color)
    return bm


def build_guvercin():
    o = MG_ORIGIN
    root = empty("Guvercin", _mg(), loc=tuple(o), kind="ARROWS", size=0.12)
    body = to_object(guvercin_govde_bm(), "Guvercin_Govde", _mg(), loc=tuple(o), sharp_angle=50)
    parent_keep(body, root)
    shoulder = Vector((0.045, -0.06, 0.155))
    for name, side in (("Kanat_Sol", 1), ("Kanat_Sag", -1)):
        w = to_object(kanat_bm(side), name, _mg(), loc=tuple(o + Vector((side * shoulder.x, shoulder.y, shoulder.z))), sharp_angle=50)
        parent_keep(w, root)
    return root


# ------------------------------------------------------------------ MEKTUP (katlanmış parşömen + ip + mühür)
def _letter_bm(seed, guild):
    r = random.Random(seed)
    w, d, t = 0.20, 0.13, 0.014
    parts = []
    paper = mix(pal("siva", 0), pal("kil", 0), 0.45)
    body = bm_box(w, d, t, bevel=0.002)
    jitter(body, 0.0012, seed=seed)
    paint_by(body, lambda f, l: paper if f.normal.z > 0.5 else mix(paper, pal("kil", 1), 0.5))
    parts.append((body, None))
    # üst kat: üçgen kapak (mektup katlanıp kilitlenmiş)
    flap = bmesh.new()
    a = flap.verts.new((-w / 2 + 0.004, -d / 2 + 0.004, t))
    b = flap.verts.new((w / 2 - 0.004, -d / 2 + 0.004, t))
    c = flap.verts.new((0.0, d * 0.18, t + 0.0015))
    flap.faces.new((a, b, c))
    res = bmesh.ops.extrude_face_region(flap, geom=list(flap.faces))
    bmesh.ops.translate(flap, vec=(0, 0, 0.002), verts=[g for g in res["geom"] if isinstance(g, bmesh.types.BMVert)])
    paint_flat(flap, mix(paper, pal("siva", 1), 0.25))
    parts.append((flap, None))
    # ip ya da Lonca şeridi
    band_c = pal("altin", 1) if guild else mix(pal("kil", 2), pal("ahsap", 1), 0.5)
    band_w = 0.022 if guild else 0.006
    for rot in ((0,), (90,)) if not guild else ((0,),):
        bw = bm_box(w + 0.006 if rot[0] == 0 else band_w, band_w if rot[0] == 0 else d + 0.006, t + 0.006, origin="bottom")
        transform_bm(bw, Matrix.Translation((0, 0, -0.002)))
        paint_flat(bw, band_c)
        parts.append((bw, None))
    # balmumu mühür: düzensiz kenarlı disk + kabartma halka + simge (damla = ustanın işareti)
    wax = mix(pal("cati", 2), demir(0.8), 0.25) if not guild else mix(pal("cati", 2), pal("altin", 2), 0.35)
    rr = 0.024 if guild else 0.019
    seal = bm_cyl(rr, rr * 0.92, 0.006, sides=12, bevel=0.0015)
    jitter(seal, 0.0015, seed=seed + 1, axes=(1, 1, 0))
    transform_bm(seal, Matrix.Translation((0, d * 0.05, t + 0.003)))
    paint_flat(seal, wax)
    parts.append((seal, None))
    ring = bm_lathe([(rr * 0.62, 0), (rr * 0.72, 0), (rr * 0.72, 0.0016), (rr * 0.62, 0.0016)], sides=12)
    transform_bm(ring, Matrix.Translation((0, d * 0.05, t + 0.009)))
    paint_flat(ring, mix(wax, pal("cati", 0), 0.25) if not guild else pal("altin", 0))
    parts.append((ring, None))
    drop = bm_lathe([(0, 0), (rr * 0.28, 0.0), (rr * 0.18, 0.0016), (0, 0.0016)], sides=8)
    bmesh.ops.scale(drop, vec=(0.8, 1.35, 1), verts=drop.verts)
    transform_bm(drop, Matrix.Translation((0, d * 0.05, t + 0.009)))
    paint_flat(drop, mix(wax, demir(0.6), 0.35))
    parts.append((drop, None))
    return bm_merge(*parts)


def build_zarflar():
    o = MG_ORIGIN
    z1 = to_object(_letter_bm(7101, False), "Zarf", _mg(), loc=tuple(o + Vector((0.45, 0, 0))), sharp_angle=40)
    z2 = to_object(_letter_bm(7102, True), "Zarf_Lonca", _mg(), loc=tuple(o + Vector((0.80, 0, 0))), sharp_angle=40)
    return z1, z2


# ------------------------------------------------------------------ MASADA AÇIK KÂĞIT (kenarları kıvrık + mürekkep satırları)
def _open_paper_bm(seed):
    r = random.Random(seed)
    w, d, nx, ny = 0.30, 0.40, 10, 12
    bm = bmesh.new()
    grid = []
    for i in range(nx + 1):
        row = []
        for j in range(ny + 1):
            x = -w / 2 + w * i / nx
            y = -d / 2 + d * j / ny
            ex = abs(2 * i / nx - 1)
            ey = abs(2 * j / ny - 1)
            z = 0.002 + 0.010 * max(0.0, ex - 0.75) ** 1.5 * 6 + 0.012 * max(0.0, ey - 0.8) ** 1.5 * 6
            row.append(bm.verts.new((x, y, z)))
        grid.append(row)
    for i in range(nx):
        for j in range(ny):
            bm.faces.new((grid[i][j], grid[i + 1][j], grid[i + 1][j + 1], grid[i][j + 1]))
    res = bmesh.ops.extrude_face_region(bm, geom=list(bm.faces))
    bmesh.ops.translate(bm, vec=(0, 0, -0.0015), verts=[g for g in res["geom"] if isinstance(g, bmesh.types.BMVert)])
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    paper = mix(pal("siva", 0), pal("kil", 0), 0.35)
    _face_paint(bm, lambda p, n: paper if n.z > 0 else mix(paper, pal("kil", 1), 0.4))
    parts = [(bm, None)]
    ink = mix(demir(0.55), pal("ahsap", 2), 0.4)
    yy = d / 2 - 0.05
    for k in range(9):          # yazı satırları (okunmaz; yalnız doku hissi)
        if k in (2, 6):
            yy -= 0.018
        ln = r.uniform(0.16, 0.24) if k else 0.12
        line = bm_box(ln, 0.0035, 0.0006, origin="bottom")
        transform_bm(line, Matrix.Translation((-w / 2 + 0.03 + ln / 2, yy, 0.0022)))
        paint_flat(line, ink)
        parts.append((line, None))
        yy -= 0.032
    return bm_merge(*parts)


def build_kagit():
    o = MG_ORIGIN
    return to_object(_open_paper_bm(7103), "Masa_Acik_Kagit", _mg(), loc=tuple(o + Vector((1.25, 0, 0))), sharp_angle=40)


# ------------------------------------------------------------------ kur + dışa aktar
def build_masa_gorev():
    clear_collection(MG_COL)
    g = build_guvercin()
    z1, z2 = build_zarflar()
    k = build_kagit()
    return [g, z1, z2, k]


def _select_tree(root):
    for o in list(bpy.context.selected_objects):
        o.select_set(False)
    stack = [root]
    while stack:
        ob = stack.pop()
        ob.select_set(True)
        stack.extend(ob.children)


def export_masa_gorev(out_dir=None):
    out_dir = out_dir or EXPORT_DIR
    os.makedirs(out_dir, exist_ok=True)
    paths = []
    for name in ("Guvercin", "Zarf", "Zarf_Lonca", "Masa_Acik_Kagit"):
        root = bpy.data.objects[name]
        old = root.location.copy()
        root.location = (0, 0, 0)
        bpy.context.view_layer.update()
        _select_tree(root)
        bpy.context.view_layer.objects.active = root
        path = os.path.join(out_dir, name + ".fbx")
        bpy.ops.export_scene.fbx(
            filepath=path, use_selection=True, object_types={"MESH", "EMPTY"},
            apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
            mesh_smooth_type="OFF", use_mesh_modifiers=True, colors_type="LINEAR", bake_anim=False, add_leaf_bones=False)
        root.location = old
        paths.append(path)
    for o in list(bpy.context.selected_objects):
        o.select_set(False)
    return paths
