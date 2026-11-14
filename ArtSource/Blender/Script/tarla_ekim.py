# Sv2 Tohum Nefes Alır: tarladaki ekim sandığı (Ekim_Sandigi) — elle yazılmış model, palet renkleri (09-tema).
# Ahşap tahta sandık, demir köşe bantları, arkaya yaslanmış açık kapak, içte tohum yığını (Ekim_Sandigi_Tohum:
# orijini sandık tabanında, Unity'de yukarı doğru ölçeklenip "dolar"), kenarda ahşap kepçe.
# Yarışma süresince yazıldı (0'dan üret). Blender 5.x. harita_tarla.py yardımcılarını kullanır.
#
#   blender -b harita.blend --python tarla_ekim.py   (dosyayı kaydetmez; yalnızca FBX verir)

import os, math, random
_p = os.path.join(r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\ArtSource\Blender\Script", "harita_tarla.py")
exec(compile(open(_p, encoding="utf-8").read(), _p, "exec"), globals())

EKIM_EXPORT = r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\Assets\_Project\Art\Tarla"
W, D, H, T = 1.2, 0.7, 0.55, 0.035      # dış ölçü (x, y, z) ve tahta kalınlığı

def _planks(w, d, h, x=0.0, y=0.0, z=0.0, n=4, seed=0, axis="z"):
    """Yatay tahtalardan duvar: her tahta hafif farklı ton, aralarında ince derz."""
    r = random.Random(seed)
    out = []
    for i in range(n):
        hh = h / n
        b = box(w, d, hh - 0.006, x=x, y=y, z=z + i * hh + 0.003, bevel=0.006)
        t = 0.25 + 0.5 * r.random()
        paint_by(b, lambda f, l, t=t: mix(pal("ahsap", 1), pal("ahsap", 2), t))
        out.append(b)
    return out

def build_ekim_sandigi():
    col = collection("Ekim_Tmp")
    clear_collection("Ekim_Tmp")
    parts = []
    # taban
    fl = box(W - 2 * T, D - 2 * T, 0.04, z=0.06, bevel=0.005)
    grad(fl, "ahsap", 2, 1)
    parts.append(fl)
    # dört duvar (yatay tahtalar)
    parts += _planks(W, T, H - 0.06, y=-(D - T) / 2, z=0.06, seed=1)
    parts += _planks(W, T, H - 0.06, y=(D - T) / 2, z=0.06, seed=2)
    parts += _planks(T, D - 2 * T, H - 0.06, x=-(W - T) / 2, z=0.06, seed=3)
    parts += _planks(T, D - 2 * T, H - 0.06, x=(W - T) / 2, z=0.06, seed=4)
    # köşe dikmeleri + ayaklar
    for sx in (-1, 1):
        for sy in (-1, 1):
            po = box(0.07, 0.07, H + 0.02, x=sx * (W / 2 - 0.02), y=sy * (D / 2 - 0.02), z=0.0, bevel=0.008)
            grad(po, "ahsap", 2, 0)
            parts.append(po)
            # demir köşe bandı (üst ve alt)
            for zb in (0.1, H - 0.08):
                bd = box(0.085, 0.085, 0.04, x=sx * (W / 2 - 0.02), y=sy * (D / 2 - 0.02), z=zb)
                flat(bd, c_iron(0.45))
                parts.append(bd)
    # üst kenar pervazı
    for sy in (-1, 1):
        rim = box(W + 0.02, 0.06, 0.03, y=sy * (D / 2 - 0.01), z=H, bevel=0.006)
        grad(rim, "ahsap", 2, 0)
        parts.append(rim)
    # arkaya yaslanmış açık kapak (menteşe arka kenarda, 110° açık)
    lid = _planks(W, 0.03, D, n=3, seed=5)
    lid_bm = merge(lid)
    transform_bm(lid_bm, Matrix.Translation((0, D / 2, H + 0.02)) @ Matrix.Rotation(math.radians(-20), 4, "X") @ Matrix.Translation((0, 0, 0)))
    parts.append(lid_bm)
    for sx in (-0.4, 0.4):                                  # menteşeler
        hg = bm_cyl(0.018, 0.018, 0.08, sides=6)
        transform_bm(hg, Matrix.Translation((sx, D / 2, H + 0.02)) @ Matrix.Rotation(math.pi / 2, 4, "Y") @ Matrix.Translation((0, 0, -0.04)))
        flat(hg, c_iron(0.55))
        parts.append(hg)
    # kenarda ahşap kepçe (tohum ölçeği)
    sc = bm_lathe([(0.0, 0.0), (0.05, 0.0), (0.065, 0.04), (0.06, 0.06)], sides=10, close_top=False)
    transform_bm(sc, Matrix.Translation((W / 2 - 0.12, -D / 2 + 0.02, H + 0.03)) @ Matrix.Rotation(math.radians(25), 4, "X"))
    grad(sc, "ahsap", 2, 0)
    parts.append(sc)
    hd = tube_pts([(W / 2 - 0.12, -D / 2 - 0.02, H + 0.08), (W / 2 - 0.12, -D / 2 - 0.2, H + 0.16)], 0.012, sides=5)
    grad(hd, "ahsap", 2, 1)
    parts.append(hd)

    root = empty("Ekim_Sandigi", col, kind="PLAIN_AXES", size=0.3)
    body = to_object(merge(parts), "SM_Ekim_Sandigi", col, sharp_angle=40)
    body.parent = root

    # tohum yığını: orijin sandık tabanında (z=0.1); yukarı ölçeklenince dolar. Yüzeyde tane tane kabarıklık.
    r = random.Random(7)
    heap = bmesh.new()
    nx, ny = 14, 8
    vs = []
    for j in range(ny + 1):
        row = []
        for i in range(nx + 1):
            u, v = i / nx, j / ny
            x = (u - 0.5) * (W - 2 * T - 0.02)
            y = (v - 0.5) * (D - 2 * T - 0.02)
            dome = 1.0 - (2 * u - 1) ** 2 * 0.35 - (2 * v - 1) ** 2 * 0.35
            z = 0.36 * dome + 0.015 * (r.random() - 0.5)
            row.append(heap.verts.new((x, y, z)))
        vs.append(row)
    for j in range(ny):
        for i in range(nx):
            heap.faces.new((vs[j][i], vs[j][i + 1], vs[j + 1][i + 1], vs[j + 1][i]))
    # kenar duvarları (yığın kutuyu doldurur, alttan görünmez)
    base = [heap.verts.new((v.co.x, v.co.y, 0.0)) for v in [vs[0][i] for i in range(nx + 1)]]
    paint_by(heap, lambda f, l: mix(pal("altin", 1), pal("kil", 0), 0.25 + 0.3 * r.random()))
    hob = to_object(heap, "Ekim_Sandigi_Tohum", col, loc=(0, 0, 0.1), sharp_angle=60)
    hob.parent = root
    hob.location = (0, 0, 0.1)
    print("Ekim sandığı:", tri_count([body, hob]), "üçgen")
    return root

def export_ekim():
    root = build_ekim_sandigi()
    bpy.context.view_layer.update()
    objs = [root] + list(root.children_recursive)
    return _fbx(os.path.join(EKIM_EXPORT, "Ekim_Sandigi.fbx"), objs)

if __name__ == "__main__":
    print("EXPORT", export_ekim())
