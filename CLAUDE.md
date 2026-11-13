# CLAUDE.md — Bilgi Adası

Bu dosya, bu Unity projesinde çalışan Claude için kalıcı bağlamdır. Her oturumun başında okunur. Ayrıntılı tasarım `BilgiAdasi_Docs/` klasöründedir; **bu dosya ile dokümanlar çelişirse bu dosya geçerlidir** (dokümanlar sonra güncellenir).

---

## 1. Proje özeti

- **Oyun:** Bilgi Adası — 10. sınıf (15–16 yaş) öğrencileri için eğitsel 3D oyun prototipi.
- **Hikâye:** Kimya dersinde uyuyakalan öğrenci, ortaçağda her şehri bir ders olan bir adada uyanır. Kimya şehrinde Câbir Usta'nın çırağı olur. Şehir = ders, ilçe = tema, mahalle = konu, her mahalle = 5 görev.
- **İki görev tipi:** *Sipariş* (kazan + iki terazi, uygulama konuları) ve *Masa* (kâğıt üzerinde sözel sorular). Kurallar: `BilgiAdasi_Docs/02_Mekanik/`.
- **Prototip kapsamı:** Kimya şehri, Etkileşim ilçesi, iki oynanabilir mahalle: *Denkleştirme* (5 sipariş) ve *Tepkime Türleri* (5 masa görevi).
- **Yarışma teslimi:** Cumartesi 14 Kasım 2026, 11.00 — çalışan build + sunum. **Kod dondurma: 08.00.**

### Roller
| Kişi | Kapsam |
|---|---|
| **Mehmet** (Claude ile çalışan) | Mekanik ve kod: oyuncu, istasyonlar, denetleyiciler, veri (SO) yapısı, kayıt, build |
| **Mustafa Akbaş** | Haritalar (şehir/mahalle sahnesi, greybox dahil) ve UI (HUD, sayaçlar, kâğıt, kartlar, harita ekranı, menüler) |

