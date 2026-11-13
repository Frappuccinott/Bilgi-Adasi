# Câbir Usta'nın kilitli dolabı — tehlikeli maddeler (cıva, kezzap, kral suyu, zırnık) pazarda yok, yalnızca burada.
# Hikâye: 04_Icerik/10-gorev-pazar-npc.md ("Usta izni olmadan dokunulmaz") · Referans: ArtSource/Referans/kilitli_dolap.md
# Kurallar: 09-tema-ve-model-kurallari.md · Mekanik: Scripts/Order/World/LockedCabinet.cs (kapaklar + kilit sarsılır)
#
# Komut satırından (açık Blender dosyasına dokunmadan) üretilir ve dışa aktarılır:
#   blender --background --factory-startup --python kilitli_dolap.py
#
# Parçalar (hiyerarşi bir kademe):
#   Kilitli_Dolap (boş, pivot taban ortası, ön yüz -Y)
#   ├─ SM_Dolap_Govde     gövde, taç, ayaklar, iç raflar ve şişeler (sabit)
#   ├─ Dolap_Kapak_Sol    pivot: sol menteşe kenarı, tabanda (Z ekseninde döner)
#   ├─ Dolap_Kapak_Sag    pivot: sağ menteşe kenarı, tabanda
#   └─ Dolap_Kilit        asma kilit, pivot: halkanın asıldığı nokta

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

EXPORT = os.path.join(REPO, "Assets", "_Project", "Art", "Esya", "Kilitli_Dolap.fbx")
COL = "Kilitli_Dolap"

# ölçü (m): tezgâhtan yüksek, raftan alçak; göz hizasında (1,56–1,68) kilit ve parmaklık okunur
W, D, H = 0.86, 0.42, 1.62
FOOT = 0.09          # ayak yüksekliği
DOOR_Z0, DOOR_Z1 = FOOT + 0.07, H - 0.12
DOOR_T = 0.035
GAP = 0.006

# ---------------------------------------------------------------- boyama
def wood(bm, seed, lo=2, hi=1, bias=0.15, noise=0.06):
    r = random.Random(seed)
    k = 1.0 + r.uniform(-noise, noise)
    zs = [v.co.z for v in bm.verts]
    z0, z1 = min(zs), max(zs)
    span = max(z1 - z0, 1e-6)
    a, b = pal("ahsap", lo), pal("ahsap", hi)
    def fn(f, l):
        t = (l.vert.co.z - z0) / span
        c = mix(a, b, max(0.0, min(1.0, t * (1 - bias) + bias)))
        if f.normal.z < -0.5:                       # alt yüzler gölgede
            c = mix(c, pal("ahsap", 2), 0.5)
        return (min(c[0] * k, 1), min(c[1] * k, 1), min(c[2] * k, 1), 1.0)
    paint_by(bm, fn)
    return bm

def iron(bm, light=0.6):
    # dövme demir: ba_lib demir() tonunun koyusu (saf siyah değil; kapakta mavi değil demir okunsun)
    paint_by(bm, lambda f, l: mix(demir(0.32), demir(light), max(0.0, f.normal.z) * 0.5 + 0.2))
    return bm

def brass(bm):
    # kilidin gövdesi: altın gölge + bakır (aynı tonun koyusu) — göz kilide gitsin (vurgu rengi yalnızca burada)
    paint_by(bm, lambda f, l: mix(pal("bakir", 1), pal("altin", 2), 0.35 + 0.4 * max(0.0, f.normal.z)))
    return bm

