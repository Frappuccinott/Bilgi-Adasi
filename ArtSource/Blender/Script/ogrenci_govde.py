# Bilgi Adası — Öğrencinin gövdesi (baş ve kollar hariç): sweatshirt gövde, pantolon, spor ayakkabı + iskelet + klipler.
# Kollar ayrı rig'dir (ogrenci_kol.py, kameraya bağlı) ve bu script ona DOKUNMAZ: kol klipleri aynen kalır.
# Referans: ArtSource/Referans/ogrenci_govde.md · Kurallar: 09-tema-ve-model-kurallari.md
# Yarışma süresince yazıldı (0'dan üret; hazır mesh/üretici yok). Blender 5.x. Yardımcılar: ba_lib.py + npc.py.
#
# Kullanım (Blender içinden / MCP):
#   p = r"<repo>/ArtSource/Blender/Script/ogrenci_govde.py"
#   exec(compile(open(p, encoding="utf-8").read(), p, "exec"), globals())
#   rig, ob = build_ogrenci_govde()
#   export_ogrenci_govde()      # Assets/_Project/Art/Animasyon/Ogrenci_Govde_Rig.fbx
#
# Yön: önü Blender -Y (Unity +Z), solu +X, zemin z=0. Göz 1,56 m (kol rig'inin kökü) — gövde bununla hizalı:
# omuz eklemi gözün 0,27 m altında, ±0,19 m yanda (kol rig'inin omzu). Baş yok; yalnızca gölge için ayrı "Golge_Bas".

import os
import math
import bmesh
import bpy
from mathutils import Vector

_HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else \
    r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\ArtSource\Blender\Script"
_npc = os.path.join(_HERE, "npc.py")
exec(compile(open(_npc, encoding="utf-8").read(), _npc, "exec"), globals())

G_COL = "Ogrenci_Govde"
G_RIG = "Ogrenci_Govde_Rig"
G_EXPORT = r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\Assets\_Project\Art\Animasyon\Ogrenci_Govde_Rig.fbx"
G_FPS = 30
H, HR = 1.66, 0.13        # boy, baş yarıçapı (göz 1,56 → skeleton'da baş merkezi 1,52)
HIP_K = 0.52              # kalça 0,86 m (NPC'lerden uzun bacak: velosipedin en uzak pedal noktasına ~0,86 m yetişir)

# npc.py'deki make_armature / merge bu listeleri kullanır: gövde rig'inde baş, boyun ve kol yok.
BONES = ["Kalca", "Omurga", "Gogus",
         "Bacak_Ust_Sol", "Bacak_Alt_Sol", "Ayak_Sol", "Bacak_Ust_Sag", "Bacak_Alt_Sag", "Ayak_Sag"]
BONE_PARENT = {"Omurga": "Kalca", "Gogus": "Omurga",
               "Bacak_Ust_Sol": "Kalca", "Bacak_Alt_Sol": "Bacak_Ust_Sol", "Ayak_Sol": "Bacak_Alt_Sol",
               "Bacak_Ust_Sag": "Kalca", "Bacak_Alt_Sag": "Bacak_Ust_Sag", "Ayak_Sag": "Bacak_Alt_Sag"}


# ------------------------------------------------------------------ renkler (yalnız paletten / türetilmiş, 09 §3)
def _k(c, k):
    return (c[0] * k, c[1] * k, c[2] * k, 1.0)

SW = (mix(pal("dag", 1), pal("dag", 2), 0.45), mix(pal("dag", 1), pal("dag", 0), 0.35))   # kollarla aynı sweatshirt
SW_RIB = (mix(pal("dag", 2), pal("dag", 1), 0.2), mix(pal("dag", 2), pal("dag", 1), 0.45))  # ribana / kıvrım gölgesi
_pant = mix(pal("kaya", 2), pal("dag", 2), 0.35)
PANT = (_k(_pant, 0.55), _k(_pant, 0.78))            # koyu gri-mavi okul pantolonu (siyah değil)
SKIN = (pal("ten", 1), pal("ten", 0))
SOLE = (mix(pal("kaya", 0), pal("siva", 0), 0.5), pal("siva", 0))
SHOE = (mix(pal("cati", 2), pal("cati", 1), 0.35), mix(pal("cati", 1), pal("cati", 0), 0.25))   # turuncu spor ayakkabı
LACE = (pal("siva", 1), pal("siva", 0))


