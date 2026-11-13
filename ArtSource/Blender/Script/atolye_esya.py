# Câbir Usta'nın Atölyesi — eşyalar (ocak, kazan, teraziler, raflar, kavanozlar, masa, athanor, imbik, aletler)
# Referans: ArtSource/Referans/atolye.md §F · Kurallar: 09-tema-ve-model-kurallari.md
# Önce ba_lib.py ve atolye_kabuk.py çalıştırılmış olmalı (IX, IY, H, roof_z ... buradan okunur).
#
# Her eşya ayrı obje (prefab adayı). Pivot: taban ortası (taşınabilir) ya da hareket ekseni.
# Hareketli parçalar tek mesh; hiyerarşi en fazla bir kademe (üst boş obje + çocuklar).

import math
import random
from mathutils import Matrix, Vector

RP = random.Random(2611)
ROOT_COL = "Atolye_Esya"

# yerleşim (dünya, iç zemin z=0)
HEARTH_X, HEARTH_W, HEARTH_D, HEARTH_H = 1.40, 1.50, 1.00, 0.55
HEARTH_Y = IY - HEARTH_D / 2                 # arka duvara yaslı
KZ = 1.20                                    # kazan ölçeği (kahraman nesne, okunur)
SCALE_POS = {"Giren": (0.10, 1.95), "Cikan": (2.70, 1.95)}

def _col():
    return collection(ROOT_COL)

def _obj(bm, name, loc=(0, 0, 0), rot=(0, 0, 0), mat=None, sharp=40):
    return to_object(bm, name, _col(), loc=loc, rot=rot, sharp_angle=sharp, mat=mat)

def _group(name, loc=(0, 0, 0), rot=(0, 0, 0)):
    ob = bpy.data.objects.get(name)
    if ob is None:
        ob = empty(name, _col(), loc=loc, rot=rot, kind="PLAIN_AXES", size=0.25)
    return ob

def _stone_block(w, d, h, seed, warm=0.5, bevel=0.03):
    bm = bm_box(w, d, h, bevel=bevel)
    jitter(bm, 0.01, seed=seed)
    r = random.Random(seed)
    c = mix(pal("kaya", 1), pal("siva", 0), warm + r.uniform(-0.15, 0.15))
    c = mix(c, pal("kil", 1), 0.18 + r.uniform(0.0, 0.12))
    c = mix(c, pal("kaya", 2), r.uniform(0.0, 0.2))
    paint_by(bm, lambda f, l, c=c: c if f.normal.z > -0.5 else mix(c, pal("kaya", 2), 0.5))
    return bm

def _wood(bm, seed, lo=1, hi=0, bias=0.2):
    return paint_part(bm, "ahsap", lo=lo, hi=hi, noise=0.07, seed=seed, bias=bias)

# ================================================================== OCAK + DAVLUMBAZ
def build_hearth():
    parts = []
    w, d, h = HEARTH_W, HEARTH_D, HEARTH_H
    # taban sekisi (iri taşlar)
    x = -w / 2
    i = 0
    while x < w / 2 - 0.05:
        bw = min(RP.uniform(0.42, 0.62), w / 2 - x)
        bm = _stone_block(bw - 0.015, d + 0.10, 0.12, 3000 + i)
        parts.append((bm, Matrix.Translation((x + bw / 2, 0.05, 0.0))))
        x += bw
        i += 1
    # ateş haznesinin yan ve arka duvarları: taş sıraları
    for side in (-1, 1):
        z = 0.12
        k = 0
        while z < h - 0.13:
            hh = min(RP.uniform(0.13, 0.17), h - 0.13 - z)
            for j, (dy, dl) in enumerate(((-d / 2 + 0.2, 0.42), (0.18, 0.5))):
                bm = _stone_block(0.30, dl - 0.015, hh - 0.012, 3100 + side * 50 + k * 7 + j, warm=0.4, bevel=0.02)
                parts.append((bm, Matrix.Translation((side * (w / 2 - 0.15), dy + (0.03 if k % 2 else -0.03), z))))
            z += hh
            k += 1
    bm = _stone_block(w - 0.6, 0.25, h - 0.25, 3200, warm=0.2)
    parts.append((bm, Matrix.Translation((0, d / 2 - 0.13, 0.12))))
    # üst tabla: kazan için yuvarlak delikli kalın taş
    ring = []
    for a in range(16):
        ang = 2 * math.pi * a / 16
        ring.append((math.cos(ang) * 0.36 * KZ, math.sin(ang) * 0.36 * KZ))
    top = bm_wall([(-w / 2 - 0.04, -d / 2 - 0.04), (w / 2 + 0.04, -d / 2 - 0.04), (w / 2 + 0.04, d / 2), (-w / 2 - 0.04, d / 2)],
                  [list(reversed(ring))], 0.13)
    # bm_wall (u, y→kalınlık, z) üretir: z'yi yatay y'ye çevir
    transform_bm(top, Matrix.Translation((0, 0, 0.13)) @ Matrix.Rotation(-math.pi / 2, 4, "X"))
    bevel_all(top, 0.025)
    jitter(top, 0.006, seed=3300)
    paint_by(top, lambda f, l: mix(mix(pal("kaya", 1), pal("siva", 1), 0.4), pal("kaya", 2),
                                   0.55 if (Vector((l.vert.co.x, l.vert.co.y, 0)).length < 0.45) else 0.1))
    parts.append((top, Matrix.Translation((0, 0, h))))
    # ön yüzde kemerli ateş ağzı çerçevesi (iki söve + lento)
    for side in (-1, 1):
        bm = _stone_block(0.16, 0.18, h - 0.14, 3400 + side, warm=0.5, bevel=0.025)
        parts.append((bm, Matrix.Translation((side * 0.38, -d / 2 + 0.02, 0.12))))
    lint = bm_prism(pointed_arch(0.0, 0.62, 0.0, 0.0, 0.12, n=6)[1:-1] + [(0.40, 0.20), (-0.40, 0.20)], -0.09, 0.09)
    bevel_all(lint, 0.015)
    paint_by(lint, lambda f, l: mix(pal("kaya", 1), pal("siva", 0), 0.35))
    parts.append((lint, Matrix.Translation((0, -d / 2 + 0.02, h - 0.22))))
    out = bm_merge(*parts)
    bmesh.ops.recalc_face_normals(out, faces=out.faces)
    ob = _obj(out, "SM_Ocak", loc=(HEARTH_X, HEARTH_Y, 0))
    # davlumbaz: sıvalı, tabanı is lekeli, önde ahşap kirişli
    secs = []
    for z, ww, dd in ((1.98, 1.72, 0.98), (2.18, 1.64, 0.92), (2.75, 1.22, 0.70), (3.30, 0.92, 0.52), (4.05, 0.86, 0.48)):
        y0 = IY
        secs.append([Vector((-ww / 2, y0 - dd, z)), Vector((ww / 2, y0 - dd, z)),
                     Vector((ww / 2, y0 + 0.02, z)), Vector((-ww / 2, y0 + 0.02, z))])
    hood = bm_sweep(secs)
    jitter(hood, 0.012, seed=3500)
    paint_by(hood, lambda f, l: mix(pal("siva", 2), pal("siva", 0), min(1.0, max(0.0, (l.vert.co.z - 1.95) / 1.4)) ** 0.7))
    parts2 = [(hood, Matrix.Translation((HEARTH_X, 0, 0)))]
    mantel = bm_box(1.92, 0.24, 0.20, bevel=0.03)
    jitter(mantel, 0.01, seed=3501)
    _wood(mantel, 3502, lo=2, hi=1)
    parts2.append((mantel, Matrix.Translation((HEARTH_X, IY - 0.98 + 0.06, 1.86))))
    # davlumbazı taşıyan taş ayaklar (ocak tablasından kirişe)
    for side in (-1, 1):
        z = HEARTH_H + 0.13
        k = 0
        while z < 1.86 - 0.05:
            hh = min(RP.uniform(0.24, 0.32), 1.86 - z)
            dep = 0.90 if k % 2 == 0 else 0.84
            bm = _stone_block(0.24 if k % 2 == 0 else 0.20, dep, hh - 0.015, 3510 + side * 20 + k, warm=0.55, bevel=0.025)
            parts2.append((bm, Matrix.Translation((HEARTH_X + side * 0.80, IY - dep / 2 - 0.02, z))))
            z += hh
            k += 1
    hb = bm_merge(*parts2)
    _obj(hb, "SM_Ocak_Davlumbaz")
    return ob

