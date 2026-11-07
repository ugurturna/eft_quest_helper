"""Escape from Tarkov Fandom wiki -> data/tasks_en.json

Uses the MediaWiki API (regular /wiki/ pages return 403 for scripts).
Only stdlib. Raw wikitext is cached under tools/scraper/cache/ so re-runs are offline.

    python tools/scraper/scrape_wiki.py            # use cache when present
    python tools/scraper/scrape_wiki.py --refresh  # re-download everything
"""
import argparse
import json
import re
import sys
import time
import urllib.parse
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

API = "https://escapefromtarkov.fandom.com/api.php"
WIKI = "https://escapefromtarkov.fandom.com/wiki/"
UA = "TarkovQuestTracker/0.1 (personal quest tracker; low-rate)"
ROOT = Path(__file__).resolve().parents[2]
CACHE = Path(__file__).resolve().parent / "cache"
OUT = ROOT / "data"

TRADERS = ["Prapor", "Therapist", "Fence", "Skier", "Peacekeeper", "Mechanic",
           "Ragman", "Jaeger", "Ref", "Lightkeeper", "BTR Driver"]
MAPS = ["Customs", "Factory", "Woods", "Shoreline", "Interchange", "Reserve", "The Lab",
        "Lighthouse", "Streets of Tarkov", "Ground Zero", "The Labyrinth", "Terminal"]
MAP_ALIASES = {"Daytime Factory": "Factory", "Night Factory": "Factory",
               "Labs": "The Lab", "Laboratory": "The Lab", "Streets": "Streets of Tarkov",
               "Labyrinth": "The Labyrinth"}



def api_get(params):
    params = {**params, "format": "json", "formatversion": "2"}
    url = API + "?" + urllib.parse.urlencode(params)
    req = urllib.request.Request(url, headers={"User-Agent": UA})
    for attempt in range(4):
        try:
            with urllib.request.urlopen(req, timeout=60) as r:
                return json.loads(r.read().decode("utf-8"))
        except Exception as e:  # noqa: BLE001 - network, retry with backoff
            print(f"  retry {attempt + 1}: {e}", file=sys.stderr)
            time.sleep(2 ** attempt * 2)
    raise RuntimeError(f"failed: {url}")


def cache_path(title):
    safe = re.sub(r'[<>:"/\\|?*]', "_", title)
    return CACHE / f"{safe}.wiki"


def fetch_pages(titles, refresh):
    """Returns {requested title: (final title, wikitext)}; batches 50 titles per request."""
    CACHE.mkdir(exist_ok=True)
    result, todo = {}, []
    for t in dict.fromkeys(titles):
        p = cache_path(t)
        if p.exists() and not refresh:
            final, _, text = p.read_text(encoding="utf-8").partition("\n")
            result[t] = (final, text)
        else:
            todo.append(t)
    for i in range(0, len(todo), 50):
        chunk = todo[i:i + 50]
        print(f"  fetching {i + len(chunk)}/{len(todo)}")
        data = api_get({"action": "query", "prop": "revisions", "rvprop": "content",
                        "rvslots": "main", "redirects": "1", "titles": "|".join(chunk)})
        q = data.get("query", {})
        alias = {}
        for kind in ("normalized", "redirects"):
            for n in q.get(kind, []):
                alias[n["to"]] = alias.get(n["from"], n["from"])
        for page in q.get("pages", []):
            if "missing" in page:
                continue
            text = page["revisions"][0]["slots"]["main"]["content"]
            final = page["title"]
            req = final
            while req in alias:
                req = alias[req]
            cache_path(req).write_text(final + "\n" + text, encoding="utf-8")
            result[req] = (final, text)
        time.sleep(1)
    return result



LINK = re.compile(r"\[\[([^\]|#]+)(?:#[^\]|]*)?(?:\|([^\]]*))?\]\]")


def links(text):
    return [m.group(1).strip() for m in LINK.finditer(text or "")]


