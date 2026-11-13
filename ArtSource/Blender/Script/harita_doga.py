# Kasaba haritası — doğa: ağaç, çalı, kaya modelleri ve vadiye dağıtım.
# Önce harita.py (yükseklik, yollar, dere bilgisi buradan okunur).
# Her tür bir kez modellenir (SM_*), sahneye "Yer_<Tür>_NNN" boş objeleriyle dağıtılır.
# Unity'de prefab + GPU instancing ile yerleştirilir (09 §7 "Kopyalar").
# Yarışma süresince yazıldı (0'dan üret). Blender 5.x.
#
#   p = r"<repo>/ArtSource/Blender/Script/harita_doga.py"
#   g = {"__name__": "harita_doga"}; exec(compile(open(p, encoding="utf-8").read(), p, "exec"), g)
#   g["build_doga_modelleri"](); g["scatter"](); g["preview_instances"]()

import os
_hp = os.path.join(r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\ArtSource\Blender\Script", "harita.py")
exec(compile(open(_hp, encoding="utf-8").read(), _hp, "exec"), globals())

DOGA_COL = "Harita_Doga_Model"     # model kaynakları (x = 200 m'de dizili, dışa ayrı aktarılır)
YER_COL = "Harita_Yerler"

def paint_part(bm, group, lo=2, hi=0, noise=0.06, seed=0):
    """Alttan üste paletin gölge → aydınlık tonu (atolye_kabuk.py ile aynı fikir)."""
    r = random.Random(seed)
    k = 1.0 + r.uniform(-noise, noise)
    zs = [v.co.z for v in bm.verts]
    z0, z1 = min(zs), max(zs)
    span = max(z1 - z0, 1e-6)
    a, b = pal(group, lo), pal(group, hi)
    def fn(f, l):
        c = mix(a, b, (l.vert.co.z - z0) / span)
        return (min(c[0] * k, 1), min(c[1] * k, 1), min(c[2] * k, 1), 1.0)
    paint_by(bm, fn)
    return bm

def _blob(center, radius, sub=2, squash=1.0, seed=0, jit=0.12):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=radius)
    rnd = random.Random(seed)
    floor = -radius * 0.35 * squash
    for v in bm.verts:
        v.co *= 1.0 + rnd.uniform(-jit, jit)
        v.co.z *= squash
        if v.co.z < floor:                      # alt düzleşir: yaprak yığını oturur
            v.co.z = floor + (v.co.z - floor) * 0.35
    transform_bm(bm, Matrix.Translation(center))
    return bm

def _custom_normals(ob, center_of):
    """09 §3: taç/çalı normalleri kümenin merkezinden (yumuşak, kırık yüzsüz).
    center_of(vertex_index) → Vector ya da None (None: kendi normali)."""
    me = ob.data
    nrm = []
    for lp in me.loops:
        c = center_of(lp.vertex_index)
        if c is None:
            nrm.append(tuple(lp.normal))
        else:
            nrm.append(tuple((me.vertices[lp.vertex_index].co - c).normalized()))
    me.normals_split_custom_set(nrm)

