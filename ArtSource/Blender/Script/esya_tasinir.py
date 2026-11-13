# Bilgi Adası — taşınabilir eşyalar (malzeme kapları + el aletleri)
# Her eşya AYRI obje: rafta durur, envantere girer (6 göz), elde 3B görünür, yere atılır, kazana konur.
# Referans: ArtSource/Referans/esya.md · Kurallar: 09-tema-ve-model-kurallari.md
# Önce ba_lib.py çalıştırılmış olmalı.
#
# Eşya sözleşmesi (Unity tarafı: 08-mustafa-entegrasyon.md §7):
#   <Ad>                      boş üst obje, pivot = taban ortası (raf/yere konunca dik durur)
#   ├── <Ad>_Model            mesh (tek parça, M_Stilize; cam/sıvı ayrı malzeme yok)
#   ├── Tutma_Noktasi         boş: elde tutulduğunda el/kamera çapasına hizalanacak nokta
#   └── Etiket_Yuzu           boş: formül etiketi (TMP) buraya; ileri ekseni etiketin baktığı yön
# Hiyerarşi tek kademe (09 §7).

import math
import random
import bmesh
import bpy
from mathutils import Matrix, Vector

RT = random.Random(9090)
ITEM_COL = "Esya_Tasinir"
CATALOG_ORIGIN = Vector((14.0, -4.0, 0.0))     # atölyenin dışında, inceleme için dizilir

# ------------------------------------------------------------------ element renkleri (oyunun kodu, palete eşlenmiş)
# 05-icerik-prototip.md: H beyaz, O kırmızı, C koyu gri, N mavi, Ca açık yeşil
ELEM = {
    "H": lambda: pal("siva", 0),
    "O": lambda: pal("cati", 1),
    "C": lambda: mix(demir(0.45), pal("ahsap", 2), 0.35),     # nötr koyu gri; N mavisinden ayrışır
    "N": lambda: pal("gok", 2),
    "Ca": lambda: pal("yaprak", 0),
}
SPECIES = {   # formül → bileşim (05 §1)
    "H2": {"H": 2}, "O2": {"O": 2}, "H2O": {"H": 2, "O": 1}, "CH4": {"C": 1, "H": 4}, "CO2": {"C": 1, "O": 2},
    "C3H8": {"C": 3, "H": 8}, "CaCO3": {"Ca": 1, "C": 1, "O": 3}, "CaO": {"Ca": 1, "O": 1},
    "CaOH2": {"Ca": 1, "O": 2, "H": 2}, "N2": {"N": 2}, "NH3": {"N": 1, "H": 3}, "CO": {"C": 1, "O": 1},
}

def atom_glyph(e, r):
    """Renk + ŞEKİL kodu (renk körlüğü: renk tek başına bilgi taşımaz — XAG 103).
    H küre · O halka · C küp · N piramit · Ca altıgen disk. Taban z=0."""
    if e == "H":
        g = bmesh.new()
        bmesh.ops.create_icosphere(g, subdivisions=1, radius=r)
        bmesh.ops.scale(g, vec=(1, 1, 0.7), verts=g.verts)
        bmesh.ops.translate(g, vec=(0, 0, r * 0.35), verts=g.verts)
    elif e == "O":
        g = bm_lathe([(r * 0.55, 0), (r * 1.05, 0.0), (r * 1.1, r * 0.45), (r * 0.6, r * 0.5)], sides=8, close_top=False, close_bottom=False)
        # iç duvarı kapat (halka)
        g2 = bm_lathe([(r * 0.6, r * 0.5), (r * 0.5, r * 0.25), (r * 0.55, 0)], sides=8, close_top=False, close_bottom=False)
        g = bm_merge((g, None), (g2, None))
    elif e == "C":
        g = bm_box(r * 1.5, r * 1.5, r * 1.1, bevel=r * 0.15)
    elif e == "N":
        g = bm_cyl(r * 1.15, 0.0005, r * 1.4, sides=3)
    else:   # Ca
        g = bm_cyl(r * 1.1, r * 1.1, r * 0.55, sides=6, bevel=r * 0.12)
    return g

def _ic():
    return collection(ITEM_COL)

def wax():
    """Mühür balmumu: kehribar (oksijenin kırmızısından ayrışsın)."""
    return pal("altin", 2)

# ------------------------------------------------------------------ ortak parçalar
def seal_medallion(radius, comp, seed=0):
    """Balmumu mühür diski + üstünde atom noktaları (her atom bir kabartma, element renginde).
    comp: {"H":2,"O":1}. Noktalar halka(lar) hâlinde dizilir. Taban z=0, üst yüz +Z."""
    r = random.Random(seed)
    parts = []
    disc = bm_lathe([(0, 0), (radius * 0.92, 0), (radius, radius * 0.18), (radius * 0.94, radius * 0.32), (radius * 0.6, radius * 0.36),
                     (0, radius * 0.34)], sides=10)
    jitter(disc, radius * 0.03, seed=seed)
    paint_by(disc, lambda f, l: mix(wax(), pal("altin", 1), 0.25 if f.normal.z > 0.6 else 0.0))
    parts.append((disc, None))
    atoms = []
    for e in ("C", "Ca", "N", "O", "H"):          # ağır olandan hafife: okunur sıra
        atoms += [e] * comp.get(e, 0)
    n = len(atoms)
    top = radius * 0.34
    if n <= 7:
        rings = [(atoms, radius * (0.0 if n == 1 else 0.52))]
    else:
        k = min(7, n - 3)
        rings = [(atoms[:n - k], radius * 0.30), (atoms[n - k:], radius * 0.66)]
    for ring_atoms, rr in rings:
        m = len(ring_atoms)
        dot_r = radius * (0.24 if n <= 3 else 0.17 if n <= 7 else 0.12)
        for i, e in enumerate(ring_atoms):
            a = 2 * math.pi * i / max(1, m) + 0.3
            dot = atom_glyph(e, dot_r)
            paint_flat(dot, ELEM[e]())
            p = (0.0, 0.0) if rr == 0 else (math.cos(a) * rr, math.sin(a) * rr)
            parts.append((dot, Matrix.Translation((p[0], p[1], top))))
    return bm_merge(*parts)

def cord_wrap(r_neck, z, turns=2, thick=0.006, seed=0):
    """Boyna sarılı ip (iki tur) + sarkan iki uç."""
    parts = []
    for t in range(turns):
        ring = bm_lathe([(r_neck + thick, z + t * thick * 1.8 - thick), (r_neck + thick * 2, z + t * thick * 1.8),
                         (r_neck + thick, z + t * thick * 1.8 + thick), (r_neck, z + t * thick * 1.8)], sides=8,
                        close_top=False, close_bottom=False)
        paint_flat(ring, mix(pal("kil", 0), pal("altin", 2), 0.25))
        parts.append((ring, None))
    knot = bmesh.new()                       # düğüm (yanda küçük yumru)
    bmesh.ops.create_icosphere(knot, subdivisions=1, radius=thick * 1.8)
    paint_flat(knot, mix(pal("kil", 0), pal("altin", 2), 0.35))
    parts.append((knot, Matrix.Translation((r_neck + thick * 2.2, 0, z + thick))))
    return bm_merge(*parts)

