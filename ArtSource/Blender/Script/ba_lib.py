# Bilgi Adası — Blender yardımcı kütüphanesi (stilize low-poly, köşe rengi)
# Kurallar: BilgiAdasi_Docs/01_Tasarim/09-tema-ve-model-kurallari.md
# Yarışma süresince yazıldı (0'dan üret). Blender 5.x.
#
# Kullanım (Blender içinden):
#   p = r"<repo>/ArtSource/Blender/Script/ba_lib.py"
#   exec(compile(open(p, encoding="utf-8").read(), p, "exec"), globals())

import math
import random
import bmesh
import bpy
from mathutils import Vector, Matrix

# ---------------------------------------------------------------- palet (09 §3)
def hex2rgb(h):
    h = h.lstrip("#")
    srgb = [int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]
    # köşe rengi lineer saklanır (export: colors_type=LINEAR)
    lin = [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in srgb]
    return (lin[0], lin[1], lin[2], 1.0)

PAL = {
    # grup: (aydınlık, orta, gölge)
    "cimen":   ("#A6E03C", "#6CC02E", "#2F8A3A"),
    "yaprak":  ("#7ED63B", "#3FA83A", "#1F6E3A"),
    "altin":   ("#FFD23F", "#F7A800", "#D07A00"),
    "gok":     ("#BDEFFA", "#4FCBEA", "#2A9FD0"),
    "su":      ("#C8F6FF", "#35BDEB", "#1E86C4"),
    "dag":     ("#9AD3D6", "#6FA9B8", "#4F7F96"),
    "kaya":    ("#D5DEE6", "#A3B1BE", "#6E8090"),
    "cati":    ("#FF7A3D", "#E8602A", "#B5431C"),
    "ahsap":   ("#B07A4E", "#8A5A3A", "#5C3A26"),
    # türetilmiş (09 §3): kaya + altın/ahşap karışımı
    "siva":    ("#DDDCC5", "#A7A09B", "#686B70"),
    "bakir":   ("#D77A45", "#9F4E2B", "#773D23"),
    "kil":     ("#CBB595", "#987D6A", "#62524B"),
    # türetilmiş (09 §3): öğrencinin eli — sıva/kil + çatı karışımı
    "ten":     ("#EBB58F", "#BE8769", "#834C38"),
}

def demir(k=0.55):
    """Dövme demir: kaya gölgenin koyusu (aynı tonun koyusu; saf siyah değil)."""
    c = pal("kaya", 2)
    return (c[0] * k, c[1] * k, c[2] * k * 1.05, 1.0)

def pal(group, tone=1):
    """tone: 0 aydınlık, 1 orta, 2 gölge"""
    return hex2rgb(PAL[group][tone])

def mix(a, b, t):
    return tuple(a[i] + (b[i] - a[i]) * t for i in range(4))

# ---------------------------------------------------------------- sahne
def collection(name, parent=None):
    col = bpy.data.collections.get(name)
    if col is None:
        col = bpy.data.collections.new(name)
        (parent or bpy.context.scene.collection).children.link(col)
    return col

def clear_collection(name):
    col = bpy.data.collections.get(name)
    if col is None:
        return
    for ob in list(col.all_objects):
        me = ob.data if ob.type == "MESH" else None
        bpy.data.objects.remove(ob, do_unlink=True)
        if me is not None and me.users == 0:
            bpy.data.meshes.remove(me)

