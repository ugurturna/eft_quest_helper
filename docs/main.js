/* ==========================================================================
   EFT Quest Helper — Landing Page Interactive Scripts & Translations
   ========================================================================== */

const translations = {
  en: {
    announcement: "🚀 {v} is here! Zero ban risk, instant quest overlay for Escape from Tarkov.",
    navFeatures: "Features",
    navSafety: "Ban Safety",
    navGuide: "How to Use",
    navTraders: "Traders",
    navFaq: "FAQ",
    navDownload: "Download {v}",
    
    heroBadge: "Escape from Tarkov Desktop Companion",
    heroTitlePre: "Master Every Quest.",
    heroTitleHighlight: "Escape with Kappa.",
    heroSubtitle: "A blazing-fast, zero-ban-risk quest tracker and goal planner with an always-on-top, click-through in-game overlay. Track objectives live without Alt+Tabbing.",
    btnDownload: "Download for Windows ({v})",
    btnGithub: "View on GitHub",
    metaSafe: "100% Safe (No Memory Hooks)",
    metaWin: "Windows 10 / 11 (64-bit)",
    metaOffline: "Offline & Fully Private",
    overlayTag: "In-Game Overlay",
    zoomHint: "🔍 Click image to enlarge",

    featTag: "Engineered for PMCs",
    featTitle: "Everything You Need to Conquer Tarkov",
    featSub: "Designed from the ground up for seamless raiding, zero distractions, and ultimate progression.",
    
    f1Title: "Live In-Game Overlay",
    f1Desc: "Docked transparently to the side of your screen. Lock it with Ctrl+Shift+T so clicks go directly to the game. Check off items and kill counts mid-raid.",
    f1Badge: "Always-on-top · Click-through",

    f2Title: "Goal Planner & Kappa Path",
    f2Desc: "Pick any target quest, like Punisher Part 6 or Collector. The app works out the exact chain, what you can do right now and what's left. The Kappa path follows Collector's real prerequisite chain.",
    f2Badge: "Chain solver · Map grouping",

    f3Title: "Auto-Chain Completion",
    f3Desc: "Mark Punisher Part 5 as active, and Parts 1–4 are automatically marked completed. Made a mistake? Undo in a single click.",
    f3Badge: "Smart prerequisites",

    f4Title: "PvP and PvE Modes",
    f4Desc: "Separate, isolated progress tracking for PvP and PvE raids. PvE-specific requirements and level discrepancies are built-in.",
    f4Badge: "Independent saves",

    f5Title: "Fuzzy Multilingual Search",
    f5Desc: "Search in English or Turkish. Tolerant of typos, alternate trader names, and accented characters (e.g., 'cezalandırıcı', 'punisher 3').",
    f5Badge: "Instant search",

    f6Title: "100% Local & Multi-Profile",
    f6Desc: "Your data never leaves your computer. Multiple profiles allow several players or accounts on the same PC to track their own progress.",
    f6Badge: "Zero telemetry · Portable",

    safeTag: "Fair Play & Security",
    safeTitle: "Is EFT Quest Helper Safe? (Ban Risk Explained)",
    safeSub: "We take account security very seriously. Here is exactly how EFT Quest Helper works under the hood.",
    safeDoesNotTitle: "What It DOES NOT Do",
    safeDoesNot1: "Does NOT inject DLLs or hook game memory.",
    safeDoesNot2: "Does NOT read or modify game files or client logs.",
    safeDoesNot3: "Does NOT send simulated keystrokes or mouse clicks to the game.",
    safeDoesNot4: "Does NOT intercept or inspect network traffic or talk to BSG servers.",
    safeDoesTitle: "What It DOES Do",
    safeDoes1: "Runs as a completely separate, standard Windows WPF application.",
    safeDoes2: "Overlay is a standard transparent top-level Windows window (like a sticky note).",
    safeDoes3: "Uses standard Windows RegisterHotKey API for hotkeys.",
    safeDoes4: "Stores all progress locally in the data folder next to the app.",
    safeDisclaimer: "Notice: EFT Quest Helper is an independent fan project not affiliated with Battlestate Games. Always play Tarkov in Borderless or Windowed mode to allow Windows to render overlays.",
    vtTitle: "🛡️ VIRUSTOTAL SCAN",
    vtMeta: "{v} · scanned {d}",
    vtResult: "{n} / {t} engines flagged it →",
    vtNote: "Every release is uploaded to VirusTotal automatically when it is built. You can compare the SHA-256 with the file you downloaded.",

    guideTag: "Quick Start",
    guideTitle: "Get Started in 4 Easy Steps",
    guideSub: "No complex configuration or setup required. Up and running in less than 60 seconds.",
    step1Title: "1. Download & Launch",
    step1Desc: "Download the latest release zip from GitHub, extract it anywhere and run TarkovQuestTracker.exe. No installation needed.",
    step2Title: "2. Set Game to Borderless",
    step2Desc: "In Escape from Tarkov display settings, select Borderless or Windowed mode (Windows cannot overlay exclusive fullscreen).",
    step3Title: "3. Choose Your Quests",
    step3Desc: "Mark your completed quests or pick a goal. Active quests automatically appear on the in-game overlay tracker.",
    step4Title: "4. Lock & Raid",
    step4Desc: "Press Ctrl+Shift+T to lock overlay click-through, launch into your raid, and track objectives in real-time!",

    hotkeysTitle: "Keyboard Shortcuts",
    hk1Desc: "Lock / unlock overlay click-through",
    hk2Desc: "Show / hide overlay completely",
    hk3Desc: "Scale overlay size (60% to 200%)",
    hk4Desc: "Reposition overlay vertically on screen",

    tradersTag: "Comprehensive Database",
    tradersTitle: "All 12 Traders & Story Chapters Included",
    tradersSub: "Over 870+ quests updated with objectives, keys, guides, and rewards.",

    faqTag: "Got Questions?",
    faqTitle: "Frequently Asked Questions",
    faq1Q: "Do I need to install .NET to run the release?",
    faq1A: "The standalone release build has .NET embedded, so you can just run the executable directly on any modern Windows 10/11 64-bit PC.",
    faq2Q: "Why does the overlay disappear when I launch Tarkov?",
    faq2A: "Make sure Escape from Tarkov is set to 'Borderless' or 'Windowed' mode in the graphics settings. Windows does not allow any external window to render over an exclusive fullscreen game.",
    faq3Q: "Does the app automatically detect in-raid progress?",
    faq3A: "No, EFT Quest Helper does not read game memory or network packets to ensure 100% ban safety. You check off objectives manually or between raids.",
    faq4Q: "Can I transfer my progress to another computer?",
    faq4A: "Yes! Open Settings in the app and click 'Export'. You can import the resulting file on any PC, or copy the data\\profiles folder next to TarkovQuestTracker.exe.",

    ctaTitle: "Ready to Conquer Your Tarkov Wipe?",
    ctaSub: "Download EFT Quest Helper today. Free, open source, and built for PMCs.",
    ctaBtn: "Download {v} for Windows",
    
    navShots: "Screenshots",
    newTag: "New",
    navEndgame: "End Game",
    f9Title: "End Game: The Ticket Endings",
    f9Desc: "Which of the 4 endings can you still reach, which way do you go, and where are you right now? Mark your decisions, pick a target ending and follow the route step by step.",
    f9Badge: "4 endings · Route planner →",
    tabEndgame: "End Game",
    capEndgame: "The End Game guide for The Ticket: the 4 endings with their achievement icons, your decisions, and the route to your target ending with \"You are here\".",
    tabFlea: "Flea Market",
    capFlea: "Ctrl+Shift+F in game: flea prices for your mode (PvP left, PvE right) after 3 letters, with price per slot, 24 h average, 48 h trend and the best trader. Esc returns to the game.",
    f10Title: "Flea Market Prices In Game",
    f10Desc: "Press Ctrl+Shift+F mid-raid: a price search opens in the middle of the screen. Type 3 letters and see the flea price for your mode (PvP or PvE), price per slot and the best trader. Esc takes you back to the game.",
    f10Badge: "PvP & PvE prices · tarkov.dev",
    egTag: "End Game · The Ticket",
    egTitle: "Plan Your Ending in The Ticket",
    egSub: "The Ticket has 4 endings and 3 point-of-no-return decisions. The End Game guide shows which endings you can still reach, which way to go and where you are right now.",
    egSurvivor: "Refuse Kerman, pay Prapor and finish his tasks.",
    egSavior: "Bring Kerman every major TerraGroup evidence, then work with Fence and the BTR driver.",
    egDebtor: "Give Kerman 2 major evidence, then turn away and work for Lightkeeper.",
    egFallen: "Refuse to dig up evidence for Kerman and side with Prapor.",
    eg1Title: "1. Open End Game",
    eg1Desc: "Open the End Game tab, or make The Ticket a goal in the Goal Planner: the whole guide appears right under its quest chain.",
    eg2Title: "2. Mark Your Decisions",
    eg2Desc: "Click the choices you already made in game: the armored case, working with Kerman, the evidence. Every option shows which endings it keeps open. Click again to undo.",
    eg3Title: "3. Pick a Target Ending",
    eg3Desc: "Press \"Make target\" on an ending. The decisions ahead get a \"Pick this for your target\" mark, and you are warned if your choices already closed that ending.",
    eg4Title: "4. Tick Off the Route",
    eg4Desc: "Every step, achievement (Easy Way, U Turn, Will It Blow, Little Triumph…) and the escape rules are listed in order. Tick what you did; \"You are here\" marks the next step.",
    egNote: "PvP and PvE objectives differ (e.g. Co-Op extracts vs. 5 PMC kills without killing Scavs) and the guide shows the ones for your mode. Choices are saved per profile and mode. Based on the community \"Story Endings\" flowchart v1.7.6 by Callsign_Smokey / Kay08x.",
    f7Title: "Quest Items Tracker",
    f7Desc: "See which trader needs which items for which quest, grouped by trader, with item icons, counts and Found-in-Raid flags. Search an item to find every quest that wants it.",
    f7Badge: "FIR flags · Item search",
    f8Title: "Item Icons in Every Objective",
    f8Desc: "Find and hand-over objectives show the item's icon in quest details, the active quest list and the overlay, so you recognise loot at a glance mid-raid.",
    f8Badge: "Overlay ready",
    shotsTag: "Screenshots",
    shotsTitle: "See It in Action",
    shotsSub: "Real screenshots from the app. No mockups.",
    tabQuests: "Quests",
    tabItems: "Items",
    tabKappa: "Kappa",
    tabPlanner: "Goal Planner",
    capQuests: "Search every quest, filter by type, status and trader. The detail panel shows objectives with item icons, required items, prerequisites and rewards.",
    capItems: "Every item each trader wants, quest by quest, with counts and FIR flags. Hide completed quests or search for a single item.",
    capKappa: "Your Kappa progress and what you can do right now, grouped by map so you can stack quests in the same raid.",
    capPlanner: "Pick a goal and get the full chain in order, with what is available right now.",
    faq5Q: "How do I update to a new version without losing progress?",
    faq5A: "Your progress lives in the data\\profiles folder next to the exe. Extract the new release to a new folder and copy data\\profiles over, or use Export in the old version and Import in the new one.",
    footerDisclaimer: "Escape from Tarkov, trader names and logos are trademarks of Battlestate Games. This application is an unofficial, non-commercial fan-made tool."
  },

  tr: {
    announcement: "🚀 {v} yayında! Sıfır ban riski, Escape from Tarkov için anlık görev overlay'i.",
    navFeatures: "Özellikler",
    navSafety: "Ban Güvenliği",
    navGuide: "Nasıl Kullanılır",
    navTraders: "Traderlar",
    navFaq: "SSS",
    navDownload: "{v} İndir",
    
    heroBadge: "Escape from Tarkov Masaüstü Yardımcısı",
    heroTitlePre: "Tüm Görevleri Yönet.",
    heroTitleHighlight: "Kappa'ya Hızla Ulaş.",
    heroSubtitle: "Her zaman üstte duran, tıklama geçiren (click-through) oyun içi overlay'e ve zincirleme hedef planlayıcısına sahip, sıfır ban riskli masaüstü görev yardımcısı.",
    btnDownload: "Windows İçin {v} İndir",
    btnGithub: "GitHub'da İncele",
    metaSafe: "%100 Güvenli (Bellek Kancası Yok)",
    metaWin: "Windows 10 / 11 (64-bit)",
    metaOffline: "Çevrimdışı ve Gizli",
    overlayTag: "Oyun İçi Overlay",
    zoomHint: "🔍 Büyütmek için görsele tıklayın",

    featTag: "PMC'ler İçin Geliştirildi",
    featTitle: "Tarkov'u Fethetmek İçin İhtiyacın Olan Her Şey",
    featSub: "Oyun sırasında dikkat dağıtmayan, akıcı ve pratik bir raid deneyimi için tasarlandı.",
    
    f1Title: "Canlı Oyun İçi Overlay",
    f1Desc: "Ekranın sağına yapışık, şeffaf takipçi. Ctrl+Shift+T ile kilitleyin, tıklamalar doğrudan oyuna geçsin. Raid ortasında hedefleri işaretleyin.",
    f1Badge: "Her zaman üstte · Tıklama geçirir",

    f2Title: "Hedef Planlayıcı & Kappa Yolu",
    f2Desc: "İstediğin hedefi seç (ör. Punisher Part 6 veya Collector). Uygulama tüm zinciri, şu an yapabileceklerini ve kalanları listeler. Kappa yolu, Collector'ın gerçek ön koşul zincirini izler.",
    f2Badge: "Zincir çözücü · Harita gruplama",

    f3Title: "Otomatik Zincir Tamamlama",
    f3Desc: "Punisher Part 5'i aktif yap, Part 1–4 otomatik tamamlandı sayılsın. Yanlış mı işaretledin? Tek tıkla geri al.",
    f3Badge: "Akıllı ön koşul mantığı",

    f4Title: "PvP ve PvE Modları",
    f4Desc: "PvP ve PvE için tamamen ayrı ilerleme kaydı. PvE'ye özgü seviye ve görev farkları hazır olarak gelir.",
    f4Badge: "Bağımsız kayıtlar",

    f5Title: "Türkçe & İngilizce Akıllı Arama",
    f5Desc: "Yazım hatalarına, Türkçe karakterlere ve alternatif trader adlarına toleranslı anlık arama ('cezalandırıcı', 'punisher 3', 'punişır').",
    f5Badge: "Anında sonuç",

    f6Title: "Tamamen Yerel & Çoklu Profil",
    f6Desc: "Verileriniz asla bilgisayarınızdan dışarı çıkmaz. Tek bilgisayarda birden fazla profil desteği sunar.",
    f6Badge: "Sıfır telemetri · Taşınabilir",

    safeTag: "Güvenlik & Adil Oyun",
    safeTitle: "EFT Quest Helper Güvenli mi? (Ban Riski Analizi)",
    safeSub: "Hesap güvenliği en büyük önceliğimizdir. Uygulamanın nasıl çalıştığını şeffafça açıklıyoruz.",
    safeDoesNotTitle: "KESİNLİKLE YAPMADIKLARI",
    safeDoesNot1: "Oyunun belleğine (memory) bağlanmaz, DLL enjekte etmez.",
    safeDoesNot2: "Oyun dosyalarını veya loglarını okumaz ve değiştirmez.",
    safeDoesNot3: "Oyuna yapay tuş veya fare komutları göndermez.",
    safeDoesNot4: "Ağ trafiğini izlemez, BSG sunucularına bağlanmaz.",
    safeDoesTitle: "NASIL ÇALIŞIR?",
    safeDoes1: "Tamamen bağımsız, standart bir Windows masaüstü uygulamasıdır.",
    safeDoes2: "Overlay, ekranda tutulan standart şeffaf bir Windows penceresidir.",
    safeDoes3: "Kısayol tuşları için standart Windows RegisterHotKey API'sini kullanır.",
    safeDoes4: "Tüm ilerlemenizi uygulamanın yanındaki data klasöründe, yerel olarak saklar.",
    safeDisclaimer: "Bilgilendirme: EFT Quest Helper, Battlestate Games ile bağı olmayan bağımsız bir hayran projesidir. Windows'un pencere çizebilmesi için Tarkov'u Çerçevesiz (Borderless) modda oynayın.",
    vtTitle: "🛡️ VIRUSTOTAL TARAMASI",
    vtMeta: "{v} · {d} tarihinde tarandı",
    vtResult: "{t} motordan {n} tanesi işaretledi →",
    vtNote: "Her sürüm derlendiği anda otomatik olarak VirusTotal'a yüklenir. SHA-256 değerini indirdiğiniz dosyayla karşılaştırabilirsiniz.",

    guideTag: "Hızlı Başlangıç",
    guideTitle: "4 Kolay Adımda Kullanmaya Başla",
    guideSub: "Karmaşık ayarlar veya yükleme süreçleri yok. 60 saniyede hazır.",
    step1Title: "1. İndir ve Çalıştır",
    step1Desc: "GitHub'dan son sürümün zip'ini indir, istediğin yere çıkar ve TarkovQuestTracker.exe'yi aç. Kurulum gerektirmez.",
    step2Title: "2. Tarkov'u Çerçevesiz Yap",
    step2Desc: "Escape from Tarkov grafik ayarlarında ekran modunu Borderless (Çerçevesiz) veya Windowed olarak seç.",
    step3Title: "3. Görevlerini Seç",
    step3Desc: "Tamamladığın görevleri işaretle veya hedefini seç. Aktif görevlerin oyun içi overlay'de hemen belirsin.",
    step4Title: "4. Kilitle ve Raide Gir",
    step4Desc: "Ctrl+Shift+T tuşlarına basarak overlay'i kilitle (tıklamalar oyuna geçsin) ve hedeflerini canlı canlı takip et!",

    hotkeysTitle: "Klavye Kısayolları",
    hk1Desc: "Overlay tıklama geçirmeyi kilitle / aç",
    hk2Desc: "Overlay'i tamamen göster / gizle",
    hk3Desc: "Overlay boyutunu ölçekle (%60 - %200)",
    hk4Desc: "Overlay'i dikeyde yukarı/aşağı taşı",

    tradersTag: "Kapsamlı Veritabanı",
    tradersTitle: "12 Trader & Hikaye Bölümleri Dahil",
    tradersSub: "870'den fazla görev; hedefleri, anahtarları, rehberleri ve ödülleriyle birlikte hazır.",

    faqTag: "Sorularınız mı Var?",
    faqTitle: "Sıkça Sorulan Sorular",
    faq1Q: "Çalıştırmak için .NET yüklemem gerekiyor mu?",
    faq1A: "Hazır release sürümünde .NET gömülü geldiği için ekstra hiçbir şey kurmadan Windows 10/11 üzerinde doğrudan açabilirsiniz.",
    faq2Q: "Tarkov'u açınca overlay neden kayboluyor?",
    faq2A: "Tarkov grafik ayarlarından ekran modunu 'Borderless' veya 'Windowed' yaptığınızdan emin olun. Windows, tam ekran oyunların üzerine pencere çizemez.",
    faq3Q: "Raiddeki ilerlememi otomatik algılar mı?",
    faq3A: "%100 ban güvenliğini korumak için oyun belleği veya paketleri okunmaz; hedefleri raid içinde veya sonrasında kutucuklara tıklayarak işaretlersiniz.",
    faq4Q: "İlerlememi başka bir bilgisayara aktarabilir miyim?",
    faq4A: "Evet! Ayarlar'dan 'Dışa aktar' ile dosyanızı alıp başka bilgisayarda içe aktarabilir ya da exe'nin yanındaki data\\profiles klasörünü kopyalayabilirsiniz.",

    ctaTitle: "Tarkov Wipe'ını Fethetmeye Hazır mısın?",
    ctaSub: "EFT Quest Helper'ı hemen ücretsiz indir. Açık kaynak kodlu ve oyuncular için üretildi.",
    ctaBtn: "Windows İçin {v} İndir",
    
    navShots: "Ekran Görüntüleri",
    newTag: "Yeni",
    navEndgame: "End Game",
    f9Title: "End Game: Bilet (The Ticket) Sonları",
    f9Desc: "4 sondan hangilerine hâlâ ulaşabilirsin, hangi yoldan gitmelisin, şu an neredesin? Kararlarını işaretle, hedef sonunu seç ve yolu adım adım takip et.",
    f9Badge: "4 son · Yol planlayıcı →",
    tabEndgame: "End Game",
    capEndgame: "The Ticket için End Game rehberi: başarım ikonlarıyla 4 son, verdiğin kararlar ve \"Şu an buradasın\" işaretiyle hedef sonuna giden yol.",
    tabFlea: "Flea Market",
    capFlea: "Oyunda Ctrl+Shift+F: 3 harf yaz, modundaki flea fiyatlarını gör (solda PvP, sağda PvE); slot başı fiyat, 24s ortalama, 48 saatlik değişim ve en iyi trader. Esc oyuna döndürür.",
    f10Title: "Oyun İçinde Flea Fiyatları",
    f10Desc: "Raid ortasında Ctrl+Shift+F: ekranın ortasında fiyat araması açılır. 3 harf yaz, modundaki (PvP ya da PvE) flea fiyatını, slot başı fiyatı ve en iyi trader'ı gör. Esc seni oyuna döndürür.",
    f10Badge: "PvP & PvE fiyatları · tarkov.dev",
    egTag: "End Game · The Ticket",
    egTitle: "The Ticket'ta Sonunu Planla",
    egSub: "The Ticket'ın 4 sonu ve geri dönüşü olmayan 3 kararı var. End Game rehberi hangi sonlara hâlâ ulaşabileceğini, hangi yoldan gitmen gerektiğini ve şu an nerede olduğunu gösterir.",
    egSurvivor: "Kerman'ı reddet, Prapor'a para öde ve onun görevlerini bitir.",
    egSavior: "Kerman'a tüm büyük TerraGroup kanıtlarını getir, sonra Fence ve BTR sürücüsüyle çalış.",
    egDebtor: "Kerman'a 2 büyük kanıt ver, sonra ondan dönüp Lightkeeper için çalış.",
    egFallen: "Kerman için kanıt aramayı reddet ve Prapor'un tarafına geç.",
    eg1Title: "1. End Game'i Aç",
    eg1Desc: "End Game sekmesini aç ya da Hedef Planlayıcı'da The Ticket'ı hedef yap: rehberin tamamı görev zincirinin hemen altında çıkar.",
    eg2Title: "2. Kararlarını İşaretle",
    eg2Desc: "Oyunda verdiğin kararlara tıkla: zırhlı çanta, Kerman'la çalışmak, kanıtlar. Her seçenek hangi sonları açık tuttuğunu gösterir. Geri almak için tekrar tıkla.",
    eg3Title: "3. Hedef Sonunu Seç",
    eg3Desc: "Bir sonda \"Hedef yap\"a bas. Önündeki kararlarda \"Hedefin için bunu seç\" işareti çıkar; kararların o sonu zaten kapattıysa uyarılırsın.",
    eg4Title: "4. Yolu Adım Adım Bitir",
    eg4Desc: "Her adım, başarım (Easy Way, U Turn, Will It Blow, Little Triumph…) ve kaçış kuralları sırayla listelenir. Yaptıklarını işaretle; \"Şu an buradasın\" sıradaki adımı gösterir.",
    egNote: "PvP ve PvE hedefleri farklıdır (ör. Co-Op çıkışları yerine Scav öldürmeden 5 PMC) ve rehber senin modununkini gösterir. Seçimler profil ve mod başına kaydedilir. Topluluğun \"Story Endings\" akış şeması v1.7.6'ya (Callsign_Smokey / Kay08x) dayanır.",
    f7Title: "Görev Eşyaları Takibi",
    f7Desc: "Hangi trader'ın hangi görev için hangi item'lara ihtiyacı olduğunu ikonları, adetleri ve FIR işaretleriyle trader bazında gör. Bir item'ı arat, onu isteyen tüm görevleri bul.",
    f7Badge: "FIR işareti · Item arama",
    f8Title: "Her Hedefte Item İkonu",
    f8Desc: "Bulma ve teslim hedefleri; görev detayında, aktif görevlerde ve overlay'de item'ın ikonunu gösterir. Raid ortasında loot'u bir bakışta tanırsın.",
    f8Badge: "Overlay'de de var",
    shotsTag: "Ekran Görüntüleri",
    shotsTitle: "Uygulamayı Yakından Gör",
    shotsSub: "Uygulamadan alınmış gerçek ekran görüntüleri.",
    tabQuests: "Görevler",
    tabItems: "Eşyalar",
    tabKappa: "Kappa",
    tabPlanner: "Hedef Planlayıcı",
    capQuests: "Tüm görevlerde ara; tür, durum ve trader'a göre filtrele. Detay panelinde item ikonlu hedefler, gerekli item'lar, ön koşullar ve ödüller.",
    capItems: "Her trader'ın görev görev istediği tüm item'lar; adet ve FIR işaretiyle. Bitenleri gizle ya da tek bir item'ı arat.",
    capKappa: "Kappa ilerlemen ve şu an yapabileceklerin; aynı raid'de birleştirebilmen için haritaya göre gruplu.",
    capPlanner: "Bir hedef seç; tüm zinciri sırasıyla ve şu an yapılabilenleri gör.",
    faq5Q: "İlerlememi kaybetmeden yeni sürüme nasıl geçerim?",
    faq5A: "İlerlemeniz exe'nin yanındaki data\\profiles klasöründedir. Yeni sürümü ayrı bir klasöre çıkarıp data\\profiles'ı oraya kopyalayın ya da eski sürümde Dışa aktar, yenisinde İçe aktar kullanın.",
    footerDisclaimer: "Escape from Tarkov, trader isimleri ve logoları Battlestate Games'in tescilli markalarıdır. Bu uygulama bağımsız, ticari olmayan bir hayran projesidir."
  },

  ru: {
    announcement: "🚀 Доступна версия {v}! Без риска бана, оверлей для Escape from Tarkov.",
    navFeatures: "Функции",
    navSafety: "Безопасность",
    navGuide: "Инструкция",
    navTraders: "Торговцы",
    navFaq: "FAQ",
    navDownload: "Скачать {v}",
    
    heroBadge: "Десктопный помощник для Escape from Tarkov",
    heroTitlePre: "Все квесты под контролем.",
    heroTitleHighlight: "Путь к Каппе открыт.",
    heroSubtitle: "Быстрый трекер квестов и планировщик без риска бана с оверлеем поверх экрана и сквозными кликами. Отслеживайте цели прямо в рейде.",
    btnDownload: "Скачать для Windows ({v})",
    btnGithub: "Открыть на GitHub",
    metaSafe: "100% безопасно (без вмешательства в память)",
    metaWin: "Windows 10 / 11 (64-бит)",
    metaOffline: "Офлайн и конфиденциально",
    overlayTag: "Игровой оверлей",
    zoomHint: "🔍 Нажмите для увеличения",

    featTag: "Создано для ЧВК",
    featTitle: "Всё необходимое для рейдов",
    featSub: "Спроектировано для комфортной игры без лишних отвлечений.",
    
    f1Title: "Живой игровой оверлей",
    f1Desc: "Удобно расположен у края экрана. Заблокируйте по Ctrl+Shift+T — клики будут проходить сквозь него прямо в игру.",
    f1Badge: "Поверх всех окон · Сквозные клики",

    f2Title: "Планировщик целей и Каппы",
    f2Desc: "Выберите желаемый квест (например Каратель 6 или Коллекционер). Приложение рассчитает всю цепочку и доступные шаги. Путь к Каппе следует реальной цепочке требований Коллекционера.",
    f2Badge: "Цепочки квестов · Группировка по картам",

    f3Title: "Авто-закрытие требований",
    f3Desc: "Активируйте Каратель Часть 5, и части 1–4 автоматически отметятся выполненными. Отмена в один клик.",
    f3Badge: "Умные пререквизиты",

    f4Title: "Режимы PvP и PvE",
    f4Desc: "Полностью раздельный прогресс для PvP и PvE с учетом всех отличий PvE по уровням и целям.",
    f4Badge: "Раздельные сохранения",

    f5Title: "Умный поиск",
    f5Desc: "Устойчив к опечаткам, альтернативным названиям торговцев и языковым символам.",
    f5Badge: "Мгновенный поиск",

    f6Title: "Локально и мультипрофильно",
    f6Desc: "Данные не покидают ваш ПК. Поддержка нескольких профилей на одном компьютере.",
    f6Badge: "Без телеметрии · Портативно",

    safeTag: "Честная игра и безопасность",
    safeTitle: "Безопасен ли EFT Quest Helper? (Анализ риска бана)",
    safeSub: "Безопасность вашего аккаунта — главный приоритет. Вот как устроен помощник:",
    safeDoesNotTitle: "ЧЕГО ОН НЕ ДЕЛАЕТ",
    safeDoesNot1: "НЕ читает и не изменяет оперативную память игры, не внедряет DLL.",
    safeDoesNot2: "НЕ читает и не модифицирует файлы игры или логи клиента.",
    safeDoesNot3: "НЕ отправляет в игру виртуальные нажатия клавиш или мыши.",
    safeDoesNot4: "НЕ перехватывает сеть и не связывается с серверами BSG.",
    safeDoesTitle: "КАК ОН РАБОТАЕТ",
    safeDoes1: "Является отдельным стандартным приложением Windows (WPF).",
    safeDoes2: "Оверлей — обычное прозрачное верхнее окно Windows (как стикер).",
    safeDoes3: "Использует официальный Windows API RegisterHotKey для горячих клавиш.",
    safeDoes4: "Хранит весь прогресс локально в папке data рядом с приложением.",
    safeDisclaimer: "Примечание: EFT Quest Helper — независимый фанатский проект, не связанный с Battlestate Games. Запускайте Tarkov в режиме 'Без рамки' (Borderless).",
    vtTitle: "🛡️ ПРОВЕРКА VIRUSTOTAL",
    vtMeta: "{v} · проверено {d}",
    vtResult: "Сработало {n} из {t} антивирусов →",
    vtNote: "Каждый релиз автоматически загружается в VirusTotal сразу после сборки. Сравните SHA-256 со скачанным файлом.",

    guideTag: "Быстрый старт",
    guideTitle: "Начните за 4 простых шага",
    guideSub: "Без сложной настройки и установки. Готов к работе за минуту.",
    step1Title: "1. Скачайте и запустите",
    step1Desc: "Скачайте zip последнего релиза с GitHub, распакуйте и запустите TarkovQuestTracker.exe. Установка не требуется.",
    step2Title: "2. Выберите режим 'Без рамки'",
    step2Desc: "В настройках графики Tarkov переключитесь в 'Borderless' или 'В окне' (Windows не рисует окна поверх полноэкранного режима).",
    step3Title: "3. Выберите квесты",
    step3Desc: "Отметьте выполненные задания или задайте цель. Активные задания сразу отобразятся в оверлее.",
    step4Title: "4. Заблокируйте и в рейд",
    step4Desc: "Нажмите Ctrl+Shift+T, чтобы заблокировать оверлей для сквозных кликов, и следите за целями прямо в рейде!",

    hotkeysTitle: "Горячие клавиши",
    hk1Desc: "Включить / отключить сквозные клики оверлея",
    hk2Desc: "Показать / скрыть оверлей целиком",
    hk3Desc: "Масштабировать оверлей (от 60% до 200%)",
    hk4Desc: "Переместить оверлей вверх/вниз по краю",

    tradersTag: "База данных",
    tradersTitle: "Все 12 торговцев и сюжетные главы",
    tradersSub: "Более 870 заданий с целями, ключами, гайдами и наградами.",

    faqTag: "Вопросы и ответы",
    faqTitle: "Часто задаваемые вопросы",
    faq1Q: "Нужно ли устанавливать .NET?",
    faq1A: "Готовая сборка релиза включает все необходимые компоненты, приложение запускается сразу на Windows 10/11 x64.",
    faq2Q: "Почему оверлей скрывается при запуске игры?",
    faq2A: "Убедитесь, что в Tarkov выбран режим 'Без рамки' (Borderless) или 'В окне'. Эксклюзивный полноэкранный режим блокирует сторонние окна.",
    faq3Q: "Отслеживаются ли цели автоматически в рейде?",
    faq3A: "Нет, ради 100% гарантии от бана приложение не считывает память игры. Вы отмечаете галочки вручную.",
    faq4Q: "Можно ли перенести прогресс на другой ПК?",
    faq4A: "Да, используйте 'Экспорт' в настройках приложения или скопируйте папку data\\profiles рядом с TarkovQuestTracker.exe.",

    ctaTitle: "Готовы покорять вайп Tarkov?",
    ctaSub: "Скачайте EFT Quest Helper прямо сейчас. Бесплатно и с открытым исходным кодом.",
    ctaBtn: "Скачать {v} для Windows",
    
    navShots: "Скриншоты",
    newTag: "Новое",
    navEndgame: "End Game",
    f9Title: "End Game: концовки «Билета»",
    f9Desc: "Какие из 4 концовок ещё доступны, каким путём идти и где вы сейчас? Отметьте свои решения, выберите целевую концовку и проходите маршрут шаг за шагом.",
    f9Badge: "4 концовки · Планировщик маршрута →",
    tabEndgame: "End Game",
    capEndgame: "Гид End Game для «Билета» (The Ticket): 4 концовки с иконками достижений, ваши решения и маршрут к целевой концовке с отметкой «Вы здесь».",
    tabFlea: "Барахолка",
    capFlea: "Ctrl+Shift+F в игре: цены барахолки для вашего режима (слева PvP, справа PvE) после 3 букв, с ценой за слот, средним за 24 ч, изменением за 48 ч и лучшим торговцем. Esc возвращает в игру.",
    f10Title: "Цены барахолки в игре",
    f10Desc: "Нажмите Ctrl+Shift+F посреди рейда: поиск цен откроется в центре экрана. Введите 3 буквы и увидите цену на барахолке для вашего режима (PvP или PvE), цену за слот и лучшего торговца. Esc возвращает в игру.",
    f10Badge: "Цены PvP и PvE · tarkov.dev",
    egTag: "End Game · The Ticket",
    egTitle: "Спланируйте свою концовку в «Билете»",
    egSub: "У «Билета» 4 концовки и 3 решения без возврата. Гид End Game показывает, какие концовки ещё доступны, каким путём идти и где вы сейчас.",
    egSurvivor: "Откажите Керману, заплатите Прапору и выполните его задания.",
    egSavior: "Принесите Керману все главные улики TerraGroup, затем работайте с Скупщиком и водителем БТР.",
    egDebtor: "Отдайте Керману 2 главные улики, затем отвернитесь от него и работайте на Смотрителя.",
    egFallen: "Откажитесь искать улики для Кермана и встаньте на сторону Прапора.",
    eg1Title: "1. Откройте End Game",
    eg1Desc: "Откройте вкладку End Game или сделайте «Билет» целью в планировщике: весь гид появится прямо под цепочкой квестов.",
    eg2Title: "2. Отметьте решения",
    eg2Desc: "Нажмите на решения, которые вы уже приняли в игре: бронекейс, работа с Керманом, улики. Каждый вариант показывает, какие концовки он оставляет открытыми. Нажмите ещё раз, чтобы отменить.",
    eg3Title: "3. Выберите цель",
    eg3Desc: "Нажмите «Make target» на концовке. У решений впереди появится отметка «выберите это для цели», а если ваши решения уже закрыли концовку — вы увидите предупреждение.",
    eg4Title: "4. Проходите маршрут",
    eg4Desc: "Каждый шаг, достижение (Easy Way, U Turn, Will It Blow, Little Triumph…) и правила побега перечислены по порядку. Отмечайте сделанное; «Вы здесь» показывает следующий шаг.",
    egNote: "Задачи в PvP и PvE отличаются (например, Co-Op выходы против 5 убийств ЧВК без убийства Диких), и гид показывает задачи для вашего режима. Решения сохраняются для каждого профиля и режима. Основано на схеме «Story Endings» v1.7.6 от Callsign_Smokey / Kay08x.",
    f7Title: "Предметы для квестов",
    f7Desc: "Какие предметы нужны каждому торговцу и для каких квестов: с иконками, количеством и пометкой «найдено в рейде». Поиск по предмету покажет все квесты, где он нужен.",
    f7Badge: "FIR · Поиск предметов",
    f8Title: "Иконки предметов в целях",
    f8Desc: "Цели «найти» и «передать» показывают иконку предмета в деталях квеста, списке активных квестов и оверлее.",
    f8Badge: "Работает в оверлее",
    shotsTag: "Скриншоты",
    shotsTitle: "Посмотрите в деле",
    shotsSub: "Настоящие скриншоты приложения.",
    tabQuests: "Квесты",
    tabItems: "Предметы",
    tabKappa: "Каппа",
    tabPlanner: "Планировщик",
    capQuests: "Поиск по всем квестам, фильтры по типу, статусу и торговцу. В деталях — цели с иконками предметов, нужные предметы, требования и награды.",
    capItems: "Все предметы, которые нужны каждому торговцу, по квестам — с количеством и пометкой FIR.",
    capKappa: "Прогресс к Каппе и доступные квесты, сгруппированные по картам.",
    capPlanner: "Выберите цель и получите всю цепочку по порядку.",
    faq5Q: "Как обновиться, не потеряв прогресс?",
    faq5A: "Прогресс хранится в папке data\\profiles рядом с exe. Распакуйте новую версию в новую папку и скопируйте туда data\\profiles, либо используйте Экспорт/Импорт.",
    footerDisclaimer: "Escape from Tarkov, имена и портреты торговцев являются собственностью Battlestate Games. Приложение является неофициальным некоммерческим инструментом."
  }
};