# ---------------------------------------------------------------- gövde
def body_bm():
    parts = []
    r = random.Random(41)
    # yan panolar: hafif eğri (cetvelle çizilmemiş), kalın ön kenar
    for side in (-1, 1):
        secs = []
        for k in range(4):
            z = FOOT + (H - FOOT) * k / 3
            bulge = 0.006 * math.sin(math.pi * k / 3)
            x0 = side * (W / 2 - 0.05) + side * bulge
            x1 = side * (W / 2) + side * bulge
            secs.append([Vector((min(x0, x1), -D / 2, z)), Vector((max(x0, x1), -D / 2, z)),
                         Vector((max(x0, x1), D / 2, z)), Vector((min(x0, x1), D / 2, z))])
        pb = bm_sweep(secs)
        bevel_all(pb, 0.008)
        wood(pb, 50 + side, lo=2, hi=1)
        parts.append((pb, None))
    # arka pano (dikey tahtalar; parmaklıktan bakınca şişeler koyu zeminde okunur)
    nb = 3
    for i in range(nb):
        bw = (W - 0.1) / nb
        bk = bm_box(bw - 0.006, 0.02, H - FOOT - 0.04 + r.uniform(-0.015, 0.0))
        wood(bk, 60 + i, lo=2, hi=2, bias=0.1 + 0.1 * (i % 2))
        parts.append((bk, Matrix.Translation((-W / 2 + 0.05 + bw * (i + 0.5), D / 2 - 0.015, FOOT))))
    # taban ve tavan
    for z, t in ((FOOT, 0.05), (H - 0.06, 0.06)):
        sl = bm_box(W - 0.06, D - 0.02, t)
        wood(sl, 70 + int(z * 10), lo=1, hi=1)
        parts.append((sl, Matrix.Translation((0, 0, z))))
    # taç: iki kademe, öne ve yana taşar (siluet: dolap "ağır" ve resmi dursun)
    crown = bm_box(W + 0.12, D + 0.08, 0.06, bevel=0.018)
    wood(crown, 80, lo=1, hi=0)
    parts.append((crown, Matrix.Translation((0, -0.03, H))))
    crown2 = bm_box(W + 0.05, D + 0.03, 0.04, bevel=0.012)
    wood(crown2, 81, lo=2, hi=1)
    parts.append((crown2, Matrix.Translation((0, -0.012, H - 0.03))))
    # ön çerçeve: kapakların üst/alt kayıtları
    for z, h in ((FOOT + 0.02, 0.06), (DOOR_Z1 + GAP, H - DOOR_Z1 - 0.06)):
        rail = bm_box(W - 0.02, 0.05, h, bevel=0.01)
        wood(rail, 90 + int(z * 10), lo=1, hi=0)
        parts.append((rail, Matrix.Translation((0, -D / 2 + 0.02, z))))
    # ayaklar: kalın, hafif dışa açık
    for sx in (-1, 1):
        for sy in (-1, 1):
            leg = bm_cyl(0.04, 0.05, FOOT + 0.02, sides=4)
            wood(leg, 100 + sx * 3 + sy, lo=2, hi=1)
            parts.append((leg, Matrix.Translation((sx * (W / 2 - 0.06), sy * (D / 2 - 0.06), 0))))
    # iç raf (bir tane) — şişeler iki katta
    shelf_z = DOOR_Z0 + (DOOR_Z1 - DOOR_Z0) * 0.46 + 0.03
    sh = bm_box(W - 0.1, D - 0.08, 0.03)
    wood(sh, 110, lo=1, hi=0)
    parts.append((sh, Matrix.Translation((0, 0.01, shelf_z))))
    # şişeler: parmaklıktan görünen tehlikeli maddeler (renk paletten; etiket yok, yazı Unity'de değil)
    contents = [
        ("civa", "kaya", 0, 0.22),       # cıva: ağır, gümüşi
        ("kezzap", "altin", 0, 0.30),    # kezzap: soluk sarı
        ("kralsuyu", "cati", 1, 0.28),   # kral suyu: turuncu
        ("zirnik", "altin", 1, 0.24),    # zırnık: sarı toz
    ]
    for i, (name, grp, tone, hh) in enumerate(contents):
        z0 = shelf_z + 0.03
        x = -0.27 + i * 0.18 + r.uniform(-0.02, 0.02)
        parts.append((bottle_bm(hh, grp, tone, seed=200 + i, squat=(name == "civa")), Matrix.Translation((x, 0.05, z0))))
    return bm_merge(*parts)

def bottle_bm(h, grp, tone, seed, squat=False):
    rad = 0.075 if squat else 0.06
    prof = [(0, 0), (rad * 0.8, 0), (rad, 0.03), (rad * 1.02, h * (0.45 if squat else 0.4)), (rad * 0.6, h * 0.6),
            (rad * 0.28, h * 0.72), (rad * 0.26, h * 0.92), (rad * 0.36, h * 0.95), (0, h)]
    b = bm_lathe(prof, sides=6)
    c = pal(grp, tone)
    glass = mix(pal("siva", 0), pal("su", 0), 0.35)
    paint_by(b, lambda f, l: mix(c, mix(c, pal("kaya", 2), 0.3), 0.4 if l.vert.co.z < h * 0.15 else 0.0)
             if l.vert.co.z <= h * 0.55 else glass)
    # mühür mumu (kırmızı = "dokunma")
    cap = bm_lathe([(0, 0), (rad * 0.42, 0), (rad * 0.46, 0.018), (rad * 0.3, 0.035), (0, 0.04)], sides=7)
    paint_by(cap, lambda f, l: pal("cati", 2))
    out = bm_merge((b, None), (cap, Matrix.Translation((0, 0, h - 0.01))))
    transform_bm(out, Matrix.Rotation(random.Random(seed).uniform(-0.4, 0.4), 4, "Z"))
    return out

