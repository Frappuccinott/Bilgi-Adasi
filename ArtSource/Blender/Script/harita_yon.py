# Kasaba haritası — yön bulma: köy kimlik rengi + simgesi, yön tabelaları, köy giriş kemerleri, mil taşları,
# yol ağaçları, köy zemini ve meydanda harita kabartması.
# Referans: ArtSource/Referans/yon_bulma.md (Lynch: yol, kenar, bölge, düğüm, simge). Önce harita_yapi.py (bu dosya yükler).
# Yazı mesh olmaz (09 §7): tabelalarda simge mesh, yazı için Unity'de TMP bağlanacak "Tabela_Yazi_*" boş objeleri.
# Yarışma süresince yazıldı (0'dan üret). Blender 5.x.
#
#   g = {"__name__": "harita_yon"}; exec(compile(open(p, encoding="utf-8").read(), p, "exec"), g)
#   g["build_yon"]()

import os
_yp = os.path.join(r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\ArtSource\Blender\Script", "harita_yapi.py")
exec(compile(open(_yp, encoding="utf-8").read(), _yp, "exec"), globals())

YON_COL = "Harita_Yon"

def put_yon(*a, **k):
    """harita_yapi.put ile aynı; obje Harita_Yon koleksiyonuna gider (yeniden kurulumda çoğalmasın)."""
    ob = put(*a, **k)
    for c in list(ob.users_collection):
        c.objects.unlink(ob)
    collection(YON_COL).objects.link(ob)
    return ob

# ------------------------------------------------------------------ kimlik (bölge rengi + simge)
KIMLIK = {
    "Denk":   {"renk": lambda t=0.5: mix(pal("altin", 1), pal("altin", 0), t), "simge": "terazi"},
    "Tepk":   {"renk": lambda t=0.5: mix(pal("gok", 2), pal("gok", 1), t), "simge": "guvercin"},
    "Atolye": {"renk": lambda t=0.5: mix(pal("bakir", 1), pal("bakir", 0), t), "simge": "sise"},
    "Tarla":  {"renk": lambda t=0.5: mix(pal("cimen", 1), pal("cimen", 0), t), "simge": "basak"},
    "Kilitli": {"renk": lambda t=0.5: mix(pal("kaya", 1), pal("kaya", 0), t), "simge": "kilit"},
    # kilitli şehirler (CLAUDE.md §1): kapıya kadar bisikletle gidilir, içleri yok
    "Mat":    {"renk": lambda t=0.5: mix(pal("dag", 2), pal("dag", 1), t), "simge": "ucgen"},
    "Tur":    {"renk": lambda t=0.5: mix(pal("cati", 2), pal("cati", 1), t), "simge": "kitap"},
}
MUREKKEP = lambda: mix(pal("ahsap", 2), pal("dag", 2), 0.35)      # oyma/boya simge rengi (siyah değil)

# ------------------------------------------------------------------ simgeler (u, z düzleminde, y kalınlığı ortada)
def _plate(poly, th):
    b = bm_wall(poly, [], th)
    transform_bm(b, Matrix.Translation((0, -th / 2, 0)))
    return b

def _rect(cx, cz, w, h):
    return [(cx - w / 2, cz - h / 2), (cx + w / 2, cz - h / 2), (cx + w / 2, cz + h / 2), (cx - w / 2, cz + h / 2)]

def _disc(cx, cz, r, n=10, a0=0.0, a1=2 * math.pi):
    return [(cx + math.cos(a0 + (a1 - a0) * i / n) * r, cz + math.sin(a0 + (a1 - a0) * i / n) * r) for i in range(n + 1)]

def simge_bm(kind, size=0.5, th=0.1, color=None):
    s = size
    parts = []
    if kind == "terazi":
        parts.append(_plate(_rect(0, -0.38 * s, 0.42 * s, 0.07 * s), th))
        parts.append(_plate(_rect(0, -0.08 * s, 0.07 * s, 0.56 * s), th))
        parts.append(_plate(_rect(0, 0.2 * s, 0.86 * s, 0.06 * s), th))
        for sx in (-1, 1):
            parts.append(_plate(_rect(sx * 0.4 * s, 0.07 * s, 0.025 * s, 0.24 * s), th))
            pan = [(sx * 0.4 * s - 0.17 * s, -0.05 * s)] + [(sx * 0.4 * s + math.cos(math.pi + math.pi * i / 6) * 0.17 * s,
                                                            -0.05 * s + math.sin(math.pi + math.pi * i / 6) * 0.11 * s) for i in range(7)]
            parts.append(_plate(pan, th))
        parts.append(_plate(_disc(0, 0.27 * s, 0.05 * s, 8), th))
    elif kind == "guvercin":
        body = [(-0.42, 0.02), (-0.2, 0.08), (0.05, 0.1), (0.22, 0.16), (0.33, 0.2), (0.42, 0.17), (0.36, 0.11),
                (0.24, 0.05), (0.12, -0.06), (-0.08, -0.1), (-0.24, -0.08), (-0.46, -0.16), (-0.4, -0.04)]
        wing = [(-0.12, 0.06), (0.0, 0.42), (0.1, 0.48), (0.08, 0.3), (0.16, 0.08)]
        parts.append(_plate([(x * s, z * s) for (x, z) in body], th))
        parts.append(_plate([(x * s, z * s) for (x, z) in wing], th))
    elif kind == "sise":
        body = [(0.0, -0.42)]
        for i in range(13):
            a = -math.pi / 2 + (2 * math.pi - 0.9) * (i / 12) * (1 if True else 1)
        pts = [(-0.06, 0.42), (0.06, 0.42), (0.06, 0.08)] + \
              [(math.cos(a) * 0.3, -0.16 + math.sin(a) * 0.3) for a in [math.radians(d) for d in (70, 30, 0, -30, -60, -90, -120, -150, -180, -210, 110)]] + \
              [(-0.06, 0.08)]
        parts.append(_plate([(x * s, z * s) for (x, z) in pts], th))
        parts.append(_plate(_rect(0, 0.45 * s, 0.2 * s, 0.06 * s), th))
    elif kind == "basak":
        parts.append(_plate(_rect(0, -0.12 * s, 0.05 * s, 0.6 * s), th))
        for i in range(4):
            for sx in (-1, 1):
                cz = (-0.05 + i * 0.13) * s
                g = [(0, cz), (sx * 0.1 * s, cz + 0.04 * s), (sx * 0.16 * s, cz + 0.12 * s), (sx * 0.06 * s, cz + 0.09 * s)]
                parts.append(_plate(g if sx > 0 else list(reversed(g)), th))
        parts.append(_plate([(-0.04 * s, 0.47 * s), (0.04 * s, 0.47 * s), (0, 0.6 * s)], th))
    elif kind == "ucgen":                                   # Matematik: üçgen + iç teğet çember
        tri = [(0.0, 0.42), (-0.46, -0.36), (0.46, -0.36)]
        inner = [(0.0, 0.22), (-0.29, -0.26), (0.29, -0.26)]
        b = bm_wall([(x * s, z * s) for (x, z) in tri], [[(x * s, z * s) for (x, z) in inner]], th)
        transform_bm(b, Matrix.Translation((0, -th / 2, 0)))
        parts.append(b)
        parts.append(_plate(_disc(0, -0.1 * s, 0.13 * s, 10), th))
    elif kind == "kitap":                                   # Türkçe: açık kitap + kalem (kamış)
        for sx in (-1, 1):
            pg = [(0, -0.28), (sx * 0.44, -0.2), (sx * 0.44, 0.22), (0, 0.14)]
            parts.append(_plate([(x * s, z * s) for (x, z) in (pg if sx > 0 else list(reversed(pg)))], th))
        quill = [(0.08, -0.05), (0.42, 0.46), (0.36, 0.48), (0.04, -0.02)]
        q = _plate([(x * s, z * s) for (x, z) in quill], th * 1.4)
        parts.append(q)
    elif kind == "kilit":
        parts.append(_plate(_rect(0, -0.15 * s, 0.5 * s, 0.42 * s), th))
        arc = [(math.cos(math.radians(a)) * 0.17 * s, 0.06 * s + math.sin(math.radians(a)) * 0.2 * s) for a in range(0, 181, 20)]
        parts.append(tube_pts([(x, 0, z) for (x, z) in arc], 0.035 * s, sides=5))
        parts.append(_plate(_disc(0, -0.12 * s, 0.05 * s, 8), th * 1.3))
    m_ = merge(parts)
    flat(m_, color or MUREKKEP())
    return m_

# ------------------------------------------------------------------ yön tabelası (fingerpost)
def kol_bm(kim, L=1.5, h=0.36, th=0.07):
    """Ok biçimli tabela kolu, yerel +X'e bakar (x=0 direkte). Uç bölümü hedefin renginde, ortada simge."""
    poly = [(0.0, -h / 2), (L - 0.2, -h / 2), (L, 0.0), (L - 0.2, h / 2), (0.0, h / 2)]
    b = _plate(poly, th)
    rc = KIMLIK[kim]["renk"]
    def fn(f, l):
        u = l.vert.co.x
        if u > L * 0.62:
            return rc(0.45)
        return mix(pal("ahsap", 1), pal("ahsap", 0), 0.55)
    b.faces.index_update()
    paint_by(b, fn)
    em = simge_bm(KIMLIK[kim]["simge"], size=0.32, th=th + 0.025)
    transform_bm(em, Matrix.Translation((L * 0.36, 0, 0)))
    parts = [b, em]
    for sx in (L * 0.08,):
        bolt = box(0.04, th + 0.03, 0.04, x=sx, z=-0.02, origin="center")
        flat(bolt, demir(0.85))
        parts.append(bolt)
    return merge(parts)

def tabela_bm(hedefler, seed=0):
    """hedefler: [(kimlik, yön açısı(rad, dünya), etiket)] — direk yerelde (0,0). Dönen: (bm, yazı çapaları)."""
    rnd = random.Random(seed)
    parts = []
    base = box(0.5, 0.5, 0.5, z=-0.25, bevel=0.05)
    jitter(base, 0.02, seed=seed)
    grad(base, "kaya", 2, 0)
    parts.append(base)
    post = box(0.16, 0.16, 2.75, z=0.2, bevel=0.02)
    grad(post, "ahsap", 2, 1)
    parts.append(post)
    cap = bm_cyl(0.16, 0.02, 0.22, sides=4)
    transform_bm(cap, M(0, 0, 2.95, yaw=math.pi / 4))
    grad(cap, "cati", 2, 1)
    parts.append(cap)
    anchors = []
    for i, (kim, ang, et) in enumerate(hedefler):
        z = 2.45 - i * 0.38
        k = kol_bm(kim)
        transform_bm(k, M(0.08 * 0, 0, z, yaw=ang + rnd.uniform(-0.03, 0.03), rx=rnd.uniform(-0.03, 0.03)))
        parts.append(k)
        anchors.append((et, Vector((math.cos(ang) * 0.7, math.sin(ang) * 0.7, z)), ang))
    return merge(parts), anchors

# ------------------------------------------------------------------ köy giriş kemeri
def giris_kemeri_bm(kim, width=6.2, seed=0):
    """Yerel: yol +Y boyunca geçer, kemer X ekseninde yolu aşar. Ortada asılı simge levhası, direklerde flama."""
    rc = KIMLIK[kim]["renk"]
    parts = []
    H = 4.1
    for sx in (-1, 1):
        x = sx * width / 2
        ft = box(0.75, 0.75, 0.7, x=x, z=-0.3, bevel=0.05)
        jitter(ft, 0.02, seed=seed + sx)
        grad(ft, "kaya", 2, 0)
        parts.append(ft)
        p = box(0.32, 0.32, H, x=x, z=0.35, bevel=0.03)
        grad(p, "ahsap", 2, 1)
        parts.append(p)
        br = box(0.14, 0.14, 1.25, origin="center")
        transform_bm(br, M(x - sx * 0.42, 0, H - 0.15, ry=sx * math.radians(45)))
        flat(br, pal("ahsap", 2))
        parts.append(br)
        # direkte köy renginde uzun flama (yoldan gelene bakar: iki yüz)
        for fy in (-0.2, 0.2):
            fl = bm_prism([(-0.22, 0), (-0.22, -1.7), (0, -1.45), (0.22, -1.7), (0.22, 0)], -0.015, 0.015)
            paint_by(fl, lambda f, l: rc(0.6) if abs(l.vert.co.x) < 0.09 else rc(0.25))
            transform_bm(fl, M(x, fy * 1.0 + (0.0), H - 0.35, yaw=math.pi / 2 if False else 0.0))
            transform_bm(fl, Matrix.Translation((0, (0.18 if fy > 0 else -0.18), 0)))
            parts.append(fl)
    beam = box(width + 1.1, 0.3, 0.32, z=H + 0.35, bevel=0.03)
    grad(beam, "ahsap", 2, 1)
    parts.append(beam)
    rf, _, _, _ = roof_bm(width + 1.0, 0.75, H + 0.67, seed, ov_x=0.25, ov_e=0.3, pitch_deg=38, rows=2)
    parts.append(rf)
    # asılı levha: çerçeve (ahşap) + köy renginde yüz + simge (iki yanda)
    lz = H - 0.75
    for sx in (-0.45, 0.45):
        ch = box(0.03, 0.03, 0.55, x=sx, z=lz + 0.6)
        flat(ch, demir(0.85))
        parts.append(ch)
    fr = box(1.45, 0.1, 0.85, z=lz - 0.25, bevel=0.03)
    grad(fr, "ahsap", 2, 1)
    parts.append(fr)
    face = box(1.25, 0.13, 0.66, z=lz - 0.16)
    flat(face, rc(0.55))
    parts.append(face)
    em = simge_bm(KIMLIK[kim]["simge"], size=0.62, th=0.17)
    transform_bm(em, Matrix.Translation((0, 0, lz + 0.16)))
    parts.append(em)
    return merge(parts), Vector((0, 0, lz + 0.17))

# ------------------------------------------------------------------ mil taşı (yol boyu kırıntı izi)
def mil_tasi_bm(kim, seed):
    rnd = random.Random(seed)
    st = bm_lathe(chaikin([(0.22, -0.2), (0.24, 0.3), (0.21, 0.62), (0.12, 0.78), (0.0, 0.8)], 1), sides=8)
    transform_bm(st, Matrix.Scale(0.75, 4, (1, 0, 0)))
    jitter(st, 0.012, seed=seed)
    rc = KIMLIK[kim]["renk"]
    def fn(f, l):
        z = l.vert.co.z
        if z > 0.6:
            return rc(0.5)                       # boyalı tepe: gidilen köyün rengi
        return mix(pal("kaya", 1), pal("kaya", 0), smooth01((z + 0.2) / 0.9))
    paint_by(st, fn)
    em = simge_bm(KIMLIK[kim]["simge"], size=0.22, th=0.02)
    transform_bm(em, M(0, -0.165, 0.38))
    return merge([st, em])

# ------------------------------------------------------------------ köy zemini (bölgeyi okutur)
def koy_zemin_bm(c, rx, ry, yaw, seed):
    rnd = random.Random(seed)
    bm = bmesh.new()
    lay = bm.loops.layers.float_color.new("Col")
    # sık halka: seyrek halkada arazi üçgenleri zeminin içinden çıkıyordu (z-fighting, denetim 13.11)
    rings = [0.0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.88, 0.95, 1.0, 1.05]     # kısa, dik etek: arazi ile kesişme bandı dar
    segs = 56
    cs, sn = math.cos(yaw), math.sin(yaw)
    vs = []
    for k, r in enumerate(rings):
        ring = []
        n = 1 if r == 0 else segs
        for i in range(n):
            a = 2 * math.pi * i / segs
            wob = 1.0 + 0.09 * n2(math.cos(a) * 5, math.sin(a) * 5, 1.5, seed)
            u, v = math.cos(a) * rx * r * wob, math.sin(a) * ry * r * wob
            x, y = c.x + u * cs - v * sn, c.y + u * sn + v * cs
            # yükseklik arazi AĞINDAN (zemin_ust): fonksiyona göre konunca ağ zeminin içinden çıkıyordu (656 nokta,
            # 13.11). Yan yollar 0.042, ana yollar 0.062'de → zemin (0.03) hep yolların altında kalır.
            if r < 1.0:
                zz_ = zemin_ust(x, y) + 0.03
            elif r == 1.0:
                zz_ = zemin_ust(x, y) + 0.02
            else:
                zz_ = min(height(x, y), arazi_z(x, y)) - 0.09
            ring.append(bm.verts.new((x - c.x, y - c.y, zz_ - height(c.x, c.y))))
        vs.append(ring)
    def col_at(v, k):
        p = Vector((v.co.x + c.x, v.co.y + c.y))
        blob = 0.5 + 0.5 * n2(p.x, p.y, 3.5, seed)
        base = mix(mix(pal("kil", 1), pal("cimen", 2), 0.35), pal("kil", 0), blob * 0.6)
        edge = smooth01((rings[k] - 0.8) / 0.32)
        return mix(base, mix(pal("cimen", 1), pal("cimen", 2), 0.4), edge)
    for k in range(len(rings) - 1):
        A, B = vs[k], vs[k + 1]
        for i in range(segs):
            j = (i + 1) % segs
            f = bm.faces.new((A[0], B[i], B[j])) if len(A) == 1 else bm.faces.new((A[i], B[i], B[j], A[j]))
            for l in f.loops:
                kk = next(q for q in range(len(vs)) if l.vert in vs[q])
                l[lay] = col_at(l.vert, kk)
    bm.normal_update()
    for f in bm.faces:
        if f.normal.z < 0:
            f.normal_flip()
    return bm

# ------------------------------------------------------------------ harita kabartması (meydanda masa)
MAP_X0, MAP_X1, MAP_Y0, MAP_Y1 = -72.0, 84.0, -40.0, 30.0
MAP_S = 1.0 / 66.0          # 156 m → 2.36 m
MAP_ZX = 1.6                 # dikey abartma

def harita_kabartma_bm(binalar, agaclar):
    parts = []
    W = (MAP_X1 - MAP_X0) * MAP_S
    D = (MAP_Y1 - MAP_Y0) * MAP_S
    top = 0.88
    ped = box(W + 0.34, D + 0.34, top + 0.15, bevel=0.05)
    transform_bm(ped, Matrix.Translation((0, 0, -0.2)))
    jitter(ped, 0.015, seed=5)
    grad(ped, "kaya", 2, 0)
    parts.append(ped)
    rim = box(W + 0.22, D + 0.22, 0.12, z=top - 0.12, bevel=0.02)
    grad(rim, "ahsap", 2, 1)
    parts.append(rim)
    step = 2.5
    nx = int((MAP_X1 - MAP_X0) / step) + 1
    ny = int((MAP_Y1 - MAP_Y0) / step) + 1
    bm = bmesh.new()
    lay = bm.loops.layers.float_color.new("Col")
    def mp(x, y, z):
        return Vector(((x - (MAP_X0 + MAP_X1) / 2) * MAP_S, (y - (MAP_Y0 + MAP_Y1) / 2) * MAP_S,
                       top + 0.01 + max(-1.5, min(z, 16.0)) * MAP_S * MAP_ZX))
    grid = []
    cols = []
    for j in range(ny):
        row, crow = [], []
        for i in range(nx):
            x, y = MAP_X0 + i * step, MAP_Y0 + j * step
            h = height(x, y)
            hx = height(x + 1, y) - h
            hy = height(x, y + 1) - h
            nz = 1.0 / math.sqrt(1 + hx * hx + hy * hy)
            p = Vector((x, y))
            c = terrain_color(x, y, h, nz)
            dr, _ = road_info(p)
            if dr < 0.6:
                c = pal("kil", 0)
            if poly_dist(p, STREAM) < 2.6:
                c = pal("su", 1)
            if (p - MEYDAN).length < MEYDAN_R:
                c = pal("kaya", 0)
            row.append(bm.verts.new(mp(x, y, h)))
            crow.append(c)
        grid.append(row)
        cols.append(crow)
    for j in range(ny - 1):
        for i in range(nx - 1):
            f = bm.faces.new((grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i]))
            for l, (jj, ii) in zip(f.loops, ((j, i), (j, i + 1), (j + 1, i + 1), (j + 1, i))):
                l[lay] = cols[jj][ii]
    parts.append(bm)
    # binalar: küçük gövde + turuncu çatı (çatı rengi = tek bakışta "ev")
    for (x, y, yaw, w, d, kim) in binalar:
        z = mp(x, y, height(x, y)).z
        b = box(w * MAP_S, d * MAP_S, 0.035, x=0, y=0, z=0)
        flat(b, pal("siva", 0))
        r = bm_prism([(-d * MAP_S * 0.55, 0), (d * MAP_S * 0.55, 0), (0, 0.03)], -w * MAP_S * 0.55, w * MAP_S * 0.55)
        for v in r.verts:
            v.co.x, v.co.y = v.co.y, v.co.x
        bmesh.ops.recalc_face_normals(r, faces=r.faces)
        flat(r, pal("cati", 1))
        transform_bm(r, Matrix.Translation((0, 0, 0.035)))
        bb = merge([b, r])
        transform_bm(bb, M(mp(x, y, 0).x, mp(x, y, 0).y, z, yaw))
        parts.append(bb)
    for (x, y, s) in agaclar:
        t = _blob(Vector((0, 0, 0)), 0.045 * s, sub=1, squash=0.9, seed=int(x * 7 + y))
        flat(t, mix(pal("yaprak", 1), pal("yaprak", 2), 0.3))
        q = mp(x, y, height(x, y))
        transform_bm(t, Matrix.Translation((q.x, q.y, q.z + 0.035 * s)))
        parts.append(t)
    # köy bayrakları + "buradasın" iğnesi
    pins = []
    for kim, (x, y) in (("Denk", DENK), ("Tepk", TEPK), ("Atolye", ATOLYE), ("Tarla", Vector((75.0, 10.0)))):
        q = mp(x, y, height(x, y))
        pole = box(0.012, 0.012, 0.22, x=q.x, y=q.y, z=q.z)
        flat(pole, pal("ahsap", 2))
        fl = box(0.13, 0.008, 0.08, x=q.x + 0.065, y=q.y, z=q.z + 0.14)
        flat(fl, KIMLIK[kim]["renk"](0.5))
        em = simge_bm(KIMLIK[kim]["simge"], size=0.07, th=0.012)
        transform_bm(em, Matrix.Translation((q.x + 0.065, q.y, q.z + 0.18)))
        parts += [pole, fl]
        pins.append(em)
    for nm, p, d in KAPILAR:
        q = mp(p.x, p.y, height(p.x, p.y))
        b = _blob(Vector((q.x, q.y, q.z + 0.03)), 0.09, sub=1, squash=0.6, seed=3)
        flat(b, pal("gok", 0))
        parts.append(b)
    q = mp(MEYDAN.x, MEYDAN.y, height(MEYDAN.x, MEYDAN.y))
    pin = bm_lathe([(0.0, 0.0), (0.035, 0.06), (0.045, 0.1), (0.03, 0.14), (0.0, 0.15)], sides=8)
    transform_bm(pin, Matrix.Translation((q.x, q.y, q.z)))
    flat(pin, pal("cati", 0))
    parts.append(pin)
    return merge(parts + pins), Vector((0, -(D / 2 + 0.2), top - 0.05))