def build_fire():
    """Ocak_Ates: kor + alev (M_Fener). Mekanik açar/kapatır."""
    parts = []
    for i in range(7):   # kömür/odun parçaları (sönükken de görünür)
        bm = bm_box(RP.uniform(0.10, 0.18), RP.uniform(0.08, 0.12), RP.uniform(0.05, 0.08), bevel=0.02, origin="center")
        jitter(bm, 0.01, seed=3600 + i)
        paint_flat(bm, mix(demir(0.6), pal("ahsap", 2), RP.uniform(0.2, 0.6)))
        parts.append((bm, Matrix.Translation((RP.uniform(-0.18, 0.18), RP.uniform(-0.15, 0.15), 0.16))
                       @ Matrix.Rotation(RP.uniform(0, 3), 4, "Z")))
    coal = bm_merge(*parts)
    ob_c = _obj(coal, "SM_Ocak_Kor", loc=(HEARTH_X, HEARTH_Y - 0.05, 0))
    # alev: üç dilli yumuşak koniler
    fl = []
    for i, (dx, dy, hh, rr) in enumerate(((0, 0, 0.36, 0.13), (-0.12, 0.05, 0.26, 0.09), (0.11, -0.04, 0.28, 0.10))):
        prof = [(0.0, 0.0), (rr, 0.02), (rr * 1.05, hh * 0.3), (rr * 0.6, hh * 0.65), (0.0, hh)]
        bm = bm_lathe(prof, sides=7)
        transform_bm(bm, Matrix.Rotation(math.radians(RP.uniform(-8, 8)), 4, "X"))
        paint_by(bm, lambda f, l, hh=hh: mix(pal("altin", 0), pal("cati", 0), min(1.0, l.vert.co.z / hh)))
        fl.append((bm, Matrix.Translation((dx, dy, 0.18))))
    flame = bm_merge(*fl)
    mat = material_named("M_Fener", "#FFB13D", emission=4.0)
    ob_f = _obj(flame, "Ocak_Ates", loc=(HEARTH_X, HEARTH_Y - 0.05, 0), mat=mat)
    return ob_c, ob_f

# ================================================================== KAZAN
def build_cauldron():
    z0 = HEARTH_H - 0.10       # kazan deliğe oturur
    prof = [(0.0, 0.0), (0.16, 0.0), (0.30, 0.07), (0.40, 0.20), (0.42, 0.30), (0.39, 0.42), (0.33, 0.50),
            (0.31, 0.54), (0.36, 0.57), (0.375, 0.61), (0.345, 0.635), (0.30, 0.61), (0.29, 0.53), (0.0, 0.50)]
    bm = bm_lathe(prof, sides=14, close_top=False, close_bottom=False)
    def cfn(f, l):
        z = l.vert.co.z
        inner = Vector((l.vert.co.x, l.vert.co.y, 0)).length < 0.305 and z > 0.49
        if inner:
            return pal("bakir", 2)
        t = min(1.0, z / 0.6)
        c = mix(mix(demir(0.75), pal("bakir", 2), 0.5), pal("bakir", 1), min(1.0, t * 1.5))   # dip isli
        return mix(c, pal("bakir", 0), max(0.0, t - 0.6) * 1.8)
    paint_by(bm, cfn)
    parts = [(bm, None)]
    # perçinli kuşak
    band = bm_lathe([(0.425, 0.27), (0.44, 0.29), (0.44, 0.33), (0.425, 0.35)], sides=14, close_top=False, close_bottom=False)
    paint_by(band, lambda f, l: pal("bakir", 1))
    parts.append((band, None))
    for a in range(10):
        ang = 2 * math.pi * a / 10
        rv = bm_cyl(0.016, 0.012, 0.012, sides=5)
        paint_flat(rv, pal("bakir", 0))
        parts.append((rv, Matrix.Translation((math.cos(ang) * 0.44, math.sin(ang) * 0.44, 0.31))
                      @ Matrix.Rotation(ang, 4, "Z") @ Matrix.Rotation(math.pi / 2, 4, "Y")))
    # iri kulp halkaları (iki yanda)
    for side in (-1, 1):
        lug = bm_box(0.08, 0.10, 0.10, bevel=0.02, origin="center")
        paint_flat(lug, pal("bakir", 2))
        parts.append((lug, Matrix.Translation((side * 0.36, 0, 0.48))))
        tor = bm_lathe([(0.10, -0.02), (0.125, 0.0), (0.10, 0.02), (0.075, 0.0), (0.10, -0.02)], sides=10,
                       close_top=False, close_bottom=False)
        iron(tor)
        parts.append((tor, Matrix.Translation((side * 0.44, 0, 0.40)) @ Matrix.Rotation(math.pi / 2, 4, "X")))
    out = bm_merge(*parts)
    transform_bm(out, Matrix.Scale(KZ, 4))
    z0 = HEARTH_H - 0.10 * KZ
    ob = _obj(out, "Kazan", loc=(HEARTH_X, HEARTH_Y - 0.05, z0), sharp=50)
    # iksir yüzeyi: ayrı mesh (mekanik rengini değiştirir), M_Su
    surf = bmesh.new()
    verts = [surf.verts.new((math.cos(2 * math.pi * a / 14) * 0.295 * KZ, math.sin(2 * math.pi * a / 14) * 0.295 * KZ, 0)) for a in range(14)]
    c = surf.verts.new((0, 0, 0.012))
    for a in range(14):
        surf.faces.new((c, verts[a], verts[(a + 1) % 14]))
    paint_by(surf, lambda f, l: mix(pal("su", 1), pal("su", 0), 0.3))
    _obj(surf, "Kazan_Iksir", loc=(HEARTH_X, HEARTH_Y - 0.05, z0 + 0.535 * KZ), mat=material_named("M_Su", "#35BDEB"), sharp=0)
    return ob

