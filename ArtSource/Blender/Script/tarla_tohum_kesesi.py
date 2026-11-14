# Sv2 Tohum Nefes Alır: pazarda satılan tohum kesesi (10 tohum) — elle yazılmış model + envanter simgesi.
# Ağzı ip ile bağlı keten kese, ağız kısmı dışa kıvrık; tepede görünen buğday taneleri. Palet renkleri (09-tema).
# Unity: kök Esya_Tohum_Kesesi, çocuk _Model ve _Tutma_Noktasi (HeldItemDisplay elden tutar).
# Yarışma süresince yazıldı (0'dan üret). Blender 5.x.
#
#   blender -b harita.blend --python tarla_tohum_kesesi.py   (dosyayı kaydetmez)

import os, math, random
_d = r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\ArtSource\Blender\Script"
for _f in ("harita_tarla.py", "ikon_render.py"):
    _p = os.path.join(_d, _f)
    exec(compile(open(_p, encoding="utf-8").read(), _p, "exec"), globals())

ESYA_EXPORT = r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\Assets\_Project\Art\Esya"
NAME = "Esya_Tohum_Kesesi"

def kese_bm(seed=3):
    r = random.Random(seed)
    # profil (yarıçap, yükseklik): şişkin gövde → bağlı boyun → dışa kıvrık ağız
    prof = [(0.0, 0.0), (0.06, 0.0), (0.085, 0.02), (0.095, 0.07), (0.09, 0.12), (0.07, 0.16), (0.032, 0.185),
            (0.028, 0.195), (0.05, 0.215), (0.065, 0.225), (0.055, 0.232)]
    bm = bm_lathe(prof, sides=14, close_top=False)
    for v in bm.verts:                                    # kumaş kırışığı ve yamukluk
        a = math.atan2(v.co.y, v.co.x)
        if 0.01 < v.co.z < 0.17:
            k = 1 + 0.05 * math.sin(a * 5 + v.co.z * 30) + 0.03 * (r.random() - 0.5)
            v.co.x *= k
            v.co.y *= k
        if v.co.z > 0.2:                                  # kıvrık ağız dalgalı
            v.co.z += 0.008 * math.sin(a * 7)
    linen = mix(pal("kil", 0), pal("siva", 0), 0.45)
    paint_by(bm, lambda f, l: mix(mix(pal("kil", 1), linen, 0.5), linen, min(1.0, l.vert.co.z / 0.14)))
    parts = [bm]
    # ip: boyunda iki tur
    for k in range(2):
        z = 0.183 + k * 0.009
        pts = [(math.cos(2 * math.pi * i / 16) * 0.034, math.sin(2 * math.pi * i / 16) * 0.034, z) for i in range(17)]
        rope = tube_pts(pts, 0.0045, sides=5)
        flat(rope, mix(pal("ahsap", 0), pal("kil", 1), 0.4))
        parts.append(rope)
    tail = tube_pts([(0.034, 0.0, 0.186), (0.05, -0.012, 0.165), (0.055, -0.02, 0.14)], 0.004, sides=5)
    flat(tail, mix(pal("ahsap", 0), pal("kil", 1), 0.4))
    parts.append(tail)
    # ağızdan görünen tohumlar: tepede küçük yığın + birkaç tane
    mound = bm_lathe([(0.0, 0.205), (0.045, 0.205), (0.035, 0.222), (0.0, 0.232)], sides=12)
    flat(mound, pal("altin", 1))
    parts.append(mound)
    for i in range(14):
        g = bmesh.new()
        bmesh.ops.create_icosphere(g, subdivisions=1, radius=0.006)
        bmesh.ops.scale(g, vec=(1.6, 0.9, 0.8), verts=g.verts)
        a = r.random() * 2 * math.pi
        rr = 0.012 + 0.03 * r.random()
        transform_bm(g, Matrix.Translation((math.cos(a) * rr, math.sin(a) * rr, 0.226 + 0.006 * r.random()))
                     @ Matrix.Rotation(r.random() * math.pi, 4, "Z"))
        flat(g, mix(pal("altin", 0), pal("altin", 2), r.random()))
        parts.append(g)
    return merge(parts)

def build():
    col = collection("Tohum_Tmp")
    clear_collection("Tohum_Tmp")
    root = empty(NAME, col, kind="PLAIN_AXES", size=0.08)
    model = to_object(kese_bm(), NAME + "_Model", col, sharp_angle=45)
    model.parent = root
    grip = empty(NAME + "_Tutma_Noktasi", col, loc=(0, 0, 0.17), kind="SPHERE", size=0.02)
    grip.parent = root
    return root, model

def export_and_icon():
    root, model = build()
    bpy.context.view_layer.update()
    out = _fbx(os.path.join(ESYA_EXPORT, NAME + ".fbx"), [root, model] + [c for c in root.children if c != model])
    # simge: ikon_render.py ile aynı kamera, ışık ve ölçek
    sc, cam = _setup_icon_scene()
    ob = bpy.data.objects.new("_Ikon_" + NAME, model.data)
    sc.collection.objects.link(ob)
    dims = model.dimensions
    ob.location = (0, 0, -dims.z / 2)
    ob.rotation_euler = (0, 0, math.radians(-30))
    cam.data.ortho_scale = ICON_SCALE
    cam.location = Vector((0, -1, math.tan(math.radians(18)))).normalized() * 3.0
    cam.rotation_euler = (math.radians(90 - 18), 0, 0)
    sc.render.filepath = os.path.join(ICON_DIR, NAME + ".png")
    bpy.ops.render.render(write_still=True, scene=sc.name)
    return out, sc.render.filepath

if __name__ == "__main__":
    print("EXPORT", export_and_icon())