# ------------------------------------------------------------------ KİLİTLİ ŞEHİR KAPISI (Matematik / Türkçe)
def sehir_kapisi_bm(kim, seed):
    """Yerel: yol +Y boyunca dışarı gider; kapının ön yüzü -Y (oyuncuya). Geçidi iki kule + duvar kapatır."""
    rc = KIMLIK[kim]["renk"]
    rnd = random.Random(seed)
    parts = []
    T = 1.4                               # duvar kalınlığı
    # ana duvar: kemerli açıklık (kapı kanatları kapalı)
    outer = [(-12.0, -0.6), (12.0, -0.6), (12.0, 7.2), (-12.0, 7.2)]
    hole = pointed_arch(0, 4.2, 0.0, 3.6, 1.6, n=7)
    wall = bm_wall(outer, [hole], T)
    transform_bm(wall, Matrix.Translation((0, -T / 2, 0)))
    wall.faces.index_update()
    def wfn(f, l):
        c = f.calc_center_median()
        k = 0.93 + 0.12 * ((hash((int(c.x * 1.3), int(c.z * 1.8))) % 100) / 100.0)
        return tone(mix(pal("kaya", 1), pal("kaya", 0), 0.25 + 0.45 * smooth01(c.z / 7.0)), k)
    paint_by(wall, wfn)
    parts.append(wall)
    # mazgallı üst (ritmik dişler)
    for i in range(-11, 12, 2):
        m_ = box(1.1, T + 0.2, 0.75, x=i, z=7.2, bevel=0.05)
        grad(m_, "kaya", 1, 0)
        parts.append(m_)
    # kemer taşı halkası + kapı kanatları (kapalı, demir bant, kilit)
    # kemer halkası (ortası boş: kapı kanatları görünür; alt şerit zeminin altında kalır)
    ring = bm_wall(pointed_arch(0, 5.0, -0.35, 3.6, 1.95, n=9), [pointed_arch(0, 4.2, -0.25, 3.6, 1.6, n=9)], 0.17)
    transform_bm(ring, Matrix.Translation((0, -T / 2 - 0.14, 0)))
    flat(ring, rc(0.35))
    parts.append(ring)
    for sx in (-1, 1):
        lf = bm_prism([(0, 0), (sx * 2.08, 0), (sx * 2.08, 3.6), (0, 5.15)], -0.12, 0.12)
        if sx < 0:
            bmesh.ops.recalc_face_normals(lf, faces=lf.faces)
        transform_bm(lf, Matrix.Translation((0, -T / 2 + 0.3, 0)))
        paint_by(lf, lambda f, l: tone(mix(pal("ahsap", 2), pal("ahsap", 1), 0.3 + 0.4 * (int((l.vert.co.x + 5) / 0.3) % 2)), 1.0))
        parts.append(lf)
        for zz in (0.7, 2.0, 3.3):
            b = box(2.0, 0.06, 0.16, x=sx * 1.04, y=-T / 2 + 0.15, z=zz)
            flat(b, demir(0.8))
            parts.append(b)
    lk = simge_bm("kilit", size=0.7, th=0.16, color=demir(0.95))
    transform_bm(lk, Matrix.Translation((0, -T / 2 + 0.1, 2.0)))
    parts.append(lk)
    # kemerin üstünde şehir madalyonu (renk + simge)
    md = bm_cyl(1.25, 1.25, 0.22, sides=20)
    transform_bm(md, M(0, -T / 2 - 0.12, 6.0, rx=math.pi / 2))
    flat(md, rc(0.55))
    parts.append(md)
    mr = bm_lathe([(1.25, -0.02), (1.45, 0.0), (1.45, 0.24), (1.25, 0.26)], sides=20, close_top=False, close_bottom=False)
    transform_bm(mr, M(0, -T / 2 - 0.12, 6.0, rx=math.pi / 2))
    grad(mr, "bakir", 2, 0)
    parts.append(mr)
    em = simge_bm(KIMLIK[kim]["simge"], size=1.7, th=0.32, color=pal("siva", 0))
    transform_bm(em, Matrix.Translation((0, -T / 2 - 0.3, 6.0)))
    parts.append(em)
    # iki kule: Matematik → piramit tepe + küp/küre süs; Türkçe → kubbe + kitap yığını süs
    for sx in (-1, 1):
        x = sx * 6.2
        tw = bm_lathe([(2.1, -0.6), (2.0, 4.0), (1.9, 8.6), (2.2, 8.8), (2.2, 9.6), (1.9, 9.7)], sides=14, close_bottom=False)
        transform_bm(tw, Matrix.Translation((x, -0.2, 0)))
        grad(tw, "kaya", 2, 0, seed=seed + sx)
        parts.append(tw)
        for k in range(10):
            a = 2 * math.pi * k / 10
            mz = box(0.6, 0.5, 0.55, x=x + math.cos(a) * 2.0, y=-0.2 + math.sin(a) * 2.0, z=9.6, yaw=a)
            grad(mz, "kaya", 1, 0)
            parts.append(mz)
        sl = box(0.35, 0.3, 1.0, x=x, y=-0.2 - 2.0, z=5.6)
        flat(sl, mix(pal("ahsap", 2), pal("dag", 2), 0.5))
        parts.append(sl)
        if kim == "Mat":
            roof = bm_cyl(2.4, 0.05, 3.6, sides=4)
            transform_bm(roof, M(x, -0.2, 9.7, yaw=math.pi / 4))
            grad(roof, "dag", 2, 0)
            parts.append(roof)
            orn = box(0.55, 0.55, 0.55, x=x, y=-0.2, z=13.3, yaw=math.pi / 4, origin="center")
            transform_bm(orn, Matrix.Translation((0, 0, 0.27)))
            transform_bm(orn, M(0, 0, 0, rx=0.0))
            flat(orn, pal("altin", 0))
            parts.append(orn)
            sp = _blob(Vector((x, -0.2, 14.25)), 0.3, sub=1, jit=0.0)
            flat(sp, pal("altin", 0))
            parts.append(sp)
        else:
            dm = bm_lathe(chaikin([(2.3, 9.7), (2.2, 10.6), (1.6, 11.6), (0.7, 12.3), (0.0, 12.5)], 1), sides=14)
            transform_bm(dm, Matrix.Translation((x, -0.2, 0)))
            grad(dm, "cati", 2, 0)
            parts.append(dm)
            for k in range(3):                               # kitap yığını süs
                bk = box(0.9 - k * 0.12, 0.6, 0.18, x=x + rnd.uniform(-0.05, 0.05), y=-0.2, z=12.4 + k * 0.19, yaw=rnd.uniform(-0.3, 0.3))
                flat(bk, [pal("cati", 1), pal("dag", 1), pal("altin", 1)][k])
                parts.append(bk)
        # kulelerde şehir renginde uzun flama (yoldan gelene)
        fl = bm_prism([(-0.45, 0), (-0.45, -3.6), (0, -3.1), (0.45, -3.6), (0.45, 0)], -0.02, 0.02)
        paint_by(fl, lambda f, l: rc(0.65) if abs(l.vert.co.x) < 0.15 else rc(0.3))
        transform_bm(fl, Matrix.Translation((x, -2.35, 8.4)))
        parts.append(fl)
    return merge(parts)