# ------------------------------------------------------------------ ağırlık (yumuşak eklem)
def _blend(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)

def torso_w(co):
    """Gövde: kalça → bel → göğüs yumuşak geçiş (oturma/çömelme bükülmesi kırılmasın)."""
    z = co.z
    if z < 0.90:
        return {"Kalca": 1.0}
    if z < 1.02:
        t = _blend((z - 0.90) / 0.12)
        return {"Kalca": 1 - t, "Omurga": t}
    if z < 1.10:
        return {"Omurga": 1.0}
    if z < 1.22:
        t = _blend((z - 1.10) / 0.12)
        return {"Omurga": 1 - t, "Gogus": t}
    return {"Gogus": 1.0}

def leg_w(J, s):
    hj, kn = J["hj_" + s], J["kn_" + s]
    up, lo = "Bacak_Ust_" + s, "Bacak_Alt_" + s

    def f(co):
        z = co.z
        if z >= hj.z + 0.06:
            return {"Kalca": 1.0}
        if z >= hj.z - 0.06:
            t = _blend((z - (hj.z - 0.06)) / 0.12)
            return {"Kalca": t, up: 1 - t}
        if z >= kn.z + 0.05:
            return {up: 1.0}
        if z >= kn.z - 0.05:
            t = _blend((z - (kn.z - 0.05)) / 0.10)
            return {up: t, lo: 1 - t}
        return {lo: 1.0}
    return f

def pelvis_w(J):
    fl, fr = leg_w(J, "Sol"), leg_w(J, "Sag")

    def f(co):
        if abs(co.x) < 0.035:
            return torso_w(co)
        return fl(co) if co.x > 0 else fr(co)
    return f

def merge_soft(parts):
    """parts: (bmesh, kemik adı | fonksiyon(co) -> {kemik: ağırlık})."""
    out = bmesh.new()
    lay_out = out.loops.layers.float_color.new("Col")
    dl = out.verts.layers.deform.verify()
    for bm, spec in parts:
        lay_in = bm.loops.layers.float_color.get("Col")
        vmap = {}
        for v in bm.verts:
            nv = out.verts.new(v.co)
            ws = {spec: 1.0} if isinstance(spec, str) else spec(v.co)
            tot = sum(ws.values()) or 1.0
            for b, w in ws.items():
                if w > 1e-4:
                    nv[dl][BONES.index(b)] = w / tot
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


