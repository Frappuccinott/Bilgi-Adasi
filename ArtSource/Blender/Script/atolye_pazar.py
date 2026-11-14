# Atölye önü pazarı: Câbir Usta'nın atölyesinin önündeki satış tezgâhı.
# Hikâye: müşteriler (sakinler) sokaktan gelip tezgâhta sipariş verir, hazır ürünü buradan satın alır (karar 14.11).
# Referans: ArtSource/Referans/atolye_pazar.md · Kurallar: 09-tema-ve-model-kurallari.md
# Mekanik: Scripts/City/ShopCounter.cs (sipariş/teslim/satış noktası), Art/NPC/Davranis/CustomerQueue.cs (sıra)
#
# Komut satırından üretilir ve dışa aktarılır:
#   blender --background --factory-startup --python atolye_pazar.py
# Blender MCP'den: exec ile yükle (__name__ farklı olur), build() çağır, look ile bak.
#
# Parçalar (hiyerarşi bir kademe; ön yüz = müşteri tarafı = -Y):
#   Atolye_Pazar (boş, pivot tezgâh taban ortası)
#   ├─ SM_Pazar_Tezgah       tezgâh gövdesi, üst tabla, sergi basamağı, akçe kutusu, sipariş defteri, iç raf
#   ├─ SM_Pazar_Tente        dört direk, kirişler, çizgili tente, sarkan diller, alın tahtası (tabela)
#   ├─ SM_Bekleme_Oturak     sıradaki müşterinin oturduğu bank (tezgâhın sağında)
#   ├─ Tabela_Yazi_Pazar     TMP çapası (alın tahtasının önü; yazı mesh değil)
#   ├─ Musteri_Nokta_1       tezgâhın önünde sipariş veren müşterinin durduğu yer (+Y'ye bakar)
#   ├─ Musteri_Nokta_2       bankta bekleyen müşteri
#   ├─ Satici_Nokta          oyuncunun durduğu yer (tezgâh arkası)
#   └─ Sergi_1..4            sergi basamağındaki kavanoz yerleri (Unity'de Malzeme_* konur)

import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

try:
    HERE = os.path.dirname(os.path.abspath(__file__))
except NameError:
    HERE = r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\ArtSource\Blender\Script"
REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
_lib = os.path.join(HERE, "ba_lib.py")
exec(compile(open(_lib, encoding="utf-8").read(), _lib, "exec"), globals())

EXPORT = os.path.join(REPO, "Assets", "_Project", "Art", "Esya", "Atolye_Pazar.fbx")
COL = "Atolye_Pazar"

# ölçü (m) — 08 §1: tezgâh ~0,9 m; göz 1,56–1,68 m; tente altı ≥ 2,0 m (müşteri başı 1,75)
W = 2.30            # tezgâh gövdesi genişliği
D = 0.62            # gövde derinliği
H = 0.88            # gövde yüksekliği (üst tabla altı)
TOP_T = 0.06        # üst tabla kalınlığı
TOP = H + TOP_T     # tezgâh üstü (0,94)
POST_X = 1.30       # direklerin x'i
POST_BACK_Y = 0.42  # arka direkler (satıcı tarafı)
POST_FRONT_Y = -1.05  # ön direkler (müşteri tarafı)
BACK_H = 2.45       # tente arka yüksekliği
FRONT_H = 2.08      # tente ön yüksekliği

KAGIT = ("#E2DBB1", "#D6CCB2", "#9E8B7E")   # 09 §3 türetilmiş kâğıt (tente açık şeridi)

def kagit(t=0):
    return hex2rgb(KAGIT[t])

def chaikin(prof, it=2, keep_ends=True):
    pts = list(prof)
    for _ in range(it):
        out = [pts[0]] if keep_ends else []
        for a, b in zip(pts, pts[1:]):
            out.append((a[0] * 0.75 + b[0] * 0.25, a[1] * 0.75 + b[1] * 0.25))
            out.append((a[0] * 0.25 + b[0] * 0.75, a[1] * 0.25 + b[1] * 0.75))
        if keep_ends:
            out.append(pts[-1])
        pts = out
    return pts

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
        if f.normal.z < -0.5:
            c = mix(c, pal("ahsap", 2), 0.5)
        return (min(c[0] * k, 1), min(c[1] * k, 1), min(c[2] * k, 1), 1.0)
    paint_by(bm, fn)
    return bm

