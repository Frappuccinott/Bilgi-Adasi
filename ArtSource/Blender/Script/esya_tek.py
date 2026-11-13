# Seçili taşınabilir eşyaları (CATALOG adları) tek başına kurar, iskeletler ve FBX'e aktarır.
# Bütün atölyeyi yeniden kurmadan yeni bir malzeme eklemek için:
#   blender -b --factory-startup --python ArtSource/Blender/Script/esya_tek.py -- Malzeme_C Malzeme_C6H12O6 [--onizleme <klasör>]
# Çıktı: Assets/_Project/Art/Esya/<Ad>.fbx (yeniden_kur.py ile aynı ayarlar). --onizleme verilirse her eşyanın PNG önizlemesi.
import os
import sys
import math
import bpy
from mathutils import Vector

BASE = os.path.dirname(os.path.abspath(__file__))
ORDER = ("ba_lib.py", "atolye_kabuk.py", "atolye_esya.py", "atolye_ic.py", "esya_tasinir.py",
         "anim_ortak.py", "simya_tezgahi.py", "anim_atolye.py")
for f in ORDER:
    p = os.path.join(BASE, f)
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), globals())

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
preview_dir = None
if "--onizleme" in argv:
    i = argv.index("--onizleme")
    preview_dir = argv[i + 1]
    argv = argv[:i] + argv[i + 2:]
names = argv
assert names, "Eşya adı verilmedi"

reset_anim()
report = build_catalog(only=set(names))
for r in report:
    print("[esya_tek] kuruldu:", r)


def render_preview(name, out_dir):
    """Workbench + köşe rengi: oyundaki M_Stilize'ye en yakın hızlı görünüm."""
    os.makedirs(out_dir, exist_ok=True)
    sc = bpy.context.scene
    root = bpy.data.objects[name]
    for o in bpy.data.objects:
        o.hide_render = not (o == root or o.parent == root)
        if o.type == "MESH" and len(o.data.color_attributes):
            ca = o.data.color_attributes
            ca.active_color_index = 0
            ca.render_color_index = 0
    c = Vector(root.location) + Vector((0, 0, 0.11))
    cam_data = bpy.data.cameras.new("OnizlemeKam")
    cam_data.lens = 70
    cam = bpy.data.objects.new("OnizlemeKam", cam_data)
    sc.collection.objects.link(cam)
    cam.location = c + Vector((0.22, -0.62, 0.26))
    cam.rotation_euler = (c - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.camera = cam
    try:
        sc.render.engine = "BLENDER_WORKBENCH"
    except TypeError:
        pass
    sh = sc.display.shading
    sh.light = "STUDIO"
    sh.color_type = "VERTEX"
    sh.show_shadows = True
    sh.show_cavity = False
    sc.world = sc.world or bpy.data.worlds.new("W")
    sc.render.resolution_x = 640
    sc.render.resolution_y = 640
    sc.render.film_transparent = False
    sc.render.filepath = os.path.join(out_dir, name + ".png")
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam, do_unlink=True)
    for o in bpy.data.objects:
        o.hide_render = False


if preview_dir:
    for n in names:
        render_preview(n, preview_dir)

paths = []
for n in names:
    arm = rig_item(n)
    paths.append(export_rig(arm, EXPORT_ROOT + "\\Esya\\" + n + ".fbx"))
for p in paths:
    print("[esya_tek] FBX:", p)
