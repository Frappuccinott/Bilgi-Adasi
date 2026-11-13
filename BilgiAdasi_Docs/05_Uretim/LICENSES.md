# Lisanslar ve Yapay Zekâ Beyanı

Kayıtsız hiçbir hazır dosya projeye girmez. Bu tablo oyun içi **Emeği Geçenler** ekranına ve sunumdaki lisans slaytına aktarılır.

## Hazır varlıklar

| Varlık | Tür | Kaynak / yazar | Bağlantı | Lisans | Değişiklik | Ekleyen |
|---|---|---|---|---|---|---|
| Unity | Motor | Unity Technologies | — | Unity Personal | — | — |
| TextMeshPro | Paket | Unity | — | Unity paketi | — | — |
| Universal Render Pipeline | Paket | Unity | — | Unity paketi | — | — |
| Input System | Paket | Unity | — | Unity paketi | — | — |
| Cinemachine 3 | Paket | Unity | — | Unity paketi | — | Mehmet |
| Liberation Sans (TMP Essentials) | Font | Red Hat / Unity TMP | — | SIL OFL 1.1 | — | Mehmet |
| | | | | | | |

**Kurallar**
- Öncelik **CC0**. CC-BY ise yazar adı Emeği Geçenler'e yazılır.
- Freesound, Poly Pizza, Sketchfab gibi sitelerde lisans **dosya dosya** farklıdır; her dosyanın lisans sayfasının ekran görüntüsü `Docs/license-proof/` içine konur.
- "Kişisel kullanım" ya da lisansı yazılmayan şey kullanılmaz. Oyun/film/dizi/anime görüntü ve sesi kullanılmaz.
- Font: SIL Open Font License (ör. Google Fonts); Türkçe karakter desteği kontrol edilir.

## Yapay zekâ kullanım beyanı

| Araç | Ne için | Çıktı nerede | Kontrol eden |
|---|---|---|---|
| Claude | Senaryo güncellemesi, geliştirme dokümanları, kod yardımı | `Docs/`, ilgili scriptler | |
| Claude | Çekirdek sistemler (GameEvents, SaveSystem, SceneFlow, GameManager), kimya hesabı (AtomMath, FormulaFormatter) + birim testleri, girdi asset'i, editör kurulum aracı | `Assets/_Project/Scripts/Core`, `Chemistry`, `Editor`, `Tests` | Mehmet |
| Claude | Oyuncu (PlayerMotor, PlayerLook, HeadBob, PlayerInteractor, PlayerFootsteps), istasyon modu (StationBase), GameInput, ViewLocator, sahne bağlantı noktaları, Faz 2 editör kurulum aracı | `Assets/_Project/Scripts/Player`, `Stations`, `City`, `Core` | Mehmet |
| Claude | Sipariş mekaniği (OrderSession, OrderController, görünüm arayüzleri), Denkleştirme veri üreticisi, ham sipariş paneli, testler | `Assets/_Project/Scripts/Order`, `UI`, `Tests` | Mehmet |
| ChatGPT | Harita konsept görseli | Referans | |
| Claude | Tema ve model kuralları dokümanının uyarlanması (ekibin kendi Simya notlarından), repo kuralları | `BilgiAdasi_Docs/01_Tasarim/09-…`, `03_Teknik/08-…` §6 | Mustafa |
| | | | |

Kimya içeriği AI çıktısı olsa bile ders kitabıyla ekipçe kontrol edilir.