def plain(text):
    """Very small wikitext -> plain text."""
    t = text or ""
    t = re.sub(r"<!--.*?-->", "", t, flags=re.S)
    t = re.sub(r"<gallery[^>]*>.*?</gallery>", "", t, flags=re.S | re.I)
    # categories and interlanguage links ([[Category:Quests]], [[ru:...]], [[pt-br:...]])
    t = re.sub(r"\[\[(?:Category|[a-z]{2}(?:-[a-z]{2})?):[^\]]*\]\]", "", t)
    t = re.sub(r"\[\[(?:File|Image):[^\]]*\]\]", "", t)
    t = re.sub(r"^(?:File|Image):[^\n]*$", "", t, flags=re.M)
    t = LINK.sub(lambda m: (m.group(2) or m.group(1)).strip(), t)
    t = re.sub(r"\[https?://\S+ ([^\]]+)\]", r"\1", t)
    t = re.sub(r"\{\{[^{}]*\}\}", "", t)
    t = re.sub(r"<br\s*/?>", " ", t)
    t = re.sub(r"<[^>]+>", "", t)
    t = t.replace("'''", "").replace("''", "").replace("&nbsp;", " ")
    return re.sub(r"[ \t]+", " ", t).strip()


def sections(text):
    """{lowercase heading: body} for == level 2 == headings."""
    out = {}
    parts = re.split(r"^==\s*([^=].*?)\s*==\s*$", text, flags=re.M)
    for i in range(1, len(parts) - 1, 2):
        out[parts[i].strip().lower()] = parts[i + 1]
    return out


def infobox(text):
    m = re.search(r"\{\{Infobox quest(.*?)\n\}\}", text, flags=re.S | re.I)
    box = {}
    if not m:
        return box
    for line in m.group(1).split("\n|"):
        k, sep, v = line.partition("=")
        if sep:
            box[k.strip().lstrip("|").strip().lower()] = v.strip()
    return box


def bullets(body):
    out = []
    for line in (body or "").split("\n"):
        m = re.match(r"^(\*+)\s*(.*)", line)
        if m and m.group(2).strip():
            out.append((len(m.group(1)), m.group(2).strip()))
    return out


def slug(title):
    s = title.lower().replace("&", "and")
    s = re.sub(r"[’']", "", s)
    s = re.sub(r"[^a-z0-9]+", "-", s)
    return s.strip("-")


def norm_map(name):
    name = MAP_ALIASES.get(name, name)
    return name if name in MAPS else None



def quest_list(refresh):
    """Trader (side) quests from the Quests page tabs: [(title, trader)]."""
    page = fetch_pages(["Quests"], refresh)["Quests"][1]
    tabs = page.split('<div class="wds-tab__content')[1:len(TRADERS) + 1]
    out = []
    for trader, tab in zip(TRADERS, tabs):
        for name in re.findall(r"\n\|-\s*\n\|\s*\[\[([^\]|]+)", tab):
            out.append((name.strip(), trader))
    return out


def story_list(refresh):
    """Story chapters from the 'Story chapters' table (second column links)."""
    page = fetch_pages(["Story chapters"], refresh)["Story chapters"][1]
    return list(dict.fromkeys(m.strip() for m in re.findall(r"\n\|\s*\[\[([^\]|:]+)(?:\|[^\]]*)?\]\]\s*\n", page)))


def category_members(category, refresh):
    """All page titles in a wiki category (cached as a JSON list)."""
    path = CACHE / f"_category_{category}.json"
    if path.exists() and not refresh:
        return json.loads(path.read_text(encoding="utf-8"))
    titles, cont = [], {}
    while True:
        data = api_get({"action": "query", "list": "categorymembers", "cmtitle": f"Category:{category}",
                        "cmlimit": "500", "cmnamespace": "0", **cont})
        titles += [m["title"] for m in data["query"]["categorymembers"]]
        if "continue" not in data:
            break
        cont = data["continue"]
    CACHE.mkdir(exist_ok=True)
    path.write_text(json.dumps(titles, ensure_ascii=False), encoding="utf-8")
    return titles