let currentLang = 'en';

// "{v}" in the texts; replaced by the latest GitHub release tag once it is known
const REPO = 'ugurturna/eft_quest_helper';
let version = (document.querySelector('[data-version]') || {}).textContent || '';

function storage(key, value) {
  try {
    if (value === undefined) return localStorage.getItem(key);
    localStorage.setItem(key, value);
  } catch (e) {
    // private mode / blocked storage: language just isn't remembered
  }
  return null;
}

function setLanguage(lang) {
  if (!translations[lang]) lang = 'en';
  currentLang = lang;
  storage('eqh_lang', lang);

  // Update button active states
  document.querySelectorAll('.lang-btn').forEach(btn => {
    btn.classList.toggle('active', btn.getAttribute('data-lang') === lang);
  });

  // Update texts
  document.querySelectorAll('[data-i18n]').forEach(el => {
    const key = el.getAttribute('data-i18n');
    if (translations[lang][key]) {
      el.textContent = translations[lang][key].replaceAll('{v}', version);
    }
  });
  document.querySelectorAll('[data-version]').forEach(el => { el.textContent = version; });
  renderVirusTotal();

  // Update HTML lang attribute
  document.documentElement.lang = lang;
}

// VirusTotal results of the latest release; written to docs/virustotal.json by the release workflow
let virusTotal = null;

