# İnsan NPC'ler: Câbir Usta, Hesap Hocası, Köpük Efendi, köylü erkek ve kadın, ulak.
# Referans: ArtSource/Referans/npc.md. Kurallar: 09-tema-ve-model-kurallari.md.
# Yarışma süresince yazıldı (0'dan üret; hazır mesh/üretici kullanılmadı). Blender 5.x.
#
# Kullanım (Blender içinden):
#   p = r"<repo>/ArtSource/Blender/Script/npc.py"
#   exec(compile(open(p, encoding="utf-8").read(), p, "exec"), globals())
#   build_all_npc()        # beş karakteri yan yana kurar (rig + animasyon)
#   export_all_npc()       # Assets/_Project/Art/NPC/NPC_<Ad>.fbx
#
# Yön: karakterin önü Blender -Y (Unity +Z). Zemin z=0. Karakterin solu +X.
# Her karakter: Armatür "NPC_<Ad>" + tek mesh "NPC_<Ad>_Govde" (Armature modifier, katı ağırlık).

import os
import math
import random
import bmesh
import bpy
from mathutils import Vector, Matrix, Euler

_HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else \
    r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\ArtSource\Blender\Script"
_lib = os.path.join(_HERE, "ba_lib.py")
exec(compile(open(_lib, encoding="utf-8").read(), _lib, "exec"), globals())

COL = "NPC"
EXPORT_DIR = r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\Assets\_Project\Art\NPC"
FPS = 30

BONES = ["Kalca", "Omurga", "Gogus", "Boyun", "Bas",
         "Kol_Ust_Sol", "Kol_Alt_Sol", "El_Sol", "Kol_Ust_Sag", "Kol_Alt_Sag", "El_Sag",
         "Bacak_Ust_Sol", "Bacak_Alt_Sol", "Ayak_Sol", "Bacak_Ust_Sag", "Bacak_Alt_Sag", "Ayak_Sag"]

# ------------------------------------------------------------------ renkler (yalnız paletten / türetilmiş)
def tone(g, t=0.5):
    """Grubun gölge→aydınlık arası tonu (0 = gölge, 1 = aydınlık)."""
    if t < 0.5:
        return mix(pal(g, 2), pal(g, 1), t * 2)
    return mix(pal(g, 1), pal(g, 0), (t - 0.5) * 2)

def ten(t=0.6, koyu=0.0):
    """Ten: kil aydınlık + çatı aydınlık (türetilmiş, 09 §3 'Ten' satırı). koyu: ahşaba doğru."""
    a = mix(pal("kil", 1), pal("cati", 1), 0.22)
    b = mix(pal("kil", 0), pal("cati", 0), 0.20)
    c = mix(a, b, t)
    return mix(c, pal("ahsap", 1), koyu)

def murekkep():
    """Göz/kaş koyusu: ahşap gölge + dağ gölge, koyulaştırılmış (saf siyah değil)."""
    c = mix(pal("ahsap", 2), pal("dag", 2), 0.35)
    return (c[0] * 0.45, c[1] * 0.45, c[2] * 0.5, 1.0)

def kumas(g, t_lo=0.25, t_hi=0.75):
    return (tone(g, t_lo), tone(g, t_hi))

def blend2(g1, g2, k, t_lo=0.25, t_hi=0.75):
    return (mix(tone(g1, t_lo), tone(g2, t_lo), k), mix(tone(g1, t_hi), tone(g2, t_hi), k))

# ------------------------------------------------------------------ geometri yardımcıları
def ell(c, r, u=10, v=7):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=1.0)
    bmesh.ops.scale(bm, vec=r, verts=bm.verts)
    bmesh.ops.translate(bm, vec=c, verts=bm.verts)
    return bm

def tube(a, b, r1, r2, sides=8, cap=True):
    a, b = Vector(a), Vector(b)
    d = b - a
    bm = bm_cyl(r1, r2, d.length, sides=sides, cap=cap)
    q = Vector((0, 0, 1)).rotation_difference(d.normalized())
    transform_bm(bm, Matrix.Translation(a) @ q.to_matrix().to_4x4())
    return bm

def box(c, s, rot=(0, 0, 0), bevel=0.0):
    bm = bm_box(s[0], s[1], s[2], bevel=bevel, origin="center")
    M = Matrix.Translation(c) @ Euler([math.radians(x) for x in rot]).to_matrix().to_4x4()
    return transform_bm(bm, M)

def lathe(profile, sides=12, sy=1.0, c=(0, 0, 0)):
    bm = bm_lathe(profile, sides=sides)
    bmesh.ops.scale(bm, vec=(1, sy, 1), verts=bm.verts)
    bmesh.ops.translate(bm, vec=c, verts=bm.verts)
    return bm

def torus(R, r, c=(0, 0, 0), rot=(0, 0, 0), sides=12, ring=6, sy=1.0):
    prof = [(R + r * math.cos(2 * math.pi * i / ring), r * math.sin(2 * math.pi * i / ring)) for i in range(ring + 1)]
    bm = bm_lathe(prof, sides=sides, close_top=False, close_bottom=False)
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=1e-5)
    bmesh.ops.scale(bm, vec=(1, sy, 1), verts=bm.verts)
    M = Matrix.Translation(c) @ Euler([math.radians(x) for x in rot]).to_matrix().to_4x4()
    return transform_bm(bm, M)

def bent_cone(base_c, r0, h, bend, sides=10, n=7, tip_r=0.012):
    """Ucu arkaya (+Y) bükülen sivri külah."""
    secs = []
    for i in range(n + 1):
        t = i / n
        cz = base_c[2] + h * t
        cy = base_c[1] + bend * t ** 2.2
        r = r0 * (1 - t) ** 0.9 + tip_r * t
        secs.append([(base_c[0] + math.cos(2 * math.pi * k / sides) * r,
                      cy + math.sin(2 * math.pi * k / sides) * r, cz) for k in range(sides)])
    return bm_sweep(secs)

def paint(bm, cols, noise=0.04, seed=0, axis=2):
    lo, hi = cols if isinstance(cols[0], tuple) else (cols, cols)
    paint_gradient(bm, lo, hi, axis=axis, noise=noise, seed=seed)
    return bm

# ------------------------------------------------------------------ iskelet
def skeleton(H, hr, hip_k=0.45, sh_w=0.21, arm_out=0.05, stoop=0.0):
    head_c = H - hr * 1.08
    chin = head_c - hr * 0.95
    neck_b = chin - 0.05
    sh_z = neck_b - 0.03
    hip = H * hip_k
    J = {
        "H": H, "hr": hr, "head_c": Vector((0, -stoop, head_c)), "chin": chin,
        "hip": Vector((0, 0, hip)),
        "waist": Vector((0, 0, hip + (sh_z - hip) * 0.33)),
        "chest": Vector((0, -stoop * 0.4, hip + (sh_z - hip) * 0.72)),
        "neck": Vector((0, -stoop * 0.8, neck_b)),
        "head_b": Vector((0, -stoop, chin + 0.02)),
        "head_t": Vector((0, -stoop, H)),
    }
    ua, fa = 0.165 * H, 0.15 * H
    for side, sx in (("Sol", 1), ("Sag", -1)):
        sh = Vector((sx * sh_w, -stoop * 0.6, sh_z - 0.03))
        el = sh + Vector((sx * arm_out, 0.015, -ua))
        wr = el + Vector((sx * 0.012, -0.04, -fa))
        J["sh_" + side], J["el_" + side], J["wr_" + side] = sh, el, wr
        J["hd_" + side] = wr + Vector((0, -0.01, -0.10))
        hj = Vector((sx * 0.09, 0, hip - 0.03))
        kn = Vector((sx * 0.10, -0.01, hip * 0.53))
        an = Vector((sx * 0.10, 0.0, 0.09))
        J["hj_" + side], J["kn_" + side], J["an_" + side] = hj, kn, an
        J["to_" + side] = an + Vector((0, -0.14, -0.05))
    return J