# ================================================================== TERAZİ (×2)
BEAM_L = 1.10
COL_H = 1.30
def build_scale(tag):
    """Terazi_<tag> (boş, taban ortasında) → çocuklar:
       SM_Terazi_<tag>_Govde (sabit) · Terazi_<tag>_Kol (dayanak noktasında, yerel Y ekseni etrafında döner)
       Terazi_<tag>_Kefe_L / _Kefe_R (asılma noktasında; kol dönünce script dikey taşır)."""
    x, y = SCALE_POS[tag]
    g = _group("Terazi_" + tag, loc=(x, y, 0))
    sd = 4000 + (0 if tag == "Giren" else 500)
    # --- gövde: ayaklı ahşap kaide + pirinç sütun + üstte çatal
    parts = []
    for k, ang in enumerate((0, 120, 240)):   # üç tombul ayak
        a = math.radians(ang + 30)
        foot = bm_prism([(0, 0), (0.34, 0), (0.30, 0.08), (0.06, 0.20), (0, 0.22)], -0.05, 0.05)
        bevel_all(foot, 0.015)
        _wood(foot, sd + k, lo=2, hi=1)
        parts.append((foot, Matrix.Rotation(a, 4, "Z")))
    base = bm_lathe([(0, 0.18), (0.13, 0.18), (0.14, 0.24), (0.10, 0.30), (0.0, 0.30)], sides=8)
    _wood(base, sd + 5, lo=1, hi=0)
    parts.append((base, None))
    colm = bm_lathe([(0.0, 0.30), (0.05, 0.30), (0.045, 0.6), (0.04, COL_H - 0.12), (0.055, COL_H - 0.08),
                     (0.035, COL_H - 0.04), (0.0, COL_H - 0.04)], sides=8)
    paint_part(colm, "altin", lo=2, hi=1, seed=sd + 6)
    parts.append((colm, None))
    for side in (-1, 1):   # dayanak çatalı
        fk = bm_box(0.03, 0.05, 0.14, bevel=0.008)
        paint_part(fk, "altin", lo=2, hi=1, seed=sd + 7)
        parts.append((fk, Matrix.Translation((0, side * 0.045, COL_H - 0.06))))
    # ibre kadranı (yay), kolun okunurluğu için
    arc = []
    for i in range(9):
        a = math.radians(-35 + 70 * i / 8)
        arc.append((math.sin(a) * 0.16, COL_H + 0.10 + math.cos(a) * 0.16))
    arc_poly = arc + [(p[0] * 0.80, COL_H + 0.10 + (p[1] - COL_H - 0.10) * 0.80) for p in reversed(arc)]
    dial = bm_prism(arc_poly, -0.015, 0.015)
    paint_part(dial, "altin", lo=1, hi=0, seed=sd + 8)
    parts.append((dial, Matrix.Translation((0, -0.07, -0.04))))
    stem = bm_box(0.025, 0.02, 0.12, bevel=0.005)
    paint_part(stem, "altin", lo=2, hi=1, seed=sd + 9)
    parts.append((stem, Matrix.Translation((0, -0.07, COL_H - 0.02))))
    body = bm_merge(*parts)
    ob_b = _obj(body, "SM_Terazi_%s_Govde" % tag, loc=(x, y, 0))
    # --- kol: pivot dayanakta
    kp = []
    beam = bm_sweep([[Vector((-BEAM_L / 2, -0.03, -0.035)), Vector((-BEAM_L / 2, 0.03, -0.035)),
                      Vector((-BEAM_L / 2, 0.03, 0.02)), Vector((-BEAM_L / 2, -0.03, 0.02))],
                     [Vector((0, -0.04, -0.06)), Vector((0, 0.04, -0.06)), Vector((0, 0.04, 0.04)), Vector((0, -0.04, 0.04))],
                     [Vector((BEAM_L / 2, -0.03, -0.035)), Vector((BEAM_L / 2, 0.03, -0.035)),
                      Vector((BEAM_L / 2, 0.03, 0.02)), Vector((BEAM_L / 2, -0.03, 0.02))]])
    bevel_all(beam, 0.008)
    iron(beam)
    kp.append((beam, None))
    for side in (-1, 1):   # uçlarda yuvarlak topuz + kanca
        knob = bm_lathe([(0, -0.04), (0.035, -0.03), (0.04, 0.0), (0.03, 0.03), (0, 0.04)], sides=8)
        paint_part(knob, "altin", lo=1, hi=0, seed=sd + 20)
        kp.append((knob, Matrix.Translation((side * BEAM_L / 2, 0, -0.01)) @ Matrix.Rotation(math.pi / 2, 4, "Y")))
    pin = bm_cyl(0.02, 0.02, 0.14, sides=6)
    paint_part(pin, "altin", lo=2, hi=1, seed=sd + 21)
    kp.append((pin, Matrix.Translation((0, -0.07, 0)) @ Matrix.Rotation(-math.pi / 2, 4, "X")))
    needle = bm_prism([(-0.012, 0.0), (0.012, 0.0), (0.0, 0.20)], -0.008, 0.008)
    paint_part(needle, "cati", lo=1, hi=0, seed=sd + 22)       # ibre: vurgu rengi, okunur
    kp.append((needle, Matrix.Translation((0, -0.075, 0.02))))
    kol = bm_merge(*kp)
    ob_k = _obj(kol, "Terazi_%s_Kol" % tag, loc=(x, y, COL_H))
    # --- kefeler: asılma noktası pivot
    pans = []
    for side, nm in ((-1, "L"), (1, "R")):
        pp = []
        hang = 0.42
        pan = bm_lathe([(0.0, -hang - 0.06), (0.10, -hang - 0.055), (0.19, -hang - 0.02), (0.205, -hang + 0.005),
                        (0.19, -hang + 0.0), (0.17, -hang - 0.025), (0.0, -hang - 0.04)], sides=12,
                       close_top=False, close_bottom=False)
        paint_part(pan, "altin", lo=2, hi=0, seed=sd + 30)
        pp.append((pan, None))
        for k in range(3):   # üç kalın çubuk (zincir yerine; okunur)
            a = 2 * math.pi * k / 3 + 0.4
            top = Vector((0, 0, -0.02))
            bot = Vector((math.cos(a) * 0.18, math.sin(a) * 0.18, -hang))
            d = bot - top
            rod = bm_cyl(0.008, 0.008, d.length, sides=5)
            q = Vector((0, 0, 1)).rotation_difference(d.normalized())
            pp.append((rod, Matrix.Translation(top) @ q.to_matrix().to_4x4()))
            iron(rod)
        hook = bm_lathe([(0.0, -0.03), (0.022, -0.02), (0.022, 0.0), (0.0, 0.01)], sides=6)
        iron(hook)
        pp.append((hook, None))
        kefe = bm_merge(*pp)
        _obj(kefe, "Terazi_%s_Kefe_%s" % (tag, nm), loc=(x + side * BEAM_L / 2, y, COL_H - 0.02))
        pans.append(kefe)
    for o in (ob_b, ob_k, bpy.data.objects["Terazi_%s_Kefe_L" % tag], bpy.data.objects["Terazi_%s_Kefe_R" % tag]):
        _parent_local(o, g)
    return g