def make_tree(name, seed, height=5.0, crown=2.4, golden=False, col=None, sub=2, trunk=1.0):
    rnd = random.Random(seed)
    parts = []
    lean = Vector((rnd.uniform(-0.25, 0.25), rnd.uniform(-0.25, 0.25), 0))
    th = height * 0.42                       # kısa, kalın gövde; ağırlık taçta (tombul siluet)
    prof = [(r * trunk, z) for (r, z) in [(0.55, 0.0), (0.38, 0.3), (0.30, th * 0.45), (0.25, th * 0.85), (0.20, th)]]
    tr = bm_lathe(prof, sides=7, close_bottom=False)
    for v in tr.verts:
        v.co += lean * (v.co.z / th) ** 1.5
    jitter(tr, 0.02, seed=seed)
    paint_part(tr, "ahsap", lo=2, hi=1, seed=seed)
    parts.append(tr)
    top = Vector((0, 0, th)) + lean
    for k in range(2):
        a = rnd.uniform(0, 2 * math.pi) + k * math.pi
        end = top + Vector((math.cos(a) * crown * 0.45, math.sin(a) * crown * 0.45, crown * 0.45))   # dal tacın içinde biter
        secs = []
        for t in (0.0, 1.0):
            p = top.lerp(end, t) - Vector((0, 0, 0.35 * (1 - t)))
            r = (0.15 if t == 0 else 0.08) * trunk
            secs.append([p + Vector((math.cos(i * 2.094) * r, math.sin(i * 2.094) * r, 0)) for i in range(3)])
        br = bm_sweep(secs)
        paint_part(br, "ahsap", lo=2, hi=1, seed=seed + k)
        parts.append(br)
    grp = "altin" if golden else "yaprak"
    n = rnd.randint(6, 8)
    cc0 = Vector((0, 0, th + crown * 0.4)) + lean
    lo_z = cc0.z - crown * 0.9
    hi_z = cc0.z + crown * 1.0
    blobs = []
    for k in range(n):
        if k == 0:
            c = cc0 + Vector((0, 0, crown * 0.15))
            r = crown * 0.72
        else:
            a = 2 * math.pi * k / (n - 1) + rnd.uniform(-0.3, 0.3)
            c = cc0 + Vector((math.cos(a) * crown * 0.62, math.sin(a) * crown * 0.62, rnd.uniform(-0.3, 0.3) * crown))
            r = crown * rnd.uniform(0.52, 0.66)
        b = _blob(c, r, sub=sub, squash=0.85, seed=seed * 7 + k)
        lay = b.loops.layers.float_color.get("Col") or b.loops.layers.float_color.new("Col")
        tone = rnd.uniform(-0.06, 0.06)
        for f in b.faces:
            for l in f.loops:
                t = smooth01((l.vert.co.z - lo_z) / (hi_z - lo_z) + tone)
                cc = mix(pal(grp, 2), pal(grp, 1), min(1.0, t * 1.6))
                l[lay] = mix(cc, pal(grp, 0), max(0.0, t * 1.6 - 0.6))
        blobs.append(b)
    nb_trunk = sum(len(p.verts) for p in parts)
    ob = to_object(bm_merge(*[(p, None) for p in parts + blobs]), name, col or collection(DOGA_COL), sharp_angle=0)
    _custom_normals(ob, lambda vi: cc0 if vi >= nb_trunk else None)
    return ob

def make_bush(name, seed, size=0.9, col=None, sub=2):
    rnd = random.Random(seed)
    blobs = []
    n = rnd.randint(3, 4)
    for k in range(n):
        a = 2 * math.pi * k / n + rnd.uniform(-0.4, 0.4)
        if k == 0:
            c, r = Vector((0, 0, size * 0.5)), size * 0.7
        else:
            c, r = Vector((math.cos(a) * size * 0.45, math.sin(a) * size * 0.45, size * 0.35)), size * rnd.uniform(0.5, 0.65)
        blobs.append(_blob(c, r, sub=sub, squash=0.8, seed=seed + k))
    bm = bm_merge(*[(b, None) for b in blobs])
    paint_part(bm, "yaprak", lo=2, hi=0, seed=seed, noise=0.05)
    ob = to_object(bm, name, col or collection(DOGA_COL), sharp_angle=0)
    ctr = Vector((0, 0, size * 0.25))
    _custom_normals(ob, lambda vi: ctr)
    return ob

def make_rock(name, seed, size=1.2, flat=0.6, col=None):
    """Düz yüzeyli iri kaya: az nokta → dışbükey gövde → küçük pah (09 §3)."""
    rnd = random.Random(seed)
    bm = bmesh.new()
    pts = []
    for i in range(16):
        u = rnd.uniform(0, 2 * math.pi)
        w = rnd.uniform(-0.9, 1.0)
        r = math.sqrt(1 - w * w)
        pts.append(bm.verts.new((math.cos(u) * r * size * rnd.uniform(0.8, 1.1),
                                 math.sin(u) * r * size * rnd.uniform(0.6, 0.9),
                                 w * size * flat)))
    bmesh.ops.convex_hull(bm, input=pts)
    for v in [v for v in bm.verts if not v.link_faces]:
        bm.verts.remove(v)
    zmin = min(v.co.z for v in bm.verts)
    for v in bm.verts:
        v.co.z -= zmin + size * flat * 0.25     # biraz gömülü
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(8), verts=list(bm.verts), edges=list(bm.edges))
    bevel_all(bm, size * 0.05, 1)
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
    paint_part(bm, "kaya", lo=2, hi=0, seed=seed, noise=0.05)
    return to_object(bm, name, col or collection(DOGA_COL), sharp_angle=40)