def bone_pairs(J):
    return {
        "Kalca": (J["hip"], J["waist"]), "Omurga": (J["waist"], J["chest"]),
        "Gogus": (J["chest"], J["neck"]), "Boyun": (J["neck"], J["head_b"]),
        "Bas": (J["head_b"], J["head_t"]),
        **{f"Kol_Ust_{s}": (J["sh_" + s], J["el_" + s]) for s in ("Sol", "Sag")},
        **{f"Kol_Alt_{s}": (J["el_" + s], J["wr_" + s]) for s in ("Sol", "Sag")},
        **{f"El_{s}": (J["wr_" + s], J["hd_" + s]) for s in ("Sol", "Sag")},
        **{f"Bacak_Ust_{s}": (J["hj_" + s], J["kn_" + s]) for s in ("Sol", "Sag")},
        **{f"Bacak_Alt_{s}": (J["kn_" + s], J["an_" + s]) for s in ("Sol", "Sag")},
        **{f"Ayak_{s}": (J["an_" + s], J["to_" + s]) for s in ("Sol", "Sag")},
    }

BONE_PARENT = {"Omurga": "Kalca", "Gogus": "Omurga", "Boyun": "Gogus", "Bas": "Boyun",
               "Kol_Ust_Sol": "Gogus", "Kol_Alt_Sol": "Kol_Ust_Sol", "El_Sol": "Kol_Alt_Sol",
               "Kol_Ust_Sag": "Gogus", "Kol_Alt_Sag": "Kol_Ust_Sag", "El_Sag": "Kol_Alt_Sag",
               "Bacak_Ust_Sol": "Kalca", "Bacak_Alt_Sol": "Bacak_Ust_Sol", "Ayak_Sol": "Bacak_Alt_Sol",
               "Bacak_Ust_Sag": "Kalca", "Bacak_Alt_Sag": "Bacak_Ust_Sag", "Ayak_Sag": "Bacak_Alt_Sag"}

# ------------------------------------------------------------------ ortak parçalar
def add_head(P, J, skin, brow_col, nose="yumru", ears=True, mouth=True, brow_k=1.0, seed=0):
    hr, hc = J["hr"], J["head_c"]
    P.append((paint(ell(hc, (hr * 0.93, hr * 0.98, hr * 1.06), u=12, v=9), skin, seed=seed), "Bas"))
    # çene/yanak kütlesi: yüzün alt yarısı hafif öne
    P.append((paint(ell(hc + Vector((0, -hr * 0.25, -hr * 0.45)), (hr * 0.72, hr * 0.7, hr * 0.55)), skin), "Bas"))
    fy = hc.y - hr * 0.93
    if nose == "yumru":
        P.append((paint(ell(hc + Vector((0, -hr * 1.02, -0.014)), (0.03, 0.036, 0.034), u=10, v=8), skin), "Bas"))
    elif nose == "uzun":
        P.append((paint(tube(hc + Vector((0, -hr * 0.82, 0.004)), hc + Vector((0, -hr * 1.42, -0.035)),
                             0.024, 0.011, sides=7), skin), "Bas"))
        P.append((paint(ell(hc + Vector((0, -hr * 1.42, -0.035)), (0.014, 0.014, 0.014), u=6, v=5), skin), "Bas"))
    elif nose == "top":
        P.append((paint(ell(hc + Vector((0, -hr * 1.0, -0.016)), (0.034, 0.034, 0.034), u=8, v=6), skin), "Bas"))
    eye = murekkep()
    for sx in (1, -1):
        P.append((paint(ell(hc + Vector((sx * hr * 0.33, -hr * 0.86, 0.018)), (0.014, 0.010, 0.020), u=6, v=5), eye), "Bas"))
        P.append((paint(box(hc + Vector((sx * hr * 0.34, -hr * 0.9, 0.05)), (0.05 * brow_k, 0.018, 0.016 * brow_k),
                            rot=(0, sx * 8, 0), bevel=0.004), brow_col), "Bas"))
        if ears:
            P.append((paint(ell(hc + Vector((sx * hr * 0.93, 0.005, 0.0)), (0.018, 0.03, 0.042), u=6, v=5), skin), "Bas"))
    if mouth:
        P.append((paint(box(hc + Vector((0, -hr * 0.985, -hr * 0.45)), (0.042, 0.014, 0.01), rot=(-25, 0, 0), bevel=0.003),
                        mix(skin[0] if isinstance(skin[0], tuple) else skin, eye, 0.6)), "Bas"))

def add_neck(P, J, skin, r=0.05):
    P.append((paint(tube(J["neck"] + Vector((0, 0, -0.04)), J["head_b"] + Vector((0, 0, 0.03)), r, r * 0.9), skin), "Boyun"))

def add_arm(P, J, side, sleeve, hand, sleeve_r=(0.062, 0.056, 0.05), flare=0.0, cuff=None, hand_k=1.0,
            fore_skin=False):
    sh, el, wr = J["sh_" + side], J["el_" + side], J["wr_" + side]
    # omuz topu (kumaş kütlesi, eklemi örter)
    sx = 1 if side == "Sol" else -1
    r0 = sleeve_r[0] * 1.12
    P.append((paint(ell(sh + Vector((-sx * 0.012, 0, -0.025)), (r0, r0 * 0.95, r0), u=8, v=6), sleeve), "Gogus"))
    P.append((paint(tube(sh + Vector((0, 0, -0.03)), el, sleeve_r[0], sleeve_r[1]), sleeve), "Kol_Ust_" + side))
    # dirsek: küre tüplerden biraz iri → kesişim tırtığı (z-fighting) yerine yumuşak düğüm
    P.append((paint(ell(el, (sleeve_r[1] * 1.08,) * 3, u=8, v=6), sleeve), "Kol_Ust_" + side))
    fore = hand if fore_skin else sleeve
    P.append((paint(tube(el, wr + (wr - el).normalized() * (0.02 + flare * 0.3),
                         sleeve_r[1] * (0.8 if fore_skin else 0.92), sleeve_r[2] + flare, sides=10 if flare else 8), fore),
              "Kol_Alt_" + side))
    if cuff:
        P.append((paint(torus(sleeve_r[2] + flare, 0.014, c=wr + (wr - el).normalized() * (0.02 + flare * 0.3),
                              sides=10, ring=5), cuff), "Kol_Alt_" + side))
    # el: iri, eldiven gibi (avuç + başparmak)
    hc = wr + Vector((0, -0.005, -0.065)) * hand_k
    P.append((paint(ell(hc, (0.042 * hand_k, 0.052 * hand_k, 0.066 * hand_k), u=8, v=6), hand), "El_" + side))
    P.append((paint(ell(hc + Vector((-sx * 0.012, -0.045, 0.02)) * 1.0, (0.018 * hand_k, 0.02 * hand_k, 0.032 * hand_k),
                        u=6, v=5), hand), "El_" + side))

def add_legs(P, J, hose, upper_r=0.075, lower_r=0.055):
    for s in ("Sol", "Sag"):
        hj, kn, an = J["hj_" + s], J["kn_" + s], J["an_" + s]
        P.append((paint(tube(hj, kn, upper_r, lower_r * 1.1), hose), "Bacak_Ust_" + s))
        P.append((paint(tube(kn, an, lower_r * 1.1, lower_r * 0.85), hose), "Bacak_Alt_" + s))

