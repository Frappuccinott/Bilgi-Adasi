# Kasaba haritasını scriptlerden sıfırdan kurar, FBX'leri dışa aktarır, harita.blend'i kaydeder.
# Canlı Blender'a dokunmadan arka planda çalıştırılır (doğa model kaynakları dosyada kalır, yeniden kurulmaz):
#   blender -b ArtSource/Blender/harita.blend --python ArtSource/Blender/Script/harita_kur.py
# Ardından denetim:  ... --python ArtSource/Blender/Script/harita_denetim.py -- <rapor.txt>
# Unity'de: BilgiAdasi > Sanat > Haritayı açık sahneye yerleştir.
import os
import time
import bpy

BASE = os.path.dirname(os.path.abspath(__file__))
t0 = time.time()
p = os.path.join(BASE, "harita_tarla.py")              # harita_detay → harita_yon → harita_yapi → harita_doga → harita
exec(compile(open(p, encoding="utf-8").read(), p, "exec"), globals())

build_harita()
scatter()
build_yapilar()
build_yon()
build_detay()
scatter_detay()
build_traktor()
build_aletler()
build_bereket()
build_koru()
preview_instances()
export_harita_tum()
export_tarla()
bpy.ops.wm.save_mainfile()
print("HARITA_KUR_TAMAM sure=%.1fsn" % (time.time() - t0))