def build_doga_modelleri():
    col = collection(DOGA_COL)
    clear_collection(DOGA_COL)
    for me in list(bpy.data.meshes):
        if me.users == 0:
            bpy.data.meshes.remove(me)
    obs = []
    specs = [("SM_Agac_Yesil_1", dict(seed=101, height=5.5, crown=3.2)),
             ("SM_Agac_Yesil_2", dict(seed=102, height=4.6, crown=2.7)),
             ("SM_Agac_Yesil_3", dict(seed=103, height=6.6, crown=3.7)),
             ("SM_Agac_Altin_1", dict(seed=104, height=5.4, crown=3.2, golden=True)),
             ("SM_Agac_Altin_2", dict(seed=105, height=4.6, crown=2.7, golden=True))]
    # LOD (09 §7): Ad (boş) + Ad_LOD0 (yakın) + Ad_LOD1 (taç kümeleri ikosahedron, ~%30)
    def lod_pair(name, loc, fn):
        root = empty(name, col, loc=loc, kind="PLAIN_AXES", size=0.5)
        for k, sub in ((0, 2), (1, 1)):
            o = fn(name + "_LOD%d" % k, sub)
            o.location = loc
            parent_keep(o, root)
            obs.append(o)
        return root
    for i, (n, kw) in enumerate(specs):
        lod_pair(n, (200 + i * 8, 0, 0), lambda nm, sb, kw=kw: make_tree(nm, col=col, sub=sb, **kw))
    for i in range(3):
        lod_pair("SM_Cali_%d" % (i + 1), (200 + i * 4, 12, 0),
                 lambda nm, sb, i=i: make_bush(nm, 201 + i, size=0.8 + 0.25 * i, col=col, sub=sb))
    for i in range(4):
        ob = make_rock("SM_Kaya_%d" % (i + 1), 301 + i, size=0.8 + 0.6 * i, flat=0.5 + 0.08 * i, col=col)
        ob.location = (200 + i * 6, 22, 0)
        obs.append(ob)
    print("Doğa üçgen:", {o.name: tri_count([o]) for o in obs})
    return obs

# ------------------------------------------------------------------ DAĞITIM
def yer_z(kind, x, y, s=1.0):
    """Doğa kopyasının taban yüksekliği: arazi AĞININ (SM_Arazi) kapladığı alandaki en alçak noktası, türe göre biraz
    gömülü. Blender önizlemesi ve Unity aynı değeri kullanır; Unity ayrıca yere oturtmaz (13.11: Unity'nin kendi
    oturtması kayaları 0.5–1.6 m fazla gömüyor, köprü altındaki sazı köprünün üstüne taşıyordu)."""
    if kind.startswith("Kaya_Kitle"):
        r, gom = 1.6 * s, 0.35 * s
    elif kind.startswith("Kaya"):
        r, gom = 0.45 * s, 0.12 * s
    elif kind.startswith(("Agac", "Cam", "Kavak", "Meyve")):
        r, gom = 0.35, 0.08
    elif kind.startswith("Cali"):
        r, gom = 0.5 * s, 0.05
    else:                                                   # çimen, çiçek, çakıl, saz
        r, gom = 0.12, 0.02
    zs = [arazi_z(x, y)] + [arazi_z(x + dx, y + dy) for dx, dy in ((r, 0), (-r, 0), (0, r), (0, -r))]
    return min(zs) - gom

def egim(x, y, e=0.6):
    """Arazi eğimi (tan): 0 düz, 1 = 45°."""
    gx = (height(x + e, y) - height(x - e, y)) / (2 * e)
    gy = (height(x, y + e) - height(x, y - e)) / (2 * e)
    return math.hypot(gx, gy)

