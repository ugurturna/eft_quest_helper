<p align="center">
  <img src="TarkovQuestTracker/Assets/app-icon.png" width="120" alt="EFT Quest Helper ikonu">
</p>

<h1 align="center">EFT Quest Helper</h1>

<p align="center">
  <b>Escape from Tarkov</b> için masaüstü görev takip ve planlama uygulaması — her zaman üstte duran bir overlay ile.<br>
  <a href="README.md">English</a> · <b>Türkçe</b> · <a href="README.ru.md">Русский</a>
</p>

<p align="center">
  <a href="https://github.com/ugurturna/eft_quest_helper/releases/tag/v0.0.1">
    <img src="https://img.shields.io/badge/%C4%B0ndir-v0.0.1-2ea44f?style=for-the-badge&logo=windows&logoColor=white" alt="İndir v0.0.1">
  </a><br>
  <a href="https://github.com/ugurturna/eft_quest_helper/releases/tag/v0.0.1">https://github.com/ugurturna/eft_quest_helper/releases/tag/v0.0.1</a><br>
  🌐 <b>Web Sitesi:</b> <a href="https://ugurturna.github.io/eft_quest_helper/">https://ugurturna.github.io/eft_quest_helper/</a>
</p>

---

## Ban riski

> [!TIP]
> **Bilinen bir ban riski yok.** EFT Quest Helper normal, ayrı bir Windows uygulamasıdır. Oyunun belleğini **okumaz ve değiştirmez**, oyuna hiçbir şey **enjekte etmez**, oyunun dosyalarını veya loglarını **okumaz**, oyuna tuş/fare girdisi **göndermez** ve oyunun sunucularıyla **konuşmaz**. Overlay, ekranın üstünde tutulan sıradan bir penceredir (yapışkan not gibi); kısayol tuşları Windows'un standart `RegisterHotKey` API'sini kullanır.

> [!CAUTION]
> **Kullanım sorumluluğu sana aittir. Bu proje Battlestate Games ile bağlantılı değildir ve onlar tarafından onaylanmamıştır; BSG kurallarını her an değiştirebilir. BSG bir gün bu tür araçlara izin verilmediğini açıklarsa kullanmayı bırak.**

---

<p align="center">
  <img src="docs/main-window.png" width="780" alt="Ana pencere">
</p>

## Özellikler

- **Tüm görevler tek yerde** — ~870 görev: trader (yan) görevleri, hikaye bölümleri ve event görevleri; hedefler, gerekli itemler, ödüller, rehber ve notlarla birlikte.
- **Ön koşul zincirleri** — her görev neye ihtiyaç duyduğunu ve neyi açtığını bilir.
- **Hedef planlayıcı** — bir hedef seç (ör. *The Punisher – Part 3* ya da *Collector*), tüm zinciri sırayla, şu an yapabildiklerini ve kalanları gör.
- **Kappa / Collector yolu** — ilerleme çubuğu, yapılabilir Kappa görevleri **haritaya göre gruplu** (aynı raidde birkaçını birden yap) ve seviye bekleyen görevler.
- **Aktif görevler + overlay** — şu an yaptığın görevler ekranın sağ kenarındaki küçük takipçide görünür; hedef kutucukları ve öldürme/item sayaçlarıyla.
- **Bir görevi aktif yapmak zincirini tamamlar** — *Punisher Part 5*'i aktif yap, Part 1–4 otomatik tamamlandı sayılsın (tek tıkla geri alınabilir).
- **PvP ve PvE** — her mod için ayrı ilerleme; PvE'ye özel farklar (seviye, hedefler) gösterilir.
- **İngilizce ve Türkçe arama** — yazım hatalarına ve Türkçe karakterlere toleranslı (`punisher 3`, `punişır 3`, `cezalandırıcı`).
- **İki ayrı dil** — uygulama dili (menüler) ve görev dili (görev metinleri) farklı olabilir; ör. uygulama Türkçe, görevler İngilizce. Tüm hikaye bölümleri Türkçeye çevrildi.
- **Profiller** — aynı bilgisayarda birden fazla oyuncu, herkesin kendi ilerlemesi ve ayarları.
- **Event görevleri** — aktif eventler listelenir; geçmiş eventler Ayarlar'dan gösterilebilir.
- **Hızlı** — listeler kaydırdıkça sayfa sayfa yüklenir.

### Overlay

<img src="docs/overlay.png" width="320" align="right" alt="Overlay">

- Sağ kenara yapışık, **sadece yukarı/aşağı hareket eder** (başlıktan sürükle).
- Ayarlanabilir **opaklık**, **boyut** (%60–200, ayrıca <kbd>Ctrl</kbd> + fare tekerleği) ve **genişlik**.
- İki görünüm: **sadece başlık** ya da **başlık + detay**.
- <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>T</kbd> — overlay'i kilitle (tıklamalar oyuna geçer) / aç.
- <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>H</kbd> — overlay'i göster / gizle.
- Oyundan odağı asla çalmaz.

