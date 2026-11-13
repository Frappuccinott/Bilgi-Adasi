# Kazan boşaltma (sıfırlama) kolu — ocağın sağ ön köşesinde, kazanın yanında. Çekilince kazan boşalır.
# Mekanik: Scripts/Order/World/CauldronDrain.cs (kol öne çekilir, bekler, yaylanarak geri gelir)
# Referans: ArtSource/Referans/kazan_kolu.md · Kurallar: 09-tema-ve-model-kurallari.md
#
#   blender --background --factory-startup --python kazan_kolu.py [-- --render]
#
# Parçalar (hiyerarşi bir kademe):
#   Kazan_Kolu (boş, pivot: yatağın tabanı, ön yüz -Y)
#   ├─ SM_Kazan_Kolu_Yatak   ahşap takoz + iki demir yanak + pim (sabit)
#   └─ Kazan_Kolu_Cubuk      demir çubuk + ahşap tutamak; pivot pimde, X ekseninde döner (öne = -Y)

import math
import os
import random
import sys

import bpy
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
_lib = os.path.join(HERE, "ba_lib.py")
exec(compile(open(_lib, encoding="utf-8").read(), _lib, "exec"), globals())

EXPORT = os.path.join(REPO, "Assets", "_Project", "Art", "Esya", "Kazan_Bosaltma_Kolu.fbx")
COL = "Kazan_Kolu"
PIN_Z = 0.075          # pim yüksekliği (takozun üstünden)
ROD_LEN = 0.42         # pimden tutamağın altına

def wood(bm, seed, lo=2, hi=0):
    r = random.Random(seed)
    k = 1.0 + r.uniform(-0.05, 0.05)
    zs = [v.co.z for v in bm.verts]
    z0, z1 = min(zs), max(zs)
    span = max(z1 - z0, 1e-6)
    a, b = pal("ahsap", lo), pal("ahsap", hi)
    paint_by(bm, lambda f, l: tuple(min(x * k, 1) for x in mix(a, b, (l.vert.co.z - z0) / span)[:3]) + (1.0,))
    return bm

def iron(bm, light=0.6):
    paint_by(bm, lambda f, l: mix(demir(0.32), demir(light), max(0.0, f.normal.z) * 0.5 + 0.2))
    return bm

def bracket_bm():
    parts = []
    block = bm_box(0.16, 0.13, 0.05, bevel=0.01)
    jitter(block, 0.003, seed=7)
    wood(block, 11, lo=2, hi=1)
    parts.append((block, None))
    plate = bm_box(0.12, 0.09, 0.008)
    iron(plate)
    parts.append((plate, Matrix.Translation((0, 0, 0.05))))
    for sx in (-1, 1):   # yanaklar: çubuk aralarında döner
        cheek = bm_box(0.012, 0.05, 0.06, bevel=0.003)
        iron(cheek, 0.7)
        parts.append((cheek, Matrix.Translation((sx * 0.026, 0, 0.055))))
    pin = bm_cyl(0.008, 0.008, 0.075, sides=6)
    iron(pin, 0.8)
    parts.append((pin, Matrix.Translation((0, 0, 0)) @ Matrix.Translation((-0.0375, 0, PIN_Z)) @ Matrix.Rotation(math.radians(90), 4, "Y")))
    # perçin başları (plakada dört köşe)
    for sx in (-1, 1):
        for sy in (-1, 1):
            rv = bm_cyl(0.007, 0.005, 0.006, sides=5)
            iron(rv, 0.8)
            parts.append((rv, Matrix.Translation((sx * 0.048, sy * 0.034, 0.058))))
    return bm_merge(*parts)

