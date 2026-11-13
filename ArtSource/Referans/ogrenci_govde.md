# Referans — Öğrencinin gövdesi (baş ve kollar hariç)

Kural: `09-tema-ve-model-kurallari.md` §6. Araştırma 13.11.2026. Görseller repoya konmaz.
Script: `ArtSource/Blender/Script/ogrenci_govde.py` · FBX: `Assets/_Project/Art/Animasyon/Ogrenci_Govde_Rig.fbx`
Unity: `PlayerBodyView` (Player/Govde), kurulum **Kurulum → 15**.

## 1. Neden

Bisiklette yana bakınca kol rig'inin omuz ucu "kesik" görünüyordu ve aşağı bakınca beden yoktu. Oyuncu artık
aşağı bakınca göğsünü, bacaklarını ve ayakkabılarını görür; bisiklette ayaklar pedalları gerçekten çevirir.
Kollar ayrı rig olarak kalır (kameraya bağlı, klipleri değişmedi); gövdede kol yok, omuz başları kol ucunu örter.

## 2. Kaynaklar ve alınanlar

| # | Kaynak | Ne alındı |
|---|---|---|
| 1 | [Unity Discussions — full body first-person camera placement](https://discussions.unity.com/t/proper-cinemachine-camera-placement-on-full-body-first-person-model/1590365) | Kamera gözde, gövde ayrı mesh; aşağı bakınca öncelik kalça > göğüs. |
| 2 | [Roblox DevForum — body visible in first person](https://devforum.roblox.com/t/making-body-visible-in-first-person-mode/3395170) | Baş gizli, gövde görünür; dik aşağı bakınca gövde görüşü kapatmasın → aşağı bakınca gövde 0,16 m geri kayar. |
| 3 | [PC Gamer — Rust legs](https://www.pcgamer.com/witness-the-body-horror-that-allows-rust-players-to-look-down-and-see-their-legs) | Kötü örnek: gövdeyi esnetmek yerine kaydırmak. |
| 4 | [Blender Artists — animated cyclist](https://blenderartists.org/t/animated-cyclist/673408) · [Roblox — feet on pedals](https://devforum.roblox.com/t/how-can-get-the-players-feet-to-be-on-top-of-the-bikes-pedal/1830217) | Kalça seleye, ayak IK hedefi pedal; diz ipucu (pole) önde-yukarıda, yoksa diz döner. |
| 5 | [Roblox — animate cycle pedaling](https://devforum.roblox.com/t/how-to-animate-cycle-pedaling/244882) | Pedallar tekerle döner, bacaklar pedalı izler (klip değil IK). |
| 6 | `ogrenci_kol.md` #10, #12 | Aynı okul sweatshirt'ü (`dag`), logosuz; ten `ten` satırı. |

## 3. Tasarım kararları

| Karar | Gerekçe |
|---|---|
| Sweatshirt + kanguru cep + kapüşon ve ipleri | Modern okul kıyafeti (hikâye: uyuyakalan öğrenci); aşağı bakınca göğüste görünen ayrıntı. |
| Koyu gri-mavi pantolon (`kaya`+`dag` gölge), turuncu spor ayakkabı (`cati`) + açık taban | Ortaçağ dünyasında tek modern siluet; ayakkabı aşağı bakınca hemen okunur. Siyah yok. |
| Bacak NPC'lerden uzun (kalça 0,86 m) | Velosipedin en uzak pedal noktasına (~0,86 m) bacak tam uzanınca yetişir. |
| Kemikler: Kalca, Omurga, Gogus, Bacak_Ust/Alt, Ayak (sol/sağ); yumuşak ağırlık (kalça, diz, bel) | Oturma ve çömelmede eklem kırılmasın. |
| Klipler: Bekle, Yuru, Egil, Egil_Yuru, Havada, Bisiklet (hepsi 1 sn döngü) | Yürüme 1 sn = 2 adım; Unity tempoyu adım döngüsüne bağlar (adım sesiyle aynı ritim). |
| Gölge başı (ShadowsOnly) | Oyuncunun gölgesi başsız görünmesin. |
| Bisiklet selesi 0,21 m öne, omurga çukuru 0,62 → 0,48 | Eski yerde (merkez y 0,13) en uzak pedal 0,95 m'ydi, bacak yetişmiyordu (karar 13.11, Mustafa). |