function loadVirusTotal() {
  fetch('virustotal.json', { cache: 'no-cache' })
    .then(r => (r.ok ? r.json() : null))
    .then(data => {
      if (!data || !Array.isArray(data.files) || !data.files.length) return;
      virusTotal = data;
      renderVirusTotal();
    })
    .catch(() => { /* no scan published yet: the block stays hidden */ });
}

function renderVirusTotal() {
  const box = document.getElementById('vt-scan');
  if (!box || !virusTotal) return;
  const t = translations[currentLang];
  const date = new Date(virusTotal.scannedAt).toLocaleDateString(currentLang, { year: 'numeric', month: 'long', day: 'numeric' });
  document.getElementById('vt-meta').textContent = t.vtMeta.replace('{v}', virusTotal.version).replace('{d}', date);

  const list = document.getElementById('vt-files');
  list.replaceChildren(...virusTotal.files.map(f => {
    const li = document.createElement('li');
    const name = document.createElement('span');
    name.className = 'vt-name';
    name.textContent = f.name;
    const flagged = f.malicious + f.suspicious;
    const link = document.createElement('a');
    link.className = flagged ? 'vt-result flagged' : 'vt-result';
    link.href = f.url;
    link.target = '_blank';
    link.rel = 'noopener';
    link.textContent = t.vtResult.replace('{n}', flagged).replace('{t}', f.engines);
    const sha = document.createElement('code');
    sha.className = 'vt-sha';
    sha.textContent = `SHA-256: ${f.sha256}`;
    li.append(name, link, sha);
    return li;
  }));
  box.hidden = false;
}

