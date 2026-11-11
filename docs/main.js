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
    f1Desc: "Sits on top of the game; drag it anywhere on screen. Shows the quests you pin, or in a raid every quest on that map. Lock it with Ctrl+Shift+T so clicks go straight to the game.",
    f1Badge: "Always-on-top · Click-through",

    f2Title: "Goal Planner & Kappa Path",
    f2Desc: "Pick any target quest, like Punisher Part 6 or Collector. The app works out the exact chain, what you can do right now and what's left. The Kappa path follows Collector's real prerequisite chain. Making a quest your goal activates the step you are at.",
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
    step3Desc: "Mark your completed quests or pick a goal: the quests you can do now become active. Pin the ones you want on the overlay with 📌.",
    step4Title: "4. Lock & Raid",
    step4Desc: "In raid press O twice (auto-detect) or Ctrl+Shift+R to pick the map: the overlay shows that map's quests and the raid time left. Ctrl+Shift+T locks it so clicks go to the game.",

    hotkeysTitle: "Keyboard Shortcuts",
    hk1Desc: "Lock / unlock overlay click-through",
    hk2Desc: "Show / hide overlay completely",
    hk3Desc: "Scale overlay size (60% to 200%)",
    hk4Desc: "Move the overlay anywhere on screen",

    tradersTag: "Comprehensive Database",
    tradersTitle: "All 12 Traders & Story Chapters Included",
    tradersSub: "Over 870+ quests updated with objectives, keys, guides, and rewards.",

    faqTag: "Got Questions?",
    faqTitle: "Frequently Asked Questions",
    faq1Q: "Do I need to install .NET to run the release?",
    faq1A: "The standalone release build has .NET embedded, so you can just run the executable directly on any modern Windows 10/11 64-bit PC.",
    faq2Q: "Why does the overlay disappear when I launch Tarkov?",
    faq2A: "Make sure Escape from Tarkov is set to 'Borderless' or 'Windowed' mode in the graphics settings. Windows does not allow any external window to render over an exclusive fullscreen game.",
    faq3Q: "Does the app detect the raid or my progress automatically?",
    faq3A: "The raid, yes: press O twice in raid and the map and raid time are read off the exfil list at the top right of the screen (Windows OCR, no keyboard hook, nothing saved). Objective progress, no: the game's memory and network packets are never read, so you tick objectives yourself.",
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
    f11Title: "Entering a Raid",
    f11Desc: "Pick the map you are going to, or let a double O pick it for you: the overlay shows every active quest on it by itself. Quests on several maps show only that map's objectives; hand-over steps are hidden. Ctrl+Shift+R picks the map without leaving the game.",
    f11Badge: "Map picker · Ctrl+Shift+R",
    f12Title: "What to Bring Into the Raid",
    f12Desc: "Keys, how many markers you need, items to plant, the weapon to use and the clothes to wear are shown at the top of every quest and on its active quest card.",
    f12Badge: "Keys · Markers · Weapons",
    f13Title: "Hand In, Next Quest Accepted",
    f13Desc: "Complete an active quest and the quests it unlocks become active. Make a quest your goal and the step you are at is activated. Nothing is pushed onto the overlay: you decide what to pin.",
    f13Badge: "Auto-accept · Goal steps",
    f14Title: "Light on Your PC",
    f14Desc: "Made to run next to the game all session: about 0% CPU when idle and a small memory footprint. Flea prices are only prepared for the mode you play.",
    f14Badge: "~0% CPU when idle",
    hk5Desc: "Flea market price search mid-raid",
    hk6Desc: "Pick the raid map: the overlay shows its quests",
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
    footerDisclaimer: "Escape from Tarkov, trader names and logos are trademarks of Battlestate Games. This application is an unofficial, non-commercial fan-made tool.",
    navRead: "Read Quests",
    betaTag: "BETA",
    f15Title: "Read Quests From the Game",
    f15Desc: "Open the Tasks screen in game and scroll it once: the tracker reads your quests with the OCR built into Windows. Confirm the list and every earlier quest counts as done. Nothing is added to the game.",
    f15Badge: "Windows OCR · 30 s scan →",
    rqTag: "New · BETA",
    rqTitle: "Read Your Quests From the Game",
    rqSub: "No need to enter dozens of quests by hand. Open the Tasks screen in game, scroll it once, and the tracker reads every quest name. Confirm the list: the quests become active and everything before them counts as done.",
    rqNotice: "⚠ Works with the text recognition (OCR) built into Windows. It adds nothing to the game: no mod, no plugin, no injection.",
    rqNoticeSub: "It only takes a picture of the screen: no reading the game's memory, no fake key presses. Pictures stay in memory and are never saved or sent anywhere.",
    rq1Title: "1. English, Borderless",
    rq1Desc: "Set the game language to English and run the game in Borderless or Windowed mode. Quest names are matched against the English names.",
    rq2Title: "2. Open the Tasks Screen",
    rq2Desc: "In game, open the TASKS page and go to Side tasks, so your quest list is on screen.",
    rq3Title: "3. Start the Scan",
    rq3Desc: "Press \"Start scan\" on the Read Quests (BETA) page and switch to the game. Scroll the list slowly to the bottom: it is read about every 0.7 s and the scan stops by itself after 30 seconds. Short list? Ctrl+Shift+I reads just the current screen.",
    rq4Title: "4. Check and Apply",
    rq4Desc: "Back in the tracker, untick anything read wrong and press \"Apply\". The quests become active, their earlier quests are marked done, and one click on \"Undo\" reverts it all.",
    rqCap: "The Read Quests (BETA) page: how to use it, the scan countdown and the quests read from the game, ready to confirm.",
    rqNote: "BETA: reading works best at 1080p or higher with the game in English. Nothing is changed until you press \"Apply\", and only what you tick is used.",
    tabRead: "Read Quests",
    capRead: "Read Quests (BETA): scan the in-game Tasks screen for 30 seconds, then confirm the quests it found. Already active ones are marked and left unticked.",
    safeDoes5: "Read Quests (BETA) only takes a picture of the screen and reads it with the OCR built into Windows.",
    safeDoesNot5: "Does NOT save or upload screen pictures: they stay in memory only.",
    navRaid: "Auto Raid",
    f16Title: "Auto Raid Detection (Double O)",
    f16Desc: "Press O twice in raid: the tracker reads the exfil list, recognizes the map and starts the raid with the time left. Already in a raid? Only the time is updated. No keyboard hook, no key reading.",
    f16Badge: "Exfil list · raid timer →",
    arTag: "New · BETA",
    arTitle: "The Raid Starts by Itself: Press O Twice",
    arSub: "A double O in raid lists your exfils at the top right. Their names tell the map, and the timer above them tells the time left. The tracker reads both and puts that map's quests and a raid countdown on the overlay.",
    arNotice: "⚠ No keyboard hook and no key reading. This is not a keylogger.",
    arNoticeSub: "While the game is in front, only the top-right corner of the screen is read now and then with the OCR built into Windows. The picture stays in memory, is never saved or sent, and nothing is sent to the game.",
    ar1Title: "1. Turn It On",
    ar1Desc: "The On / Off switch sits at the top of the \"Entering a raid\" page (on by default). Run the game in Borderless or Windowed mode The overlay can stay in the top-right corner: it is left out of screen pictures, so the list under it is still read (it also stays out of recordings and streams).",
    ar2Title: "2. Press O Twice",
    ar2Desc: "In raid, double-tap O as usual. The exfil list opens in game exactly as before: the tracker only looks at the screen, the key still goes to the game.",
    ar3Title: "3. It Waits for the List",
    ar3Desc: "Not every PC draws the list at once. Once the first exfils show up, the corner is read again until no new ones appear (up to about 3 s), and only then is the map decided. An empty corner just means you are not in a raid: no error.",
    ar4Title: "4. Map + Time on the Overlay",
    ar4Desc: "The overlay switches to that map's quests and counts the raid time down. Already in a raid there? A double O only corrects the time, it never restarts the raid. The overlay locks by itself so clicks go to the game. When the time runs out, the raid ends by itself. A single O (just the clock) corrects the time of a raid already running. FPS counter on? The clock under it is still read, to the minute.",
    arLeft: "left",
    arResult: "Raid started · 5 quests on the overlay",
    arCap: "A double O lists the exfils; the tracker reads them and the raid timer, and starts the raid on that map.",
    arNote: "BETA: it needs an exfil only that map has, and two exfils in all on the big maps. When it is not sure it does nothing, and you can always pick the map yourself with Ctrl+Shift+R.",
    safeDoes6: "Auto raid detection reads only the top-right corner of the screen (Windows OCR) while the game is in front.",
    safeDoesNot6: "Does NOT hook the keyboard or read your key presses.",
    faq6Q: "Is the double O detection a keylogger?",
    faq6A: "No. The app never hooks the keyboard and never reads your keys: O goes to the game as always. It only notices that the exfil list is on screen, by reading the top-right corner of the screen with Windows OCR while the game is in front. You can turn it off at the top of the \"Entering a raid\" page.",
    faqRQ: "Is reading quests from the screen safe? Can it get me banned?",
    faqRA: "It works like a screenshot tool: it takes a picture of the screen and reads the text with the OCR built into Windows. It never touches the game: no memory reading, no injection, no fake key or mouse input; you scroll the list yourself. Pictures stay in memory and are never saved or uploaded.",
    f17Title: "Your Position on the Map",
    f17Desc: "Take a screenshot in raid with your own screenshot key: a map window shows where you are and which way you face, your path, the exfils and your active quests' spots. The floor follows your height. Only the screenshot's file name is read.",
    f17Badge: "Any screenshot key · tarkov.dev maps →",
    tabMap: "Map",
    tabMapFollow: "Map · Follow",
    capMap: "Take a screenshot in raid and the map shows where you are: the red arrow points where you face, the dashed line is your path from earlier screenshots. Exfils (PMC / Scav / shared, transits) and your active quests' spots (yellow) are on the map.",
    capMapFollow: "Follow mode keeps the view on you, zoomed in to the street. The floor follows your height; wheel zooms, dragging looks around, Esc puts you back in the game.",
    navMap: "Map GPS",
    navGoons: "Goons",
    f18Title: "Goon Tracker",
    f18Desc: "Where were the Goons last seen? The \"Entering a raid\" page shows the latest reports from tarkov-goon-tracker.com for the mode you play (PvP or PvE), and the map card where they were seen gets a GOONS badge.",
    f18Badge: "PvP + PvE · every 5 min →",
    mgTag: "New",
    mgTitle: "See Where You Are: Take a Screenshot",
    mgSub: "Lost on the map? Press your screenshot key in raid. The game writes your position into the screenshot's file name; the tracker reads that name and a map window opens with you on it: which way you face, your path, the exfils and your active quests' spots.",
    mgNotice: "✔ Only the file names of your own screenshots are read. Nothing is added to the game: no mod, no plugin, no injection.",
    mgNoticeSub: "The picture itself is never opened, no key is read and nothing is sent to the game. The maps come from tarkov.dev.",
    mg1Title: "1. Enter a Raid",
    mg1Desc: "The map follows the raid's map, picked on \"Entering a raid\" or found by the double O. Not in a raid? Pick the map in the map window.",
    mg2Title: "2. Take a Screenshot",
    mg2Desc: "Use your in-game screenshot key. Any key you bound works: the app watches the screenshot folder, not the key.",
    mg3Title: "3. You Are on the Map",
    mg3Desc: "The arrow shows where you stand and which way you face; earlier screenshots draw your path. The floor follows your height. Wheel zooms, dragging looks around, ◎ centres on you again.",
    mg4Title: "4. Esc, Back to the Game",
    mg4Desc: "Press Esc or click the game: the map closes and the game gets the keyboard back. Take another screenshot whenever you want to see where you are.",
    mgCap1: "Customs after a screenshot: the red arrow is you, the dashed line your path. Exfils, transits and your quest spots are on the map.",
    mgCap2: "The In-Game GPS page: one switch and four steps.",
    mgCap3: "Follow mode keeps the view on you, zoomed in to the street.",
    mgModesTitle: "Three Ways to Show the Map",
    mgModesSub: "Pick one on the In-Game GPS page. Every screenshot moves you on it.",
    mgModeOvTitle: "In-Game Overlay",
    mgModeOvDesc: "The big map window opens over the game; Esc takes you back.",
    mgModeMiniTitle: "Round Mini Map",
    mgModeMiniDesc: "A round map around you in a corner of the screen for a few seconds. Pick the corner, size and zoom; keep north up or let it turn with you. It never takes the mouse or the keyboard.",
    mgModeAppTitle: "In App",
    mgModeAppDesc: "The map stays on inside the app (In-Game GPS → Map). Put it on your second monitor: nothing pops up over the game.",
    mgCap4: "The mini map on Woods after a screenshot: you in the middle, exfils and the BTR's route around you.",
    mgCap5: "Turned with you: where you look is up, the N on the ring shows north.",
    mgCap6: "In app: the Map page under In-Game GPS, always on for a second monitor.",
    navMini: "Mini Map",
    mmTag: "New",
    mmTitle: "A Round Mini Map, or the Map on Your Second Screen",
    mmSub: "The GPS map now shows the way you want: a small round mini map in a corner of the game, the map window over the game, or the map inside the app on your second monitor. Take a screenshot and only your position moves.",
    mmP1Title: "Screenshot, then a glance",
    mmP1Desc: "Press your screenshot key: the mini map comes up for a few seconds (3 to 30, you pick), then it's gone.",
    mmP2Title: "Never in your way",
    mmP2Desc: "Clicks go straight through to the game; it never takes the mouse or the keyboard.",
    mmP3Title: "Any of the 4 corners",
    mmP3Desc: "Top left, top right, bottom left or bottom right, with the size and zoom you like.",
    mmP4Title: "North up, or turning with you",
    mmP4Desc: "Keep north up, or let the map turn so where you look is always up; the N on the ring shows north.",
    mmP5Title: "Everything the big map has",
    mmP5Desc: "Exfils, transits, your quest spots, the BTR and your path, on the raid's map from the double O. A quest spot off the mini map gets a yellow arrow on the ring pointing the way, with the distance.",
    mmNote: "Like the map window, it only reads your screenshots' file names: no mod, no injection. Run the game in Borderless or Windowed mode so the mini map can show on top.",
    f20Title: "Round Mini Map",
    f20Desc: "A screenshot brings up a round mini map around you in a corner of the game for a few seconds, clicks go through. Or keep the map on inside the app on your second monitor.",
    f20Badge: "4 corners · turns with you · second screen →",
    mgNote: "Tip: set the screenshot folder on the In-Game GPS page if yours is not the default one. Run the game in Borderless or Windowed mode so the map window can show on top.",
    gtTag: "New",
    gtTitle: "Where Are the Goons Now?",
    gtSub: "Knight, Big Pipe and Birdeye roam Customs, Woods, Shoreline and Lighthouse. The tracker shows where players last reported them, so you know which map to pick for them, or which one to avoid.",
    gt1Title: "PvP and PvE Apart",
    gt1Desc: "The reports follow the PvP | PvE switch at the top: PvP shows the PvP reports, PvE the PvE ones.",
    gt2Title: "Last Seen + 5 Reports",
    gt2Desc: "On \"Entering a raid\": the map they were last seen on and how long ago, plus the last 5 reports. That map's card gets a GOONS badge.",
    gt3Title: "Always Fresh",
    gt3Desc: "Refreshed every 5 minutes while the app runs, or right away with ⟳. \"Goon Tracker ↗\" opens the full site for that mode.",
    gtCap: "The Goon card on \"Entering a raid\": where they were last seen, the last reports and the GOONS badge on that map.",
    gtNote: "Reports come from players on tarkov-goon-tracker.com, so they can be wrong or late. The tracker only reads that public page; nothing is sent from your game.",
    tabGoons: "Goons",
    tabMini: "Mini Map",
    capMini: "The round mini map in a corner of the game after a screenshot: you in the middle, exfils and the BTR's route around you. Clicks go through to the game.",
    tabInApp: "Map · In App",
    capInApp: "The map inside the app (In-Game GPS → Map), always on for a second monitor: every screenshot moves you on it.",
    capGoons: "Goon tracker: where the Goons were last seen in your mode (PvP or PvE), the last reports, and a GOONS badge on that map's card.",
    navBtr: "BTR",
    f19Title: "BTR Tracker",
    f19Desc: "Where is the BTR now, and when does it reach the next stop? On Woods, Streets and Lighthouse the tracker works it out from the raid clock: the next stop and its ETA on the overlay, the route and the BTR on the map window, every stop on the raid page.",
    f19Badge: "Woods · Streets · Lighthouse →",
    btTag: "New",
    btTitle: "Where Is the BTR, and When Does It Come?",
    btSub: "On Woods, Streets of Tarkov and Lighthouse the BTR drives fixed routes on a timetable: it spawns at a set raid time, waits at each stop and goes round again. From the raid time left, the tracker estimates where it is and when it reaches each stop.",
    bt1Title: "1. Raid Clock",
    bt1Desc: "Double O in raid: the map and the raid time left are read off the exfil list. The BTR timetable starts from there.",
    bt2Title: "2. Next Stop on the Overlay",
    bt2Desc: "One line under the raid time: where the BTR is heading and its ETA, or which stop it waits at and for how long.",
    bt3Title: "3. On the Map",
    bt3Desc: "Take a screenshot: the map window shows the routes it may be on, every stop with its ETA and where the BTR should be now.",
    bt4Title: "4. Seen It? Press Seen",
    bt4Desc: "On the raid page, press Seen next to the stop where you spot the BTR: the times line up with it and routes that don't fit are dropped.",
    btNote: "The times are estimates (the spawn time varies by about a minute); one Seen brings them much closer. Each view can be turned off in Settings. Nothing is read from the game other than the raid clock the double O already reads.",
    safeDoes7: "The map reads only the file names of the screenshots you take yourself (the game writes your position into them); the pictures are never opened.",
    faq7Q: "How does the map know where I am? Which key do I press?",
    faq7A: "When you take a screenshot in raid, the game itself writes your position into the file name. The app watches your screenshot folder (Documents\\Escape from Tarkov\\Screenshots) and reads that name, so whatever key you bound for screenshots in the game works: there is no extra hotkey. The picture is never opened, and nothing touches the game."
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
    f1Desc: "Oyunun üstünde durur, ekranda istediğin yere sürükle. Sabitlediğin görevleri, raid'deyken de o haritadaki bütün görevleri gösterir. Ctrl+Shift+T ile kilitle, tıklamalar oyuna geçsin.",
    f1Badge: "Her zaman üstte · Tıklama geçirir",

    f2Title: "Hedef Planlayıcı & Kappa Yolu",
    f2Desc: "İstediğin hedefi seç (ör. Punisher Part 6 veya Collector). Uygulama tüm zinciri, şu an yapabileceklerini ve kalanları listeler. Kappa yolu, Collector'ın gerçek ön koşul zincirini izler. Bir görevi hedef yapınca bulunduğun adım aktif olur.",
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
    step3Desc: "Tamamladığın görevleri işaretle veya hedefini seç: şu an yapabileceğin görevler aktif olur. Overlay'de görmek istediklerini 📌 ile sabitle.",
    step4Title: "4. Kilitle ve Raide Gir",
    step4Desc: "Raid'de O'ya iki kez bas (otomatik algılama) ya da Ctrl+Shift+R ile haritayı seç: overlay o haritanın görevlerini ve kalan raid süresini göstersin. Ctrl+Shift+T ile kilitle, tıklamalar oyuna geçsin.",

    hotkeysTitle: "Klavye Kısayolları",
    hk1Desc: "Overlay tıklama geçirmeyi kilitle / aç",
    hk2Desc: "Overlay'i tamamen göster / gizle",
    hk3Desc: "Overlay boyutunu ölçekle (%60 - %200)",
    hk4Desc: "Overlay'i ekranda istediğin yere taşı",

    tradersTag: "Kapsamlı Veritabanı",
    tradersTitle: "12 Trader & Hikaye Bölümleri Dahil",
    tradersSub: "870'den fazla görev; hedefleri, anahtarları, rehberleri ve ödülleriyle birlikte hazır.",

    faqTag: "Sorularınız mı Var?",
    faqTitle: "Sıkça Sorulan Sorular",
    faq1Q: "Çalıştırmak için .NET yüklemem gerekiyor mu?",
    faq1A: "Hazır release sürümünde .NET gömülü geldiği için ekstra hiçbir şey kurmadan Windows 10/11 üzerinde doğrudan açabilirsiniz.",
    faq2Q: "Tarkov'u açınca overlay neden kayboluyor?",
    faq2A: "Tarkov grafik ayarlarından ekran modunu 'Borderless' veya 'Windowed' yaptığınızdan emin olun. Windows, tam ekran oyunların üzerine pencere çizemez.",
    faq3Q: "Raid'i ya da ilerlememi otomatik algılar mı?",
    faq3A: "Raid'i evet: raid'de O'ya iki kez basınca map ve raid süresi ekranın sağ üstündeki exfil listesinden okunur (Windows OCR, klavye hook'u yok, hiçbir şey kaydedilmez). Hedef ilerlemesini hayır: oyun belleği ve paketleri hiç okunmaz, hedefleri kutucuklara tıklayarak işaretlersin.",
    faq4Q: "İlerlememi başka bir bilgisayara aktarabilir miyim?",
    faq4A: "Evet! Ayarlar'dan 'Dışa aktar' ile dosyanızı alıp başka bilgisayarda içe aktarabilir ya da exe'nin yanındaki data\\profiles klasörünü kopyalayabilirsiniz.",

    ctaTitle: "Tarkov Wipe'ını Fethetmeye Hazır mısın?",
    ctaSub: "EFT Quest Helper'ı hemen ücretsiz indir. Açık kaynak kodlu ve oyuncular için üretildi.",
    ctaBtn: "Windows İçin {v} İndir",
    
    navShots: "Ekran Görüntüleri",
    newTag: "Yeni",
    navEndgame: "End Game",
    f9Title: "End Game: The Ticket Sonları",
    f9Desc: "4 sondan hangilerine hâlâ ulaşabilirsin, hangi yoldan gitmelisin, şu an neredesin? Kararlarını işaretle, hedef sonunu seç ve yolu adım adım takip et.",
    f9Badge: "4 son · Yol planlayıcı →",
    tabEndgame: "End Game",
    capEndgame: "The Ticket için End Game rehberi: başarım ikonlarıyla 4 son, verdiğin kararlar ve \"Şu an buradasın\" işaretiyle hedef sonuna giden yol.",
    tabFlea: "Flea Market",
    capFlea: "Oyunda Ctrl+Shift+F: 3 harf yaz, modundaki flea fiyatlarını gör (solda PvP, sağda PvE); slot başı fiyat, 24s ortalama, 48 saatlik değişim ve en iyi trader. Esc oyuna döndürür.",
    f10Title: "Oyun İçinde Flea Fiyatları",
    f10Desc: "Raid ortasında Ctrl+Shift+F: ekranın ortasında fiyat araması açılır. 3 harf yaz, modundaki (PvP ya da PvE) flea fiyatını, slot başı fiyatı ve en iyi trader'ı gör. Esc seni oyuna döndürür.",
    f10Badge: "PvP & PvE fiyatları · tarkov.dev",
    f11Title: "Raid'e Giriyorum",
    f11Desc: "Gireceğin haritayı seç ya da çift O ile kendisi seçsin: overlay o haritadaki bütün aktif görevleri kendiliğinden gösterir. Birden çok haritalı görevlerde sadece o haritanın hedefleri görünür, teslim adımları gizlenir. Ctrl+Shift+R ile oyundan çıkmadan harita seçilir.",
    f11Badge: "Harita seçici · Ctrl+Shift+R",
    f12Title: "Raid'e Ne Götürmeli",
    f12Desc: "Anahtarlar, kaç marker gerektiği, yerleştirilecek itemler, kullanılacak silah ve giyilecek kıyafet her görevin en üstünde ve aktif görev kartında görünür.",
    f12Badge: "Anahtar · Marker · Silah",
    f13Title: "Teslim Et, Sonraki Görev Kabul",
    f13Desc: "Aktif bir görevi tamamla, açtığı görevler aktif olsun. Bir görevi hedef yap, bulunduğun adım aktif olsun. Overlay'e hiçbir şey zorla eklenmez: neyi sabitleyeceğine sen karar verirsin.",
    f13Badge: "Otomatik kabul · Hedef adımları",
    f14Title: "Bilgisayarını Yormaz",
    f14Desc: "Oyunla birlikte saatlerce açık kalacak şekilde yapıldı: boştayken neredeyse %0 CPU ve küçük bellek kullanımı. Flea fiyatları sadece oynadığın mod için hazırlanır.",
    f14Badge: "Boştayken ~%0 CPU",
    hk5Desc: "Raid ortasında flea fiyat araması",
    hk6Desc: "Raid haritasını seç: overlay o haritanın görevlerini gösterir",
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
    footerDisclaimer: "Escape from Tarkov, trader isimleri ve logoları Battlestate Games'in tescilli markalarıdır. Bu uygulama bağımsız, ticari olmayan bir hayran projesidir.",
    navRead: "Görevleri Oku",
    betaTag: "BETA",
    f15Title: "Görevleri Oyundan Oku",
    f15Desc: "Oyunda Tasks ekranını aç ve bir kez kaydır: uygulama görevlerini Windows'un kendi OCR'ı ile okur. Listeyi onayla, önceki tüm görevler tamamlandı sayılsın. Oyuna hiçbir şey eklenmez.",
    f15Badge: "Windows OCR · 30 sn tarama →",
    rqTag: "Yeni · BETA",
    rqTitle: "Görevlerini Oyundan Oku",
    rqSub: "Onlarca görevi elle girmene gerek yok. Oyunda Tasks ekranını aç, bir kez kaydır; uygulama tüm görev adlarını okur. Listeyi onayla: görevler aktif olur, onlardan önceki her şey tamamlandı sayılır.",
    rqNotice: "⚠ Windows'un kendi yazı tanıma (OCR) özelliğiyle çalışır. Oyuna herhangi bir eklenti, mod veya müdahale yapmaz.",
    rqNoticeSub: "Sadece ekranın görüntüsünü alır: oyunun belleğini okumaz, sahte tuş basmaz. Görüntüler sadece bellekte kalır, kaydedilmez ve hiçbir yere gönderilmez.",
    rq1Title: "1. İngilizce, Borderless",
    rq1Desc: "Oyun dilini İngilizce yap, oyunu Borderless veya Windowed modda çalıştır. Görev adları İngilizce adlarla eşleştirilir.",
    rq2Title: "2. Tasks Ekranını Aç",
    rq2Desc: "Oyun içinden TASKS sayfasını aç ve Side tasks bölümüne gel, görev listen ekranda olsun.",
    rq3Title: "3. Taramayı Başlat",
    rq3Desc: "\"Görevleri Oku (BETA)\" sayfasında \"Taramayı başlat\"a bas ve oyuna geç. Listeyi yavaşça en alta kaydır: yaklaşık 0,7 sn'de bir okunur, tarama 30 saniye sonra kendiliğinden durur. Liste kısa mı? Ctrl+Shift+I sadece o anki ekranı okur.",
    rq4Title: "4. Kontrol Et ve Uygula",
    rq4Desc: "Uygulamaya dön, yanlış okunanların işaretini kaldır ve \"Uygula\"ya bas. Görevler aktif olur, önceki görevleri tamamlandı sayılır; tek tıkla \"Geri al\" hepsini geri çevirir.",
    rqCap: "\"Görevleri Oku (BETA)\" sayfası: kullanım adımları, tarama geri sayımı ve oyundan okunup onay bekleyen görevler.",
    rqNote: "BETA: en iyi 1080p ve üstünde, oyun İngilizceyken çalışır. \"Uygula\"ya basana kadar hiçbir şey değişmez, sadece işaretlediklerin kullanılır.",
    tabRead: "Görevleri Oku",
    capRead: "Görevleri Oku (BETA): oyundaki Tasks ekranını 30 saniye tara, bulunan görevleri onayla. Zaten aktif olanlar belirtilir ve işaretsiz gelir.",
    safeDoes5: "Görevleri Oku (BETA) sadece ekranın görüntüsünü alır ve Windows'un kendi OCR'ı ile okur.",
    safeDoesNot5: "Ekran görüntülerini kaydetmez veya yüklemez: sadece bellekte kalır.",
    navRaid: "Otomatik Raid",
    f16Title: "Otomatik Raid Algılama (Çift O)",
    f16Desc: "Raid'de O'ya iki kez bas: takipçi exfil listesini okur, map'i tanır ve raid'i kalan süreyle başlatır. Zaten raid'desin? Sadece süre güncellenir. Klavye hook'u yok, tuş okunmaz.",
    f16Badge: "Exfil listesi · raid süresi →",
    arTag: "Yeni · BETA",
    arTitle: "Raid Kendiliğinden Başlar: O'ya İki Kez Bas",
    arSub: "Raid'de çift O, exfillerini sağ üstte listeler. İsimleri map'i, üstlerindeki sayaç da kalan süreyi söyler. Takipçi ikisini de okur; overlay'e o map'in görevlerini ve raid geri sayımını koyar.",
    arNotice: "⚠ Klavye hook'u yok, tuş okunmaz. Bu bir keylogger değil.",
    arNoticeSub: "Oyun öndeyken sadece ekranın sağ üst köşesi Windows'un kendi OCR'ı ile ara ara okunur. Görüntü bellekte kalır, kaydedilmez ve gönderilmez; oyuna hiçbir şey gönderilmez.",
    ar1Title: "1. Aç",
    ar1Desc: "Açık / Kapalı anahtarı \"Raid'e giriyorum\" sayfasının en üstünde (varsayılan: açık). Oyunu Borderless veya Pencereli modda çalıştır, Overlay sağ üst köşede kalabilir: ekran görüntülerinden çıkarılır, altındaki liste yine okunur (kayıt ve yayında da görünmez).",
    ar2Title: "2. O'ya İki Kez Bas",
    ar2Desc: "Raid'de her zamanki gibi O'ya çift bas. Exfil listesi oyunda eskisi gibi açılır: takipçi sadece ekrana bakar, tuş yine oyuna gider.",
    ar3Title: "3. Listenin Yüklenmesini Bekler",
    ar3Desc: "Her PC listeyi bir anda çizmez. İlk exfiller görününce köşe yeni exfil gelmeyene kadar tekrar okunur (en fazla ~3 sn), map ancak ondan sonra belirlenir. Köşe boşsa raid'de değilsin demektir: hata yok.",
    ar4Title: "4. Overlay'de Map + Süre",
    ar4Desc: "Overlay o map'in görevlerine geçer ve raid süresini geri sayar. Zaten o map'te raid'desin? Çift O sadece süreyi düzeltir, raid'i asla baştan başlatmaz. Overlay kendiliğinden kilitlenir, tıklamalar oyuna gider. Süre bitince raid kendiliğinden kapanır. Tek O (sadece saat) devam eden raid'in süresini düzeltir. FPS sayacı açık mı? Altındaki saat yine okunur, dakikasına kadar.",
    arLeft: "kaldı",
    arResult: "Raid başladı · overlay'de 5 görev",
    arCap: "Çift O exfilleri listeler; takipçi onları ve raid sayacını okur, raid'i o map'te başlatır.",
    arNote: "BETA: sadece o map'te olan bir exfil, büyük map'lerde de toplam iki exfil ister. Emin değilse bir şey yapmaz; map'i her zaman Ctrl+Shift+R ile kendin de seçebilirsin.",
    safeDoes6: "Otomatik raid algılama, oyun öndeyken sadece ekranın sağ üst köşesini okur (Windows OCR).",
    safeDoesNot6: "Klavyeyi hook'lamaz, tuş basışlarını okumaz.",
    faq6Q: "Çift O algılaması bir keylogger mı?",
    faq6A: "Hayır. Uygulama klavyeyi hiç hook'lamaz, tuşlarını okumaz: O her zamanki gibi oyuna gider. Sadece exfil listesinin ekranda olduğunu, oyun öndeyken ekranın sağ üst köşesini Windows OCR ile okuyarak fark eder. \"Raid'e giriyorum\" sayfasının en üstünden kapatabilirsin.",
    faqRQ: "Görevleri ekrandan okumak güvenli mi? Ban yer miyim?",
    faqRA: "Ekran görüntüsü aracı gibi çalışır: ekranın görüntüsünü alır ve yazıyı Windows'un kendi OCR'ı ile okur. Oyuna hiç dokunmaz: bellek okuma, enjeksiyon, sahte tuş veya fare girdisi yok; listeyi sen kaydırırsın. Görüntüler sadece bellekte kalır, kaydedilmez ve yüklenmez.",
    f17Title: "Haritada Konumun",
    f17Desc: "Raidde kendi ekran görüntüsü tuşunla ekran görüntüsü al: harita penceresi nerede olduğunu ve nereye baktığını, izini, exfilleri ve aktif görevlerinin noktalarını gösterir. Kat yüksekliğine göre değişir. Sadece ekran görüntüsünün dosya adı okunur.",
    f17Badge: "Her ekran görüntüsü tuşu · tarkov.dev haritaları →",
    tabMap: "Harita",
    tabMapFollow: "Harita · Takip",
    capMap: "Raidde ekran görüntüsü al, harita nerede olduğunu göstersin: kırmızı ok baktığın yönü, kesikli çizgi önceki ekran görüntülerinden izini gösterir. Exfiller (PMC / Scav / ortak, transitler) ve aktif görevlerinin noktaları (sarı) haritada.",
    capMapFollow: "Takip modu görünümü sende tutar, sokak seviyesine yakınlaşır. Kat yüksekliğine göre değişir; tekerlek yakınlaştırır, sürüklemek etrafa bakar, Esc seni oyuna geri döndürür.",
    navMap: "Harita GPS",
    navGoons: "Goons",
    f18Title: "Goon Tracker",
    f18Desc: "Goons en son nerede görüldü? \"Raid'e giriyorum\" sayfası, oynadığın modun (PvP veya PvE) tarkov-goon-tracker.com'daki son bildirimlerini gösterir; görüldükleri haritanın kartına GOONS rozeti gelir.",
    f18Badge: "PvP + PvE · 5 dakikada bir →",
    mgTag: "Yeni",
    mgTitle: "Nerede Olduğunu Gör: Ekran Görüntüsü Al",
    mgSub: "Haritada kayboldun mu? Raidde ekran görüntüsü tuşuna bas. Oyun konumunu ekran görüntüsünün dosya adına yazar; takipçi bu adı okur ve seni gösteren bir harita penceresi açılır: nereye baktığın, izin, exfiller ve aktif görevlerinin noktaları.",
    mgNotice: "✔ Sadece kendi ekran görüntülerinin dosya adları okunur. Oyuna hiçbir şey eklenmez: mod yok, eklenti yok, injection yok.",
    mgNoticeSub: "Resmin kendisi hiç açılmaz, tuş okunmaz, oyuna hiçbir şey gönderilmez. Haritalar tarkov.dev'den gelir.",
    mg1Title: "1. Raide Gir",
    mg1Desc: "Harita raidin haritasını takip eder: \"Raid'e giriyorum\"da seçtiğin ya da çift O ile bulunan map. Raidde değil misin? Haritayı harita penceresinden seç.",
    mg2Title: "2. Ekran Görüntüsü Al",
    mg2Desc: "Oyundaki ekran görüntüsü tuşunu kullan. Hangi tuşu atadıysan çalışır: uygulama tuşu değil, ekran görüntüsü klasörünü izler.",
    mg3Title: "3. Haritadasın",
    mg3Desc: "Ok nerede durduğunu ve nereye baktığını gösterir; önceki ekran görüntüleri izini çizer. Kat yüksekliğine göre değişir. Tekerlek yakınlaştırır, sürüklemek etrafa bakar, ◎ seni yeniden ortalar.",
    mg4Title: "4. Esc, Oyuna Dön",
    mg4Desc: "Esc'ye bas ya da oyuna tıkla: harita kapanır, klavye oyuna döner. Nerede olduğunu görmek istediğinde yeni bir ekran görüntüsü al.",
    mgCap1: "Ekran görüntüsünden sonra Customs: kırmızı ok sensin, kesikli çizgi izin. Exfiller, transitler ve görev noktaların haritada.",
    mgCap2: "Oyun İçi GPS sayfası: tek anahtar, dört adım.",
    mgCap3: "Takip modu görünümü sende tutar, sokak seviyesine yakınlaşır.",
    mgModesTitle: "Haritayı Görmenin Üç Yolu",
    mgModesSub: "Oyun İçi GPS sayfasından birini seç. Her ekran görüntüsü seni haritada ilerletir.",
    mgModeOvTitle: "Oyun İçi Overlay",
    mgModeOvDesc: "Büyük harita penceresi oyunun üstünde açılır; Esc ile oyuna dönersin.",
    mgModeMiniTitle: "Yuvarlak Mini Harita",
    mgModeMiniDesc: "Ekranın bir köşesinde seni ortalayan yuvarlak bir harita birkaç saniye görünür. Köşeyi, boyutu ve yakınlığı seç; kuzey yukarıda kalsın ya da seninle dönsün. Fareyi ve klavyeyi hiç almaz.",
    mgModeAppTitle: "Uygulama İçi",
    mgModeAppDesc: "Harita uygulamanın içinde hep açık kalır (Oyun İçi GPS → Harita). İkinci ekranına koy: oyunun üstüne hiçbir şey açılmaz.",
    mgCap4: "Ekran görüntüsünden sonra Woods'ta mini harita: sen ortada, çevrende exfil'ler ve BTR'nin rotası.",
    mgCap5: "Seninle dönerken: baktığın yön yukarıda, halkadaki N kuzeyi gösterir.",
    mgCap6: "Uygulama içi: Oyun İçi GPS altındaki Harita sayfası, ikinci ekran için hep açık.",
    navMini: "Mini Harita",
    mmTag: "Yeni",
    mmTitle: "Yuvarlak Mini Harita ya da İkinci Ekranında Harita",
    mmSub: "GPS haritası artık istediğin gibi görünüyor: oyunun bir köşesinde küçük yuvarlak bir mini harita, oyunun üstünde harita penceresi ya da ikinci ekranında uygulamanın içinde harita. Ekran görüntüsü al, sadece konumun güncellensin.",
    mmP1Title: "Ekran görüntüsü, sonra bir bakış",
    mmP1Desc: "Ekran görüntüsü tuşuna bas: mini harita birkaç saniye görünür (3 ile 30 arası, sen seçersin), sonra kaybolur.",
    mmP2Title: "Hiç yolunda durmaz",
    mmP2Desc: "Tıklamalar doğrudan oyuna geçer; fareyi ve klavyeyi hiç almaz.",
    mmP3Title: "4 köşeden biri",
    mmP3Desc: "Sol üst, sağ üst, sol alt ya da sağ alt; istediğin boyut ve yakınlıkta.",
    mmP4Title: "Kuzey yukarıda ya da seninle dönen",
    mmP4Desc: "Kuzey yukarıda kalsın ya da harita dönsün, baktığın yön hep yukarıda olsun; halkadaki N kuzeyi gösterir.",
    mmP5Title: "Büyük haritadaki her şey",
    mmP5Desc: "Exfil'ler, transitler, görev noktaların, BTR ve izin; çift O ile bulunan raid haritasında. Mini haritanın dışında kalan görev noktası için halkada yönü gösteren sarı bir ok ve mesafe çıkar.",
    mmNote: "Harita penceresi gibi sadece ekran görüntülerinin dosya adlarını okur: mod yok, müdahale yok. Mini haritanın üstte görünmesi için oyunu Borderless veya Pencereli modda çalıştır.",
    f20Title: "Yuvarlak Mini Harita",
    f20Desc: "Ekran görüntüsü alınca oyunun bir köşesinde seni ortalayan yuvarlak bir mini harita birkaç saniye görünür, tıklamalar oyuna geçer. Ya da haritayı ikinci ekranında uygulamanın içinde hep açık tut.",
    f20Badge: "4 köşe · seninle döner · ikinci ekran →",
    mgNote: "İpucu: ekran görüntüsü klasörün varsayılan değilse Oyun İçi GPS sayfasından seç. Harita penceresinin üstte görünmesi için oyunu Borderless veya Pencereli modda çalıştır.",
    gtTag: "Yeni",
    gtTitle: "Goons Şu An Nerede?",
    gtSub: "Knight, Big Pipe ve Birdeye Customs, Woods, Shoreline ve Lighthouse'ta dolaşır. Takipçi oyuncuların onları en son nerede bildirdiğini gösterir; onlar için hangi haritaya gireceğini ya da hangisinden uzak duracağını bilirsin.",
    gt1Title: "PvP ve PvE Ayrı",
    gt1Desc: "Bildirimler üstteki PvP | PvE anahtarını takip eder: PvP'de PvP bildirimleri, PvE'de PvE bildirimleri.",
    gt2Title: "Son Görülme + 5 Bildirim",
    gt2Desc: "\"Raid'e giriyorum\"da: en son görüldükleri harita ve ne kadar önce olduğu, ayrıca son 5 bildirim. O haritanın kartına GOONS rozeti gelir.",
    gt3Title: "Hep Güncel",
    gt3Desc: "Uygulama açıkken 5 dakikada bir, ya da ⟳ ile hemen yenilenir. \"Goon Tracker ↗\" o modun sitesini açar.",
    gtCap: "\"Raid'e giriyorum\"daki Goon kartı: en son görüldükleri yer, son bildirimler ve o haritadaki GOONS rozeti.",
    gtNote: "Bildirimler tarkov-goon-tracker.com'daki oyunculardan gelir; yanlış ya da geç olabilir. Takipçi sadece o herkese açık sayfayı okur, oyunundan hiçbir şey gönderilmez.",
    tabGoons: "Goons",
    tabMini: "Mini Harita",
    capMini: "Ekran görüntüsünden sonra oyunun köşesinde yuvarlak mini harita: sen ortada, çevrende exfil'ler ve BTR'nin rotası. Tıklamalar oyuna geçer.",
    tabInApp: "Harita · Uygulama İçi",
    capInApp: "Uygulamanın içinde harita (Oyun İçi GPS → Harita), ikinci ekran için hep açık: her ekran görüntüsü seni haritada ilerletir.",
    capGoons: "Goon tracker: modundaki (PvP veya PvE) Goons'un en son görüldüğü yer, son bildirimler ve o haritanın kartında GOONS rozeti.",
    navBtr: "BTR",
    f19Title: "BTR Takibi",
    f19Desc: "BTR şu an nerede, bir sonraki durağa ne zaman varacak? Woods, Streets ve Lighthouse'ta takipçi bunu raid süresinden hesaplar: overlay'de sonraki durak ve TVS, harita penceresinde rota ve BTR, raid sayfasında tüm duraklar.",
    f19Badge: "Woods · Streets · Lighthouse →",
    btTag: "Yeni",
    btTitle: "BTR Nerede, Ne Zaman Gelecek?",
    btSub: "Woods, Streets of Tarkov ve Lighthouse'ta BTR sabit rotalarda bir zaman çizelgesiyle gider: belli bir raid süresinde doğar, her durakta bekler ve rota bitince başa döner. Takipçi kalan raid süresinden nerede olduğunu ve her durağa ne zaman varacağını (TVS) tahmin eder.",
    bt1Title: "1. Raid Süresi",
    bt1Desc: "Raidde çift O: harita ve kalan raid süresi exfil listesinden okunur. BTR çizelgesi buradan başlar.",
    bt2Title: "2. Overlay'de Sonraki Durak",
    bt2Desc: "Raid süresinin altında tek satır: BTR'nin gittiği durak ve TVS, ya da hangi durakta ne kadar daha bekleyeceği.",
    bt3Title: "3. Haritada",
    bt3Desc: "Ekran görüntüsü al: harita penceresi olası rotaları, her durağı TVS'siyle ve BTR'nin şu an olması gereken yeri gösterir.",
    bt4Title: "4. Gördün mü? Gördüm'e Bas",
    bt4Desc: "Raid sayfasında BTR'yi gördüğün durağın yanındaki Gördüm'e bas: süreler buna göre ayarlanır, uymayan rotalar elenir.",
    btNote: "Süreler tahminidir (doğma süresi yaklaşık bir dakika oynar); tek bir Gördüm süreleri çok yaklaştırır. Her görünüm Ayarlar'dan kapatılabilir. Oyundan, çift O'nun zaten okuduğu raid süresi dışında hiçbir şey okunmaz.",
    safeDoes7: "Harita sadece senin aldığın ekran görüntülerinin dosya adlarını okur (oyun konumunu bu adlara yazar); görüntüler hiç açılmaz.",
    faq7Q: "Harita nerede olduğumu nasıl biliyor? Hangi tuşa basacağım?",
    faq7A: "Raidde ekran görüntüsü aldığında oyunun kendisi konumunu dosya adına yazar. Uygulama ekran görüntüsü klasörünü (Belgeler\\Escape from Tarkov\\Screenshots) izler ve bu adı okur; yani oyunda ekran görüntüsü için hangi tuşu atadıysan o çalışır, ek bir kısayol yok. Görüntü hiç açılmaz, oyuna hiçbir şekilde dokunulmaz."
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
    f1Desc: "Поверх игры, перетаскивается в любое место экрана. Показывает закреплённые задания, а в рейде — все задания этой карты. Заблокируйте по Ctrl+Shift+T — клики пойдут прямо в игру.",
    f1Badge: "Поверх всех окон · Сквозные клики",

    f2Title: "Планировщик целей и Каппы",
    f2Desc: "Выберите желаемый квест (например Каратель 6 или Коллекционер). Приложение рассчитает всю цепочку и доступные шаги. Путь к Каппе следует реальной цепочке требований Коллекционера. Если сделать задание целью, текущий шаг станет активным.",
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
    step3Desc: "Отметьте выполненные задания или задайте цель: доступные сейчас задания станут активными. Закрепите нужные на оверлее кнопкой 📌.",
    step4Title: "4. Заблокируйте и в рейд",
    step4Desc: "В рейде дважды нажмите O (автоопределение) или Ctrl+Shift+R, чтобы выбрать карту: оверлей покажет её задания и оставшееся время рейда. Ctrl+Shift+T блокирует его, чтобы клики шли в игру.",

    hotkeysTitle: "Горячие клавиши",
    hk1Desc: "Включить / отключить сквозные клики оверлея",
    hk2Desc: "Показать / скрыть оверлей целиком",
    hk3Desc: "Масштабировать оверлей (от 60% до 200%)",
    hk4Desc: "Переместить оверлей в любое место экрана",

    tradersTag: "База данных",
    tradersTitle: "Все 12 торговцев и сюжетные главы",
    tradersSub: "Более 870 заданий с целями, ключами, гайдами и наградами.",

    faqTag: "Вопросы и ответы",
    faqTitle: "Часто задаваемые вопросы",
    faq1Q: "Нужно ли устанавливать .NET?",
    faq1A: "Готовая сборка релиза включает все необходимые компоненты, приложение запускается сразу на Windows 10/11 x64.",
    faq2Q: "Почему оверлей скрывается при запуске игры?",
    faq2A: "Убедитесь, что в Tarkov выбран режим 'Без рамки' (Borderless) или 'В окне'. Эксклюзивный полноэкранный режим блокирует сторонние окна.",
    faq3Q: "Определяет ли приложение рейд или прогресс автоматически?",
    faq3A: "Рейд — да: дважды нажмите O в рейде, и карта и время рейда считываются со списка выходов справа вверху экрана (OCR Windows, без перехвата клавиатуры, ничего не сохраняется). Прогресс целей — нет: память игры и сетевые пакеты не читаются, галочки вы ставите сами.",
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
    f11Title: "Захожу в рейд",
    f11Desc: "Выберите карту сами или позвольте двойному O выбрать её: оверлей сам покажет все активные задания на ней. У заданий на нескольких картах видны только цели этой карты, шаги «передать» скрыты. Ctrl+Shift+R выбирает карту, не выходя из игры.",
    f11Badge: "Выбор карты · Ctrl+Shift+R",
    f12Title: "Что взять в рейд",
    f12Desc: "Ключи, сколько нужно маркеров, предметы, которые нужно спрятать, нужное оружие и одежда показаны вверху каждого задания и на его карточке в активных заданиях.",
    f12Badge: "Ключи · Маркеры · Оружие",
    f13Title: "Сдал — следующее принято",
    f13Desc: "Выполните активное задание, и открытые им задания станут активными. Сделайте задание целью — активным станет текущий шаг. На оверлей ничего не добавляется само: что закрепить, решаете вы.",
    f13Badge: "Автопринятие · Шаги цели",
    f14Title: "Не нагружает ПК",
    f14Desc: "Сделано, чтобы работать рядом с игрой всю сессию: около 0% CPU в простое и небольшой расход памяти. Цены барахолки готовятся только для вашего режима.",
    f14Badge: "~0% CPU в простое",
    hk5Desc: "Поиск цен барахолки прямо в рейде",
    hk6Desc: "Выбрать карту рейда: оверлей покажет её задания",
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
    footerDisclaimer: "Escape from Tarkov, имена и портреты торговцев являются собственностью Battlestate Games. Приложение является неофициальным некоммерческим инструментом.",
    navRead: "Чтение заданий",
    betaTag: "BETA",
    f15Title: "Чтение заданий из игры",
    f15Desc: "Откройте в игре экран заданий и один раз прокрутите его: трекер прочитает задания встроенным в Windows OCR. Подтвердите список, и все предыдущие задания будут отмечены выполненными. В игру ничего не добавляется.",
    f15Badge: "Windows OCR · 30 с →",
    rqTag: "Новое · BETA",
    rqTitle: "Прочитайте свои задания прямо из игры",
    rqSub: "Не нужно вводить десятки заданий вручную. Откройте в игре экран заданий, прокрутите его один раз, и трекер прочитает все названия. Подтвердите список: задания станут активными, а всё, что было до них, будет отмечено выполненным.",
    rqNotice: "⚠ Работает со встроенным в Windows распознаванием текста (OCR). Ничего не добавляет в игру: никаких модов, плагинов и инъекций.",
    rqNoticeSub: "Делает только снимок экрана: не читает память игры и не нажимает клавиши. Снимки остаются в памяти, не сохраняются и никуда не отправляются.",
    rq1Title: "1. Английский, Borderless",
    rq1Desc: "Поставьте английский язык игры и режим Borderless или Windowed. Названия сверяются с английскими.",
    rq2Title: "2. Откройте экран заданий",
    rq2Desc: "В игре откройте страницу TASKS и перейдите в Side tasks, чтобы список заданий был на экране.",
    rq3Title: "3. Запустите сканирование",
    rq3Desc: "Нажмите «Start scan» на странице Read Quests (BETA) и переключитесь в игру. Медленно прокрутите список до конца: он читается примерно каждые 0,7 с, а сканирование само остановится через 30 секунд. Короткий список? Ctrl+Shift+I читает только текущий экран.",
    rq4Title: "4. Проверьте и примените",
    rq4Desc: "Вернитесь в трекер, снимите отметки с неверно прочитанных и нажмите «Apply». Задания станут активными, предыдущие будут отмечены выполненными, а «Undo» всё отменит одним кликом.",
    rqCap: "Страница Read Quests (BETA): инструкция, обратный отсчёт и прочитанные из игры задания, ожидающие подтверждения.",
    rqNote: "BETA: лучше всего работает в 1080p и выше с английским языком игры. Ничего не меняется, пока вы не нажмёте «Apply», и используется только отмеченное.",
    tabRead: "Чтение заданий",
    capRead: "Read Quests (BETA): 30 секунд сканирования экрана заданий, затем подтвердите найденные задания. Уже активные помечены и не отмечены.",
    safeDoes5: "Read Quests (BETA) только делает снимок экрана и читает его встроенным в Windows OCR.",
    safeDoesNot5: "НЕ сохраняет и не загружает снимки экрана: они остаются только в памяти.",
    navRaid: "Авторейд",
    f16Title: "Автоопределение рейда (двойное O)",
    f16Desc: "Дважды нажмите O в рейде: трекер прочитает список выходов, узнает карту и начнёт рейд с оставшимся временем. Уже в рейде? Обновится только время. Без перехвата клавиатуры и без чтения клавиш.",
    f16Badge: "Список выходов · таймер рейда →",
    arTag: "Новое · BETA",
    arTitle: "Рейд начинается сам: дважды нажмите O",
    arSub: "Двойное O в рейде показывает ваши выходы справа вверху. Их названия говорят о карте, а таймер над ними — об оставшемся времени. Трекер читает и то и другое и выводит на оверлей задания этой карты и обратный отсчёт рейда.",
    arNotice: "⚠ Без перехвата клавиатуры и без чтения клавиш. Это не кейлоггер.",
    arNoticeSub: "Пока игра на переднем плане, время от времени читается только правый верхний угол экрана встроенным в Windows OCR. Снимок остаётся в памяти, не сохраняется и не отправляется, в игру ничего не передаётся.",
    ar1Title: "1. Включите",
    ar1Desc: "Переключатель Вкл / Выкл находится вверху страницы «Захожу в рейд» (по умолчанию включено). Запустите игру в режиме Borderless или Windowed Оверлей может оставаться в правом верхнем углу: он исключается из снимков экрана, и список под ним всё равно читается (в записях и стримах его тоже не видно).",
    ar2Title: "2. Дважды нажмите O",
    ar2Desc: "В рейде, как обычно, дважды нажмите O. Список выходов открывается в игре как раньше: трекер только смотрит на экран, клавиша по-прежнему идёт в игру.",
    ar3Title: "3. Ждёт загрузки списка",
    ar3Desc: "Не каждый ПК рисует список сразу. Как только появляются первые выходы, угол читается снова, пока не перестанут появляться новые (до ~3 с), и только потом определяется карта. Пустой угол значит, что вы не в рейде: ошибки нет.",
    ar4Title: "4. Карта и время на оверлее",
    ar4Desc: "Оверлей переключается на задания этой карты и ведёт обратный отсчёт времени рейда. Уже в рейде на ней? Двойное O только уточняет время и никогда не перезапускает рейд. Оверлей сам блокируется, чтобы клики шли в игру. Когда время выходит, рейд завершается сам. Одиночное O (только часы) уточняет время уже идущего рейда. Включён счётчик FPS? Часы под ним всё равно читаются, с точностью до минуты.",
    arLeft: "осталось",
    arResult: "Рейд начат · 5 заданий на оверлее",
    arCap: "Двойное O показывает выходы; трекер читает их и таймер рейда и начинает рейд на этой карте.",
    arNote: "BETA: нужен выход, который есть только на этой карте, а на больших картах — два выхода всего. Если нет уверенности, ничего не делается; карту всегда можно выбрать самому через Ctrl+Shift+R.",
    safeDoes6: "Автоопределение рейда читает только правый верхний угол экрана (Windows OCR), пока игра на переднем плане.",
    safeDoesNot6: "НЕ перехватывает клавиатуру и не читает нажатия клавиш.",
    faq6Q: "Определение по двойному O — это кейлоггер?",
    faq6A: "Нет. Приложение никогда не перехватывает клавиатуру и не читает клавиши: O, как всегда, идёт в игру. Оно лишь замечает, что на экране список выходов, читая правый верхний угол экрана через Windows OCR, пока игра на переднем плане. Выключить можно вверху страницы «Захожу в рейд».",
    faqRQ: "Безопасно ли читать задания с экрана? Могут ли забанить?",
    faqRA: "Работает как программа для скриншотов: делает снимок экрана и читает текст встроенным в Windows OCR. Никогда не трогает игру: без чтения памяти, инъекций и имитации клавиатуры или мыши; список вы прокручиваете сами. Снимки остаются в памяти, не сохраняются и не загружаются.",
    f17Title: "Ваша позиция на карте",
    f17Desc: "Сделайте скриншот в рейде своей клавишей скриншота: окно карты покажет, где вы и куда смотрите, ваш путь, выходы и точки активных заданий. Этаж выбирается по высоте. Читается только имя файла скриншота.",
    f17Badge: "Любая клавиша скриншота · карты tarkov.dev →",
    tabMap: "Карта",
    tabMapFollow: "Карта · Слежение",
    capMap: "Сделайте скриншот в рейде, и карта покажет, где вы: красная стрелка — куда вы смотрите, пунктир — ваш путь по прошлым скриншотам. На карте выходы (ЧВК / Дикий / общие, переходы) и точки активных заданий (жёлтые).",
    capMapFollow: "Режим слежения держит вид на вас, приближая до уровня улицы. Этаж выбирается по высоте; колесо — масштаб, перетаскивание — обзор, Esc возвращает в игру.",
    navMap: "GPS на карте",
    navGoons: "Goons",
    f18Title: "Трекер Goons",
    f18Desc: "Где последний раз видели Goons? Страница «Захожу в рейд» показывает последние сообщения с tarkov-goon-tracker.com для вашего режима (PvP или PvE), а карточка карты, где их видели, получает значок GOONS.",
    f18Badge: "PvP + PvE · каждые 5 мин →",
    mgTag: "Новое",
    mgTitle: "Где вы: просто сделайте скриншот",
    mgSub: "Потерялись на карте? Нажмите клавишу скриншота в рейде. Игра записывает вашу позицию в имя файла скриншота; трекер читает это имя, и открывается окно карты с вами: куда вы смотрите, ваш путь, выходы и точки активных заданий.",
    mgNotice: "✔ Читаются только имена файлов ваших скриншотов. В игру ничего не добавляется: ни модов, ни плагинов, ни инъекций.",
    mgNoticeSub: "Сама картинка не открывается, клавиши не читаются, в игру ничего не отправляется. Карты берутся с tarkov.dev.",
    mg1Title: "1. Зайдите в рейд",
    mg1Desc: "Карта следует за картой рейда — выбранной на «Захожу в рейд» или найденной двойным O. Не в рейде? Выберите карту в окне карты.",
    mg2Title: "2. Сделайте скриншот",
    mg2Desc: "Используйте свою клавишу скриншота в игре. Подойдёт любая: приложение следит за папкой скриншотов, а не за клавишей.",
    mg3Title: "3. Вы на карте",
    mg3Desc: "Стрелка показывает, где вы стоите и куда смотрите; прошлые скриншоты рисуют ваш путь. Этаж выбирается по высоте. Колесо — масштаб, перетаскивание — обзор, ◎ снова центрирует на вас.",
    mg4Title: "4. Esc — обратно в игру",
    mg4Desc: "Нажмите Esc или щёлкните по игре: карта закроется, клавиатура вернётся в игру. Сделайте новый скриншот, когда захотите узнать, где вы.",
    mgCap1: "Customs после скриншота: красная стрелка — это вы, пунктир — ваш путь. Выходы, переходы и точки заданий на карте.",
    mgCap2: "Страница In-Game GPS: один переключатель и четыре шага.",
    mgCap3: "Режим слежения держит вид на вас, приближая до уровня улицы.",
    mgModesTitle: "Три способа показать карту",
    mgModesSub: "Выберите один на странице In-Game GPS. Каждый скриншот передвигает вас на карте.",
    mgModeOvTitle: "Оверлей в игре",
    mgModeOvDesc: "Большое окно карты открывается поверх игры; Esc возвращает в игру.",
    mgModeMiniTitle: "Круглая миникарта",
    mgModeMiniDesc: "Круглая карта вокруг вас в углу экрана на несколько секунд. Выберите угол, размер и масштаб; север сверху или поворот вместе с вами. Мышь и клавиатуру она не забирает.",
    mgModeAppTitle: "В приложении",
    mgModeAppDesc: "Карта всегда открыта внутри приложения (In-Game GPS → Карта). Поставьте его на второй монитор: поверх игры ничего не появляется.",
    mgCap4: "Миникарта на Лесу после скриншота: вы в центре, вокруг выходы и маршрут БТР.",
    mgCap5: "С поворотом: направление взгляда сверху, буква N на кольце показывает север.",
    mgCap6: "В приложении: страница Карта под In-Game GPS, всегда открыта для второго монитора.",
    navMini: "Миникарта",
    mmTag: "Новое",
    mmTitle: "Круглая миникарта или карта на втором мониторе",
    mmSub: "Карта GPS теперь показывается так, как вам удобно: маленькая круглая миникарта в углу игры, окно карты поверх игры или карта внутри приложения на втором мониторе. Сделайте скриншот — и обновится только ваша позиция.",
    mmP1Title: "Скриншот — и взгляд",
    mmP1Desc: "Нажмите клавишу скриншота: миникарта появится на несколько секунд (от 3 до 30, на ваш выбор) и исчезнет.",
    mmP2Title: "Никогда не мешает",
    mmP2Desc: "Клики проходят прямо в игру; мышь и клавиатуру она не забирает.",
    mmP3Title: "Любой из 4 углов",
    mmP3Desc: "Слева сверху, справа сверху, слева снизу или справа снизу, с нужным размером и масштабом.",
    mmP4Title: "Север сверху или поворот с вами",
    mmP4Desc: "Север сверху, или карта поворачивается так, что направление взгляда всегда сверху; N на кольце показывает север.",
    mmP5Title: "Всё, что есть на большой карте",
    mmP5Desc: "Выходы, переходы, точки ваших квестов, БТР и ваш путь — на карте рейда, найденной по двойному O. Если точка квеста за пределами миникарты, на кольце появляется жёлтая стрелка в её сторону с расстоянием.",
    mmNote: "Как и окно карты, читает только имена файлов ваших скриншотов: никаких модов и инъекций. Запускайте игру в режиме Borderless или Windowed, чтобы миникарта была поверх.",
    f20Title: "Круглая миникарта",
    f20Desc: "Скриншот показывает круглую миникарту вокруг вас в углу игры на несколько секунд, клики проходят в игру. Или держите карту открытой в приложении на втором мониторе.",
    f20Badge: "4 угла · поворот с вами · второй монитор →",
    mgNote: "Совет: если папка скриншотов не стандартная, укажите её на странице In-Game GPS. Запускайте игру в режиме Borderless или Windowed, чтобы окно карты было поверх.",
    gtTag: "Новое",
    gtTitle: "Где сейчас Goons?",
    gtSub: "Knight, Big Pipe и Birdeye ходят по Customs, Woods, Shoreline и Lighthouse. Трекер показывает, где игроки видели их последний раз, — вы знаете, какую карту выбрать ради них или какой избегать.",
    gt1Title: "PvP и PvE отдельно",
    gt1Desc: "Сообщения следуют переключателю PvP | PvE вверху: в PvP — сообщения PvP, в PvE — PvE.",
    gt2Title: "Последнее место + 5 сообщений",
    gt2Desc: "На «Захожу в рейд»: карта, где их видели последний раз, и как давно, плюс 5 последних сообщений. Карточка этой карты получает значок GOONS.",
    gt3Title: "Всегда свежее",
    gt3Desc: "Обновляется каждые 5 минут, пока приложение работает, или сразу кнопкой ⟳. «Goon Tracker ↗» открывает сайт для этого режима.",
    gtCap: "Карточка Goons на «Захожу в рейд»: где их видели последний раз, последние сообщения и значок GOONS на карте.",
    gtNote: "Сообщения присылают игроки на tarkov-goon-tracker.com, поэтому они могут быть неточными или запоздалыми. Трекер только читает эту публичную страницу; из вашей игры ничего не отправляется.",
    tabGoons: "Goons",
    tabMini: "Миникарта",
    capMini: "Круглая миникарта в углу игры после скриншота: вы в центре, вокруг выходы и маршрут БТР. Клики проходят в игру.",
    tabInApp: "Карта · В приложении",
    capInApp: "Карта внутри приложения (In-Game GPS → Карта), всегда открыта для второго монитора: каждый скриншот передвигает вас на ней.",
    capGoons: "Трекер Goons: где их видели последний раз в вашем режиме (PvP или PvE), последние сообщения и значок GOONS на карточке карты.",
    navBtr: "BTR",
    f19Title: "Трекер BTR",
    f19Desc: "Где сейчас BTR и когда он доедет до следующей остановки? На Woods, Streets и Lighthouse трекер считает это по времени рейда: следующая остановка и ETA на оверлее, маршрут и BTR в окне карты, все остановки на странице рейда.",
    f19Badge: "Woods · Streets · Lighthouse →",
    btTag: "Новое",
    btTitle: "Где BTR и когда он приедет?",
    btSub: "На Woods, Streets of Tarkov и Lighthouse BTR ездит по фиксированным маршрутам по расписанию: появляется в определённое время рейда, стоит на каждой остановке и идёт на новый круг. По оставшемуся времени рейда трекер оценивает, где он и когда доедет до каждой остановки.",
    bt1Title: "1. Время рейда",
    bt1Desc: "Двойное O в рейде: карта и оставшееся время читаются из списка выходов. С этого начинается расписание BTR.",
    bt2Title: "2. Следующая остановка на оверлее",
    bt2Desc: "Одна строка под временем рейда: куда едет BTR и его ETA, или на какой остановке он стоит и сколько ещё.",
    bt3Title: "3. На карте",
    bt3Desc: "Сделайте скриншот: окно карты покажет возможные маршруты, каждую остановку с ETA и где BTR должен быть сейчас.",
    bt4Title: "4. Видите его? Нажмите Seen",
    bt4Desc: "На странице рейда нажмите Seen у остановки, где видите BTR: время подстроится, а неподходящие маршруты отпадут.",
    btNote: "Время — оценка (появление плавает примерно на минуту); одно нажатие Seen заметно его уточняет. Каждый вид отключается в настройках. Из игры не читается ничего, кроме времени рейда, которое и так читает двойное O.",
    safeDoes7: "Карта читает только имена файлов скриншотов, которые вы делаете сами (игра записывает в них вашу позицию); изображения не открываются.",
    faq7Q: "Как карта узнаёт, где я? Какую клавишу нажимать?",
    faq7A: "Когда вы делаете скриншот в рейде, игра сама записывает вашу позицию в имя файла. Приложение следит за папкой скриншотов (Документы\\Escape from Tarkov\\Screenshots) и читает это имя, поэтому работает любая клавиша скриншота, назначенная в игре: дополнительной горячей клавиши нет. Изображение не открывается, игра никак не затрагивается."
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