# ---------------------------------------------------------------- kapaklar
def door_bm(side):
    """Yerel: pivot menteşe kenarında (x=0), tabanda (z=0); kapak menteşeden içe doğru (side=+1 sağa uzanır)."""
    dw = W / 2 - 0.035 - GAP
    dh = DOOR_Z1 - DOOR_Z0
    parts = []
    # çerçeve: dört kayıt (stile/rail), orta boşluğun üst yarısı parmaklıklı, alt yarısı dolu tabla
    stile = 0.06
    for x0 in (0.0, dw - stile):
        st = bm_box(stile, DOOR_T, dh, bevel=0.008)
        wood(st, 300 + int(x0 * 100) + side, lo=1, hi=0)
        parts.append((st, Matrix.Translation((side * (x0 + stile / 2), 0, 0))))
    mid_z = dh * 0.46
    for z, hh in ((0.0, 0.07), (mid_z, 0.06), (dh - 0.07, 0.07)):
        rl = bm_box(dw - 2 * stile, DOOR_T, hh, bevel=0.008 if z == 0.0 else 0.0)
        wood(rl, 320 + int(z * 100) + side, lo=1, hi=0)
        parts.append((rl, Matrix.Translation((side * dw / 2, 0, z))))
    # alt tabla: içe gömük
    pn = bm_box(dw - 2 * stile + 0.01, DOOR_T * 0.5, mid_z - 0.07 + 0.01)
    wood(pn, 340 + side, lo=2, hi=1)
    parts.append((pn, Matrix.Translation((side * dw / 2, DOOR_T * 0.15, 0.065))))
    # üst parmaklık: dövme demir çubuklar (opak), arkadaki şişeler görünür
    gz0, gz1 = mid_z + 0.06, dh - 0.07
    n = 3
    for i in range(n):
        x = stile + (dw - 2 * stile) * (i + 0.5) / n
        bar = bm_cyl(0.008, 0.008, gz1 - gz0 + 0.02, sides=4, cap=False)
        iron(bar)
        parts.append((bar, Matrix.Translation((side * x, 0, gz0 - 0.01))))
    cross = bm_box(dw - 2 * stile + 0.02, 0.016, 0.016)
    iron(cross)
    parts.append((cross, Matrix.Translation((side * dw / 2, 0, (gz0 + gz1) / 2))))
    # menteşe kayışları: dış yüzde iki uzun demir şerit, uçları sivri
    for z in (0.12, dh - 0.16):
        strap = bm_box(dw * 0.62, 0.008, 0.035)
        iron(strap)
        parts.append((strap, Matrix.Translation((side * (dw * 0.31 + 0.005), -DOOR_T / 2 - 0.004, z))))
        knob = bm_cyl(0.015, 0.015, 0.06, sides=5)
        iron(knob)
        parts.append((knob, Matrix.Translation((0, -DOOR_T / 2, z - 0.01))))
    # kilit dili (sağ kapakta) ve kilit kulağı (sol kapakta): kapaklar ortada buluşur
    if side == -1:
        hasp = bm_box(0.11, 0.01, 0.045, bevel=0.004)
        iron(hasp, 0.9)
        parts.append((hasp, Matrix.Translation((side * (dw - 0.03), -DOOR_T / 2 - 0.006, dh * 0.5))))
    else:
        staple = bm_box(0.03, 0.03, 0.05, bevel=0.006)
        iron(staple, 0.9)
        parts.append((staple, Matrix.Translation((side * (dw - 0.02), -DOOR_T / 2 - 0.016, dh * 0.5))))
    out = bm_merge(*parts)
    transform_bm(out, Matrix.Translation((0, 0, 0)))
    return out