def uzak_sehir_bm(kim, seed):
    """Kilitli şehrin uzaktan silueti (pusa kayan renk, gölge düşürmez — 09 §7 uzak manzara). Yerel: tepe merkezi."""
    rnd = random.Random(seed)
    parts = []
    far = lambda c, t=0.55: mix(c, pal("gok", 0), t)          # derinlik pusu
    base = bm_lathe(chaikin([(46.0, -6.0), (40.0, 4.0), (30.0, 9.0), (0.0, 10.5)], 2), sides=20)
    paint_by(base, lambda f, l: far(mix(pal("dag", 1), pal("cimen", 1), 0.35), 0.45))
    parts.append(base)
    n = 13
    for i in range(n):
        a = rnd.uniform(0, 2 * math.pi)
        r = rnd.uniform(0, 22) if i else 0.0
        x, y = math.cos(a) * r, math.sin(a) * r
        h = rnd.uniform(14, 32) * (1.6 if i == 0 else 1.0)
        w = rnd.uniform(3.5, 6.5)
        z0 = 9.0
        if kim == "Mat":
            kind = ["silindir", "kup", "piramit", "kure"][i % 4] if i else "kubbe_kule"
            if kind == "silindir":
                b = bm_cyl(w * 0.5, w * 0.45, h, sides=10)
                tp = bm_cyl(w * 0.55, 0.1, w * 0.9, sides=10)
                transform_bm(tp, Matrix.Translation((0, 0, h)))
                b = merge([b, tp])
            elif kind == "kup":
                b = merge([box(w, w, h * 0.7), box(w * 0.7, w * 0.7, h * 0.3, z=h * 0.7)])
            elif kind == "piramit":
                b = bm_cyl(w * 0.9, 0.05, h * 0.8, sides=4)
            elif kind == "kure":
                b = merge([bm_cyl(w * 0.35, w * 0.35, h * 0.6, sides=8), _blob(Vector((0, 0, h * 0.6 + w * 0.5)), w * 0.6, sub=2, jit=0.0)])
            else:
                b = merge([bm_cyl(w * 0.6, w * 0.5, h, sides=12), _blob(Vector((0, 0, h)), w * 0.62, sub=2, squash=0.9, jit=0.0),
                           bm_cyl(0.3, 0.05, 4.0, sides=6)])
                transform_bm(b, Matrix.Identity(4))
            roofc = pal("dag", 0)
        else:
            kind = ["kitap", "kubbe", "kitap", "kalem"][i % 4] if i else "kalem_buyuk"
            if kind == "kitap":
                bl = []
                z = 0.0
                for k in range(int(h / 3)):
                    th_ = rnd.uniform(2.2, 3.4)
                    bl.append(box(w * rnd.uniform(0.9, 1.2), w * 0.75, th_, z=z, yaw=rnd.uniform(-0.25, 0.25)))
                    z += th_
                b = merge(bl)
            elif kind == "kubbe":
                b = merge([bm_cyl(w * 0.6, w * 0.6, h * 0.5, sides=12), _blob(Vector((0, 0, h * 0.5)), w * 0.65, sub=2, squash=1.1, jit=0.0)])
            else:
                hh = h * (1.0 if kind == "kalem" else 1.2)
                b = merge([bm_cyl(w * 0.35, w * 0.32, hh, sides=8), bm_cyl(w * 0.4, 0.05, w * 1.4, sides=8)])
                transform_bm(b, Matrix.Identity(4))
                tip = bm_cyl(w * 0.4, 0.05, w * 1.4, sides=8)
                transform_bm(tip, Matrix.Translation((0, 0, hh)))
                b = merge([bm_cyl(w * 0.35, w * 0.32, hh, sides=8), tip])
            roofc = pal("cati", 0)
        zs = [v.co.z for v in b.verts]
        top = max(zs)
        paint_by(b, lambda f, l, top=top, roofc=roofc: far(mix(pal("siva", 1), pal("siva", 0), l.vert.co.z / top)
                                                      if l.vert.co.z < top * 0.82 else roofc, 0.5))
        transform_bm(b, Matrix.Translation((x, y, z0)))
        parts.append(b)
    return merge(parts)