def add_shoes(P, J, col, kind="cizme", k=1.0):
    for s in ("Sol", "Sag"):
        sx = 1 if s == "Sol" else -1
        an = J["an_" + s]
        base = Vector((an.x + sx * 0.005, an.y - 0.035, 0.0))
        if kind == "cizme":
            P.append((paint(tube(base + Vector((0, 0.035, 0.0)), an + Vector((0, 0.0, 0.07)), 0.058 * k, 0.055 * k), col),
                      "Bacak_Alt_" + s))
            P.append((paint(ell(base + Vector((0, -0.03, 0.05)), (0.062 * k, 0.115 * k, 0.05)), col), "Ayak_" + s))
        elif kind == "pabuc":
            P.append((paint(ell(base + Vector((0, -0.02, 0.04)), (0.058 * k, 0.11 * k, 0.04)), col), "Ayak_" + s))
        elif kind == "kivrik":       # ucu kıvrık pabuç (Câbir)
            P.append((paint(ell(base + Vector((0, -0.02, 0.04)), (0.058, 0.105, 0.04)), col), "Ayak_" + s))
            tip0 = base + Vector((0, -0.11, 0.035))
            P.append((paint(tube(tip0, tip0 + Vector((0, -0.05, 0.05)), 0.03, 0.006, sides=6), col), "Ayak_" + s))

def add_belt(P, J, z, r, sy, col, h=0.06, bone="Kalca"):
    P.append((paint(lathe([(r, z - h / 2), (r * 1.03, z), (r, z + h / 2)], sides=14, sy=sy), col, noise=0.0), bone))

def flask(P, c, col_glass, col_cork, s=1.0, bone="Kalca"):
    prof = [(0.0, 0.0), (0.028 * s, 0.004), (0.034 * s, 0.03 * s), (0.026 * s, 0.06 * s), (0.012 * s, 0.075 * s),
            (0.012 * s, 0.095 * s)]
    bm = lathe(prof, sides=8, c=c)
    P.append((paint(bm, col_glass), bone))
    P.append((paint(bm_cyl(0.014 * s, 0.012 * s, 0.025 * s, sides=6), col_cork), bone))
    transform_bm(P[-1][0], Matrix.Translation(Vector(c) + Vector((0, 0, 0.09 * s))))

# ------------------------------------------------------------------ karakterler
def npc_cabir():
    """Câbir Usta: kare/sağlam şekil dili. Büyük sarık, uzun ak sakal, önlük, şişeler, kıvrık pabuç."""
    J = skeleton(1.76, 0.14, hip_k=0.47, sh_w=0.22, arm_out=0.05, stoop=0.03)
    P = []
    skin = (ten(0.35, 0.15), ten(0.75, 0.1))
    robe = blend2("bakir", "ahsap", 0.45, 0.2, 0.7)
    white = (tone("kaya", 0.7), tone("kaya", 1.0))
    sash = kumas("cati", 0.3, 0.8)
    # cüppe: alt (Kalca) + üst (Gogus), kuşak eklemi örter
    w = J["waist"].z
    P.append((paint(lathe([(0.30, 0.02), (0.30, 0.09), (0.265, 0.45), (0.235, 0.75), (0.215, w + 0.03)],
                          sides=16, sy=0.76), robe, seed=1), "Kalca"))
    P.append((paint(lathe([(0.215, w - 0.03), (0.235, J["chest"].z - 0.05), (0.235, J["sh_Sol"].z - 0.02),
                           (0.16, J["neck"].z + 0.01), (0.07, J["neck"].z + 0.04)],
                          sides=16, sy=0.74, c=(0, -0.012, 0)), robe, seed=2), "Gogus"))
    # omuz şalı (kalın yaka, #2)
    P.append((paint(torus(0.15, 0.05, c=(0, -0.015, J["neck"].z - 0.025), rot=(-8, 0, 0), sides=14, sy=0.8),
                    blend2("bakir", "ahsap", 0.7, 0.0, 0.4)), "Gogus"))
    add_belt(P, J, w, 0.222, 0.78, sash, h=0.08)
    # kuşak düğümü sarkan uçlar (sağda)
    P.append((paint(box((-0.08, -0.17, w - 0.12), (0.06, 0.025, 0.18), rot=(4, 0, -12), bevel=0.008), sash), "Kalca"))
    # önlük: is lekeli etek (alt koyu)
    ap_top, ap_bot = w + 0.02, 0.30
    ang = math.degrees(math.atan2(0.05, ap_top - ap_bot))
    P.append((paint(box((0, -0.205, (ap_top + ap_bot) / 2), (0.34, 0.02, ap_top - ap_bot), rot=(-ang, 0, 0), bevel=0.006),
                    (mix(tone("kil", 0.1), tone("ahsap", 0.0), 0.4), tone("kil", 0.9))), "Kalca"))
    # kemerde şişeler (opak pastel cam) — sağ yan
    for i, (x, col) in enumerate(((-0.17, kumas("gok", 0.4, 0.9)), (-0.2, kumas("yaprak", 0.5, 0.9)),
                                  (-0.135, kumas("altin", 0.4, 0.9)))):
        flask(P, (x, -0.13 + i * 0.03, w - 0.17 - i * 0.02), col, kumas("ahsap", 0.3, 0.6), s=0.9 + 0.1 * (i == 1))
    add_neck(P, J, skin)
    add_head(P, J, skin, white, nose="yumru", mouth=False, brow_k=1.5)
    hr, hc = J["hr"], J["head_c"]
    # uzun ak sakal: yanaktan başlayan geniş kütle + göğse inen sivri uç (tek siluet) + bıyık
    P.append((paint(ell(hc + Vector((0, -hr * 0.55, -hr * 0.62)), (hr * 0.8, hr * 0.52, hr * 0.58), u=10, v=7), white), "Bas"))
    P.append((paint(ell(hc + Vector((0, -hr * 0.72, -hr * 1.35)), (hr * 0.62, hr * 0.4, hr * 0.95), u=10, v=7), white), "Bas"))
    P.append((paint(tube(hc + Vector((0, -hr * 0.8, -hr * 1.9)), hc + Vector((0, -hr * 0.9, -hr * 2.55)),
                         hr * 0.36, 0.012, sides=8), white), "Bas"))
    for sx in (1, -1):
        P.append((paint(ell(hc + Vector((sx * 0.04, -hr * 1.0, -0.052)), (0.048, 0.024, 0.02), u=8, v=5), white), "Bas"))
    # sarık: kat kat sargı tek dönel yüzey (şişkin katlar) + çapraz sargı bandı + tepe kavuk
    tc = hc + Vector((0, 0.015, hr * 0.42))
    turb = kumas("siva", 0.55, 1.0)
    R = hr * 1.04
    prof = [(R * 0.98, 0.0), (R * 1.10, 0.035), (R * 1.02, 0.06), (R * 1.13, 0.10), (R * 1.03, 0.13),
            (R * 1.06, 0.165), (R * 0.88, 0.2), (R * 0.55, 0.235), (0.0, 0.245)]
    P.append((paint(lathe(prof, sides=16, sy=0.97, c=tc), turb, seed=3), "Bas"))
    band = torus(R * 1.07, 0.022, c=tc + Vector((0, 0, 0.1)), rot=(16, -10, 0), sides=16, ring=5)
    P.append((paint(band, kumas("siva", 0.35, 0.8)), "Bas"))
    P.append((paint(ell(tc + Vector((0, 0, 0.245)), (hr * 0.36, hr * 0.36, hr * 0.3), u=8, v=6), kumas("cati", 0.3, 0.7)), "Bas"))
    for s in ("Sol", "Sag"):
        add_arm(P, J, s, robe, skin, sleeve_r=(0.068, 0.064, 0.06), flare=0.035, cuff=kumas("kil", 0.4, 0.8))
    add_shoes(P, J, kumas("cati", 0.1, 0.45), kind="kivrik")
    return "Cabir", J, P

