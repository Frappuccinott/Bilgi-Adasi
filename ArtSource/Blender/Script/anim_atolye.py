# Câbir Usta'nın Atölyesi — animasyonlu varlıklar ve klipler
# Önce: ba_lib, atolye_kabuk, atolye_esya, atolye_ic, esya_tasinir, anim_ortak, simya_tezgahi (aynı globals).
# Çalıştırma sırası (tam yeniden kurulum): reset_anim(); build_all(); build_core(); build_interior();
#   build_catalog(); build_tezgah(); build_anims(); export_all()

import math
import bpy
from mathutils import Matrix, Vector

def reset_anim():
    clear_collection(ANIM_COL)
    for a in list(bpy.data.actions):
        if a.users == 0:
            bpy.data.actions.remove(a)
    for a in list(bpy.data.armatures):
        if a.users == 0:
            bpy.data.armatures.remove(a)

def _ease_keys(arm, bone, keys, pivot=None):
    """keys: [(kare, loc, rot(derece), scale)]"""
    for fr, loc, rot, sc in keys:
        key_pose(arm, fr, {bone: pose_delta(arm, bone, loc=loc, rot=rot, scale=sc, pivot=pivot)})

# ================================================================== KAPI
def rig_door():
    leaf = bpy.data.objects["Kapi_Kanat"]
    leaf.rotation_euler = (0, 0, 0)
    bpy.context.view_layer.update()        # tam kurulum tek çağrıda yapılınca matrix_world bayat kalıyordu → iskelet (0,0,0)'a kuruluyordu
    hinge = Vector(leaf.matrix_world.translation)
    assert hinge.length > 0.5, "Kapı menteşesi orijinde okundu: matrix_world güncel değil"
    arm = make_rig("Kapi_Rig", tuple(hinge), [("Kanat", (0, 0, 0), (0, 0, 0.5), None)])
    bind(leaf, arm, "Kanat")
    new_clip(arm, "Kapi_Bosta", 1, lambda a: key_pose(a, 1, {"Kanat": pose_delta(a, "Kanat")}))      # kapalı
    # içeri açılır: +Z; hafif aşma (overshoot) ve yerine oturma — ağır meşe kapı hissi
    new_clip(arm, "Kapi_Ac", 22, lambda a: _ease_keys(a, "Kanat", [
        (1, (0, 0, 0), (0, 0, 0), 1), (4, (0, 0, 0), (0, 0, 6), 1), (16, (0, 0, 0), (0, 0, 99), 1), (22, (0, 0, 0), (0, 0, 95), 1)]))
    new_clip(arm, "Kapi_Kapat", 20, lambda a: _ease_keys(a, "Kanat", [
        (1, (0, 0, 0), (0, 0, 95), 1), (14, (0, 0, 0), (0, 0, -1.5), 1), (17, (0, 0, 0), (0, 0, 0.8), 1), (20, (0, 0, 0), (0, 0, 0), 1)]))
    return arm

# ------------------------------------------------------------------ yardımcılar
H0 = 1e-3      # "gizli" ölçek (0 yerine: FBX/Unity'de dejenere matris olmasın)

def _hide(arm, frame, bones):
    key_pose(arm, frame, {b: pose_delta(arm, b, scale=H0) for b in bones})

def _flicker(arm, bone, f0, f1, amp=0.12, seed=0, base=1.0):
    """Alev titremesi: düzensiz ama döngüye kapanan ölçek/eğim anahtarları."""
    import random as _r
    rr = _r.Random(seed)
    n = 6
    for i in range(n + 1):
        fr = f0 + (f1 - f0) * i / n
        if i in (0, n):
            sy, rx = base, 0.0
        else:
            sy, rx = base * (1 + rr.uniform(-amp, amp)), rr.uniform(-6, 6)
        key_pose(arm, int(round(fr)), {bone: pose_delta(arm, bone, rot=(rx, rr.uniform(-4, 4) if 0 < i < n else 0, 0),
                                                         scale=(base * (2 - sy) ** 0.5, sy, base * (2 - sy) ** 0.5))})

def _bone_up(name, head, length=0.1, parent=None):
    h = Vector(head)
    return (name, tuple(h), tuple(h + Vector((0, 0, length))), parent)

def _bone_down(name, head, length=0.1, parent=None):
    h = Vector(head)
    return (name, tuple(h), tuple(h - Vector((0, 0, length))), parent)

def _local(arm, world):
    return tuple(arm.matrix_world.inverted() @ Vector(world))