def material_stilize():
    mat = bpy.data.materials.get("M_Stilize")
    if mat:
        return mat
    mat = bpy.data.materials.new("M_Stilize")
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    attr = nt.nodes.new("ShaderNodeVertexColor")
    attr.layer_name = "Col"
    attr.location = (-300, 200)
    nt.links.new(attr.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.85
    return mat

def material_named(name, base_hex, emission=0.0, alpha=1.0):
    """Ek malzemeler (09 §7: su, fener, köpük, duman, cam). Yine köşe rengini çarpar."""
    mat = bpy.data.materials.get(name)
    if mat:
        return mat
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Base Color"].default_value = hex2rgb(base_hex)
    bsdf.inputs["Roughness"].default_value = 0.25
    if emission > 0:
        bsdf.inputs["Emission Color"].default_value = hex2rgb(base_hex)
        bsdf.inputs["Emission Strength"].default_value = emission
    if alpha < 1.0:
        bsdf.inputs["Alpha"].default_value = alpha
        mat.surface_render_method = "BLENDED"
    return mat

# ---------------------------------------------------------------- bmesh yardımcıları
def _color_layer(bm):
    return bm.loops.layers.float_color.get("Col") or bm.loops.layers.float_color.new("Col")

def paint_gradient(bm, c_bottom, c_top, axis=2, noise=0.0, seed=0):
    """Alttan üste koyudan açığa geçiş (09 §3). noise: yüz başına hafif ton oynaması."""
    rnd = random.Random(seed)
    lay = _color_layer(bm)
    if not bm.verts:
        return
    lo = min(v.co[axis] for v in bm.verts)
    hi = max(v.co[axis] for v in bm.verts)
    span = max(hi - lo, 1e-6)
    for f in bm.faces:
        k = 1.0 + (rnd.uniform(-noise, noise) if noise else 0.0)
        for l in f.loops:
            t = (l.vert.co[axis] - lo) / span
            c = mix(c_bottom, c_top, t)
            l[lay] = (min(c[0] * k, 1), min(c[1] * k, 1), min(c[2] * k, 1), 1.0)

def paint_by(bm, fn):
    """fn(face, loop) -> renk. Yüze/konuma göre boyama (iç sıva / dış taş gibi)."""
    lay = _color_layer(bm)
    bm.normal_update()
    for f in bm.faces:
        for l in f.loops:
            l[lay] = fn(f, l)

def bm_sweep(sections, cap=True):
    """sections: her biri aynı sayıda noktadan oluşan kapalı kesitler (Vector/tuple).
    Kesitler arası dörtgen yüzler + uç kapakları. Kiriş, kiremit sırası, korkuluk vb."""
    bm = bmesh.new()
    rings = [[bm.verts.new(p) for p in sec] for sec in sections]
    n = len(sections[0])
    for a, b in zip(rings, rings[1:]):
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((a[i], a[j], b[j], b[i]))
    if cap:
        bm.faces.new(list(reversed(rings[0])))
        bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm

def bm_prism(poly2d, y0, y1, plane="xz"):
    """2B çokgeni (u, z) düzleminde y0→y1 kalınlığında prizma yap."""
    bm = bmesh.new()
    a = [bm.verts.new((u, y0, z)) for (u, z) in poly2d]
    b = [bm.verts.new((u, y1, z)) for (u, z) in poly2d]
    n = len(poly2d)
    bm.faces.new(a)
    bm.faces.new(list(reversed(b)))
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((a[i], b[i], b[j], a[j]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm

def bm_wall(outer, holes, thick):
    """(u, z) düzleminde delikli duvar: dış yüz y=0, iç yüz y=thick."""
    bm = bmesh.new()
    edges = []
    for lp in [outer] + list(holes):
        vs = [bm.verts.new((x, 0.0, z)) for (x, z) in lp]
        for i in range(len(vs)):
            edges.append(bm.edges.new((vs[i], vs[(i + 1) % len(vs)])))
    res = bmesh.ops.triangle_fill(bm, use_beauty=True, use_dissolve=False, edges=edges)
    faces = [g for g in res["geom"] if isinstance(g, bmesh.types.BMFace)]
    ex = bmesh.ops.extrude_face_region(bm, geom=faces)
    vv = [g for g in ex["geom"] if isinstance(g, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, vec=(0, thick, 0), verts=vv)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm

def pointed_arch(cx, w, z0, spring, rise, n=6):
    """Sivri kemerli açıklık çokgeni (u, z): sol alt → tepe → sağ alt. z0 = eşik/denizlik."""
    r = w * 0.8
    lc = cx - w / 2 + r
    rc = cx + w / 2 - r
    top = math.sqrt(max(r * r - (cx - lc) ** 2, 1e-9))
    k = rise / top
    a_end = math.acos((cx - lc) / r)          # sol yayın tepe açısı (sol merkeze göre)
    a_end_r = math.pi - a_end                  # sağ yayın tepe açısı (sağ merkeze göre) — simetrik
    pts = [(cx - w / 2, z0)]
    for i in range(n + 1):
        a = math.pi - (math.pi - a_end) * i / n
        pts.append((lc + r * math.cos(a), spring + r * math.sin(a) * k))
    for i in range(1, n + 1):
        a = a_end_r * (1 - i / n)
        pts.append((rc + r * math.cos(a), spring + r * math.sin(a) * k))
    pts.append((cx + w / 2, z0))
    return pts

def transform_bm(bm, mat):
    bmesh.ops.transform(bm, matrix=mat, verts=bm.verts)
    return bm

def paint_flat(bm, color):
    lay = _color_layer(bm)
    for f in bm.faces:
        for l in f.loops:
            l[lay] = color

def jitter(bm, amount, seed=0, axes=(1, 1, 1)):
    """Cetvelle çizilmiş görünmesin: köşeleri hafifçe oynat."""
    rnd = random.Random(seed)
    for v in bm.verts:
        v.co.x += rnd.uniform(-amount, amount) * axes[0]
        v.co.y += rnd.uniform(-amount, amount) * axes[1]
        v.co.z += rnd.uniform(-amount, amount) * axes[2]

def bevel_all(bm, width, segments=1):
    if width <= 0:
        return
    bmesh.ops.bevel(bm, geom=list(bm.edges), offset=width, segments=segments,
                    profile=0.5, affect="EDGES", clamp_overlap=True)

def bm_box(sx, sy, sz, bevel=0.0, seg=1, origin="bottom"):
    """origin: 'bottom' → pivot taban ortasında, 'center' → merkezde."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=(sx, sy, sz), verts=bm.verts)
    if origin == "bottom":
        bmesh.ops.translate(bm, vec=(0, 0, sz / 2), verts=bm.verts)
    bevel_all(bm, bevel, seg)
    return bm

def bm_cyl(r1, r2, h, sides=8, cap=True, bevel=0.0):
    """Taban z=0'da koni/silindir. r1 alt, r2 üst yarıçap."""
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=cap, cap_tris=False, segments=sides,
                          radius1=r1, radius2=r2, depth=h)
    bmesh.ops.translate(bm, vec=(0, 0, h / 2), verts=bm.verts)
    if bevel > 0:
        rim = [e for e in bm.edges if any(abs(f.normal.z) > 0.7 for f in e.link_faces)]
        bmesh.ops.bevel(bm, geom=rim, offset=bevel, segments=1, affect="EDGES", clamp_overlap=True)
    return bm

def bm_lathe(profile, sides=10, close_top=True, close_bottom=True):
    """profile: [(r, z), ...] alttan üste. Kavanoz, şişe, kazan gibi dönel nesneler."""
    bm = bmesh.new()
    rings = []
    POLE = 1e-5
    for (r, z) in profile:
        if r < POLE:                       # eksen üstünde: tek kutup noktası (üst üste binen halka yerine)
            rings.append(bm.verts.new((0.0, 0.0, z)))
            continue
        ring = []
        for i in range(sides):
            a = 2 * math.pi * i / sides
            ring.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r, z)))
        rings.append(ring)
    for k in range(len(rings) - 1):
        a, b = rings[k], rings[k + 1]
        a_pole, b_pole = not isinstance(a, list), not isinstance(b, list)
        if a_pole and b_pole:
            continue
        for i in range(sides):
            j = (i + 1) % sides
            if a_pole:
                bm.faces.new((a, b[j], b[i]))
            elif b_pole:
                bm.faces.new((a[i], a[j], b))
            else:
                bm.faces.new((a[i], a[j], b[j], b[i]))
    if close_bottom and isinstance(rings[0], list):
        bm.faces.new(list(reversed(rings[0])))
    if close_top and isinstance(rings[-1], list):
        bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm

