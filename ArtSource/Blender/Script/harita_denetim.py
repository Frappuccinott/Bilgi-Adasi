# Harita denetimi: harita.blend'deki her parçayı arazi ve oyun alanlarıyla karşılaştırır, hata listesini yazar.
# Canlı Blender'a dokunmadan arka planda çalışır:
#   blender -b ArtSource/Blender/harita.blend --python ArtSource/Blender/Script/harita_denetim.py -- <rapor.txt>
# Kontroller (Blender koordinatı, metre):
#   1  havada parça      : hiçbir şeye değmeyen ve zeminden yüksekte duran ada (ayrık mesh parçası)
#   2  kenarı havada     : zemine oturan ama tabanının bir köşesi boşta kalan ada (yamaçtaki ev, duvar)
#   3  yol / meydan / dere işgali : yürüme yolunu kapatan, meydan döşemesine ya da suya giren parça
#   4  bina içine giren  : başka bir binanın gövdesine giren parça ya da doğa örneği
#   5  zemin delinmesi   : arazinin yol, meydan, köy zemini içinden çıkması (z-fighting), suyun kıyıdan taşması
#   6  doğa yerleşimi    : yolda, meydanda, suda, binada ya da üst üste duran ağaç/kaya/çalı
# Yarışma süresince yazıldı (0'dan üret). Blender 5.x.

import os
import sys
import math
import time
import collections
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

_HERE = r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\ArtSource\Blender\Script"
_hp = os.path.join(_HERE, "harita.py")
exec(compile(open(_hp, encoding="utf-8").read(), _hp, "exec"), globals())

T0 = time.time()
OUT = []

def rapor(kod, ad, x, y, ayrinti):
    OUT.append((kod, ad, x, y, ayrinti))

# ------------------------------------------------------------------ hangi objeler
ZEMIN = ("SM_Arazi", "SM_Meydan_Doseme", "SM_Yollar", "SM_Koy_Zeminleri")
ATLA_ON = ("Bulutlar", "SM_Uzak_", "Pus_", "LockedZone_", "_Atolye_Isaret", "Onizleme", "COL_")
ATLA_SON = ("_Isik", "_Duman", "_Kor", "_Su")
DURUM = ("Denk_P", "Tepk_P", "Denk_Once", "Tepk_Once", "Denk_Sonra", "Tepk_Sonra")
BINA = ("SM_Denk_Ev_", "SM_Tepk_Ev_", "SM_Denk_Firin", "SM_Tepk_Lonca", "SM_Tepk_Guvercin_Kulesi", "SM_Degirmen")
MEYDAN_IZINLI = ("SM_Meydan_", "SM_Denk_Tezgah", "SM_Tepk_Tezgah", "SM_Tabela_", "SM_Bisiklet_Park")
YOL_IZINLI = ("Kopru", "SM_Giris_Kemeri", "SM_Sehir_Kapisi", "SM_Kapi_", "Traktor", "Alet_")
SU_IZINLI = ("Kopru", "Saz")

def statik(ob):
    if ob.type != "MESH" or not ob.visible_get():
        return False
    n = ob.name
    if any(n.startswith(a) for a in ATLA_ON) or any(n.endswith(a) for a in ATLA_SON):
        return False
    if n.endswith("_LOD1") or "_LOD" in n and not n.endswith("_LOD0"):
        return False
    c = ob.users_collection[0].name if ob.users_collection else ""
    if c in ("Harita_Doga_Model", "_Onizleme_Doga", "_Onizleme_Isik", "_Onizleme_Harita"):
        return False
    if c == "Bereket_Tarlasi" and n.startswith("Tarla_P") and not n.endswith("_Nadas"):
        return False      # aynı parselin 6 durumu üst üste; yalnızca oyun başındaki görünür
    if c == "Traktor" and n.startswith("Alet_"):
        return True
    return True

def dunya(ob):
    mw = ob.matrix_world
    vs = [mw @ v.co for v in ob.data.vertices]
    ps = [tuple(p.vertices) for p in ob.data.polygons]
    return vs, ps

# ------------------------------------------------------------------ zemin BVH
def bvh_of(names):
    V, P = [], []
    for n in names:
        ob = bpy.data.objects.get(n)
        if ob is None:
            continue
        vs, ps = dunya(ob)
        o = len(V)
        V.extend(vs)
        P.extend(tuple(i + o for i in p) for p in ps)
    return BVHTree.FromPolygons(V, P) if P else None

