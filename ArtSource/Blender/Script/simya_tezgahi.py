# Simya tezgâhı (orta tezgâh): atölyede bulunan dönem aletleri çalışır düzende.
# Kaynak: ArtSource/Referans/atolye.md §F, esya.md. Ders kitabı (TYMM 9, s.30): Câbir → imbik; Râzî → kroze ve fırın;
# İbn Sînâ → damıtma, süblimleştirme, eritme, kireçleştirme.
# Her alet ayrı obje; hareketli parçalar ayrı mesh (anim_atolye.py iskelete bağlar).
# Önce: ba_lib, atolye_kabuk, atolye_esya, atolye_ic, esya_tasinir.

import math
import random
import bmesh
import bpy
from mathutils import Matrix, Vector

RS = random.Random(5150)
TZ_COL = "Simya_Tezgahi"
TOP = 0.86                       # orta tezgâh üstü
# yerleşim (dünya): tezgâh x -1.3…0.7, y -0.91…0.01
P_DAMITMA = Vector((-0.86, -0.50, TOP))
P_KABILE = Vector((-0.30, -0.52, TOP))
P_HAVAN = Vector((0.08, -0.18, TOP))
P_SUBANYO = Vector((0.40, -0.58, TOP))
P_KOR_KROZE = Vector((-0.02, -0.74, TOP))
P_KORUK = Vector((-0.38, -1.25, 0.46))   # tezgâh dolu: körük önündeki sıranın üstünde, mangal tarafında

def _tc():
    return collection(TZ_COL)

def _ob(bm, name, loc, rot=(0, 0, 0), mat=None, sharp=45):
    return to_object(bm, name, _tc(), loc=tuple(loc), rot=rot, sharp_angle=sharp, mat=mat)