> [!NOTE]
> Tarkov'u **Borderless (Çerçevesiz)** ya da **Windowed (Pencere)** modda çalıştır. Windows, tam ekran (exclusive fullscreen) bir oyunun üstüne başka pencere çizemez.

<br clear="right">

## İndir & çalıştır

**Hazır sürümü indir (önerilen):** [v0.0.1 Sürümü](https://github.com/ugurturna/eft_quest_helper/releases/tag/v0.0.1)

Gereksinimler: **Windows 10/11** ve **[.NET 10 SDK](https://dotnet.microsoft.com/download)** (derlemek için) ya da .NET 10 Desktop Runtime (hazır derlenmiş sürümü çalıştırmak için).

```powershell
git clone git@github.com:ugurturna/eft_quest_helper.git
cd eft_quest_helper
dotnet run --project TarkovQuestTracker
```

Her yere kopyalanabilen bağımsız bir klasör üretmek için:

```powershell
dotnet publish TarkovQuestTracker -c Release -r win-x64 --self-contained true -o publish
# publish\TarkovQuestTracker.exe dosyasını çalıştır
```

## İlerlemem nerede saklanıyor?

Her şey bilgisayarında saklanır, hiçbir yere yüklenmez:

```
%APPDATA%\TarkovQuestTracker\
  app.json                      son kullanılan profil
  profiles\<ad>\settings.json   overlay, diller, kısayollar
  profiles\<ad>\progress_pvp.json
  profiles\<ad>\progress_pve.json
```

İlerleme her tıklamada kaydedilir. Yedeklemek ya da başka bilgisayara taşımak için **Ayarlar → Dışa / İçe aktar**'ı kullan. Verileri başka bir yerde tutmak için (taşınabilir kullanım) `TARKOV_TRACKER_HOME` ortam değişkenini ayarla.

Kısayollar `settings.json` içinden değiştirilebilir (`clickThroughHotkey`, `toggleOverlayHotkey`, ör. `"Ctrl+Alt+F9"`).

## Görev verisi

Görev verisi `data/` klasöründedir ve `tools/scraper/` içindeki scriptlerle üretilir (Python 3.12, sadece standart kütüphane):

| Komut | Ne yapar |
|---|---|
| `python tools/scraper/scrape_wiki.py` | Tüm görevleri [EFT Fandom wiki](https://escapefromtarkov.fandom.com/)'den indirir → `data/tasks_en.json` (`--refresh` yerel önbelleği yok sayar) |
| `python tools/scraper/fetch_tarkovdev.py` | [tarkov.dev](https://tarkov.dev/)'den ek veri indirir (Kappa bayrakları, seviyeler, eksik ön koşullar, Türkçe adlar) |
| `python tools/scraper/merge.py` | tarkov.dev verisini wiki verisine ekler ve iki kaynağın ayrıştığı yerleri `data/diff_report.md`'ye yazar |
| `python tools/scraper/build_translations.py tr` | `tools/translations/tr/` içindeki elle yazılmış çevirileri `data/tasks_tr.json`'a işler |

Her görev, her alanın hangi kaynaktan geldiğini (`sources`) saklar; wiki verisini tarkov.dev verisinden ayırt edebilirsin.

### Çeviri yapmak

Çeviriler `tools/translations/<dil>/` içinde, her görev için ayrı düz JSON dosyalarıdır ve wiki yeniden çekilse de kaybolmaz. Var olan bir dosyayı kopyala (ör. `tools/translations/tr/story/tour.json`), `name`, `objectives[].text`, `objectives[].details` ve `guide` alanlarını çevir, sonra `build_translations.py`'yi çalıştır. Script eksik ya da uyumsuz hedefleri bildirir.

Uygulama (menü) metinleri `TarkovQuestTracker/Strings/Strings.<dil>.xaml` dosyalarındadır.

## Proje yapısı

```
TarkovQuestTracker/          WPF uygulaması (.NET 10): pencereler, overlay, view model'ler, arayüz metinleri
TarkovQuestTracker.Core/     görev modelleri, veri yükleme, arama, ön koşul grafiği, profiller/ilerleme
tests/                       xUnit testleri (planlayıcı, arama, profiller, gerçek veri kontrolleri)
tools/scraper/               veri scriptleri (wiki, tarkov.dev, birleştirme, çeviriler)
tools/translations/          elle yazılmış çeviriler
tools/make_icon.ps1          PNG'den .ico üretir
data/                        uygulamayla birlikte gelen görev verisi
```

Testleri `dotnet test` ile çalıştır.

## Kaynaklar & yasal

- Görev verisi: [Escape from Tarkov Wiki (Fandom)](https://escapefromtarkov.fandom.com/) — içerik [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) lisansıyla — ve [tarkov.dev](https://tarkov.dev/).
- *Escape from Tarkov*, trader adları ve trader portreleri **Battlestate Games**'in markası/mülküdür. Burada sadece oyun içi içeriği tanımlamak için kullanılmıştır. Bu ticari olmayan bir hayran projesidir.