def insert_rings(prof, zs):
    """Profil üzerine verilen yüksekliklerde ara nokta ekle (renk bantları keskin dursun)."""
    out = [prof[0]]
    for a, b in zip(prof, prof[1:]):
        lo, hi = sorted((a[1], b[1]))
        mids = sorted([z for z in zs if lo < z < hi], reverse=(b[1] < a[1]))
        for z in mids:
            t = (z - a[1]) / (b[1] - a[1])
            out.append((a[0] + (b[0] - a[0]) * t, z))
        out.append(b)
    return out

def label_plate(w, h, curve_r):
    """Gövdeye sarılmış hafif kavisli parşömen etiket (formül yazısı Unity'de TMP ile). -Y'ye bakar."""
    secs = []
    n = 5
    for i in range(n + 1):
        a = -w / (2 * curve_r) + (w / curve_r) * i / n
        x, y = math.sin(a) * (curve_r + 0.002), -math.cos(a) * (curve_r + 0.002)
        xo, yo = math.sin(a) * (curve_r + 0.005), -math.cos(a) * (curve_r + 0.005)
        secs.append([Vector((x, y, -h / 2)), Vector((xo, yo, -h / 2)), Vector((xo, yo, h / 2)), Vector((x, y, h / 2))])
    bm = bm_sweep(secs)
    paint_by(bm, lambda f, l: mix(pal("siva", 0), pal("kil", 0), 0.25))
    return bm

# ================================================================== renkler (araştırma §1-2)
def glass():
    """İslam camı: bitki külü + demir → açık yeşilimsi (opak pastel; ayrı malzeme yok)."""
    return mix(mix(pal("su", 0), pal("yaprak", 0), 0.35), pal("siva", 0), 0.35)

def glass_dark():
    return mix(glass(), pal("yaprak", 2), 0.35)

def parchment():
    return mix(pal("siva", 0), pal("kil", 0), 0.3)

def cord_col():
    return mix(pal("kil", 0), pal("altin", 2), 0.25)

GLAZE = {   # albarello sırları
    "kobalt":   lambda: mix(pal("gok", 2), pal("dag", 2), 0.45),
    "turkuaz":  lambda: mix(pal("su", 1), pal("yaprak", 0), 0.25),
    "bal":      lambda: mix(pal("altin", 1), pal("kil", 0), 0.35),
    "yesil":    lambda: mix(pal("yaprak", 1), pal("su", 1), 0.3),
}

# ================================================================== kap yapıcıları
def chaikin(prof, it=2, keep_ends=True):
    """Profil noktalarını köşe kesmeyle yoğunlaştır: kırık yüz yerine yumuşak, tombul siluet;
    renk bantlarını taşıyacak ara halkalar oluşur."""
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

def _glass_body(prof, sides=12, ribs=0, rib_amp=0.0, liquid=None, liquid_z=0.0, soot=0.0):
    """Dönel cam gövde. ribs>0: kalıp üfleme petek/nervür (yarıçap modülasyonu).
    liquid: içerik rengi (alt kısım), liquid_z: sıvı seviyesi."""
    prof = chaikin(prof, 1)
    bm = bm_lathe(prof, sides=sides)
    if ribs:
        for v in bm.verts:
            r = math.hypot(v.co.x, v.co.y)
            if r > 1e-4:
                a = math.atan2(v.co.y, v.co.x)
                k = 1.0 + rib_amp * math.cos(ribs * a)
                v.co.x *= k
                v.co.y *= k
    top = max(p[1] for p in prof)
    def fn(f, l):
        z = l.vert.co.z
        g = mix(glass_dark(), glass(), min(1.0, z / (top * 0.7)))
        if liquid is not None and z <= liquid_z:
            g = mix(liquid, g, 0.25)
        if soot > 0 and z > top * 0.55:
            g = mix(g, demir(0.7), soot * (z - top * 0.55) / (top * 0.45))
        return g
    paint_by(bm, fn)
    return bm

def _sealed_tip(z, r=0.018):
    """Hermetik mühür: eritilip kapatılmış boyun ucu — boyundan biraz kalın, yuvarlak damla; tepede hafif is."""
    prof = chaikin([(r * 0.85, -0.004), (r * 1.15, 0.006), (r * 1.1, 0.016), (r * 0.6, 0.026), (0, 0.029)], 1)
    tip = bm_lathe(prof, sides=12, close_bottom=False)
    paint_by(tip, lambda f, l: mix(glass(), mix(pal("kil", 1), demir(0.8), 0.45), min(1.0, max(0.0, (l.vert.co.z - 0.008) / 0.02))))
    return tip, Matrix.Translation((0, 0, z))

def _lute_ring(r, z, h=0.016):
    """Lutum sapientiae: ağız/bağlantıda kurumuş çamur halkası."""
    rg = bm_lathe([(r * 0.96, z - h / 2), (r + 0.007, z - h * 0.2), (r + 0.006, z + h * 0.35), (r * 0.98, z + h / 2)], sides=10,
                  close_top=False, close_bottom=False)
    jitter(rg, 0.0025, seed=int(z * 997))
    paint_by(rg, lambda f, l: mix(pal("kil", 0), pal("altin", 2), 0.15))
    return rg

