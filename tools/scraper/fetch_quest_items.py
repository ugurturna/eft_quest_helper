"""Items each quest asks you to find / hand over / plant -> data/quest_items.json + data/icons/{itemId}.png

    python tools/scraper/fetch_quest_items.py

Source: json.tarkov.dev static dumps (same data as the GraphQL API, which is often down).
Quests are matched to ours by wiki link, then by name. Needs Pillow (webp -> png).
"""
import io
import re
import json
import sys
import time
import urllib.request
from difflib import SequenceMatcher
from pathlib import Path

from PIL import Image

from merge import name_key, wiki_key

BASE = "https://json.tarkov.dev"
ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / "data"
ICONS = DATA / "icons"
MODES = {"pvp": "regular", "pve": "pve"}
LANGS = ["en", "tr"]
# findItem only links an icon to its objective; the item lists use giveItem/plantItem
TYPES = {"giveItem", "plantItem", "findItem"}
# how close a tarkov.dev objective text must be to ours to share its icon
MIN_RATIO = 0.6
# objectives that accept dozens of alternatives ("any weapon") only get icons for the first few
MAX_ICONS = 4


def get(url, as_json=True):
    req = urllib.request.Request(url, headers={"User-Agent": "TarkovQuestTracker/0.1"})
    for attempt in range(5):
        try:
            with urllib.request.urlopen(req, timeout=120) as r:
                body = r.read()
            return json.loads(body.decode("utf-8")) if as_json else body
        except Exception as e:  # noqa: BLE001 - network, retry with backoff
            wait = 3 * 2 ** attempt
            print(f"  {url}: {e} - retry in {wait}s", file=sys.stderr)
            time.sleep(wait)
    return None


def norm(text):
    return re.sub(r"[^a-z0-9]+", " ", (text or "").lower()).strip()


def stem(text):
    """norm() without plural s, with a leading space: "Bottles of vodka" -> " bottle of vodka"."""
    return "".join(" " + w.rstrip("s") for w in norm(text).split())


def match_objectives(ours, theirs, item_names):
    """tarkov.dev objectives -> our objective ids: best text match first, then any objective
    that names the item ("Obtain 1 Dorm room 303 key"); each of ours is used once."""
    pairs = sorted(((SequenceMatcher(None, norm(d), norm(o["text"])).ratio(), i, o["id"])
                    for i, d in enumerate(theirs) for o in ours), reverse=True)
    out, used = {}, set()
    for ratio, i, oid in pairs:
        if ratio < MIN_RATIO or i in out or oid in used:
            continue
        out[i] = oid
        used.add(oid)
    for i, name in enumerate(item_names):
        if i in out or len(norm(name)) < 3:
            continue
        hit = next((o["id"] for o in ours if o["id"] not in used and stem(name) in stem(o["text"])), None)
        if hit:
            out[i] = hit
            used.add(hit)
    return out


def raid_needs(td):
    """What to bring into the raid besides plantItem: keys, markers, the weapon to use, the clothes
    to wear -> (type, items, count, objective or None); items are alternatives, the first gets the icon."""
    out, keys = [], set()

    def key(alternatives):
        alternatives = [k for k in alternatives if k]
        if alternatives and tuple(alternatives) not in keys:
            keys.add(tuple(alternatives))
            out.append(("key", alternatives, 1, None))

    for needed in td.get("neededKeys") or []:
        for k in needed.get("keys") or []:
            key([k])
    for o in td["objectives"]:
        for alternatives in o.get("requiredKeys") or []:
            key(alternatives)
        if o["type"] == "mark" and o.get("markerItem"):
            out.append(("mark", [o["markerItem"]], o.get("count") or 1, o))
        if o.get("usingWeapon"):
            out.append(("useWeapon", o["usingWeapon"], 1, None))
        for group in o.get("wearing") or []:
            ids = [w["id"] if isinstance(w, dict) else w for w in group]
            if ids:
                out.append(("wear", ids, 1, None))
    return out