ZEMIN_BVH = bvh_of(ZEMIN)
ARAZI_BVH = bvh_of(("SM_Arazi",))
YOL_BVH = bvh_of(("SM_Yollar",))
MEYDAN_BVH = bvh_of(("SM_Meydan_Doseme",))
KZ_BVH = bvh_of(("SM_Koy_Zeminleri",))
DOWN = Vector((0, 0, -1))

def zemin_z(bvh, x, y, top=400.0):
    hit = bvh.ray_cast(Vector((x, y, top)), DOWN, top + 200.0)
    return hit[0].z if hit[0] is not None else None

_zc = {}
def zz(x, y):
    k = (round(x, 2), round(y, 2))
    if k not in _zc:
        _zc[k] = zemin_z(ZEMIN_BVH, x, y)
    return _zc[k]

# ------------------------------------------------------------------ adalar
class Ada:
    __slots__ = ("ob", "vs", "lo", "hi", "idx")

def adalar(ob):
    vs, ps = dunya(ob)
    n = len(vs)
    par = list(range(n))
    def f(a):
        while par[a] != a:
            par[a] = par[par[a]]
            a = par[a]
        return a
    def u(a, b):
        ra, rb = f(a), f(b)
        if ra != rb:
            par[ra] = rb
    for p in ps:
        for i in p[1:]:
            u(p[0], i)
    weld = {}
    for i, v in enumerate(vs):
        k = (round(v.x, 3), round(v.y, 3), round(v.z, 3))
        if k in weld:
            u(i, weld[k])
        else:
            weld[k] = i
    groups = collections.defaultdict(list)
    for i in range(n):
        groups[f(i)].append(i)
    out = []
    for g in groups.values():
        a = Ada()
        a.ob = ob.name
        a.vs = [vs[i] for i in g]
        a.lo = Vector((min(v.x for v in a.vs), min(v.y for v in a.vs), min(v.z for v in a.vs)))
        a.hi = Vector((max(v.x for v in a.vs), max(v.y for v in a.vs), max(v.z for v in a.vs)))
        out.append(a)
    return out

STATIK = [ob for ob in bpy.context.scene.objects if statik(ob) and ob.name not in ZEMIN]
ADALAR = []
for ob in STATIK:
    for i, a in enumerate(adalar(ob)):
        a.idx = len(ADALAR)
        ADALAR.append(a)
print("Ada sayısı:", len(ADALAR), "obje:", len(STATIK), "%.1fsn" % (time.time() - T0))

# AABB ızgarası (temas)
CELL = 4.0
grid = collections.defaultdict(list)
for a in ADALAR:
    for gx in range(int(math.floor(a.lo.x / CELL)), int(math.floor(a.hi.x / CELL)) + 1):
        for gy in range(int(math.floor(a.lo.y / CELL)), int(math.floor(a.hi.y / CELL)) + 1):
            grid[(gx, gy)].append(a.idx)

def komsular(a, pad=0.03):
    seen = set()
    for gx in range(int(math.floor((a.lo.x - pad) / CELL)), int(math.floor((a.hi.x + pad) / CELL)) + 1):
        for gy in range(int(math.floor((a.lo.y - pad) / CELL)), int(math.floor((a.hi.y + pad) / CELL)) + 1):
            for j in grid.get((gx, gy), ()):
                if j == a.idx or j in seen:
                    continue
                seen.add(j)
                b = ADALAR[j]
                if (a.lo.x - pad <= b.hi.x and b.lo.x <= a.hi.x + pad and a.lo.y - pad <= b.hi.y and b.lo.y <= a.hi.y + pad
                        and a.lo.z - pad <= b.hi.z and b.lo.z <= a.hi.z + pad):
                    yield b

def ornek(vs, n=160):
    if len(vs) <= n:
        return vs
    st = len(vs) / n
    return [vs[int(i * st)] for i in range(n)]

def durum(ad):
    return any(ad.startswith(d) for d in DURUM)

