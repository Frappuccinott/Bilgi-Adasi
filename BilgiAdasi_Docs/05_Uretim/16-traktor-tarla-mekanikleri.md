# 16 — Simya Traktörü ve Tarla Mekanikleri (Aşama 2: oyuncu sürer)

> Talep (Mustafa, 14.11): Oyuncu traktörü kendisi sürsün. Sürme, ekme, sulama, gübre atma ve hasat görsel olarak yapılsın. Mekanikler adım adım planlansın; gerekirse animasyonlar Blender (MCP) ya da Unity'de yapılsın.
> Bağlı plan: `04_Icerik/15-bereket-tarlasi-mat-biyo.md` (5 görev, ders notları). Bu dosya o görevlerin **traktörlü** oynanışını ve bütün mekanikleri tanımlar.
> Öncelik: **P2 / Sürüm 2.** Kod dondurma (14.11, 08.00) öncesi Harita sahnesine ve var olan koda dokunulmaz. Yeni kod yalnızca yeni dosyalarda (`Scripts/Player/Tractor/`, `Scripts/Field/`), sahne kurulumu **Kurulum menüsüyle** ve teslimden sonra.

---

## 0. Özet

| # | Mekanik | Ne | Nerede yapılır | Süre |
|---|---|---|---|---|
| M1 | **Traktör sürüşü** | Bin (F / X), gaz-fren-geri, direksiyon, birinci şahıs koltuk kamerası | Unity kod | 3 sa |
| M2 | **Sürücü gövdesi** | Eller direksiyonda, ayaklar basamakta, gövde koltukta | Blender (tutma noktaları) + Unity (var olan IK) | 1 sa |
| M3 | **Alet takma ve indirme** | Geri yanaş → tak; Space / A ile indir-kaldır; ahır önünde bırak | Unity kod | 2 sa |
| M4 | **Parsel sistemi** | 24 parsel, alet geçtikçe dolan kapsama, 6 durum, yumuşak geçiş | Unity kod | 2,5 sa |
| M5 | **Görsel efektler** | Toprak, tohum, su, gübre, saman, duman, ıslak toprak, büyüme | Unity (ParticleSystem, MaterialPropertyBlock) | 3 sa |
| M6 | **Hareketli parçalar** | Tekerler, ön aks, direksiyon, volan, ekici diskleri, serpici diski, biçer çarkı | Blender (parçaları ayır) + Unity (hıza bağlı dönüş) | 2 sa |
| M7 | **Canlı animasyonları** | Kuş kanat çırpma, şahin, solucan kıvrılması, ekin rüzgârda | Blender (klip) + Unity | 2 sa |
| M8 | **HUD** | Bakır hız ibresi, alet adı ve durumu (kalkık/inik), iş genişliği | Figma → Unity view | 1,5 sa |
| M9 | **Ses** | Motor (devire göre perde), alet sesleri, su, gübre | `ses_sentez.py` + AudioDirector | 1 sa |
| M10 | **Kayıt** | Traktör konumu, takılı alet, aletlerin yeri, parsel durumları | Unity kod | 1 sa |
| M11 | **Görev bağlantısı** | 15'teki 5 görevin traktörlü adımları ve öğretici ayarlar | Unity kod + veri | 2 sa |
| | **Toplam** | | | **≈ 21 sa** (2 kişi ≈ 2 gün) |

**Animasyon kararı:** Makinelerin bütün hareketleri (teker, direksiyon, disk, çark, alet indirme) **hıza ve girdiye bağlı** olduğu için önceden pişirilmiş klip değil, Unity'de kodla döner (bisikletteki `BicycleRig` gibi). Blender'ın işi: hareketli parçaları **ayrı obje ve doğru pivotla** dışa vermek, efekt çıkış noktalarını (boş obje) koymak. Blender'da **klip** yalnızca canlılar için yapılır (kuş, şahin, solucan; M7).

---

## 1. M1 — Traktör sürüşü

**Model:** `Art/Traktor/Traktor.fbx`. Sahnede `Araclar/Traktor` (yön −X, arka teker x −43,7, ön teker x −46,15, bağlantı pimi arkada).