# ------------------------------------------------------------------ GEZİNTİ NOKTALARI
def degirmen_bm(seed=1201):
    """Yel değirmeni: taş gövde + konik çatı (sabit). Kanatlar ayrı (Degirmen_Kanat, pivot göbekte, yerel Y ekseninde döner)."""
    parts = []
    tw = bm_lathe(chaikin([(2.6, -0.5), (2.5, 2.0), (2.15, 5.0), (1.85, 7.2)], 1), sides=14, close_bottom=False)
    paint_by(tw, lambda f, l: mix(pal("siva", 1), pal("siva", 0), smooth01(l.vert.co.z / 7.2)))
    parts.append(tw)
    band = bm_lathe([(2.62, -0.5), (2.62, 0.6), (2.55, 0.7)], sides=14, close_top=False, close_bottom=False)
    grad(band, "kaya", 2, 1)
    parts.append(band)
    rf = bm_lathe(chaikin([(2.25, 7.1), (2.1, 7.6), (1.4, 8.8), (0.4, 9.7), (0.0, 9.9)], 1), sides=14)
    grad(rf, "cati", 2, 0)
    parts.append(rf)
    d = door_bm(1.0, 1.5, 0.4, base=0.0)
    transform_bm(d, Matrix.Translation((0, -2.5, 0)))
    parts.append(d)
    for z in (3.5, 5.6):
        wb = window_bm(0.55, 0.7)
        transform_bm(wb, M(0, -(2.5 - 0.11 * z) - 0.05, z))
        parts.append(wb)
    hub = bm_cyl(0.32, 0.28, 0.9, sides=8)
    transform_bm(hub, M(0, -1.6, 7.6, rx=math.pi / 2))
    grad(hub, "ahsap", 2, 1)
    parts.append(hub)
    return merge(parts), Vector((0, -2.45, 7.6))