def _parchment_cap(r, z, seed=0):
    """Ağzı örten parşömen kapak: üstte düz, kenarı aşağı sarkan dalgalı etek; altında ip."""
    rr = random.Random(seed)
    secs = []
    n = 12
    prof = []
    bm = bmesh.new()
    top_c = bm.verts.new((0, 0, z + 0.006))
    rim = []
    skirt = []
    for i in range(n):
        a = 2 * math.pi * i / n
        rim.append(bm.verts.new((math.cos(a) * (r + 0.004), math.sin(a) * (r + 0.004), z + 0.004)))
        d = r + 0.012 + rr.uniform(0, 0.006)
        skirt.append(bm.verts.new((math.cos(a) * d, math.sin(a) * d, z - 0.022 - rr.uniform(0, 0.012))))
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((top_c, rim[i], rim[j]))
        bm.faces.new((rim[i], skirt[i], skirt[j], rim[j]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    paint_by(bm, lambda f, l: parchment() if f.normal.z > 0.3 else mix(parchment(), pal("kil", 1), 0.3))
    return bm

def _medallion_on(comp, radius, mat, seed=0):
    """Mühür madalyonu: comp None ise damgalı düz mühür (dönem maddeleri)."""
    if comp:
        return (seal_medallion(radius, comp, seed=seed), mat)
    d = bm_lathe([(0, 0), (radius * 0.92, 0), (radius, radius * 0.18), (radius * 0.94, radius * 0.32), (radius * 0.55, radius * 0.36),
                  (radius * 0.35, radius * 0.5), (0, radius * 0.5)], sides=10)
    jitter(d, radius * 0.03, seed=seed)
    paint_by(d, lambda f, l: mix(wax(), pal("altin", 1), 0.25 if f.normal.z > 0.6 else 0.0))
    return (d, mat)

def radius_at(prof, z):
    """Profilin z yüksekliğindeki dış yarıçapı ve yüzey eğimi (rad) — mühür/etiketi yüzeye oturtmak için."""
    best = None
    for a, b in zip(prof, prof[1:]):
        if (a[1] - z) * (b[1] - z) <= 0 and a[1] != b[1]:
            t = (z - a[1]) / (b[1] - a[1])
            r = a[0] + (b[0] - a[0]) * t
            slope = math.atan2(b[0] - a[0], b[1] - a[1])       # +: yukarı doğru genişliyor
            if best is None or r > best[0]:
                best = (r, slope)
    return best or (0.05, 0.0)

def on_surface(prof, z, gap=0.003, scale=1.0):
    """-Y yüzeyinde, yüzeye teğet oturan yerleşim matrisi (yerel +Z yüzeyden dışa)."""
    r, slope = radius_at(prof, z)
    return Matrix.Translation((0, -(r * scale + gap), z)) @ Matrix.Rotation(math.pi / 2 + slope, 4, "X")

def _front(z, y, tilt=0.0):
    """Ön yüze (-Y) bakan yerleşim: mühür/etiketin +Z'si -Y'ye döner."""
    return Matrix.Translation((0, y, z)) @ Matrix.Rotation(math.pi / 2 + tilt, 4, "X")

def ruh_sisesi(form, comp, seed, soot=0.0):
    """Gaz kabı: kalın küresel cam, hermetik mühürlü boyun, boyunda ip, ipte mühür, gövdede etiket.
    form: armut | kure_nervur | sogan | uzun_boyun | kure"""
    if form == "armut":
        prof = [(0, 0), (0.035, 0), (0.042, 0.006), (0.072, 0.045), (0.082, 0.095), (0.068, 0.15), (0.038, 0.195), (0.017, 0.225),
                (0.016, 0.262), (0, 0.262)]
        neck_r, neck_top, body_r, body_z = 0.016, 0.262, 0.082, 0.095
    elif form == "sogan":
        prof = [(0, 0), (0.045, 0), (0.05, 0.006), (0.088, 0.035), (0.098, 0.07), (0.082, 0.11), (0.04, 0.135), (0.019, 0.15),
                (0.018, 0.19), (0, 0.19)]
        neck_r, neck_top, body_r, body_z = 0.018, 0.19, 0.098, 0.07
    elif form == "uzun_boyun":
        prof = [(0, 0), (0.035, 0), (0.04, 0.006), (0.068, 0.03), (0.078, 0.075), (0.066, 0.12), (0.03, 0.148), (0.013, 0.16),
                (0.012, 0.29), (0.016, 0.296), (0, 0.296)]
        neck_r, neck_top, body_r, body_z = 0.013, 0.296, 0.078, 0.075
    else:   # kure / kure_nervur
        prof = [(0, 0), (0.038, 0), (0.043, 0.006), (0.075, 0.035), (0.086, 0.085), (0.075, 0.135), (0.036, 0.165), (0.017, 0.178),
                (0.016, 0.225), (0, 0.225)]
        neck_r, neck_top, body_r, body_z = 0.016, 0.225, 0.086, 0.085
    ribs = 12 if form == "kure_nervur" else 0
    amp = 0.07 if ribs else 0.0
    parts = [(_glass_body(prof[:-1], sides=18 if ribs else 16, ribs=ribs, rib_amp=amp, soot=soot), None)]
    parts.append(_sealed_tip(neck_top - 0.012, r=neck_r))
    body_r *= (1 + amp)
    cz = neck_top - 0.045
    parts.append((cord_wrap(neck_r, cz, seed=seed), None))
    # ipten sarkan mühür (ön yüzde, omuz hizası)
    # mühür: omuza yapıştırılmış balmumu (ip düğümünün altında)
    sp = chaikin(prof[:-1], 1)
    mz = body_z + (neck_top - body_z) * 0.30          # omuz
    parts.append(_medallion_on(comp, 0.032, on_surface(sp, mz, gap=0.002, scale=1 + amp), seed=seed))
    # gövde etiketi
    parts.append((label_plate(0.075, 0.042, body_r * 0.98), Matrix.Translation((0, 0, body_z * 0.75))))
    return bm_merge(*parts), dict(grip=body_z, label_z=body_z * 0.75, label_r=body_r * 0.98, mouth=neck_top + 0.015)

def mesane_kesesi(comp, seed, big=False):
    """Kurutulmuş mesane kesesi (18. yy gaz kesesi): şişkin, yarı saydam sarımsı; boyun iple bağlı, lute'lu tahta tıpa."""
    r = random.Random(seed)
    s = 1.18 if big else 1.0
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=12, v_segments=8, radius=0.085 * s)
    bmesh.ops.scale(bm, vec=(1.0, 0.92, 0.82), verts=bm.verts)
    for v in bm.verts:
        v.co.z += 0.07 * s
        if v.co.z < 0.012:
            v.co.z = 0.012 + (v.co.z - 0.012) * 0.25     # düz taban: dik durur
        # boyna doğru daralma
        if v.co.z > 0.11 * s:
            k = 1 - (v.co.z - 0.11 * s) / (0.05 * s) * 0.55
            v.co.x *= max(0.35, k)
            v.co.y *= max(0.35, k)
    jitter(bm, 0.006 * s, seed=seed)
    base = mix(pal("kil", 0), pal("altin", 0), 0.25)
    paint_by(bm, lambda f, l: mix(mix(pal("kil", 1), pal("altin", 2), 0.3), base, min(1.0, l.vert.co.z / (0.13 * s)))
             if (int(l.vert.co.x * 90) + int(l.vert.co.z * 70)) % 9 else mix(base, pal("cati", 2), 0.25))   # ince damar lekeleri
    parts = [(bm, None)]
    top = max(v.co.z for v in bm.verts)
    nr = 0.02 * s
    neck = bm_lathe([(nr * 1.2, top - 0.012), (nr * 0.9, top + 0.01), (nr, top + 0.03), (nr * 1.3, top + 0.04), (0, top + 0.04)], sides=8)
    jitter(neck, 0.002, seed=seed + 1)
    paint_by(neck, lambda f, l: base)
    parts.append((neck, None))
    parts.append((cord_wrap(nr * 0.95, top + 0.012, turns=3, seed=seed), None))
    parts.append((_lute_ring(nr * 1.2, top + 0.035), None))
    plug = bm_cyl(0.011, 0.009, 0.05, sides=6)
    _wood(plug, seed + 2, lo=1, hi=0)
    parts.append((plug, Matrix.Translation((0, 0, top + 0.03))))
    parts.append(_medallion_on(comp, 0.032, _front(top - 0.03, -0.068 * s, tilt=-0.5), seed=seed))
    parts.append((label_plate(0.07, 0.04, 0.085 * s), Matrix.Translation((0, 0, 0.05 * s))))
    return bm_merge(*parts), dict(grip=0.08 * s, label_z=0.05 * s, label_r=0.085 * s, mouth=top + 0.08)

def su_sisesi(comp, seed):
    """H₂O: kalıp üfleme petek desenli küresel gövde, uzun silindir boyun, katlanmış dudak; tıpa + lute + parşömen + ip + mühür."""
    prof = [(0, 0.006), (0.03, 0.0), (0.04, 0.0), (0.045, 0.008), (0.074, 0.04), (0.08, 0.075), (0.07, 0.115), (0.032, 0.14),
            (0.019, 0.155), (0.017, 0.25), (0.024, 0.258), (0.02, 0.266), (0, 0.262)]
    parts = [(_glass_body(prof, sides=14, ribs=7, rib_amp=0.06, liquid=pal("su", 1), liquid_z=0.095), None)]
    parts.append((_lute_ring(0.021, 0.262, h=0.014), None))
    parts.append((_parchment_cap(0.022, 0.27, seed=seed), None))
    parts.append((cord_wrap(0.019, 0.248, seed=seed), None))
    parts.append(_medallion_on(comp, 0.033, Matrix.Translation((0, 0, 0.274)), seed=seed))    # mühür ağzın üstünde
    parts.append((label_plate(0.07, 0.038, 0.08 * 1.06 + 0.002), Matrix.Translation((0, 0, 0.07))))   # nervürün üstünde
    return bm_merge(*parts), dict(grip=0.075, label_z=0.07, label_r=0.087, mouth=0.28)

def kucuk_sise(comp, liquid, seed):
    """NH₃ (nişadır/geyik boynuzu ruhu) gibi keskin sıvılar: basık gövde, kısa boyun, sıkı tıpa + lute + mühür."""
    prof = [(0, 0.004), (0.04, 0.0), (0.05, 0.006), (0.066, 0.03), (0.066, 0.075), (0.05, 0.1), (0.022, 0.115), (0.02, 0.15),
            (0.026, 0.156), (0, 0.156)]
    parts = [(_glass_body(prof, sides=14, liquid=liquid, liquid_z=0.07), None)]
    parts.append((_lute_ring(0.024, 0.154, h=0.016), None))
    stop = bm_lathe([(0, 0), (0.018, 0), (0.022, 0.02), (0.026, 0.032), (0, 0.034)], sides=8)
    _wood(stop, seed, lo=1, hi=0)
    parts.append((stop, Matrix.Translation((0, 0, 0.152))))
    parts.append((cord_wrap(0.021, 0.14, seed=seed), None))
    parts.append(_medallion_on(comp, 0.032, Matrix.Translation((0, 0, 0.186)), seed=seed))
    parts.append((label_plate(0.07, 0.04, 0.068), Matrix.Translation((0, 0, 0.05))))
    return bm_merge(*parts), dict(grip=0.06, label_z=0.05, label_r=0.068, mouth=0.19)

def albarello(comp, glaze, seed, h=0.20, r=0.072):
    """İslam kökenli ilaç küpü: belli silindir gövde, kısa boyun, dışa açılan dudak; parşömen kapak + ip + düğümde mühür.
    Sır: zemin + koyu boya bantları (kobalt/siyah boya sır altında)."""
    prof = [(0, 0.004), (r * 0.82, 0.0), (r * 0.86, 0.012), (r, 0.03), (r * 0.9, h * 0.5), (r, h * 0.78), (r * 0.82, h * 0.86),
            (r * 0.74, h * 0.9), (r * 0.76, h * 0.95), (r * 0.86, h), (r * 0.8, h + 0.004), (0, h - 0.004)]
    bands = [h * f for f in (0.165, 0.18, 0.21, 0.225, 0.705, 0.72, 0.75, 0.765)]
    bm = bm_lathe(insert_rings(chaikin(prof[:-1], 1), bands) + [prof[-1]], sides=16)
    gz = GLAZE[glaze]()
    dark = mix(pal("ahsap", 2), demir(0.6), 0.5)
    def fn(f, l):
        z = l.vert.co.z / h
        if z < 0.06:
            return mix(pal("kil", 1), pal("kil", 0), 0.4)        # sırsız dip
        if 0.18 < z < 0.21 or 0.72 < z < 0.75:
            return dark                                           # boya bantları
        if 0.3 < z < 0.62:                                        # orta kuşakta dikey kaligrafi-benzeri leke ritmi
            a = math.atan2(l.vert.co.y, l.vert.co.x)
            if math.sin(a * 7) > 0.55:
                return mix(gz, dark, 0.55)
        return mix(gz, pal("siva", 0), 0.15 * z)
    paint_by(bm, fn)
    parts = [(bm, None)]
    parts.append((_parchment_cap(r * 0.86, h + 0.004, seed=seed), None))
    parts.append((cord_wrap(r * 0.76, h * 0.93, seed=seed), None))
    parts.append(_medallion_on(comp, 0.036, Matrix.Translation((0, 0, h + 0.01)), seed=seed))
    parts.append((label_plate(0.08, 0.045, r * 0.93), Matrix.Translation((0, 0, h * 0.46))))
    return bm_merge(*parts), dict(grip=h * 0.5, label_z=h * 0.46, label_r=r * 0.93, mouth=h + 0.01)

def tas_kesesi(comp, seed):
    """CaCO₃ kireç taşı: ağzı bağlı keten kese, içteki taşlar torbayı yumrulu yapar; bağın üstünde örnek taş + mühür."""
    r = random.Random(seed)
    bm = bm_lathe([(0, 0), (0.06, 0.0), (0.08, 0.025), (0.085, 0.07), (0.07, 0.11), (0.035, 0.135), (0.024, 0.15), (0.04, 0.17),
                   (0.03, 0.185), (0, 0.18)], sides=12)
    for v in bm.verts:                          # içteki taş yumruları
        a = math.atan2(v.co.y, v.co.x)
        if 0.02 < v.co.z < 0.12:
            k = 1 + 0.08 * math.sin(a * 3 + v.co.z * 40) * math.sin(v.co.z * 60)
            v.co.x *= k
            v.co.y *= k
    jitter(bm, 0.004, seed=seed)
    linen = mix(pal("kil", 0), pal("siva", 0), 0.4)
    paint_by(bm, lambda f, l: mix(mix(pal("kil", 1), linen, 0.5), linen, min(1, l.vert.co.z / 0.12)))
    parts = [(bm, None)]
    parts.append((cord_wrap(0.026, 0.148, turns=3, seed=seed), None))
    stone = bmesh.new()
    bmesh.ops.create_icosphere(stone, subdivisions=1, radius=0.022)
    bmesh.ops.scale(stone, vec=(1.2, 0.9, 0.75), verts=stone.verts)
    jitter(stone, 0.004, seed=seed + 1)
    paint_flat(stone, mix(pal("kaya", 0), pal("kil", 0), 0.45))     # kirli beyaz, gri-bej
    parts.append((stone, Matrix.Translation((0.02, 0.0, 0.192))))
    parts.append(_medallion_on(comp, 0.027, _front(0.12, -0.072, tilt=-0.4), seed=seed))
    parts.append((label_plate(0.07, 0.035, 0.084), Matrix.Translation((0, 0, 0.055))))
    return bm_merge(*parts), dict(grip=0.07, label_z=0.055, label_r=0.084, mouth=0.19)

def finish_item(name, bm, grip_z, label_z=None, label_r=None, loc=(0, 0, 0), mouth_z=None):
    """Eşya objesi + boş noktalar. Döner: üst boş obje."""
    root = bpy.data.objects.get(name)
    if root is None:
        root = empty(name, _ic(), loc=loc, kind="PLAIN_AXES", size=0.08)
    me_ob = to_object(bm, name + "_Model", _ic(), sharp_angle=45)
    me_ob.parent = root
    me_ob.matrix_parent_inverse = Matrix.Identity(4)
    me_ob.location = (0, 0, 0)
    g = empty("Tutma_Noktasi", _ic(), loc=(0, 0, grip_z), kind="SPHERE", size=0.02)
    g.name = name + "_Tutma_Noktasi"
    g.parent = root
    g.matrix_parent_inverse = Matrix.Identity(4)
    g.location = (0, 0, grip_z)
    if label_z is not None:
        lb = empty(name + "_Etiket_Yuzu", _ic(), kind="SINGLE_ARROW", size=0.05)
        lb.parent = root
        lb.matrix_parent_inverse = Matrix.Identity(4)
        lb.location = (0, -(label_r or 0.05) - 0.006, label_z)
        lb.rotation_euler = (math.radians(90), 0, 0)    # ok -Y'ye (etiketin baktığı yön)
    if mouth_z is not None:
        mo = empty(name + "_Agiz_Noktasi", _ic(), kind="CONE", size=0.02)
        mo.parent = root
        mo.matrix_parent_inverse = Matrix.Identity(4)
        mo.location = (0, 0, mouth_z)
    return root

# ================================================================== dönem maddeleri (oyun dışı da alınabilir)
def civa_kabi(seed):
    """Sfero-konik kap (Fustat tipi, 'cıva şişesi'): küresel üst, sivri dip, çok kalın duvar, küçük ağız,
    damga dekor. Sivri dip dik durmaz → aynı eşyanın parçası olan ahşap halka altlıkta durur."""
    prof = [(0, 0.0), (0.012, 0.004), (0.035, 0.03), (0.058, 0.07), (0.066, 0.1), (0.06, 0.13), (0.04, 0.15),
            (0.018, 0.158), (0.016, 0.168), (0.022, 0.172), (0.013, 0.176), (0, 0.172)]
    bm = bm_lathe(prof, sides=16)
    stamp = mix(pal("kil", 1), pal("kil", 2), 0.5)
    def fn(f, l):
        z = l.vert.co.z
        a = math.atan2(l.vert.co.y, l.vert.co.x)
        c = mix(pal("kil", 1), mix(pal("kil", 0), pal("kaya", 1), 0.4), min(1, z / 0.15))
        if 0.105 < z < 0.125 and math.sin(a * 8) > 0.3:      # damga halkası (çam kozalağı ritmi)
            return stamp
        return c
    paint_by(bm, fn)
    parts = [(bm, Matrix.Translation((0, 0, 0.03)))]
    ring = bm_lathe([(0.03, 0.0), (0.05, 0.0), (0.052, 0.025), (0.044, 0.035), (0.03, 0.03)], sides=14)
    _wood(ring, seed, lo=2, hi=1)
    parts.append((ring, None))
    plug = bm_lathe([(0, 0), (0.014, 0), (0.018, 0.012), (0, 0.016)], sides=8)
    paint_flat(plug, wax())
    parts.append((plug, Matrix.Translation((0, 0, 0.203))))
    parts.append((label_plate(0.06, 0.03, 0.064), Matrix.Translation((0, 0, 0.13))))
    return bm_merge(*parts), dict(grip=0.1, label_z=0.13, label_r=0.064, mouth=0.21)

def cam_kavanoz(content, seed, h=0.14, r=0.07):
    """Geniş ağızlı cam kavanoz: içerik (kristal/toz) alt yarıda görünür; parşömen kapak + ip + düz mühür."""
    prof = [(0, 0.004), (r * 0.85, 0), (r, 0.012), (r * 1.02, h * 0.82), (r * 0.86, h * 0.9), (r * 0.8, h * 0.94), (r * 0.86, h),
            (r * 0.8, h + 0.004), (0, h)]
    parts = [(_glass_body(prof, sides=16, liquid=content, liquid_z=h * 0.55), None)]
    rr = random.Random(seed)
    for k in range(5):     # üst yüzeyde kristal uçları
        cr = bm_cyl(0.012, 0.0, rr.uniform(0.02, 0.035), sides=4)
        paint_flat(cr, mix(content, pal("siva", 0), 0.25))
        parts.append((cr, Matrix.Translation((rr.uniform(-r * 0.5, r * 0.5), rr.uniform(-r * 0.5, r * 0.5), h * 0.52))
                      @ Matrix.Rotation(rr.uniform(-0.4, 0.4), 4, "X")))
    parts.append((_parchment_cap(r * 0.86, h + 0.004, seed=seed), None))
    parts.append((cord_wrap(r * 0.8, h * 0.93, seed=seed), None))
    parts.append(_medallion_on(None, 0.026, Matrix.Translation((0, 0, h + 0.01)), seed=seed))
    parts.append((label_plate(0.07, 0.035, r * 1.01), Matrix.Translation((0, 0, h * 0.68))))
    return bm_merge(*parts), dict(grip=h * 0.5, label_z=h * 0.68, label_r=r * 1.01, mouth=h + 0.01)

def kalin_sise(liquid, seed):
    """Asitler (kezzap, kral suyu, zaç yağı): kare kesitli, kalın duvarlı cam (molar şişesi ailesi, büyütülmüş);
    sıkı tıpa + lute + hermetik balmumu; ince kısa boyun."""
    prof = [(0, 0.0), (0.05, 0.0), (0.056, 0.008), (0.056, 0.13), (0.046, 0.15), (0.02, 0.165), (0.018, 0.195),
            (0.024, 0.2), (0, 0.2)]
    bm = _glass_body(prof, sides=4, liquid=liquid, liquid_z=0.105)
    transform_bm(bm, Matrix.Rotation(math.pi / 4, 4, "Z"))
    parts = [(bm, None)]
    for k in range(4):   # azı dişi ayakları (kısa, sivri)
        a = math.pi / 4 + k * math.pi / 2
        ft = bm_cyl(0.012, 0.004, 0.012, sides=5)
        transform_bm(ft, Matrix.Rotation(math.pi, 4, "X"))
        paint_flat(ft, glass_dark())
        parts.append((ft, Matrix.Translation((math.cos(a) * 0.045, math.sin(a) * 0.045, 0.012))))
    parts.append((_lute_ring(0.021, 0.198, h=0.014), None))
    cap = bm_lathe([(0, 0), (0.022, 0), (0.026, 0.012), (0.018, 0.026), (0, 0.03)], sides=10)
    jitter(cap, 0.002, seed=seed)
    paint_flat(cap, wax())
    parts.append((cap, Matrix.Translation((0, 0, 0.2))))
    lab = bm_box(0.07, 0.004, 0.05, bevel=0.001)
    paint_flat(lab, parchment())
    parts.append((lab, Matrix.Translation((0, -0.058, 0.05))))
    return bm_merge(*parts), dict(grip=0.08, label_z=0.075, label_r=0.056, mouth=0.23)

def qumqum(liquid, seed):
    """Gül suyu serpme şişesi: basık yuvarlak gövde, gövdenin ~1,3 katı ince uzun boyun, delik gibi ağız."""
    prof = [(0, 0.004), (0.03, 0.0), (0.045, 0.006), (0.072, 0.03), (0.075, 0.06), (0.06, 0.085), (0.02, 0.1), (0.011, 0.11),
            (0.008, 0.2), (0.013, 0.24), (0.009, 0.25), (0, 0.25)]
    parts = [(_glass_body(prof, sides=16, ribs=10, rib_amp=0.05, liquid=liquid, liquid_z=0.05), None)]
    for z in (0.14, 0.18):   # boyunda iplik sarma dekor
        rg = bm_lathe([(0.009, z), (0.012, z + 0.004), (0.009, z + 0.008)], sides=10, close_top=False, close_bottom=False)
        paint_flat(rg, mix(pal("gok", 1), glass(), 0.3))
        parts.append((rg, None))
    parts.append((label_plate(0.06, 0.03, 0.074), Matrix.Translation((0, 0, 0.045))))
    return bm_merge(*parts), dict(grip=0.05, label_z=0.045, label_r=0.074, mouth=0.25)

def testi_kulplu(seed, glaze="yesil"):
    """Sirke testisi: toprak gövde, ağız ve omuzda sır akıntısı, tek kulp."""
    prof = [(0, 0), (0.05, 0), (0.07, 0.04), (0.085, 0.1), (0.075, 0.16), (0.04, 0.2), (0.032, 0.235), (0.04, 0.25), (0.034, 0.255), (0, 0.25)]
    bm = bm_lathe(prof, sides=16)
    gz = GLAZE[glaze]()
    paint_by(bm, lambda f, l: mix(pal("kil", 1), pal("kil", 0), min(1, l.vert.co.z / 0.15)) if l.vert.co.z < 0.12 + 0.02 * math.sin(math.atan2(l.vert.co.y, l.vert.co.x) * 3) else gz)
    parts = [(bm, None)]
    secs = []
    for k in range(7):     # kulp: omuzdan karına yay
        t = k / 6
        a = math.pi * t
        c = Vector((0.06 + 0.06 * math.sin(a), 0, 0.215 - 0.1 * t))
        secs.append([c + Vector((-0.008, -0.014, 0)), c + Vector((0.008, -0.014, 0)), c + Vector((0.008, 0.014, 0)), c + Vector((-0.008, 0.014, 0))])
    hd = bm_sweep(secs)
    paint_flat(hd, gz)
    parts.append((hd, None))
    parts.append((_parchment_cap(0.036, 0.255, seed=seed), None))
    parts.append((cord_wrap(0.033, 0.238, seed=seed), None))
    parts.append(_medallion_on(None, 0.024, Matrix.Translation((0, 0, 0.262)), seed=seed))
    return bm_merge(*parts), dict(grip=0.12, label_z=None, label_r=None, mouth=0.26)

def kese(seed, col_k=0.4):
    """Küçük keten/deri kese (boraks, şap tozu): ağzı büzgülü, iple bağlı."""
    bm = bm_lathe([(0, 0), (0.045, 0), (0.06, 0.02), (0.062, 0.05), (0.045, 0.08), (0.02, 0.095), (0.016, 0.1), (0.03, 0.115),
                   (0.024, 0.125), (0, 0.12)], sides=12)
    jitter(bm, 0.004, seed=seed)
    paint_part(bm, "kil", lo=1, hi=0, seed=seed, bias=col_k)
    parts = [(bm, None), (cord_wrap(0.017, 0.098, turns=2, seed=seed), None)]
    parts.append(_medallion_on(None, 0.02, _front(0.07, -0.05, tilt=-0.4), seed=seed))
    return bm_merge(*parts), dict(grip=0.05, label_z=None, label_r=None, mouth=0.12)

def kutu(seed):
    """Şap kutusu: kapaklı ahşap kutu, metal mandal, kapakta yazı plakası."""
    parts = []
    body = bm_box(0.14, 0.1, 0.07, bevel=0.008)
    _wood(body, seed, lo=2, hi=1)
    parts.append((body, None))
    lid = bm_box(0.15, 0.11, 0.025, bevel=0.008)
    _wood(lid, seed + 1, lo=1, hi=0)
    parts.append((lid, Matrix.Translation((0, 0, 0.07))))
    for s in (-1, 1):
        bd = bm_box(0.012, 0.112, 0.012, bevel=0.002)
        iron(bd)
        parts.append((bd, Matrix.Translation((s * 0.05, 0, 0.088))))
    latch = bm_box(0.02, 0.006, 0.035, bevel=0.002)
    iron(latch)
    parts.append((latch, Matrix.Translation((0, -0.056, 0.055))))
    pl = bm_box(0.06, 0.03, 0.003)
    paint_flat(pl, parchment())
    parts.append((pl, Matrix.Translation((0, 0, 0.095))))
    return bm_merge(*parts), dict(grip=0.045, label_z=None, label_r=None, mouth=0.095)

def kulce(metal, seed):
    """Metal külçe: düzlem-dışbükey (plano-convex) çubuk; üstte damga çukuru. Renk: palete eşlenmiş metal tonu."""
    cols = {"altin": (pal("altin", 2), pal("altin", 0)), "gumus": (pal("kaya", 1), pal("kaya", 0)),
            "bakir": (pal("bakir", 2), pal("bakir", 0)), "kursun": (mix(pal("dag", 2), demir(0.8), 0.5), pal("dag", 1)),
            "kalay": (mix(pal("kaya", 1), pal("siva", 1), 0.5), mix(pal("kaya", 0), pal("siva", 0), 0.5)),
            "demir": (demir(0.6), demir(1.0))}
    lo, hi = cols[metal]
    secs = []
    for k in range(7):
        t = k / 6
        x = -0.06 + 0.12 * t
        bulge = math.sin(math.pi * t) ** 0.5
        w, h = 0.022 + 0.012 * bulge, 0.012 + 0.022 * bulge
        secs.append([Vector((x, -w, 0)), Vector((x, w, 0)), Vector((x, w * 0.75, h)), Vector((x, -w * 0.75, h))])
    bm = bm_sweep(secs)
    bevel_all(bm, 0.004)
    jitter(bm, 0.0015, seed=seed)
    paint_by(bm, lambda f, l: mix(lo, hi, min(1, l.vert.co.z / 0.03) * (0.8 if f.normal.z > 0.5 else 0.5)))
    st = bm_cyl(0.008, 0.008, 0.004, sides=8)
    paint_flat(st, lo)
    out = bm_merge((bm, None), (st, Matrix.Translation((0.01, 0, 0.032))))
    return out, dict(grip=0.015, label_z=None, label_r=None, mouth=None)

# ================================================================== el aletleri (araştırma §6)
def havan_tunc(seed):
    """Tunç havan (Met 444529: 11,4 × 14,6 cm): kalın kâse, dikey nervürler, iki yan kulp. Havaneli ayrı eşya."""
    prof = [(0, 0), (0.06, 0), (0.065, 0.012), (0.07, 0.09), (0.078, 0.105), (0.074, 0.114), (0.06, 0.112), (0.05, 0.04), (0, 0.035)]
    bm = bm_lathe(prof, sides=18)
    paint_part(bm, "altin", lo=2, hi=1, seed=seed, bias=0.1)
    parts = [(bm, None)]
    for k in range(8):
        a = k * math.pi / 4
        rb = bm_box(0.01, 0.012, 0.075, bevel=0.003)
        paint_part(rb, "altin", lo=2, hi=2, seed=seed + 1)
        parts.append((rb, Matrix.Translation((math.cos(a) * 0.069, math.sin(a) * 0.069, 0.015)) @ Matrix.Rotation(a, 4, "Z")))
    for s in (-1, 1):
        lug = bm_lathe([(0.012, -0.004), (0.018, 0), (0.012, 0.004), (0.008, 0)], sides=8, close_top=False, close_bottom=False)
        paint_part(lug, "altin", lo=1, hi=1, seed=seed + 2)
        parts.append((lug, Matrix.Translation((s * 0.084, 0, 0.07)) @ Matrix.Rotation(math.pi / 2, 4, "Y")))
    return bm_merge(*parts), dict(grip=0.06, label_z=None, label_r=None, mouth=0.11)

def havaneli(seed):
    """Havaneli (22-27 cm): iki ucu yumru, ortadan yumruk kavrayış. Yatık durur; pivot orta-alt."""
    prof = [(0, 0), (0.022, 0.004), (0.026, 0.02), (0.02, 0.04), (0.013, 0.06), (0.012, 0.18), (0.016, 0.21), (0.02, 0.225),
            (0.015, 0.24), (0, 0.245)]
    bm = bm_lathe(prof, sides=12)
    paint_part(bm, "altin", lo=1, hi=0, seed=seed)
    transform_bm(bm, Matrix.Translation((0, 0, 0.026)) @ Matrix.Rotation(math.pi / 2, 4, "Y") @ Matrix.Translation((0, 0, -0.122)))
    return bm, dict(grip=0.026, label_z=None, label_r=None, mouth=None)

def masa_aleti(seed):
    """Ocak maşası: iki dövme demir kol, uçta kıvrık kavrayıcı, tutma halkası. Yatık durur."""
    parts = []
    for s in (-1, 1):
        secs = []
        for k in range(6):
            t = k / 5
            x = -0.25 + 0.5 * t
            y = s * (0.008 + 0.018 * t ** 2)
            z = 0.012 + (0.02 * (t - 0.8) / 0.2 if t > 0.8 else 0)
            w = 0.006
            secs.append([Vector((x, y - w, z - w * 0.6)), Vector((x, y + w, z - w * 0.6)), Vector((x, y + w, z + w * 0.6)), Vector((x, y - w, z + w * 0.6))])
        arm = bm_sweep(secs)
        iron(arm)
        parts.append((arm, None))
    ring = bm_lathe([(0.02, -0.005), (0.026, 0), (0.02, 0.005), (0.014, 0)], sides=10, close_top=False, close_bottom=False)
    iron(ring)
    parts.append((ring, Matrix.Translation((-0.27, 0, 0.012))))
    return bm_merge(*parts), dict(grip=0.012, label_z=None, label_r=None, mouth=None)

def kepce_aleti(seed):
    parts = []
    sp = bm_box(0.3, 0.014, 0.008, bevel=0.002, origin="center")
    iron(sp)
    parts.append((sp, Matrix.Translation((0.0, 0, 0.03))))
    bowl = bm_lathe([(0, 0), (0.035, 0.005), (0.045, 0.025), (0.04, 0.03), (0.03, 0.012), (0, 0.008)], sides=14)
    iron(bowl)
    parts.append((bowl, Matrix.Translation((0.17, 0, 0.0))))
    hk = bm_lathe([(0.01, -0.003), (0.014, 0), (0.01, 0.003), (0.006, 0)], sides=8, close_top=False, close_bottom=False)
    iron(hk)
    parts.append((hk, Matrix.Translation((-0.16, 0, 0.03)) @ Matrix.Rotation(math.pi / 2, 4, "X")))
    return bm_merge(*parts), dict(grip=0.03, label_z=None, label_r=None, mouth=None)

def huni_aleti(seed):
    bm = bm_lathe([(0.008, 0.0), (0.011, 0.06), (0.07, 0.13), (0.075, 0.135), (0.066, 0.135), (0.007, 0.065), (0.005, 0.0)], sides=16,
                  close_top=False, close_bottom=False)
    paint_part(bm, "kil", lo=2, hi=0, seed=seed)
    st = bm_lathe([(0.02, 0.0), (0.035, 0.0), (0.03, 0.03), (0.015, 0.03)], sides=12)   # durabilsin diye halka ayak
    paint_part(st, "kil", lo=2, hi=1, seed=seed + 1)
    return bm_merge((bm, Matrix.Translation((0, 0, 0.0))), (st, None)), dict(grip=0.1, label_z=None, label_r=None, mouth=0.13)

def elek_aleti(seed):
    rim = bm_lathe([(0.15, 0.0), (0.16, 0.0), (0.16, 0.07), (0.15, 0.07)], sides=20)
    _wood(rim, seed, lo=1, hi=0)
    mesh = bm_cyl(0.15, 0.15, 0.003, sides=20)
    paint_by(mesh, lambda f, l: mix(pal("kil", 0), pal("siva", 1), 0.5) if (int((l.vert.co.x + 1) * 60) + int((l.vert.co.y + 1) * 60)) % 2 else pal("kil", 1))
    return bm_merge((rim, None), (mesh, Matrix.Translation((0, 0, 0.012)))), dict(grip=0.035, label_z=None, label_r=None, mouth=None)

def el_terazisi(seed):
    """El terazisi: üst halkadan tutulur; kol + iki kefe + ibre. Kutusu yok; yatık durur."""
    parts = []
    arm = bm_box(0.3, 0.012, 0.012, bevel=0.003, origin="center")
    iron(arm)
    parts.append((arm, Matrix.Translation((0, 0, 0.2))))
    ring = bm_lathe([(0.016, -0.004), (0.021, 0), (0.016, 0.004), (0.011, 0)], sides=10, close_top=False, close_bottom=False)
    paint_part(ring, "altin", lo=1, hi=0, seed=seed)
    parts.append((ring, Matrix.Translation((0, 0, 0.235)) @ Matrix.Rotation(math.pi / 2, 4, "X")))
    nd = bm_prism([(-0.005, 0.0), (0.005, 0.0), (0.0, 0.04)], -0.003, 0.003)
    paint_part(nd, "cati", lo=1, hi=0, seed=seed)
    parts.append((nd, Matrix.Translation((0, 0, 0.2))))
    for s in (-1, 1):
        pn = bm_lathe([(0, 0), (0.05, 0.004), (0.055, 0.014), (0.0, 0.006)], sides=14, close_top=False)
        paint_part(pn, "altin", lo=1, hi=0, seed=seed + 1)
        parts.append((pn, Matrix.Translation((s * 0.14, 0, 0.0))))
        for k in range(3):
            a = k * 2.09
            top = Vector((s * 0.14, 0, 0.2))
            bot = Vector((s * 0.14 + math.cos(a) * 0.04, math.sin(a) * 0.04, 0.012))
            d = bot - top
            rd = bm_cyl(0.0018, 0.0018, d.length, sides=3)
            iron(rd)
            q = Vector((0, 0, 1)).rotation_difference(d.normalized())
            parts.append((rd, Matrix.Translation(top) @ q.to_matrix().to_4x4()))
    return bm_merge(*parts), dict(grip=0.235, label_z=None, label_r=None, mouth=None)

def kroze_aleti(seed, hh=0.11, rr=0.065):
    """Üç ağızlı kroze (Hessen tipi): kadeh gövde, üst duvar içe bastırılarak üç ağız."""
    bm = bm_lathe([(0, 0), (rr * 0.5, 0), (rr * 0.6, 0.01), (rr, hh), (rr * 0.86, hh), (rr * 0.5, 0.025), (0, 0.022)], sides=18)
    for v in bm.verts:
        if v.co.z > hh * 0.5:
            a = math.atan2(v.co.y, v.co.x)
            k = 1 - 0.18 * (v.co.z - hh * 0.5) / (hh * 0.5) * (0.5 + 0.5 * math.cos(3 * a))
            v.co.x *= k
            v.co.y *= k
    paint_by(bm, lambda f, l: mix(mix(pal("kil", 2), demir(0.8), 0.3), pal("kil", 0), min(1, l.vert.co.z / hh)) if f.normal.z > -0.3
             else mix(pal("kil", 2), demir(0.7), 0.5))
    return bm, dict(grip=hh * 0.5, label_z=None, label_r=None, mouth=hh)

def kupel_aleti(seed):
    bm = bm_lathe([(0, 0), (0.028, 0), (0.034, 0.022), (0.03, 0.026), (0.018, 0.018), (0, 0.016)], sides=16)
    paint_by(bm, lambda f, l: mix(pal("siva", 0), pal("kaya", 0), 0.3) if f.normal.z < 0.8 else mix(pal("siva", 1), pal("kil", 0), 0.4))
    return bm, dict(grip=0.012, label_z=None, label_r=None, mouth=0.026)

# ================================================================== katalog
def _gas(form, sp, soot=0.0):
    return lambda s: ruh_sisesi(form, SPECIES[sp], s, soot=soot)

# (ad, yapıcı, satır) — oyun türleri önce. Dönem adları: araştırma §4-5 (C₃H₈ adı kurgusal, oyunda belirtilecek)
CATALOG = [
    # oyun türleri: gazlar (renksiz → siluet + mühür + etiket ile ayrılır)
    ("Malzeme_H2", _gas("armut", "H2"), 0),                  # yanıcı hava (Cavendish 1766)
    ("Malzeme_O2", _gas("kure_nervur", "O2"), 0),            # ateş havası (Priestley 1774)
    ("Malzeme_N2", _gas("sogan", "N2"), 0),                  # boğucu hava (Rutherford 1772)
    ("Malzeme_CO2", _gas("uzun_boyun", "CO2"), 0),           # sabit hava (Black 1754-56)
    ("Malzeme_CO", _gas("kure", "CO", soot=0.6), 0),         # kömür buğusu (isli cam, uyarı)
    ("Malzeme_CH4", lambda s: mesane_kesesi(SPECIES["CH4"], s), 0),             # bataklık ruhu (Volta 1776)
    ("Malzeme_C3H8", lambda s: mesane_kesesi(SPECIES["C3H8"], s, big=True), 0),  # kaya yağı ruhu (kurgusal ad)
    # oyun türleri: sıvı ve katılar
    ("Malzeme_H2O", lambda s: su_sisesi(SPECIES["H2O"], s), 1),
    ("Malzeme_NH3", lambda s: kucuk_sise(SPECIES["NH3"], mix(pal("altin", 0), pal("siva", 0), 0.6), s), 1),   # geyik boynuzu ruhu
    ("Malzeme_CaCO3", lambda s: tas_kesesi(SPECIES["CaCO3"], s), 1),           # kireç taşı
    ("Malzeme_CaO", lambda s: albarello(SPECIES["CaO"], "kobalt", s), 1),      # diri kireç (nem çeker → kapalı küp)
    ("Malzeme_CaOH2", lambda s: albarello(SPECIES["CaOH2"], "turkuaz", s), 1), # sönmüş kireç
    # dönem maddeleri
    ("Madde_Civa", civa_kabi, 2),
    ("Madde_Kukurt", lambda s: albarello(None, "bal", s, h=0.17, r=0.065), 2),
    ("Madde_Goztasi", lambda s: cam_kavanoz(pal("su", 1), s), 2),
    ("Madde_Nisadir", lambda s: cam_kavanoz(pal("siva", 0), s), 2),
    ("Madde_Zirnik", lambda s: cam_kavanoz(pal("cati", 0), s, h=0.11, r=0.055), 2),
    ("Madde_Kezzap", lambda s: kalin_sise(mix(pal("altin", 0), pal("siva", 0), 0.6), s), 2),
    ("Madde_Kral_Suyu", lambda s: kalin_sise(pal("altin", 1), s), 2),
    ("Madde_Zac_Yagi", lambda s: kalin_sise(glass(), s), 2),
    ("Madde_Gul_Suyu", lambda s: qumqum(mix(pal("cati", 0), pal("siva", 0), 0.75), s), 2),
    ("Madde_Sirke", testi_kulplu, 2),
    ("Madde_Boraks", lambda s: kese(s, 0.5), 2),
    ("Madde_Sap", kutu, 2),
    # metaller (Râzî: cisimler)
    ("Kulce_Altin", lambda s: kulce("altin", s), 3), ("Kulce_Gumus", lambda s: kulce("gumus", s), 3),
    ("Kulce_Bakir", lambda s: kulce("bakir", s), 3), ("Kulce_Kursun", lambda s: kulce("kursun", s), 3),
    ("Kulce_Kalay", lambda s: kulce("kalay", s), 3), ("Kulce_Demir", lambda s: kulce("demir", s), 3),
    # el aletleri
    ("Alet_Havan", havan_tunc, 4), ("Alet_Havaneli", havaneli, 4), ("Alet_Ocak_Masasi", masa_aleti, 4),
    ("Alet_Kepce", kepce_aleti, 4), ("Alet_Huni", huni_aleti, 4), ("Alet_Elek", elek_aleti, 4),
    ("Alet_El_Terazisi", el_terazisi, 4), ("Alet_Kroze", kroze_aleti, 4), ("Alet_Kupel", kupel_aleti, 4),
]

def build_catalog(only=None):
    clear_collection(ITEM_COL)
    col_count = {}
    report = []
    for i, (name, fn, row) in enumerate(CATALOG):
        if only and name not in only:
            continue
        k = col_count.get(row, 0)
        col_count[row] = k + 1
        loc = CATALOG_ORIGIN + Vector((k * 0.42, row * 0.6, 0))
        bm, info = fn(9000 + i * 17)
        root = finish_item(name, bm, info["grip"], info.get("label_z"), info.get("label_r"), loc=tuple(loc),
                           mouth_z=info.get("mouth"))
        me = bpy.data.objects[name + "_Model"]
        report.append((name, tri_count([me]), tuple(round(v, 3) for v in me.dimensions)))
    return report