# ================================================================== KAZAN (+ ocak ateşi)
def _kazan_extras(center, surf_z, rim_z):
    """Kepçe (ahşap karıştırma küreği), kabarcıklar, köpük, buhar mesh'leri."""
    col = anim_col()
    objs = {}
    # kepçe: uzun sap + kürek ağzı; kazana eğik dayalı
    parts = []
    shaft = bm_cyl(0.022, 0.018, 1.05, sides=8)
    _wood(shaft, 8801, lo=1, hi=0)
    parts.append((shaft, None))
    blade = bm_box(0.12, 0.03, 0.22, bevel=0.012)
    jitter(blade, 0.004, seed=8802)
    _wood(blade, 8803, lo=2, hi=1)
    parts.append((blade, Matrix.Translation((0, 0, -0.18))))
    kb = bm_merge(*parts)
    objs["kepce"] = to_object(kb, "Kazan_Kepce", col, loc=(center.x + 0.16, center.y, surf_z - 0.05), rot=(0, -22, 0), sharp_angle=45)
    import random as _r
    rr = _r.Random(8804)
    objs["kabarcik"] = []
    for k in range(5):
        b = bmesh.new()
        bmesh.ops.create_icosphere(b, subdivisions=1, radius=rr.uniform(0.03, 0.05))
        bmesh.ops.scale(b, vec=(1, 1, 0.7), verts=b.verts)
        paint_flat(b, mix(pal("su", 0), pal("siva", 0), 0.4))
        a = rr.uniform(0, 6.28)
        d = rr.uniform(0.04, 0.24)
        objs["kabarcik"].append(to_object(b, "Kazan_Kabarcik_%d" % (k + 1), col,
                                          loc=(center.x + math.cos(a) * d, center.y + math.sin(a) * d, surf_z + 0.01),
                                          mat=material_named("M_Su", "#35BDEB"), sharp_angle=0))
    # köpük: kazandan taşan yumuşak kabarık kütle (M_Kopuk)
    fp = []
    for k in range(9):
        b = bmesh.new()
        bmesh.ops.create_icosphere(b, subdivisions=2, radius=rr.uniform(0.09, 0.15))
        jitter(b, 0.01, seed=8810 + k)
        paint_flat(b, mix(pal("su", 0), pal("siva", 0), rr.uniform(0.2, 0.6)))
        a = 2 * math.pi * k / 9
        d = 0.0 if k == 0 else rr.uniform(0.2, 0.36)
        fp.append((b, Matrix.Translation((math.cos(a) * d, math.sin(a) * d, rr.uniform(0.0, 0.08)))))
    objs["kopuk"] = to_object(bm_merge(*fp), "Kazan_Kopuk", col, loc=(center.x, center.y, surf_z), mat=material_named("M_Kopuk", "#C8F6FF", emission=0.3), sharp_angle=0)
    # buhar: üç yumuşak bulut
    objs["buhar"] = []
    for k in range(3):
        b = bmesh.new()
        bmesh.ops.create_icosphere(b, subdivisions=2, radius=0.12)
        jitter(b, 0.02, seed=8820 + k)
        paint_flat(b, pal("siva", 0))
        objs["buhar"].append(to_object(b, "Kazan_Buhar_%d" % (k + 1), col, loc=(center.x + (k - 1) * 0.08, center.y, rim_z + 0.05),
                                       mat=material_named("M_Duman", "#DDDCC5", alpha=0.6), sharp_angle=0))
    return objs

