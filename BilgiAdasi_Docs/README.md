# Bilgi Adası — Geliştirme Dokümanları

> Eğitsel oyun prototipi · Teslim: **Cumartesi 14 Kasım 2026, 11.00** (çalışan prototip kodu + sunum)
> Motor: Unity · Hedef kitle: 10. sınıf (15–16 yaş) · Konu: Kimya (simya çerçevesinde)

Bu klasör, prototipi geliştiren ekibin tek doğru kaynağıdır. Bir karar değişirse önce ilgili `.md` güncellenir, sonra kod.

## Klasör yapısı

```
BilgiAdasi_Docs/
├── README.md                  ← buradan başla
├── 01_Tasarim/   01-oyun-tasarimi.md · 09-tema-ve-model-kurallari.md · 11-ui-ux-plani.md · referans/
├── 02_Mekanik/   02-siparis-mekanigi.md · 03-masa-mekanigi.md · 04-gorev-plani.md
├── 03_Teknik/    04-teknik-mimari.md · 08-mustafa-entegrasyon.md
├── 04_Icerik/    05-icerik-prototip.md · 10-gorev-pazar-npc.md · 11-gorev-ve-basarim-listesi.md ·
│                 12-ders-notu-arastirma.md · 15-bereket-tarlasi-mat-biyo.md · 17-icerik-kontrol-listesi.md
└── 05_Uretim/    06-yapilacaklar.md · 07-kod-plani.md · 12-ilk-20-gorev-mekanikleri.md · 13-ders-notu-plani.md ·
                  14-defter-plani.md · 16-traktor-tarla-mekanikleri.md · 17-fragman-plani.md · LICENSES.md
```

**Güncel durum (14.11):** oynanan görevler ve sırası → `04_Icerik/11-gorev-ve-basarim-listesi.md` ("Oynanabilir durum"); yapılanlar → `05_Uretim/06-yapilacaklar.md` ("14.11 eklenenler"). Tek oyun sahnesi `Harita`, ilk sahne `AnaMenu`.

## Dosyalar

| Dosya | İçerik | Kim okur |
|---|---|---|
| `01_Tasarim/01-oyun-tasarimi.md` | Çerçeve hikâye, harita hiyerarşisi, iki görev tipi, akış | Herkes |
| `01_Tasarim/09-tema-ve-model-kurallari.md` | Görsel dil, renk paleti, font/renk yasakları, "AI işi" görünüme karşı kurallar, referans akışı, poligon/pivot/export/optimizasyon | Harita, model, UI |
| `02_Mekanik/02-siparis-mekanigi.md` | Kazan + terazi mekaniğinin tam kuralları ve kabul kriterleri | Kod, UI |
| `02_Mekanik/03-masa-mekanigi.md` | Çalışma masası (sözel görevler): soru tipleri, hikâye sarmalayıcıları | Kod, UI, içerik |
| `03_Teknik/04-teknik-mimari.md` | Sahneler, veri modeli (ScriptableObject), sınıflar, durum makineleri, kayıt | Kod |
| `04_Icerik/05-icerik-prototip.md` | Prototipteki iki köyün içeriği: 5 sipariş + 5 masa görevi, soru havuzu | İçerik, kod (veri girişi) |
| `04_Icerik/10-gorev-pazar-npc.md` | Görev listesi özeti, element/malzeme tablosu, pazar tezgâhları ve fiyatlar, yan müşteriler, sahne NPC'leri, yol sohbetleri | İçerik, harita, kod |
| `05_Uretim/06-yapilacaklar.md` | Öncelikli görev listesi, roller, zaman planı, "bitti" tanımı | Herkes |
| `03_Teknik/08-mustafa-entegrasyon.md` | Sahne bağlantı noktaları ve UI arayüzleri (harita/UI ↔ kod) | Mustafa, kod |
| `05_Uretim/07-kod-plani.md` | Kod fazları, öncelik ve kesme sırası | Kod |
| `05_Uretim/LICENSES.md` | Hazır varlık lisansları + yapay zekâ beyanı | İçerik/sunum |
| `01_Tasarim/11-ui-ux-plani.md` | Ekran akışı, ekran listesi, UI arayüzleri | UI, kod |
| `02_Mekanik/04-gorev-plani.md` | Seviye seviye görev planı ve eğitsel katmanlar (Tahmin, Hata Dedektifi, Atom Görüşü, Sorgu, Keşif Defteri) | Herkes |
| `04_Icerik/11-gorev-ve-basarim-listesi.md` | Görev kimlikleri (`taskId`), başarımlar, **oynanabilir durum** | Kod, içerik |
| `04_Icerik/12-ders-notu-arastirma.md` | Ders notu içeriğinin kazanım ve kavram yanılgısı araştırması | İçerik |
| `04_Icerik/15-bereket-tarlasi-mat-biyo.md` | Bereket Tarlası (Mat + Biyo) görevleri; `bereket_2` oyunda | İçerik, kod, harita |
| `04_Icerik/17-icerik-kontrol-listesi.md` | Kitapla kontrol edilecek metinler | İçerik |
| `05_Uretim/12-ilk-20-gorev-mekanikleri.md` | İlk 20 görevin mekanik özeti | Kod |
| `05_Uretim/13-ders-notu-plani.md` · `14-defter-plani.md` | Ders Notu ve defter tasarımı/akışı | Kod, UI |
| `05_Uretim/16-traktor-tarla-mekanikleri.md` | Simya Traktörü ve tarla mekanikleri | Kod, harita |
| `05_Uretim/17-fragman-plani.md` | Fragman/sunum planı | Sunum |