def bm_merge(*parts):
    """Birden çok bmesh'i tek bmesh'te birleştir (her parça kendi rengini korur)."""
    out = bmesh.new()
    lay_out = _color_layer(out)
    for bm, mat in parts:
        lay_in = bm.loops.layers.float_color.get("Col")
        vmap = {}
        for v in bm.verts:
            vmap[v] = out.verts.new(mat @ v.co if mat else v.co)
        for f in bm.faces:
            try:
                nf = out.faces.new([vmap[v] for v in f.verts])
            except ValueError:
                continue
            nf.smooth = f.smooth
            if lay_in:
                for l_new, l_old in zip(nf.loops, f.loops):
                    l_new[lay_out] = l_old[lay_in]
        bm.free()
    return out

def clean_bm(bm):
    """Dışa aktarım öncesi temizlik: üst üste binen köşeleri birleştir, sıfır alanlı yüz/kenarları sil.
    (Unity sıfır alanlı ya da kendini kesen çokgeni atar → görünür delik.)"""
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=1e-6)
    bmesh.ops.dissolve_degenerate(bm, dist=1e-7, edges=list(bm.edges))
    loose = [v for v in bm.verts if not v.link_faces]
    if loose:
        bmesh.ops.delete(bm, geom=loose, context="VERTS")

