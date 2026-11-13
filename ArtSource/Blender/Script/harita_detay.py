# Kasaba haritası — detay geçişi: zemin örtüsü (çimen, çiçek, çakıl, saz), ağaç çeşitliliği (kavak, çam, meyve),
# iri kaya kümeleri, uzak dağ halkası + bulutlar, köy hayatı (çarşı tezgahları, bahçeler, taş duvar, araba,
# çamaşır ipi, arı kovanları, odun yığınları).
# Referans: ArtSource/Referans/detay.md. Önce harita_yon.py (bu dosya yükler).
# Yarışma süresince yazıldı (0'dan üret). Blender 5.x.
#
#   g = {"__name__": "harita_detay"}; exec(compile(open(p, encoding="utf-8").read(), p, "exec"), g)
#   g["build_detay_modelleri"](); g["build_detay"](); g["scatter_detay"](); g["preview_instances"]()

import os
_np = os.path.join(r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\ArtSource\Blender\Script", "harita_yon.py")
exec(compile(open(_np, encoding="utf-8").read(), _np, "exec"), globals())

DETAY_COL = "Harita_Detay"

def put_detay(*a, **k):
    ob = put(*a, **k)
    for c in list(ob.users_collection):
        c.objects.unlink(ob)
    collection(DETAY_COL).objects.link(ob)
    return ob

def _up_normals(ob):
    """09 §3: ince parçaların (çimen, çiçek, saz) normali yukarı — zeminle aynı ışığı alır."""
    me = ob.data
    me.normals_split_custom_set([(0.0, 0.0, 1.0)] * len(me.loops))

# ================================================================== KÜÇÜK MODELLER
def _blade(base, h, w, lean, rnd):
    """Kıvrık çimen yaprağı: 3 nokta yükseklik, uca doğru incelir (3 üçgen)."""
    bm = bmesh.new()
    a = rnd.uniform(0, 2 * math.pi)
    side = Vector((math.cos(a), math.sin(a), 0)) * (w / 2)
    fwd = Vector((-math.sin(a), math.cos(a), 0)) * lean
    p0l, p0r = base - side, base + side
    p1 = base + fwd * 0.35 + Vector((0, 0, h * 0.55))
    p1l, p1r = p1 - side * 0.6, p1 + side * 0.6
    tip = base + fwd + Vector((0, 0, h))
    vs = [bm.verts.new(p) for p in (p0l, p0r, p1r, p1l, tip)]
    bm.faces.new((vs[0], vs[1], vs[2], vs[3]))
    bm.faces.new((vs[3], vs[2], vs[4]))
    return bm

def cimen_bm(seed, n=9, hmax=0.42):
    rnd = random.Random(seed)
    parts = []
    for i in range(n):
        r = rnd.uniform(0, 0.16)
        a = rnd.uniform(0, 2 * math.pi)
        b = _blade(Vector((math.cos(a) * r, math.sin(a) * r, -0.02)), rnd.uniform(0.55, 1.0) * hmax, rnd.uniform(0.05, 0.08),
                   rnd.uniform(0.04, 0.14), rnd)
        parts.append(b)
    m_ = merge(parts)
    paint_by(m_, lambda f, l: mix(pal("cimen", 2), pal("cimen", 0), smooth01(l.vert.co.z / hmax) * 0.9))
    return m_

def cicek_bm(seed, renkler):
    rnd = random.Random(seed)
    parts = [cimen_bm(seed, n=6, hmax=0.3)]
    for i in range(rnd.randint(3, 5)):
        r = rnd.uniform(0.03, 0.2)
        a = rnd.uniform(0, 2 * math.pi)
        hh = rnd.uniform(0.28, 0.45)
        c = Vector((math.cos(a) * r, math.sin(a) * r, hh))
        stem = _blade(Vector((c.x, c.y, -0.02)), hh, 0.025, 0.02, rnd)
        flat(stem, pal("yaprak", 1))
        parts.append(stem)
        head = bmesh.new()
        ctr = head.verts.new(c + Vector((0, 0, 0.02)))
        pet = []
        for k in range(10):
            ang = 2 * math.pi * k / 10
            rr = 0.075 if k % 2 == 0 else 0.035
            pet.append(head.verts.new(c + Vector((math.cos(ang) * rr, math.sin(ang) * rr, 0))))
        for k in range(10):
            head.faces.new((ctr, pet[k], pet[(k + 1) % 10]))
        col = rnd.choice(renkler)
        paint_by(head, lambda f, l, col=col: pal("altin", 0) if l.vert is ctr else col)
        parts.append(head)
    return merge(parts)

def cakil_bm(seed):
    rnd = random.Random(seed)
    parts = []
    for i in range(rnd.randint(3, 5)):
        b = _blob(Vector((rnd.uniform(-0.25, 0.25), rnd.uniform(-0.25, 0.25), 0.0)), rnd.uniform(0.05, 0.12), sub=1, squash=0.55,
                  seed=seed + i, jit=0.2)
        flat(b, mix(pal("kaya", 1), pal("kaya", 0), rnd.random()))
        parts.append(b)
    return merge(parts)

def saz_bm(seed):
    rnd = random.Random(seed)
    parts = []
    for i in range(11):
        r = rnd.uniform(0, 0.3)
        a = rnd.uniform(0, 2 * math.pi)
        parts.append(_blade(Vector((math.cos(a) * r, math.sin(a) * r, -0.05)), rnd.uniform(0.8, 1.45), 0.06, rnd.uniform(0.05, 0.25), rnd))
    m_ = merge(parts)
    paint_by(m_, lambda f, l: mix(pal("yaprak", 2), mix(pal("cimen", 0), pal("altin", 0), 0.25), smooth01(l.vert.co.z / 1.4)))
    heads = []
    for i in range(4):
        r = rnd.uniform(0, 0.22)
        a = rnd.uniform(0, 2 * math.pi)
        hz = rnd.uniform(1.0, 1.35)
        st = bm_cyl(0.012, 0.012, hz, sides=4)
        transform_bm(st, Matrix.Translation((math.cos(a) * r, math.sin(a) * r, -0.05)))
        flat(st, pal("yaprak", 2))
        hd = bm_cyl(0.035, 0.035, 0.2, sides=6)
        transform_bm(hd, Matrix.Translation((math.cos(a) * r, math.sin(a) * r, hz - 0.1)))
        flat(hd, mix(pal("ahsap", 2), pal("ahsap", 1), 0.3))
        heads += [st, hd]
    return merge([m_] + heads)

# ------------------------------------------------------------------ AĞAÇ ÇEŞİTLERİ
def kavak_bm(seed, height=11.0, sub=2):
    """Kavak: dik sütun taç (Anadolu dere kenarı, detay.md #3). Gövde ince, taç dar ve uzun."""
    rnd = random.Random(seed)
    tr = bm_lathe([(0.32, 0.0), (0.22, 0.4), (0.16, height * 0.5), (0.1, height * 0.9)], sides=6, close_bottom=False)
    paint_part(tr, "ahsap", lo=2, hi=1, seed=seed)
    blobs = []
    n = 6
    for k in range(n):
        t = k / (n - 1)
        z = height * (0.3 + 0.62 * t)
        r = 1.15 * (1.0 - 0.55 * t) * rnd.uniform(0.9, 1.1)
        c = Vector((rnd.uniform(-0.15, 0.15), rnd.uniform(-0.15, 0.15), z))
        b = _blob(c, r, sub=sub, squash=1.6, seed=seed + k, jit=0.1)
        blobs.append(b)
    cm = merge(blobs)
    z0, z1 = height * 0.15, height * 1.05
    paint_by(cm, lambda f, l: mix(pal("yaprak", 2), pal("yaprak", 0), smooth01((l.vert.co.z - z0) / (z1 - z0)) * 0.95))
    nt = len(tr.verts)
    return merge([tr, cm]), nt, Vector((0, 0, height * 0.6))

def cam_bm(seed, height=7.5, sub=2):
    """Çam: tepelerde koyu, katmanlı koni (silueti yuvarlak ağaçlardan ayırır)."""
    rnd = random.Random(seed)
    tr = bm_lathe([(0.3, 0.0), (0.2, height * 0.3), (0.08, height * 0.95)], sides=6, close_bottom=False)
    paint_part(tr, "ahsap", lo=2, hi=1, seed=seed)
    tiers = []
    for k in range(4):
        z = height * (0.22 + 0.19 * k)
        r = height * 0.3 * (1 - 0.2 * k)
        cone = bm_lathe(chaikin([(r, z), (r * 0.82, z + 0.25), (r * 0.35, z + height * 0.24), (0.0, z + height * 0.3)], 1),
                        sides=8 if sub > 1 else 6)
        jitter(cone, 0.05, seed=seed + k)
        tiers.append(cone)
    cm = merge(tiers)
    paint_by(cm, lambda f, l: mix(mix(pal("yaprak", 2), pal("dag", 2), 0.25), pal("yaprak", 1), smooth01(l.vert.co.z / height) * 0.8))
    nt = len(tr.verts)
    return merge([tr, cm]), nt, Vector((0, 0, height * 0.55))

def meyve_bm(seed, sub=2):
    """Meyve ağacı (bahçe): kısa gövde, yuvarlak taç, turuncu meyveler (vurgu küçük noktalar)."""
    ob = make_tree("_tmp_meyve", seed, height=3.6, crown=1.9, col=collection("_Tmp"), sub=sub, trunk=0.7)
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    me = ob.data
    bpy.data.objects.remove(ob, do_unlink=True)
    bpy.data.meshes.remove(me)
    rnd = random.Random(seed)
    fr = []
    if sub > 1:
        for i in range(10):
            a = rnd.uniform(0, 2 * math.pi)
            z = rnd.uniform(2.3, 3.6)
            r = 1.6 - abs(z - 2.9) * 0.6
            f_ = _blob(Vector((math.cos(a) * r, math.sin(a) * r, z)), 0.09, sub=1, jit=0.0)
            flat(f_, pal("cati", 0) if i % 3 else pal("altin", 0))
            fr.append(f_)
    return merge([bm] + fr), None, None

def kaya_kitle_bm(seed, size=5.0):
    """İri, düz yüzlü kaya kütlesi (vadi kenarı: 'düz yüzeyli iri kayalar', 09 §3) — 2-3 bloğun yaslanması."""
    rnd = random.Random(seed)
    parts = []
    for k in range(3):
        r = make_rock("_tmp_kk", seed * 3 + k, size=size * (1.0 - 0.28 * k), flat=0.75 - 0.1 * k, col=collection("_Tmp"))
        bm = bmesh.new()
        bm.from_mesh(r.data)
        me = r.data
        bpy.data.objects.remove(r, do_unlink=True)
        bpy.data.meshes.remove(me)
        transform_bm(bm, M(rnd.uniform(-0.6, 0.6) * size * k * 0.5, rnd.uniform(-0.4, 0.4) * size * k * 0.5, 0, rnd.uniform(0, 6)))
        parts.append(bm)
    return merge(parts)

def _model_lod(name, loc, fn, col, nlod=2):
    root = empty(name, col, loc=loc, kind="PLAIN_AXES", size=0.5)
    obs = []
    for k in range(nlod):
        sub = 2 if k == 0 else 1
        bm, n_trunk, center = fn(sub)
        o = to_object(bm, name + "_LOD%d" % k, col, loc=loc, sharp_angle=0)
        if center is not None:
            _custom_normals(o, lambda vi, nt=n_trunk, c=center: c if vi >= nt else None)
        parent_keep(o, root)
        obs.append(o)
    return obs

def build_detay_modelleri():
    col = collection(DOGA_COL)
    tmp = collection("_Tmp")
    names = ["SM_Cimen_", "SM_Cicek_", "SM_Cakil", "SM_Saz", "SM_Kavak", "SM_Cam", "SM_Meyve", "SM_Kaya_Kitle"]
    for o in list(col.objects):
        if any(o.name.startswith(n) for n in names):
            me = o.data if o.type == "MESH" else None
            bpy.data.objects.remove(o, do_unlink=True)
            if me is not None and me.users == 0:
                bpy.data.meshes.remove(me)
    obs = []
    y = 34.0
    for i in range(3):
        o = to_object(cimen_bm(2100 + i, n=8 + i * 2, hmax=0.35 + 0.08 * i), "SM_Cimen_%d" % (i + 1), col, loc=(200 + i * 2, y, 0), sharp_angle=0)
        _up_normals(o)
        obs.append(o)
    renk_set = [[pal("altin", 0), pal("siva", 0)], [pal("cati", 0), pal("altin", 0)], [pal("gok", 1), pal("siva", 0)]]
    for i in range(3):
        o = to_object(cicek_bm(2200 + i, renk_set[i]), "SM_Cicek_%d" % (i + 1), col, loc=(208 + i * 2, y, 0), sharp_angle=0)
        _up_normals(o)
        obs.append(o)
    obs.append(to_object(cakil_bm(2301), "SM_Cakil", col, loc=(216, y, 0), sharp_angle=40))
    o = to_object(saz_bm(2401), "SM_Saz", col, loc=(220, y, 0), sharp_angle=0)
    _up_normals(o)
    obs.append(o)
    obs += _model_lod("SM_Kavak", (226, y, 0), lambda sb: kavak_bm(2501, sub=sb), col)
    obs += _model_lod("SM_Cam", (232, y, 0), lambda sb: cam_bm(2601, sub=sb), col)
    obs += _model_lod("SM_Meyve", (238, y, 0), lambda sb: meyve_bm(2701, sub=sb), col)
    for i in range(2):
        obs.append(to_object(kaya_kitle_bm(2801 + i, size=4.0 + 1.5 * i), "SM_Kaya_Kitle_%d" % (i + 1), col, loc=(246 + i * 14, y, 0), sharp_angle=40))
    print("Detay modelleri:", {o.name: tri_count([o]) for o in obs})

# ================================================================== KÖY HAYATI (tekil, birleşik mesh)
def _meyve(c, r, seed=0, squash=1.0):
    """Yuvarlak meyve/sebze: 7 kenarlı küre (ikosahedron gibi kristal görünmez), ~50 üçgen."""
    prof = [(math.sin(math.pi * k / 4) * r, -math.cos(math.pi * k / 4) * r * squash) for k in range(5)]
    b = bm_lathe(prof, sides=7)
    transform_bm(b, Matrix.Translation(c))
    return b

def _sepet(x, y, z, r, h, seed, icerik=None, n_icerik=7):
    """Örgü sepet: bantlı (açık/koyu sıra) + kalın ağız halkası, içinde mal."""
    rnd = random.Random(seed)
    parts = []
    b = bm_lathe([(0.0, 0.0), (r * 0.75, 0.0), (r, h * 0.7), (r * 1.05, h), (r * 0.9, h), (r * 0.82, h * 0.25), (0.0, h * 0.2)], sides=12)
    paint_by(b, lambda f, l: mix(pal("altin", 2), pal("ahsap", 0), 0.35 + 0.4 * (int(l.vert.co.z / (h / 5) + 0.5) % 2)))
    transform_bm(b, Matrix.Translation((x, y, z)))
    parts.append(b)
    if icerik:
        for j in range(n_icerik):
            a = rnd.uniform(0, 2 * math.pi)
            rr = rnd.uniform(0, r * 0.6)
            fr = _meyve(Vector((x + math.cos(a) * rr, y + math.sin(a) * rr, z + h * 0.85 + rnd.uniform(0, 0.06))),
                        rnd.uniform(0.055, 0.08), seed + j, rnd.uniform(0.85, 1.1))
            flat(fr, rnd.choice(icerik))
            parts.append(fr)
    return parts

def _kasa(x, y, z, yaw, seed, icerik):
    """Çıtalı kasa (aralıklı tahtalar, köşe dikmeleri) + üstü dolu mal — kalıp sandık değil, pazar kasası."""
    rnd = random.Random(seed)
    parts = []
    W, D, H = 0.62, 0.42, 0.3
    for zz in (0.02, 0.13, 0.24):
        for sy in (-1, 1):
            s_ = box(W, 0.025, 0.07, y=sy * D / 2, z=zz)
            grad(s_, "ahsap", 1, 0)
            parts.append(s_)
        for sx in (-1, 1):
            s_ = box(0.025, D, 0.07, x=sx * W / 2, z=zz)
            grad(s_, "ahsap", 1, 0)
            parts.append(s_)
    for sx in (-1, 1):
        for sy in (-1, 1):
            p = box(0.045, 0.045, H + 0.02, x=sx * (W / 2 - 0.02), y=sy * (D / 2 - 0.02))
            flat(p, pal("ahsap", 2))
            parts.append(p)
    fl = box(W - 0.04, D - 0.04, 0.03, z=0.01)
    flat(fl, pal("ahsap", 2))
    parts.append(fl)
    for j in range(12):
        fr = _meyve(Vector((rnd.uniform(-W / 2 + 0.08, W / 2 - 0.08), rnd.uniform(-D / 2 + 0.08, D / 2 - 0.08), H + rnd.uniform(-0.02, 0.05))),
                    rnd.uniform(0.06, 0.085), seed + j, rnd.uniform(0.85, 1.1))
        flat(fr, rnd.choice(icerik))
        parts.append(fr)
    m_ = merge(parts)
    transform_bm(m_, M(x, y, z, yaw))
    return m_

def _cuval(x, y, z, seed, tane):
    """Ağzı kıvrılmış açık çuval; içinde tane/baharat tepeciği."""
    rnd = random.Random(seed)
    cv = bm_lathe(chaikin([(0.0, 0.0), (0.24, 0.0), (0.29, 0.2), (0.26, 0.42), (0.3, 0.48), (0.27, 0.5), (0.22, 0.44), (0.0, 0.44)], 1), sides=10)
    jitter(cv, 0.015, seed=seed)
    grad(cv, "kil", 2, 0)
    tp = bm_lathe([(0.23, 0.44), (0.12, 0.52), (0.0, 0.55)], sides=10, close_bottom=True)
    flat(tp, tane)
    m_ = merge([cv, tp])
    transform_bm(m_, Matrix.Translation((x, y, z)))
    return m_

def _kup(x, y, z, s, seed, renk="kil"):
    """Pişmiş toprak küp / testi (dönem kabı, 09 kil satırı)."""
    k = bm_lathe(chaikin([(0.0, 0.0), (0.1, 0.0), (0.17, 0.12), (0.18, 0.26), (0.1, 0.4), (0.08, 0.46), (0.1, 0.5), (0.0, 0.5)], 1), sides=10)
    paint_by(k, lambda f, l: mix(pal(renk, 2), pal(renk, 0), smooth01(l.vert.co.z / 0.5)) if not (0.27 < l.vert.co.z < 0.31)
             else pal("cati", 1))
    transform_bm(k, Matrix.Translation((x, y, z)) @ Matrix.Scale(s, 4))
    return k

def _terazi(x, y, z, seed):
    """Tezgah terazisi (Denkleştirme köyünün konusu: iki kefe dengede)."""
    parts = []
    bs = box(0.32, 0.18, 0.04, z=0.0, bevel=0.01)
    grad(bs, "ahsap", 2, 1)
    parts.append(bs)
    p = bm_cyl(0.018, 0.014, 0.42, sides=6)
    transform_bm(p, Matrix.Translation((0, 0, 0.04)))
    grad(p, "bakir", 2, 0)
    parts.append(p)
    bm_ = tube_pts([(-0.22, 0, 0.45), (0.22, 0, 0.45)], 0.012, sides=5)
    grad(bm_, "bakir", 2, 0)
    parts.append(bm_)
    for sx in (-1, 1):
        for k in range(3):
            a = 2 * math.pi * k / 3
            ch = tube_pts([(sx * 0.22, 0, 0.45), (sx * 0.22 + math.cos(a) * 0.09, math.sin(a) * 0.09, 0.2)], 0.004, sides=3)
            flat(ch, pal("bakir", 2))
            parts.append(ch)
        pan = bm_lathe([(0.0, 0.0), (0.1, 0.02), (0.11, 0.04), (0.0, 0.03)], sides=10)
        transform_bm(pan, Matrix.Translation((sx * 0.22, 0, 0.17)))
        grad(pan, "bakir", 2, 0)
        parts.append(pan)
        for j in range(3):
            fr = _blob(Vector((sx * 0.22 + (j - 1) * 0.04, 0.0, 0.23)), 0.035, sub=1, jit=0.0)
            flat(fr, pal("cati", 0) if sx < 0 else pal("altin", 0))
            parts.append(fr)
    m_ = merge(parts)
    transform_bm(m_, Matrix.Translation((x, y, z)))
    return m_

def _orgu_asili(x, y, ztop, n, renk, seed):
    """Tente kirişinden sarkan örgü (soğan/sarımsak dizisi ya da kuru biber)."""
    rnd = random.Random(seed)
    parts = []
    rope = box(0.015, 0.015, 0.75, x=x, y=y, z=ztop - 0.75)
    flat(rope, mix(pal("kil", 0), pal("altin", 2), 0.3))
    parts.append(rope)
    for j in range(n):
        zz = ztop - 0.12 - j * (0.62 / n)
        b = _meyve(Vector((x + rnd.uniform(-0.04, 0.04), y + rnd.uniform(-0.04, 0.04), zz)), 0.055 - j * 0.002, seed + j, 1.2)
        flat(b, renk)
        parts.append(b)
    return parts

def _ot_demeti(x, y, ztop, seed):
    """Baş aşağı asılı kuru ot demeti (yeşil koni + ip)."""
    d = bm_cyl(0.02, 0.11, 0.38, sides=7)
    transform_bm(d, Matrix.Translation((x, y, ztop - 0.5)))
    paint_by(d, lambda f, l: mix(pal("yaprak", 2), pal("cimen", 0), smooth01((l.vert.co.z - (ztop - 0.5)) / 0.38)))
    st = box(0.012, 0.012, 0.12, x=x, y=y, z=ztop - 0.12)
    flat(st, pal("kil", 0))
    return [d, st]

def tezgah_bm(seed, mal="manav", kim="Denk"):
    """Pazar tezgahı (detay.md §2). Yerel ön = -Y (alıcı). mal: manav · baharat · ekmek · kagit.
    Çatkı + oymalı dikme başları, eğik çizgili tente (sarkık, saçak dişli), önü kumaş örtülü tezgah,
    arkada raf, kirişten sarkan mallar, tezgahta sepetler/terazi, önde kasa/çuval/küp, tepede simgeli levha."""
    rnd = random.Random(seed)
    parts = []
    W, D = 2.6, 1.4
    rc = KIMLIK[kim]["renk"]
    yf, yb = -D / 2, D / 2
    # --- çatkı
    for sx in (-1, 1):
        for sy, hh in ((-1, 2.2), (1, 2.7)):
            p = box(0.12, 0.12, hh, x=sx * W / 2, y=sy * D / 2, z=-0.1, bevel=0.015)
            grad(p, "ahsap", 2, 1)
            parts.append(p)
            kn = bm_lathe([(0.0, 0.0), (0.08, 0.0), (0.09, 0.06), (0.05, 0.12), (0.0, 0.14)], sides=8)
            transform_bm(kn, Matrix.Translation((sx * W / 2, sy * D / 2, hh - 0.1)))
            grad(kn, "ahsap", 1, 0)
            parts.append(kn)
    for zz, yy, L_ in ((2.12, yf, W + 0.25), (2.62, yb, W + 0.25)):
        bmm = box(L_, 0.1, 0.1, y=yy, z=zz, bevel=0.01)
        grad(bmm, "ahsap", 2, 1)
        parts.append(bmm)
    # --- tezgah: kalın üst + önünde köy renginde dişli örtü
    top = box(W + 0.15, D * 0.6, 0.09, y=-D * 0.18, z=0.9, bevel=0.02)
    grad(top, "ahsap", 1, 0)
    parts.append(top)
    body = box(W - 0.05, D * 0.55, 0.85, y=-D * 0.16, z=0.05)
    paint_by(body, lambda f, l: mix(pal("ahsap", 2), pal("ahsap", 1), 0.25 + 0.35 * (int((l.vert.co.x + 3) / 0.22) % 2)))
    parts.append(body)
    cz = 0.97
    cloth_pts = [(-W / 2 - 0.05, cz)]
    nteeth = 9
    for i in range(nteeth):
        u0 = -W / 2 - 0.05 + (W + 0.1) * i / nteeth
        u1 = -W / 2 - 0.05 + (W + 0.1) * (i + 1) / nteeth
        cloth_pts += [(u0, 0.5), ((u0 + u1) / 2, 0.38), (u1, 0.5)]
    cloth_pts += [(W / 2 + 0.05, cz)]
    cl = bm_wall(cloth_pts, [], 0.02)
    transform_bm(cl, Matrix.Translation((0, -D * 0.18 - D * 0.3 - 0.04, 0)))
    paint_by(cl, lambda f, l: rc(0.65) if l.vert.co.z > 0.86 else rc(0.35))
    parts.append(cl)
    # --- arka raf (iki kat)
    for zz in (1.0, 1.45):
        sh = box(W - 0.15, 0.32, 0.05, y=yb - 0.2, z=zz)
        grad(sh, "ahsap", 1, 0)
        parts.append(sh)
    # --- tente: iki parçalı (sarkık), çizgili, saçak dişli, ortada mahya sırığı
    n = 9
    def tz(t):            # t: 0 arka → 1 ön; hafif sarkma
        return 2.7 + (2.1 - 2.7) * t - 0.07 * math.sin(math.pi * t)
    for i in range(n):
        x0 = -W / 2 - 0.2 + (W + 0.4) * i / n
        x1 = -W / 2 - 0.2 + (W + 0.4) * (i + 1) / n
        colr = pal("cati", 1) if i % 2 == 0 else pal("siva", 0)
        if kim == "Tepk":
            colr = rc(0.45) if i % 2 == 0 else pal("siva", 0)
        for k in range(3):
            t0, t1 = k / 3, (k + 1) / 3
            ya, yb_ = yb + 0.12 + (yf - 0.5 - yb - 0.12) * t0, yb + 0.12 + (yf - 0.5 - yb - 0.12) * t1
            tb = bmesh.new()
            v = [tb.verts.new(p) for p in ((x0, ya, tz(t0)), (x1, ya, tz(t0)), (x1, yb_, tz(t1)), (x0, yb_, tz(t1)))]
            tb.faces.new(v)
            flat(tb, colr)
            parts.append(tb)
        tri = bmesh.new()
        tv = [tri.verts.new(p) for p in ((x0, yf - 0.5, tz(1)), (x1, yf - 0.5, tz(1)), ((x0 + x1) / 2, yf - 0.52, tz(1) - 0.22))]
        tri.faces.new(tv)
        flat(tri, colr)
        parts.append(tri)
    # --- tepede simgeli levha (köy simgesi; yazı TMP ile)
    sg = box(0.9, 0.06, 0.42, y=yf - 0.52, z=tz(1) + 0.1, bevel=0.02)
    grad(sg, "ahsap", 2, 1)
    parts.append(sg)
    em = simge_bm(KIMLIK[kim]["simge"], size=0.3, th=0.09, color=rc(0.6))
    transform_bm(em, Matrix.Translation((0, yf - 0.52, tz(1) + 0.31)))
    parts.append(em)
    zt = 0.99
    # --- mallar
    if mal == "manav":
        parts += _sepet(-0.85, -0.3, zt, 0.24, 0.17, seed + 1, [pal("cati", 0), pal("cati", 1)])
        parts += _sepet(-0.3, -0.38, zt, 0.22, 0.15, seed + 2, [pal("altin", 0), pal("altin", 1)])
        parts += _sepet(0.25, -0.3, zt, 0.24, 0.17, seed + 3, [pal("yaprak", 0), pal("cimen", 0)])
        parts.append(_terazi(0.85, -0.3, zt, seed + 4))
        parts.append(_kasa(-0.75, yf - 0.45, 0.0, 0.08, seed + 5, [pal("cati", 0), pal("altin", 0)]))
        parts.append(_kasa(0.0, yf - 0.48, 0.0, -0.05, seed + 6, [pal("yaprak", 0), pal("yaprak", 1)]))
        k2 = _kasa(-0.4, yf - 0.48, 0.32, 0.15, seed + 7, [pal("cati", 1)])
        parts.append(k2)
        for j, x in enumerate((-0.9, -0.3, 0.3, 0.9)):
            parts += _orgu_asili(x, yf - 0.35, 2.08, 8, mix(pal("siva", 0), pal("altin", 0), 0.25) if j % 2 == 0 else pal("cati", 1), seed + 10 + j)
        for j in range(5):
            parts.append(_kup(-1.0 + j * 0.42, yb - 0.2, 1.05, 0.55, seed + 20 + j))
    elif mal == "baharat":
        tones = [pal("cati", 1), pal("altin", 1), pal("cati", 2), pal("kil", 0), pal("yaprak", 1), pal("altin", 2)]
        for j in range(6):
            x = -1.0 + j * 0.4
            bowl = bm_lathe([(0.0, 0.0), (0.12, 0.0), (0.17, 0.07), (0.16, 0.09), (0.0, 0.05)], sides=10)
            transform_bm(bowl, Matrix.Translation((x, -0.32, zt)))
            grad(bowl, "ahsap", 2, 0)
            mound = bm_lathe([(0.15, 0.08), (0.08, 0.15), (0.0, 0.18)], sides=10, close_bottom=True)
            transform_bm(mound, Matrix.Translation((x, -0.32, zt)))
            flat(mound, tones[j])
            parts += [bowl, mound]
        parts.append(_terazi(0.9, -0.05, zt, seed + 4))
        for j in range(4):
            parts.append(_cuval(-0.9 + j * 0.6, yf - 0.45, 0.0, seed + 30 + j, tones[j]))
        for j in range(4):
            parts += _ot_demeti(-0.95 + j * 0.62, yf - 0.35, 2.08, seed + 40 + j)
        for j in range(6):
            parts.append(_kup(-1.05 + j * 0.42, yb - 0.2, 1.05 + (0.45 if j % 2 else 0), 0.5, seed + 50 + j, "kil" if j % 3 else "bakir"))
    elif mal == "ekmek":
        for j in range(3):
            x = -0.8 + j * 0.8
            parts += _sepet(x, -0.3, zt, 0.28, 0.14, seed + j)
            for k in range(4):
                lf = bm_lathe(chaikin([(0.0, 0.0), (0.09, 0.0), (0.1, 0.05), (0.06, 0.09), (0.0, 0.1)], 1), sides=8)
                transform_bm(lf, M(x + rnd.uniform(-0.12, 0.12), -0.3 + rnd.uniform(-0.1, 0.1), zt + 0.1 + k * 0.03, rnd.uniform(0, 3))
                             @ Matrix.Scale(1.7, 4, (1, 0, 0)))
                paint_by(lf, lambda f, l: mix(pal("altin", 2), pal("altin", 1), smooth01(l.vert.co.z / 0.1) * 0.7))
                parts.append(lf)
        for zz in (1.05, 1.5):                           # rafta simit benzeri halka ekmekler
            for j in range(5):
                rg = bm_lathe([(0.07, -0.02), (0.11, 0.0), (0.07, 0.02)], sides=10, close_top=False, close_bottom=False)
                transform_bm(rg, M(-1.0 + j * 0.5, yb - 0.2, zz + 0.02, rx=0.2))
                flat(rg, pal("altin", 2))
                parts.append(rg)
        for j in range(2):
            parts.append(_cuval(-0.6 + j * 0.9, yf - 0.45, 0.0, seed + 60 + j, mix(pal("siva", 0), pal("altin", 0), 0.2)))
        parts += _orgu_asili(0.0, yf - 0.35, 2.08, 6, pal("altin", 1), seed + 70)
    else:                                                # kâğıtçı (Tepkime Türleri: mektup, defter, mühür)
        for k in range(8):
            sc = bm_cyl(0.05, 0.05, 0.44, sides=6)
            transform_bm(sc, M(-1.0 + k * 0.17, -0.25, zt + 0.06, ry=math.pi / 2) @ Matrix.Translation((0, 0, -0.22)))
            flat(sc, mix(pal("siva", 0), pal("altin", 0), 0.15))
            parts.append(sc)
            if k % 3 == 0:
                sl = bm_cyl(0.04, 0.04, 0.02, sides=8)
                transform_bm(sl, M(-1.0 + k * 0.17, -0.31, zt + 0.06, rx=math.pi / 2))
                flat(sl, pal("cati", 1))
                parts.append(sl)
        z = zt
        for k in range(6):
            bk = box(0.36, 0.26, 0.07, x=0.6, y=-0.3, z=z, yaw=rnd.uniform(-0.25, 0.25), bevel=0.01)
            flat(bk, [pal("cati", 2), pal("dag", 2), pal("yaprak", 2), pal("altin", 2), pal("ahsap", 2), pal("gok", 2)][k])
            parts.append(bk)
            z += 0.07
        ink = bm_lathe([(0.0, 0.0), (0.06, 0.0), (0.06, 0.08), (0.03, 0.1), (0.0, 0.1)], sides=8)
        transform_bm(ink, Matrix.Translation((0.15, -0.3, zt)))
        flat(ink, pal("dag", 2))
        parts.append(ink)
        qu = bmesh.new()
        vs = [qu.verts.new(p) for p in ((0.17, -0.3, zt + 0.08), (0.28, -0.26, zt + 0.42), (0.24, -0.27, zt + 0.44))]
        qu.faces.new(vs)
        flat(qu, pal("siva", 0))
        parts.append(qu)
        for k in range(4):                               # askıda mühürlü mektuplar
            lt = box(0.22, 0.012, 0.16, x=-0.9 + k * 0.6, y=yf - 0.35, z=1.72)
            flat(lt, mix(pal("siva", 0), pal("altin", 0), 0.2))
            sl = bm_cyl(0.03, 0.03, 0.02, sides=8)
            transform_bm(sl, M(-0.9 + k * 0.6, yf - 0.37, 1.8, rx=math.pi / 2))
            flat(sl, pal("cati", 1))
            st = box(0.01, 0.01, 0.3, x=-0.9 + k * 0.6, y=yf - 0.35, z=1.88)
            flat(st, pal("kil", 0))
            parts += [lt, sl, st]
        for j in range(5):
            parts.append(_kup(-1.0 + j * 0.5, yb - 0.2, 1.05, 0.45, seed + 80 + j, "bakir" if j % 2 else "kil"))
        parts.append(_kasa(-0.6, yf - 0.45, 0.0, 0.1, seed + 90, [mix(pal("siva", 0), pal("altin", 0), 0.15)]))
    # tezgah önünde zemin: dökülmüş birkaç mal / çakıl yok; fener (akşam pazarı)
    lh = box(0.02, 0.02, 0.25, x=W / 2 - 0.15, y=yf - 0.4, z=1.85)
    flat(lh, demir(0.85))
    lf_ = box(0.16, 0.16, 0.22, x=W / 2 - 0.15, y=yf - 0.4, z=1.62, bevel=0.01)
    grad(lf_, "bakir", 2, 0)
    parts += [lh, lf_]
    return merge(parts)

def bahce_bm(w, d, seed):
    """Ev arkası sebze bahçesi: çitli küçük tarla + ekin (her zaman görünür)."""
    soil = tarla_bm(0, 0, w, d, 0.0, None, seed)
    crop = tarla_bm(0, 0, w, d, 0.0, "sebze", seed)
    m_ = merge([soil, crop])
    for v in m_.verts:
        v.co.z -= height(0, 0) * 0           # tarla_bm yüksekliği (cx, cy)'ye göre göreli verir
    return m_

def tas_duvar_bm(pts, seed, h=0.7):
    """Kuru taş duvar: yol boyunca iri taş sırası + kapak taşları. pts dünya XY."""
    rnd = random.Random(seed)
    parts = []
    rs = resample(pts, 0.75)
    for i in range(len(rs) - 1):
        a, b = rs[i], rs[i + 1]
        d = (b - a)
        yaw = math.atan2(d.y, d.x)
        for row, (zz, sh) in enumerate(((0.0, 0.36), (0.33, 0.32))):
            m = a.lerp(b, 0.5 + (0.25 if row else 0.0))
            st = box(0.6 + rnd.uniform(-0.08, 0.1), 0.5 + rnd.uniform(-0.05, 0.05), sh + rnd.uniform(-0.04, 0.04),
                     x=m.x, y=m.y, z=height(m.x, m.y) + zz - 0.1, yaw=yaw + rnd.uniform(-0.12, 0.12), bevel=0.05)
            jitter(st, 0.03, seed=seed + i * 2 + row)
            k = rnd.uniform(0.9, 1.08)
            paint_by(st, lambda f, l, k=k: tone(mix(pal("kaya", 1), pal("kaya", 0), 0.3 + 0.5 * max(0.0, f.normal.z)), k))
            parts.append(st)
        if i % 2 == 0:
            m = a.lerp(b, 0.5)
            cp = box(1.05, 0.62, 0.12, x=m.x, y=m.y, z=height(m.x, m.y) + 0.6, yaw=yaw + rnd.uniform(-0.06, 0.06), bevel=0.04)
            grad(cp, "kaya", 1, 0)
            parts.append(cp)
        if rnd.random() < 0.18:                                 # duvar dibinde çimen/yosun
            m = a.lerp(b, 0.5)
            g = cimen_bm(seed + i, n=6, hmax=0.3)
            transform_bm(g, Matrix.Translation((m.x + rnd.uniform(-0.3, 0.3), m.y + rnd.uniform(-0.4, 0.4), height(m.x, m.y))))
            parts.append(g)
    return merge(parts)

def araba_bm(seed):
    """İki tekerli el arabası / at arabası (çuval yüklü). Yerel uzunluk X."""
    rnd = random.Random(seed)
    parts = []
    bed = box(2.2, 1.3, 0.12, z=0.72, bevel=0.02)
    grad(bed, "ahsap", 1, 0)
    parts.append(bed)
    for sy in (-1, 1):
        sd = box(2.2, 0.08, 0.38, y=sy * 0.62, z=0.82)
        paint_by(sd, lambda f, l: mix(pal("ahsap", 2), pal("ahsap", 1), 0.3 + 0.4 * (int((l.vert.co.z + 1) / 0.12) % 2)))
        parts.append(sd)
    for sy in (-1, 1):
        sh = tube_pts([(1.1, sy * 0.4, 0.75), (2.2, sy * 0.38, 0.55), (3.0, sy * 0.36, 0.42)], 0.05, sides=5)
        grad(sh, "ahsap", 2, 1)
        parts.append(sh)
    ax = tube_pts([(0, -0.85, 0.48), (0, 0.85, 0.48)], 0.05, sides=6)
    flat(ax, demir(0.85))
    parts.append(ax)
    for sy in (-1, 1):
        wh = bm_lathe([(0.12, -0.05), (0.48, -0.05), (0.5, 0.0), (0.48, 0.05), (0.12, 0.05)], sides=14, close_top=False, close_bottom=False)
        transform_bm(wh, M(0, sy * 0.78, 0.48, rx=math.pi / 2))
        paint_by(wh, lambda f, l: demir(0.8) if l.vert.co.xz.length > 0.47 else pal("ahsap", 1))
        parts.append(wh)
        for k in range(6):
            sp = box(0.05, 0.05, 0.72, origin="center")
            transform_bm(sp, M(0, sy * 0.78, 0.48, ry=k * math.pi / 6))
            flat(sp, pal("ahsap", 1))
            parts.append(sp)
    rest = box(0.1, 0.1, 0.45, x=2.9, z=0.0)
    flat(rest, pal("ahsap", 2))
    parts.append(rest)
    for k in range(4):
        cv = bm_lathe(chaikin([(0.0, 0.0), (0.24, 0.0), (0.27, 0.22), (0.18, 0.42), (0.0, 0.46)], 1), sides=8)
        jitter(cv, 0.02, seed=seed + k)
        transform_bm(cv, M(-0.6 + (k % 2) * 0.6, -0.28 + (k // 2) * 0.55, 0.84, rx=math.pi / 2 * (0.9 if k == 3 else 0)))
        grad(cv, "kil", 2, 0)
        parts.append(cv)
    return merge(parts)

def camasir_bm(p0, p1, seed):
    rnd = random.Random(seed)
    parts = []
    tops = []
    for p in (p0, p1):
        z = height(p.x, p.y)
        po = box(0.1, 0.1, 2.2, x=p.x, y=p.y, z=z - 0.3)
        grad(po, "ahsap", 2, 1)
        parts.append(po)
        tops.append(Vector((p.x, p.y, z + 1.85)))
    n = 10
    pts = [tops[0].lerp(tops[1], i / n) - Vector((0, 0, 0.3 * 4 * (i / n) * (1 - i / n))) for i in range(n + 1)]
    rp = tube_pts(pts, 0.012, sides=4)
    flat(rp, mix(pal("kil", 0), pal("altin", 2), 0.3))
    parts.append(rp)
    d = (tops[1] - tops[0]).normalized()
    for i in range(2, n - 1, 2):
        w = rnd.uniform(0.45, 0.7)
        h = rnd.uniform(0.5, 0.85)
        c = pts[i]
        cl = bmesh.new()
        q = [c - d * w / 2, c + d * w / 2, c + d * w / 2 - Vector((0, 0, h)), c - d * w / 2 - Vector((0, 0, h * rnd.uniform(0.9, 1.05)))]
        vs = [cl.verts.new(v) for v in q]
        cl.faces.new(vs)
        flat(cl, rnd.choice([pal("siva", 0), pal("gok", 1), pal("cati", 0), pal("altin", 0)]))
        parts.append(cl)
    return merge(parts)

def kovan_bm(seed):
    """Saman arı kovanı (ortaçağ 'skep'), taş/ahşap sehpa üstünde, 3 tane."""
    parts = []
    st = box(2.4, 0.6, 0.08, z=0.5, bevel=0.02)
    grad(st, "ahsap", 1, 0)
    parts.append(st)
    for sx in (-1, 1):
        lg = box(0.12, 0.5, 0.55, x=sx * 1.05, z=-0.05)
        flat(lg, pal("ahsap", 2))
        parts.append(lg)
    for k in range(3):
        sk = bm_lathe(chaikin([(0.3, 0.0), (0.32, 0.18), (0.26, 0.38), (0.14, 0.52), (0.0, 0.56)], 1), sides=10)
        paint_by(sk, lambda f, l: mix(pal("altin", 2), pal("altin", 0), 0.3 + 0.5 * (int(l.vert.co.z / 0.07) % 2)))
        transform_bm(sk, Matrix.Translation((-0.75 + k * 0.75, 0, 0.58)))
        parts.append(sk)
        dr = box(0.1, 0.04, 0.06, x=-0.75 + k * 0.75, y=-0.3, z=0.6)
        flat(dr, pal("ahsap", 2))
        parts.append(dr)
    return merge(parts)

def odun_bm(seed, n=12):
    rnd = random.Random(seed)
    parts = []
    for i in range(n):
        row = i // 5
        lg = bm_cyl(0.11, 0.11, 1.1, sides=6)
        transform_bm(lg, M(-0.5 + (i % 5) * 0.24 + row * 0.12, 0, 0.11 + row * 0.2, rx=math.pi / 2) @ Matrix.Translation((0, 0, -0.55)))
        paint_by(lg, lambda f, l: pal("ahsap", 1) if abs(f.normal.y) < 0.7 else pal("kil", 0))
        parts.append(lg)
    return merge(parts)

# ------------------------------------------------------------------ UZAK MANZARA
def uzak_daglar_bm(seed=3001):
    """Vadinin çevresinde uzak dağ halkası: pusa kayan mavi, tepeler açık (09 §3 'uzaklaştıkça maviye ve açığa')."""
    rnd = random.Random(seed)
    bm = bmesh.new()
    lay = bm.loops.layers.float_color.new("Col")
    n = 120
    c = Vector((8.0, -3.0))
    rings = []
    for i in range(n):
        a = 2 * math.pi * i / n
        h = 70 + 55 * (0.5 + 0.5 * n2(math.cos(a) * 3, math.sin(a) * 3, 1.0, 4.4)) + 30 * (0.5 + 0.5 * n2(math.cos(a) * 9, math.sin(a) * 9, 1.0, 7.7))
        ca, sa = math.cos(a), math.sin(a)
        prof = [(330.0, -5.0), (370.0, h * 0.55), (405.0, h), (440.0, h * 0.7), (470.0, h * 0.3)]
        rings.append([bm.verts.new((c.x + ca * r, c.y + sa * r, z)) for (r, z) in prof])
    for i in range(n):
        A, B = rings[i], rings[(i + 1) % n]
        for j in range(4):
            f = bm.faces.new((A[j], B[j], B[j + 1], A[j + 1]))
            for l in f.loops:
                t = smooth01(l.vert.co.z / 150.0)
                l[lay] = mix(mix(pal("dag", 1), pal("gok", 1), 0.25), mix(pal("dag", 0), pal("gok", 0), 0.5), t)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    for f in bm.faces:
        cc = f.calc_center_median()
        if f.normal.dot(Vector((c.x - cc.x, c.y - cc.y, 0)).normalized()) < -0.2:
            f.normal_flip()
    return bm

def uzak_zemin_bm(z=9.0):
    """Arazi ızgarasının dışı ile dağ halkası arası: pusa kayan düz yaka (boşluk görünmesin)."""
    bm = bmesh.new()
    lay = bm.loops.layers.float_color.new("Col")
    inner = [(X0 + 1, Y0 + 1), (X1 - 1, Y0 + 1), (X1 - 1, Y1 - 1), (X0 + 1, Y1 - 1)]
    outer = [(-460, -470), (480, -470), (480, 460), (-460, 460)]
    vi = [bm.verts.new((x, y, z)) for (x, y) in inner]
    vo = [bm.verts.new((x, y, z - 4)) for (x, y) in outer]
    for i in range(4):
        j = (i + 1) % 4
        f = bm.faces.new((vi[i], vo[i], vo[j], vi[j]))
        for l in f.loops:
            l[lay] = mix(pal("cimen", 1), pal("dag", 1), 0.55)
    for f in bm.faces:
        if f.normal.z < 0:
            f.normal_flip()
    return bm

def bulut_bm(seed, n=14):
    rnd = random.Random(seed)
    parts = []
    for i in range(n):
        a = rnd.uniform(0, 2 * math.pi)
        r = rnd.uniform(140, 300)
        c = Vector((8 + math.cos(a) * r, -3 + math.sin(a) * r, rnd.uniform(70, 115)))
        for k in range(rnd.randint(4, 6)):
            b = _blob(c + Vector((rnd.uniform(-14, 14), rnd.uniform(-6, 6), rnd.uniform(-2, 4))), rnd.uniform(7, 13), sub=2,
                      squash=0.55, seed=seed + i * 10 + k, jit=0.08)
            paint_by(b, lambda f, l, cz=c.z: mix(mix(pal("gok", 0), pal("dag", 0), 0.3), pal("siva", 0), smooth01((l.vert.co.z - cz + 6) / 10)))
            parts.append(b)
    return merge(parts)

# ================================================================== YERLEŞİM
TEZGAHLAR = [(46.4, -2.3, 0.0, "manav"), (54.6, -2.2, 0.05, "ekmek"), (46.0, -10.9, math.pi, "baharat"), (-39.4, -6.6, math.pi, "kagit")]
BAHCELER = [(42.5, 6.6, 0.06, 5.0, 3.6), (61.2, 9.0, 0.32, 4.5, 3.2), (50.5, -19.6, -0.06, 4.5, 3.4),
            (-37.5, 8.9, 0.05, 4.8, 3.4), (-56.0, -14.4, 0.1, 4.6, 3.2)]
DUVARLAR = [[(7.5, -12.2), (16.0, -12.8), (24.0, -12.0)], [(-8.0, -11.2), (-17.0, -10.4)], [(-26.0, -8.4), (-31.0, -7.6)],
            [(63.0, -3.0), (68.0, 3.0)], [(44.0, -24.0), (57.0, -24.0)]]
ARABA = (37.6, -11.6, 0.4)
CAMASIR = [(Vector((-38.0, -14.8)), Vector((-33.4, -14.2))), (Vector((60.0, 6.8)), Vector((63.6, 9.2)))]
KOVAN = (59.0, -27.5, 0.0)
ODUNLAR = [(39.4, 3.6, 0.1), (-60.5, -10.8, 1.4), (-47.5, 12.8, 0.3)]
BAHCE_MEYVE = (50.5, -28.5, 5, 4, 4.2)     # (x, y, sütun, satır, aralık)

def _yapi_kutulari(margin=1.2):
    kut = []
    for cn in ("Harita_Yapilar", "Harita_Yon", DETAY_COL):
        c = bpy.data.collections.get(cn)
        if not c:
            continue
        for o in c.all_objects:
            if o.type != "MESH" or not o.name.startswith("SM_") or o.name.startswith(("SM_Mil", "SM_Koy_Zemin", "SM_Uzak", "SM_Bulut")):
                continue
            bb = [o.matrix_world @ Vector(cc) for cc in o.bound_box]
            xs = [v.x for v in bb]; ys = [v.y for v in bb]
            if (max(xs) - min(xs)) * (max(ys) - min(ys)) > 400:
                continue
            kut.append((min(xs) - margin, max(xs) + margin, min(ys) - margin, max(ys) + margin))
    return kut

def build_detay():
    col = collection(DETAY_COL)
    clear_collection(DETAY_COL)
    made = []
    for i, (x, y, yaw, kind) in enumerate(TEZGAHLAR):
        par = bpy.data.objects["Koy_Denk"] if x > 0 else bpy.data.objects["Koy_Tepk"]
        made.append(put_detay(tezgah_bm(4000 + i, kind, "Denk" if x > 0 else "Tepk"), "SM_%s_Tezgah_%d_%s" % ("Denk" if x > 0 else "Tepk", i + 1, kind.capitalize()), x, y, yaw,
                              z=ground_min(x, y, 1.3, 0.8, yaw), parent=par))
    bh = []
    for i, (x, y, yaw, w, d) in enumerate(BAHCELER):
        b = merge([tarla_bm(x, y, w, d, yaw, None, 4100 + i), tarla_bm(x, y, w, d, yaw, "sebze", 4100 + i)])
        transform_bm(b, Matrix.Translation((x, y, height(x, y))))
        bh.append(b)
    made.append(put_detay(merge(bh), "SM_Ev_Bahceleri", 0.0, 0.0, 0.0, z=0.0))
    made.append(put_detay(merge([tas_duvar_bm([Vector(p) for p in pts], 4200 + i) for i, pts in enumerate(DUVARLAR)]),
                          "SM_Tas_Duvarlar", 0.0, 0.0, 0.0, z=0.0))
    x, y, yaw = ARABA
    made.append(put_detay(araba_bm(4300), "SM_Denk_Araba", x, y, yaw, z=ground_min(x, y, 1.5, 1.0, yaw)))
    made.append(put_detay(merge([camasir_bm(a, b, 4400 + i) for i, (a, b) in enumerate(CAMASIR)]), "SM_Camasir_Ipleri", 0.0, 0.0, 0.0, z=0.0))
    x, y, yaw = KOVAN
    made.append(put_detay(kovan_bm(4500), "SM_Ari_Kovanlari", x, y, yaw, z=ground_min(x, y, 1.2, 0.4, yaw)))
    od = []
    for i, (x, y, yaw) in enumerate(ODUNLAR):
        b = odun_bm(4600 + i)
        transform_bm(b, M(x, y, height(x, y), yaw))
        od.append(b)
    made.append(put_detay(merge(od), "SM_Odun_Yiginlari", 0.0, 0.0, 0.0, z=0.0))
    # uzak manzara (gölge düşürmez; Unity'de Cast Shadows = Off)
    ob = to_object(uzak_daglar_bm(), "SM_Uzak_Daglar", col, sharp_angle=0)
    made.append(ob)
    made.append(to_object(uzak_zemin_bm(), "SM_Uzak_Zemin", col, sharp_angle=0))
    made.append(to_object(bulut_bm(3101), "Bulutlar", col, sharp_angle=0, mat=m_bulut()))
    print("Detay yapıları:", {o.name: tri_count([o]) for o in made})
    return made

def scatter_detay():
    """Zemin örtüsü ve ağaç çeşitliliği: Yer_* boş objeleri (Unity'de prefab + GPU instancing)."""
    yc = collection(YER_COL)
    for o in list(yc.objects):
        if any(t in o.name for t in ("Cimen", "Cicek", "Cakil", "Saz", "Kavak", "Cam_", "Meyve", "Kaya_Kitle")):
            bpy.data.objects.remove(o, do_unlink=True)
    # tepe ağaçlarının bir kısmı çam olur (yükseklikle)
    rnd = random.Random(5150)
    for o in list(yc.objects):
        if "Agac_Yesil" in o.name and o.location.z > 13.0 and rnd.random() < 0.55:
            o.name = o.name.replace("Agac_Yesil_1", "Cam").replace("Agac_Yesil_2", "Cam").replace("Agac_Yesil_3", "Cam")
    kut = _yapi_kutulari()
    def in_box(p):
        return any(a <= p.x <= b and c <= p.y <= d for (a, b, c, d) in kut)
    trees = [Vector((o.location.x, o.location.y)) for o in yc.objects]
    cnt = {}
    def yer(kind, p, s=(0.85, 1.25), yaw=None):
        z = height(p.x, p.y)
        nm = "Yer_%s_D%04d" % (kind, sum(cnt.values()))
        e = empty(nm, yc, loc=(p.x, p.y, z - 0.03), rot=(0, 0, yaw if yaw is not None else rnd.uniform(0, 360)), kind="SINGLE_ARROW", size=0.6)
        sc = rnd.uniform(*s)
        e.scale = (sc, sc, sc)
        cnt[kind] = cnt.get(kind, 0) + 1
    def zemin_ok(p, road_min=0.35):
        if in_box(p):
            return False
        if (p - MEYDAN).length < MEYDAN_R + 0.4:
            return False
        if poly_dist(p, STREAM) < STREAM_HALF - 0.6:
            return False
        if road_info(p)[0] < road_min:
            return False
        if abs(p.x - ATOLYE.x) < ATOLYE_HALF[0] - 1.5 and abs(p.y - ATOLYE.y) < ATOLYE_HALF[1] - 1.5:
            return False
        return True
    # 1) yol kenarları: çimen öbekleri + çakıl, yer yer çiçek (göz hizasında zemin boş kalmaz)
    for n, pts, w in ROADS:
        rs = resample(pts, 1.0)
        for i in range(1, len(rs) - 1):
            d = (rs[i + 1] - rs[i - 1]).normalized()
            nrm = Vector((-d.y, d.x))
            for s in (-1, 1):
                if rnd.random() < 0.55:
                    q = rs[i] + nrm * s * (w / 2 + rnd.uniform(0.35, 1.8)) + d * rnd.uniform(-0.4, 0.4)
                    if zemin_ok(q):
                        r = rnd.random()
                        yer("Cimen_%d" % rnd.randint(1, 3) if r < 0.7 else ("Cakil" if r < 0.85 else "Cicek_%d" % rnd.randint(1, 3)), q)
    # 2) çiçek tarhları: köy zeminlerinin kenarı, meydan çevresi, ev önleri (öbek öbek)
    merkezler = []
    for c, r in ((DENK + Vector((1, -5)), 14.0), (TEPK + Vector((-3, -2)), 12.5), (MEYDAN, MEYDAN_R + 1.6)):
        for k in range(18):
            a = 2 * math.pi * k / 18 + rnd.uniform(-0.1, 0.1)
            merkezler.append(c + Vector((math.cos(a), math.sin(a))) * (r + rnd.uniform(-0.5, 1.5)))
    for k in range(70):
        merkezler.append(Vector((rnd.uniform(-60, 80), rnd.uniform(-35, 30))))
    for c in merkezler:
        tur = "Cicek_%d" % rnd.randint(1, 3)
        for j in range(rnd.randint(4, 9)):
            q = c + Vector((rnd.uniform(-1.4, 1.4), rnd.uniform(-1.4, 1.4)))
            if zemin_ok(q, 0.6):
                yer(tur if rnd.random() < 0.7 else "Cimen_%d" % rnd.randint(1, 3), q, s=(0.8, 1.2))
    # 3) vadi tabanı: seyrek çimen öbekleri (düz yeşil boşluk kalmasın) + ağaç diplerinde öbek
    for k in range(900):
        q = Vector((rnd.uniform(-66, 84), rnd.uniform(-40, 32)))
        if play_outside(q.x, q.y) < 0 and zemin_ok(q, 1.0):
            yer("Cimen_%d" % rnd.randint(1, 3), q, s=(0.8, 1.4))
    for t in trees[::2]:
        for j in range(2):
            q = t + Vector((rnd.uniform(-1.2, 1.2), rnd.uniform(-1.2, 1.2)))
            if zemin_ok(q, 0.8):
                yer("Cimen_%d" % rnd.randint(1, 3), q, s=(1.0, 1.5))
    # 4) dere: iki kıyıda saz + kavak dizisi (Anadolu'da derenin "yeşil vahası", detay.md #3)
    sr = resample(STREAM, 1.0)
    for i in range(1, len(sr) - 1):
        d = (sr[i + 1] - sr[i - 1]).normalized()
        nrm = Vector((-d.y, d.x))
        for s in (-1, 1):
            if rnd.random() < 0.45:
                q = sr[i] + nrm * s * rnd.uniform(STREAM_HALF - 2.2, STREAM_HALF - 0.6)
                if not in_box(q) and road_info(q)[0] > 0.8 and (q - KOPRU).length > 7.5:
                    yer("Saz", q, s=(0.8, 1.3))
        if i % 9 == 0:
            s = 1 if (i // 9) % 2 else -1
            q = sr[i] + nrm * s * (STREAM_HALF + rnd.uniform(1.5, 2.6))
            if zemin_ok(q, 2.0) and play_outside(q.x, q.y) < 6 and all((q - t).length > 3.0 for t in trees):
                yer("Kavak", q, s=(0.85, 1.15))
    # 5) kavak sırası: Matematik yolunun vadideki ilk bölümü (uzaktan çizgi gibi okunur)
    pts, w = next((pp, ww) for (nn, pp, ww) in ROADS if nn == "Yol_Matematik")
    rs = [p for p in resample(pts, 1.0) if p.y < 30]
    for i in range(6, len(rs), 8):
        d = (rs[min(i + 1, len(rs) - 1)] - rs[i - 1]).normalized()
        for s in (-1, 1):
            q = rs[i] + Vector((-d.y, d.x)) * s * (w / 2 + 2.8)
            if zemin_ok(q, 1.5):
                yer("Kavak", q, s=(0.8, 1.0))
    # 6) meyve bahçesi (Denkleştirme güneyi) + arı kovanları yanında
    x0, y0, nc, nr, sp = BAHCE_MEYVE
    for i in range(nc):
        for j in range(nr):
            q = Vector((x0 + (i - (nc - 1) / 2) * sp + rnd.uniform(-0.3, 0.3), y0 + (j - (nr - 1) / 2) * sp + rnd.uniform(-0.3, 0.3)))
            if zemin_ok(q, 1.0):
                yer("Meyve", q, s=(0.85, 1.1))
    # 7) iri kaya kümeleri: vadi basamağının dibinde, aralıklı (sınırı ve manzarayı süsler)
    for k in range(260):
        q = Vector((rnd.uniform(X0 + 25, X1 - 25), rnd.uniform(Y0 + 25, Y1 - 20)))
        if -1.0 < play_outside(q.x, q.y) < 8.0 and zemin_ok(q, 4.0) and rnd.random() < 0.5:
            if all((q - t).length > 2.5 for t in trees):
                yer("Kaya_Kitle_%d" % rnd.randint(1, 2), q, s=(0.8, 1.3))
    print("Detay yerleşimi:", cnt)

# ------------------------------------------------------------------ dışa aktarma ek
def export_harita_tum():
    """export_harita + Harita_Detay koleksiyonu Harita.fbx'e eklenir."""
    base = list(bpy.data.collections[COL].all_objects)
    for cn in ("Harita_Yapilar", "Harita_Yon", DETAY_COL):
        if bpy.data.collections.get(cn):
            base += list(bpy.data.collections[cn].all_objects)
    out = [_fbx(os.path.join(EXPORT_DIR, "Harita.fbx"), base)]
    models = list(bpy.data.collections[DOGA_COL].objects)
    roots = [o for o in models if o.parent is None]
    keep = {o.name: tuple(o.location) for o in roots}
    for o in roots:
        o.location = (0, 0, 0)
    try:
        out.append(_fbx(os.path.join(EXPORT_DIR, "Harita_Doga.fbx"), models))
    finally:
        for o in roots:
            o.location = keep[o.name]
    out.append(_fbx(os.path.join(EXPORT_DIR, "Harita_Yerler.fbx"), list(bpy.data.collections[YER_COL].objects)))
    return out