def rig_kazan():
    kz = bpy.data.objects["Kazan"]
    iks = bpy.data.objects["Kazan_Iksir"]
    c = Vector(kz.location)
    surf_z = iks.location.z
    rim_z = c.z + 0.635 * KZ
    ex = _kazan_extras(c, surf_z, rim_z)
    bones = [_bone_up("Kok", (0, 0, 0)),
             _bone_up("Govde", (0, 0, 0), parent="Kok"),
             _bone_up("Iksir", (0, 0, surf_z - c.z), parent="Kok"),
             ("Kepce", (0, 0, surf_z - c.z - 0.05), (0, 0, surf_z - c.z + 0.3), "Kok"),
             _bone_up("Kopuk", (0, 0, surf_z - c.z), parent="Kok")]
    for k, b in enumerate(ex["kabarcik"]):
        bones.append(_bone_up("Kabarcik_%d" % (k + 1), tuple(Vector(b.location) - c), 0.05, "Kok"))
    for k, b in enumerate(ex["buhar"]):
        bones.append(_bone_up("Buhar_%d" % (k + 1), tuple(Vector(b.location) - c), 0.1, "Kok"))
    arm = make_rig("Kazan_Rig", tuple(c), bones)
    bind(kz, arm, "Govde")
    bind(iks, arm, "Iksir")
    bind(ex["kepce"], arm, "Kepce")
    bind(ex["kopuk"], arm, "Kopuk")
    for k, b in enumerate(ex["kabarcik"]):
        bind(b, arm, "Kabarcik_%d" % (k + 1))
    for k, b in enumerate(ex["buhar"]):
        bind(b, arm, "Buhar_%d" % (k + 1))
    kab = ["Kabarcik_%d" % (k + 1) for k in range(len(ex["kabarcik"]))]
    buh = ["Buhar_%d" % (k + 1) for k in range(len(ex["buhar"]))]
    RIG_DEFAULTS[arm.name] = {b: "gizli" for b in ["Kopuk"] + kab + buh}
    axis = (0, 0, 0)    # kazan ekseni (armature uzayı)

    def bosta(a):
        _hide(a, 1, ["Kopuk"] + kab + buh)
    new_clip(arm, "Kazan_Bosta", 1, bosta)

    def karistir(a):
        # kepçe kazan ekseni çevresinde döner (doğrusal → kusursuz döngü); sıvı yüzeyi gecikmeli döner
        for i in range(5):
            fr = 1 + i * 15
            key_pose(a, fr, {"Kepce": pose_delta(a, "Kepce", rot=(0, 0, i * 90), pivot=axis),
                             "Iksir": pose_delta(a, "Iksir", rot=(0, 0, i * 45))})
        _hide(a, 1, ["Kopuk"] + kab + buh)
        _hide(a, 61, ["Kopuk"] + kab + buh)
    new_clip(arm, "Kazan_Karistir_Dongu", 61, karistir, loop=True, interp="LINEAR")

    def kaynama(a):
        import random as _r
        rr = _r.Random(8830)
        for k, b in enumerate(kab):
            s0 = int(1 + k * 9)
            seq = [(1, H0, 0.0)]
            for cyc in range(2):
                st = (s0 + cyc * 24) % 48 + 1
                seq += [(st, H0, 0.0), (st + 6, 1.0, 0.02), (st + 9, 1.25, 0.03), (st + 10, H0, 0.03)]
            seq.append((49, H0, 0.0))
            seq = sorted(dict((f, (s, z)) for f, s, z in seq if 1 <= f <= 49).items())
            for fr, (s, z) in seq:
                key_pose(a, fr, {b: pose_delta(a, b, loc=(0, 0, z), scale=s)})
        for fr, z in ((1, 0.0), (13, 0.012), (25, 0.0), (37, 0.01), (49, 0.0)):
            key_pose(a, fr, {"Iksir": pose_delta(a, "Iksir", loc=(0, 0, z))})
        _hide(a, 1, ["Kopuk"] + buh)
    new_clip(arm, "Kazan_Kaynama_Dongu", 49, kaynama, loop=True)

    def kopurme(a):
        # dengesiz ateş: köpük yükselir, kazan sarsılır, taşar, sonra iner (ceza yok, kazan boşalır)
        for fr, s, z in ((1, H0, 0.0), (8, 0.5, 0.02), (18, 1.25, 0.10), (24, 1.1, 0.12), (32, 1.3, 0.14), (48, 1.0, 0.08), (66, H0, 0.0)):
            key_pose(a, fr, {"Kopuk": pose_delta(a, "Kopuk", loc=(0, 0, z), scale=s)})
        for fr, r in ((1, 0), (6, 2.0), (9, -2.0), (12, 1.6), (15, -1.4), (18, 1.0), (22, -0.6), (26, 0)):
            key_pose(a, fr, {"Govde": pose_delta(a, "Govde", rot=(r, r * 0.6, 0))})
        _hide(a, 1, kab + buh)
        _hide(a, 66, kab + buh)
    new_clip(arm, "Kazan_Kopurme", 66, kopurme)

    def basari(a):
        # dengede tepkime: üç buhar bulutu yükselip açılır, sıvı hafif kabarır
        for k, b in enumerate(buh):
            o = k * 6
            for fr, s, z in ((1, H0, 0.0), (1 + o, H0, 0.0), (10 + o, 0.8, 0.15), (26 + o, 1.6, 0.55), (40 + o, 2.2, 0.9), (46 + o, H0, 1.0)):
                key_pose(a, fr, {b: pose_delta(a, b, loc=(0, 0, z), scale=s)})
        for fr, z, s in ((1, 0.0, 1.0), (12, 0.02, 1.03), (30, 0.0, 1.0)):
            key_pose(a, fr, {"Iksir": pose_delta(a, "Iksir", loc=(0, 0, z), scale=s)})
        _hide(a, 1, ["Kopuk"] + kab)
        _hide(a, 58, ["Kopuk"] + kab)
    new_clip(arm, "Kazan_Basari", 58, basari)

    return arm