def iron(bm, light=0.6):
    paint_by(bm, lambda f, l: mix(demir(0.32), demir(light), max(0.0, f.normal.z) * 0.5 + 0.2))
    return bm

def brass(bm):
    paint_by(bm, lambda f, l: mix(pal("bakir", 1), pal("altin", 2), 0.35 + 0.4 * max(0.0, f.normal.z)))
    return bm

def plank(sx, sy, sz, seed, bevel=0.008, lo=2, hi=1, jit=0.004):
    b = bm_box(sx, sy, sz, bevel=bevel)
    jitter(b, jit, seed=seed)
    return wood(b, seed, lo=lo, hi=hi)

def T(x, y, z):
    return Matrix.Translation((x, y, z))

# ---------------------------------------------------------------- tezgâh
def counter_bm():
    parts = []
    r = random.Random(14)
    # köşe ayakları: kalın kare dikmeler (gövdeyi taşır)
    for sx in (-1, 1):
        for sy in (-1, 1):
            leg = plank(0.08, 0.08, H, 100 + sx * 3 + sy, bevel=0.01)
            parts.append((leg, T(sx * (W / 2 - 0.04), sy * (D / 2 - 0.04), 0)))
    # ön pano: dikey tahtalar, boyları hafif oynar (cetvelle çizilmemiş); müşteri tarafı (-Y)
    n = 7
    bw = (W - 0.08) / n
    for i in range(n):
        h = H - 0.10 + r.uniform(-0.02, 0.0)
        bk = plank(bw - 0.008, 0.03, h, 120 + i, bevel=0.006, lo=2, hi=1)
        parts.append((bk, T(-W / 2 + 0.04 + bw * (i + 0.5), -D / 2 + 0.01, 0.06)))
    # ön pano kuşakları (yatay iki kayıt) — siluette çerçeve
    for z, h in ((0.10, 0.08), (H - 0.16, 0.09)):
        rail = plank(W - 0.02, 0.04, h, 140 + int(z * 100), bevel=0.01, lo=1, hi=0)
        parts.append((rail, T(0, -D / 2 - 0.012, z)))
    # yan panolar
    for sx in (-1, 1):
        sp = plank(0.03, D - 0.06, H - 0.12, 150 + sx, bevel=0.006)
        parts.append((sp, T(sx * (W / 2 - 0.01), 0, 0.06)))
    # iç raf (satıcı tarafı açık): alt raf + orta raf
    for z, s in ((0.10, 160), (0.46, 161)):
        sh = plank(W - 0.12, D - 0.12, 0.03, s, bevel=0.005, lo=1, hi=1)
        parts.append((sh, T(0, 0.02, z)))
    # üst tabla: iki kalın tahta, müşteri tarafına taşar; araları küçük oynama
    for i, (y, d) in enumerate(((-0.17, 0.44), (0.21, 0.34))):
        tb = plank(W + 0.16 + r.uniform(-0.01, 0.01), d, TOP_T, 170 + i, bevel=0.012, lo=1, hi=0)
        parts.append((tb, T(r.uniform(-0.01, 0.01), y, H)))
    # sergi basamağı (tezgâhın arkasında, iki kademe): hazır ürünler müşteriye dönük durur
    for k, (y, h) in enumerate(((0.10, 0.10), (0.24, 0.20))):
        st = plank(1.20, 0.16, h, 180 + k, bevel=0.01, lo=1, hi=0)
        parts.append((st, T(-0.35, y, TOP)))
    # akçe kutusu: küçük sandık, pirinç köşe (vurgu yalnız burada)
    box = plank(0.26, 0.18, 0.12, 190, bevel=0.012, lo=1, hi=0)
    lid = bm_prism(chaikin([(-0.13, 0.0), (-0.13, 0.025), (-0.08, 0.05), (0.08, 0.05), (0.13, 0.025), (0.13, 0.0)], 1),
                   -0.095, 0.095)
    wood(lid, 191, lo=1, hi=0)
    band = bm_box(0.27, 0.025, 0.13, bevel=0.003)
    brass(band)
    lock = bm_box(0.04, 0.012, 0.045, bevel=0.004)
    brass(lock)
    coin = bm_merge((box, None), (lid, T(0, 0, 0.12)), (band, None), (lock, T(0, -0.095, 0.07)))
    parts.append((coin, T(0.78, 0.12, TOP) @ Matrix.Rotation(math.radians(-8), 4, "Z")))
    # sipariş defteri: açık defter, kâğıt sayfa + ahşap kapak (yazı yok)
    cover = bm_box(0.36, 0.25, 0.012, bevel=0.003)
    paint_flat(cover, pal("ahsap", 2))
    pages = []
    for s in (-1, 1):
        pg = bm_box(0.165, 0.23, 0.018, bevel=0.002)
        for v in pg.verts:          # sayfalar ortaya doğru hafif çöker
            v.co.z += 0.008 * (abs(v.co.x + s * 0.0825) / 0.0825) if v.co.z > 0.01 else 0
        paint_by(pg, lambda f, l: kagit(0) if f.normal.z > 0.5 else kagit(2))
        pages.append((pg, T(s * 0.0875, 0, 0.012)))
    book = bm_merge((cover, None), *pages)
    parts.append((book, T(0.30, -0.22, TOP) @ Matrix.Rotation(math.radians(12), 4, "Z")))
    return bm_merge(*parts)