def npc_hesap():
    """Hesap Hocası: uzun üçgen şekil dili. Çok uzun, bükük sivri külah, iri gözlük, usturlap, pergel, tomar kutusu."""
    J = skeleton(1.86, 0.13, hip_k=0.47, sh_w=0.19, arm_out=0.035)
    P = []
    skin = (ten(0.45, 0.0), ten(0.85))
    robe = blend2("dag", "gok", 0.15, 0.1, 0.65)
    gray = (tone("kaya", 0.35), tone("kaya", 0.8))
    w = J["waist"].z
    P.append((paint(lathe([(0.23, 0.02), (0.225, 0.1), (0.19, 0.5), (0.17, 0.85), (0.16, w + 0.03)],
                          sides=14, sy=0.8), robe, seed=5), "Kalca"))
    P.append((paint(lathe([(0.16, w - 0.03), (0.18, J["chest"].z - 0.05), (0.185, J["sh_Sol"].z - 0.02),
                           (0.12, J["neck"].z), (0.055, J["neck"].z + 0.05)], sides=14, sy=0.8), robe, seed=6), "Gogus"))
    # önde dikey şerit (uzunluğu vurgular)
    P.append((paint(box((0, -0.168, 0.55), (0.06, 0.02, 1.0), rot=(-2.2, 0, 0), bevel=0.006), kumas("altin", 0.2, 0.6)),
              "Kalca"))
    add_belt(P, J, w, 0.165, 0.82, kumas("ahsap", 0.15, 0.5), h=0.05)
    # pergel: kemerden asılı (sol yan)
    pv = Vector((0.15, -0.09, w - 0.04))
    iron = (demir(0.8), demir(1.25))
    P.append((paint(torus(0.018, 0.006, c=pv, rot=(90, 0, 0), sides=8, ring=4), kumas("bakir", 0.4, 0.9)), "Kalca"))
    for sx in (1, -1):
        P.append((paint(tube(pv + Vector((0, 0, -0.015)), pv + Vector((sx * 0.05, -0.01, -0.24)), 0.008, 0.003, sides=5),
                        iron), "Kalca"))
    # usturlap (göğüste, tanıtıcı obje — #4, #12)
    ac = Vector((0, -0.185, J["chest"].z - 0.01))
    cop = kumas("bakir", 0.45, 1.0)
    P.append((paint(tube(ac + Vector((0, 0.01, 0)), ac + Vector((0, -0.012, 0)), 0.075, 0.075, sides=16), cop), "Gogus"))
    P.append((paint(torus(0.075, 0.009, c=ac + Vector((0, -0.012, 0)), rot=(90, 0, 0), sides=16, ring=4),
                    kumas("altin", 0.4, 0.9)), "Gogus"))
    P.append((paint(box(ac + Vector((0, -0.016, 0)), (0.13, 0.008, 0.012), rot=(0, 30, 0)), kumas("altin", 0.6, 1.0)), "Gogus"))
    # zincir: boyundan usturlaba iki çizgi
    for sx in (1, -1):
        P.append((paint(tube(ac + Vector((sx * 0.02, 0, 0.07)), Vector((sx * 0.07, -0.1, J["neck"].z - 0.02)),
                             0.006, 0.006, sides=4), kumas("altin", 0.4, 0.8)), "Gogus"))
    # sırtta tomar kutusu (çapraz)
    t0, t1 = Vector((-0.12, 0.17, J["waist"].z + 0.05)), Vector((0.12, 0.19, J["sh_Sol"].z + 0.2))
    P.append((paint(tube(t0, t1, 0.055, 0.055, sides=10), blend2("dag", "ahsap", 0.5, 0.0, 0.3)), "Gogus"))
    for k in (0.0, 0.35, 0.7, 1.0):
        a = t0.lerp(t1, k)
        q = Vector((0, 0, 1)).rotation_difference((t1 - t0).normalized())
        bm = torus(0.058, 0.01, sides=10, ring=4)
        transform_bm(bm, Matrix.Translation(a) @ q.to_matrix().to_4x4())
        P.append((paint(bm, kumas("bakir", 0.3, 0.8)), "Gogus"))
    P.append((paint(ell(t1, (0.05, 0.05, 0.03), u=8, v=5), kumas("bakir", 0.3, 0.8)), "Gogus"))
    # kutudan taşan parşömen ucu
    P.append((paint(tube(t1, t1 + (t1 - t0).normalized() * 0.07, 0.035, 0.035, sides=8), kumas("siva", 0.6, 1.0)), "Gogus"))
    add_neck(P, J, skin, r=0.045)
    add_head(P, J, skin, gray, nose="uzun", mouth=True, brow_k=1.2)
    hr, hc = J["hr"], J["head_c"]
    # sivri keçi sakalı
    P.append((paint(tube(hc + Vector((0, -hr * 0.72, -hr * 0.78)), hc + Vector((0, -hr * 0.95, -hr * 1.75)),
                         0.035, 0.006, sides=7), gray), "Bas"))
    # iri yuvarlak gözlük
    gl = kumas("bakir", 0.3, 0.8)
    for sx in (1, -1):
        P.append((paint(torus(0.04, 0.0065, c=hc + Vector((sx * hr * 0.36, -hr * 0.96, 0.02)), rot=(90, 0, 0),
                              sides=12, ring=4), gl), "Bas"))
    P.append((paint(tube(hc + Vector((hr * 0.36 - 0.04, -hr * 0.97, 0.025)), hc + Vector((-hr * 0.36 + 0.04, -hr * 0.97, 0.025)),
                         0.005, 0.005, sides=4), gl), "Bas"))
    # saç: yanlarda kır tutamlar
    for sx in (1, -1):
        P.append((paint(ell(hc + Vector((sx * hr * 0.82, 0.03, -0.01)), (0.04, 0.075, 0.06), u=7, v=5), gray), "Bas"))
    # külah: geniş kenar + bükük sivri gövde + bant
    hat = blend2("dag", "gok", 0.1, 0.05, 0.6)
    bz = hc + Vector((0, 0.01, hr * 0.62))
    P.append((paint(lathe([(0.0, -0.012), (0.22, -0.01), (0.23, 0.0), (0.2, 0.012), (0.0, 0.016)], sides=16,
                          c=bz), hat), "Bas"))
    P.append((paint(bent_cone(bz, hr * 1.02, 0.58, 0.16), hat, seed=7), "Bas"))
    P.append((paint(torus(hr * 0.98, 0.016, c=bz + Vector((0, 0.003, 0.04)), sides=14, ring=4), kumas("altin", 0.3, 0.8)), "Bas"))
    for s in ("Sol", "Sag"):
        add_arm(P, J, s, robe, skin, sleeve_r=(0.055, 0.05, 0.048), flare=0.012, cuff=kumas("altin", 0.2, 0.6),
                hand_k=0.95)
    add_shoes(P, J, kumas("ahsap", 0.05, 0.35), kind="pabuc", k=1.05)
    return "HesapHocasi", J, P

