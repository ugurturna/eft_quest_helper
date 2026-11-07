/* ==========================================================================
   EFT Quest Helper — Landing Page Interactive Scripts & Translations
   ========================================================================== */

const translations = {
  en: {
    announcement: "🚀 v0.0.1 is here! Zero ban risk, instant quest overlay for Escape from Tarkov.",
    navFeatures: "Features",
    navSafety: "Ban Safety",
    navGuide: "How to Use",
    navTraders: "Traders",
    navFaq: "FAQ",
    navDownload: "Download v0.0.1",
    
    heroBadge: "Escape from Tarkov Desktop Companion",
    heroTitlePre: "Master Every Quest.",
    heroTitleHighlight: "Escape with Kappa.",
    heroSubtitle: "A blazing-fast, zero-ban-risk quest tracker and goal planner with an always-on-top, click-through in-game overlay. Track objectives live without Alt+Tabbing.",
    btnDownload: "Download for Windows (v0.0.1)",
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
    f2Desc: "Pick any target quest (like Punisher Part 6 or The Collector). The app calculates the exact chain, what to do right now, and what's left.",
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
    safeDoes4: "Stores all quest state locally in your AppData folder.",
    safeDisclaimer: "Notice: EFT Quest Helper is an independent fan project not affiliated with Battlestate Games. Always play Tarkov in Borderless or Windowed mode to allow Windows to render overlays.",

    guideTag: "Quick Start",
    guideTitle: "Get Started in 4 Easy Steps",
    guideSub: "No complex configuration or setup required. Up and running in less than 60 seconds.",
    step1Title: "1. Download & Launch",
    step1Desc: "Download the standalone v0.0.1 release from GitHub and run TarkovQuestTracker.exe. No installation needed.",
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
    faq4A: "Yes! Open Settings in the app and click 'Export Progress'. You can import the resulting file on any PC, or copy your %APPDATA%\\TarkovQuestTracker folder.",

    ctaTitle: "Ready to Conquer Your Tarkov Wipe?",
    ctaSub: "Download EFT Quest Helper today. Free, open source, and built for PMCs.",
    ctaBtn: "Download v0.0.1 for Windows",
    
    footerDisclaimer: "Escape from Tarkov, trader names and logos are trademarks of Battlestate Games. This application is an unofficial, non-commercial fan-made tool."
  },

  tr: {
    announcement: "🚀 v0.0.1 yayında! Sıfır ban riski, Escape from Tarkov için anlık görev overlay'i.",
    navFeatures: "Özellikler",
    navSafety: "Ban Güvenliği",
    navGuide: "Nasıl Kullanılır",
    navTraders: "Traderlar",
    navFaq: "SSS",
    navDownload: "v0.0.1 İndir",
    
    heroBadge: "Escape from Tarkov Masaüstü Yardımcısı",
    heroTitlePre: "Tüm Görevleri Yönet.",
    heroTitleHighlight: "Kappa'ya Hızla Ulaş.",
    heroSubtitle: "Her zaman üstte duran, tıklama geçiren (click-through) oyun içi overlay'e ve zincirleme hedef planlayıcısına sahip, sıfır ban riskli masaüstü görev yardımcısı.",
    btnDownload: "Windows İçin v0.0.1 İndir",
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
    f2Desc: "İstediğin hedefi seç (ör. Punisher Part 6 veya Collector). Uygulama tüm zinciri, şu an yapabileceklerini ve kalanları listelesin.",
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
    safeDoes4: "Tüm ilerlemenizi yalnızca yerel AppData klasörünüzde saklar.",
    safeDisclaimer: "Bilgilendirme: EFT Quest Helper, Battlestate Games ile bağı olmayan bağımsız bir hayran projesidir. Windows'un pencere çizebilmesi için Tarkov'u Çerçevesiz (Borderless) modda oynayın.",

    guideTag: "Hızlı Başlangıç",
    guideTitle: "4 Kolay Adımda Kullanmaya Başla",
    guideSub: "Karmaşık ayarlar veya yükleme süreçleri yok. 60 saniyede hazır.",
    step1Title: "1. İndir ve Çalıştır",
    step1Desc: "GitHub Releases sayfasından hazır v0.0.1 sürümünü indir ve TarkovQuestTracker.exe'yi aç. Kurulum gerektirmez.",
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
    faq4A: "Evet! Ayarlar menüsünden 'İlerlemeyi Dışa Aktar' butonuna tıklayarak dosyanızı alabilir ve yeni bilgisayarınıza kolayca yükleyebilirsiniz.",

    ctaTitle: "Tarkov Wipe'ını Fethetmeye Hazır mısın?",
    ctaSub: "EFT Quest Helper'ı hemen ücretsiz indir. Açık kaynak kodlu ve oyuncular için üretildi.",
    ctaBtn: "Windows İçin v0.0.1 İndir",
    
    footerDisclaimer: "Escape from Tarkov, trader isimleri ve logoları Battlestate Games'in tescilli markalarıdır. Bu uygulama bağımsız, ticari olmayan bir hayran projesidir."
  },

  ru: {
    announcement: "🚀 Доступна версия v0.0.1! Без риска бана, оверлей для Escape from Tarkov.",
    navFeatures: "Функции",
    navSafety: "Безопасность",
    navGuide: "Инструкция",
    navTraders: "Торговцы",
    navFaq: "FAQ",
    navDownload: "Скачать v0.0.1",
    
    heroBadge: "Десктопный помощник для Escape from Tarkov",
    heroTitlePre: "Все квесты под контролем.",
    heroTitleHighlight: "Путь к Каппе открыт.",
    heroSubtitle: "Быстрый трекер квестов и планировщик без риска бана с оверлеем поверх экрана и сквозными кликами. Отслеживайте цели прямо в рейде.",
    btnDownload: "Скачать для Windows (v0.0.1)",
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
    f2Desc: "Выберите желаемый квест (например Каратель 6 или Коллекционер). Приложение рассчитает всю цепочку и доступные шаги.",
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
    safeDoes4: "Хранит весь прогресс только локально в вашей папке AppData.",
    safeDisclaimer: "Примечание: EFT Quest Helper — независимый фанатский проект, не связанный с Battlestate Games. Запускайте Tarkov в режиме 'Без рамки' (Borderless).",

    guideTag: "Быстрый старт",
    guideTitle: "Начните за 4 простых шага",
    guideSub: "Без сложной настройки и установки. Готов к работе за минуту.",
    step1Title: "1. Скачайте и запустите",
    step1Desc: "Скачайте релиз v0.0.1 с GitHub и запустите TarkovQuestTracker.exe. Установка не требуется.",
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
    faq4A: "Да, используйте 'Экспорт' в настройках приложения или просто скопируйте папку %APPDATA%\\TarkovQuestTracker.",

    ctaTitle: "Готовы покорять вайп Tarkov?",
    ctaSub: "Скачайте EFT Quest Helper прямо сейчас. Бесплатно и с открытым исходным кодом.",
    ctaBtn: "Скачать v0.0.1 для Windows",
    
    footerDisclaimer: "Escape from Tarkov, имена и портреты торговцев являются собственностью Battlestate Games. Приложение является неофициальным некоммерческим инструментом."
  }
};

let currentLang = 'en';

function setLanguage(lang) {
  if (!translations[lang]) lang = 'en';
  currentLang = lang;
  localStorage.setItem('eqh_lang', lang);

  // Update button active states
  document.querySelectorAll('.lang-btn').forEach(btn => {
    btn.classList.toggle('active', btn.getAttribute('data-lang') === lang);
  });

  // Update texts
  document.querySelectorAll('[data-i18n]').forEach(el => {
    const key = el.getAttribute('data-i18n');
    if (translations[lang][key]) {
      el.textContent = translations[lang][key];
    }
  });

  // Update HTML lang attribute
  document.documentElement.lang = lang;
}

// Lightbox Logic
function setupLightbox() {
  const modal = document.getElementById('lightbox-modal');
  const img = document.getElementById('lightbox-img');
  const close = document.getElementById('lightbox-close');

  if (!modal || !img || !close) return;

  document.querySelectorAll('[data-zoom]').forEach(el => {
    el.addEventListener('click', (e) => {
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
  const savedLang = localStorage.getItem('eqh_lang');
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
  setupFaq();
  setupMobileMenu();
});