# ================================================================== DAMITMA (imbik)
def rig_damitma(res):
    base = Vector(bpy.data.objects["SM_Damitma_Mangal"].location)
    tip = res["tip"]
    liq = bpy.data.objects["Kabile_Sivi"]
    up = Vector((0, 0, 1))
    lq = Vector(liq.location) - base
    bones = [_bone_up("Kok", (0, 0, 0)),
             _bone_up("Ates", tuple(Vector(bpy.data.objects["Damitma_Ates"].location) - base), parent="Kok"),
             _bone_up("Kor", (0, 0, 0.15), parent="Kok")]
    for k in range(3):
        bones.append(_bone_down("Damla_%d" % (k + 1), tuple(tip - base + Vector((0, 0, -0.012))), 0.03, "Kok"))
    arm = make_rig("Damitma_Rig", tuple(base), bones)
    bind(bpy.data.objects["Damitma_Ates"], arm, "Ates")
    bind(bpy.data.objects["Damitma_Kor"], arm, "Kor")
    for k in range(3):
        bind(bpy.data.objects["Damla_%d" % (k + 1)], arm, "Damla_%d" % (k + 1))
    for n in ("SM_Damitma_Mangal", "SM_Damitma_Govde"):
        bind(bpy.data.objects[n], arm, "Kok")
    drops = ["Damla_1", "Damla_2", "Damla_3"]
    RIG_DEFAULTS[arm.name] = {b: "gizli" for b in ["Ates", "Kor"] + drops}
    fall = 0.075     # gaga ucundan kâbile ağzına

    def bosta(a):
        _hide(a, 1, ["Ates", "Kor"] + drops)
    new_clip(arm, "Damitma_Bosta", 1, bosta)

    def basla(a):
        for fr, s in ((1, H0), (8, 0.7), (16, 1.15), (24, 1.0)):
            key_pose(a, fr, {"Ates": pose_delta(a, "Ates", scale=s), "Kor": pose_delta(a, "Kor", scale=min(1.0, max(H0, s)))})
        _hide(a, 1, drops)
        _hide(a, 24, drops)
    new_clip(arm, "Damitma_Basla", 24, basla)

    def dongu(a):
        _flicker(a, "Ates", 1, 91, amp=0.12, seed=8850)
        for k, b in enumerate(drops):
            o = 1 + k * 30
            seq = [(1, H0, 0.0)] if o > 1 else []
            seq += [(o, H0, 0.0), (o + 14, 0.8, 0.0), (o + 20, 1.0, -0.004), (o + 23, 1.1, -fall * 0.35), (o + 26, 0.9, -fall),
                    (o + 27, H0, -fall)]
            seq.append((91, H0, 0.0))
            for fr, (s, z) in sorted(dict((f, (s, z)) for f, s, z in seq if 1 <= f <= 91).items()):
                key_pose(a, fr, {b: pose_delta(a, b, loc=(0, 0, z), scale=(s, s * (1.4 if z < -0.01 else 1.0), s))})
    new_clip(arm, "Damitma_Dongu", 91, dongu, loop=True)

    def dur(a):
        for fr, s in ((1, 1.0), (8, 1.1), (20, H0)):
            key_pose(a, fr, {"Ates": pose_delta(a, "Ates", scale=s), "Kor": pose_delta(a, "Kor", scale=max(H0, min(1.0, s)))})
        _hide(a, 1, drops)
    new_clip(arm, "Damitma_Dur", 20, dur)
    return arm

def rig_kabile():
    """Kâbile (toplama şişesi) ayrı iskelet: damlatma döngüsüyle aynı anda dolabilsin."""
    sehpa = bpy.data.objects["SM_Kabile_Sehpa"]
    liq = bpy.data.objects["Kabile_Sivi"]
    base = Vector(sehpa.location)
    up = Vector((0, 0, 1))
    lq = Vector(liq.location) - base
    arm = make_rig("Kabile_Rig", tuple(base), [_bone_up("Kok", (0, 0, 0)), ("Sivi", tuple(lq), tuple(lq + up * 0.1), "Kok")])
    for n in ("SM_Kabile_Sehpa", "SM_Kabile"):
        bind(bpy.data.objects[n], arm, "Kok")
    bind(liq, arm, "Sivi")
    new_clip(arm, "Kabile_Bosta", 1, lambda a: key_pose(a, 1, {"Sivi": pose_delta(a, "Sivi", scale=(1, 0.12, 1))}))
    new_clip(arm, "Kabile_Dolum", 300, lambda a: [key_pose(a, fr, {"Sivi": pose_delta(a, "Sivi", scale=(1, s, 1))})
                                                  for fr, s in ((1, 0.12), (300, 1.0))], interp="LINEAR")
    new_clip(arm, "Kabile_Dolu", 1, lambda a: key_pose(a, 1, {"Sivi": pose_delta(a, "Sivi", scale=(1, 1.0, 1))}))
    return arm