def rod_bm():
    """Pivot pimde (0,0,0); çubuk yukarı uzanır, tepede hafif öne kıvrılır; ahşap tutamak + demir yüzük."""
    parts = []
    secs = []
    n = 6
    t = 0.011
    for i in range(n + 1):
        f = i / n
        z = f * ROD_LEN
        y = -0.04 * (f ** 2.2)            # tepeye doğru öne (oyuncuya) kıvrılır
        secs.append([Vector((-t, y - t, z)), Vector((t, y - t, z)), Vector((t, y + t, z)), Vector((-t, y + t, z))])
    rod = bm_sweep(secs)
    bevel_all(rod, 0.003)
    iron(rod, 0.65)
    parts.append((rod, None))
    # alt kuyruk (pimin altında kısa karşı ağırlık)
    tail = bm_box(0.022, 0.022, 0.05, origin="center")
    iron(tail, 0.55)
    parts.append((tail, Matrix.Translation((0, 0.004, -0.02))))
    # göbek: pimin geçtiği yuvarlak
    hub = bm_cyl(0.02, 0.02, 0.03, sides=8)
    iron(hub, 0.7)
    parts.append((hub, Matrix.Translation((-0.015, 0, 0)) @ Matrix.Rotation(math.radians(90), 4, "Y")))
    # demir yüzük + ahşap tutamak (tombul, elle tutulur)
    top = Vector((0, -0.04, ROD_LEN))
    ring = bm_cyl(0.018, 0.018, 0.018, sides=8)
    iron(ring, 0.8)
    parts.append((ring, Matrix.Translation(top)))
    grip = bm_lathe([(0.0, 0.0), (0.019, 0.0), (0.024, 0.02), (0.026, 0.06), (0.022, 0.1), (0.014, 0.115), (0.0, 0.118)], sides=8)
    wood(grip, 21, lo=1, hi=0)
    parts.append((grip, Matrix.Translation(top + Vector((0, 0, 0.016)))))
    return bm_merge(*parts)

def build():
    clear_collection(COL)
    col = collection(COL)
    root = empty("Kazan_Kolu", col, size=0.2)
    yat = to_object(bracket_bm(), "SM_Kazan_Kolu_Yatak", col)
    yat.parent = root
    cub = to_object(rod_bm(), "Kazan_Kolu_Cubuk", col, loc=(0, 0, PIN_Z))
    cub.parent = root
    print("tri:", tri_count([yat, cub]))
    return col

def export(col):
    os.makedirs(os.path.dirname(EXPORT), exist_ok=True)
    for o in bpy.data.objects:
        try:
            o.select_set(o.name in col.objects)
        except RuntimeError:
            pass
    for o in col.objects:
        if o.type == "MESH":
            m = o.modifiers.new("tri", "TRIANGULATE")
            m.keep_custom_normals = True
    bpy.ops.export_scene.fbx(
        filepath=EXPORT, use_selection=True, object_types={"MESH", "EMPTY"},
        apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        mesh_smooth_type="OFF", use_mesh_modifiers=True, colors_type="LINEAR", bake_anim=False, add_leaf_bones=False)
    for o in col.objects:
        if o.type == "MESH":
            o.modifiers.remove(o.modifiers["tri"])
    print("export:", EXPORT)

if __name__ == "__main__":
    if bpy.app.background:
        for o in list(bpy.data.objects):
            bpy.data.objects.remove(o, do_unlink=True)
    c = build()
    export(c)
    if bpy.app.background:
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(REPO, "ArtSource", "Blender", "kazan_kolu.blend"))
        if "--render" in sys.argv:
            scn = bpy.context.scene
            cam = bpy.data.objects.new("Kontrol_Kamera", bpy.data.cameras.new("Kontrol_Kamera"))
            scn.collection.objects.link(cam)
            cam.location = (0.55, -0.75, 0.55)
            d = Vector((0, 0, 0.25)) - cam.location
            cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
            scn.camera = cam
            sun = bpy.data.objects.new("Gunes", bpy.data.lights.new("Gunes", "SUN"))
            sun.data.energy = 3.0
            sun.rotation_euler = (math.radians(50), 0, math.radians(30))
            scn.collection.objects.link(sun)
            world = scn.world or bpy.data.worlds.new("W")
            scn.world = world
            world.use_nodes = True
            bg = next(n for n in world.node_tree.nodes if n.type == "BACKGROUND")
            bg.inputs["Color"].default_value = hex2rgb("#BDEFFA")
            scn.render.resolution_x, scn.render.resolution_y = 600, 700
            scn.render.filepath = os.path.join(os.environ.get("BA_RENDER_DIR", REPO), "kazan_kolu_kontrol.png")
            bpy.ops.render.render(write_still=True)
            print("render:", scn.render.filepath)
