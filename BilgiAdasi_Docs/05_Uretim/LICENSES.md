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
| MCP for Unity v10.3.0 (`com.coplaydev.unity-mcp`) | Editör aracı (oyuna girmez) | CoplayDev / Aura | https://github.com/CoplayDev/unity-mcp | MIT | — | Mustafa |
| Alegreya Bold (Alegreya-Bold.ttf) | Font | Juan Pablo del Peral, Huerta Tipográfica | https://fonts.google.com/specimen/Alegreya | SIL OFL 1.1 | TMP SDF asset üretildi | Mehmet |
| Alegreya Sans Medium (AlegreyaSans-Medium.ttf) | Font | Juan Pablo del Peral, Huerta Tipográfica | https://fonts.google.com/specimen/Alegreya+Sans | SIL OFL 1.1 (`Assets/_Project/Fonts/OFL.txt`) | TMP SDF asset üretildi | Mehmet |
| UI/Mühür (UI_Muhur.png, UI_MuhurMum.png) | UI görseli | Ekip (Mustafa) — Figma "Bilgi Adası — Marka Kimliği" | — | Ekibin kendi çizimi | Figma MCP ile PNG dışa aktarıldı, arka planı saydamlaştırıldı | Mehmet |
| UI/Akçe (UI_Akce.png) | UI simgesi | Claude ile Figma'da çizildi (marka paleti) | — | Proje çıktısı | Figma MCP ile PNG dışa aktarıldı, arka planı saydamlaştırıldı | Mehmet |
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
| Claude | Köy akışı (NeighborhoodData, NeighborhoodRunner, DialogueDirector, DoorStation), konuşma arayüzü + ham yer tutucusu, geliştirici kısayolları, elle teslim, atölye (PlayerInventory, EconomyData, Shelf/ShelfSlot, MarketStall, MarketCatalog + pazar kuralları `Order/Market`, raf yeri yeşil işareti, senaryo Inspector'ı `Editor/Scenario`, CauldronStove/Drain, CauldronVisuals, ScaleModel, ChalkboardText, WorldInteractable), OrderSession/OrderController yeniden, yer tutucu atölye HUD'u, Faz 4 editör kurulum araçları | `Assets/_Project/Scripts/City`, `UI`, `Editor` | Mehmet |
| Claude | Bisiklet sürüşü (Bicycle, BicycleRig, BicycleSettings), PlayerLook bağlı bakış, küp yer tutucu bisiklet + editör kurulum aracı | `Assets/_Project/Scripts/Player/Bicycle`, `Editor/BicycleSetup.cs` | Mehmet |
| Claude | Başarım sistemi (AchievementTree, AchievementController, ağaç yerleşimi + testleri), Esc menüsü (PauseMenuController), ham bildirim ve ham Esc menüsü/sekmeli ağaç görünümü, editör kurulum aracı ve başarım Inspector'ı (doğrulama + önizleme), görev ↔ başarım listesi (Mustafa'nın görev planından) | `Assets/_Project/Scripts/Core/Achievements`, `Core/PauseMenuController.cs`, `UI/Placeholder`, `Editor/AchievementSetup.cs`, `Editor/AchievementTreeEditor.cs`, `Tests`, `BilgiAdasi_Docs/04_Icerik/11-…` | Mehmet |
| ChatGPT | Harita konsept görseli | Referans | |
| Claude | Simya tezgâhı (imbik, su banyosu, havan, körük), 8 düzenek + 39 eşya iskeleti ve 155 animasyon klibi (Blender scriptleri), Unity sanat içe aktarma aracı (`Art/Editor/SanatIceAktarma.cs`) | `ArtSource/Blender/Script/anim_*.py`, `simya_tezgahi.py`, `Assets/_Project/Art/` | Mustafa |
| Claude | Atölye kabuğu, iç düzeni, eşyalar ve 39 taşınabilir eşya için Blender scriptleri (model üretimi elle yazılmış Python ile; hazır mesh/üretici kullanılmadı), envanter ikonları, araştırma raporları | `ArtSource/Blender/Script/`, `ArtSource/Referans/`, `Assets/_Project/UI/Ikonlar/` | Mustafa |
| Claude | Kasaba haritası: arazi, yollar, dere, doğa (ağaç/çalı/kaya), köy yapıları (evler, fırın, köprü, güvercin kulesi, lonca, çeşme, kilitli kapılar), yön bulma (tabelalar, kemerler, mil taşları, harita masası), kilitli şehir kapıları ve siluetleri Blender scriptleri + araştırma | `ArtSource/Blender/Script/harita*.py`, `ArtSource/Referans/harita.md`, `yapilar.md`, `yon_bulma.md`, `Assets/_Project/Art/Harita/` | Mustafa |
| Claude | Simya traktörü + 6 alet, Bereket Tarlası (24 parsel × 6 durum), Ormancı Korusu modelleri (Blender scripti) ve seviye seviye görev planı (Keşif Defteri, Hata Dedektifi, Ustanın Sorgusu, Atom Görüşü katmanları; Mat/Biyo/Fizik saha soruları) | `ArtSource/Blender/Script/harita_tarla.py`, `BilgiAdasi_Docs/02_Mekanik/04-gorev-plani.md`, `Assets/_Project/Art/Traktor/` | Mustafa |
| Claude | Unity harita yerleştirme aracı (köy grupları, collider, doğa prefab + LOD, araçlar), atölye collider'ları, kapı pivotu düzeltmesi, Unity yürüme/performans testleri | `Assets/_Project/Art/Editor/HaritaYerlestir.cs`, `SanatIceAktarma.cs`, `Assets/_Project/Scenes/Deneme_Mustafa_Harita.unity` | Mustafa |
| Claude | Bisiklet (velosiped) modeli: referans araştırması + Blender scripti (elle yazılmış Python; hazır mesh/üretici kullanılmadı) | `ArtSource/Blender/Script/bisiklet.py`, `ArtSource/Referans/bisiklet.md`, `Assets/_Project/Art/Bisiklet/` | Mustafa |
| Claude | Tema ve model kuralları dokümanının uyarlanması (ekibin kendi Simya notlarından), repo kuralları | `BilgiAdasi_Docs/01_Tasarim/09-…`, `03_Teknik/08-…` §6 | Mustafa |
| Claude | Figma arayüzünün Unity bağlantısı: tema (UiTheme), prosedürel UI kiti, HUD/ipucu/not/sipariş kartı/"Gerçek hayatta"/diyalog/envanter + kese/pazar tezgâhı/Esc (opak ahşap zemin, şu anki görev kartı) + başarı ağacı + Ayarlar/başarım bildirimi view'ları, oyuncu ayarları (GameSettings), şu anki görev verisi (CurrentTaskInfo), Kurulum 14; Figma'da "UI/Başarım bildirimi" bileşeni (3 tür), HUD içi örnek kare ve UI/Akçe simgesi (Figma MCP, marka paleti ve Alegreya ile) | `Assets/_Project/Scripts/UI/Figma`, `Editor/FigmaUiSetup.cs`, Figma: Keşif Defteri ve Başarılar sayfası | Mehmet |
| | | | |

Kimya içeriği AI çıktısı olsa bile ders kitabıyla ekipçe kontrol edilir.