# ================================================================== OCAK ATEŞİ (kazanın altı) — ayrı iskelet
def rig_ocak_ates():
    ates = bpy.data.objects["Ocak_Ates"]
    root = Vector(ates.location)
    arm = make_rig("Ocak_Ates_Rig", tuple(root), [_bone_up("Ates", (0, 0, 0.18), 0.2)])
    bind(ates, arm, "Ates")
    RIG_DEFAULTS[arm.name] = {"Ates": "gizli"}
    new_clip(arm, "Ates_Bosta", 1, lambda a: _hide(a, 1, ["Ates"]))
    new_clip(arm, "Ates_Yan", 20, lambda a: [key_pose(a, fr, {"Ates": pose_delta(a, "Ates", scale=s)})
                                             for fr, s in ((1, H0), (6, 0.6), (13, 1.18), (20, 1.0))])
    new_clip(arm, "Ates_Titre_Dongu", 31, lambda a: _flicker(a, "Ates", 1, 31, amp=0.14, seed=8840), loop=True)
    new_clip(arm, "Ates_Son", 16, lambda a: [key_pose(a, fr, {"Ates": pose_delta(a, "Ates", scale=s)})
                                             for fr, s in ((1, 1.0), (6, 1.1), (16, H0))])
    return arm

# ================================================================== TAŞINABİLİR EŞYALAR: al / dök / bırak
ITEM_KIND = {"Malzeme_H2": "gaz", "Malzeme_O2": "gaz", "Malzeme_N2": "gaz", "Malzeme_CO2": "gaz", "Malzeme_CO": "gaz",
             "Malzeme_CH4": "gaz", "Malzeme_C3H8": "gaz", "Malzeme_H2O": "sivi", "Malzeme_NH3": "sivi",
             "Malzeme_CaO": "toz", "Malzeme_CaOH2": "toz", "Malzeme_CaCO3": "tas"}
TILT = {"gaz": -62, "sivi": -115, "toz": -125, "tas": -130}

def _effect_mesh(kind, name):
    """Dökme efekti: ağız noktasından AŞAĞI uzanan mesh (kemik Y'si aşağı → Y ölçeği = akış uzunluğu)."""
    import random as _r
    rr = _r.Random(hash(name) & 0xffff)
    col = anim_col()
    if kind == "gaz":
        parts = []
        for k in range(4):
            b = bmesh.new()
            bmesh.ops.create_icosphere(b, subdivisions=2, radius=rr.uniform(0.035, 0.06))
            jitter(b, 0.006, seed=k)
            paint_flat(b, pal("siva", 0))
            parts.append((b, Matrix.Translation((rr.uniform(-0.04, 0.04), rr.uniform(-0.04, 0.04), rr.uniform(0.0, 0.06)))))
        return to_object(bm_merge(*parts), name, col, mat=material_named("M_Duman", "#DDDCC5", alpha=0.6), sharp_angle=0)
    L = 0.42
    if kind == "sivi":
        cc = mix(pal("su", 0), pal("siva", 0), 0.25) if "H2O" in name else mix(pal("altin", 0), pal("siva", 0), 0.55)
        prof = [(0.0, 0.0)] + [(0.013 - 0.006 * t + 0.003 * math.sin(t * 18), -L * t) for t in [i / 10 for i in range(11)]] + [(0.0, -L)]
        bm = bm_lathe([(r, z) for r, z in prof], sides=8)
        paint_flat(bm, cc)
        return to_object(bm, name, col, mat=material_named("M_Su", "#35BDEB"), sharp_angle=0)
    if kind == "toz":
        prof = [(0.0, 0.0)] + [(0.016 + 0.01 * t, -L * t) for t in [i / 8 for i in range(9)]] + [(0.0, -L)]
        bm = bm_lathe(prof, sides=7)
        jitter(bm, 0.004, seed=3)
        paint_by(bm, lambda f, l: mix(pal("siva", 0), pal("kaya", 0), rr.uniform(0, 0.4)))
        return to_object(bm, name, col, sharp_angle=0)
    parts = []   # taş: dört parça, düşüş hattında
    for k in range(4):
        b = bmesh.new()
        bmesh.ops.create_icosphere(b, subdivisions=1, radius=rr.uniform(0.014, 0.022))
        jitter(b, 0.004, seed=k)
        paint_flat(b, mix(pal("kaya", 0), pal("kil", 0), 0.45))
        parts.append((b, Matrix.Translation((rr.uniform(-0.01, 0.01), rr.uniform(-0.01, 0.01), -0.05 - k * 0.09))))
    return to_object(bm_merge(*parts), name, col, sharp_angle=0)