def _parent_local(child, parent):
    """Çocuğun dünya konumunu koruyarak, ters matris kullanmadan ebeveyne bağla (Unity'de temiz yerel değerler)."""
    wl = Vector(child.location)
    child.parent = parent
    child.matrix_parent_inverse = Matrix.Identity(4)
    child.location = wl - Vector(parent.location)

# ================================================================== KAVANOZ / ŞİŞE / KÜP
# İçerik renkleri (referans §F.3) — hepsi paletten
CONTENT = {
    "civa":     pal("kaya", 0),
    "kukurt":   pal("altin", 0),
    "tuz":      mix(pal("kaya", 0), pal("siva", 0), 0.5),
    "sap":      mix(pal("su", 0), pal("siva", 0), 0.6),
    "nisadir":  pal("siva", 0),
    "goztasi":  pal("su", 1),
    "yesilzac": pal("yaprak", 0),
    "kralsuyu": pal("altin", 1),
    "kezzap":   mix(pal("altin", 0), pal("siva", 0), 0.6),
    "zirnik":   pal("cati", 0),
    "boraks":   mix(pal("siva", 0), pal("kaya", 0), 0.3),
    "kirec":    pal("siva", 0),
    "sirke":    pal("kil", 0),
    "bos":      mix(pal("su", 0), pal("siva", 0), 0.5),
}
GLASS = mix(pal("su", 0), pal("gok", 0), 0.5)
WAX = pal("cati", 1)          # ustanın mührü: vurgu rengi

def jar_bm(kind, content, fill=0.65, seed=0):
    """Pivot taban ortası. kind: kavanoz | sise | kup | testi. Opak pastel cam + içerik rengi."""
    r = random.Random(seed)
    s = r.uniform(0.92, 1.08)
    parts = []
    cc = CONTENT[content]
    if kind == "kavanoz":
        h, rad = 0.20 * s, 0.075 * s
        prof = [(0, 0), (rad * 0.85, 0), (rad, 0.02), (rad * 1.02, h * 0.75), (rad * 0.8, h * 0.88), (rad * 0.62, h * 0.9),
                (rad * 0.62, h), (0, h)]
    elif kind == "sise":
        h, rad = 0.26 * s, 0.07 * s
        prof = [(0, 0), (rad * 0.7, 0), (rad, 0.04), (rad * 1.05, h * 0.35), (rad * 0.75, h * 0.55), (rad * 0.28, h * 0.68),
                (rad * 0.26, h * 0.92), (rad * 0.34, h * 0.95), (rad * 0.3, h), (0, h)]
    elif kind == "kup":
        h, rad = 0.55 * s, 0.24 * s
        prof = [(0, 0), (rad * 0.55, 0), (rad * 0.95, h * 0.35), (rad, h * 0.55), (rad * 0.7, h * 0.86), (rad * 0.55, h * 0.9),
                (rad * 0.62, h * 0.97), (rad * 0.55, h), (0, h * 0.97)]
    else:  # testi
        h, rad = 0.34 * s, 0.11 * s
        prof = [(0, 0), (rad * 0.6, 0), (rad, h * 0.3), (rad * 0.95, h * 0.55), (rad * 0.45, h * 0.78), (rad * 0.35, h * 0.92),
                (rad * 0.45, h), (0, h * 0.98)]
    body = bm_lathe(prof, sides=8 if kind in ("kavanoz", "sise") else 10)
    if kind in ("kup", "testi"):
        paint_by(body, lambda f, l: mix(pal("kil", 2), pal("kil", 0), min(1, l.vert.co.z / h)) if l.vert.co.z < h * 0.8
                 else mix(pal("kil", 1), pal("su", 2), 0.35))       # ağız kenarı sırlı (mavi-yeşil sır)
    else:
        fz = h * fill
        paint_by(body, lambda f, l: mix(cc, mix(cc, pal("kaya", 2), 0.25), 0.0 if l.vert.co.z > fz * 0.4 else 0.6)
                 if l.vert.co.z <= fz else GLASS)
    parts.append((body, None))
    if kind in ("kavanoz", "sise"):
        # mühür: balmumu kapak + ip
        top_r = prof[-2][0] if kind == "kavanoz" else prof[-3][0]
        cap = bm_lathe([(0, 0), (top_r * 1.25, 0), (top_r * 1.3, 0.02), (top_r * 0.9, 0.04), (0, 0.045)], sides=7)
        jitter(cap, 0.003, seed=seed)
        paint_by(cap, lambda f, l: mix(WAX, pal("cati", 2), 0.3 if f.normal.z < 0.5 else 0.0))
        parts.append((cap, Matrix.Translation((0, 0, h - 0.01))))
        # etiket alanı (yazı Unity'de TMP ile)
        lab = bm_box(rad * 1.1, 0.006, h * 0.32, origin="center")
        paint_flat(lab, pal("siva", 0))
        parts.append((lab, Matrix.Translation((0, -prof[3][0] - 0.002, h * 0.42))))
    elif kind == "kup":
        lid = bm_lathe([(0, 0), (rad * 0.62, 0), (rad * 0.6, 0.03), (rad * 0.2, 0.06), (0.03, 0.09), (0, 0.09)], sides=10)
        paint_part(lid, "ahsap", lo=1, hi=0, seed=seed)
        parts.append((lid, Matrix.Translation((0, 0, h - 0.01))))
    out = bm_merge(*parts)
    transform_bm(out, Matrix.Rotation(r.uniform(-0.3, 0.3), 4, "Z"))
    return out

def build_mechanic_jar():
    """Mekanik için temiz şablon: Kavanoz_Muhurlu (etiket yüzü -Y, pivot taban). Kodda içerik/etiket değişir.
    İlk raf yuvasında durur (Raf_Yuva_1_1)."""
    bm = jar_bm("kavanoz", "bos", fill=0.0, seed=1)
    y = bpy.data.objects.get("Raf_Yuva_1_1")
    _obj(bm, "Kavanoz_Muhurlu", loc=tuple(y.location) if y else (-1.47, 2.86, 0.8))