def npc_kopuk():
    """Köpük Efendi: yuvarlak şekil dili. Kısa, tombul, kabarık saç, alında gözlük, şişe kayışı, iri eldiven."""
    J = skeleton(1.50, 0.145, hip_k=0.40, sh_w=0.22, arm_out=0.07)
    P = []
    skin = (ten(0.4, 0.05), ten(0.85))
    robe = blend2("yaprak", "dag", 0.55, 0.05, 0.5)
    w = J["waist"].z
    # tombul gövde: göbek önde
    P.append((paint(lathe([(0.25, 0.22), (0.28, 0.35), (0.29, w - 0.02), (0.285, w + 0.04)], sides=16, sy=0.85),
                    robe, seed=8), "Kalca"))
    P.append((paint(lathe([(0.285, w - 0.03), (0.30, w + 0.08), (0.27, J["chest"].z), (0.22, J["sh_Sol"].z - 0.02),
                           (0.13, J["neck"].z + 0.01), (0.06, J["neck"].z + 0.04)], sides=16, sy=0.85,
                          c=(0, -0.02, 0)), robe, seed=9), "Gogus"))
    # deri önlük (göbeğe oturur)
    leather = (mix(tone("ahsap", 0.0), tone("cati", 0.0), 0.2), tone("ahsap", 0.55))
    # deri önlük: yalnız önde, göbeğin üstüne oturan kalın kalkan
    P.append((paint(ell((0, -0.13, w - 0.06), (0.24, 0.15, 0.30), u=12, v=8), leather, seed=10), "Kalca"))
    add_belt(P, J, w + 0.02, 0.30, 0.86, kumas("ahsap", 0.0, 0.3), h=0.05)
    # göğüste çapraz şişe kayışı + 4 tüp (gövde yüzeyinin dışında)
    a, b = Vector((0.15, -0.17, J["sh_Sol"].z - 0.03)), Vector((-0.2, -0.27, w + 0.06))
    P.append((paint(tube(a, b, 0.025, 0.025, sides=6), kumas("ahsap", 0.1, 0.4)), "Gogus"))
    vials = (kumas("gok", 0.4, 1.0), kumas("altin", 0.4, 1.0), kumas("cati", 0.5, 0.95), kumas("yaprak", 0.5, 1.0))
    for i, vc in enumerate(vials):
        p = a.lerp(b, 0.2 + 0.2 * i) + Vector((0, -0.035, 0))
        P.append((paint(tube(p + Vector((0, 0, -0.05)), p + Vector((0, 0, 0.05)), 0.018, 0.018, sides=6), vc), "Gogus"))
        P.append((paint(ell(p + Vector((0, 0, 0.055)), (0.016, 0.016, 0.012), u=6, v=4), kumas("ahsap", 0.3, 0.6)), "Gogus"))
    add_neck(P, J, skin, r=0.06)
    add_head(P, J, skin, (tone("kaya", 0.6), tone("kaya", 1.0)), nose="top", mouth=True, brow_k=1.7)
    hr, hc = J["hr"], J["head_c"]
    # kabarık, uçları isli saç (yuvarlak bulut)
    hair = (mix(tone("kaya", 0.4), tone("ahsap", 0.1), 0.45), tone("kaya", 1.0))
    rnd = random.Random(11)
    for i in range(9):
        ang = 2 * math.pi * i / 9 + 0.3
        rr = hr * (0.85 + rnd.uniform(-0.08, 0.1))
        c = hc + Vector((math.cos(ang) * rr, 0.035 + math.sin(ang) * rr * 0.8, hr * (0.55 + rnd.uniform(0, 0.25))))
        s = hr * (0.42 + rnd.uniform(0, 0.12))
        if math.sin(ang) < -0.6:          # alın önü açık kalsın
            continue
        P.append((paint(ell(c, (s, s, s * 0.9), u=8, v=6), hair, seed=12 + i), "Bas"))
    P.append((paint(ell(hc + Vector((0, 0.03, hr * 0.85)), (hr * 0.75, hr * 0.7, hr * 0.42), u=10, v=6), hair), "Bas"))
    # bıyık (kıvrık)
    for sx in (1, -1):
        P.append((paint(ell(hc + Vector((sx * 0.03, -hr * 1.0, -0.05)), (0.035, 0.02, 0.016), u=6, v=5), hair), "Bas"))
    # alında gözlük-maske: iki kalın halka + cam + kayış
    gz = hc + Vector((0, 0, hr * 0.55))
    for sx in (1, -1):
        cc = gz + Vector((sx * 0.045, -hr * 0.86, 0))
        P.append((paint(tube(cc + Vector((0, 0.03, 0)), cc + Vector((0, -0.02, 0)), 0.04, 0.036, sides=10),
                        kumas("bakir", 0.3, 0.9)), "Bas"))
        P.append((paint(tube(cc + Vector((0, -0.018, 0)), cc + Vector((0, -0.024, 0)), 0.03, 0.03, sides=10),
                        kumas("su", 0.5, 1.0)), "Bas"))
    P.append((paint(torus(hr * 0.98, 0.014, c=gz + Vector((0, 0.0, -0.005)), sides=14, ring=4, sy=0.98),
                    kumas("ahsap", 0.0, 0.3)), "Bas"))
    gloves = (mix(tone("ahsap", 0.1), tone("cati", 0.1), 0.25), tone("ahsap", 0.6))
    for s in ("Sol", "Sag"):
        add_arm(P, J, s, robe, gloves, sleeve_r=(0.07, 0.065, 0.06), cuff=gloves, hand_k=1.25)
    add_legs(P, J, kumas("ahsap", 0.1, 0.45), upper_r=0.09, lower_r=0.065)
    add_shoes(P, J, kumas("ahsap", 0.0, 0.3), kind="cizme", k=1.1)
    return "KopukEfendi", J, P

def npc_koylu_erkek():
    J = skeleton(1.72, 0.135, hip_k=0.45, sh_w=0.21, arm_out=0.05)
    P = []
    skin = (ten(0.3, 0.25), ten(0.7, 0.15))
    tunic = blend2("yaprak", "ahsap", 0.55, 0.15, 0.6)
    w = J["waist"].z
    P.append((paint(lathe([(0.0, 0.40), (0.235, 0.40), (0.24, 0.44), (0.215, J["hip"].z), (0.195, w + 0.03)],
                          sides=14, sy=0.74), tunic, seed=13), "Kalca"))
    P.append((paint(lathe([(0.195, w - 0.03), (0.215, J["chest"].z - 0.05), (0.215, J["sh_Sol"].z - 0.02),
                           (0.14, J["neck"].z), (0.065, J["neck"].z + 0.04)], sides=14, sy=0.72), tunic, seed=14), "Gogus"))
    add_belt(P, J, w, 0.2, 0.75, kumas("ahsap", 0.0, 0.3), h=0.05)
    # kese (sol kalça)
    P.append((paint(ell((0.17, -0.07, w - 0.1), (0.045, 0.035, 0.06), u=8, v=6), kumas("kil", 0.2, 0.7)), "Kalca"))
    add_neck(P, J, skin)
    add_head(P, J, skin, kumas("ahsap", 0.0, 0.3), nose="yumru", mouth=False)
    hr, hc = J["hr"], J["head_c"]
    beard = kumas("ahsap", 0.1, 0.5)
    P.append((paint(ell(hc + Vector((0, -hr * 0.52, -hr * 0.6)), (hr * 0.75, hr * 0.5, hr * 0.48)), beard), "Bas"))
    P.append((paint(ell(hc + Vector((0, 0.03, hr * 0.2)), (hr * 0.98, hr * 0.98, hr * 0.9), u=12, v=8), beard), "Bas"))
    # hasır şapka (geniş kenar + tepe)
    straw = (mix(tone("altin", 0.0), tone("ahsap", 0.9), 0.4), tone("altin", 0.95))
    bz = hc + Vector((0, 0.01, hr * 0.55))
    P.append((paint(lathe([(0.0, -0.01), (0.30, -0.035), (0.31, -0.025), (0.17, 0.01), (0.0, 0.02)], sides=18, c=bz),
                    straw, seed=15), "Bas"))
    P.append((paint(lathe([(0.15, 0.0), (0.14, 0.09), (0.09, 0.13), (0.0, 0.14)], sides=14, c=bz), straw, seed=16), "Bas"))
    P.append((paint(torus(0.148, 0.012, c=bz + Vector((0, 0, 0.025)), sides=14, ring=4), kumas("cati", 0.1, 0.4)), "Bas"))
    for s in ("Sol", "Sag"):
        add_arm(P, J, s, tunic, skin, sleeve_r=(0.06, 0.055, 0.05))
    add_legs(P, J, kumas("ahsap", 0.05, 0.4))
    add_shoes(P, J, kumas("ahsap", 0.0, 0.25), kind="cizme")
    return "KoyluErkek", J, P