def m_cam():
    mat = bpy.data.materials.get("M_Cam")
    if mat:
        return mat
    mat = bpy.data.materials.new("M_Cam")
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    vc = nt.nodes.new("ShaderNodeVertexColor")
    vc.layer_name = "Col"
    nt.links.new(vc.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.15
    bsdf.inputs["Alpha"].default_value = 0.35
    mat.surface_render_method = "BLENDED"
    return mat

def m_duman():
    return material_named("M_Duman", "#DDDCC5", emission=0.0, alpha=0.6)

def m_kopuk():
    return material_named("M_Kopuk", "#C8F6FF", emission=0.3)

def m_su():
    return material_named("M_Su", "#35BDEB")

def m_fener():
    return material_named("M_Fener", "#FFB13D", emission=4.0)

# ------------------------------------------------------------------ ortak parçalar
def mangal_bm(r=0.17, seed=0):
    """Üç ayaklı kil mangal: kalın kâse, ağız kenarı isli, içinde kömür; üstte kap için demir halka ve 3 dil."""
    parts = []
    bowl = bm_lathe(chaikin([(0, 0.08), (r * 0.7, 0.08), (r * 0.95, 0.10), (r * 1.05, 0.16), (r * 1.1, 0.19), (r * 0.98, 0.20),
                             (r * 0.85, 0.13), (0, 0.12)], 1), sides=18)
    paint_by(bowl, lambda f, l: mix(pal("kil", 1), pal("kil", 0), min(1, (l.vert.co.z - 0.08) / 0.1))
             if l.vert.co.z < 0.175 else mix(pal("kil", 2), demir(0.7), 0.4))
    parts.append((bowl, None))
    for k in range(3):
        a = 2 * math.pi * k / 3 + 0.5
        secs = []
        for i in range(5):
            t = i / 4
            c = Vector((math.cos(a) * (r * 0.6 + r * 0.35 * t), math.sin(a) * (r * 0.6 + r * 0.35 * t), 0.09 - 0.09 * t))
            w = 0.025 - 0.008 * t
            secs.append([c + Vector((-w, -w, 0)), c + Vector((w, -w, 0)), c + Vector((w, w, 0)), c + Vector((-w, w, 0))])
        leg = bm_sweep(secs)
        paint_part(leg, "kil", lo=2, hi=1, seed=seed + k)
        parts.append((leg, None))
    rr = random.Random(seed)
    for k in range(9):    # kömür
        cb = bm_box(rr.uniform(0.04, 0.07), rr.uniform(0.035, 0.06), rr.uniform(0.03, 0.045), bevel=0.01, origin="center")
        jitter(cb, 0.006, seed=seed + 10 + k)
        paint_flat(cb, mix(demir(0.5), pal("ahsap", 2), rr.uniform(0.0, 0.3)))
        a = rr.uniform(0, 6.28)
        d = rr.uniform(0, r * 0.6)
        parts.append((cb, Matrix.Translation((math.cos(a) * d, math.sin(a) * d, 0.14)) @ Matrix.Rotation(rr.uniform(0, 3), 4, "Z")))
    ring = bm_lathe([(r * 0.62, 0.0), (r * 0.7, 0.0), (r * 0.7, 0.012), (r * 0.62, 0.012)], sides=18, close_top=False, close_bottom=False)
    iron(ring)
    parts.append((ring, Matrix.Translation((0, 0, 0.215))))
    for k in range(3):
        a = 2 * math.pi * k / 3
        pr = bm_box(0.012, 0.012, 0.05, bevel=0.002)
        iron(pr)
        parts.append((pr, Matrix.Translation((math.cos(a) * r * 0.85, math.sin(a) * r * 0.85, 0.175))))
    return bm_merge(*parts)

def kor_bm(r=0.17, seed=0):
    """Kızgın korlar (M_Fener): kömürlerin arasında parlayan parçalar — mangal yanınca görünür."""
    rr = random.Random(seed)
    parts = []
    for k in range(6):
        cb = bm_box(rr.uniform(0.03, 0.05), rr.uniform(0.03, 0.045), rr.uniform(0.02, 0.03), bevel=0.008, origin="center")
        jitter(cb, 0.004, seed=seed + k)
        paint_flat(cb, mix(pal("cati", 0), pal("altin", 0), rr.uniform(0.0, 0.6)))
        a = rr.uniform(0, 6.28)
        d = rr.uniform(0, r * 0.5)
        parts.append((cb, Matrix.Translation((math.cos(a) * d, math.sin(a) * d, 0.155))))
    return bm_merge(*parts)

def alev_bm(scale=1.0, seed=0):
    """Dilli alev: üç yumuşak koni (M_Fener). Pivot tabanda (alev kökü)."""
    rr = random.Random(seed)
    fl = []
    for i, (dx, dy, hh, r0) in enumerate(((0, 0, 0.16, 0.05), (-0.05, 0.02, 0.11, 0.035), (0.045, -0.02, 0.12, 0.04))):
        prof = chaikin([(0.0, 0.0), (r0, 0.01), (r0 * 1.05, hh * 0.3), (r0 * 0.55, hh * 0.68), (0.0, hh)], 1)
        bm = bm_lathe(prof, sides=8)
        paint_by(bm, lambda f, l, hh=hh: mix(pal("altin", 0), pal("cati", 0), min(1, l.vert.co.z / hh)))
        fl.append((bm, Matrix.Translation((dx, dy, 0)) @ Matrix.Rotation(rr.uniform(-0.15, 0.15), 4, "X")))
    out = bm_merge(*fl)
    transform_bm(out, Matrix.Scale(scale, 4))
    return out

def tube_bm(points, r0, r1, sides=8, col=None):
    """Nokta dizisi boyunca daralan boru (gaga)."""
    secs = []
    n = len(points)
    for i, p in enumerate(points):
        p = Vector(p)
        if i < n - 1:
            d = (Vector(points[i + 1]) - p).normalized()
        else:
            d = (p - Vector(points[i - 1])).normalized()
        up = Vector((0, 0, 1)) if abs(d.z) < 0.9 else Vector((1, 0, 0))
        s1 = d.cross(up).normalized()
        s2 = s1.cross(d).normalized()
        r = r0 + (r1 - r0) * i / (n - 1)
        secs.append([p + (s1 * math.cos(2 * math.pi * k / sides) + s2 * math.sin(2 * math.pi * k / sides)) * r for k in range(sides)])
    bm = bm_sweep(secs)
    if col:
        paint_flat(bm, col)
    return bm

# ------------------------------------------------------------------ İMBİK (damıtma) düzeneği
def build_damitma():
    """Câbir'e atfedilen imbik: kar'a (gövde) + inbîk (oluklu başlık, gaga) + kâbile (toplama).
    Objeler: SM_Damitma_Mangal, SM_Damitma_Govde (kar'a+başlık+gaga), Damitma_Kor, Damitma_Ates,
    SM_Kabile (cam, M_Cam), Kabile_Sivi (M_Su), Damla_1..3 (M_Su)."""
    out = {}
    out["mangal"] = _ob(mangal_bm(0.17, 6001), "SM_Damitma_Mangal", P_DAMITMA)
    out["kor"] = _ob(kor_bm(0.17, 6002), "Damitma_Kor", P_DAMITMA, mat=m_fener(), sharp=0)
    out["ates"] = _ob(alev_bm(0.7, 6003), "Damitma_Ates", P_DAMITMA + Vector((0, 0, 0.15)), mat=m_fener(), sharp=0)
    parts = []
    z0 = 0.205
    gourd = bm_lathe(chaikin([(0, 0), (0.06, 0), (0.11, 0.035), (0.135, 0.095), (0.125, 0.16), (0.085, 0.22), (0.052, 0.26),
                              (0.048, 0.295), (0.056, 0.30)], 1), sides=18, close_top=False)
    paint_by(gourd, lambda f, l: mix(mix(demir(0.7), pal("bakir", 2), 0.5), pal("bakir", 0), min(1, l.vert.co.z / 0.25)))
    parts.append((gourd, Matrix.Translation((0, 0, z0))))
    for zz in (0.09, 0.15):   # perçinli bant
        bd = bm_lathe([(0.135, zz), (0.142, zz + 0.008), (0.135, zz + 0.016)], sides=18, close_top=False, close_bottom=False)
        paint_flat(bd, pal("bakir", 1))
        parts.append((bd, Matrix.Translation((0, 0, z0))))
    parts.append((_lute_ring(0.056, z0 + 0.305, h=0.022), None))         # bilgelik çamuru
    hz = z0 + 0.30
    head = bm_lathe(chaikin([(0.05, 0.0), (0.075, 0.012), (0.098, 0.03), (0.104, 0.07), (0.085, 0.12), (0.045, 0.155), (0.0, 0.165)], 1),
                    sides=18, close_bottom=False)
    paint_by(head, lambda f, l: mix(pal("bakir", 1), pal("bakir", 0), min(1, l.vert.co.z / 0.15)))
    parts.append((head, Matrix.Translation((0, 0, hz))))
    gut = bm_lathe([(0.098, 0.028), (0.112, 0.024), (0.114, 0.036), (0.1, 0.04)], sides=18, close_top=False, close_bottom=False)
    paint_flat(gut, pal("bakir", 2))                                      # iç oluk dudağı (dışa yansıyan kenar)
    parts.append((gut, Matrix.Translation((0, 0, hz))))
    knob = bm_lathe([(0, 0), (0.018, 0.0), (0.02, 0.012), (0.01, 0.03), (0, 0.032)], sides=10)
    paint_flat(knob, pal("bakir", 0))
    parts.append((knob, Matrix.Translation((0, 0, hz + 0.16))))
    # gaga: oluk hizasından çıkar, aşağı eğimle kâbileye iner (yerel +X → kâbile)
    d = P_KABILE - P_DAMITMA
    tip = Vector((d.x, d.y, 0.32))
    beak_pts = [(0.10, 0, hz + 0.035), (0.20, d.y * 0.3, hz + 0.0), (0.32, d.y * 0.6, hz - 0.08), (tip.x - 0.06, d.y * 0.9, tip.z + 0.05),
                (tip.x, d.y, tip.z)]
    beak = tube_bm(beak_pts, 0.022, 0.009, sides=10, col=None)
    paint_by(beak, lambda f, l: mix(pal("bakir", 1), pal("bakir", 0), 0.4))
    parts.append((beak, None))
    out["govde"] = _ob(bm_merge(*parts), "SM_Damitma_Govde", P_DAMITMA)
    # kâbile: ahşap halka sehpada hafif eğik cam balon, ağzı gagaya bakar
    st = []
    ring = bm_lathe([(0.055, 0.0), (0.085, 0.0), (0.085, 0.05), (0.07, 0.07), (0.055, 0.06)], sides=16)
    _wood(ring, 6010, lo=1, hi=0)
    st.append((ring, None))
    out["kabile_sehpa"] = _ob(bm_merge(*st), "SM_Kabile_Sehpa", P_KABILE)
    rec = bm_lathe(chaikin([(0, 0.004), (0.04, 0.0), (0.07, 0.03), (0.082, 0.075), (0.07, 0.12), (0.035, 0.15), (0.02, 0.16),
                            (0.018, 0.21), (0.024, 0.215)], 1), sides=18, close_top=False)
    paint_by(rec, lambda f, l: mix(glass(), pal("su", 0), 0.3))
    tilt = Matrix.Rotation(math.radians(-18), 4, "Y")
    out["kabile"] = _ob(rec, "SM_Kabile", P_KABILE + Vector((0, 0, 0.045)), mat=m_cam(), sharp=60)
    # kâbiledeki damıtılmış sıvı: ayrı mesh, tabandan ölçeklenir (Kabile_Sivi kemiği)
    liq = bm_lathe(chaikin([(0, 0.014), (0.034, 0.012), (0.058, 0.035), (0.068, 0.072), (0.0, 0.072)], 1), sides=16)
    paint_flat(liq, mix(pal("su", 0), pal("siva", 0), 0.3))
    out["sivi"] = _ob(liq, "Kabile_Sivi", P_KABILE + Vector((0, 0, 0.045)), mat=m_su(), sharp=0)
    # damlalar: gaga ucunda
    tip_w = P_DAMITMA + tip
    out["damlalar"] = []
    for k in range(3):
        dr = bm_lathe(chaikin([(0, 0), (0.007, 0.004), (0.008, 0.01), (0.004, 0.018), (0, 0.022)], 1), sides=8)
        transform_bm(dr, Matrix.Translation((0, 0, -0.011)))
        paint_flat(dr, mix(pal("su", 0), pal("siva", 0), 0.4))
        out["damlalar"].append(_ob(dr, "Damla_%d" % (k + 1), tip_w + Vector((0, 0, -0.012)), mat=m_su(), sharp=0))
    out["tip"] = tip_w
    return out

# ------------------------------------------------------------------ SU BANYOSU (hammâm / bain-marie)
def build_su_banyosu():
    out = {}
    out["mangal"] = _ob(mangal_bm(0.15, 6101), "SM_SuBanyosu_Mangal", P_SUBANYO)
    out["kor"] = _ob(kor_bm(0.15, 6102), "SuBanyosu_Kor", P_SUBANYO, mat=m_fener(), sharp=0)
    out["ates"] = _ob(alev_bm(0.55, 6103), "SuBanyosu_Ates", P_SUBANYO + Vector((0, 0, 0.14)), mat=m_fener(), sharp=0)
    parts = []
    z0 = 0.205
    basin = bm_lathe(chaikin([(0, 0), (0.12, 0), (0.135, 0.015), (0.14, 0.13), (0.152, 0.145), (0.125, 0.145), (0.122, 0.03), (0, 0.025)], 1),
                     sides=18)
    paint_by(basin, lambda f, l: mix(mix(demir(0.7), pal("bakir", 2), 0.5), pal("bakir", 0), min(1, l.vert.co.z / 0.14)))
    parts.append((basin, Matrix.Translation((0, 0, z0))))
    for s in (-1, 1):
        hd = bm_lathe([(0.02, -0.005), (0.026, 0), (0.02, 0.005), (0.014, 0)], sides=8, close_top=False, close_bottom=False)
        paint_flat(hd, pal("bakir", 2))
        parts.append((hd, Matrix.Translation((s * 0.16, 0, z0 + 0.11)) @ Matrix.Rotation(math.pi / 2, 4, "Y")))
    water = bm_cyl(0.122, 0.122, 0.004, sides=18)
    paint_flat(water, pal("su", 0))
    inner = bm_lathe(chaikin([(0, 0.03), (0.05, 0.03), (0.07, 0.07), (0.072, 0.17), (0.05, 0.21), (0.04, 0.24), (0.046, 0.25), (0, 0.245)], 1), sides=16)
    paint_part(inner, "kil", lo=1, hi=0, seed=6104)
    parts.append((inner, Matrix.Translation((0, 0, z0))))
    out["govde"] = _ob(bm_merge(*parts), "SM_SuBanyosu", P_SUBANYO)
    out["su"] = _ob(water, "SuBanyosu_Su", P_SUBANYO + Vector((0, 0, z0 + 0.12)), mat=m_su(), sharp=0)
    out["buhar"] = []
    for k in range(2):
        pf = bmesh.new()
        bmesh.ops.create_icosphere(pf, subdivisions=2, radius=0.05)
        jitter(pf, 0.008, seed=6110 + k)
        paint_flat(pf, pal("siva", 0))
        out["buhar"].append(_ob(pf, "SuBanyosu_Buhar_%d" % (k + 1), P_SUBANYO + Vector((0.04 * (1 - 2 * k), 0, z0 + 0.16)), mat=m_duman(), sharp=0))
    return out

# ------------------------------------------------------------------ HAVAN (dövme)
def build_havan():
    out = {}
    bm, info = havan_tunc(6201)
    transform_bm(bm, Matrix.Scale(1.25, 4))
    out["havan"] = _ob(bm, "SM_Tezgah_Havan", P_HAVAN)
    pes, _ = havaneli(6202)
    # havaneli dik: yatık hâlden çevir (havaneli() yatay üretir) → ağır ucu aşağıda
    pes2 = bm_lathe([(0, 0), (0.026, 0.004), (0.031, 0.024), (0.024, 0.048), (0.016, 0.072), (0.015, 0.21), (0.019, 0.25), (0.024, 0.27),
                     (0.018, 0.29), (0, 0.295)], sides=14)
    paint_part(pes2, "altin", lo=1, hi=0, seed=6203)
    out["havaneli"] = _ob(pes2, "Tezgah_Havaneli", P_HAVAN + Vector((0, 0, 0.06)))
    # havanda ezilen malzeme (kükürt sarısı toz)
    pw = bm_lathe([(0, 0.0), (0.07, 0.0), (0.06, 0.012), (0, 0.02)], sides=14)
    jitter(pw, 0.003, seed=6204)
    paint_flat(pw, pal("altin", 0))
    out["toz"] = _ob(pw, "SM_Havan_Toz", P_HAVAN + Vector((0, 0, 0.058)))
    return out

# ------------------------------------------------------------------ EL KÖRÜĞÜ
def build_koruk():
    """Tezgâhta yatan el körüğü: alt kapak sabit, üst kapak burun tarafındaki menteşeden açılır; deri körük esner."""
    out = {}
    L = 0.42
    def plate(seed):
        pl = bm_prism([(0.0, -0.11), (0.0, 0.11), (0.28, 0.12), (0.34, 0.06), (0.36, -0.06), (0.28, -0.12)], -0.01, 0.01)
        transform_bm(pl, Matrix.Rotation(math.pi / 2, 4, "X"))   # XY düzlemine yatır
        bevel_all(pl, 0.005)
        _wood(pl, seed, lo=1, hi=0)
        for k in range(3):   # pirinç süs çivileri
            nb = bm_cyl(0.008, 0.006, 0.006, sides=6)
            paint_part(nb, "altin", lo=1, hi=0, seed=seed)
            pl = bm_merge((pl, None), (nb, Matrix.Translation((0.12 + k * 0.07, 0.0, 0.01))))
        return pl
    lower = plate(6301)
    nozzle = bm_cyl(0.02, 0.008, 0.14, sides=8)
    paint_part(nozzle, "bakir", lo=1, hi=0, seed=6302)
    handle = []
    for s in (-1, 1):
        h = bm_cyl(0.014, 0.012, 0.12, sides=6)
        _wood(h, 6303, lo=1, hi=0)
        handle.append(h)
    low = bm_merge((lower, None), (nozzle, Matrix.Translation((0.0, 0, 0.01)) @ Matrix.Rotation(-math.pi / 2, 4, "Y")),
                   (handle[0], Matrix.Translation((0.36, 0, 0.0)) @ Matrix.Rotation(math.pi / 2, 4, "Y")))
    # yerel X: burun (−) → saplar (+); masada burun mangala bakar
    yaw = math.radians(8)          # sıra boyunca yatık, burun batıya (mangal tarafı)
    out["alt"] = _ob(low, "SM_Koruk_Alt", P_KORUK + Vector((0, 0, 0.012)), rot=(0, 0, math.degrees(yaw)))
    up = bm_merge((plate(6304), None), (handle[1], Matrix.Translation((0.36, 0, 0.0)) @ Matrix.Rotation(math.pi / 2, 4, "Y")))
    out["ust"] = _ob(up, "Koruk_Ust", P_KORUK + Vector((0, 0, 0.012 + 0.07)), rot=(0, 0, math.degrees(yaw)))
    lea = bm_lathe(chaikin([(0.0, 0.0), (0.1, 0.0), (0.11, 0.035), (0.1, 0.07), (0.0, 0.07)], 1), sides=12)
    transform_bm(lea, Matrix.Scale(1.25, 4, (1, 0, 0)))
    transform_bm(lea, Matrix.Translation((0.16, 0, 0)))
    paint_part(lea, "ahsap", lo=2, hi=2, seed=6305)
    out["deri"] = _ob(lea, "Koruk_Deri", P_KORUK + Vector((0, 0, 0.012)), rot=(0, 0, math.degrees(yaw)))
    out["yaw"] = yaw
    return out

# ------------------------------------------------------------------ KROZE + MAŞA (taş levha üstünde)
def build_kroze_kosesi():
    parts = []
    slab = bm_box(0.30, 0.20, 0.03, bevel=0.01)
    jitter(slab, 0.004, seed=6401)
    paint_part(slab, "kaya", lo=1, hi=0, seed=6402)
    parts.append((slab, None))
    for k, (hh, rr, dx) in enumerate(((0.11, 0.06, -0.06), (0.08, 0.045, 0.07))):
        kb, _ = kroze_aleti(6403 + k, hh=hh, rr=rr)
        parts.append((kb, Matrix.Translation((dx, 0.0, 0.03)) @ Matrix.Rotation(k * 0.8, 4, "Z")))
    ms, _ = masa_aleti(6405)
    transform_bm(ms, Matrix.Scale(0.75, 4))
    parts.append((ms, Matrix.Translation((0.05, -0.15, -0.0)) @ Matrix.Rotation(0.15, 4, "Z")))
    _ob(bm_merge(*parts), "SM_Kroze_Kosesi", P_KOR_KROZE)

def build_tezgah_mumu():
    m = candle(0.58, -0.06, TOP, h=0.15, seed=6501)
    _ob(m, "SM_Tezgah_Mumu", Vector((0.58, -0.06, TOP)))

def build_tezgah():
    clear_collection(TZ_COL)
    FLAMES.clear()
    res = {"damitma": build_damitma(), "su": build_su_banyosu(), "havan": build_havan(), "koruk": build_koruk()}
    build_kroze_kosesi()
    build_tezgah_mumu()
    if FLAMES:
        _ob(bm_merge(*FLAMES), "SM_Tezgah_Alevler", Vector((0, 0, 0)), mat=m_fener(), sharp=0)
    return res