def event_names(refresh):
    """{quest title: event heading} from the Events page (newest event wins)."""
    page = fetch_pages(["Events"], refresh)["Events"][1]
    out = {}
    parts = re.split(r"^==\s*([^=].*?)\s*==\s*$", page, flags=re.M)
    for i in range(1, len(parts) - 1, 2):
        for l in links(parts[i + 1]):
            out.setdefault(l, parts[i].strip())
    return out


def page_flags(text):
    return {"event": "{{Event content" in text, "historical": "{{Historical content" in text}


def parse_objectives(body):
    objs = []
    for depth, raw in bullets(body):
        text = plain(raw)
        if not text:
            continue
        if depth > 1 and objs:  # sub-bullets are details of the previous objective
            objs[-1]["details"].append(text)
            continue
        maps = [m for m in (norm_map(l) for l in links(raw)) if m]
        count = re.search(r"\b(\d[\d,]*)\b", text)
        objs.append({
            "id": f"o{len(objs) + 1}",
            "text": text,
            "count": int(count.group(1).replace(",", "")) if count else 1,
            "optional": "(optional)" in text.lower(),
            "fir": "in raid" in text.lower(),
            "map": maps[0] if maps else None,
            "pveOnly": bool(re.search(r"\bPvE\b", raw)),
            "details": [],
        })
    return objs


def parse_required_items(guide):
    items = []
    m = re.search(r"Related Quest Items(.*?)\n\|\}", guide or "", flags=re.S)
    if not m:
        return items
    for row in m.group(1).split("\n|-")[1:]:
        cells = [c.strip() for c in re.split(r"\n[|!]", "\n" + row.strip()) if c.strip()]
        if len(cells) < 4 or not links(cells[1]):
            continue
        amount = re.search(r"\d+", plain(cells[2]))
        items.append({
            "name": links(cells[1])[0],
            "count": int(amount.group()) if amount else 1,
            "requirement": plain(cells[3]),
            "fir": "yes" in plain(cells[4]).lower() if len(cells) > 4 else False,
        })
    return items


def parse_rewards(body):
    lines = [plain(r) for d, r in bullets(body) if d == 1]
    xp = 0
    for l in lines:
        m = re.match(r"\+?([\d,]+)\s*EXP", l)
        if m:
            xp = int(m.group(1).replace(",", ""))
    rep = []
    for l in lines:
        m = re.match(r"(.+?) Rep\s*([+-][\d.]+)", l)
        if m:
            rep.append({"trader": m.group(1).strip(), "value": float(m.group(2))})
    return {"xp": xp, "rep": rep, "items": [l for l in lines if "EXP" not in l and " Rep " not in l]}


def parse_requirements(body):
    """Requirements section: level (+ PvE level) + extra prerequisite quests."""
    level, pve_level, quests, other = None, None, [], []
    for depth, raw in bullets(body):
        text = plain(raw)
        m = re.search(r"level (\d+)", text, flags=re.I)
        if m and "loyalty" not in text.lower():
            level = int(m.group(1))
            pm = re.search(r"\((\d+) in the PvE mode\)", text)
            if pm:
                pve_level = int(pm.group(1))
        if depth > 1 or re.search(r"complete|finish", text, flags=re.I):
            quests += links(raw)
        if depth == 1 and not re.search(r"complete the quests?:?$", text, flags=re.I):
            other.append(text)
    return level, pve_level, quests, other