def npc_koylu_kadin():
    J = skeleton(1.64, 0.13, hip_k=0.47, sh_w=0.185, arm_out=0.045)
    P = []
    skin = (ten(0.4, 0.1), ten(0.85))
    dress = blend2("gok", "dag", 0.6, 0.05, 0.55)
    w = J["waist"].z
    P.append((paint(lathe([(0.27, 0.02), (0.27, 0.08), (0.24, 0.35), (0.2, J["hip"].z), (0.17, w + 0.03)],
                          sides=16, sy=0.76), dress, seed=17), "Kalca"))
    P.append((paint(lathe([(0.17, w - 0.03), (0.195, J["chest"].z - 0.04), (0.19, J["sh_Sol"].z - 0.02),
                           (0.12, J["neck"].z), (0.055, J["neck"].z + 0.04)], sides=14, sy=0.75), dress, seed=18), "Gogus"))
    # önlük
    ap_top, ap_bot = w - 0.01, 0.16
    ang = math.degrees(math.atan2(0.07, ap_top - ap_bot))
    P.append((paint(box((0, -0.17, (ap_top + ap_bot) / 2), (0.3, 0.018, ap_top - ap_bot), rot=(-ang, 0, 0), bevel=0.006),
                    kumas("siva", 0.45, 1.0)), "Kalca"))
    add_belt(P, J, w, 0.175, 0.78, kumas("cati", 0.15, 0.45), h=0.045)
    add_neck(P, J, skin, r=0.045)
    add_head(P, J, skin, kumas("ahsap", 0.05, 0.3), nose="yumru", mouth=True)
    hr, hc = J["hr"], J["head_c"]
    # başörtüsü: başı saran örtü + ensede düğüm + sarkan uç
    # keten örtü (açık) + alnı çevreleyen kalın kıvrım (koyu) → saç değil örtü olduğu okunur
    cloth = kumas("siva", 0.5, 1.0)
    P.append((paint(ell(hc + Vector((0, 0.025, hr * 0.2)), (hr * 1.07, hr * 1.06, hr * 1.0), u=12, v=8), cloth, seed=19), "Bas"))
    P.append((paint(torus(hr * 1.0, 0.026, c=hc + Vector((0, -0.005, hr * 0.42)), rot=(-22, 0, 0), sides=16, ring=5),
                    blend2("cati", "kil", 0.5, 0.2, 0.6)), "Bas"))
    P.append((paint(ell(hc + Vector((0, hr * 0.98, -hr * 0.2)), (0.05, 0.04, 0.045), u=8, v=6), cloth), "Bas"))
    P.append((paint(box(hc + Vector((0, hr * 1.0, -hr * 0.7)), (0.09, 0.02, 0.14), rot=(10, 0, 0), bevel=0.008), cloth), "Bas"))
    for s in ("Sol", "Sag"):
        add_arm(P, J, s, dress, skin, sleeve_r=(0.055, 0.05, 0.042), fore_skin=True, hand_k=0.9)
        # sıvanmış kol kıvrımı (dirsek üstü)
        P.append((paint(torus(0.056, 0.016, c=J["el_" + s] + Vector((0, 0, 0.02)), sides=10, ring=4), dress), "Kol_Ust_" + s))
    add_shoes(P, J, kumas("ahsap", 0.0, 0.3), kind="pabuc", k=0.95)
    return "KoyluKadin", J, P

def npc_ulak():
    """Ulak: köyden atölyeye haber getiren genç. Çevik, ince; kısa omuz pelerini (kapüşon sırtta),
    çapraz heybe + taşan mektup tomarı, tüylü yumuşak başlık. Turuncu vurgu: oyuncuyu çağıran kişi (09 §3)."""
    J = skeleton(1.62, 0.13, hip_k=0.47, sh_w=0.19, arm_out=0.045)
    P = []
    skin = (ten(0.45, 0.0), ten(0.9))
    tunic = kumas("siva", 0.3, 0.85)
    cape = blend2("cati", "kil", 0.35, 0.15, 0.6)
    w = J["waist"].z
    # kısa tunik (uyluk ortası) + kemer
    P.append((paint(lathe([(0.0, 0.52), (0.2, 0.52), (0.205, 0.56), (0.185, J["hip"].z), (0.17, w + 0.03)],
                          sides=14, sy=0.74), tunic, seed=20), "Kalca"))
    P.append((paint(lathe([(0.17, w - 0.03), (0.19, J["chest"].z - 0.05), (0.19, J["sh_Sol"].z - 0.02),
                           (0.12, J["neck"].z), (0.055, J["neck"].z + 0.04)], sides=14, sy=0.72), tunic, seed=21), "Gogus"))
    add_belt(P, J, w, 0.175, 0.76, kumas("ahsap", 0.05, 0.35), h=0.045)
    # omuz pelerini: omuzlara oturan kısa çan + sırtta toplanmış kapüşon + önde bakır toka
    cz = J["neck"].z
    # pelerin tuniğin ve omuz toplarının dışında kalır (kesişim tırtığı olmasın)
    P.append((paint(lathe([(0.30, cz - 0.30), (0.305, cz - 0.27), (0.30, cz - 0.16), (0.24, cz - 0.06),
                           (0.15, cz + 0.0), (0.085, cz + 0.045)], sides=16, sy=0.8, c=(0, 0.01, 0)), cape, seed=22), "Gogus"))
    P.append((paint(ell((0, 0.15, cz + 0.0), (0.13, 0.085, 0.1), u=10, v=7), cape, seed=23), "Gogus"))
    P.append((paint(tube((0, -0.11, cz - 0.01), (0, -0.135, cz - 0.01), 0.025, 0.025, sides=8), kumas("bakir", 0.4, 0.9)), "Gogus"))
    # çapraz heybe kayışı (pelerinin altından çıkar → sağ kalça) + heybe + tomar
    a, b = Vector((0.12, -0.155, cz - 0.28)), Vector((-0.17, -0.1, w - 0.04))
    P.append((paint(tube(a, b, 0.018, 0.018, sides=6), kumas("ahsap", 0.1, 0.4)), "Gogus"))
    bag = Vector((-0.2, -0.02, w - 0.12))
    P.append((paint(box(bag, (0.08, 0.22, 0.17), rot=(0, 0, 8), bevel=0.025), kumas("ahsap", 0.15, 0.55)), "Kalca"))
    P.append((paint(box(bag + Vector((-0.005, 0, 0.06)), (0.09, 0.23, 0.07), rot=(0, 0, 8), bevel=0.02),
                    kumas("ahsap", 0.3, 0.7)), "Kalca"))         # kapak
    P.append((paint(tube(bag + Vector((0, 0.03, 0.02)), bag + Vector((-0.02, 0.06, 0.2)), 0.03, 0.03, sides=8),
                    kumas("siva", 0.6, 1.0)), "Kalca"))           # mektup tomarı
    P.append((paint(torus(0.031, 0.007, c=bag + Vector((-0.012, 0.047, 0.12)), rot=(-8, -6, 0), sides=8, ring=4),
                    kumas("cati", 0.2, 0.5)), "Kalca"))           # tomar bağı
    add_neck(P, J, skin, r=0.045)
    add_head(P, J, skin, kumas("ahsap", 0.1, 0.4), nose="yumru", mouth=True, brow_k=0.9)
    hr, hc = J["hr"], J["head_c"]
    # kısa saç: başın arka yarısı (ensede ve kulak üstünde görünür)
    hair = kumas("ahsap", 0.15, 0.55)
    P.append((paint(ell(hc + Vector((0, 0.04, hr * 0.12)), (hr * 1.0, hr * 0.98, hr * 0.95), u=12, v=8), hair, seed=24), "Bas"))
    # yumuşak başlık (yana yatık) + tüy
    cap_c = hc + Vector((0.015, 0.01, hr * 0.62))
    capm = lathe([(0.0, -0.005), (hr * 1.02, 0.0), (hr * 1.12, 0.03), (hr * 1.0, 0.07), (hr * 0.5, 0.095), (0.0, 0.1)], sides=14)
    transform_bm(capm, Matrix.Translation(cap_c) @ Euler((math.radians(-6), math.radians(-12), 0)).to_matrix().to_4x4())
    P.append((paint(capm, blend2("yaprak", "dag", 0.5, 0.1, 0.5), seed=25), "Bas"))
    f0 = cap_c + Vector((0.11, 0.02, 0.05))
    fe = tube(f0, f0 + Vector((0.05, 0.1, 0.17)), 0.018, 0.004, sides=6)
    transform_bm(fe, Matrix.Translation(f0) @ Matrix.Diagonal((0.5, 1.0, 1.0, 1.0)) @ Matrix.Translation(-f0))  # yassı tüy
    P.append((paint(fe, kumas("altin", 0.4, 1.0)), "Bas"))
    for s in ("Sol", "Sag"):
        add_arm(P, J, s, tunic, skin, sleeve_r=(0.052, 0.047, 0.042), cuff=kumas("ahsap", 0.1, 0.4), hand_k=0.92)
    add_legs(P, J, kumas("ahsap", 0.05, 0.4), upper_r=0.068, lower_r=0.052)
    add_shoes(P, J, kumas("ahsap", 0.0, 0.3), kind="cizme", k=0.95)
    return "Ulak", J, P

