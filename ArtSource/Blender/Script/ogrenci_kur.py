# Öğrenci FPS kollarını arka planda sıfırdan kurar (canlı Blender'a dokunmaz).
#   blender -b --factory-startup --python ArtSource/Blender/Script/ogrenci_kur.py -- [onizleme <klasör>] [disa]
# onizleme: pozları oyuncu kamerasından (FOV 70, 16:9) PNG'ye çizer. disa: FBX'i Unity'ye yazar.
import os
import sys
import bpy

BASE = os.path.dirname(os.path.abspath(__file__))
for f in ("ba_lib.py", "anim_ortak.py", "ogrenci_kol.py"):
    p = os.path.join(BASE, f)
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), globals())

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
for o in list(bpy.data.objects):
    bpy.data.objects.remove(o, do_unlink=True)
arm, ob, P = build_ogrenci_kol()

if "onizleme" in args:
    out = args[args.index("onizleme") + 1]
    os.makedirs(out, exist_ok=True)
    sc = bpy.context.scene
    try:
        sc.render.engine = "BLENDER_EEVEE"
    except TypeError:
        sc.render.engine = "BLENDER_EEVEE_NEXT"
    w = bpy.data.worlds.new("Dunya")
    sc.world = w
    w.use_nodes = True
    bg = next(n for n in w.node_tree.nodes if n.type == "BACKGROUND")
    bg.inputs[0].default_value = pal("gok", 0)
    bg.inputs[1].default_value = 0.9
    sun = bpy.data.objects.new("Gunes", bpy.data.lights.new("Gunes", "SUN"))
    sc.collection.objects.link(sun)
    sun.rotation_euler = (0.9, 0.3, 2.6)
    sun.data.energy = 3.0
    sc.render.resolution_x, sc.render.resolution_y = 960, 540
    sc.view_settings.view_transform = "Standard"
    shots = [("Kol_Bekle_Dongu", 1), ("Kol_Uzan", 12), ("Kol_Tut_Dongu", 1), ("Kol_Kaldir", 6), ("Kol_Bosta", 1)]
    extra = [a for a in args if a.startswith("klip=")]
    if extra:
        shots = [(e.split("=")[1].split(":")[0], int(e.split(":")[1])) for e in extra]
    for clip, fr in shots:
        preview_clip(arm, clip, fr)
        sc.render.filepath = os.path.join(out, "%s_%02d.png" % (clip, fr))
        bpy.ops.render.render(write_still=True)
    # yan görünüş (model kontrolü)
    preview_clip(arm, "Kol_Bekle_Dongu", 1)
    cam2 = bpy.data.objects.new("Kam_Yan", bpy.data.cameras.new("Kam_Yan"))
    sc.collection.objects.link(cam2)
    cam2.data.lens = 50
    import math
    from mathutils import Vector
    tgt = Vector((-0.2, -0.38, -0.22))
    cam2.location = tgt + Vector((-0.35, -0.35, 0.25))
    cam2.rotation_euler = (tgt - cam2.location).to_track_quat("-Z", "Y").to_euler()
    sc.camera = cam2
    sc.render.filepath = os.path.join(out, "yan_sag_el.png")
    bpy.ops.render.render(write_still=True)
    sc.camera = bpy.data.objects["Kam_FPS"]
    sc.collection.objects.unlink(cam2)

if "disa" in args:
    print("FBX:", export_ogrenci_kol())

clear_preview(arm)
bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(BASE, "..", "ogrenci.blend"), compress=True)
print("OGRENCI_KUR_TAMAM")
