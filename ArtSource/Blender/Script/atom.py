# Bilgi Adası — terazi kefelerindeki atom modelleri (Atom_H, Atom_O, Atom_C, Atom_N, Atom_Ca)
# Şekil + renk kodu kavanoz mühürleriyle AYNI (esya_tasinir.atom_glyph): H küre · O halka · C küp · N piramit · Ca altıgen.
# Renk tek başına bilgi taşımaz (02 §4). Pivot = taban ortası; kefe yüzeyine oturur.
# Kullanım (Blender içinden, ba_lib.py + esya_tasinir.py'den sonra):
#   exec(open(".../atom.py", encoding="utf-8").read())  →  build_atoms(); export_atoms(<repo>)
# Dosyayı kaydetmez; geçici koleksiyon dışa aktarımdan sonra silinir.

import os
import bpy

ATOM_COL = "Atom_Gecici"
ATOM_R = 0.026          # glif yarıçapı → atom ~0,06 m (kefede 5'li sıra sığar)
ATOMS = ("H", "O", "C", "N", "Ca")


def build_atoms():
    clear_collection(ATOM_COL)
    col = collection(ATOM_COL)
    objs = []
    for i, e in enumerate(ATOMS):
        g = atom_glyph(e, ATOM_R)
        paint_flat(g, ELEM[e]())
        ob = to_object(g, "Atom_" + e, col, loc=(40.0 + i * 0.12, -10.0, 0.0), mat=material_stilize())
        _shape_for_scale(ob, e)
        objs.append(ob)
    return [(o.name, tri_count([o]), tuple(round(v, 3) for v in o.dimensions)) for o in objs]


def _shape_for_scale(ob, e):
    """Kefede yandan okunsun: O halkası ve Ca altıgeni dik durur (yüzü −Y = Unity +Z, kameraya),
    H tam küre olur; hepsi taban ortasına alınıp ~5,8 cm'ye ölçeklenir (C küpü 5 cm)."""
    import math
    from mathutils import Matrix
    me = ob.data
    if e in ("O", "Ca"):
        me.transform(Matrix.Rotation(math.radians(90), 4, "X"))
    if e == "H":
        me.transform(Matrix.Diagonal((1, 1, 1 / 0.7, 1)))
    xs = [v.co.x for v in me.vertices]
    ys = [v.co.y for v in me.vertices]
    zs = [v.co.z for v in me.vertices]
    size = max(max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs))
    me.transform(Matrix.Translation((-(max(xs) + min(xs)) / 2, -(max(ys) + min(ys)) / 2, -min(zs))))
    me.transform(Matrix.Scale((0.05 if e == "C" else 0.058) / size, 4))
    me.update()


def export_atoms(repo):
    out_dir = os.path.join(repo, "Assets", "_Project", "Art", "Atom")
    os.makedirs(out_dir, exist_ok=True)
    paths = []
    for e in ATOMS:
        ob = bpy.data.objects["Atom_" + e]
        old = ob.location.copy()
        ob.location = (0, 0, 0)
        for o in list(bpy.context.selected_objects):
            o.select_set(False)
        ob.select_set(True)
        bpy.context.view_layer.objects.active = ob
        path = os.path.join(out_dir, "Atom_%s.fbx" % e)
        bpy.ops.export_scene.fbx(
            filepath=path, use_selection=True, object_types={"MESH"},
            apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
            mesh_smooth_type="OFF", use_mesh_modifiers=True, colors_type="LINEAR", bake_anim=False, add_leaf_bones=False)
        ob.location = old
        ob.select_set(False)
        paths.append(path)
    return paths


def remove_atoms():
    clear_collection(ATOM_COL)
    c = bpy.data.collections.get(ATOM_COL)
    if c is not None:
        bpy.data.collections.remove(c)