def padlock_bm():
    """Pivot: halkanın tepesi (asıldığı nokta). Kilit gövdesi aşağı sarkar."""
    parts = []
    body = bm_box(0.09, 0.035, 0.095, bevel=0.014, origin="center")
    brass(body)
    parts.append((body, Matrix.Translation((0, 0, -0.085))))
    # halka: yarım halka (6 parça)
    secs = []
    n = 7
    rr, t = 0.026, 0.007
    for i in range(n):
        a = math.pi * i / (n - 1)
        cx, cz = math.cos(a) * rr, math.sin(a) * rr - 0.045
        secs.append([Vector((cx - t, -t, cz)), Vector((cx + t, -t, cz)), Vector((cx + t, t, cz + 0.0001)), Vector((cx - t, t, cz + 0.0001))])
    ring = bm_sweep(secs)
    iron(ring, 0.9)
    parts.append((ring, Matrix.Translation((0, 0, 0.02))))
    hole = bm_cyl(0.008, 0.008, 0.006, sides=6)
    paint_by(hole, lambda f, l: demir(0.4))
    parts.append((hole, Matrix.Rotation(math.radians(90), 4, "X") @ Matrix.Translation((0, 0.098, 0.014))))
    return bm_merge(*parts)

# ---------------------------------------------------------------- kur + dışa aktar
def build():
    clear_collection(COL)
    col = collection(COL)
    root = empty("Kilitli_Dolap", col, kind="PLAIN_AXES", size=0.4)
    gov = to_object(body_bm(), "SM_Dolap_Govde", col)
    gov.parent = root
    front_y = -D / 2 - DOOR_T / 2 + 0.005
    for side, name in ((-1, "Dolap_Kapak_Sol"), (1, "Dolap_Kapak_Sag")):
        hinge_x = side * (W / 2 - 0.035)   # sol kapak sol kenarda menteşeli, içe (sağa) uzanır
        ob = to_object(door_bm(-side), name, col, loc=(hinge_x, front_y, DOOR_Z0))
        ob.parent = root
    dh = DOOR_Z1 - DOOR_Z0
    lock = to_object(padlock_bm(), "Dolap_Kilit", col, loc=(0.0, front_y - DOOR_T / 2 - 0.03, DOOR_Z0 + dh * 0.5 + 0.03))
    lock.parent = root
    objs = [o for o in col.objects if o.type == "MESH"]
    print("tri:", tri_count(objs))
    return root, col

def export(col):
    os.makedirs(os.path.dirname(EXPORT), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT") if bpy.context.view_layer.objects.active else None
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
    # varsayılan sahne objelerini temizle (yalnız arka plan çalıştırmada)
    if bpy.app.background:
        for o in list(bpy.data.objects):
            bpy.data.objects.remove(o, do_unlink=True)
    _, c = build()
    if "--no-export" not in sys.argv:
        export(c)
    if bpy.app.background:
        out = os.path.join(REPO, "ArtSource", "Blender", "kilitli_dolap.blend")
        bpy.ops.wm.save_as_mainfile(filepath=out)
        if "--render" in sys.argv:
            # kontrol görüntüsü (göz hizası, önden-çapraz)
            scn = bpy.context.scene
            cam = bpy.data.objects.new("Kontrol_Kamera", bpy.data.cameras.new("Kontrol_Kamera"))
            scn.collection.objects.link(cam)
            cam.location = (1.15, -2.0, 1.6)
            d = Vector((0, 0, 0.85)) - cam.location
            cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
            scn.camera = cam
            sun = bpy.data.objects.new("Gunes", bpy.data.lights.new("Gunes", "SUN"))
            sun.data.energy = 3.0
            sun.rotation_euler = (math.radians(50), 0, math.radians(30))
            scn.collection.objects.link(sun)
            world = bpy.data.worlds.new("W") if not scn.world else scn.world
            scn.world = world
            world.use_nodes = True
            bg = next(n for n in world.node_tree.nodes if n.type == "BACKGROUND")
            bg.inputs["Color"].default_value = hex2rgb("#BDEFFA")
            bg.inputs["Strength"].default_value = 0.8
            scn.render.engine = "BLENDER_EEVEE" if "BLENDER_EEVEE" in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items] else scn.render.engine
            scn.render.resolution_x, scn.render.resolution_y = 700, 900
            scn.render.filepath = os.path.join(os.environ.get("BA_RENDER_DIR", REPO), "kilitli_dolap_kontrol.png")
            bpy.ops.render.render(write_still=True)
            print("render:", scn.render.filepath)