CHARACTERS = (npc_cabir, npc_hesap, npc_kopuk, npc_koylu_erkek, npc_koylu_kadin, npc_ulak)

# ------------------------------------------------------------------ mesh + rig
def merge_weighted(parts):
    out = bmesh.new()
    lay_out = out.loops.layers.float_color.new("Col")
    dl = out.verts.layers.deform.verify()
    for bm, bone in parts:
        bi = BONES.index(bone)
        lay_in = bm.loops.layers.float_color.get("Col")
        vmap = {}
        for v in bm.verts:
            nv = out.verts.new(v.co)
            nv[dl][bi] = 1.0
            vmap[v] = nv
        for f in bm.faces:
            try:
                nf = out.faces.new([vmap[v] for v in f.verts])
            except ValueError:
                continue
            if lay_in:
                for ln, lo in zip(nf.loops, f.loops):
                    ln[lay_out] = lo[lay_in]
        bm.free()
    return out

def make_armature(name, J, col, x):
    arm = bpy.data.armatures.new(name + "_Rig")
    ob = bpy.data.objects.new(name, arm)
    col.objects.link(ob)
    ob.location = (x, 0, 0)
    ob.show_in_front = True
    bpy.context.view_layer.objects.active = ob
    for o in bpy.context.selected_objects:
        o.select_set(False)
    ob.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    pairs = bone_pairs(J)
    for bn in BONES:
        h, t = pairs[bn]
        eb = arm.edit_bones.new(bn)
        eb.head, eb.tail = h, t
        eb.roll = 0.0
    for bn, pn in BONE_PARENT.items():
        arm.edit_bones[bn].parent = arm.edit_bones[pn]
        arm.edit_bones[bn].use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    for pb in ob.pose.bones:
        pb.rotation_mode = "XYZ"
    return ob

def build_npc(fn, col, x=0.0):
    name, J, parts = fn()
    oname = "NPC_" + name
    rig = make_armature(oname, J, col, x)
    bm = merge_weighted(parts)
    me_ob = to_object(bm, oname + "_Govde", col, sharp_angle=None)
    for bn in BONES:
        me_ob.vertex_groups.new(name=bn)
    me_ob.parent = rig
    mod = me_ob.modifiers.new("Armature", "ARMATURE")
    mod.object = rig
    build_anims(rig, name)
    print(oname, "üçgen:", tri_count([me_ob]))
    return rig, me_ob

# ------------------------------------------------------------------ animasyon
def _key(rig, act, frames, locs=None):
    """frames: {frame: {bone: (rx, ry, rz) derece}}; locs: {frame: {bone: (x, y, z) m, kemik yerel}} (yalnız Kalca)."""
    rig.animation_data_create()
    rig.animation_data.action = act
    for f, poses in sorted(frames.items()):
        for pb in rig.pose.bones:
            r = poses.get(pb.name, (0, 0, 0))
            pb.rotation_euler = [math.radians(a) for a in r]
            pb.keyframe_insert("rotation_euler", frame=f)
        if locs is not None:
            pb = rig.pose.bones["Kalca"]
            pb.location = locs.get(f, {}).get("Kalca", (0, 0, 0))
            pb.keyframe_insert("location", frame=f)
    for pb in rig.pose.bones:
        pb.location = (0, 0, 0)

# yürüyüş: 1 sn'de 2 adım (30 kare), ~1,1 m/s'de adım boyu ~0,55 m. Kod oynatma hızını gerçek hıza oranlar.
WALK_FRAMES = FPS
WALK_REF_SPEED = 1.1

def _walk_pose(phase, k):
    """phase 0..1 (0 = sol ayak önde basar). k: kişiye göre genlik (tombul/kısa → küçük adım)."""
    s = math.sin(2 * math.pi * phase)            # +1: sol bacak önde
    lift_l = max(0.0, math.sin(2 * math.pi * phase - math.pi / 2))   # sol bacak geri→ileri savrulurken diz bükülür
    lift_r = max(0.0, math.sin(2 * math.pi * phase + math.pi / 2))
    return {
        "Bacak_Ust_Sol": (-24 * k * s, 0, 0), "Bacak_Ust_Sag": (24 * k * s, 0, 0),
        "Bacak_Alt_Sol": (32 * k * lift_l, 0, 0), "Bacak_Alt_Sag": (32 * k * lift_r, 0, 0),
        "Ayak_Sol": (-8 * k * lift_l, 0, 0), "Ayak_Sag": (-8 * k * lift_r, 0, 0),
        "Kol_Ust_Sol": (16 * k * s, 0, 0), "Kol_Ust_Sag": (-16 * k * s, 0, 0),
        "Kol_Alt_Sol": (-10 * k - 6 * k * max(0.0, -s), 0, 0), "Kol_Alt_Sag": (-10 * k - 6 * k * max(0.0, s), 0, 0),
        "Kalca": (0, 4 * k * s, 0), "Gogus": (-1.5, -5 * k * s, 0), "Bas": (1.0, 1.5 * k * s, 0),
    }

def build_walk(rig, name):
    k = {"HesapHocasi": 0.8, "KopukEfendi": 0.75, "Cabir": 0.85}.get(name, 1.0)
    act = bpy.data.actions.new(f"{name}_Yuru_Dongu")
    frames, locs = {}, {}
    for i in range(9):                            # 8 eşit aralık + kapanış (ilk = son)
        f = 1 + i * WALK_FRAMES // 8
        ph = i / 8
        frames[f] = _walk_pose(ph, k)
        # adımda kalça iki kez iner-kalkar (basış anında en alçak)
        locs[f] = {"Kalca": (0, -0.018 * k * abs(math.sin(2 * math.pi * ph)), 0)}   # bacaklar açıkken alçak
    _key(rig, act, frames, locs)
    return act