def rig_item(name):
    root = bpy.data.objects[name]
    model = bpy.data.objects[name + "_Model"]
    grip = bpy.data.objects[name + "_Tutma_Noktasi"].location.z
    mouth_ob = bpy.data.objects.get(name + "_Agiz_Noktasi")
    mouth = mouth_ob.location.z if mouth_ob else grip + 0.1
    kind = ITEM_KIND.get(name)
    loc = Vector(root.location)
    bones = [_bone_up("Kok", (0, 0, grip), 0.08)]
    if kind:
        bones.append(_bone_down("Efekt", (0, 0, mouth), 0.1))
    arm = make_rig(name + "_Rig", tuple(loc), bones)
    children = [model] + [o for o in root.children if o is not model]
    for o in children:
        bind(o, arm, "Kok")
    eff = None
    if kind:
        eff = _effect_mesh(kind, name + "_Efekt")
        eff.location = loc + Vector((0, 0, mouth))
        bind(eff, arm, "Efekt")
        RIG_DEFAULTS[arm.name] = {"Efekt": "gizli"}
    bpy.data.objects.remove(root, do_unlink=True)
    G = Vector((0, 0, grip))
    M0 = Vector((0, 0, mouth))
    new_clip(arm, "El_Bosta", 1, lambda a: key_pose(a, 1, {"Kok": pose_delta(a, "Kok")}))
    new_clip(arm, "El_Al", 10, lambda a: [key_pose(a, fr, {"Kok": pose_delta(a, "Kok", loc=l, scale=s)})
                                          for fr, l, s in ((1, (0, 0, -0.14), 0.85), (7, (0, 0, 0.012), 1.02), (10, (0, 0, 0), 1.0))])
    new_clip(arm, "El_Birak", 8, lambda a: [key_pose(a, fr, {"Kok": pose_delta(a, "Kok", loc=l, scale=s)})
                                            for fr, l, s in ((1, (0, 0, 0), 1.0), (8, (0, 0, -0.16), 0.85))])
    if not kind:
        return arm
    T = TILT[kind]
    kok_keys = [(1, (0, 0, 0), 0), (12, (-0.05, 0.05, 0.07), -35), (22, (-0.08, 0.07, 0.10), T), (28, (-0.08, 0.07, 0.10), T + 5),
                (34, (-0.08, 0.07, 0.10), T - 4), (40, (-0.08, 0.07, 0.10), T + 4), (46, (-0.08, 0.07, 0.10), T),
                (56, (-0.03, 0.03, 0.04), -25), (66, (0, 0, 0), 0)]
    def kok_at(fr):
        for (f0, l0, r0), (f1, l1, r1) in zip(kok_keys, kok_keys[1:]):
            if f0 <= fr <= f1:
                t = (fr - f0) / (f1 - f0)
                return Vector(l0).lerp(Vector(l1), t), r0 + (r1 - r0) * t
        return Vector(kok_keys[-1][1]), kok_keys[-1][2]
    def mouth_at(fr):
        l, r = kok_at(fr)
        R = Matrix.Rotation(math.radians(r), 3, "Y")
        return G + R @ (M0 - G) + l
    if kind == "gaz":
        eff_keys = [(1, H0, 0.0), (20, H0, 0.0), (24, 0.7, 0.02), (32, 1.3, 0.08), (40, 1.7, 0.16), (46, H0, 0.2), (66, H0, 0.0)]
    else:
        eff_keys = [(1, H0, 0.0), (21, H0, 0.0), (23, 0.35, 0.0), (26, 1.0, 0.0), (46, 1.0, 0.0), (50, H0, 0.0), (66, H0, 0.0)]

    def dok(a):
        for fr, l, r in kok_keys:
            key_pose(a, fr, {"Kok": pose_delta(a, "Kok", loc=l, rot=(0, r, 0), pivot=G)})
        frames = sorted(set([k[0] for k in kok_keys] + [k[0] for k in eff_keys]))
        for fr in frames:
            s, up = next(((s, z) for f, s, z in eff_keys if f == fr), (None, None))
            if s is None:   # efekt anahtarı yoksa en yakın öncekini kullan
                prev = [k for k in eff_keys if k[0] <= fr][-1]
                s, up = prev[1], prev[2]
            d = mouth_at(fr) - M0 + Vector((0, 0, up))
            sc = (s, s, s) if kind == "gaz" else (1.0, s, 1.0) if s > H0 else (H0, H0, H0)
            key_pose(a, fr, {"Efekt": pose_delta(a, "Efekt", loc=tuple(d), scale=sc)})
    new_clip(arm, "El_Dok", 66, dok)
    return arm