**Bileşenler** (`BilgiAdasi.Player`, `Scripts/Player/Tractor/`):
- `Tractor : WorldInteractable` — bin/in, sürüş benzetimi (kinematik, `CharacterController`, devrilme yok). `Bicycle` ile aynı desen ama ayrı sınıf (çalışan bisiklet koduna dokunulmaz).
- `TractorSettings` (SO) — bütün sayılar ve oyuncuya görünen metinler.
- `TractorRig` — görünüm: teker dönüşü (hız / yarıçap), ön aks sapması, direksiyon simidi (sapma × 4), volan (motor devri), baca dumanı yoğunluğu.

**Sürüş modeli (bisiklet modeli, iki aks):** dönüş hızı = hız ÷ dingil mesafesi × tan(sapma). Dingil mesafesi modelden 2,45 m.

| Parametre | Değer | Not |
|---|---|---|
| En yüksek hız (alet kalkık) | 4,0 m/s | 04 §5.2 |
| İş hızı (alet inik) | 1,6 m/s | A2 sorusunun sayısı |
| Geri | 1,5 m/s | |
| Hızlanma / yavaşlama | 1,8 / 4,0 m/s² | Ağır makine hissi; bırakınca 1 sn'de durur |
| En büyük sapma | 32° (hızlıyken 18°'ye iner) | Tarla başında dönüş ≈ 4 m yarıçap |
| Sapma hızı | 70°/sn, bırakınca 90°/sn ile ortaya döner | |
| Eğim | Yokuşta ×0,7'ye kadar | |
| Kaldırım / basamak | 0,25 m | Tarla kapısı ve patika |
| Çarpma | Engelde durur, sarsıntı ≤ 0,03 m (rahatlık §4.4) | |

**Kamera:** `Traktor_Koltuk_Kamera` (göz 1,56 m'ye göre ayarlanır). Birinci şahıs, roll yok. Serbest bakış gövdeye göre ±120°; geri giderken bakış kendiliğinden geriye dönmez (oyuncu döner). Motor titreşimi kameraya **verilmez** (yalnızca direksiyon ve gövde modelinde çok hafif).

**Girdi — yeni aksiyon yok** (Player map, bisikletle aynı yol):

| İş | Klavye / Fare | Gamepad | Aksiyon |
|---|---|---|---|
| Bin / in | F | X (West) | `Use` |
| Gaz / fren-geri | W / S | Sol çubuk ↑ / ↓ | `Move.y` |
| Direksiyon | A / D | Sol çubuk ← → | `Move.x` |
| Bakış | Fare | Sağ çubuk | `Look` |
| Aleti indir / kaldır · tak · bırak (bağlama göre) | Space | A (South) | `Jump` |
| Hızlı (yalnızca alet kalkıkken) | Sol Shift | L3 | `Sprint` |

`Jump` sürüşte zıplamaz; bağlama göre tek iş yapar (§3). Ekranda tuş ipucu `ControlGlyphs.For(PromptAction.Jump)` ile cihaza göre değişir. CLAUDE.md §3 tablosuna "Traktör" satırı eklenir.

**İnme:** dururken F → oyuncu sol basamaktan iner (bisikletteki boş yer arama aynen).

## 2. M2 — Sürücü gövdesi

- Kollar: var olan `RiderArmsIK` (`IRiderArmsView`) → `Traktor_Direksiyon` altındaki `Tutma_Sol/Sag`. Simit dönünce eller onunla döner.
- Bacaklar: var olan `TwoBoneIK` (`IRiderBodyView`) → `Traktor_Ayak_Sol/Sag` (basamak), kalça `Traktor_Sele_Oturma`.
- Blender'da eklenecek boş objeler: `Tutma_Sol`, `Tutma_Sag` (direksiyonun çocuğu, simit kenarında saat 10 ve 2 konumu), `Traktor_Sele_Oturma`, `Traktor_Ayak_Sol`, `Traktor_Ayak_Sag`.

## 3. M3 — Alet takma, indirme, bırakma

**Bileşenler:** `TractorHitch` (traktörde, `Traktor_Baglanti` noktası), `FarmImplement` (her alette), `ImplementData` (SO: tür, iş genişliği, indirme açısı, gerekli parsel durumu → sonraki durum, efekt ve ses).

| Alet | İş genişliği | Parsel geçişi | Görev |
|---|---|---|---|
| `Alet_Pulluk` | 1,6 m | Nadas → Sürülmüş | bereket_1 |
| `Alet_Ekici` | 1,8 m (6 boru × 0,3 m) | Sürülmüş → Ekili | bereket_2 |
| `Alet_Su_Tanki` | 2,0 m (fıskiye borusu) | Ekili → Filiz (sulama) | bereket_3 |
| `Alet_Serpici` | 3,0 m (disk savurur) | Filiz: gübre işareti (Olgun için koşul) | bereket_4 |
| `Alet_Bicer` | 2,6 m (bıçak kirişi) | Olgun → Hasat (+ çuval) | bereket_5 |
| `Alet_Kizak` | — | — (orman) | — |

**Akış (tek tuş, bağlama göre):**
1. **Tak:** Alet takılı değil, traktör geri geri gelip pim alete ≤ 0,6 m ve yön farkı ≤ 25° → ipucu "Space Aleti tak" → 0,4 sn'de alet pime oturur (yumuşak), kanca sesi.
2. **İndir / kaldır:** Alet takılı → Space ile alet pim ekseninde indirme açısına döner (0,5 sn). İnikken iş hızı sınırı ve iş yapılır; kalkıkken tam hız.
3. **Bırak:** Alet kalkık, traktör duruyor ve ahır önündeki **alet yeri** bölgesinde → ipucu "Space Aleti bırak" → alet yere konur, traktör serbest.
- Yanlış alet ya da yanlış parsel durumu: alet iner ama iş yapmaz; Elif ya da usta tek cümle söyler ("Toprak sürülmeden ekici boşuna döner.").

## 4. M4 — Parsel sistemi

**Model:** `Bereket_Tarlasi` altında 24 parsel × 6 durum (`Tarla_P<satır><sütun>_<Durum>`, 144 obje; başlangıçta yalnızca `_Nadas` açık).

**Bileşenler** (`BilgiAdasi.Field`, `Scripts/Field/`): `FieldPlot` (bir parsel: 6 durum objesi, kapsama ızgarası, su ve gübre bayrakları), `FieldController` (24 parsel, kayıt, olaylar), `GameEvents.PlotChanged`.

- **Kapsama:** her parsel 4 m × 4 m, içinde 8 × 8 hücre (0,5 m). İnik alet her karede iş genişliği boyunca 5 nokta örnekler, değdiği hücreleri işaretler. Hücrelerin **%75'i** dolunca parsel bir sonraki duruma geçer (oyuncu kenarları kaçırsa da tıkanmaz).
- **Geçiş görseli:** eski durum 0,6 sn'de 0,15 m aşağı iner ve solar, yenisi aşağıdan yükselir; kısa toz ya da toprak patlaması (M5).
- **Ara görüntü (kısmi iş):** alet geçtiği yerde **iz** bırakır (M5 izi). Böylece parsel geçmeden önce de oyuncu nereyi sürdüğünü görür.
- **Kontrol sırası (deney):** bereket_2'de oyuncu bir sırayı (6 parsel) "gübresiz" seçer, kazık dikilir. Serpici o sıradan geçerse gübre **işlemez** ve Hayat Ustası uyarır. Gübresiz parseller Olgun'da daha kısa ve açık renkli görünür (ölçek ×0,8, renk MaterialPropertyBlock).
- **Gölgelik (deney):** bereket_3'te iki parsel hasır gölgelik altında; Filiz durumunda boyları ×0,6.

## 5. M5 — Görsel efektler (Unity, URP ParticleSystem)

| Efekt | Çıkış noktası | Görünüş | Tetik |
|---|---|---|---|
| **Toprak keseği** | `Pulluk_Toprak_1…3` (her bıçak) | Kahverengi küçük mesh parçacık, kısa yay çizip düşer | Pulluk inik ve hız > 0,2 |
| **Sürme izi** | Pulluk arkası | Yere yatık şerit (TrailRenderer, toprak dokusu), parsel geçince söner | Pulluk inik |
| **Tohum** | `Ekici_Cikis_1…6` (borular) | Küçük açık sarı taneler, borudan düşer | Ekici inik |
| **Su** | `Fiskiye_1…5` | Gerilmiş billboard damlacık, açık mavi, yerde küçük sıçrama | Su tankı inik |
| **Islak toprak** | Parsel | Toprak rengi %30 koyulaşır, 60 sn'de kurur (MaterialPropertyBlock) | Su değince |
| **Gübre** | `Serpici_Cikis` (disk) | Beyazımsı granül, diskten daireye savrulur | Serpici inik (disk hızlı döner) |
| **Saman ve kavuz** | `Bicer_Cikis` | Altın sarısı ince parçacık + birkaç uçuşan başak | Biçer inik |
| **Çuval** | Parsel ortası | Hasat bitince çuval belirir (envanter maddesi, Interact ile alınır) | Hasat |
| **Baca dumanı** | `Traktor_Duman_Noktasi` | Yumuşak gri puf; rölantide seyrek, gazda sık | Motor |
| **Teker tozu** | Arka tekerler | Kuru toprakta açık kahverengi toz | Hız > 1, ıslak değil |
| **Büyüme** | Parsel durumu | Ekili → Filiz → Olgun geçişinde bitkiler Y'de 0'dan büyür (ease-out, 1,2 sn) | Durum değişimi |

Renkler 09-tema paletinden. Parçacık sayıları düşük tutulur (tek efekt ≤ 200 parçacık; hedef 60 FPS).

## 6. M6 — Hareketli parçalar (Blender'da ayrılacak)

| Alet / araç | Ayrılacak parça (pivot) | Dönüş (Unity) | Boş obje (efekt / bağlantı) |
|---|---|---|---|
| Traktör | var: `Traktor_Arka_Tekerler`, `_On_Aks`, `_On_Tekerler`, `_Volan`, `_Direksiyon` | teker: hız ÷ R; aks: sapma; simit: sapma × 4; volan: devir | ekle: `Tutma_Sol/Sag`, `Traktor_Sele_Oturma`, `Traktor_Ayak_Sol/Sag` |
| Pulluk | `Pulluk_Teker` (aks) | yer hızı | `Pulluk_Toprak_1…3` |
| Ekici | `Ekici_Diskler` (X ekseni, z −0,72), `Ekici_Tekerler` (z −0,5) | yer hızı | `Ekici_Cikis_1…6` |
| Su tankı | `SuTanki_Tekerler` | yer hızı | `Fiskiye_1…5` |
| Serpici | `Serpici_Disk` (Z ekseni, z −0,4), `Serpici_Tekerler` | disk inikken 8 tur/sn | `Serpici_Cikis` |
| Biçer | `Bicer_Cark` (X ekseni, mil z −0,05), `Bicer_Teker` | çark yer hızıyla ×1,5 | `Bicer_Cikis` |

Her aletin kökü pim olarak kalır (Unity'deki `Alet_*` adları bozulmaz; parçalar onun çocuğu olur).

## 7. M7 — Canlı animasyonları (Blender klipleri)

| Canlı | Klip | Görev |
|---|---|---|
| Kuş (serçe boyu, 3 adet) | `Kus_Ucus` (kanat döngüsü 0,25 sn), `Kus_Kon` (konma), `Kus_Gagala` | bereket_4 |
| Şahin | `Sahin_Dur` (direkte, baş çevirme), `Sahin_Suzul` | bereket_4 |
| Solucan | `Solucan_Kivril` (2 kemikli zincir, döngü) | bereket_1 örnek kare |
| Ekin | Rüzgâr: klip değil, Unity'de parsel başına çok hafif sallanma (tepe kemiği yok; durum objesi X'te ±2°) | Olgun |

Modeller ve klipler `npc.py` / `anim_ortak.py` diliyle elle yazılan Blender scriptinde (`ArtSource/Blender/Script/tarla_canli.py`); hazır asset ya da üretici yok (09-tema kuralı).

## 8. M8 — HUD

- Sol altta mini haritanın üstünde küçük **bakır ibre** (0–4 m/s), altında alet adı ve durumu ("Pulluk · inik"), iş genişliği şeridi.
- Tuş ipuçları (`ControlGlyphs`): "Space Aleti indir / kaldır / tak / bırak", "F İn".
- Arayüz: `ITractorHudView` (`RegisteredView`); denetleyici Canvas bilmez.
- Figma: `HUD/Traktör` karesi.

## 9. M9 — Ses (sentez, 0'dan)

`ses_sentez.py`'e: motor döngüsü (pistonlu buhar "puf-puf", perde ve tempo devire bağlı), kanca takma "tak", pulluk toprak hışırtısı, ekici tıkırtısı, su şırıltısı, gübre serpme tıslaması, biçer çark vınlaması. `SoundId` + `SoundLibrary`'ye eklenir.

## 10. M10 — Kayıt

`SaveData`: traktör konumu (`SetPose("tractor")`), takılı aletin kimliği ve inik/kalkık durumu, her aletin konumu, 24 parselin durumu + su/gübre/kontrol bayrakları. Kayıt yalnızca otomatik kayıtla (görev bitince) yazılır (CLAUDE.md §6); `GameEvents.BeforeSave` ile.

## 11. M11 — Görevlere bağlantı (15 ile)

| Görev | Traktörle ne yapılır | Öğretici ayar (oyuncu karar verir) |
|---|---|---|
| bereket_1 | Pulluğu tak, tarlayı sür | Sürmeden önce 3 örnek karede say (yürüyerek). Hesap Ustası: "İz 1,6 m, tarla 16 m: kaç tur?" → 10. Sürme izleri bu tur sayısını görünür kılar |
| bereket_2 | Ekiciyi tak, ek | Ekici çarkı ayarı: "150 tohum, 24 parsel → parsel başına kaç?" (≈ 6). Kontrol sırasını seç (kazık) |
| bereket_3 | Su tankını kuyudan doldur, sula | İki deney parseline **eşit** su (aynı hızla bir kez geçmek). Gölgelik altı ile açık parsel aynı su alır: değişken sabit tutma |
| bereket_4 | Serpiciyle gübre at, kontrol sırasını atla | Kontrol sırasından geçmemek oyuncunun işi. Ardından kuş yuvaları (yürüyerek) |
| bereket_5 | Biçerle biç, çuvalları terazide tart | Gübreli ve gübresiz parsellerin çuvalları ayrı; ortalamalar ve iki yönlü tablo |

Her görevin başında ders notu (15 §3), sonunda saha sorusu ve defter aynen kalır.

---

## 12. Adım adım uygulama sırası

| Adım | İş | Kim | Çıktı | Kabul |
|---|---|---|---|---|
| **1** | Blender: traktöre tutma/oturma/ayak noktaları; aletlerin hareketli parçalarını ayır, efekt noktalarını koy; FBX'leri yeniden ver | Mustafa + Claude | `harita_tarla.py`, `Traktor.fbx`, `Traktor_Aletler.fbx` | Unity'de `Alet_*` adları aynı, yeni çocuklar doğru pivotta |
| **2** | Unity: `TractorSettings`, `Tractor`, `TractorRig`; Kurulum → 22 (traktöre bileşenleri ve collider'ı ekler) | Claude (Mehmet onayı) | Sürülebilir traktör | F ile bin, sür, dön, in; klavye ve gamepad |
| **3** | Sürücü gövdesi (eller, ayaklar) | Claude | | Simit dönünce eller onunla döner |
| **4** | Alet takma / indirme / bırakma | Claude | `TractorHitch`, `FarmImplement`, `ImplementData` | 5 aletin hepsi takılıp bırakılır |
| **5** | Parsel sistemi + kapsama + geçiş | Claude | `FieldPlot`, `FieldController` | Pulluk geçince parsel Sürülmüş olur |
| **6** | Efektler (toprak, tohum, su, gübre, saman, duman, ıslaklık, büyüme) | Claude + Mustafa (renk/doku) | Prefab'lar `Art/VFX/Tarla/` | Her alet görsel olarak çalışır, 60 FPS |
| **7** | Canlılar ve klipler (kuş, şahin, solucan) | Mustafa + Claude (Blender) | `tarla_canli.py`, FBX | |
| **8** | HUD + ses | Mustafa (Figma) + Claude | `ITractorHudView`, sesler | |
| **9** | Kayıt | Claude | | Yeniden yüklemede parseller ve traktör yerinde |
| **10** | Görev bağlantısı (15'teki akış) | Mehmet + Claude | `FieldTaskRunner` | bereket_1 baştan sona |

### 12.1 Durum (14.11, 04.45)

| Adım | Durum |
|---|---|
| 1 Blender | **Yapıldı.** `harita_tarla.py`: `Traktor_Tutma_Sol/Sag`, `Traktor_Sele_Oturma`, `Traktor_Ayak_Sol/Sag`; aletlerde dönen parçalar (`Pulluk_Teker`, `Ekici_Diskler/Tekerler`, `SuTanki_Tekerler`, `Serpici_Disk/Tekerler`, `Bicer_Cark/Teker`) ve efekt noktaları (`Pulluk_Toprak_1–3`, `Ekici_Cikis_1–6`, `Fiskiye_1–5`, `Serpici_Cikis`, `Bicer_Cikis`); su tankına 5 fıskiye memesi. `export_traktor()` yalnızca iki traktör FBX'ini verir. Tutma noktaları FBX'te köke bağlı (Blender'ın iç içe boş obje dönüşüm hatası); Kurulum → 23 onları direksiyona taşır. Sahnedeki eski kopyalar FBX'e bağlı değil, değişmedi. |
| 2 Sürüş | **Kod yazıldı, derleniyor.** `Player/Tractor/Tractor.cs`, `TractorRig.cs`, `TractorSettings.cs`, `Editor/TractorSetup.cs` (Kurulum → 23; eski duran traktörü `Traktor_Eski` olarak kapatır). Play'de binme ve koltuk kamerası çalıştı. Gaz/direksiyon klavye ve gamepad ile elle denenmedi. Eller direksiyona uzanıyor (`IRiderArmsView`); **gövde/bacaklar henüz koltukta değil** (adım 3: `IRiderBodyView` şimdilik `BicycleRig` istiyor, genelleştirilmeli). |

**Düzeltme (14.11, 04.50, Play geri bildirimi):**
- *Önü görünmüyordu:* göz 1,91 m, ocak kutusunun tepesi 1,90 m, kazan 1,88 m idi. Sürücü platformu, koltuk, kollar ve direksiyon 0,4 m yükseltildi (platform şasi kirişine ayaklarla oturur, sol yanda iki basamak), ocak kutusu 1,65 m'ye alçaltıldı, tente 0,3 m yükseltildi. Göz ≈ 2,40 m: yol ve tarla kazanın üstünden görünüyor.
- *Eller direksiyona oturmuyordu:* simit sürücüye 0,14 m yaklaştırıldı (mil yeniden çizildi), tutma noktalarına el yönü verildi (bisikletteki kural: parmaklar öne-aşağı simidi sarar, başparmak simit boyunca içe; `TractorSetup.OrientGrip`).

**Düzeltme 2 (14.11, 05.00):**
- *Simit yatıktı, eller yana açık kalıyordu:* simit araba direksiyonu gibi sürücüye dönük (yataydan 65°, `STEER_TILT`), mil ocak kutusunun üstünden simit eksenine gelir. Tutma noktaları saat 9 ve 3'te (x ekseni boyunca); `TractorRig.steeringWheelAxis` = (0, 0.423, −0.906).
- *Eller hiç tutmuyordu:* `ViewLocator`'da Destroy edilmiş eski bir `RiderArmsIK` kayıtlı kalmıştı (yok edilen nesne null dönmüyor). Traktör artık binerken kayıt ölü ise oyuncunun kendi kollarını kullanır (`Tractor.FindArmsView`). **Aynı sorun bisikleti de etkileyebilir**; kalıcı çözüm `ViewLocator.Get`'in yok edilmiş Unity nesnesini null saymasıdır (ortak kod, Mehmet'e).
- *Simit asıl hatası:* `traktor_direksiyon_bm` simidi zaten yatay kuruyordu, `build_traktor` bir kez daha Y'de 90° döndürüyordu; simit dik ve ileri-geri doğrultuda (YZ) duruyordu. Fazla dönüş kaldırıldı; topuz simidin tepesine alındı (sol ele çakışıyordu). Play'de eller simidi 9 ve 3'ten kavrıyor (tutma noktasına uzaklık 0).
- *F ile inilmiyordu:* iniş yeri yoklaması traktörün kendi gövde kutusunu engel sayıyordu; artık kendi collider'ları ve oyuncu yok sayılır, yan iniş mesafesi 1,9 m.

**Sahne kuralı:** Adım 2'den itibaren sahne değişikliği yalnızca Kurulum menüsüyle ve **teslimden sonra** yapılır; kod dondurma öncesi Harita sahnesi değişmez.

## 13. Riskler

- **Bisiklet koduyla çakışma:** `Tractor` ayrı sınıf; bisiklet dosyalarına dokunulmaz. Ortak parça (`RiderArmsIK`, `TwoBoneIK`) yalnızca kullanılır.
- **Kapsama hissi:** %75 eşiği ve iz görseli, "parsel neden dönmedi?" sorusunu önler. Play'de ayarlanır (`ImplementData`).
- **Performans:** 144 parsel objesi zaten sahnede; kapsama ızgarası parsel başına 64 bool, ayırma yok.
- **Lisans:** Hepsi 0'dan (script + sentez). LICENSES.md beyan tablosuna satır eklenir.
