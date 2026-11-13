# Bilgi Adası — animasyon ortak araçları (iskelet, klip, NLA, FBX dışa aktarım)
# Tüm animasyonlar Blender'da üretilir; Unity'de yalnızca klip oynatılır.
# Yöntem: her animasyonlu varlık = bir Armature + kemiklere bağlı (deform olmayan) mesh'ler.
# Her klip ayrı bir Action → NLA şeridi → FBX'te ayrı "take" → Unity'de ayrı AnimationClip.
# Adlandırma: döngü klipleri "_Dongu" ile biter (Unity içe aktarma aracı Loop Time'ı bundan açar).
# Önce ba_lib.py.

import math
import os
import bpy
from mathutils import Matrix, Vector, Quaternion, Euler

FPS = 30
ANIM_COL = "Animasyonlu"
EXPORT_ROOT = r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\Assets\_Project\Art"

def anim_col():
    return collection(ANIM_COL)

def set_fps():
    sc = bpy.context.scene
    sc.render.fps = FPS
    sc.render.fps_base = 1.0

def make_rig(name, loc, bones):
    """bones: [(ad, baş(yerel), kuyruk(yerel), ebeveyn|None)]. Döner: armature objesi.
    Kemiğin yerel Y'si baş→kuyruk yönüdür; poz anahtarlarını armature uzayında matrisle verdiğimiz için
    eksen karışıklığı olmaz (key_pose)."""
    old = bpy.data.objects.get(name)
    if old:
        bpy.data.objects.remove(old, do_unlink=True)
    arm_data = bpy.data.armatures.new(name)
    arm = bpy.data.objects.new(name, arm_data)
    arm.location = loc
    anim_col().objects.link(arm)
    arm.show_in_front = True
    vl = bpy.context.view_layer
    prev_active = vl.objects.active
    for o in list(bpy.context.selected_objects):
        o.select_set(False)
    vl.objects.active = arm
    arm.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    eb = arm_data.edit_bones
    for bname, head, tail, parent in bones:
        b = eb.new(bname)
        b.head = head
        b.tail = tail
        b.use_deform = False
        if parent:
            b.parent = eb[parent]
            b.use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    arm.select_set(False)
    return arm

def bind(ob, arm, bone):
    """Mesh'i dünya konumunu koruyarak kemiğe bağla (bone parent, deform yok)."""
    bpy.context.view_layer.update()
    mw = ob.matrix_world.copy()
    ob.parent = arm
    ob.parent_type = "BONE"
    ob.parent_bone = bone
    bpy.context.view_layer.update()
    ob.matrix_world = mw
    if ob.name not in anim_col().objects:
        for c in list(ob.users_collection):
            c.objects.unlink(ob)
        anim_col().objects.link(ob)

def rest_matrix(arm, bone):
    return arm.data.bones[bone].matrix_local.copy()

def key_pose(arm, frame, poses):
    """poses: {kemik: Matrix (armature uzayında hedef matris)}. Kemik anahtarlarını yazar.
    Kolaylık: hedef = dinlenme matrisi ile çarpılmış bir 'delta' için pose_delta() kullan."""
    sc = bpy.context.scene
    sc.frame_set(frame)
    for bname, M in poses.items():
        pb = arm.pose.bones[bname]
        pb.rotation_mode = "QUATERNION"
        pb.matrix = M
        bpy.context.view_layer.update()
        pb.keyframe_insert("location", frame=frame)
        pb.keyframe_insert("rotation_quaternion", frame=frame)
        pb.keyframe_insert("scale", frame=frame)

def pose_delta(arm, bone, loc=(0, 0, 0), rot=(0, 0, 0), scale=1.0, pivot=None):
    """Armature uzayında: dinlenme matrisini 'pivot' çevresinde döndür (derece, XYZ), kaydır, ölçekle."""
    R = rest_matrix(arm, bone)
    p = Vector(pivot) if pivot is not None else R.translation.copy()
    rot_m = Euler([math.radians(a) for a in rot], "XYZ").to_matrix().to_4x4()
    S = Matrix.Diagonal((scale, scale, scale, 1.0)) if not isinstance(scale, (tuple, list)) else Matrix.Diagonal((*scale, 1.0))
    T = Matrix.Translation(Vector(loc))
    return T @ Matrix.Translation(p) @ rot_m @ Matrix.Translation(-p) @ R @ S

RIG_DEFAULTS = {}   # iskelet adı → {kemik: "gizli" | None}; anahtarsız kemiklere yazılacak değer

