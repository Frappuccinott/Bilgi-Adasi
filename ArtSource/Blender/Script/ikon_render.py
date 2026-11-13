# Envanter ikonları: her taşınabilir eşya için 256×256 şeffaf PNG.
# Araştırma: aynı ortografik kamera + sabit ölçek (eşyalar birbirine göre doğru boyda), 3/4 açı
# (X 18° yukarıdan, Z 30° dönük), anahtar + dolgu + kenar (rim) ışığı. Çıktı: Assets/_Project/UI/Ikonlar/
# Önce ba_lib.py ve esya_tasinir.py (katalog kurulmuş olmalı).
import math
import os
import bpy
from mathutils import Vector

ICON_DIR = r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\Assets\_Project\UI\Ikonlar"
ICON_SCALE = 0.36            # ortografik ölçek: en büyük kap (~0,31 m) sığar; aletler kendi ölçeğiyle

def _setup_icon_scene():
    sc = bpy.data.scenes.get("Ikon") or bpy.data.scenes.new("Ikon")
    sc.render.engine = bpy.context.scene.render.engine
    sc.render.resolution_x = sc.render.resolution_y = 256
    sc.render.film_transparent = True
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGBA"
    sc.view_settings.view_transform = bpy.context.scene.view_settings.view_transform
    sc.world = bpy.data.worlds.get("IkonDunya") or bpy.data.worlds.new("IkonDunya")
    sc.world.use_nodes = True
    bg = next(n for n in sc.world.node_tree.nodes if n.type == "BACKGROUND")
    bg.inputs["Color"].default_value = hex2rgb("#DDDCC5")
    bg.inputs["Strength"].default_value = 0.55
    for o in list(sc.collection.objects):
        sc.collection.objects.unlink(o)
    cam = bpy.data.objects.get("Ikon_Kamera")
    if cam is None:
        cam = bpy.data.objects.new("Ikon_Kamera", bpy.data.cameras.new("Ikon_Kamera"))
    cam.data.type = "ORTHO"
    sc.collection.objects.link(cam)
    sc.camera = cam
    def light(name, rot, energy, color):
        lo = bpy.data.objects.get(name)
        if lo is None:
            lo = bpy.data.objects.new(name, bpy.data.lights.new(name, "SUN"))
        lo.data.energy = energy
        lo.data.color = color
        lo.rotation_euler = [math.radians(a) for a in rot]
        sc.collection.objects.link(lo)
    light("Ikon_Anahtar", (50, 0, -35), 3.0, (1.0, 0.95, 0.86))
    light("Ikon_Dolgu", (60, 0, 70), 0.8, (0.85, 0.92, 1.0))
    light("Ikon_Kenar", (-60, 0, 160), 2.2, (1.0, 0.9, 0.75))
    return sc, cam

def render_icons(names=None):
    os.makedirs(ICON_DIR, exist_ok=True)
    sc, cam = _setup_icon_scene()
    done = []
    for name, fn, row in CATALOG:
        if names and name not in names:
            continue
        src = bpy.data.objects[name + "_Model"]
        ob = bpy.data.objects.new("_Ikon_" + name, src.data)
        sc.collection.objects.link(ob)
        dims = src.dimensions
        size = max(dims.x, dims.y, dims.z)
        ob.location = (0, 0, -dims.z / 2)
        ob.rotation_euler = (0, 0, math.radians(-30))
        cam.data.ortho_scale = max(ICON_SCALE, size * 1.18) if row == 4 else ICON_SCALE if size < ICON_SCALE * 0.9 else size * 1.15
        d = Vector((0, -1, math.tan(math.radians(18)))).normalized() * 3.0
        cam.location = d
        cam.rotation_euler = (math.radians(90 - 18), 0, 0)
        sc.render.filepath = os.path.join(ICON_DIR, name + ".png")
        bpy.ops.render.render(write_still=True, scene=sc.name)
        sc.collection.objects.unlink(ob)
        bpy.data.objects.remove(ob)
        done.append(name)
    return done
