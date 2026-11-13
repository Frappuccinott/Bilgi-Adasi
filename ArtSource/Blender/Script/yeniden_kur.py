# Atölyeyi scriptlerden sıfırdan kurar, bütün FBX'leri dışa aktarır ve atolye.blend'i kaydeder.
# Canlı Blender'a dokunmadan arka planda çalıştırılır:
#   blender -b ArtSource/Blender/atolye.blend --python ArtSource/Blender/Script/yeniden_kur.py
# Sıra: ba_lib → atolye_kabuk → atolye_esya → atolye_ic → esya_tasinir → anim_ortak → simya_tezgahi → anim_atolye
import os
import sys
import time
import bpy

BASE = os.path.dirname(os.path.abspath(__file__))
ORDER = ("ba_lib.py", "atolye_kabuk.py", "atolye_esya.py", "atolye_ic.py", "esya_tasinir.py",
         "anim_ortak.py", "simya_tezgahi.py", "anim_atolye.py")

t0 = time.time()
for f in ORDER:
    p = os.path.join(BASE, f)
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), globals())

reset_anim()
build_all()
build_core()
build_interior()
build_catalog()
res = build_tezgah()
build_anims(res)
paths = export_all()
for r in [o for o in bpy.data.objects if o.type == "ARMATURE"]:
    clear_preview(r)
bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(BASE, "..", "atolye.blend"), compress=True)
print("YENIDEN_KUR_TAMAM fbx=%d sure=%.1fsn" % (len(paths), time.time() - t0))