def new_clip(arm, name, frames, keyer, loop=False, interp="BEZIER"):
    """Bir klip üret: keyer(arm) anahtarları yazar. Action NLA şeridine itilir (FBX take adı = name).
    Unity Animator, klibin anahtar vermediği kemikleri varsayılan poza döndürür; bu yüzden anahtarsız
    kemiklere ilk ve son karede RIG_DEFAULTS değeri (gizli ya da dinlenme) yazılır."""
    set_fps()
    ad = arm.animation_data or arm.animation_data_create()
    act = bpy.data.actions.new(name)
    ad.action = act
    for pb in arm.pose.bones:
        pb.matrix_basis = Matrix.Identity(4)
    keyer(arm)
    keyed = set()
    for fc in _fcurves(act):
        if fc.data_path.startswith('pose.bones["'):
            keyed.add(fc.data_path.split('"')[1])
    defaults = RIG_DEFAULTS.get(arm.name, {})
    last = max(1, int(round(act.frame_range[1])))
    for pb in arm.pose.bones:
        if pb.name in keyed:
            continue
        hidden = defaults.get(pb.name) == "gizli"
        for fr in sorted({1, last}):
            M = pose_delta(arm, pb.name, scale=1e-3 if hidden else 1.0)
            key_pose(arm, fr, {pb.name: M})
    # eğri yumuşatma / döngü
    for fc in _fcurves(act):
        for kp in fc.keyframe_points:
            kp.interpolation = interp
        if loop:
            m = fc.modifiers.new("CYCLES")
    track = ad.nla_tracks.new()
    track.name = name
    strip = track.strips.new(name, int(act.frame_range[0]), act)
    strip.name = name
    track.mute = True
    ad.action = None
    for pb in arm.pose.bones:
        pb.matrix_basis = Matrix.Identity(4)
    return act

def _fcurves(act):
    """Blender 4.4+ katmanlı (slotted) action ve eski API ile uyumlu F-curve listesi."""
    if hasattr(act, "fcurves") and len(getattr(act, "fcurves", [])):
        return list(act.fcurves)
    out = []
    for layer in getattr(act, "layers", []):
        for strip in layer.strips:
            for cb in getattr(strip, "channelbags", []):
                out += list(cb.fcurves)
    return out

def preview_clip(arm, name, frame=None):
    """Önizleme: yalnız bu klibin NLA izini aç (dışa aktarımla aynı değerlendirme yolu)."""
    ad = arm.animation_data
    ad.action = None
    for t in ad.nla_tracks:
        t.mute = (t.name != name)
    if frame is not None:
        bpy.context.scene.frame_set(frame)

def clear_preview(arm):
    if arm.animation_data:
        arm.animation_data.action = None
        for t in arm.animation_data.nla_tracks:
            t.mute = True
    for pb in arm.pose.bones:
        pb.matrix_basis = Matrix.Identity(4)

def triangulate_for_export(objs):
    """Dışa aktarım süresince geçici Triangulate modifier'ı (Blender'daki mesh değişmez).
    Unity çokgenleri kendisi üçgenlerken eğik dörtgen/çokgenleri atabiliyor; üçgen göndeririz."""
    mods = []
    for o in objs:
        if o.type != "MESH":
            continue
        m = o.modifiers.new("_FBX_Ucgen", "TRIANGULATE")
        m.quad_method = "BEAUTY"
        m.ngon_method = "BEAUTY"
        m.min_vertices = 4
        if hasattr(m, "keep_custom_normals"):
            m.keep_custom_normals = True
        mods.append((o, m))
    return mods

def remove_export_mods(mods):
    for o, m in mods:
        o.modifiers.remove(m)

def export_rig(arm, path):
    """Armature + bağlı mesh'leri FBX'e; her NLA şeridi ayrı take. Eksen: -Z ileri, Y yukarı (09 §7)."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    clear_preview(arm)
    for t in arm.animation_data.nla_tracks:
        t.mute = False
    for o in list(bpy.context.selected_objects):
        o.select_set(False)
    arm.select_set(True)
    for o in arm.children:
        o.select_set(True)
    bpy.context.view_layer.objects.active = arm
    mods = triangulate_for_export(arm.children)
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"ARMATURE", "MESH", "EMPTY"},
        apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        mesh_smooth_type="OFF", use_mesh_modifiers=True, colors_type="LINEAR", use_custom_props=False,
        add_leaf_bones=False, primary_bone_axis="Y", secondary_bone_axis="X", armature_nodetype="NULL",
        bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=True, bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0,
        path_mode="AUTO", embed_textures=False)
    remove_export_mods(mods)
    for t in arm.animation_data.nla_tracks:
        t.mute = True
    for o in list(bpy.context.selected_objects):
        o.select_set(False)
    return path