def _keep_out(p):
    """Bu noktaya ağaç/kaya konmaz: yol, meydan, atölye, dere, köy merkezleri."""
    if (p - MEYDAN).length < MEYDAN_R + 3.0:
        return True
    if abs(p.x - ATOLYE.x) < ATOLYE_HALF[0] + 3 and abs(p.y - ATOLYE.y) < ATOLYE_HALF[1] + 3:
        return True
    if poly_dist(p, STREAM) < STREAM_HALF + 0.5:
        return True
    d, _ = road_info(p)
    if d < 2.5:
        return True
    for c, r in ((DENK, 16.0), (TEPK, 15.0)):
        if (p - c).length < r:
            return True
    return False

def scatter():
    col = collection(YER_COL)
    clear_collection(YER_COL)
    rnd = random.Random(4242)
    placed = []
    def put(kind, p, r, scale=(0.85, 1.2)):
        if _keep_out(p) or any((p - q).length < r for q in placed):
            return False
        # dik yamaçta (boğaz duvarı, kaya basamağı) ağaç/kaya bir yanı havada kalıyordu (Play 13.11): eğim sınırı
        if egim(p.x, p.y) > (0.62 if kind.startswith("Kaya") else 1.0):
            return False
        placed.append(p)
        s = rnd.uniform(*scale)
        z = yer_z(kind, p.x, p.y, s)
        ob = empty("Yer_%s_%03d" % (kind, len(placed)), col, loc=(p.x, p.y, z),
                   rot=(0, 0, rnd.uniform(0, 360)), kind="SINGLE_ARROW", size=1.0)
        ob.scale = (s, s, s)
        return True
    green = ["Agac_Yesil_1", "Agac_Yesil_2", "Agac_Yesil_3"]
    # 1) vadi kenarı ve tepeler: kümeler halinde (çerçeve)
    for _ in range(2600):
        p = Vector((rnd.uniform(X0 + 20, X1 - 20), rnd.uniform(Y0 + 20, Y1 - 15)))
        d = play_outside(p.x, p.y)
        dens = 0.5 + 0.5 * n2(p.x, p.y, 22.0, 8.8)
        if -3 < d < 45 and rnd.random() < dens * (0.9 if d > 0 else 0.5):   # vadi tabanı açık kalsın (dağınıklık yok)
            put(rnd.choice(green), p, 5.2, scale=(0.8, 1.45))
    # 2) altın vurgu ağaçları: yalnız simge noktalarında (09 §3 vurgu oranı)
    for c in (Vector((8.0, 10.0)), Vector((40.0, -1.0)), Vector((-36.0, 6.0)), Vector((44.0, -18.0)), Vector((-30.0, -14.0))):
        for _ in range(8):
            if put(rnd.choice(["Agac_Altin_1", "Agac_Altin_2"]), c + Vector((rnd.uniform(-2, 2), rnd.uniform(-2, 2))), 4.0):
                break
    # 3) dere boyu: kıyıda seyrek ağaç + çalı + kaya
    for p in resample(STREAM, 7.0):
        q = p + Vector((rnd.choice((-1, 1)) * (STREAM_HALF + rnd.uniform(1.0, 3.0)), rnd.uniform(-2, 2)))
        r = rnd.random()
        if r < 0.35:
            put(rnd.choice(green), q, 4.0)
        elif r < 0.75:
            put("Cali_%d" % rnd.randint(1, 3), q, 1.8)
        else:
            put("Kaya_%d" % rnd.randint(1, 3), q, 2.0)
    # 4) yol kenarı: düzensiz çalı/kaya (yolu çerçeveler, kapatmaz)
    for n, pts, w in ROADS:
        rs = resample(pts, 6.5)
        for i in range(1, len(rs) - 1):
            if rnd.random() < 0.45:
                t = (rs[i + 1] - rs[i - 1]).normalized()
                q = rs[i] + Vector((-t.y, t.x)) * rnd.choice((-1, 1)) * (w * 0.5 + rnd.uniform(2.8, 4.0))
                put("Cali_%d" % rnd.randint(1, 3) if rnd.random() < 0.7 else "Kaya_1", q, 2.2)
    # 5) vadi basamağının dibine iri kayalar (sınırı okutur)
    for _ in range(700):
        p = Vector((rnd.uniform(X0 + 25, X1 - 25), rnd.uniform(Y0 + 25, Y1 - 20)))
        if -1.5 < play_outside(p.x, p.y) < 2.5 and rnd.random() < 0.5:
            put("Kaya_%d" % rnd.randint(2, 4), p, 5.0, scale=(0.9, 1.6))
    counts = {}
    for o in col.objects:
        k = o.name.rsplit("_", 1)[0][4:]
        counts[k] = counts.get(k, 0) + 1
    print("Yerleşim:", len(col.objects), counts)

