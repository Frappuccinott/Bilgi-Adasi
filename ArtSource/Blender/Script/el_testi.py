# Elde tutma önizlemesi (yalnız Blender; Unity'ye gitmez).
# Araştırma değerleri: viewmodel FOV 55°, eşya kameraya göre sağ +0,22 / aşağı 0,19 / ileri 0,42 m, içe dönük ~-14°.
# Önce ba_lib.py ve esya_tasinir.py (katalog kurulmuş olmalı).
import math
import bpy
from mathutils import Vector

def el_testi(item, cam_loc=(0.9, 0.2, 1.62), cam_rot=(86, 0, -8), right=0.22, down=0.19, fwd=0.42, yaw=-14, fov=55):
    col = collection("_ElTesti")
    clear_collection("_ElTesti")
    cam = bpy.data.objects["Camera"]
    cam.location = cam_loc
    cam.rotation_euler = [math.radians(a) for a in cam_rot]
    cam.data.sensor_fit = "VERTICAL"
    cam.data.lens_unit = "FOV"
    cam.data.angle_y = math.radians(fov)
    cam.data.clip_start = 0.01
    bpy.context.view_layer.update()
    src = bpy.data.objects[item + "_Model"]
    grip = bpy.data.objects[item + "_Tutma_Noktasi"].location.z
    ob = bpy.data.objects.new("_El_" + item, src.data)
    col.objects.link(ob)
    p = cam.matrix_world @ Vector((right, -down, -fwd))
    ob.location = p - Vector((0, 0, grip))
    ob.rotation_euler = (0, 0, cam.rotation_euler.z + math.radians(yaw))
    return ob