def needs(mode, tasks, task_names, ours, item_name):
    by_wiki = {wiki_key(t["wikiUrl"]): t for t in ours}
    by_name = {name_key(t["name"]): t for t in ours}
    out, unmatched = {}, []
    for td in tasks.values():
        objs = [o for o in td["objectives"] if o["type"] in TYPES and o.get("items")]
        extra = raid_needs(td)
        if not objs and not extra:
            continue
        name = task_names.get(td["name"], td["name"])
        quest = by_wiki.get(wiki_key(td.get("wikiLink"))) or by_name.get(name_key(name))
        if not quest:
            unmatched.append(name)  # not in tasks_en.json (yet)
            continue
        linked = match_objectives(quest["objectives"], [task_names.get(o["description"], "") for o in objs],
                                  [item_name(o["items"][0]) for o in objs])
        out[quest["id"]] = [{"type": o["type"], "items": o["items"], "count": o.get("count") or 1,
                             "fir": bool(o.get("foundInRaid")), "objective": linked.get(i)}
                            for i, o in enumerate(objs)]
        # markers also get an icon on their objective; keys / weapons / clothes only go to the "bring" list
        used = set(linked.values())
        marks = [(i, o) for i, (t, _, _, o) in enumerate(extra) if t == "mark"]
        mark_links = match_objectives([o for o in quest["objectives"] if o["id"] not in used],
                                      [task_names.get(o["description"], "") for _, o in marks],
                                      [item_name(extra[i][1][0]) for i, _ in marks])
        mark_at = {i: mark_links.get(k) for k, (i, _) in enumerate(marks)}
        out[quest["id"]] += [{"type": t, "items": items, "count": count, "fir": False, "objective": mark_at.get(i)}
                             for i, (t, items, count, _) in enumerate(extra)]
    if unmatched:
        print(f"{mode}: {len(unmatched)} tasks not matched: {', '.join(sorted(unmatched)[:10])}...")
    return out


def main():
    ours = json.loads((DATA / "tasks_en.json").read_text(encoding="utf-8"))
    result = {"items": {}, "modes": {}}
    names = {lang: (get(f"{BASE}/regular/items_{lang}") or {}).get("data", {}) for lang in LANGS}
    items = get(f"{BASE}/regular/items")["data"]["items"]

    def item_name(item_id):
        return names["en"].get(items.get(item_id, {}).get("name"), "")

    for mode, path in MODES.items():
        tasks = get(f"{BASE}/{path}/tasks")
        task_names = (get(f"{BASE}/{path}/tasks_en") or {}).get("data", {})
        if not tasks:
            print(f"FAILED {mode} tasks")
            sys.exit(1)
        result["modes"][mode] = needs(mode, tasks["data"]["tasks"], task_names, ours, item_name)
        print(f"{mode}: {len(result['modes'][mode])} quests need items")

    wanted = {}  # item id -> show an icon somewhere
    for objs in (o for m in result["modes"].values() for o in m.values()):
        for o in objs:
            for k, i in enumerate(o["items"]):
                wanted[i] = wanted.get(i, False) or k < MAX_ICONS
    for item_id in wanted:
        it = items.get(item_id)
        if not it:
            continue
        result["items"][item_id] = {
            "name": {lang: names[lang][it["name"]] for lang in LANGS if names[lang].get(it["name"])},
            "short": {lang: names[lang][it["shortName"]] for lang in LANGS if names[lang].get(it["shortName"])},
        }

    ICONS.mkdir(parents=True, exist_ok=True)
    fetched = 0
    for item_id, show in wanted.items():
        target = ICONS / f"{item_id}.png"
        if not show or target.exists() or item_id not in items:
            continue
        raw = get(items[item_id]["iconLink"], as_json=False)
        if raw:
            Image.open(io.BytesIO(raw)).convert("RGBA").save(target, optimize=True)
            fetched += 1
    print(f"items: {len(result['items'])}, new icons: {fetched}")

    (DATA / "quest_items.json").write_text(json.dumps(result, ensure_ascii=False, separators=(",", ":")),
                                           encoding="utf-8")


if __name__ == "__main__":
    main()