def degirmen_kanat_bm():
    parts = []
    hub = bm_lathe([(0.0, -0.2), (0.38, -0.2), (0.42, 0.05), (0.2, 0.2), (0.0, 0.22)], sides=10)
    transform_bm(hub, Matrix.Rotation(math.pi / 2, 4, "X"))
    grad(hub, "bakir", 2, 0)
    parts.append(hub)
    for k in range(4):
        a = math.pi / 4 + k * math.pi / 2
        arm = box(0.16, 0.14, 5.6, z=0.2)
        sail = box(1.15, 0.05, 4.4, x=0.62, y=0.06, z=1.1)
        flat(arm, pal("ahsap", 2))
        sail.faces.index_update()
        paint_by(sail, lambda f, l: mix(pal("siva", 0), pal("altin", 0), 0.12) if int((l.vert.co.z + 5) / 0.55) % 2 else pal("siva", 1))
        rails = [box(1.3, 0.08, 0.06, x=0.6, y=0.0, z=0.95 + i * 1.1) for i in range(5)]
        for r_ in rails:
            flat(r_, pal("ahsap", 1))
        bl = merge([arm, sail] + rails)
        transform_bm(bl, Matrix.Rotation(a, 4, "Y"))
        parts.append(bl)
    return merge(parts)