# ---------------------------------------------------------------- tente + direkler
def awning_bm():
    parts = []
    r = random.Random(27)
    # direkler: arka uzun, ön kısa; hafif konik, taban taşı
    for sx in (-1, 1):
        for y, h, s in ((POST_BACK_Y, BACK_H, 0), (POST_FRONT_Y, FRONT_H, 1)):
            p = bm_cyl(0.055, 0.045, h, sides=8)
            jitter(p, 0.004, seed=200 + s + sx)
            wood(p, 200 + s * 2 + sx, lo=2, hi=1)
            parts.append((p, T(sx * POST_X, y, 0)))
            base = bm_cyl(0.10, 0.085, 0.07, sides=7, bevel=0.012)
            jitter(base, 0.01, seed=210 + s + sx)
            paint_part_stone(base, 210 + s + sx)
            parts.append((base, T(sx * POST_X, y, 0) @ Matrix.Rotation(r.uniform(0, 1), 4, "Z")))
    # kirişler: ön ve arka boyuna, iki yanda eğik
    for y, z, s in ((POST_BACK_Y, BACK_H - 0.04, 220), (POST_FRONT_Y, FRONT_H - 0.04, 221)):
        bm = plank(2 * POST_X + 0.22, 0.09, 0.09, s, bevel=0.012, lo=1, hi=0)
        parts.append((bm, T(0, y, z)))
    for sx in (-1, 1):
        a = Vector((sx * POST_X, POST_BACK_Y, BACK_H))
        b = Vector((sx * POST_X, POST_FRONT_Y, FRONT_H))
        secs = []
        for p in (a, b):
            secs.append([p + Vector((-0.035, 0, -0.07)), p + Vector((0.035, 0, -0.07)),
                         p + Vector((0.035, 0, 0.0)), p + Vector((-0.035, 0, 0.0))])
        sb = bm_sweep(secs)
        wood(sb, 230 + sx, lo=1, hi=0)
        parts.append((sb, None))
    # köşe payandaları (ön direk → ön kiriş): çapraz destek siluete "tezgâh" okutur
    for sx in (-1, 1):
        p0 = Vector((sx * POST_X, POST_FRONT_Y, FRONT_H - 0.45))
        p1 = Vector((sx * (POST_X - 0.38), POST_FRONT_Y, FRONT_H - 0.07))
        secs = [[p + Vector((0, -0.025, -0.025)), p + Vector((0, 0.025, -0.025)), p + Vector((0, 0.025, 0.025)),
                 p + Vector((0, -0.025, 0.025))] for p in (p0, p1)]
        br = bm_sweep(secs)
        wood(br, 240 + sx, lo=2, hi=1)
        parts.append((br, None))
    # tente: çizgili kumaş (çatı turuncusu / kâğıt), arka → ön sarkık; kalınlık verilir (iki yüz görünür)
    ns = 8                    # şerit sayısı
    nd = 6                    # derinlik bölümü
    x0, x1 = -POST_X - 0.16, POST_X + 0.16
    y0, y1 = POST_BACK_Y + 0.08, POST_FRONT_Y - 0.12
    cloth = bmesh.new()
    lay = cloth.loops.layers.float_color.new("Col")
    grid = []
    for j in range(nd + 1):
        t = j / nd
        y = y0 + (y1 - y0) * t
        z = BACK_H + 0.05 + (FRONT_H + 0.02 - BACK_H) * t - 0.07 * math.sin(math.pi * t)  # sarkma
        row = []
        for i in range(ns * 2 + 1):
            u = i / (ns * 2)
            x = x0 + (x1 - x0) * u
            zz = z - 0.025 * math.sin(math.pi * (i % 2))   # şerit dikişleri hafif çöker
            row.append(cloth.verts.new((x, y, zz + r.uniform(-0.004, 0.004))))
        grid.append(row)
    for j in range(nd):
        for i in range(ns * 2):
            f = cloth.faces.new((grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i]))
            stripe = (i // 2) % 2
            for l in f.loops:
                zt = (l.vert.co.y - y1) / (y0 - y1)     # öne doğru hafif koyulaşır
                c = pal("cati", 1) if stripe == 0 else kagit(0)
                l[lay] = mix(c, pal("cati", 2) if stripe == 0 else kagit(1), 0.35 * (1 - zt))
    bmesh.ops.recalc_face_normals(cloth, faces=cloth.faces)
    for f in cloth.faces:
        if f.normal.z < 0:
            f.normal_flip()
    bmesh.ops.solidify(cloth, geom=list(cloth.faces), thickness=0.018)
    parts.append((cloth, None))
    # sarkan diller: her şeridin ucunda yuvarlak kesimli dil (renk şeridiyle aynı)
    for i in range(ns):
        u0 = x0 + (x1 - x0) * (i / ns)
        u1 = x0 + (x1 - x0) * ((i + 1) / ns)
        w = (u1 - u0)
        prof = [(-w / 2, 0.0), (w / 2, 0.0), (w / 2, -0.12), (w * 0.3, -0.17), (0.0, -0.185), (-w * 0.3, -0.17), (-w / 2, -0.12)]
        fl = bm_prism(prof, -0.008, 0.008)
        jitter(fl, 0.003, seed=250 + i)
        c = pal("cati", 1) if i % 2 == 0 else kagit(0)
        c2 = pal("cati", 2) if i % 2 == 0 else kagit(1)
        paint_by(fl, lambda f, l, c=c, c2=c2: mix(c, c2, min(1.0, -l.vert.co.z / 0.185) * 0.5))
        parts.append((fl, T((u0 + u1) / 2, y1 - 0.004, FRONT_H + 0.02 - 0.01) @ Matrix.Rotation(math.radians(-6), 4, "X")))
    # alın tahtası (tabela): ön kirişin üstünde dik duran, kenarları oyulmuş tahta; yazı TMP ile
    sign_prof = chaikin([(-0.62, 0.0), (0.62, 0.0), (0.66, 0.10), (0.60, 0.22), (0.30, 0.27), (0.0, 0.29),
                         (-0.30, 0.27), (-0.60, 0.22), (-0.66, 0.10)], 1)
    sg = bm_prism(sign_prof, -0.025, 0.025)
    wood(sg, 260, lo=1, hi=0, bias=0.3)
    parts.append((sg, T(0, POST_FRONT_Y - 0.10, FRONT_H + 0.02)))
    # yazı alanı: kâğıt rengi gömme levha (mürekkep rengi TMP yazı üstünde okunur)
    pl = bm_prism(chaikin([(-0.50, 0.04), (0.50, 0.04), (0.52, 0.13), (0.46, 0.21), (0.0, 0.235), (-0.46, 0.21),
                           (-0.52, 0.13)], 1), -0.006, 0.006)
    paint_by(pl, lambda f, l: mix(kagit(0), kagit(1), 0.3 + 0.4 * (1 - l.vert.co.z / 0.235)))
    parts.append((pl, T(0, POST_FRONT_Y - 0.10 - 0.027, FRONT_H + 0.02)))
    # tabelayı kirişe bağlayan iki demir kulak
    for sx in (-1, 1):
        ear = bm_box(0.04, 0.10, 0.10, bevel=0.005)
        iron(ear)
        parts.append((ear, T(sx * 0.45, POST_FRONT_Y - 0.05, FRONT_H - 0.04)))
    return bm_merge(*parts)

def paint_part_stone(bm, seed):
    rr = random.Random(seed)
    k = rr.uniform(-0.05, 0.05)
    paint_by(bm, lambda f, l: mix(pal("kaya", 2), pal("kaya", 1), max(0.0, min(1.0, l.vert.co.z / 0.07 + k))))
    return bm

# ---------------------------------------------------------------- bank
def bench_bm():
    parts = []
    L, SEAT = 1.15, 0.44
    # oturak: iki tahta, biri hafif kısa; tombul pah
    for i, (y, w) in enumerate(((-0.075, 0.16), (0.09, 0.15))):
        s = plank(L + (0.0 if i == 0 else -0.05), w, 0.05, 300 + i, bevel=0.012, lo=1, hi=0)
        parts.append((s, T(0.0 if i == 0 else 0.02, y, SEAT - 0.05)))
    # ayaklar: A şeklinde iki çift, dışa açık
    for sx in (-1, 1):
        for sy in (-1, 1):
            leg = plank(0.06, 0.06, SEAT - 0.04, 310 + sx * 2 + sy, bevel=0.01)
            m = T(sx * (L / 2 - 0.12), sy * 0.10, 0) @ Matrix.Rotation(math.radians(-8 * sy), 4, "X")
            parts.append((leg, m))
        st = plank(0.05, 0.30, 0.05, 320 + sx, bevel=0.008, lo=2, hi=2)
        parts.append((st, T(sx * (L / 2 - 0.12), 0, 0.14)))
    # alt kayıt
    rail = plank(L - 0.30, 0.05, 0.05, 330, bevel=0.008, lo=2, hi=2)
    parts.append((rail, T(0, 0, 0.14)))
    return bm_merge(*parts)

# ---------------------------------------------------------------- kur + dışa aktar
BENCH_LOC = (2.15, -0.75, 0.0)
BENCH_ROT = -78.0     # bank tezgâha yan döner: bekleyen müşteri tezgâhı görür

def build():
    clear_collection(COL)
    col = collection(COL)
    root = empty("Atolye_Pazar", col, kind="PLAIN_AXES", size=0.5)
    for name, bm, loc, rot, sharp in (
            ("SM_Pazar_Tezgah", counter_bm(), (0, 0, 0), (0, 0, 0), 40),
            ("SM_Pazar_Tente", awning_bm(), (0, 0, 0), (0, 0, 0), 40),
            ("SM_Bekleme_Oturak", bench_bm(), BENCH_LOC, (0, 0, BENCH_ROT), 40)):
        ob = to_object(bm, name, col, loc=loc, rot=rot, sharp_angle=sharp)
        ob.parent = root
    # çapalar (Unity'de kod bunlara bağlanır)
    anchors = [
        ("Tabela_Yazi_Pazar", (0, POST_FRONT_Y - 0.14, FRONT_H + 0.15), (90, 0, 180)),
        ("Musteri_Nokta_1", (0.0, -0.80, 0.0), (0, 0, 0)),
        ("Musteri_Nokta_2", (BENCH_LOC[0], BENCH_LOC[1], 0.0), (0, 0, BENCH_ROT)),
        ("Satici_Nokta", (0.0, 0.85, 0.0), (0, 0, 180)),
    ]
    for k, x in enumerate((-0.80, -0.42, -0.06, 0.28)):
        y, z = (0.10, TOP + 0.10) if k % 2 == 0 else (0.24, TOP + 0.20)
        anchors.append(("Sergi_%d" % (k + 1), (x, y, z), (0, 0, 0)))
    for name, loc, rot in anchors:
        e = empty(name, col, loc=loc, rot=rot, kind="ARROWS", size=0.15)
        e.parent = root
    objs = [o for o in col.objects if o.type == "MESH"]
    for o in objs:
        print(o.name, "tri:", tri_count([o]))
    print("tri toplam:", tri_count(objs))
    return root, col

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
    _, c = build()
    if "--no-export" not in sys.argv:
        export(c)
    if bpy.app.background:
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(REPO, "ArtSource", "Blender", "atolye_pazar.blend"))