# ================================================================== toplu kurulum + dışa aktarım
ANIM_RIGS = ["Kapi_Rig", "Kazan_Rig", "Ocak_Ates_Rig", "Damitma_Rig", "Kabile_Rig", "SuBanyosu_Rig", "Havan_Rig", "Koruk_Rig"]

def build_anims(res):
    RIG_DEFAULTS.clear()
    rig_door()
    rig_ocak_ates()
    k = rig_kazan()
    rig_damitma(res["damitma"])
    rig_kabile()
    rig_su_banyosu()
    rig_havan()
    rig_koruk(res)
    items = [n for n, fn, row in CATALOG]
    for n in items:
        rig_item(n)
    return items

STATIC_COLS = ("Atolye", "Atolye_Esya", "Atolye_Ic", "Simya_Tezgahi")

def export_static(path=None):
    """Hareketsiz atölye (kabuk, mobilya, iç düzen, tezgâh parçaları) tek FBX; dünya konumunda (origin = atölye)."""
    path = path or EXPORT_ROOT + "\\Atolye\\Atolye_Statik.fbx"
    import os as _os
    _os.makedirs(_os.path.dirname(path), exist_ok=True)
    for o in list(bpy.context.selected_objects):
        o.select_set(False)
    n = 0
    for cn in STATIC_COLS:
        c = bpy.data.collections.get(cn)
        if not c:
            continue
        for o in c.all_objects:
            if o.type in ("MESH", "EMPTY"):
                o.select_set(True)
                n += 1
    mods = triangulate_for_export(list(bpy.context.selected_objects))
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH", "EMPTY"},
        apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        mesh_smooth_type="OFF", use_mesh_modifiers=True, colors_type="LINEAR", bake_anim=False, add_leaf_bones=False)
    remove_export_mods(mods)
    for o in list(bpy.context.selected_objects):
        o.select_set(False)
    return path, n

def export_all():
    paths = [export_static()[0]]
    for r in ANIM_RIGS:
        paths.append(export_rig(bpy.data.objects[r], EXPORT_ROOT + "\\Animasyon\\" + r + ".fbx"))
    for n, fn, row in CATALOG:
        paths.append(export_rig(bpy.data.objects[n + "_Rig"], EXPORT_ROOT + "\\Esya\\" + n + ".fbx"))
    return paths

# ================================================================== SU BANYOSU
def rig_su_banyosu():
    base = Vector(bpy.data.objects["SM_SuBanyosu_Mangal"].location)
    b1, b2 = bpy.data.objects["SuBanyosu_Buhar_1"], bpy.data.objects["SuBanyosu_Buhar_2"]
    bones = [_bone_up("Kok", (0, 0, 0)),
             _bone_up("Ates", tuple(Vector(bpy.data.objects["SuBanyosu_Ates"].location) - base), parent="Kok"),
             _bone_up("Kor", (0, 0, 0.14), parent="Kok"),
             _bone_up("Buhar_1", tuple(Vector(b1.location) - base), parent="Kok"),
             _bone_up("Buhar_2", tuple(Vector(b2.location) - base), parent="Kok")]
    arm = make_rig("SuBanyosu_Rig", tuple(base), bones)
    bind(bpy.data.objects["SuBanyosu_Ates"], arm, "Ates")
    bind(bpy.data.objects["SuBanyosu_Kor"], arm, "Kor")
    bind(b1, arm, "Buhar_1")
    bind(b2, arm, "Buhar_2")
    for n in ("SM_SuBanyosu_Mangal", "SM_SuBanyosu", "SuBanyosu_Su"):
        bind(bpy.data.objects[n], arm, "Kok")
    RIG_DEFAULTS[arm.name] = {b: "gizli" for b in ("Ates", "Kor", "Buhar_1", "Buhar_2")}
    new_clip(arm, "SuBanyosu_Bosta", 1, lambda a: _hide(a, 1, ["Ates", "Kor", "Buhar_1", "Buhar_2"]))

    def dongu(a):
        _flicker(a, "Ates", 1, 73, amp=0.12, seed=8860)
        for k, b in enumerate(("Buhar_1", "Buhar_2")):
            o = 1 + k * 36
            seq = [(1, H0, 0.0)] if o > 1 else []
            seq += [(o, H0, 0.0), (o + 10, 0.6, 0.06), (o + 24, 1.1, 0.2), (o + 34, H0, 0.3)]
            seq.append((73, H0, 0.0))
            for fr, (s, z) in sorted(dict((f, (s, z)) for f, s, z in seq if 1 <= f <= 73).items()):
                key_pose(a, fr, {b: pose_delta(a, b, loc=(0.01 * (k * 2 - 1), 0, z), scale=s)})
    new_clip(arm, "SuBanyosu_Dongu", 73, dongu, loop=True)
    new_clip(arm, "SuBanyosu_Basla", 20, lambda a: ([key_pose(a, fr, {"Ates": pose_delta(a, "Ates", scale=s), "Kor": pose_delta(a, "Kor", scale=min(1, max(H0, s)))})
                                                     for fr, s in ((1, H0), (10, 1.15), (20, 1.0))], _hide(a, 1, ["Buhar_1", "Buhar_2"]), _hide(a, 20, ["Buhar_1", "Buhar_2"])))
    new_clip(arm, "SuBanyosu_Dur", 16, lambda a: ([key_pose(a, fr, {"Ates": pose_delta(a, "Ates", scale=s), "Kor": pose_delta(a, "Kor", scale=max(H0, min(1, s)))})
                                                   for fr, s in ((1, 1.0), (16, H0))], _hide(a, 1, ["Buhar_1", "Buhar_2"])))
    return arm