// Latest release: version label everywhere, download buttons point straight at the zip
function loadLatestRelease() {
  fetch(`https://api.github.com/repos/${REPO}/releases/latest`)
    .then(r => (r.ok ? r.json() : null))
    .then(release => {
      if (!release || !release.tag_name) return;
      version = release.tag_name;
      const zip = (release.assets || []).find(a => a.name.endsWith('.zip'));
      if (zip) document.querySelectorAll('[data-download]').forEach(a => { a.href = zip.browser_download_url; });
      document.querySelectorAll('[data-release]').forEach(a => { a.href = release.html_url; });
      setLanguage(currentLang);
    })
    .catch(() => { /* offline or rate limited: keep the version baked into the page */ });
}

// Screenshot gallery tabs
function setupGallery() {
  const img = document.getElementById('gallery-img');
  const caption = document.getElementById('gallery-caption');
  if (!img || !caption) return;

  document.querySelectorAll('.gallery-tab').forEach(tab => {
    tab.addEventListener('click', () => {
      document.querySelectorAll('.gallery-tab').forEach(t => {
        t.classList.toggle('active', t === tab);
        t.setAttribute('aria-selected', t === tab ? 'true' : 'false');
      });
      img.src = tab.dataset.shot;
      img.dataset.zoom = tab.dataset.shot;
      caption.setAttribute('data-i18n', tab.dataset.caption);
      caption.textContent = translations[currentLang][tab.dataset.caption] || '';
    });
  });
}