# ------------------------------------------------------------------ model
def govde_parts():
    J = skeleton(H, HR, hip_k=HIP_K, sh_w=0.19, arm_out=0.05)
    P = []
    hip, waist, neck = J["hip"].z, J["waist"].z, J["neck"].z
    sh = J["sh_Sol"].z

    # --- sweatshirt gövdesi (önü hafif basık, sırtı dolgun): etek kalçanın altında, yaka boyunda
    prof = [(0.0, hip - 0.04), (0.17, hip - 0.04), (0.205, hip - 0.02), (0.20, waist - 0.04),
            (0.195, waist + 0.04), (0.212, J["chest"].z - 0.02), (0.222, sh - 0.06), (0.205, sh + 0.01),
            (0.15, neck - 0.005), (0.085, neck + 0.02), (0.0, neck + 0.02)]
    P.append((paint(lathe(prof, sides=18, sy=0.70), SW, seed=41), torso_w))
    # etek ribanası (bel lastiği): kalın halka, koyu
    P.append((paint(torus(0.205, 0.022, c=(0, 0, hip - 0.02), sides=18, ring=6, sy=0.72), SW_RIB, noise=0.0), torso_w))
    # kanguru cep: önde yassı kutu + üst kenar gölgesi
    pz = hip + 0.07
    P.append((paint(box((0, -0.145, pz), (0.25, 0.022, 0.13), rot=(-6, 0, 0), bevel=0.012), SW, seed=43), torso_w))
    P.append((paint(box((0, -0.158, pz + 0.062), (0.22, 0.012, 0.012), bevel=0.004), SW_RIB, noise=0.0), torso_w))
    # omuz başları: kol rig'inin üst kol ucunu örter (yana bakınca kol "kesik" görünmesin)
    for sx in (1, -1):
        c = Vector((sx * 0.19, 0.01, sh - 0.025))
        P.append((paint(ell(c, (0.085, 0.08, 0.075), u=12, v=8), SW, seed=44), "Gogus"))
    # kapüşon: sırtta yatan yumuşak kütle + yaka çevresinde kalın kenar
    P.append((paint(ell((0, 0.10, neck - 0.06), (0.16, 0.075, 0.10), u=14, v=8), SW, seed=45), "Gogus"))
    P.append((paint(torus(0.105, 0.028, c=(0, 0.015, neck + 0.005), rot=(-12, 0, 0), sides=16, ring=6, sy=0.92),
                    SW_RIB, noise=0.0), "Gogus"))
    # kapüşon ipleri + metal uçlar (aşağı bakınca göğüste görünen ayrıntı)
    for sx in (1, -1):
        a = Vector((sx * 0.045, -0.105, neck - 0.01))
        b = Vector((sx * 0.055, -0.152, J["chest"].z - 0.03))
        P.append((paint(tube(a, b, 0.006, 0.006, sides=6), LACE, noise=0.0), "Gogus"))
        P.append((paint(tube(b, b + Vector((0, -0.004, -0.03)), 0.008, 0.008, sides=6),
                        (pal("kaya", 2), pal("kaya", 1)), noise=0.0), "Gogus"))
    # boyun (ten): yakanın içini doldurur; üstü kapalı, kameranın 0,13 m altında
    P.append((paint(tube((0, 0.0, neck - 0.03), (0, -0.005, neck + 0.08), 0.052, 0.05, sides=12), SKIN), "Gogus"))

    # --- pantolon: leğen + uyluk + diz + baldır (geniş paça)
    P.append((paint(lathe([(0.0, hip - 0.15), (0.11, hip - 0.14), (0.175, hip - 0.10), (0.195, hip - 0.03),
                           (0.19, hip + 0.04)], sides=18, sy=0.74), PANT, seed=46), pelvis_w(J)))
    for s in ("Sol", "Sag"):
        sx = 1 if s == "Sol" else -1
        hj, kn, an = J["hj_" + s], J["kn_" + s], J["an_" + s]
        w = leg_w(J, s)
        P.append((paint(tube(hj + Vector((0, 0, 0.05)), kn + Vector((0, 0, 0.02)), 0.088, 0.066, sides=12),
                        PANT, seed=47), w))
        P.append((paint(ell(kn + Vector((0, -0.006, 0)), (0.066, 0.068, 0.07), u=12, v=8), PANT), w))
        hem = an + Vector((0, 0.004, 0.065))
        P.append((paint(tube(kn - Vector((0, 0, 0.02)), hem, 0.064, 0.062, sides=12), PANT, seed=48), w))
        # paça kıvrımı (koyu halka)
        P.append((paint(torus(0.062, 0.011, c=hem, sides=12, ring=5), _k(PANT[0], 0.85), noise=0.0),
                  "Bacak_Alt_" + s))
        # --- spor ayakkabı: kalın açık taban + turuncu üst + beyaz şerit + bağcık
        base = Vector((an.x + sx * 0.004, an.y - 0.045, 0.0))
        P.append((paint(ell(base + Vector((0, -0.005, 0.022)), (0.056, 0.135, 0.026), u=14, v=6), SOLE, noise=0.0),
                  "Ayak_" + s))
        P.append((paint(ell(base + Vector((0, 0.005, 0.058)), (0.052, 0.118, 0.052), u=14, v=8), SHOE, seed=49),
                  "Ayak_" + s))
        P.append((paint(tube(an + Vector((0, 0.012, -0.02)), an + Vector((0, 0.01, 0.05)), 0.054, 0.052, sides=12),
                        SHOE), "Ayak_" + s))
        for k in range(3):        # bağcık çizgileri (dil üstünde)
            y = base.y - 0.03 - k * 0.026
            P.append((paint(box((base.x, y, 0.103 - k * 0.008), (0.05, 0.008, 0.006), rot=(18, 0, 0)), LACE,
                            noise=0.0), "Ayak_" + s))
        P.append((paint(box((base.x + sx * 0.05, base.y + 0.005, 0.05), (0.008, 0.11, 0.016), rot=(0, 0, 0),
                            bevel=0.003), LACE, noise=0.0), "Ayak_" + s))
    return J, P