# ================================================================== RAF
def shelf_bm(w, h, d, levels, seed):
    """Dolap-raf: kalın yan panolar, arkada dikey tahta kaplama (eşya koyu zeminde okunur),
    üstte taşan taç tahtası, altta süpürgelik."""
    parts = []
    r = random.Random(seed)
    for side in (-1, 1):   # yan panolar (önden görünen kalın kenar)
        post = bm_box(0.06, d, h, bevel=0.015)
        jitter(post, 0.006, seed=seed + side)
        _wood(post, seed + 10 + side, lo=2, hi=1)
        parts.append((post, Matrix.Translation((side * (w / 2 - 0.03), 0, 0))))
        front = bm_box(0.09, 0.05, h + 0.02, bevel=0.015)
        _wood(front, seed + 12 + side, lo=1, hi=0)
        parts.append((front, Matrix.Translation((side * (w / 2 - 0.035), -d / 2, 0))))
    nb = max(3, int(w / 0.22))
    for i in range(nb):   # arka kaplama tahtaları
        bw = (w - 0.1) / nb
        bk = bm_box(bw - 0.006, 0.02, h - 0.05 + r.uniform(-0.02, 0.0), bevel=0.004)
        paint_part(bk, "ahsap", lo=2, hi=1, noise=0.05, seed=seed + 60 + i, bias=0.15 + 0.1 * (i % 2))
        parts.append((bk, Matrix.Translation((-w / 2 + 0.05 + bw * (i + 0.5), d / 2 - 0.01, 0.02))))
    crown = bm_box(w + 0.14, d + 0.08, 0.07, bevel=0.02)
    _wood(crown, seed + 70, lo=1, hi=0)
    parts.append((crown, Matrix.Translation((0, -0.03, h))))
    crown2 = bm_box(w + 0.06, d + 0.02, 0.05, bevel=0.015)
    _wood(crown2, seed + 71, lo=2, hi=1)
    parts.append((crown2, Matrix.Translation((0, -0.01, h - 0.05))))
    kick = bm_box(w, 0.05, 0.12, bevel=0.01)
    _wood(kick, seed + 72, lo=2, hi=2)
    parts.append((kick, Matrix.Translation((0, -d / 2 + 0.04, 0))))
    zs = []
    for i in range(levels):
        z = 0.18 + (h - 0.30) * i / max(1, levels - 1)
        zs.append(z + 0.045)
        secs = []
        for k in range(5):   # hafif sarkan tahta (pub dioraması referansı)
            x = -w / 2 + 0.01 + (w - 0.02) * k / 4
            sag = -0.012 * math.sin(math.pi * k / 4)
            secs.append([Vector((x, -d / 2, z + sag)), Vector((x, d / 2, z + sag)),
                         Vector((x, d / 2, z + 0.045 + sag)), Vector((x, -d / 2, z + 0.045 + sag))])
        bmb = bm_sweep(secs)
        bevel_all(bmb, 0.01)
        _wood(bmb, seed + 30 + i, lo=1, hi=0)
        parts.append((bmb, Matrix.Rotation(math.radians(r.uniform(-0.6, 0.6)), 4, "Y")))
    return bm_merge(*parts), zs

