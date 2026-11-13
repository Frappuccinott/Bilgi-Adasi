# 12 — İlk 20 Görev: Mekanik Analizi ve Kod Planı (Mehmet)

> Durum: analiz, 13.11 akşam. Kaynaklar: `02_Mekanik/04-gorev-plani.md` (senaryo, Mustafa + Claude), `03-masa-mekanigi.md`, `04_Icerik/05-icerik-prototip.md`, `11-gorev-ve-basarim-listesi.md`, `03_Teknik/04-teknik-mimari.md` ve **projedeki kodun bugünkü hâli**.
> Kapsam: **Seviye 1–20** = Denkleştirme (D-1…D-5) + Tepkime Türleri (T-1…T-5) + Bereket Tarlası (A1…A10). Seviye 0 (Çıraklık tanıtımı) aynı parçaları kullandığı için dahil edildi.
> Senaryo metinleri dokümanlarda hazır; **eksik olan mekanik ve veri (SO)**. Bu belge: hangi mekanik lazım, hangisi var, nereye yazılacak, ne kadar sürer.

---

## 0. Özet

| | Görevler | Senaryo | Çalışıyor mu? | Eksik |
|---|---|---|---|---|
| Denkleştirme | D-1…D-5 (Sv 1–5) | ✅ `05` §2 + SO verisi var | **Çekirdek çalışıyor** (sipariş, kazan, terazi, ocak, zincir, Purchase, kapıda teslim) | Görev katmanları: Tahmin, Atom Görüşü, Hata Dedektifi, Sorgu, Keşif Defteri |
| Tepkime Türleri | T-1…T-5 (Sv 6–10) | ✅ `05` §3 | ❌ **Hiç çalışmıyor**: masa mekaniği (Faz 5) yazılmadı, `Data/Questions` boş, köy asset'i yok | Masa akışı + 5 soru tipi + Lonca Sınavı + katmanlar + kapanış |
| Bereket Tarlası | A1…A10 (Sv 11–20) | ✅ `04` §5 (planda **Sürüm 2**) | ❌ Yalnızca modeller var (`Art/Traktor/`, `Art/Harita/Bereket_Tarlasi.fbx`) | Traktör, alet, parsel, saha sorusu kartı, parametrik soru motoru, yeni türler, tarla görev akışı |

**24 saat etkisi (dürüst tahmin):** Kod dondurmaya (Cmt 08.00) ~12 saat var.
- Sv 6–10'u **oynanır** yapmak (masa + soru tipleri + Lonca + kapanış) ≈ **10,5 saat** → dondurmaya sıkı sırayla yetişir.
- Sv 0–10'un **bütün katmanları** (Defter, Sorgu, Tahmin, Atom Görüşü, Hata Dedektifi, tanıtım) ≈ **+17 saat** → hepsi yetişmez; §7'deki sırayla en değerlileri seçilir.
- Sv 11–20 (tarla) ≈ **22 saat** ek → **dondurmaya yetişmez.** Öneri: yarışmada tarla "oyunun vaadi" olarak modellerle gösterilir (planın kendi kararı, `04` §0); istenirse yalnızca **A1–A2 dikey dilimi** (traktöre bin, pulluk, 1 saha sorusu) denenir (~5–6 sa) — ancak iki köy bittikten sonra.

---

## 1. Projede hazır olanlar (tekrar yazılmayacak)