# ------------------------------------------------------------------ 1-2 havada / kenarı havada
for a in ADALAR:
    boy = a.hi.z - a.lo.z
    alt = [v for v in a.vs if v.z <= a.lo.z + max(0.02, boy * 0.04)]
    gaps = []
    for v in ornek(alt, 60):
        g = zz(v.x, v.y)
        if g is not None:
            gaps.append((v.z - g, v))
    if not gaps:
        continue
    gmin = min(g for g, _ in gaps)
    gmax = max(g for g, _ in gaps)
    c = (a.lo + a.hi) * 0.5
    if gmin > 0.06:
        if any(True for _ in komsular(a, 0.06)):       # asılı flama, kule tepeliği: komşusuna 6 cm'den yakın
            continue
        rapor(1, a.ob, c.x, c.y, "zeminden %.2f m yukarıda, hiçbir şeye değmiyor (boy %.2f)" % (gmin, boy))
    elif gmax > 0.22 and (a.hi.x - a.lo.x) * (a.hi.y - a.lo.y) > 0.25 and boy > 0.15:
        # kenar boşluğu: boşluğun olduğu yerde başka parça (temel, merdiven) var mı?
        g, v = max(gaps, key=lambda t: t[0])
        destek = False
        for b in komsular(a, 0.05):
            if b.lo.z < v.z - 0.05 and b.lo.x - 0.1 <= v.x <= b.hi.x + 0.1 and b.lo.y - 0.1 <= v.y <= b.hi.y + 0.1:
                destek = True
                break
        if not destek:
            rapor(2, a.ob, v.x, v.y, "taban köşesi zeminden %.2f m yukarıda (en alçak köşe %.2f)" % (g, gmin))

# ------------------------------------------------------------------ 3 yol / meydan / dere işgali
def yol_ici(x, y, pay=0.35):
    p = Vector((x, y))
    for n, pts, w in ROADS:
        if poly_dist(p, pts) < w * 0.5 - pay:
            return n
    return None

for a in ADALAR:
    if durum(a.ob):
        continue
    c = (a.lo + a.hi) * 0.5
    # yol: yerden 0.15–2.2 m arasında kalan, yolu kesen parça (kemer üstü serbest)
    if not any(k in a.ob for k in YOL_IZINLI) and a.hi.z - a.lo.z > 0.12:
        sayi, ad = 0, None
        for v in ornek(a.vs, 120):
            g = zz(v.x, v.y)
            if g is None or not (g + 0.12 < v.z < g + 2.2):
                continue
            r = yol_ici(v.x, v.y, pay=0.05)
            if r:
                sayi += 1
                ad = r
        if sayi >= 3:
            rapor(3, a.ob, c.x, c.y, "yolun üstünde: %s (%d köşe)" % (ad, sayi))
    # meydan
    if not any(a.ob.startswith(k) for k in MEYDAN_IZINLI):
        ic = [v for v in ornek(a.vs, 120) if (Vector((v.x, v.y)) - MEYDAN).length < MEYDAN_R - 0.2]
        if len(ic) >= 2:
            rapor(3, a.ob, c.x, c.y, "meydan döşemesine giriyor (%d köşe, merkeze %.1f m)"
                  % (len(ic), min((Vector((v.x, v.y)) - MEYDAN).length for v in ic)))
    # dere
    if not any(k in a.ob for k in SU_IZINLI) and a.ob != "Su_Dere":
        ic = 0
        for v in ornek(a.vs, 120):
            p = Vector((v.x, v.y))
            if poly_dist(p, STREAM) < 2.6 and v.z < height(v.x, v.y) + WATER_Z + 0.25:
                ic += 1
        if ic >= 3:
            rapor(3, a.ob, c.x, c.y, "dere suyunun içinde (%d köşe)" % ic)

# ------------------------------------------------------------------ 4 bina içine giren parça
BINALAR = []
for ob in STATIK:
    if any(ob.name.startswith(b) for b in BINA):
        ads = [a for a in ADALAR if a.ob == ob.name]
        # gövde = en büyük hacimli ada
        g = max(ads, key=lambda a: (a.hi - a.lo).x * (a.hi - a.lo).y * (a.hi - a.lo).z)
        BINALAR.append((ob.name, g.lo + Vector((0.25, 0.25, 0.1)), g.hi - Vector((0.25, 0.25, 0.4))))
# atölye
BINALAR.append(("Atolye", Vector((ATOLYE.x - 4.2, ATOLYE.y - 3.45, 0.1)), Vector((ATOLYE.x + 4.2, ATOLYE.y + 3.45, 5.0))))