def build_work(rig, name):
    """Calis_Dongu: önündeki işe eğilmiş, iki el çalışır (yazar, tartar, karıştırır). Kişiye göre tempo."""
    W = 2 * FPS
    act = bpy.data.actions.new(f"{name}_Calis_Dongu")
    base = {"Gogus": (7, 0, 0), "Omurga": (3, 0, 0), "Bas": (12, 0, 0),
            "Kol_Ust_Sag": (-42, 0, 12), "Kol_Alt_Sag": (-48, 0, 0), "El_Sag": (-8, 0, 0),
            "Kol_Ust_Sol": (-26, 0, -8), "Kol_Alt_Sol": (-40, 0, 0)}

    def off(d):
        out = dict(base)
        for b, v in d.items():
            o = out.get(b, (0, 0, 0))
            out[b] = (o[0] + v[0], o[1] + v[1], o[2] + v[2])
        return out
    _key(rig, act, {
        1:          off({}),
        W // 4:     off({"Kol_Ust_Sag": (-8, 0, -6), "Kol_Alt_Sag": (6, 0, 0), "Bas": (2, 4, 0)}),
        W // 2:     off({"Kol_Ust_Sag": (2, 0, -12), "Kol_Alt_Sag": (-6, 0, 0), "Kol_Alt_Sol": (-6, 0, 0), "Gogus": (1, 3, 0)}),
        3 * W // 4: off({"Kol_Ust_Sag": (6, 0, -4), "Kol_Alt_Sag": (4, 0, 0), "Bas": (-1, -3, 0)}),
        W + 1:      off({}),
    })
    return act

def build_anims(rig, name):
    """Bekle_Dongu: nefes + bakınma. Konus_Dongu: sağ el anlatır, baş sallar. İkisi de döngülü (ilk = son kare)."""
    L = 4 * FPS
    idle = bpy.data.actions.new(f"{name}_Bekle_Dongu")
    sway = {"Cabir": 1.0, "HesapHocasi": 0.7, "KopukEfendi": 1.6, "KoyluErkek": 1.0, "KoyluKadin": 0.9,
            "Ulak": 1.4}.get(name, 1.0)
    _key(rig, idle, {
        1:          {"Gogus": (0, 0, 0), "Bas": (0, 0, 0), "Kol_Ust_Sol": (0, 0, 0), "Kol_Ust_Sag": (0, 0, 0)},
        L // 4:     {"Gogus": (2.0, 0, 0), "Omurga": (0.8, 0, 0), "Bas": (-1.5, 9 * sway, 0),
                     "Kol_Ust_Sol": (1.5, 0, 0), "Kol_Ust_Sag": (-1.5, 0, 0)},
        L // 2:     {"Gogus": (0, 0, 0), "Bas": (0, 2 * sway, 0)},
        3 * L // 4: {"Gogus": (2.0, 0, 0), "Omurga": (0.8, 0, 0), "Bas": (2.0, -7 * sway, 0),
                     "Kol_Ust_Sol": (-1.5, 0, 0), "Kol_Ust_Sag": (1.5, 0, 0)},
        L + 1:      {"Gogus": (0, 0, 0), "Bas": (0, 0, 0), "Kol_Ust_Sol": (0, 0, 0), "Kol_Ust_Sag": (0, 0, 0)},
    })
    talk = bpy.data.actions.new(f"{name}_Konus_Dongu")
    T = 3 * FPS
    up = {"Kol_Ust_Sag": (-20, 0, 8), "Kol_Alt_Sag": (-55, 0, 0), "El_Sag": (-10, 0, 0)}
    _key(rig, talk, {
        1:          {},
        T // 6:     {**up, "Bas": (-6, 0, 0), "Gogus": (-2, 0, 0)},
        T // 3:     {**{k: (v[0] - 12, v[1], v[2]) for k, v in up.items()}, "Bas": (4, 3, 0)},
        T // 2:     {**up, "Bas": (-5, -2, 0), "Kol_Alt_Sol": (-12, 0, 0)},
        2 * T // 3: {**{k: (v[0] + 8, v[1], v[2] - 4) for k, v in up.items()}, "Bas": (3, 0, 0)},
        5 * T // 6: {"Kol_Ust_Sag": (-6, 0, 3), "Kol_Alt_Sag": (-18, 0, 0), "Bas": (-2, 0, 0)},
        T + 1:      {},
    })
    walk = build_walk(rig, name)
    work = build_work(rig, name)
    for act in (idle, talk, walk, work):
        act.use_fake_user = True
        try:
            for fc in act.fcurves:
                fc.modifiers.new("CYCLES")
        except AttributeError:      # Blender 5: katmanlı action (layers/strips/channelbags)
            for layer in act.layers:
                for strip in layer.strips:
                    for cb in strip.channelbags:
                        for fc in cb.fcurves:
                            fc.modifiers.new("CYCLES")
    rig.animation_data.action = idle
    tracks = rig.animation_data.nla_tracks
    for act in (idle, talk, walk, work):
        tr = tracks.new()
        tr.name = act.name
        tr.strips.new(act.name, 1, act)
        tr.mute = True

# ------------------------------------------------------------------ sahne
def preview_scene():
    col = collection("_Onizleme")
    clear_collection("_Onizleme")
    g = bm_cyl(5.5, 5.5, 0.02, sides=32)
    transform_bm(g, Matrix.Translation((0, 0, -0.02)))
    paint_gradient(g, pal("cimen", 2), pal("cimen", 1))
    to_object(g, "Onizleme_Zemin", col)
    sun = bpy.data.lights.get("Onizleme_Gunes") or bpy.data.lights.new("Onizleme_Gunes", "SUN")
    sun.energy = 3.5
    sun.color = (1.0, 0.96, 0.88)
    so = bpy.data.objects.get("Onizleme_Gunes") or bpy.data.objects.new("Onizleme_Gunes", sun)
    if so.name not in col.objects:
        col.objects.link(so)
    so.rotation_euler = (math.radians(50), 0, math.radians(-30))
    w = bpy.context.scene.world or bpy.data.worlds.new("World")
    bpy.context.scene.world = w
    w.use_nodes = True
    bg = next(n for n in w.node_tree.nodes if n.type == "BACKGROUND")
    bg.inputs[0].default_value = pal("gok", 0)
    bg.inputs[1].default_value = 0.9

def build_all_npc(spacing=1.0):
    act_ob = bpy.context.view_layer.objects.active
    if act_ob and act_ob.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    col = collection(COL)
    clear_collection(COL)
    for a in [a for a in bpy.data.actions if a.name.endswith(("_Bekle_Dongu", "_Konus_Dongu", "_Yuru_Dongu", "_Calis_Dongu"))]:
        bpy.data.actions.remove(a)
    for arm in [a for a in bpy.data.armatures if a.users == 0]:
        bpy.data.armatures.remove(arm)
    bpy.context.scene.render.fps = FPS
    bpy.context.scene.frame_start, bpy.context.scene.frame_end = 1, 4 * FPS
    out = []
    n = len(CHARACTERS)
    for i, fn in enumerate(CHARACTERS):
        out.append(build_npc(fn, col, x=(i - (n - 1) / 2) * spacing))
    preview_scene()
    return out

def export_all_npc(directory=EXPORT_DIR):
    os.makedirs(directory, exist_ok=True)
    paths = []
    for rig in [o for o in bpy.data.collections[COL].objects if o.type == "ARMATURE"]:
        for o in list(bpy.context.selected_objects):
            o.select_set(False)
        old = rig.location.copy()
        rig.location = (0, 0, 0)
        rig.animation_data.action = None
        for tr in rig.animation_data.nla_tracks:
            tr.mute = False
        rig.select_set(True)
        for ch in rig.children:
            ch.select_set(True)
        bpy.context.view_layer.objects.active = rig
        p = os.path.join(directory, rig.name + ".fbx")
        bpy.ops.export_scene.fbx(
            filepath=p, use_selection=True, object_types={"MESH", "ARMATURE"},
            apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
            mesh_smooth_type="OFF", use_mesh_modifiers=False, colors_type="LINEAR", add_leaf_bones=False,
            primary_bone_axis="Y", secondary_bone_axis="X", armature_nodetype="NULL",
            bake_anim=True, bake_anim_use_nla_strips=True, bake_anim_use_all_actions=False,
            bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0)
        for tr in rig.animation_data.nla_tracks:
            tr.mute = True
        rig.animation_data.action = bpy.data.actions.get(rig.name[4:] + "_Bekle_Dongu")
        rig.location = old
        paths.append(p)
    return paths