def build_shelves():
    # 1) Mekanik rafı: arka duvar, ocağın solu (kazan istasyon kadrajında)
    w, h, d = 1.55, 2.0, 0.34
    bm, zs = shelf_bm(w, h, d, 4, 5000)
    sx, sy = -0.95, IY - d / 2 - 0.02
    _obj(bm, "SM_Raf_Malzeme", loc=(sx, sy, 0))
    # mekanik kavanozlarının yerleri: boş bırakılan 2. ve 3. raf → yer noktaları
    for i, z in enumerate(zs[1:3]):
        for k in range(4):
            e = empty("Raf_Yuva_%d_%d" % (i + 1, k + 1), _col(), loc=(sx - w / 2 + 0.25 + k * (w - 0.5) / 3, sy - 0.03, z), size=0.06)
    # üst ve alt rafa dekor: küp, testi, kese
    deco = []
    for k, (kind, cont) in enumerate((("testi", "sirke"), ("kup", "tuz"))):
        deco.append((jar_bm(kind, cont, seed=5100 + k), Matrix.Translation((sx - 0.45 + k * 0.85, sy, zs[0]))))
    for k, cont in enumerate(("kezzap", "kralsuyu", "civa", "nisadir")):
        deco.append((jar_bm("sise", cont, seed=5200 + k), Matrix.Translation((sx - 0.55 + k * 0.36, sy, zs[3]))))
    _obj(bm_merge(*deco), "SM_Raf_Malzeme_Dekor")
    # 2) Râzî rafı (KB köşe, batı duvarı): 6 madde grubu — 3 kat × 2 bölme
    w, h, d = 1.40, 2.15, 0.36
    bm, zs = shelf_bm(w, h, d, 4, 5300)
    divider = bm_box(0.04, d - 0.02, h - 0.3, bevel=0.008)
    _wood(divider, 5310, lo=2, hi=1)
    rx, ry = -IX + d / 2 + 0.02, 2.30
    parts = [(bm, None), (divider, Matrix.Translation((0, 0, 0.18)))]
    groups = [  # (bölme, kat) → (grup adı, içerikler)
        ("Ruhlar", ("civa", "nisadir", "kukurt", "zirnik")),
        ("Cisimler", None),
        ("Taslar", None),
        ("Zaclar", ("goztasi", "yesilzac", "sap")),
        ("Borakslar", ("boraks", "boraks")),
        ("Tuzlar", ("tuz", "kirec")),
    ]
    gi = 0
    for lvl in range(3):
        for half in (-1, 1):
            name, items = groups[gi]
            gi += 1
            z = zs[lvl + 1]
            cx = half * w / 4
            if items:
                for k, cont in enumerate(items):
                    kind = "kavanoz" if k % 2 == 0 else "sise"
                    jb = jar_bm(kind, cont, seed=5400 + gi * 10 + k)
                    parts.append((jb, Matrix.Translation((cx - 0.2 + k * 0.4 / max(1, len(items) - 1), 0.0, z))))
            elif name == "Cisimler":   # metal külçeler: altın, gümüş, bakır, kurşun, kalay, demir
                cols = (pal("altin", 0), pal("kaya", 0), pal("bakir", 0), pal("dag", 2), mix(pal("kaya", 0), pal("siva", 0), 0.5), demir(0.9))
                for k, c in enumerate(cols):
                    ing = bm_prism([(-0.06, 0), (0.06, 0), (0.045, 0.035), (-0.045, 0.035)], -0.025, 0.025)
                    bevel_all(ing, 0.006)
                    paint_flat(ing, c)
                    parts.append((ing, Matrix.Translation((cx - 0.2 + (k % 3) * 0.14, -0.06 + (k // 3) * 0.1, z + (k // 3) * 0.0))
                                  @ Matrix.Rotation(RP.uniform(-0.3, 0.3), 4, "Z")))
            else:   # Taşlar: iri kristal/maden parçaları
                cols = (pal("dag", 1), pal("yaprak", 1), pal("su", 2), pal("cati", 2), pal("kaya", 1))
                for k, c in enumerate(cols):
                    st = bm_cyl(RP.uniform(0.03, 0.05), RP.uniform(0.01, 0.03), RP.uniform(0.05, 0.09), sides=5)
                    jitter(st, 0.008, seed=5500 + k)
                    paint_flat(st, c)
                    parts.append((st, Matrix.Translation((cx - 0.2 + k * 0.1, RP.uniform(-0.06, 0.06), z))))
            # grup levhası (yazı Unity'de)
            pl = bm_box(0.30, 0.012, 0.07, bevel=0.004, origin="center")
            _wood(pl, 5600 + gi, lo=0, hi=0)
            parts.append((pl, Matrix.Translation((cx, -d / 2 - 0.006, z - 0.03))))
            empty("Raf_Levha_%s" % name, _col(), loc=(rx + d / 2 + 0.012, ry + cx, z - 0.03), size=0.05)
    out = bm_merge(*parts)
    transform_bm(out, Matrix.Rotation(math.pi / 2, 4, "Z"))      # ön yüz (-Y) doğuya, odaya baksın
    _obj(out, "SM_Raf_Razi", loc=(rx, ry, 0))

# ================================================================== KARATAHTA (doğu duvarı)
def build_chalkboard():
    w, h = 1.6, 1.05
    parts = []
    board = bm_box(w, 0.03, h, origin="center")
    paint_by(board, lambda f, l: mix(pal("dag", 2), demir(0.7), 0.62 + 0.15 * _nz_s(l.vert.co * 3)))
    parts.append((board, None))
    for (x0, z0, ww, hh) in ((0, h / 2 + 0.04, w + 0.16, 0.09), (0, -h / 2 - 0.04, w + 0.16, 0.09),
                             (-w / 2 - 0.04, 0, 0.09, h), (w / 2 + 0.04, 0, 0.09, h)):
        fr = bm_box(ww, 0.06, hh, bevel=0.012, origin="center")
        jitter(fr, 0.004, seed=int(x0 * 10 + z0 * 10) + 6000)
        _wood(fr, 6001 + int(abs(x0) * 10), lo=1, hi=0)
        parts.append((fr, Matrix.Translation((x0, -0.01, z0))))
    ledge = bm_box(w * 0.9, 0.09, 0.03, bevel=0.008, origin="center")
    _wood(ledge, 6010, lo=1, hi=0)
    parts.append((ledge, Matrix.Translation((0, -0.05, -h / 2 - 0.10))))
    for k in range(3):   # tebeşir parçaları (kireç)
        ch = bm_box(0.06, 0.012, 0.012, origin="center")
        paint_flat(ch, pal("siva", 0))
        parts.append((ch, Matrix.Translation((-0.4 + k * 0.12, -0.06, -h / 2 - 0.078)) @ Matrix.Rotation(RP.uniform(-0.4, 0.4), 4, "Z")))
    out = bm_merge(*parts)
    transform_bm(out, Matrix.Rotation(-math.pi / 2, 4, "Z"))  # -Y yüzü batıya (odaya) baksın
    _obj(out, "Karatahta", loc=(IX - 0.05, 0.35, 1.55))

# ================================================================== ÇALIŞMA MASASI (batı penceresi altı)
def _plank(w, d, h, seed, lo=1, hi=0, bevel=0.012, jit=0.005):
    bm = bm_box(w, d, h, bevel=bevel)
    jitter(bm, jit, seed=seed)
    return _wood(bm, seed, lo=lo, hi=hi)

def _leg(h, t, seed, splay=0.0):
    """Kalın, hafif konik, pahlı ahşap ayak (kare kesit + üstte boğum)."""
    a, b = t / 2, t / 2 * 0.82
    secs = []
    for z, s in ((0.0, b), (0.05, a * 0.95), (h * 0.55, b), (h - 0.14, a), (h - 0.13, a * 0.88), (h, a * 0.95)):
        secs.append([Vector((-s, -s, z)), Vector((s, -s, z)), Vector((s, s, z)), Vector((-s, s, z))])
    bm = bm_sweep(secs)
    bevel_all(bm, t * 0.12)
    jitter(bm, 0.004, seed=seed)
    return _wood(bm, seed, lo=2, hi=1, bias=0.1)

def table_bm(w, d, h, seed, top_t=0.07):
    """Ağır zanaatkâr masası: ayrı tahtalı kalın tabla, alt kuşaklar, konik ayaklar, ayak germeleri."""
    parts = []
    r = random.Random(seed)
    n = max(3, int(round(d / 0.24)))
    pw = d / n
    for i in range(n):   # tabla tahtaları (aralarında ince derz, hafif yükseklik farkı)
        pl = _plank(w + r.uniform(-0.03, 0.02), pw - 0.008, top_t + r.uniform(-0.006, 0.006), seed + i, lo=1, hi=0)
        parts.append((pl, Matrix.Translation((r.uniform(-0.01, 0.01), -d / 2 + pw * (i + 0.5), h - top_t))))
    for sx in (-1, 1):   # tabla altı kuşaklar
        cl = _plank(0.08, d - 0.04, 0.06, seed + 20 + sx, lo=2, hi=1)
        parts.append((cl, Matrix.Translation((sx * (w / 2 - 0.16), 0, h - top_t - 0.06))))
    for sy in (-1, 1):
        ap = _plank(w - 0.28, 0.05, 0.11, seed + 25 + sy, lo=2, hi=1)
        parts.append((ap, Matrix.Translation((0, sy * (d / 2 - 0.12), h - top_t - 0.11))))
    lt = 0.10 if h > 0.6 else 0.07
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append((_leg(h - top_t, lt, seed + 30 + sx * 3 + sy), Matrix.Translation((sx * (w / 2 - 0.13), sy * (d / 2 - 0.11), 0))))
        st = _plank(0.06, d - 0.22, 0.07, seed + 40 + sx, lo=2, hi=2)
        parts.append((st, Matrix.Translation((sx * (w / 2 - 0.13), 0, 0.14))))
    st = _plank(w - 0.26, 0.07, 0.06, seed + 45, lo=2, hi=1)       # orta germe
    parts.append((st, Matrix.Translation((0, 0, 0.16))))
    return bm_merge(*parts)

def build_desk():
    w, d, h = 1.45, 0.75, 0.80
    bm = table_bm(w, d, h, 6100)
    dx, dy = -IX + d / 2 + 0.06, 0.60
    transform_bm(bm, Matrix.Rotation(-math.pi / 2, 4, "Z"))
    _obj(bm, "SM_Masa", loc=(dx, dy, 0))
    # masa üstü: kâğıt alanı, divit, hokka, ustanın defteri, havan
    parts = []
    paper = bm_box(0.42, 0.30, 0.004, origin="bottom")
    paint_flat(paper, pal("siva", 0))
    parts.append((paper, Matrix.Translation((0.05, -0.05, 0)) @ Matrix.Rotation(0.06, 4, "Z")))
    ink = bm_lathe([(0, 0), (0.045, 0), (0.05, 0.03), (0.03, 0.06), (0.02, 0.065), (0, 0.06)], sides=8)
    paint_part(ink, "kil", lo=2, hi=1, seed=6110)
    parts.append((ink, Matrix.Translation((0.30, 0.22, 0))))
    pen = bm_cyl(0.006, 0.004, 0.24, sides=5)
    paint_part(pen, "ahsap", lo=0, hi=0, seed=6111)
    parts.append((pen, Matrix.Translation((0.30, 0.22, 0.04)) @ Matrix.Rotation(0.5, 4, "X")))
    book = []
    for k, (ww, dd, hh, c) in enumerate(((0.30, 0.22, 0.012, pal("ahsap", 2)), (0.29, 0.21, 0.045, pal("siva", 0)), (0.30, 0.22, 0.012, pal("ahsap", 2)))):
        b = bm_box(ww, dd, hh, bevel=0.004)
        paint_flat(b, c)
        book.append((b, Matrix.Translation((0, 0, [0, 0.012, 0.057][k]))))
    clasp = bm_box(0.04, 0.02, 0.07, bevel=0.004)
    paint_part(clasp, "altin", lo=1, hi=0, seed=6112)
    book.append((clasp, Matrix.Translation((0.15, 0, 0.0))))
    bookm = bm_merge(*book)
    _obj(bookm, "Usta_Defteri", loc=(dx + 0.12, dy - 0.48, h), rot=(0, 0, -100))
    # havan + havaneli (taş): kalın kâse, nervürlü
    mort = bm_lathe([(0, 0), (0.08, 0), (0.095, 0.02), (0.10, 0.10), (0.11, 0.13), (0.085, 0.13), (0.07, 0.05), (0, 0.045)], sides=10)
    paint_part(mort, "kaya", lo=1, hi=0, seed=6113)
    _obj(mort, "Havan", loc=(dx - 0.05, dy + 0.52, h))
    pes = bm_lathe([(0, 0), (0.03, 0.01), (0.025, 0.05), (0.014, 0.08), (0.014, 0.17), (0.022, 0.2), (0, 0.215)], sides=6)
    paint_part(pes, "kaya", lo=1, hi=0, seed=6114)
    _obj(pes, "Havaneli", loc=(dx - 0.05, dy + 0.52, h + 0.06), rot=(0, 28, 0))
    _obj(bm_merge(*parts), "SM_Masa_Ustu", loc=(dx, dy, h), rot=(0, 0, -90))
    # tabure
    st = []
    seat = bm_lathe([(0, 0), (0.19, 0), (0.2, 0.025), (0.18, 0.05), (0, 0.05)], sides=10)
    _wood(seat, 6120, lo=1, hi=0)
    st.append((seat, Matrix.Translation((0, 0, 0.42))))
    for k in range(3):
        a = 2 * math.pi * k / 3
        leg = bm_cyl(0.025, 0.02, 0.44, sides=5)
        _wood(leg, 6121 + k, lo=2, hi=1)
        st.append((leg, Matrix.Translation((math.cos(a) * 0.13, math.sin(a) * 0.13, 0)) @ Matrix.Rotation(0.12, 4, Vector((-math.sin(a), math.cos(a), 0)))))
    _obj(bm_merge(*st), "Tabure", loc=(dx + 0.65, dy + 0.1, 0))
    empty("Masa_Kagit_Noktasi", _col(), loc=(dx, dy - 0.05, h + 0.005), size=0.1)

# ================================================================== ATHANOR (KD köşe) + ALUDEL
def build_athanor():
    parts = []
    # yakıt kulesi: tombul silindir, üstü kubbe kapaklı
    tower = bm_lathe([(0, 0), (0.42, 0), (0.44, 0.15), (0.40, 1.10), (0.36, 1.30), (0.30, 1.38), (0.31, 1.42), (0.22, 1.50), (0, 1.53)], sides=12)
    paint_by(tower, lambda f, l: mix(pal("kil", 2), pal("kil", 0), min(1, l.vert.co.z / 1.2)) if l.vert.co.z < 1.36 else
             mix(pal("kil", 2), demir(0.9), 0.3))
    parts.append((tower, None))
    # bağlar (demir çember)
    for z in (0.35, 0.85):
        band = bm_lathe([(0.445, z), (0.455, z + 0.02), (0.445, z + 0.05)], sides=12, close_top=False, close_bottom=False)
        iron(band)
        parts.append((band, None))
    # yan ısıtma bölmesi (kaplar burada) + üstünde aludel
    side = bm_box(0.70, 0.62, 0.78, bevel=0.04)
    jitter(side, 0.01, seed=6201)
    paint_part(side, "kil", lo=2, hi=0, seed=6202)
    parts.append((side, Matrix.Translation((-0.62, 0.0, 0.0))))
    # küçük demir kapaklar + hava delikleri
    for (x, z, ww, hh) in ((0.0, 0.22, 0.26, 0.20), (0.0, 0.70, 0.18, 0.14), (-0.62, 0.30, 0.30, 0.20)):
        dr = bm_box(ww, 0.03, hh, bevel=0.01, origin="center")
        iron(dr)
        yoff = -0.43 if x == 0.0 else -0.33
        parts.append((dr, Matrix.Translation((x, yoff, z + hh / 2))))
        knob = bm_cyl(0.015, 0.015, 0.03, sides=5)
        iron(knob)
        parts.append((knob, Matrix.Translation((x + ww * 0.3, yoff - 0.02, z + hh / 2)) @ Matrix.Rotation(math.pi / 2, 4, "X")))
    # eğik bağlantı ağzı (kuleden bölmeye)
    nk = bm_cyl(0.10, 0.09, 0.32, sides=6)
    paint_part(nk, "kil", lo=1, hi=0, seed=6203)
    parts.append((nk, Matrix.Translation((-0.22, 0, 0.62)) @ Matrix.Rotation(-math.pi / 2 - 0.35, 4, "Y")))
    out = bm_merge(*parts)
    ax, ay = IX - 0.60, IY - 0.55       # kapaklar odaya (-Y), yan bölme ocağa doğru (-X)
    _obj(out, "SM_Athanor", loc=(ax, ay, 0))
    # aludel: dipsiz çömlekler üst üste, yukarı daralan kule (süblimleştirme)
    al = []
    z = 0.0
    for k in range(4):
        rr = 0.17 - k * 0.025
        hh = 0.13
        ring = bm_lathe([(rr * 0.85, 0), (rr, 0.03), (rr * 1.02, hh * 0.7), (rr * 0.88, hh)], sides=10, close_top=False, close_bottom=False)
        paint_part(ring, "kil", lo=1 if k % 2 else 2, hi=0, seed=6210 + k)
        al.append((ring, Matrix.Translation((0, 0, z))))
        z += hh - 0.015
    cap = bm_lathe([(0.10, 0), (0.105, 0.03), (0.05, 0.09), (0.0, 0.10)], sides=10, close_bottom=False)
    paint_part(cap, "kil", lo=1, hi=0, seed=6215)
    al.append((cap, Matrix.Translation((0, 0, z))))
    alm = bm_merge(*al)
    _obj(alm, "Aludel", loc=(ax - 0.62, ay, 0.78))

# ================================================================== İMBİK TEZGÂHI (doğu duvarı, güney)
def build_alembic_bench():
    w, d, h = 1.60, 0.70, 0.86
    bm = table_bm(w, d, h, 6300, top_t=0.09)
    bx, by = IX - d / 2 - 0.05, -1.55     # doğu penceresinin altında (buhar/duman pencereye)
    transform_bm(bm, Matrix.Rotation(math.pi / 2, 4, "Z"))
    _obj(bm, "SM_Tezgah", loc=(bx, by, 0))
    # 2. geçiş: damıtma takımı, su banyosu, huni ve retort orta "simya tezgâhı"na taşındı (simya_tezgahi.py);
    # bu tezgâh artık tartım tezgâhı (üstü atolye_ic.build_center içinde).
    return

# ================================================================== KÜÇÜK ALETLER + DEPO
def build_tools():
    # körük (ocağın sağ ayağına yaslı)
    parts = []
    for side in (-1, 1):
        pl = bm_prism([(-0.13, 0), (0.13, 0), (0.09, 0.38), (0.04, 0.46), (-0.04, 0.46), (-0.09, 0.38)], -0.012, 0.012)
        bevel_all(pl, 0.006)
        _wood(pl, 6400 + side, lo=1, hi=0)
        parts.append((pl, Matrix.Translation((0, side * 0.06, 0))))
    leather = bm_lathe([(0.0, 0.03), (0.11, 0.05), (0.12, 0.2), (0.08, 0.36), (0.0, 0.40)], sides=8)
    transform_bm(leather, Matrix.Scale(0.45, 4, (0, 1, 0)))
    paint_part(leather, "ahsap", lo=2, hi=2, seed=6402)
    parts.append((leather, None))
    nz = bm_cyl(0.025, 0.012, 0.16, sides=6)
    paint_part(nz, "bakir", lo=1, hi=0, seed=6403)
    parts.append((nz, Matrix.Translation((0, 0, -0.14))))
    for side in (-1, 1):
        hd = bm_cyl(0.018, 0.018, 0.22, sides=5)
        _wood(hd, 6404, lo=1, hi=0)
        parts.append((hd, Matrix.Translation((side * 0.05, 0, 0.44))))
    kb = bm_merge(*parts)
    _obj(kb, "Koruk", loc=(0.74, 2.06, 0.30), rot=(-18, 0, 8))   # ocağın önüne yaslı
    # maşa ve kepçe: davlumbaz ayağına asılı
    ms = []
    for side in (-1, 1):
        arm = bm_box(0.018, 0.012, 0.70, bevel=0.004)
        iron(arm)
        ms.append((arm, Matrix.Rotation(side * 0.05, 4, "Y")))
    ring = bm_lathe([(0.03, -0.006), (0.038, 0), (0.03, 0.006), (0.022, 0)], sides=6, close_top=False, close_bottom=False)
    iron(ring)
    ms.append((ring, Matrix.Translation((0, 0, 0.72)) @ Matrix.Rotation(math.pi / 2, 4, "X")))
    _obj(bm_merge(*ms), "Ocak_Masasi", loc=(0.53, 2.29, 0.84))      # davlumbaz ayağının önünde asılı
    kp = []
    sp = bm_box(0.022, 0.012, 0.62, bevel=0.004)
    iron(sp)
    kp.append((sp, None))
    bowl = bm_lathe([(0, -0.04), (0.06, -0.03), (0.065, 0.0), (0.0, -0.01)], sides=8, close_top=False)
    iron(bowl)
    kp.append((bowl, Matrix.Translation((0, -0.02, 0))))
    _obj(bm_merge(*kp), "Kepce", loc=(0.67, 2.29, 0.86))
    # kroze seti (üçgen ağızlı, iç içe boylar) ocak tablası köşesinde
    kr = []
    for k, (hh, rr) in enumerate(((0.12, 0.07), (0.09, 0.055), (0.065, 0.042))):
        prof = [(0, 0), (rr * 0.55, 0), (rr, hh), (rr * 0.82, hh), (rr * 0.45, 0.02), (0, 0.02)]
        c = bm_lathe(prof, sides=3, close_top=False)
        paint_part(c, "kil", lo=2, hi=0, seed=6420 + k)
        kr.append((c, Matrix.Translation((k * 0.13, (k % 2) * 0.06, 0)) @ Matrix.Rotation(k * 0.7, 4, "Z")))
    _obj(bm_merge(*kr), "Kroze_Seti", loc=(HEARTH_X - 0.55, HEARTH_Y - 0.40, HEARTH_H + 0.13))
    # kalıp (taş blok, yarım silindir oyuklar)
    kl = bm_box(0.22, 0.12, 0.06, bevel=0.012)
    paint_part(kl, "kaya", lo=1, hi=0, seed=6430)
    _obj(kl, "Kalip", loc=(HEARTH_X + 0.55, HEARTH_Y - 0.38, HEARTH_H + 0.13), rot=(0, 0, 8))

def build_storage():
    # küpler + çuvallar (GD köşe), su teknesi + kova (GB köşe — kapı salınımı dışında)
    items = []
    for k, (x, y, cont) in enumerate(((3.58, -2.88, "tuz"), (3.05, -2.97, "kirec"), (3.62, -2.52, "sirke"))):
        kind = "kup" if k < 2 else "testi"
        bm = jar_bm(kind, cont, seed=6500 + k)
        _obj(bm, "SM_Kup_%d" % (k + 1), loc=(x, y, 0))
    for k, (x, y) in enumerate(((2.45, -2.97), (2.05, -2.85))):
        sack = bm_lathe([(0, 0), (0.18, 0.0), (0.22, 0.12), (0.2, 0.32), (0.13, 0.42), (0.07, 0.46), (0.09, 0.52), (0, 0.52)], sides=8)
        jitter(sack, 0.02, seed=6510 + k)
        paint_part(sack, "kil", lo=1, hi=0, seed=6512 + k, bias=0.2)
        tie = bm_lathe([(0.075, 0.43), (0.085, 0.45), (0.075, 0.47)], sides=8, close_top=False, close_bottom=False)
        paint_part(tie, "ahsap", lo=2, hi=1, seed=6514)
        _obj(bm_merge((sack, None), (tie, None)), "SM_Cuval_%d" % (k + 1), loc=(x, y, 0), rot=(RP.uniform(-4, 4), RP.uniform(-4, 4), RP.uniform(0, 90)))
    # su teknesi
    tr = []
    for (w, d, h, x, y, z) in ((1.0, 0.45, 0.06, 0, 0, 0.0), (1.0, 0.06, 0.40, 0, -0.20, 0.06), (1.0, 0.06, 0.40, 0, 0.20, 0.06),
                               (0.06, 0.40, 0.40, -0.47, 0, 0.06), (0.06, 0.40, 0.40, 0.47, 0, 0.06)):
        p = bm_box(w, d, h, bevel=0.012)
        jitter(p, 0.006, seed=int(x * 100 + y * 100) + 6520)
        _wood(p, 6521, lo=2, hi=1)
        tr.append((p, Matrix.Translation((x, y, z))))
    _obj(bm_merge(*tr), "SM_Su_Teknesi", loc=(-3.45, -2.75, 0), rot=(0, 0, 90))
    water = bmesh.new()
    vs = [water.verts.new(v) for v in ((-0.44, -0.17, 0), (0.44, -0.17, 0), (0.44, 0.17, 0), (-0.44, 0.17, 0))]
    water.faces.new(vs)
    paint_flat(water, pal("su", 1))
    _obj(water, "Su_Teknesi_Su", loc=(-3.45, -2.75, 0.38), rot=(0, 0, 90), mat=material_named("M_Su", "#35BDEB"), sharp=0)
    kv = bm_lathe([(0, 0), (0.13, 0), (0.15, 0.30), (0.16, 0.32), (0.14, 0.32), (0.12, 0.03), (0, 0.03)], sides=10)
    _wood(kv, 6530, lo=2, hi=0)
    for zz in (0.06, 0.24):
        hb = bm_lathe([(0.135 + zz * 0.06, zz), (0.145 + zz * 0.06, zz + 0.015), (0.135 + zz * 0.06, zz + 0.03)], sides=10, close_top=False, close_bottom=False)
        iron(hb)
        kv = bm_merge((kv, None), (hb, None))
    _obj(kv, "Kova", loc=(-3.0, -2.95, 0))

def build_core():
    clear_collection(ROOT_COL)
    build_hearth()
    build_fire()
    build_cauldron()
    for t in ("Giren", "Cikan"):
        build_scale(t)
    build_shelves()
    build_mechanic_jar()
    build_chalkboard()
    build_desk()
    build_athanor()
    build_alembic_bench()
    build_tools()
    build_storage()