def bina_ici(x, y, z, pad=0.0, haric=None):
    for n, lo, hi in BINALAR:
        if n == haric:
            continue
        if lo.x - pad < x < hi.x + pad and lo.y - pad < y < hi.y + pad and lo.z - 0.3 < z < hi.z:
            return n
    return None

for a in ADALAR:
    if any(a.ob.startswith(b) for b in BINA) or durum(a.ob):
        continue
    sayi, ad = 0, None
    for v in ornek(a.vs, 120):
        r = bina_ici(v.x, v.y, v.z)
        if r:
            sayi += 1
            ad = r
    if sayi >= 3:
        c = (a.lo + a.hi) * 0.5
        rapor(4, a.ob, c.x, c.y, "%s gövdesine giriyor (%d köşe)" % (ad, sayi))
# bina-bina
for i, (n1, lo1, hi1) in enumerate(BINALAR):
    for n2_, lo2, hi2 in BINALAR[i + 1:]:
        ox = min(hi1.x, hi2.x) - max(lo1.x, lo2.x)
        oy = min(hi1.y, hi2.y) - max(lo1.y, lo2.y)
        if ox > 0.1 and oy > 0.1:
            rapor(4, n1, (lo1.x + hi1.x) / 2, (lo1.y + hi1.y) / 2, "%s ile iç içe (%.1f × %.1f m)" % (n2_, ox, oy))

# ------------------------------------------------------------------ 5 zemin delinmesi
def delinme(ad, bvh, noktalar, lift_min=0.004):
    kotu = []
    for (x, y) in noktalar:
        s = zemin_z(bvh, x, y)
        t = zemin_z(ARAZI_BVH, x, y)
        if s is None or t is None:
            continue
        if t > s - lift_min:
            kotu.append((x, y, t - s))
    # kümele (3 m)
    kume = []
    for k in kotu:
        for q in kume:
            if abs(q[0] - k[0]) < 3 and abs(q[1] - k[1]) < 3:
                q[3] += 1
                q[2] = max(q[2], k[2])
                break
        else:
            kume.append([k[0], k[1], k[2], 1])
    for x, y, d, cnt in kume:
        rapor(5, ad, x, y, "arazi %d noktada içinden çıkıyor (en çok %.3f m)" % (cnt, d + lift_min))

yol_n = []
for n, pts, w in ROADS:
    for p in resample(pts, 0.5):
        if poly_dist(p, STREAM) < STREAM_HALF:
            continue
        for o in (-w * 0.4, 0.0, w * 0.4):
            i = pts.index(min(pts, key=lambda q: (q - p).length))
            a, b = pts[max(i - 1, 0)], pts[min(i + 1, len(pts) - 1)]
            t = (b - a).normalized()
            q = p + Vector((-t.y, t.x)) * o
            if (q - MEYDAN).length < MEYDAN_R + 0.3:
                continue
            yol_n.append((q.x, q.y))
delinme("SM_Yollar", YOL_BVH, yol_n)
mey_n = []
st = 0.45
for j in range(-30, 31):
    for i in range(-30, 31):
        x, y = MEYDAN.x + i * st, MEYDAN.y + j * st
        if math.hypot(i * st, j * st) < MEYDAN_R - 0.2:
            mey_n.append((x, y))
delinme("SM_Meydan_Doseme", MEYDAN_BVH, mey_n)
if KZ_BVH:
    kz = bpy.data.objects["SM_Koy_Zeminleri"]
    lo = Vector([min((kz.matrix_world @ Vector(c))[k] for c in kz.bound_box) for k in range(3)])
    hi = Vector([max((kz.matrix_world @ Vector(c))[k] for c in kz.bound_box) for k in range(3)])
    kn = []
    x = lo.x
    while x < hi.x:
        y = lo.y
        while y < hi.y:
            # kenardaki etek şeridi bilerek toprağın altına iner: kenara 2 m'den yakın noktalar sayılmaz
            if zemin_z(KZ_BVH, x, y) is not None and all(zemin_z(KZ_BVH, x + dx, y + dy) is not None
                                                          for dx, dy in ((2, 0), (-2, 0), (0, 2), (0, -2))):
                kn.append((x, y))
            y += 0.6
        x += 0.6
    delinme("SM_Koy_Zeminleri", KZ_BVH, kn)