// Lightbox Logic
function setupLightbox() {
  const modal = document.getElementById('lightbox-modal');
  const img = document.getElementById('lightbox-img');
  const close = document.getElementById('lightbox-close');

  if (!modal || !img || !close) return;

  document.querySelectorAll('[data-zoom]').forEach(el => {
    el.addEventListener('click', (e) => {
      // the overlay preview sits inside the main screenshot; open only the one clicked
      e.stopPropagation();
      const src = el.getAttribute('data-zoom') || el.getAttribute('src');
      if (src) {
        img.src = src;
        modal.classList.add('active');
      }
    });
  });

  const closeModal = () => modal.classList.remove('active');
  close.addEventListener('click', closeModal);
  modal.addEventListener('click', (e) => {
    if (e.target === modal) closeModal();
  });

  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && modal.classList.contains('active')) {
      closeModal();
    }
  });
}

// FAQ Accordion Logic
function setupFaq() {
  document.querySelectorAll('.faq-item').forEach(item => {
    const question = item.querySelector('.faq-question');
    if (!question) return;

    question.addEventListener('click', () => {
      const isOpen = item.classList.contains('open');
      // Close other items
      document.querySelectorAll('.faq-item').forEach(other => {
        if (other !== item) other.classList.remove('open');
      });
      item.classList.toggle('open', !isOpen);
    });
  });
}

// Mobile Menu Toggle
function setupMobileMenu() {
  const toggle = document.querySelector('.mobile-toggle');
  const navLinks = document.querySelector('.nav-links');

  if (toggle && navLinks) {
    toggle.addEventListener('click', () => {
      navLinks.classList.toggle('mobile-open');
    });

    navLinks.querySelectorAll('a').forEach(link => {
      link.addEventListener('click', () => {
        navLinks.classList.remove('mobile-open');
      });
    });
  }
}

// Initialize on DOM ready
document.addEventListener('DOMContentLoaded', () => {
  // Determine initial language: saved, or browser
  const savedLang = storage('eqh_lang');
  if (savedLang && translations[savedLang]) {
    setLanguage(savedLang);
  } else {
    const browserLang = (navigator.language || navigator.userLanguage || '').slice(0, 2).toLowerCase();
    if (translations[browserLang]) {
      setLanguage(browserLang);
    } else {
      setLanguage('en');
    }
  }

  // Bind language buttons
  document.querySelectorAll('.lang-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      const lang = btn.getAttribute('data-lang');
      setLanguage(lang);
    });
  });

  setupLightbox();
  setupGallery();
  setupFaq();
  setupMobileMenu();
  loadLatestRelease();
  loadVirusTotal();
});