def mode_info(text):
    low = text.lower()
    modes = ["pvp", "pve"]
    if re.search(r"(?:quest|task) is only (?:available|obtainable)[^.]{0,40}\bpve\b", low):
        modes = ["pve"]
    elif re.search(r"(?:quest|task) is (?:not available|unavailable)[^.]{0,40}\bpve\b", low):
        modes = ["pvp"]
    notes = []
    for line in text.split("\n"):
        if re.search(r"\bPvE\b", line):
            p = plain(re.sub(r"^[*:#]+", "", line))
            if p:
                notes.append(p)
    return modes, notes


def parse_quest(title, trader, text):
    box, sec = infobox(text), sections(text)
    level, pve_level, req_quests, req_other = parse_requirements(sec.get("requirements", ""))
    objectives = parse_objectives(sec.get("objectives", ""))
    # "previous" may say "Accept [[X]]": X only has to be accepted, not completed - not a prerequisite
    prev, accept = [], []
    for part in re.split(r"<br\s*/?>|\n", box.get("previous", "")):
        (accept if re.match(r"\s*accept\b", part, flags=re.I) else prev).extend(
            l for l in links(part) if l != title)
    req_other += [f"Accept: {a}" for a in accept]
    guide = sec.get("guide", "")
    notes_parts = [plain(sec[k]) for k in ("notes", "trivia") if k in sec]
    guide = re.sub(r"\{\|.*?\n\|\}", "", guide, flags=re.S)
    guide = re.sub(r"^=+\s*(.*?)\s*=+\s*$", r"▸ \1", guide, flags=re.M)  # sub-headings -> "▸ title"
    guide_text = re.sub(r"\n{3,}", "\n\n", plain(guide)).strip()
    modes, pve_notes = mode_info(text)
    loc_links = [m for m in (norm_map(l) for l in links(box.get("location", ""))) if m]
    loc_plain = plain(box.get("location", ""))
    kappa_text = plain(box.get("reqkappa", "")).lower()
    return {
        "title": title,
        "kappa": True if kappa_text.startswith("yes") else False if kappa_text.startswith("no") else None,
        "trader": plain(box.get("given by", "")) or trader,
        "map": (sorted(set(loc_links))
                or (["Any"] if "any" in loc_plain.lower() else [])
                or sorted({o["map"] for o in objectives if o["map"]})),
        "minLevel": level,
        "pveMinLevel": pve_level,
        "traderLoyalty": plain(box.get("ll requirement", "")) or None,
        "prevTitles": prev,
        "reqTitles": req_quests,
        "leadsToTitles": links(box.get("leads to", "")),
        "relatedTitles": links(box.get("related", "")),
        "requirementsText": req_other,
        "objectives": objectives,
        "requiredItems": parse_required_items(guide),
        "rewards": parse_rewards(sec.get("rewards", "")),
        "guide": guide_text,
        "notes": "\n\n".join(p for p in notes_parts if p),
        "gameModes": modes,
        "pveNotes": pve_notes,
    }