def saman_bm(seed):
    rnd = random.Random(seed)
    b = bm_lathe(chaikin([(1.25, -0.1), (1.35, 0.5), (1.1, 1.3), (0.55, 1.95), (0.0, 2.15)], 2), sides=12)
    jitter(b, 0.05, seed=seed)
    paint_by(b, lambda f, l: mix(pal("altin", 2), pal("altin", 0), smooth01(l.vert.co.z / 2.1) * 0.85))
    pole = bm_cyl(0.06, 0.05, 2.9, sides=6)
    flat(pole, pal("ahsap", 2))
    s = 0.8 + 0.35 * rnd.random()
    m_ = merge([b, pole])
    transform_bm(m_, Matrix.Scale(s, 4))
    return m_

def kuyu_bm(seed=1301):
    parts = []
    ring = bm_lathe([(0.95, -0.3), (0.95, 0.85), (0.72, 0.9), (0.72, -0.1)], sides=16, close_bottom=False)
    jitter(ring, 0.02, seed=seed)
    grad(ring, "kaya", 2, 0)
    parts.append(ring)
    wtr = bm_cyl(0.72, 0.72, 0.02, sides=16)
    transform_bm(wtr, Matrix.Translation((0, 0, 0.1)))
    flat(wtr, pal("su", 2))
    parts.append(wtr)
    for sx in (-1, 1):
        p = box(0.14, 0.14, 2.1, x=sx * 0.85, z=0.6)
        grad(p, "ahsap", 2, 1)
        parts.append(p)
    ax = tube_pts([(-0.95, 0, 1.95), (0.95, 0, 1.95)], 0.07, sides=6)
    grad(ax, "ahsap", 2, 1)
    parts.append(ax)
    rope = box(0.03, 0.03, 0.9, z=1.05)
    flat(rope, mix(pal("kil", 0), pal("altin", 2), 0.3))
    bucket = bm_lathe([(0.0, 0.0), (0.16, 0.0), (0.19, 0.28), (0.0, 0.28)], sides=8)
    transform_bm(bucket, Matrix.Translation((0, 0, 0.78)))
    grad(bucket, "ahsap", 2, 1)
    parts += [rope, bucket]
    rf, _, _, _ = roof_bm(2.2, 1.7, 2.6, seed, ov_x=0.15, ov_e=0.25, pitch_deg=40, rows=2)
    parts.append(rf)
    return merge(parts)

def bank_bm(seed):
    parts = []
    for sx in (-0.8, 0.8):
        lg = box(0.42, 0.42, 0.45, x=sx, z=-0.05, bevel=0.04)
        jitter(lg, 0.02, seed=seed)
        grad(lg, "kaya", 2, 0)
        parts.append(lg)
    seat = box(2.1, 0.5, 0.1, z=0.4, bevel=0.025)
    grad(seat, "ahsap", 1, 0)
    parts.append(seat)
    return merge(parts)

def bisiklet_park_bm(n=4):
    """Bisiklet park yeri: ahşap kiriş + demir U halkalar (bisiklet ön tekeri arasına girer)."""
    parts = []
    L = n * 0.85 + 0.4
    for sx in (-1, 1):
        p = box(0.14, 0.14, 0.75, x=sx * L / 2, z=-0.2)
        grad(p, "ahsap", 2, 1)
        parts.append(p)
    beam = box(L + 0.2, 0.16, 0.12, z=0.52, bevel=0.02)
    grad(beam, "ahsap", 1, 0)
    parts.append(beam)
    for i in range(n):
        x = -L / 2 + 0.6 + i * 0.85
        u = tube_pts([(x - 0.18, 0, -0.1), (x - 0.18, 0, 0.42), (x - 0.12, 0, 0.55), (x + 0.12, 0, 0.55), (x + 0.18, 0, 0.42),
                      (x + 0.18, 0, -0.1)], 0.025, sides=5)
        flat(u, demir(0.9))
        parts.append(u)
    sg = simge_bm("bisiklet", size=0.3, th=0.06) if False else None
    return merge(parts)

# ================================================================== KURULUM
TABELALAR = [  # (konum, [(kimlik, hedef konum, etiket)])
    (Vector((1.4, -4.6)), [("Atolye", ATOLYE, "Atolye"), ("Mat", Vector((14.0, 2.0)), "Matematik_Sehri"),
                           ("Denk", DENK, "Denklestirme"), ("Tepk", TEPK, "Tepkime_Turleri"),
                           ("Tur", Vector((-12.0, -16.0)), "Turkce_Sehri")]),
    (Vector((11.0, -5.4)), [("Mat", Vector((15.0, 2.0)), "Matematik_Sehri"), ("Denk", DENK, "Denklestirme"),
                            ("Atolye", MEYDAN, "Atolye")]),
    (Vector((-17.6, -9.6)), [("Tur", Vector((-22.0, -18.0)), "Turkce_Sehri"), ("Tepk", TEPK, "Tepkime_Turleri"),
                             ("Atolye", MEYDAN, "Atolye")]),
    (Vector((16.9, 40.0)), [("Mat", Vector((20.5, 60.0)), "Matematik_Sehri"), ("Atolye", Vector((17.0, 14.0)), "Atolye")]),
    (Vector((-20.8, -55.0)), [("Tur", Vector((-24.5, -70.0)), "Turkce_Sehri"), ("Atolye", Vector((-24.0, -34.0)), "Atolye")]),
    (Vector((57.6, -9.4)), [("Tarla", Vector((66.0, 7.0)), "Tarlalar"), ("Kilitli", Vector((72.0, -24.0)), "Hesaplamalar"),
                            ("Atolye", Vector((40.0, -7.0)), "Atolye")]),
    (Vector((-47.5, 1.8)), [("Tepk", Vector((-53.0, 9.0)), "Guvercin_Kulesi"), ("Kilitli", Vector((-58.0, 9.0)), "Tepkimeler"),
                            ("Atolye", Vector((-38.0, -3.0)), "Atolye")]),
]
KEMERLER = [("Denk", Vector((41.8, -7.0)), "Yol_Dogu"), ("Tepk", Vector((-33.5, -3.6)), "Yol_Bati")]
# (köy, yol, başlangıç, bitiş: meydandan uzaklık). Başlangıç meydan döşemesinin (R 11) dışında.
MIL_YOLLARI = [("Denk", "Yol_Dogu", 13.5, 24.0), ("Tepk", "Yol_Bati", 13.5, 30.0),
               ("Mat", "Yol_Matematik", 13.5, 86.0), ("Tur", "Yol_Turkce", 13.5, 82.0)]