def golge_bas(J):
    """Yalnızca gölge için baş (Unity'de ShadowsOnly): oyuncunun gölgesi başsız görünmesin."""
    P = [(ell(J["head_c"], (HR * 0.95, HR * 1.0, HR * 1.08), u=12, v=8), "Gogus"),
         (tube(J["neck"], J["head_b"] + Vector((0, 0, 0.03)), 0.05, 0.045), "Gogus")]
    for bm, _ in P:
        paint(bm, SKIN)
    return P


# ------------------------------------------------------------------ pozlar (derece; kemik yerel X = bükme)
def leg(s, flex, knee, dorsi=None, out=0.0):
    """flex: uyluk öne (+), knee: diz bükümü (+), dorsi: ayak ucu yukarı (+; None = ayak yere paralel)."""
    if dorsi is None:
        dorsi = knee - flex
    return {"Bacak_Ust_" + s: (-flex, 0, (out if s == "Sol" else -out)), "Bacak_Alt_" + s: (knee, 0, 0),
            "Ayak_" + s: (-dorsi, 0, 0)}

def pose(*dicts):
    out = {}
    for d in dicts:
        out.update(d)
    return out

def walk_frame(ph, base_flex=0.0, base_knee=0.0, amp=1.0, crouch=False):
    """ph 0..1 = iki adım. 0'da sol ayak önde basar."""
    s = math.sin(2 * math.pi * ph)
    swing_l = max(0.0, math.sin(2 * math.pi * ph - math.pi / 2))     # sol bacak geri→ileri savrulurken
    swing_r = max(0.0, math.sin(2 * math.pi * ph + math.pi / 2))
    th, kl = (15, 22) if crouch else (27, 48)
    fl = base_flex + amp * th * s
    fr = base_flex - amp * th * s
    kl_ = base_knee + amp * (kl * swing_l + 6)
    kr_ = base_knee + amp * (kl * swing_r + 6)
    p = pose(leg("Sol", fl, kl_, dorsi=(kl_ - fl) + 10 * swing_l if crouch else 12 * swing_l - 4),
             leg("Sag", fr, kr_, dorsi=(kr_ - fr) + 10 * swing_r if crouch else 12 * swing_r - 4))
    p["Kalca"] = (0, 5 * amp * s, 0)          # leğen adımla döner
    p["Gogus"] = (-1.5 if not crouch else 0, -6 * amp * s, 0)
    return p

CROUCH = pose(leg("Sol", 108, 148, out=6), leg("Sag", 108, 148, out=6), {"Omurga": (6, 0, 0), "Gogus": (4, 0, 0)})
CROUCH_LOC = (0, -0.585, -0.10)    # Kalca yerel: Y yukarı, Z ileri → leğen çömelir, hafif geri (ayaklar altında)
SEAT = pose(leg("Sol", 78, 92), leg("Sag", 78, 92), {"Omurga": (5, 0, 0), "Gogus": (4, 0, 0)})
AIR = pose(leg("Sol", 32, 48), leg("Sag", 18, 30), {"Gogus": (-2, 0, 0)})


def _act(rig, name, frames, locs=None):
    for a in [a for a in bpy.data.actions if a.name == name]:
        bpy.data.actions.remove(a)
    act = bpy.data.actions.new(name)
    if locs is None:      # kalça konumu her klipte anahtarlı olsun (yoksa önceki klibin çömelme konumu kalır)
        locs = {f: {"Kalca": (0, 0, 0)} for f in frames}
    _key(rig, act, frames, locs)
    act.use_fake_user = True
    return act