def prereq_closure(by_id, start):
    seen, stack = set(), [start]
    while stack:
        for p in by_id[stack.pop()]["prerequisites"]:
            if p not in seen:
                seen.add(p)
                stack.append(p)
    return seen


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--refresh", action="store_true")
    args = ap.parse_args()

    # side = trader quests, story = story chapters, event = pages marked {{Event content}}
    side = quest_list(args.refresh)
    story = story_list(args.refresh)
    known = {n for n, _ in side} | set(story)
    others = [t for t in category_members("Quests", args.refresh) if t not in known and t != "Quests"]
    print(f"side: {len(side)}, story: {len(story)}, other quest pages to classify: {len(others)}")
    pages = fetch_pages([n for n, _ in side] + story + others, args.refresh)
    events = event_names(args.refresh)

    entries = [(n, t, "side") for n, t in side] + [(n, "", "story") for n in story]
    skipped_removed = []
    for n in others:
        if n not in pages:
            continue
        flags = page_flags(pages[n][1])
        if flags["event"]:
            entries.append((n, "", "event"))
        else:
            skipped_removed.append(n)  # historical/removed quests that can no longer be done

    raw, title_to_id = [], {}
    for name, trader, category in entries:
        if name not in pages:
            print(f"  missing page: {name}", file=sys.stderr)
            continue
        final, text = pages[name]
        q = parse_quest(final, trader, text)
        q["id"] = slug(final)
        if q["id"] in {r["id"] for r in raw}:
            continue
        q["category"] = category
        if category == "story" and not q["trader"]:
            q["trader"] = "Story"
        if category == "event":
            q["eventName"] = events.get(name) or events.get(final)
            q["eventActive"] = not page_flags(text)["historical"]
        title_to_id[name] = title_to_id[final] = q["id"]
        raw.append(q)

    def ids(titles):
        return sorted({title_to_id[t] for t in titles if t in title_to_id})

    tasks, unresolved = [], set()
    for q in raw:
        prereq = [i for i in ids(q["prevTitles"] + q["reqTitles"]) if i != q["id"]]
        unresolved |= {t for t in q["prevTitles"] if t not in title_to_id}
        tasks.append({
            "id": q["id"],
            "name": q["title"],
            "trader": q["trader"],
            "map": q["map"],
            "minLevel": q["minLevel"],
            "traderLoyalty": q["traderLoyalty"],
            "category": q["category"],
            "eventName": q.get("eventName"),
            "eventActive": q.get("eventActive"),
            "kappaRequired": q["kappa"],    # infobox "reqkappa"; missing ones filled by merge.py (tarkov.dev)
            "lightkeeperRequired": None,
            "gameModes": q["gameModes"],
            "prerequisites": prereq,
            "leadsTo": ids(q["leadsToTitles"]),
            "related": ids(q["relatedTitles"]),
            "requirementsText": q["requirementsText"],
            "objectives": q["objectives"],
            "requiredItems": q["requiredItems"],
            "rewards": q["rewards"],
            "guide": q["guide"],
            "notes": q["notes"],
            "modeNotes": {"pve": q["pveNotes"]} if q["pveNotes"] else {},
            "modeOverrides": {"pve": {"minLevel": q["pveMinLevel"]}} if q["pveMinLevel"] else {},
            "wikiUrl": WIKI + urllib.parse.quote(q["title"].replace(" ", "_")),
            "sources": {"base": "fandom", **({"kappaRequired": "fandom"} if q["kappa"] is not None else {})},
            "tarkovDevId": None,
        })

    # "leads to" on A means A is a prerequisite of B, even if B's "previous" is empty.
    by_id = {t["id"]: t for t in tasks}
    for t in tasks:
        for nxt in t["leadsTo"]:
            target = by_id[nxt]
            if t["id"] not in target["prerequisites"] and nxt not in prereq_closure(by_id, t["id"]):
                target["prerequisites"].append(t["id"])

    OUT.mkdir(exist_ok=True)
    (OUT / "tasks_en.json").write_text(json.dumps(tasks, ensure_ascii=False, indent=1), encoding="utf-8")
    meta_path = OUT / "meta.json"
    meta = json.loads(meta_path.read_text(encoding="utf-8")) if meta_path.exists() else {}
    meta["fandom"] = {"scrapedAt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
                      "source": WIKI + "Quests", "count": len(tasks)}
    meta_path.write_text(json.dumps(meta, indent=1), encoding="utf-8")
    print(f"wrote {len(tasks)} tasks -> data/tasks_en.json")
    for cat in ("side", "story", "event"):
        print(f"  {cat}: {sum(t['category'] == cat for t in tasks)}")
    print(f"  active events: {sum(bool(t['eventActive']) for t in tasks)}, "
          f"kappa known from wiki: {sum(t['kappaRequired'] is not None for t in tasks)}, "
          f"removed quests skipped: {len(skipped_removed)}")
    if unresolved:
        print(f"unresolved 'previous' links ({len(unresolved)}): {sorted(unresolved)[:30]}")


if __name__ == "__main__":
    main()