ALLE_YOLLARI = [("Yol_Dogu", 12.0, 25.0), ("Yol_Bati", -32.0, -10.0)]

def _road(name):
    return next(pts for (n, pts, w) in ROADS if n == name), next(w for (n, pts, w) in ROADS if n == name)

def build_yon():
    col = collection(YON_COL)
    clear_collection(YON_COL)
    for me in list(bpy.data.meshes):
        if me.users == 0:
            bpy.data.meshes.remove(me)
    made = []
    # 1) tabelalar (düğümler)
    for i, (p, hed) in enumerate(TABELALAR):
        arms = [(kim, math.atan2(t.y - p.y, t.x - p.x), et) for (kim, t, et) in hed]
        bm, anchors = tabela_bm(arms, seed=40 + i)
        ob = put_yon(bm, "SM_Tabela_%d" % (i + 1), p.x, p.y, 0.0)
        made.append(ob)
        for (et, a, ang) in anchors:
            e = empty("Tabela_Yazi_%d_%s" % (i + 1, et), col, loc=tuple(ob.location + a), rot=(90, 0, math.degrees(ang)),
                      kind="SINGLE_ARROW", size=0.3)
            parent_keep(e, ob)
    # 2) köy giriş kemerleri (bölge sınırı)
    for kim, p, rname in KEMERLER:
        pts, w = _road(rname)
        q = min(resample(pts, 0.5), key=lambda v: (v - p).length)
        i = resample(pts, 0.5).index(q)
        rs = resample(pts, 0.5)
        d = (rs[min(i + 1, len(rs) - 1)] - rs[max(i - 1, 0)]).normalized()
        yaw = math.atan2(d.y, d.x) + math.pi / 2
        bm, ya = giris_kemeri_bm(kim, seed=60 + len(made))
        ob = put_yon(bm, "SM_Giris_Kemeri_" + kim, q.x, q.y, yaw, z=min(height(q.x + d.y * 3, q.y - d.x * 3), height(q.x - d.y * 3, q.y + d.x * 3)))
        made.append(ob)
        for s in (-1, 1):
            e = empty("Tabela_Yazi_Kemer_%s_%s" % (kim, "On" if s < 0 else "Arka"), col,
                      loc=tuple(ob.location + Vector((d.x * s * 0.1, d.y * s * 0.1, ya.z - 0.55))),
                      rot=(90, 0, math.degrees(math.atan2(d.y * s, d.x * s)) - 90), kind="SINGLE_ARROW", size=0.3)
            parent_keep(e, ob)
    # 3) mil taşları (kırıntı izi): yol boyunca, sağda, gidilen köyün renginde
    ms = []
    for kim, rname, a0, a1 in MIL_YOLLARI:
        pts, w = _road(rname)
        rs = resample(pts, 1.0)
        rs = [p for p in rs if a0 <= (p - MEYDAN).length <= a1 + 14]
        for k in range(0, len(rs), 11):
            p = rs[k]
            j = min(k + 1, len(rs) - 1)
            d = (rs[j] - rs[max(k - 1, 0)]).normalized()
            side = Vector((d.y, -d.x))                       # gidiş yönünün sağı (yol noktaları meydandan dışarı)
            q = p + side * (w / 2 + 0.9)
            if road_info(q)[0] < 0.4:
                continue
            if poly_dist(q, STREAM) < STREAM_HALF + 0.5:
                continue
            if (q - MEYDAN).length < MEYDAN_R + 1.5:
                continue
            b = mil_tasi_bm(kim, 900 + k)
            yaw = math.atan2(-side.x, side.y)                # simgeli yüz (yerel -Y) yola bakar
            transform_bm(b, M(q.x, q.y, height(q.x, q.y), yaw))
            ms.append(b)
    made.append(put_yon(merge(ms), "SM_Mil_Taslari", 0.0, 0.0, 0.0, z=0.0))
    # 4) köy zeminleri (bölge)
    zz = []
    for c, rx, ry, yaw, sd in ((DENK + Vector((1.0, -5.0)), 15.0, 11.5, 0.05, 1), (TEPK + Vector((-3.0, -2.0)), 13.0, 11.0, 0.0, 2)):
        b = koy_zemin_bm(c, rx, ry, yaw, sd)
        transform_bm(b, Matrix.Translation((c.x, c.y, height(c.x, c.y))))
        zz.append(b)
    zb = merge(zz)
    made.append(put_yon(zb, "SM_Koy_Zeminleri", 0.0, 0.0, 0.0, z=0.0, sharp=0))
    # 5) yol ağaçları (yolu çizer): düzenli aralık, iki yanda; doğa yerlerinden çakışanlar silinir
    yc = collection(YER_COL)
    rnd = random.Random(717)
    alle = []
    for rname, x0, x1 in ALLE_YOLLARI:
        pts, w = _road(rname)
        rs = [p for p in resample(pts, 1.0) if x0 <= p.x <= x1]
        for k in range(0, len(rs), 9):
            p = rs[k]
            d = (rs[min(k + 1, len(rs) - 1)] - rs[max(k - 1, 0)]).normalized()
            for s in (-1, 1):
                q = p + Vector((-d.y, d.x)) * s * (w / 2 + 3.4)
                if (q - MEYDAN).length < MEYDAN_R + 3 or poly_dist(q, STREAM) < STREAM_HALF + 1.5 or road_info(q)[0] < 2.0:
                    continue
                alle.append(q)
    for o in list(yc.objects):
        if any((Vector((o.location.x, o.location.y)) - q).length < 4.0 for q in alle):
            bpy.data.objects.remove(o, do_unlink=True)
    for i, q in enumerate(alle):
        e = empty("Yer_Agac_Yesil_2_A%03d" % i, yc, loc=(q.x, q.y, yer_z("Agac_Yesil_2", q.x, q.y)), rot=(0, 0, rnd.uniform(0, 360)),
                  kind="SINGLE_ARROW", size=1.0)
        s = rnd.uniform(0.72, 0.82)
        e.scale = (s, s, s)
    # 6) meydanda harita kabartması (atölyeden gelen yolun solunda, kuzey = dünya kuzeyi)
    bin_ = [(x, y, yaw, W, D, "Denk") for (x, y, yaw, W, D, H, sd) in DENK_EVLER] + \
           [(FIRIN[0] + 1.2, FIRIN[1], FIRIN[2], 8.0, 4.4, "Denk")] + \
           [(x, y, yaw, W, D, "Tepk") for (x, y, yaw, W, D, H, sd) in TEPK_EVLER] + \
           [(LONCA[0], LONCA[1], LONCA[2], 10.5, 7.0, "Tepk"), (KULE[0], KULE[1], 0.0, 4.6, 4.6, "Tepk"),
            (ATOLYE.x, ATOLYE.y, 0.0, 8.9, 7.4, "Atolye")]
    agac = [(o.location.x, o.location.y, o.scale.x) for o in yc.objects
            if "Agac" in o.name and MAP_X0 < o.location.x < MAP_X1 and MAP_Y0 < o.location.y < MAP_Y1][::2]
    hb, yazi = harita_kabartma_bm(bin_, agac)
    hp = Vector((-4.6, -1.0))
    ob = put_yon(hb, "SM_Meydan_Harita_Kabartmasi", hp.x, hp.y, 0.0, z=height(hp.x, hp.y))
    made.append(ob)
    e = empty("Tabela_Yazi_Harita", col, loc=tuple(ob.location + yazi), rot=(90, 0, 0), kind="SINGLE_ARROW", size=0.3)
    parent_keep(e, ob)
    # 7) kilitli şehir kapıları + pus + uzak siluetler (Matematik kuzey, Türkçe güney)
    kapi_q = []
    for kim, rname, sehir in (("Mat", "Yol_Matematik", "Matematik"), ("Tur", "Yol_Turkce", "Turkce")):
        gx, gy = GECIT_KAPI[rname]
        pts, w = _road(rname)
        rs = resample(pts, 0.5)
        q = min(rs, key=lambda v: (v - Vector((gx, gy))).length)
        i = rs.index(q)
        d = (rs[min(i + 2, len(rs) - 1)] - rs[max(i - 2, 0)]).normalized()      # dışarı yön
        yaw = math.atan2(-d.x, d.y)                                            # yerel +Y = dışarı
        ob = put_yon(sehir_kapisi_bm(kim, 70 + len(made)), "SM_Sehir_Kapisi_" + sehir, q.x, q.y, yaw, z=height(q.x, q.y) - 0.05)
        made.append(ob)
        kapi_q.append(q.copy())
        pb = pus_bm(len(sehir) * 31, n=9, spread=9.0, depth=14.0)
        transform_bm(pb, Matrix.Rotation(yaw, 4, "Z") @ Matrix.Translation((0, 6.5, 0)))   # kapının arkasında kalsın
        po = to_object(pb, "Pus_" + sehir, collection(YON_COL), loc=(q.x, q.y, height(q.x, q.y)), sharp_angle=0, mat=m_bulut())
        made.append(po)
        lz_ = empty("LockedZone_" + sehir, collection(YON_COL), loc=(q.x + d.x * 2.0, q.y + d.y * 2.0, height(q.x, q.y) + 2.0),
                    rot=(0, 0, math.degrees(yaw)), kind="CUBE", size=1.0)
        lz_.scale = (4.0, 1.0, 3.0)
        e = empty("Tabela_Yazi_Sehir_" + sehir, collection(YON_COL), loc=tuple(ob.location + Vector((-d.x * 1.0, -d.y * 1.0, 4.4))),
                  rot=(90, 0, math.degrees(yaw)), kind="SINGLE_ARROW", size=0.5)
        parent_keep(e, ob)
        far = q + d * 75.0
        uo = put_yon(uzak_sehir_bm(kim, 80 + len(made)), "SM_Uzak_Sehir_" + sehir, far.x, far.y, 0.0, z=14.0, sharp=50)
        made.append(uo)
    # 8) gezinti noktaları: yel değirmeni (tarlanın simgesi), saman yığınları, kuyu, banklar, bisiklet park yerleri
    dx, dy = 84.0, 9.0
    db, hubp = degirmen_bm()
    dyaw = math.atan2(-(MEYDAN.x - dx), (MEYDAN.y - dy)) + math.pi                # kapı ve kanatlar köye bakar
    ob = put_yon(db, "SM_Degirmen", dx, dy, dyaw, z=ground_min(dx, dy, 2.6, 2.6, 0))
    made.append(ob)
    kb = degirmen_kanat_bm()
    hp = Matrix.Rotation(dyaw, 4, "Z") @ hubp
    ko = put_yon(kb, "Degirmen_Kanat", dx + hp.x, dy + hp.y, dyaw, z=ob.location.z + hp.z)
    made.append(ko)
    sm = []
    # dördüncüsü (66, 20.5) Bereket Tarlası çitinin içindeydi → çitin batısına; ilk ikisi buğday tarlasının çit
    # köşesine giriyordu (Play 13.11) → tarlanın güneyine, açığa
    for i, (sx_, sy_) in enumerate(((75.0, -1.8), (77.6, -0.6), (82.0, 21.0), (45.4, 18.6))):
        b = saman_bm(1400 + i)
        transform_bm(b, M(sx_, sy_, height(sx_, sy_), rnd.uniform(0, 6)))
        sm.append(b)
    made.append(put_yon(merge(sm), "SM_Saman_Yiginlari", 0.0, 0.0, 0.0, z=0.0))
    kx, ky = -53.0, -5.4
    made.append(put_yon(kuyu_bm(), "SM_Tepk_Kuyu", kx, ky, 0.3, z=ground_min(kx, ky, 1.0, 1.0, 0)))
    bk = []
    for i, ang in enumerate((200, 255, 305)):
        a = math.radians(ang)
        bx, by = MEYDAN.x + math.cos(a) * 8.7, MEYDAN.y + math.sin(a) * 8.7
        b = bank_bm(1500 + i)
        transform_bm(b, M(bx, by, height(bx, by) + 0.12, math.atan2(-math.cos(a), math.sin(a)) + math.pi))
        bk.append(b)
    made.append(put_yon(merge(bk), "SM_Meydan_Banklar", 0.0, 0.0, 0.0, z=0.0))
    pk = []
    for (px, py, pyaw) in ((2.6, 5.8, math.pi / 2), (36.0, -11.4, 0.05), (-30.0, -0.4, math.pi + 0.08)):
        b = bisiklet_park_bm()
        transform_bm(b, M(px, py, height(px, py), pyaw))
        pk.append(b)
    made.append(put_yon(merge(pk), "SM_Bisiklet_Park_Yerleri", 0.0, 0.0, 0.0, z=0.0))
    # şehir yollarının ve gezinti noktalarının üstüne düşen doğa yerlerini temizle
    for o in list(yc.objects):
        pp = Vector((o.location.x, o.location.y))
        # şehir kapısı duvarına giren çalı (denetim 13.11); kapının arkasındaki yamaç ağaçları Play'de kapının
        # üstünde bitmiş gibi görünüyordu → kapı çevresi 16 m ağaçsız
        agac = any(k in o.name for k in ("Agac", "Cam", "Kavak", "Meyve"))
        if (pp - Vector((dx, dy))).length < 5.0 or (pp - Vector((kx, ky))).length < 3.0 or \
                any((pp - g).length < (16.0 if agac else 8.5) for g in kapi_q):
            bpy.data.objects.remove(o, do_unlink=True)
    tot = sum(tri_count([o]) for o in made)
    print("Yön bulma:", len(made), "obje,", tot, "üçgen;", len(alle), "yol ağacı")
    return made