# ================================================================== HAVAN
def rig_havan():
    hv = bpy.data.objects["SM_Tezgah_Havan"]
    pe = bpy.data.objects["Tezgah_Havaneli"]
    base = Vector(hv.location)
    pz = pe.location.z - base.z
    arm = make_rig("Havan_Rig", tuple(base), [_bone_up("Kok", (0, 0, 0)), _bone_up("Havaneli", (0, 0, pz), 0.29, "Kok")])
    bind(hv, arm, "Kok")
    bind(bpy.data.objects["SM_Havan_Toz"], arm, "Kok")
    bind(pe, arm, "Havaneli")
    piv = (0, 0, pz)
    # boşta: havaneli kâsenin ağzına yaslı (eğik)
    new_clip(arm, "Havan_Bosta", 1, lambda a: key_pose(a, 1, {"Havaneli": pose_delta(a, "Havaneli", loc=(-0.05, 0, 0.0), rot=(0, 24, 0), pivot=piv)}))

    def dov(a):
        for fr, z, r in ((1, 0.0, 0), (8, 0.1, -6), (12, 0.11, -7), (16, 0.0, 2), (18, -0.004, 0), (25, 0.0, 0)):
            key_pose(a, fr, {"Havaneli": pose_delta(a, "Havaneli", loc=(0, 0, z), rot=(0, r, 0), pivot=piv)})
    new_clip(arm, "Havan_Dov_Dongu", 25, dov, loop=True)

    def ez(a):
        # ezme: havaneli 16° yatık, ucu kâsede daire çizer
        for i in range(5):
            ang = math.radians(i * 90)
            M = pose_delta(a, "Havaneli", rot=(16 * math.sin(ang), -16 * math.cos(ang), 0), pivot=piv)
            key_pose(a, 1 + i * 12, {"Havaneli": M})
    new_clip(arm, "Havan_Ez_Dongu", 49, ez, loop=True, interp="LINEAR")
    return arm

# ================================================================== KÖRÜK
def rig_koruk(res):
    alt = bpy.data.objects["SM_Koruk_Alt"]
    base = Vector(alt.location)
    yaw = res["koruk"]["yaw"]
    ust = bpy.data.objects["Koruk_Ust"]
    deri = bpy.data.objects["Koruk_Deri"]
    arm = make_rig("Koruk_Rig", tuple(base), [_bone_up("Kok", (0, 0, 0)), _bone_up("Ust", (0, 0, ust.location.z - base.z), 0.05, "Kok"),
                                              _bone_up("Deri", (0, 0, 0), 0.07, "Kok")])
    arm.rotation_euler = (0, 0, yaw)
    bpy.context.view_layer.update()
    for o, b in ((alt, "Kok"), (ust, "Ust"), (deri, "Deri")):
        bind(o, arm, b)
    hz = ust.location.z - base.z
    new_clip(arm, "Koruk_Bosta", 1, lambda a: key_pose(a, 1, {"Ust": pose_delta(a, "Ust"), "Deri": pose_delta(a, "Deri")}))

    def pomp(a):
        for fr, ang in ((1, 0.0), (10, -13.0), (16, -13.5), (24, -1.0), (31, 0.0)):
            open_k = abs(ang) / 13.5
            key_pose(a, fr, {"Ust": pose_delta(a, "Ust", rot=(0, ang, 0), pivot=(0, 0, hz)),
                             "Deri": pose_delta(a, "Deri", scale=(1, 1 + 0.9 * open_k, 1))})
    new_clip(arm, "Koruk_Pompala_Dongu", 31, pomp, loop=True)
    return arm