# su: kıyıdan taşma / gömülme
su = bpy.data.objects.get("Su_Dere")
if su:
    SU_BVH = bvh_of(("Su_Dere",))
    sorun = []
    for p in resample(STREAM, 1.0):
        i = STREAM.index(min(STREAM, key=lambda q: (q - p).length))
        a, b = STREAM[max(i - 1, 0)], STREAM[min(i + 1, len(STREAM) - 1)]
        t = (b - a).normalized()
        nrm = Vector((-t.y, t.x))
        for o in (-2.9, 0.0, 2.9):
            q = p + nrm * o
            sz = zemin_z(SU_BVH, q.x, q.y)
            tz = zemin_z(ARAZI_BVH, q.x, q.y)
            if sz is None or tz is None:
                continue
            if o != 0.0 and sz - tz > 0.08:
                sorun.append((q.x, q.y, "su kenarı kıyıdan %.2f m yukarıda (havada şerit)" % (sz - tz)))
            if o == 0.0 and tz > sz - 0.05:
                sorun.append((q.x, q.y, "yatak su yüzünü %.2f m deliyor" % (tz - sz)))
    kume = []
    for x, y, m in sorun:
        for k in kume:
            if abs(k[0] - x) < 6 and abs(k[1] - y) < 6 and k[2].split(" ")[0] == m.split(" ")[0]:
                k[3] += 1
                break
        else:
            kume.append([x, y, m, 1])
    for x, y, m, cnt in kume:
        rapor(5, "Su_Dere", x, y, "%s ×%d" % (m, cnt))

# ------------------------------------------------------------------ 6 doğa yerleşimi
YARICAP = {"Agac": 0.6, "Kavak": 0.5, "Cam": 0.6, "Meyve": 0.5, "Cali": 0.7, "Kaya_Kitle": 2.5, "Kaya": 0.6,
           "Cimen": 0.15, "Cicek": 0.15, "Cakil": 0.2, "Saz": 0.2}
def tur(n):
    for k in ("Kaya_Kitle", "Agac", "Kavak", "Cam", "Meyve", "Cali", "Kaya", "Cimen", "Cicek", "Cakil", "Saz"):
        if "_" + k in n:
            return k
    return None

dogalar = []
for ob in bpy.data.objects:
    c = ob.users_collection[0].name if ob.users_collection else ""
    if c != "Harita_Yerler" or not ob.name.startswith("Yer_"):
        continue
    k = tur(ob.name)
    if k is None:
        continue
    s = max(ob.matrix_world.to_scale())
    dogalar.append((ob.name, k, ob.matrix_world.translation.copy(), YARICAP[k] * s))
print("Doğa örneği:", len(dogalar))
buyuk = ("Agac", "Kavak", "Cam", "Meyve", "Cali", "Kaya", "Kaya_Kitle")
sayac = collections.Counter()
for n, k, p, r in dogalar:
    pp = Vector((p.x, p.y))
    yol = yol_ici(p.x, p.y, pay=-(r if k in buyuk else 0.0))
    if yol and k in buyuk:
        rapor(6, n, p.x, p.y, "%s yolun üstünde: %s" % (k, yol))
    elif yol:
        sayac["ot_yolda"] += 1
    if (pp - MEYDAN).length < MEYDAN_R + (r if k in buyuk else 0.0):
        if k in buyuk:
            rapor(6, n, p.x, p.y, "%s meydanda" % k)
        else:
            sayac["ot_meydanda"] += 1
    b = bina_ici(p.x, p.y, p.z + 0.5, pad=r if k in buyuk else 0.0)
    if b:
        if k in buyuk:
            rapor(6, n, p.x, p.y, "%s binanın içinde: %s" % (k, b))
        else:
            sayac["ot_binada"] += 1
    if k != "Saz" and poly_dist(pp, STREAM) < 2.8 and k != "Kaya_Kitle":
        if k in buyuk:
            rapor(6, n, p.x, p.y, "%s derenin içinde" % k)
        else:
            sayac["ot_suda"] += 1
    # statik adalar ile çakışma (ağaç/kaya bir duvarın, tezgâhın içinde)
    if k in buyuk:
        a = Ada(); a.idx = -1; a.lo = p - Vector((r, r, 0)); a.hi = p + Vector((r, r, 1.5))
        for b2 in komsular(a, 0.0):
            if b2.ob.startswith("SM_Koy_Zemin") or b2.ob == "Su_Dere" or durum(b2.ob):
                continue
            if (b2.hi.x - b2.lo.x) > 25 or (b2.hi.y - b2.lo.y) > 25:     # dev ada (AABB kaba): ayrıca ölçülür
                continue
            if (b2.hi.z - b2.lo.z) > 0.25:
                rapor(6, n, p.x, p.y, "%s şununla iç içe: %s" % (k, b2.ob))
                break
