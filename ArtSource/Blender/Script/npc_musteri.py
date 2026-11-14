# Atölye önü pazarının müşterileri: siparişi veren sakinler (04_Icerik/10 §1).
# Fırıncı Zehra (D-3), Fenerci Demir (D-2), Muhtar Yusuf (D-1). Duvarcı Halil ve Çiftçi Elif için
# var olan köylü erkek/kadın kullanılır.
# npc.py'nin iskeleti, ortak parçaları, animasyonları (Bekle/Yürü/Çalış/Konuş) ve dışa aktarımı aynen kullanılır;
# burada yalnızca kıyafet, renk ve meslek işareti (silüet) farklıdır. Referans: ArtSource/Referans/atolye_pazar.md §B
#
#   blender --background --factory-startup --python npc_musteri.py
#   → Assets/_Project/Art/NPC/NPC_Musteri<Ad>.fbx

import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else \
    r"C:\Users\Mustafa Akbaş\Desktop\Bilgi-Adasi\ArtSource\Blender\Script"
_npc = os.path.join(_HERE, "npc.py")
exec(compile(open(_npc, encoding="utf-8").read(), _npc, "exec"), globals())

COL = "NPC_Musteri"

def npc_musteri_zehra():
    """Fırıncı: un serpilmiş uzun önlük, sıvanmış kollar, başında bağlı bez başlık; sıcak (kil/altın) elbise."""
    J = skeleton(1.62, 0.13, hip_k=0.47, sh_w=0.19, arm_out=0.05)
    P = []
    skin = (ten(0.35, 0.15), ten(0.8))
    dress = blend2("kil", "altin", 0.25, 0.1, 0.6)
    w = J["waist"].z
    P.append((paint(lathe([(0.28, 0.02), (0.28, 0.08), (0.25, 0.35), (0.21, J["hip"].z), (0.18, w + 0.03)],
                          sides=16, sy=0.78), dress, seed=41), "Kalca"))
    P.append((paint(lathe([(0.18, w - 0.03), (0.20, J["chest"].z - 0.04), (0.195, J["sh_Sol"].z - 0.02),
                           (0.12, J["neck"].z), (0.055, J["neck"].z + 0.04)], sides=14, sy=0.76), dress, seed=42), "Gogus"))
    # boydan önlük (göğüsten dize): fırıncı silüeti; altı un tozu gibi açık
    ap_top, ap_bot = J["chest"].z + 0.02, 0.36
    ang = math.degrees(math.atan2(0.06, ap_top - ap_bot))
    P.append((paint(box((0, -0.195, (ap_top + ap_bot) / 2), (0.32, 0.018, ap_top - ap_bot), rot=(-ang, 0, 0), bevel=0.006),
                    (tone("siva", 1.0), tone("siva", 0.6))), "Omurga"))
    add_belt(P, J, w, 0.185, 0.8, kumas("siva", 0.5, 0.9), h=0.04)
    add_neck(P, J, skin, r=0.045)
    add_head(P, J, skin, kumas("ahsap", 0.05, 0.3), nose="top", mouth=True)
    hr, hc = J["hr"], J["head_c"]
    # bez başlık: tepeyi saran yumuşak kütle + alında katlı bant (saç değil başlık okunsun)
    cap = kumas("siva", 0.6, 1.0)
    P.append((paint(ell(hc + Vector((0, 0.03, hr * 0.35)), (hr * 1.05, hr * 1.05, hr * 0.85), u=12, v=8), cap, seed=43), "Bas"))
    P.append((paint(torus(hr * 0.98, 0.024, c=hc + Vector((0, 0.0, hr * 0.38)), rot=(-15, 0, 0), sides=16, ring=5),
                    blend2("cati", "altin", 0.4, 0.3, 0.7)), "Bas"))
    for s in ("Sol", "Sag"):
        add_arm(P, J, s, dress, skin, sleeve_r=(0.058, 0.052, 0.044), fore_skin=True, hand_k=0.95)
        P.append((paint(torus(0.058, 0.017, c=J["el_" + s] + Vector((0, 0, 0.02)), sides=10, ring=4),
                        kumas("siva", 0.5, 0.9)), "Kol_Ust_" + s))
    add_shoes(P, J, kumas("ahsap", 0.0, 0.3), kind="pabuc", k=0.95)
    return "MusteriZehra", J, P