| Mekanik | Kod | Not |
|---|---|---|
| FPS oyuncu, etkileşim (E/X), istasyon modu | `Player/*`, `Stations/StationBase` | Masa ve tahta istasyonu iskeleti var, içi boş |
| Envanter (6 slot), kese, pazar, raf | `Order/PlayerInventory`, `Order/World/*`, `Order/Market/*` | Pazar kuralları görevle açılıp kapanabiliyor (`TaskGateRule`) |
| Sipariş: kazan, 3D terazi, ocak, köpürme, boşaltma | `Order/OrderSession` (saf C#, testli), `OrderController`, `Props/ScaleModel` | `Exact` + `Purchase` (artan madde terazide) |
| Zincir görev (2 adım) | `OrderStep`, `OrderSession.AdvanceStep` | D-4 hazır |
| Kapı: sipariş alma, konuşma, elle teslim, ödeme | `DoorStation`, `DialogueDirector`, `NeighborhoodRunner` | Runner **yalnızca `OrderTask`** sunuyor |
| İpucu defteri (2 hatada parlar) | `IHintBookView` + `OrderController.ToggleHint` | Yalnızca siparişte; masa için genelleşmeli |
| "Gerçek hayatta" kartı | `IRealLifeCardView` | Siparişte çalışıyor |
| Tahta (çözülen denklemler) | `ChalkboardText` (`IChalkboardView`) | Yalnızca liste gösteriyor; etkileşim yok |
| Köy önce/sonra + parçalar | `NeighborhoodView` (Part_1…5) | Tepkime köyünün sahne objeleri Mustafa'da |
| Kilitli bölge | `LockedZone` (`unlockedBy`) | Tarla girişi D-5'e bağlanabilir |
| Bisiklet (binilen araç) | `Player/Bicycle/*` | **Traktörün temeli** (binme, kamera, kaldırım, kayıt) |
| Kayıt, olaylar, başarımlar, Esc menüsü, geliştirici kısayolları | `Core/*`, `City/DevShortcuts` | Başarım kimlikleri `11`'deki görev kimlikleriyle hazır |
| Kimya verisi | 5 element, 12 tür, 8 tepkime (`Data/`) | Glikoz (C₆H₁₂O₆) ve fotosentez/solunum **yok** |

---

## 2. Görev × mekanik matrisi

Gösterim: ✅ var · 🟡 kısmen (küçük ek) · ❌ yazılacak. Mekanik kodları §3'te.

| Sv | Görev | Ana görev | Katmanlar (`04` planı) | Gereken mekanikler | Durum |
|---|---|---|---|---|---|
| 0 | Çıraklık: İlk Kural | Kazana 2 H₂, ısıt (tepkime yok) | Tahmin · Atom Görüşü (tanıtım) · Sorgu · K-0 | M3 Tahmin/Sorgu kartı · M4 Defter · M5 Katman akışı · M6 Atom Görüşü · **M8 Tanıtım siparişi** | ❌ |
| 1 | D-1 Kuru Çeşme | ✅ sipariş | Tahmin (P2) · **Atom Görüşü** · Sorgu · K-1 | M3 · M4 · M5 · M6 | 🟡 görev ✅, katmanlar ❌ |
| 2 | D-2 İsli Fenerler | ✅ sipariş | **Tahmin (çoklu seçim, "fener söner" görseli)** · Sorgu · K-2 | M3 (çoklu seçim + görsel geri bildirim olayı) · M4 · M5 | 🟡 |
| 3 | D-3 Soğuk Fırın | ✅ sipariş | **Hata Dedektifi (2 tur)** · K-3 (sorgu yok) | **M7 Hata Dedektifi** · M11 tahta etkileşimi · M4 · M5 | 🟡 |
| 4 | D-4 Yıkık Köprü | ✅ zincir | **Ara ürün** (rafta CaO parlar, "neyi birleştireceksin?") · Sorgu · K-4 | M9 ara ürün vurgusu + raf seçimi (M3) · M4 · M5 | 🟡 |
| 5 | D-5 Gübre Ruhu | ✅ Purchase | **Tahmin: bütçe (18)** · **Artan madde (Atom Görüşü'nde "artan")** · Sorgu · K-5 | M3 · M6 + M10 artan etiketi · M4 · M5 | 🟡 |
| 6 | T-1 Ustanın Defteri | ❌ FillBlank | **Gözlem kartları → boşluğa** · K-6 | **M1 Masa akışı** · **M2 FillBlank** (+ gözlem kartı sürükleme) · M4 | ❌ |
| 7 | T-2 Etiket Masası | ❌ Classify | Formülsüz gözlem kartları (yalnız veri) · K-7 | M1 · **M2 Classify** · M4 | ❌ |
| 8 | T-3 Fırıncının Mektubu | ❌ MultipleChoice | **Kanıt kartları (çoklu) + "Defterimden kanıt göster"** · Sorgu · K-8 | M1 · **M2 MC** · M3 çoklu seçim · **M4 defterden kanıt seçme** · M5 | ❌ |
| 9 | T-4 Muhtarın Mektubu | ❌ TrueFalse | **Y'den sonra düzeltme satırı** + defterden kanıt · K-9 | M1 · **M2 TrueFalse** (+ düzeltme alt sorusu) · M4 | ❌ |
| 10 | T-5 Lonca Sınavı | ❌ GuildExam | **Transfer: fotosentez/solunum denklemleri** · Sorgu · K-10 · **kapanış: Keşif Defteri son ekranı** | M1 · **M2 GuildExam + BalanceEquation** · **M12 glikoz + 2 tepkime** · M3 · M4 · **M14 kapanış** | ❌ |
| 11 | A1 Makineyi Uyandır | Hata Dedektifi → kazan siparişi (yakıt) | K-11 | M7 · ✅ sipariş (R_methane) · **M15 traktör** · M16 yakıt · **M20 tarla görev akışı** · M4 | ❌ |
| 12 | A2 İlk Saban | Pulluk tak → sür | Saha sorusu (şerit + süre) · K-12 | M15 · **M17 alet** · **M18 parsel** · **M19 saha sorusu kartı (sayı girişi)** · M20 | ❌ |
| 13 | A3 Toprağı Tanı | Raftan seç (Ca(OH)₂) | Tahmin · K-13 | M3 (seçenek = raf maddeleri) · M20 | ❌ |
| 14 | A4 Tohum Hesabı | Ekici → ek | Olasılık sorusu · K-14 | M17 · M18 · M19 | ❌ |
| 15 | A5 Ekim Düzeni | Ekici | 2 soru (dönüş sayısı, 3!) · K-15 | M17 · M18 · M19 (çok soru) | ❌ |
| 16 | A6 Kuyudan Tarlaya | Su tankı kuyudan dolar → sula | İş/güç (2 sayı) · K-16 | M17 · **M21 kuyu etkileşimi** · M18 · M19 | ❌ |
| 17 | A7 Elif'in Gübresi | Serpici; terazide N₂+H₂→NH₃ | Biyo MC + denkleştir · K-17 | M17 · M18 · ✅ sipariş (R_ammonia) · M19 · M2 BalanceEquation | ❌ |
| 18 | A8 Güneşten Besine | Hata Dedektifi → terazide fotosentez | K-18 | M7 · **M12 glikoz/fotosentez** · ✅ sipariş | ❌ |
| 19 | A9 Hasat Raporu | Biçer; defterde **kendi verim grafiğin** | Ortalama sorusu · K-19 | M17 · M18 · **M22 verim kaydı + grafik** · M19 | ❌ |
| 20 | A10 Değirmenden Fırına | Başak → değirmen, un → fırın | 3 soru (oran, CO₂, yenilenebilir) · K-20 | **M21 değirmen/fırın etkileşimi** · M19 · M20 | ❌ |
| 11–20 | (hepsi) | | Olay soruları (yağmur, kuş, yokuş), verim ±1, **mühür çentiği** | M19 olay tetikleyici · **M23 mühür** | ❌ |

---

## 3. Yazılacak mekanikler

Her satırda: ne yapar · nereye · veri · görünüm arayüzü (Mustafa) · girdi · tahmini süre. Süreler test + yer tutucu view dahil.

### Grup A — Ortak öğrenme katmanları (Sv 0–20'nin hepsi kullanır)

| Kod | Mekanik | Ayrıntı | Konum / veri | View | Süre |
|---|---|---|---|---|---|
| **M3** | **Seçim kartı** (Tahmin · Ustanın Sorgusu · kanıt · raf seçimi) | Konuşma balonunun altında 3–5 seçenek; tek ya da çoklu seçim; doğruda "Aynen öyle.", yanlışta yanılgı notu + doğru açıklama (görev geri alınmaz). Seçenek bir **defter kaydı** ya da **madde** olabilir. Sonuç olayı: `GameEvents.PromptAnswered(promptId, correct)` (başarım "ilk seferde doğru" için hazır) | `Scripts/Learning/ChoicePrompt*.cs` · SO `ChoicePrompt` (`Data/Prompts/`) | `IChoicePromptView` | 3 sa |
| **M4** | **Keşif Defteri** | Görev bitince K-n kaydı yazılır (kayıtta), defterin 2. ayracı "Keşiflerim"; ipucunun altında ilgili eski kayıt ("Sayfa K-2'ye bak"); M3'e kayıt seçeneği verir; kapanışta tüm defter | `Scripts/Learning/DiscoveryBook.cs` · SO `DiscoveryData` (`Data/Discoveries/K-0…K-20`) · `SaveData.discoveries` | `IHintBookView` genişler ya da `IDiscoveryBookView` | 2,5 sa |
| **M5** | **Görev katman akışı** | Bir görevin önüne/arkasına aşama dizer: `Tahmin → Görev → Katman → Sorgu → Defter → Dünya`. Runner görevi başlatmadan önce ve teslimden sonra bu diziyi oynatır. Görev başına veri: hangi tahmin, hangi sorgu, hangi K-n | `Scripts/Learning/TaskLayers.cs` (SO, görev başına) + `NeighborhoodRunner` kancaları | — (M3/M4/M6/M7'yi çağırır) | 2 sa |
| **M6** | **Atom Görüşü** | Tepkime anında kazan üstünde yarı saydam pencere: giren moleküller (atom simgeleri, mühür koduyla) → bağ kopar → ürünler dizilir; sayaç `H 4 → 4 · O 2 → 2`; "tekrar oynat"; Purchase'ta eşsiz kalan molekül "artan" (**M10**). Animasyon verisi `AtomMath` + `ReactionData`'dan otomatik (elle animasyon yok) | `Scripts/Order/AtomViewer*.cs` · olay `OrderController.Reacted` | `IAtomViewer` | 3 sa (basit 2D) |
| **M7** | **Hata Dedektifi** | Tahtada hatalı denklem; oyuncu dengesiz elementleri işaretler (açık/kapalı), sonra "Neden?" (M3). Kontrol: işaretlenen küme = `AtomMath.Difference` ≠ 0 olan elementler (elle yazılmaz). Yanlışta o elementin iki taraftaki sayısı tebeşirle yazılır. Birden çok tur | `Scripts/Learning/ErrorHunt*.cs` · SO `ErrorHunt` (yanlış katsayılı denklem) · **saf C# + EditMode testi** | `IChalkboardView` genişler: `ShowErrorHunt`, `MarkCounts` | 2,5 sa |
| **M11** | Tahta etkileşimi | `ChalkboardStation` M7 için istasyona döner (gamepad: ↑↓ element, A işaretle) | `Stations/ChalkboardStation` | — | 0,5 sa |

### Grup B — Masa (Tepkime Türleri, Sv 6–10)

| Kod | Mekanik | Ayrıntı | Konum / veri | View | Süre |
|---|---|---|---|---|---|
| **M1** | **Masa görev akışı** | `DeskTask : TaskData` (wrapper: Defter/Etiket/Mektup/Lonca, soru listesi, randomCount). `DeskController` durum makinesi `PigeonArrive → Envelope → Answering ⇄ Wrong → Stamp → RealLife → Done` (`04` §5). Güvercin = basit (zarf masada belirir — kesme sırası 3). `NeighborhoodRunner` masa görevini sunar (`TryGetNext<DeskTask>`, masa kullanılınca). Yanlış → `GameEvents.Failed`, 2 yanlışta defter (ipucu ortaklaşır) | `Scripts/Desk/` (namespace `BilgiAdasi.Desk`, `CLAUDE.md`'de var) · `Data/Tasks/TepkimeTurleri/tur_1…5` · köy asset'i `Neighborhood_tepkime_turleri` | `IPaperView` (kâğıt, mühür, leke) | 3 sa |
| **M2** | **Soru tipleri** | `QuestionData` (ayrı SO, `04` §3): `MultipleChoiceQ` (şık karıştır), `TrueFalseQ` (D/Y mührü, yanlış satır lekelenir; **Y'den sonra düzeltme alt sorusu**), `ClassifyQ` (kart → kutu), `FillBlankQ` (kelime bankası + çeldirici; **T-1 gözlem kartları** = ek kart listesi), `BalanceEquationQ` (`ReactionData`'dan otomatik katsayı kutuları, `IsMinimal` notu), **GuildExam** (havuzdan rastgele 4, yanlış soru havuzdan başkasıyla değişir). Gamepad'de sürükle yok: ↑↓ seç, A "al", ↑↓ hedef, A "bırak" | `Scripts/Desk/Questions/*` · `Data/Questions/` · **kontrol mantığı saf C# + EditMode testleri** | `IPaperView` alt parçaları (soru başına render) | 5 sa |
| **M12** | Yeni kimya verisi | Tür **C₆H₁₂O₆** (glikoz; yeni element yok), tepkimeler **R_photosynthesis** (6CO₂ + 6H₂O → C₆H₁₂O₆ + 6O₂), **R_respiration**. Ayrıca T-5 havuzundaki `R_co`, `R_waterSplit` zaten var | `Chemistry/PrototypeChemistrySeed` + Kurulum 2 | — | 0,5 sa |
| **M14** | Prototip kapanışı | İki köy bitince: usta mektubu (`05` §4) → Keşif Defteri son ekranı (11 sayfa) + oyuncunun tahtası + görev başına hata sayısı → Emeği Geçenler. `Classroom` sahnesi outro | `Scripts/City/EndingFlow.cs` + `SceneFlow` | `IEndingView`, `ICreditsView` | 2 sa |

### Grup C — Denkleştirme ekleri (Sv 0–5)

| Kod | Mekanik | Ayrıntı | Süre |
|---|---|---|---|
| **M8** | Tanıtım siparişi (Sv 0) | `OrderMode.Demonstration`: tepkimesiz "ısıt" — ateşte köpürme yok, terazi iki kefe aynı kalır, Atom Görüşü aynı 4 H'yi dolaştırır. Usta replikleri + mühür notu bir kez. Görev kimliği `cirak_0` (başarım kökü buna bağlanabilir) | 1,5 sa |
| **M9** | Ara ürün vurgusu (D-4) | 1. adım bitince ürün kavanozu (CaO) envanterde/rafta parlar; usta "neyi birleştireceksin?" (M3, seçenekler raf maddeleri); yanlışta tahtada 1. adım satırı parlar | 1 sa |
| **M10** | Artan madde etiketi (D-5) | Atom Görüşü'nde eşsiz kalan N₂ "artan" etiketi; `OrderSession` artan sayısı zaten var | 0,5 sa (M6 ile) |
| — | D-2 "fener söner" | Tahmin yanlışında görsel olay: `GameEvents.PromptFeedback(promptId, optionId)` → Mustafa'nın fener objesi dinler | 0,3 sa |

### Grup D — Bereket Tarlası (Sv 11–20, planda Sürüm 2)

| Kod | Mekanik | Ayrıntı | Konum | Süre |
|---|---|---|---|---|
| **M15** | **Traktör sürüşü** | Bisikletin binme/kamera/kaldırım/kayıt mantığı ortak tabana alınır (`RideableVehicle`) ya da `Bicycle`'dan uyarlanır; hız 4 m/s, sürmede 1,6 m/s; koltuk kamerası `Traktor_Koltuk_Kamera`; tekerler/direksiyon/volan dönüşü (`Traktor_*` objeleri, `04` §11) | `Scripts/Field/Tractor*.cs` · `TractorSettings` SO | 4 sa |
| **M16** | Yakıt | Bakır ibre; boşalınca kazanda yakıt siparişi (✅ `OrderTask` + R_methane) → traktöre dökülür | `Field/TractorFuel` | 1 sa |
| **M17** | **Alet takma** | Ahırda askıdan alet seç (Pulluk, Ekici, Su tankı, Serpici, Biçer, Kızak), `Traktor_Baglanti` pimine tak; indir/kaldır | `Field/Implement*.cs` | 2,5 sa |
| **M18** | **Parsel sistemi** | 24 parsel × 6 durum (`Tarla_P<satır><sütun>_<Durum>`, `HaritaYerlestir` adları); doğru alet indirilmişken parselin üstünden geçince sonraki duruma (Nadas → Sürülmüş → Ekili → Filiz → Olgun → Hasat); kayıtta parsel durumları | `Field/FieldPlot`, `FieldController` | 3 sa |
| **M19** | **Saha sorusu kartı** | Kâğıt kart + ders rozeti; tipler MC / **Sayı (± ile)** / Grafik okuma / Sürükle-eşle. **Parametrik:** şablon `"{L} m × {W} m"`, parametre aralıkları, cevap ifadesi (`L*W/2` gibi) → **saf C# ifade değerlendirici + EditMode testi**. Açıkken zaman 0,25×. Doğru → verim +1, mühür +1; yanlış → yeni sayılarla 2. deneme; yine yanlış → eylem yapılır, verim −1 (tıkanma yok). Olay soruları (yağmur/kuş/yokuş) rastgele değil, aşamaya bağlı tetik | `Field/FieldQuestion*.cs` · SO `FieldQuestionData` (`Data/FieldQuestions/`) | 5 sa |
| **M20** | **Tarla görev akışı** | `FieldTask : TaskData` (aşama: alet, hedef parsel durumu, sorular, defter). D-5'ten sonra Elif çağırır (tetik), tarla girişi `LockedZone.unlockedBy = denklestirme`. Kimlikler `tarla_1…10` (`11`) | `Field/FieldRunner` · `Data/Tasks/BereketTarlasi/` | 2,5 sa |
| **M21** | Kuyu, değirmen, fırın | Su tankını kuyudan doldur (A6); başağı değirmene, unu fırına (A10) — `WorldInteractable` | `Field/World/*` | 1,5 sa |
| **M22** | Verim kaydı + grafik (A9) | Parsel başına verim (sorulardan ±), defterde sütun grafiği; ortalama sorusu bu veriden | `Field/HarvestLog` | 1,5 sa |
| **M23** | Mühür çentiği | Doğru saha sorusu → ilgili şehrin madalyonuna çentik (kayıt); 10 çentik → kapı açılır (`LockedZone`) | `Core/SaveData` + `City/SealGate` | 1 sa |

---

## 4. Proje yapısı önerisi

Mevcut düzen (`04-teknik-mimari.md` §2) korunur; eklenenler **kalın**:

```
Assets/_Project/
  Scripts/
    Core/        GameEvents (+ PromptAnswered, Reacted, DiscoveryAdded), SaveData (+ discoveries, field, seals)
    Chemistry/   + glikoz/fotosentez/solunum tohum verisi
    Order/       + AtomViewer (M6), OrderMode.Demonstration (M8)
    Desk/        **DeskTask, DeskController, Questions/ (MC, TF, Classify, FillBlank, BalanceEquation), GuildExamPool**
    Learning/    **ChoicePrompt (M3), DiscoveryBook (M4), TaskLayers (M5), ErrorHunt (M7)**   ← yeni namespace önerisi
    Field/       **Tractor, Implements, FieldPlot, FieldQuestion + ExpressionEvaluator, FieldRunner, HarvestLog**   ← yeni namespace
    City/        NeighborhoodRunner (+ DeskTask sunumu, katman kancaları), **EndingFlow**
    Stations/    DeskStation (M1'e bağlanır), ChalkboardStation (M7)
    UI/Placeholder/ + ham Paper, ChoicePrompt, DiscoveryBook, AtomViewer, FieldQuestion view'ları
  Data/
    Tasks/TepkimeTurleri/   tur_1…tur_5            Questions/          (şu an boş)
    Prompts/   Discoveries/ (K-0…K-20)   ErrorHunts/   FieldQuestions/   Tasks/BereketTarlasi/
  Tests/EditMode/  + QuestionCheckTests, ErrorHuntTests, ExpressionEvaluatorTests, GuildExamPoolTests
```

**Karar gerekli (CLAUDE.md §2'deki namespace listesi):** `BilgiAdasi.Learning` ve `BilgiAdasi.Field` eklensin mi? Alternatif: katmanlar `Core`'a, tarla `City`'ye girer (daha az temiz). Öneri: ekle, CLAUDE.md'ye yaz.

**Mantık ↔ görünüm (her yeni sistemde aynı kural):** denetleyici saf veri/olay yayınlar; Mustafa'nın prefab'ı `IPaperView`, `IChoicePromptView`, `IDiscoveryBookView`, `IAtomViewer`, `IFieldQuestionView`, `IEndingView` arayüzlerini uygular; benim yer tutucularım `_Placeholder` altında.

**Kontrol mantıkları saf C#:** soru doğrulama, Hata Dedektifi kümesi, ifade değerlendirici, Lonca havuzu → EditMode testli (AtomMath gibi).

---

## 5. Yeni girdi aksiyonları (her biri klavye/fare + gamepad, `CLAUDE.md` §3'e eklenecek)

| Aksiyon | Map | Klavye / Fare | Gamepad | Mekanik |
|---|---|---|---|---|
| AtomView (aç/kapa, tekrar oynat) | Player | V | R3 | M6 |
| Seçenek seç / onayla (seçim kartı) | UI | ↑↓ / fare, Enter / tık | D-pad / sol çubuk, A | M3 (mevcut UI map yeter) |
| Kâğıtta al-bırak, işaretle | Station | Sol tık / Enter, W S | A, D-pad ↑↓ | M1, M2, M7 (mevcut Station aksiyonları yeter) |
| Defter: ayraç değiştir | Station / Player | Q / E (defter açıkken) | LB / RB | M4 |
| Alet indir / kaldır | Player (sürüşte) | Sol tık | RT | M17 |
| Alet değiştir (ahırda) | Player | Q | Y | M17 |
| Sayı artır / azalt (saha kartı) | UI | ↑ / ↓, fare tekeri | D-pad ↑ / ↓ | M19 |

Grup A–B için **yalnızca AtomView ve defter ayracı** yeni; gerisi mevcut aksiyonlarla çözülür.

## 6. Kayıt ekleri (`SaveData`)

- `discoveries` (K-n listesi) · `promptResults` (prompt → ilk seferde doğru mu; başarım için) · `deskProgress` (Lonca havuzunda çekilenler) — Grup A–B
- `fieldPlots` (24 durum) · `attachedImplement` · `fuel` · `harvest` (parsel verimi) · `seals` (şehir → çentik) · traktör pozu (`SetPose` ✅) — Grup D

---

## 7. Önerilen sıra (dondurmaya göre)

| Sıra | İş | Açtığı görevler | Süre | Öncelik |
|---|---|---|---|---|
| 1 | **M1 + M2 + M12** masa akışı, 5 soru tipi, Lonca, glikoz | T-1…T-5 oynanır (**iki köy uçtan uca = "Bitti" tanımı**, `06`) | 8,5 sa | **P0/P1** |
| 2 | **M4 Keşif Defteri + M3 Sorgu + M5 katman akışı** | Sv 0–10'un hepsine defter + sorgu | 7,5 sa | P1 (`04` §0) |
| 3 | M14 kapanış + Emeği Geçenler | Prototip sonu | 2 sa | P1 (lisans kuralı) |
| 4 | M6 Atom Görüşü + M10 | D-1, D-5, Sv 0 | 3,5 sa | P2 |
| 5 | M7 + M11 Hata Dedektifi | D-3 (sonra A1, A8) | 3 sa | P2 |
| 6 | M8 tanıtım, M9 ara ürün, D-2 görseli | Sv 0, D-4, D-2 | 3 sa | P2 |
| 7 | Grup D (M15–M23) | A1…A10 | 22+ sa | **Sürüm 2** (yarışmada model + plan gösterilir) |

İlk üç madde ≈ 18 saat: dondurmaya kalan ~12 saate **sığmıyor**. Gerçekçi dondurma hedefi: **1 + 3 tam (≈10,5 sa) + 2'nin sadeleştirilmiş hâli** (Sorgu + Defter yalnız Sv 1, 5, 8, 10'da; katman akışı runner'a sabit kanca) + 3. Kalan her şey sunumda anlatılır. Kesme sırası `07-kod-plani.md` §2 ile aynı mantık: önce görsel cila, sonra katmanlar; **iki köyün uçtan uca oynanması kesilmez.**

## 8. Mustafa'dan beklenenler (bu mekanikler için)

- Kâğıt (`IPaperView`), seçim kartı, Keşif Defteri ikinci ayraç, Atom Görüşü penceresi, saha sorusu kartı tasarımları (`04` §10).
- Tepkime Türleri köyünün sahne parçaları (Part_1–5: güvercinler, flamalar, saksılar, pano, sancak) + masa ve güvercin noktası.
- Tarla: ahır + alet askısı, kuyu, değirmen, fırın etkileşim noktaları; traktör prefab'ında boş objeler (`Traktor_Koltuk_Kamera`, `Traktor_Baglanti`) zaten var.