# doğa-doğa
dg = collections.defaultdict(list)
for i, (n, k, p, r) in enumerate(dogalar):
    if k in buyuk:
        dg[(int(p.x // 4), int(p.y // 4))].append(i)
bildirilen = set()
for i, (n, k, p, r) in enumerate(dogalar):
    if k not in buyuk:
        continue
    for gx in (-1, 0, 1):
        for gy in (-1, 0, 1):
            for j in dg.get((int(p.x // 4) + gx, int(p.y // 4) + gy), ()):
                if j <= i:
                    continue
                n2_, k2, p2, r2 = dogalar[j]
                d = (Vector((p.x, p.y)) - Vector((p2.x, p2.y))).length
                if d < (r + r2) * 0.55 and (i, j) not in bildirilen:
                    bildirilen.add((i, j))
                    rapor(6, n, p.x, p.y, "%s ile %.2f m (iç içe)" % (n2_, d))
for k, v in sayac.items():
    rapor(6, "ot/çiçek/çakıl", 0, 0, "%s: %d adet" % (k, v))

# ------------------------------------------------------------------ 7 katman çakışması (üst üste düz yüzeyler)
# Yukarıdan aşağı her yüzeyi (aynı mesh içindeki üst üste parçalar dahil) toplar; arası 1.5 cm'den az olan yukarı
# bakan iki yüzey = titreme/delinme riski. Unity'deki çakışma taramasının harita katmanları için eşi (13.11).
KATMAN = ["SM_Arazi", "SM_Yollar", "SM_Koy_Zeminleri", "SM_Meydan_Doseme", "SM_Ev_Bahceleri", "SM_Denk_Tarlalar", "Su_Dere"]
KATMAN += [o.name for o in bpy.data.objects if o.name.startswith("Tarla_P") and o.name.endswith("_Nadas")]
KBVH = {n: bvh_of((n,)) for n in KATMAN if bpy.data.objects.get(n)}
cift = collections.Counter()
cift_ornek = {}
x = -78.0
while x <= 95.0:
    y = -45.0
    while y <= 40.0:
        yuz = []
        for n, b in KBVH.items():
            if b is None:
                continue
            o = Vector((x, y, 200.0))
            for _ in range(6):
                hit = b.ray_cast(o, DOWN, 400.0)
                if hit[0] is None:
                    break
                if hit[1].z > 0.5:
                    yuz.append((hit[0].z, n))
                o = hit[0] + Vector((0, 0, -0.002))
        yuz.sort(reverse=True)
        for (z1, n1), (z2, n2_) in zip(yuz, yuz[1:]):
            if z1 - z2 < 0.006:
                k = " | ".join(sorted((n1, n2_)))
                cift[k] += 1
                cift_ornek.setdefault(k, (x, y))
        y += 0.4
    x += 0.4
for k, cnt in cift.most_common():
    x_, y_ = cift_ornek[k]
    rapor(7, k, x_, y_, "%d noktada 6 mm.den yakın üst üste yüzey (etek kesişimi dahil)" % cnt)

# ------------------------------------------------------------------ yaz
BASLIK = {1: "Havada parça", 2: "Kenarı havada", 3: "Yol/meydan/dere işgali", 4: "Bina içine giren",
          5: "Zemin delinmesi / su", 6: "Doğa yerleşimi", 7: "Katman çakışması"}
satirlar = []
for kod in sorted(BASLIK):
    ls = [o for o in OUT if o[0] == kod]
    satirlar.append("## %d %s — %d" % (kod, BASLIK[kod], len(ls)))
    for _, ad, x, y, m in sorted(ls, key=lambda o: (o[1], o[2])):
        satirlar.append("  B(%.1f, %.1f)  U(%.1f, %.1f)  %-34s %s" % (x, y, -x, 12.0 - y, ad, m))
metin = "\n".join(satirlar)
print(metin)
print("DENETIM_TAMAM %d bulgu %.1fsn" % (len(OUT), time.time() - T0))
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
if argv:
    with open(argv[0], "w", encoding="utf-8") as fh:
        fh.write(metin + "\n")