def to_object(bm, name, col, loc=(0, 0, 0), rot=(0, 0, 0), sharp_angle=35, smooth=True, mat=None):
    """bmesh → obje. Yumuşak gölgeleme + açıya göre keskin kenar (09 §3, ~35°)."""
    me = bpy.data.meshes.new(name)
    clean_bm(bm)
    bm.normal_update()
    bm.to_mesh(me)
    bm.free()
    if smooth:
        for p in me.polygons:
            p.use_smooth = True
        if sharp_angle:
            me.set_sharp_from_angle(angle=math.radians(sharp_angle))
    me.materials.append(mat or material_stilize())
    ob = bpy.data.objects.new(name, me)
    ob.location = loc
    ob.rotation_euler = [math.radians(a) for a in rot]
    col.objects.link(ob)
    return ob

def empty(name, col, loc=(0, 0, 0), rot=(0, 0, 0), kind="PLAIN_AXES", size=0.3):
    ob = bpy.data.objects.new(name, None)
    ob.empty_display_type = kind
    ob.empty_display_size = size
    ob.location = loc
    ob.rotation_euler = [math.radians(a) for a in rot]
    col.objects.link(ob)
    return ob

def _world_now(ob):
    """Objenin dünya matrisi, sahne güncellenmeden de doğru: matrix_basis her zaman günceldir,
    matrix_world ise yeni oluşturulan objede view_layer.update()'e kadar birim matris kalır."""
    m = ob.matrix_basis.copy()
    if ob.parent is not None:
        m = _world_now(ob.parent) @ ob.matrix_parent_inverse @ m
    return m

def parent_keep(child, parent):
    """Dünya konumunu koruyarak bağla. Yeni oluşturulmuş ebeveynde matrix_world bayat olabildiği için
    ebeveynin dünya matrisi matrix_basis'ten hesaplanır (aksi hâlde FBX'te çocuk yerel konumu = dünya konumu olur)."""
    child.parent = parent
    child.matrix_parent_inverse = _world_now(parent).inverted()

def tri_count(objs):
    n = 0
    for ob in objs:
        if ob.type == "MESH":
            n += sum(len(p.vertices) - 2 for p in ob.data.polygons)
    return n

def apply_transforms(ob):
    """Dışa aktarmadan önce ölçek 1, dönüş 0 (09 §7)."""
    mw = ob.matrix_world.copy()
    if ob.type == "MESH":
        ob.data.transform(Matrix.LocRotScale(None, mw.to_quaternion(), mw.to_scale()))
    ob.rotation_euler = (0, 0, 0)
    ob.scale = (1, 1, 1)