- Claude **level ve UI görseli yapmaz.** Test için yalnızca en basit yer tutucuları kurar (düz zemin + birkaç küp, ham TMP metni ve düğmeler) ve bunları `_Placeholder` adlı klasör/objede tutar; Mustafa'nınkiyle değiştirilir.
- UI kodunda **mantık ile görünüm ayrıdır:** denetleyiciler veriyi event ya da arayüzle yayınlar (`IScaleView`, `IOrderCardView`, `IPaperView` …). Mustafa'nın prefab'ı bu arayüzü uygulayan küçük bir view script'iyle bağlanır. Denetleyici hiçbir zaman belirli bir Canvas ya da objeye bağımlı olmaz.
- Sahne bağlantı noktaları (Mustafa'nın sahnesinde olması gerekenler): `BilgiAdasi_Docs/03_Teknik/08-mustafa-entegrasyon.md`.

### Yarışma kuralları (her işte geçerli)
- **0'dan üret:** Kod yarışma süresince yazılır. Eski projelerden kod, paket ya da model kopyalanmaz (kendi paketlerimiz dahil).
- **Lisans:** Hazır her varlık `BilgiAdasi_Docs/05_Uretim/LICENSES.md` tablosuna yazılmadan projeye girmez.
- **Yapay zekâ serbest ama beyan edilir:** Claude'un yazdığı önemli sistemler LICENSES.md'deki beyan tablosuna eklenir.
- **Uygun içerik:** Şiddet, ayrımcılık, gerçek felaket çağrışımı, kumar, gerçek para yok.

---

## 2. Teknik temel

| Konu | Karar |
|---|---|
| Motor | **Unity 6000.4.6f1** |
| Render | **URP**, 3D |
| Girdi | **Yalnızca yeni Input System** (Active Input Handling = *Input System Package (New)*). `UnityEngine.Input` / `Input.GetKey` **kullanılmaz**. |
| Kamera | **Cinemachine 3** (`Unity.Cinemachine` namespace, `CinemachineCamera`) |
| Metin | TextMeshPro (formüller `H<sub>2</sub>O`) |
| Hedef | Windows, 1920×1080 referans, 60 FPS |
| Kod konumu | `Assets/_Project/Scripts/` (yapı: `BilgiAdasi_Docs/03_Teknik/04-teknik-mimari.md`) |
| Namespace | `BilgiAdasi.Core`, `.Player`, `.Chemistry`, `.Order`, `.Desk`, `.City`, `.UI` |

Yeni paket eklemeden önce **sor**.

---

## 3. Girdi kuralları — HER ZAMAN klavye/fare + gamepad

**Kural:** Eklenen her aksiyon, aynı commit'te hem **Keyboard&Mouse** hem **Gamepad** bağlamasıyla eklenir. Sadece klavye bağlaması olan aksiyon "bitmiş" sayılmaz.

- Tek asset: `Assets/_Project/Input/BilgiAdasiControls.inputactions`, C# sınıfı üretilir (`Generate C# Class` → `Scripts/Core/Input/`).
- Kodda tek giriş noktası: `GameInput.Controls` (tek örnek), map değişimi yalnızca `GameInput.SetMode(InputMode.Player|Station|UI)`. Son cihaz: `GameInput.LastDevice` / `DeviceChanged`.
- Control scheme'ler: `KeyboardMouse`, `Gamepad`.
- Action map'ler aynı anda yalnızca biri açık olacak şekilde yönetilir: `Player`, `Station`, `UI`.
- Ekrandaki tuş ipuçları **son kullanılan cihaza göre** değişir (`E` ↔ `Ⓧ`). `GameInput.DeviceChanged` ile.
- Gamepad'de istasyon modunda **imleç yok** (karar 13.11, sanal imleç rahat değildi): ↑↓ ile odak/seçim, A onay, RB/LB artır-azalt. İstasyondayken UI'ın kendi yön gezinmesi kapalıdır (`GameInput.SetMode`). Ekranda tuş ipuçları cihaza göre değişir.
- Ölü bölge (deadzone): çubuklar için 0.15 (Stick Deadzone processor).

| Aksiyon | Map | Klavye / Fare | Gamepad |
|---|---|---|---|
| Move | Player | WASD | Sol çubuk |
| Look | Player | Fare delta | Sağ çubuk |
| Sprint (hızlı yürü) | Player | Sol Shift (basılı) | Sol çubuk bas (L3, aç/kapa) |
| Interact | Player | E | X (West) |
| Jump | Player | Space | A (South) |
| Crouch (basılı) | Player | Sol Ctrl | — |
| CrouchToggle (aç/kapa) | Player | C | B (East) |
| OpenMap | Player | M / Tab | Select (View) |
| Pause | Player | Esc | Start (Menu) |
| Pause | Station | P (Esc istasyondan çıkar) | Start (Menu) |
| Point | Station | Fare pozisyonu | — (imleç yok) |
| Click / Drag | Station | Sol tık | — (Confirm kullanılır) |
| Confirm | Station | Enter / Space | A (South) |
| SelectPrevious / SelectNext | Station | W S / ↑ ↓ | D-pad ↑↓ / sol çubuk ↑↓ |
| Increase / Decrease | Station | Fare tekeri / + − | RB / LB |
| FireStove | Station | F | Y (North) |
| OpenHint | Station | H | X (West) |
| ExitStation | Station | Esc / sağ tık | B (East) |
| Navigate / Submit / Cancel | UI | Ok tuşları, Enter, Esc | D-pad / sol çubuk, A, B |

Yeni aksiyon eklenirse bu tablo güncellenir.

---

## 4. Oyuncu: birinci şahıs (FPS) kamera ve hareket

**Kamera kesinlikle birinci şahıs.** Üçüncü şahıs yok; oyuncu modeli kamerada görünmez.

### 4.1 His hedefi
Hedef kitle 15–16 yaş, çoğu FPS oyunlarına alışık ama bu bir aksiyon oyunu değil: **çevik ve tepkili, ama sakin ve gerçekçi.** Kaygan değil, baş döndürmez.

### 4.2 Hareket (CharacterController tabanlı)
Rigidbody değil `CharacterController` kullanılır (öngörülebilir, merdiven/eşik dostu).

| Parametre | Başlangıç değeri | Not |
|---|---|---|
| Yürüme hızı | 3.8 m/s | Gerçekçi tempolu yürüyüş |
| Hızlı yürüme (sprint) | 5.6 m/s | Koşu değil, "acele" |
| Hızlanma (zemin) | 45 m/s² | Tam hıza ~0.08 sn — oturaklı (14 kaygan hissettirdi, 13.11) |
| Yavaşlama (zemin) | 55 m/s² | Bırakınca hemen durur, kaymaz |
| Eğilme | Boy 1.10 m, hız 2.0 m/s, geçiş 4 m/s | Tavan varsa kalkmaz; koşunca/zıplayınca kalkar |
| Yön | Geri ×0.7, yan ×0.85; koşu yalnızca ileri | Ağırlık hissi |
| Eğim | Yokuş yukarı ×0.65'e kadar, aşağı ×1.1'e kadar | slopeLimit'e oranlı |
| Sert iniş | > 7.5 m/s düşüşte 0.2 sn hız ×0.6 | Normal zıplama inişi (~6.3) etkilenmez |
| Adım | Adım boyu 1.9 m (yürüyüşte ~2 adım/sn) | Kafa sallanması ve adım sesi aynı ritim |
| Yön değiştirme | Hızlanma vektörel uygulanır | Ani dönüşte savrulma yok |
| Havada kontrol | %30 | |
| Zıplama | **Açık**, 1.0 m (karar: 13.11), coyote 0.1 sn, tampon 0.12 sn | `allowJump` ile kapatılabilir |
| Yerçekimi | −20 m/s² | Basamaklardan inerken "süzülme" olmasın |
| Slope limit / Step offset | 45° / 0.3 m | Kaldırım ve eşikler takılmadan çıkılır |

Tüm değerler `PlayerMovementSettings` ScriptableObject'inde; play mode'da canlı ayarlanabilir.

### 4.3 Bakış
- Fare: **ham girdi, yumuşatma yok** (gecikme hissi yaratır). Hassasiyet ayarlanabilir.
- Gamepad: hız eğrisi (`AnimationCurve`) + hafif ivmelenme; yatay 180°/sn, dikey 120°/sn başlangıç.
- Dikey açı sınırı: −80° / +80°. Kamera **roll yok**.
- FOV varsayılan 70, ayarlardan 60–90.

### 4.4 Rahatlık (baş dönmesi önleme)
- Kafa sallanması (head bob) çok hafif: genlik ≤ 0.025 m, ayarlardan kapatılabilir.
- Adım hissi: ayak yere basınca kamera ≤ 0.025 m çöker ve ~0.3° öne eğilir (roll değil); iki adımda bir küçük yan salınım (≤ 0.0125 m).
- Dururken çok yavaş nefes salınımı (0.004 m, dakikada 15).
- Koşarken FOV +4° (yumuşak geçiş). İstasyona girince sıfırlanır.
- Adım/zıplama/iniş sesleri: `PlayerFootsteps` (klipler Inspector'dan, CC0).
- İniş sarsıntısı çok küçük: kamera en fazla 0.035 m çöker, hızla toparlanır.
- Ekran ortasında küçük bir nokta (crosshair) — referans noktası baş dönmesini azaltır.

### 4.5 Etkileşim
- Kamera merkezinden raycast, menzil 2.2 m, `IInteractable` arayüzü.
- Bakılan nesne hafif kontur alır + ekranda `[E] Kazana geç` / `Ⓧ Kazana geç` ipucu.

---

## 5. İstasyon modu (kazan, masa, kapı, harita)

Oyuncu bir istasyona yaklaşıp Interact yapınca:
1. `Player` map kapanır, `Station` map açılır; hareket ve bakış durur.
2. Cinemachine istasyonun sabit kamerasına **0.5 sn blend** ile geçer (istasyon kamerası oyuncunun boyundan bağımsızdır; okunurluk her zaman aynı).
3. İmleç açılır (fare) ya da sanal imleç belirir (gamepad).
4. ExitStation ile tersine döner; oyuncu bıraktığı noktaya ve bakış açısına geri gelir.

İstasyonlar: `CauldronStation` (sipariş), `DeskStation` (masa), `DoorStation` (sakinle konuşma), `ChalkboardStation` (okuma). Hepsi `StationBase`'ten türer.

---

## 6. Şehirde yürüme

Oyuncu mahalleler arasında **yürüyerek** gider (karar: 13.11). Bu, dokümanlardaki "harita tıklamalı, oyuncu yürümez" kuralının yerini alır.

**24 saat kapsamı için sınırlar:**
- Prototipteki şehir **küçük ve kompakt**: atölye + iki mahalle alanı + aradaki tek sokak/meydan. Uçtan uca yürüyüş ≤ 40 sn.
- Kilitli mahalle/ilçeler: sis + kapalı kapı/bariyer ile kesilir, içleri modellenmez.
- Şehir greybox'ı ve modelleri **Mustafa** yapar; kod `08-mustafa-entegrasyon.md`'deki bağlantı noktalarına dayanır. Tek oyun sahnesi: `Town` (atölye + şehir).
- Harita ekranı (`OpenMap`) kalır: bilgi + ilerleme ekranı (mahalle durumu, 5 nokta). Hızlı seyahat: P2, zaman kalırsa.
- Görev akışı: mahalledeki sakin/ulak oyuncuyu **atölyeye** çağırır; görevler atölyedeki istasyonlarda çözülür. Mahallede yalnızca "önce/sonra" değişimi görülür.

---

## 7. Boy uzama sistemi (büyüme)

Oyuncu öğrendikçe büyür: sınıf atladıkça belirgin, her mahalle bittikçe çok az.

| Sınıf | Boy | Göz yüksekliği | Mahalle başına artış |
|---|---|---|---|
| 9 | 1.60 m | 1.50 m | +0.01 m |
| **10 (prototip)** | 1.66 m | 1.56 m | +0.01 m |
| 11 | 1.71 m | 1.61 m | +0.01 m |
| 12 | 1.75 m | 1.65 m | — |

- Mahalle artışı bir sonraki sınıfın boyunu **geçemez** (clamp).
- `PlayerGrowth` bileşeni: `GameEvents.NeighborhoodCompleted` dinler, hedef boyu hesaplar, **2 sn yumuşak geçişle** `CharacterController.height/center` ve kamera kök yüksekliğini günceller.
- Büyümeden önce **tavan kontrolü** (`Physics.CapsuleCast` yukarı); doluysa açık alana çıkınca uygulanır.
- Geri bildirim: ekranda kısa not — *"Biraz uzadın gibi…"*. Sınıf atlamada belirgin ses + not.
- Prototipte görünen: 1.66 → 1.67 → 1.68 (iki mahalle). Sunumda vurgulanacak bir detay.
- Seviye tasarımı uyarısı: kapı, tezgâh, raf yükseklikleri 1.50–1.65 m göz aralığında rahat görünecek şekilde yapılır.
- Kayıt: `SaveData.grade`, `SaveData.completedNeighborhoods` → boy her yüklemede hesaplanır (ayrıca kaydedilmez).

---

## 8. Kod kuralları

- Tanımlayıcılar (sınıf, metot, değişken) **İngilizce**; yorumlar ve oyuncuya görünen metinler **Türkçe**.
- Oyuncuya görünen metin kodda sabit yazılmaz; ScriptableObject'te durur.
- `[SerializeField] private` alanlar; public alan yok (property kullan).
- `Update` içinde `Find`, `GetComponent`, LINQ, `new` (gereksiz ayırma) yok. Referanslar Inspector'dan.
- Sistemler arası iletişim `GameEvents` üzerinden; denetleyiciler görsel efektleri doğrudan bilmez.
- UI view'ları `RegisteredView<IXView>`'den türer (açılınca `ViewLocator`'a kaydolur); denetleyiciler `ViewLocator.Get<IXView>()` ile bulur, Canvas/obje referansı tutmaz.
- Singleton yalnızca `GameManager`, `SaveSystem`, `SceneFlow` için.
- Veri ScriptableObject'te (`BilgiAdasi_Docs/03_Teknik/04-teknik-mimari.md` §3).
- Kimya hesabı (`AtomMath`) saf C#; Unity'ye bağımlı değil; EditMode testleri var.

### Sözlük (Türkçe → kod)
Şehir `Subject` · İlçe `Theme` · Mahalle `Neighborhood` · Görev `Task` · Sipariş `OrderTask` · Masa görevi `DeskTask` · Kazan `Cauldron` · Terazi `Scale` · Raf `Shelf` · Kavanoz `Jar` · Ocak `Stove` · Tahta `Chalkboard` · Usta `Master` · Sakin `Resident` · Ulak `Messenger` · Büyüme `Growth`

---

## 9. Claude ile çalışma kuralları

- **Hedefli değişiklik:** Yalnızca istenen dosya/metot değişir. Çalışan dosyayı baştan yazma; değişen kısmı ver.
- Kod verdikten sonra **Unity'de yapılacak adımları** kısa liste halinde yaz (hangi objeye hangi bileşen, Inspector'da hangi referans, hangi action map).
- Her yeni girdi aksiyonunda klavye/fare **ve** gamepad bağlamasını ver; §3 tablosunu güncelle.
- Eski Input Manager API'si, Cinemachine 2 API'si (`CinemachineVirtualCamera`) ya da URP dışı (Built-in) shader önerme.
- Yeni paket, hazır asset ya da dış kod önerirsen lisansını belirt ve LICENSES.md satırını da yaz.
- Kapsamı büyüten fikirlerde önce 24 saat etkisini söyle; P0/P1/P2 önceliğini `05_Uretim/06-yapilacaklar.md`'ye göre belirt.
- Emin olmadığın Unity 6 API'sinde tahmin etme; söyle.
- İletişim dili: Türkçe, kısa ve doğrudan.