## Tek cümlede oyun

**Bilgi Adası**: 10. sınıfın ilk kimya dersinde uyuyakalan öğrenci, gözünü ortaçağda, her şehri bir ders olan bir adada, bir simyacı atölyesinde açar; Câbir Usta'nın yanında kimyanın her konusunu bir köy, her temasını bir ilçe olarak keşfeder — uygulama gereken konuları kazanda **sipariş** olarak, sözel konuları **çalışma masasında** çözer.

## Yarışma kuralları → bizim karşılığımız

| Kural | Karşılık |
|---|---|
| Kurgu, oynanış, arayüz önemli | Uyanma çerçevesi + canlanan köyler; iki görev tipi; okunur terazi/sayaç arayüzü |
| Öğrenmeyi destekleyen oynanabilir prototip | Kavram yanılgısını mekanikle imkânsız kılan kazan; kendini kontrol eden sorular |
| 0'dan üret | Kod ve içerik yarışma süresince yazılır. **Eski projelerden kod/paket/model kopyalanmaz** (kendi paketlerimiz dahil) — organizasyondan izin çıkarsa `06`'da işaretlenir |
| Hazır şeylerde lisans | Her hazır varlık `LICENSES.md` tablosuna yazılır + oyun içi Emeği Geçenler ekranı |
| AI serbest | Kullanılan her AI aracı ve amacı `LICENSES.md` içindeki beyan tablosuna yazılır |
| Uygun içerik | Şiddet, ayrımcılık, gerçek felaket çağrışımı, kumar, gerçek para yok (bkz. `01` §7) |

## Değişmeyen kararlar

- Hiyerarşi: **Şehir = ders**, **İlçe = ünite (tema)**, **Köy = konu**. Örnek: Kimya şehri → Kimya Bilimi ilçesi → Simya köyü.
- Şehirler: Kimya, Matematik, Fizik, Biyoloji, Türkçe, Sosyal.
- Prototipte **tek şehir (Kimya)**, **tek ilçe açık (Etkileşim)**, **iki köy oynanabilir**: *Tepkime Türleri* (masa) ve *Denkleştirme* (sipariş). Geri kalanı haritada kilitli/sisli görünür.
- Her köy = **5 görev**. Köy sonunda köy canlanır.
- Oyuncu 10. sınıf öğrencisidir.
- Hata cezalandırılmaz; kazan köpürür, masa "tekrar dene" der.
