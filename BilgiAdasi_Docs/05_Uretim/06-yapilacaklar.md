# 06 — Yapılacaklar

Öncelik sırası yukarıdan aşağı. Süre daralırsa **aşağıdan** kesilir. P0 olmadan oyun yok.

## Roller

| Rol | Sorumlu | Kapsam |
|---|---|---|
| Mekanik / kod | Mehmet | Bu listedeki `[KOD]` işleri, build |
| Harita / sahne + UI | Mustafa Akbaş | Şehir/köy sahnesi (greybox dahil), atölye yerleşimi; HUD, kâğıt, kart, sayaç, tahta, harita ekranı, menüler, fontlar |
| Modelleme | … | Kazan, teraziler, kavanoz, sakinler |
| İçerik / sunum | … | `05` metinlerinin kontrolü, ders kitabı doğrulaması, sunum, `05_Uretim/LICENSES.md` |

## P0 — Çekirdek (Cuma akşamına kadar)

- [ ] `[KOD]` Proje iskeleti: klasörler, sahneler, `SceneFlow`, `GameEvents`, `SaveSystem`
- [ ] `[KOD]` Kimya verisi: `ElementData`, `SpeciesData`, `ReactionData` + `AtomMath` + **birim testleri** (R_propane dengede mi, IsMinimal)
- [ ] `[KOD]` FPS oyuncu (CharacterController, bakış, etkileşim) + istasyon modu (Cinemachine geçişi)
- [ ] `[KOD]` Test sahnesi (`_Placeholder`) + sahne bağlantı noktaları (`08-mustafa-entegrasyon.md`)
- [ ] `[KOD]` Sipariş: raf → kazan sürükleme, `−/+`, iki terazi + sayaçlar, otomatik denge → ocak
- [ ] `[KOD]` Köpürme + yanıp sönen atomlar + `failCount`
- [ ] `[KOD]` Mühür notu
- [ ] `[KOD]` Tahta: denklem metni üretimi (alt indis, 1 katsayısı gizli)
- [ ] `[KOD]` `NeighborhoodRunner`: 5 görevi sırayla çalıştır
- [ ] `[İÇERİK]` Denkleştirme köyünün 5 siparişi SO olarak girildi
- [ ] `[KOD]` Görev 4 zinciri + Görev 5 `Purchase` modu
- [ ] **Gece yarısı ilk build**

## P1 — İkinci görev tipi ve çerçeve

- [ ] `[KOD]` Masa: kamera geçişi, güvercin/zarf, `PaperView`
- [ ] `[KOD]` Soru render'ları: `MultipleChoice`, `TrueFalse`, `Classify`, `FillBlank`
- [ ] `[KOD]` `BalanceEquationQ` (ReactionData'dan otomatik)
- [ ] `[KOD]` Lonca Sınavı: rastgele çekme + yanlışta soru değişimi
- [ ] `[İÇERİK]` Tepkime Türleri köyünün 5 görevi SO olarak girildi
- [ ] `[KOD]` Harita: şehir/ilçe/köy düğümleri, kilit durumları, 5 nokta göstergesi
- [ ] `[KOD]` İpucu defteri (iki tip için ortak)
- [ ] `[KOD]` Boy uzama (`PlayerGrowth`, `CLAUDE.md` §7)
- [ ] `[KOD]` Gamepad sanal imleç (istasyon modu)
- [ ] `[KOD]` "Gerçek hayatta" kartı
- [ ] `[KOD]` Emeği Geçenler ekranı (`05_Uretim/LICENSES.md`'den elle aktarılır) — **lisans kuralı için zorunlu**

## P2 — Cila

- [ ] `[KOD]` Sınıf açılış/kapanış ara sahnesi
- [ ] `[KOD]` Köy canlanma görselleri (aç/kapa parçalar)
- [ ] `[KOD]` Geliştirici kısayolları (görev atla, kayıt sıfırla) — **demo için**
- [ ] `[KOD]` Sakin figürleri / 2D kartlar kapıda
- [ ] Ses: köpürme, ocak, mühür, kâğıt (CC0)
- [ ] Müzik (CC0)

## P3 — Varsa

- [ ] Kapanışta sınıfta mini denkleştirme etkileşimi
- [ ] Kilitli şehirler için sisli silüetler

## Zaman planı

| Zaman | Hedef |
|---|---|
| Cuma 12.00–20.00 | P0 kod. Modelci atölye + kazan + terazi, UI sayaç/kart tasarımı paralel |
| Cuma 20.00–24.00 | P0 bitiş, veriler girildi, **build 1** |
| Cmt 00.00–05.00 | P1 |
| Cmt 05.00–08.00 | P2, hata düzeltme. **08.00 kod dondurma** |
| Cmt 08.00–10.00 | **Son build**, Emeği Geçenler, demo provası, yedek ekran kaydı |
| Cmt 10.00–11.00 | Tampon. Yeni özellik yok |

## "Bitti" tanımı

- Build Windows'ta açılır, iki köy uçtan uca hatasız oynanır.
- `02` ve `03`'teki kabul kriterleri işaretli.
- `05_Uretim/LICENSES.md` dolu; oyunda Emeği Geçenler var.
- Projektörde yazılar okunuyor.
- 4 dakikalık demo provası yapıldı; geliştirici kısayolları çalışıyor.

## Açık işler / sorulacaklar

- [ ] **Organizasyona sor:** "0'dan üret" eski kendi model/kodlarımızı kapsıyor mu? Cevaba göre `README` güncellenir.
- [ ] Tür adları (analiz/ayrışma), 10. sınıf Maarif ders kitabıyla kontrol.
- [ ] "Gerçek hayatta" cümleleri kitap/öğretmen kontrolü.
- [ ] Görev 5 fiyatları oynanarak ayarlanacak.