def build_govde_anims(rig):
    L = G_FPS
    acts = []
    # bekle: neredeyse durağan, çok hafif nefes (yürüme ile aynı 1 sn döngü → karışımda tempo bozulmaz)
    acts.append(_act(rig, "Govde_Bekle_Dongu",
                     {1: {"Gogus": (0, 0, 0)}, L // 2 + 1: {"Gogus": (0.8, 0, 0)}, L + 1: {"Gogus": (0, 0, 0)}}))
    frames, locs = {}, {}
    for i in range(9):
        f = 1 + i * L // 8
        ph = i / 8
        frames[f] = walk_frame(ph)
        locs[f] = {"Kalca": (0, -0.022 * abs(math.sin(2 * math.pi * ph)), 0)}
    acts.append(_act(rig, "Govde_Yuru_Dongu", frames, locs))
    frames, locs = {}, {}
    for i in range(9):
        f = 1 + i * L // 8
        ph = i / 8
        frames[f] = pose(CROUCH, walk_frame(ph, base_flex=108, base_knee=142, amp=1.0, crouch=True),
                         {"Omurga": (6, 0, 0), "Gogus": (4, -4 * math.sin(2 * math.pi * ph), 0)})
        locs[f] = {"Kalca": (CROUCH_LOC[0], CROUCH_LOC[1] - 0.012 * abs(math.sin(2 * math.pi * ph)), CROUCH_LOC[2])}
    acts.append(_act(rig, "Govde_Egil_Yuru_Dongu", frames, locs))
    acts.append(_act(rig, "Govde_Egil_Dongu", {1: CROUCH, L + 1: CROUCH},
                     {1: {"Kalca": CROUCH_LOC}, L + 1: {"Kalca": CROUCH_LOC}}))
    acts.append(_act(rig, "Govde_Havada_Dongu", {1: AIR, L + 1: AIR}))
    acts.append(_act(rig, "Govde_Bisiklet_Dongu", {1: SEAT, L + 1: SEAT}))
    for act in acts:
        try:
            fcs = list(act.fcurves)
        except AttributeError:      # Blender 5: katmanlı action
            fcs = [fc for layer in act.layers for strip in layer.strips for cb in strip.channelbags for fc in cb.fcurves]
        for fc in fcs:
            fc.modifiers.new("CYCLES")
            for kp in fc.keyframe_points:
                kp.interpolation = "BEZIER"
    rig.animation_data.action = None
    tracks = rig.animation_data.nla_tracks
    for t in list(tracks):
        tracks.remove(t)
    for act in acts:
        tr = tracks.new()
        tr.name = act.name
        tr.strips.new(act.name, 1, act)
        tr.mute = True
    rig.animation_data.action = acts[0]
    return acts


def build_ogrenci_govde():
    act_ob = bpy.context.view_layer.objects.active
    if act_ob and act_ob.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    col = collection(G_COL)
    clear_collection(G_COL)
    for arm in [a for a in bpy.data.armatures if a.users == 0]:
        bpy.data.armatures.remove(arm)
    bpy.context.scene.render.fps = G_FPS
    J, parts = govde_parts()
    rig = make_armature(G_RIG, J, col, 0.0)
    rig.location = (3.0, -3.0, 0.0)     # atölye/NPC sahnesinden uzakta kurulur; dışa aktarımda 0'a alınır
    ob = to_object(merge_soft(parts), "Ogrenci_Govde", col, sharp_angle=None)
    head = to_object(merge_soft(golge_bas(J)), "Ogrenci_Golge_Bas", col, sharp_angle=None)
    for o in (ob, head):
        for bn in BONES:
            o.vertex_groups.new(name=bn)
        o.parent = rig
        mod = o.modifiers.new("Armature", "ARMATURE")
        mod.object = rig
    build_govde_anims(rig)
    print("Ogrenci_Govde üçgen:", tri_count([ob]), "| gölge başı:", tri_count([head]))
    return rig, ob


def export_ogrenci_govde(path=G_EXPORT):
    rig = bpy.data.objects[G_RIG]
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
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH", "ARMATURE"},
        apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        mesh_smooth_type="OFF", use_mesh_modifiers=False, colors_type="LINEAR", add_leaf_bones=False,
        primary_bone_axis="Y", secondary_bone_axis="X", armature_nodetype="NULL",
        bake_anim=True, bake_anim_use_nla_strips=True, bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0)
    for tr in rig.animation_data.nla_tracks:
        tr.mute = True
    rig.animation_data.action = bpy.data.actions.get("Govde_Bekle_Dongu")
    rig.location = old
    return path