def preview_instances():
    """Önizleme: yerlere model kopyaları (bağlı mesh). Dışa aktarılmaz."""
    col = collection("_Onizleme_Doga")
    for o in list(col.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for y in bpy.data.collections[YER_COL].objects:
        base = "SM_" + y.name.rsplit("_", 1)[0][4:]
        src = bpy.data.objects.get(base + "_LOD0") or bpy.data.objects.get(base)
        if not src:
            continue
        o = bpy.data.objects.new(y.name.replace("Yer_", "On_"), src.data)
        col.objects.link(o)
        o.location = y.location
        o.rotation_euler = y.rotation_euler
        o.scale = y.scale


# ------------------------------------------------------------------ DIŞA AKTARMA
def _fbx(path, objs):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    for o in list(bpy.context.selected_objects):
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH", "EMPTY"},
        apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        mesh_smooth_type="OFF", use_mesh_modifiers=True, colors_type="LINEAR", bake_anim=False, add_leaf_bones=False)
    for o in list(bpy.context.selected_objects):
        o.select_set(False)
    return path

def export_harita():
    """Harita.fbx (arazi, yol, meydan, su, köy kökleri) · Harita_Doga.fbx (türler, her biri orijinde)
    · Harita_Yerler.fbx (Yer_* boş objeleri: Unity'de prefab kopyalarıyla değiştirilir)."""
    out = []
    base = list(bpy.data.collections[COL].all_objects)
    if bpy.data.collections.get("Harita_Yapilar"):
        base += list(bpy.data.collections["Harita_Yapilar"].all_objects)      # köy kökleri + yapılar tek dosyada
    if bpy.data.collections.get("Harita_Yon"):
        base += list(bpy.data.collections["Harita_Yon"].all_objects)          # tabela, kemer, mil taşı, harita masası
    out.append(_fbx(os.path.join(EXPORT_DIR, "Harita.fbx"), base))
    models = list(bpy.data.collections[DOGA_COL].objects)
    roots = [o for o in models if o.parent is None]
    # LOD parçaları (_LOD0/_LOD1) kökün tam üstünde olmalı (meshleri kök merkezine göre kurulu). Dosyada yerel
    # konumları model rafının konumunu (x 200–238) taşıyordu → sıfırlanır (kalıcı; harita_kur dosyayı kaydeder).
    for o in models:
        if o.parent is not None:
            o.matrix_parent_inverse = Matrix.Identity(4)
            o.location = (0, 0, 0)
            o.rotation_euler = (0, 0, 0)
            o.scale = (1, 1, 1)
    keep = {o.name: tuple(o.location) for o in roots}
    for o in roots:
        o.location = (0, 0, 0)
    # Arka planda (blender -b) çocukların (_LOD0/_LOD1) dünya matrisi güncellenmeden yazılıyordu: FBX'te LOD parçaları
    # model rafının konumunu (x 200–238) taşıyordu → Unity'de 354 ağaç/çalı kökünden 190–290 m uzakta çiziliyordu,
    # köyün üstünde uçan ağaç dahil (Play QA 13.11). Taşıdıktan sonra sahne güncellenir.
    bpy.context.view_layer.update()
    try:
        out.append(_fbx(os.path.join(EXPORT_DIR, "Harita_Doga.fbx"), models))
    finally:
        for o in roots:
            o.location = keep[o.name]
        bpy.context.view_layer.update()
    out.append(_fbx(os.path.join(EXPORT_DIR, "Harita_Yerler.fbx"), list(bpy.data.collections[YER_COL].objects)))
    return out