def npc_musteri_demir():
    """Fenerci: mavi-gri gömlek, kalın deri önlük, kısa sakal, keçe külah; belinde küçük bakır yağdanlık."""
    J = skeleton(1.76, 0.135, hip_k=0.46, sh_w=0.215, arm_out=0.05)
    P = []
    skin = (ten(0.25, 0.3), ten(0.65, 0.2))
    shirt = blend2("dag", "gok", 0.25, 0.1, 0.55)
    w = J["waist"].z
    P.append((paint(lathe([(0.0, 0.42), (0.235, 0.42), (0.24, 0.46), (0.215, J["hip"].z), (0.2, w + 0.03)],
                          sides=14, sy=0.74), shirt, seed=51), "Kalca"))
    P.append((paint(lathe([(0.2, w - 0.03), (0.22, J["chest"].z - 0.05), (0.22, J["sh_Sol"].z - 0.02),
                           (0.14, J["neck"].z), (0.065, J["neck"].z + 0.04)], sides=14, sy=0.72), shirt, seed=52), "Gogus"))
    # deri önlük: göğüsten diz altına, koyu ahşap tonu (yağ lekeli gibi alt koyu)
    ap_top, ap_bot = J["chest"].z + 0.06, 0.40
    ang = math.degrees(math.atan2(0.06, ap_top - ap_bot))
    P.append((paint(box((0, -0.18, (ap_top + ap_bot) / 2), (0.34, 0.022, ap_top - ap_bot), rot=(-ang, 0, 0), bevel=0.008),
                    (tone("ahsap", 0.0), tone("ahsap", 0.45))), "Omurga"))
    add_belt(P, J, w, 0.205, 0.75, kumas("ahsap", 0.0, 0.25), h=0.05)
    # yağdanlık (sağ kalça): fenerlere yağ koyar — meslek işareti
    flask(P, Vector((-0.2, -0.04, w - 0.16)), kumas("bakir", 0.3, 0.8), kumas("ahsap", 0.1, 0.4), s=1.1)
    add_neck(P, J, skin)
    add_head(P, J, skin, kumas("ahsap", 0.0, 0.25), nose="uzun", mouth=False)
    hr, hc = J["hr"], J["head_c"]
    beard = kumas("ahsap", 0.0, 0.35)
    P.append((paint(ell(hc + Vector((0, -hr * 0.5, -hr * 0.62)), (hr * 0.7, hr * 0.45, hr * 0.4)), beard), "Bas"))
    # keçe külah: yuvarlak, hafif sivri, kenarı kıvrık
    felt = blend2("cati", "ahsap", 0.55, 0.15, 0.55)
    bz = hc + Vector((0, 0.01, hr * 0.45))
    P.append((paint(lathe([(0.0, 0.0), (hr * 1.08, 0.0), (hr * 1.04, hr * 0.5), (hr * 0.6, hr * 0.95), (0.0, hr * 1.05)],
                          sides=14, c=bz), felt, seed=53), "Bas"))
    P.append((paint(torus(hr * 1.06, 0.02, c=bz + Vector((0, 0, 0.01)), sides=14, ring=4), felt), "Bas"))
    for s in ("Sol", "Sag"):
        add_arm(P, J, s, shirt, skin, sleeve_r=(0.062, 0.056, 0.05), cuff=kumas("ahsap", 0.1, 0.4))
    add_legs(P, J, kumas("dag", 0.05, 0.4))
    add_shoes(P, J, kumas("ahsap", 0.0, 0.25), kind="cizme")
    return "MusteriDemir", J, P

def npc_musteri_yusuf():
    """Muhtar: yaşlı, hafif kambur; yere yakın koyu yeşil cübbe-palto, açık renk sakal, yuvarlak keçe başlık."""
    J = skeleton(1.70, 0.135, hip_k=0.46, sh_w=0.2, arm_out=0.055, stoop=0.035)
    P = []
    skin = (ten(0.3, 0.2), ten(0.7, 0.1))
    coat = blend2("yaprak", "dag", 0.35, 0.0, 0.4)
    w = J["waist"].z
    P.append((paint(lathe([(0.28, 0.06), (0.28, 0.10), (0.25, 0.4), (0.215, J["hip"].z), (0.2, w + 0.03)],
                          sides=16, sy=0.78), coat, seed=61), "Kalca"))
    P.append((paint(lathe([(0.2, w - 0.03), (0.225, J["chest"].z - 0.05), (0.22, J["sh_Sol"].z - 0.02),
                           (0.14, J["neck"].z), (0.07, J["neck"].z + 0.04)], sides=14, sy=0.74), coat, seed=62), "Gogus"))
    # palto önü: açık renk iç yelek şeridi (silüette dikey çizgi)
    P.append((paint(box((0, -0.19, (w + J["chest"].z) / 2 + 0.02), (0.09, 0.02, J["chest"].z - w + 0.14), bevel=0.006),
                    kumas("altin", 0.2, 0.6)), "Omurga"))
    add_belt(P, J, w, 0.21, 0.78, blend2("cati", "kil", 0.4, 0.15, 0.5), h=0.07)
    add_neck(P, J, skin)
    add_head(P, J, skin, kumas("siva", 0.6, 0.9), nose="yumru", mouth=False, brow_k=1.2)
    hr, hc = J["hr"], J["head_c"]
    beard = kumas("siva", 0.55, 1.0)
    P.append((paint(ell(hc + Vector((0, -hr * 0.5, -hr * 0.78)), (hr * 0.78, hr * 0.5, hr * 0.62)), beard), "Bas"))
    P.append((paint(ell(hc + Vector((0, -hr * 0.88, -hr * 0.28)), (hr * 0.45, hr * 0.12, hr * 0.1)), beard), "Bas"))
    # yuvarlak keçe başlık
    felt = kumas("ahsap", 0.15, 0.5)
    bz = hc + Vector((0, 0.01, hr * 0.5))
    P.append((paint(lathe([(0.0, 0.0), (hr * 1.02, 0.0), (hr * 1.0, hr * 0.42), (hr * 0.7, hr * 0.62), (0.0, hr * 0.66)],
                          sides=14, c=bz), felt, seed=63), "Bas"))
    for s in ("Sol", "Sag"):
        add_arm(P, J, s, coat, skin, sleeve_r=(0.066, 0.06, 0.056), flare=0.012, cuff=kumas("altin", 0.2, 0.5))
    add_shoes(P, J, kumas("ahsap", 0.0, 0.3), kind="pabuc")
    return "MusteriYusuf", J, P

CHARACTERS = (npc_musteri_zehra, npc_musteri_demir, npc_musteri_yusuf)

if __name__ == "__main__":
    if bpy.app.background:
        for o in list(bpy.data.objects):
            bpy.data.objects.remove(o, do_unlink=True)
    build_all_npc()
    if "--no-export" not in sys.argv:
        print("export:", export_all_npc())
