"""Map geometry + markers per map -> data/maps.json

    python tools/scraper/fetch_maps.py

The map window puts the player (from the position the game writes into a screenshot's file name) on
the map. It needs, per map:
- how game coordinates land on the map picture (tarkov.dev's interactive map settings: transform,
  rotation, bounds, the SVG / tile picture and its floors), from the tarkov-dev repo's maps.json;
- exfils and transits (json.tarkov.dev maps dump);
- quest objective spots (zones and quest item spots, json.tarkov.dev tasks dump), by tarkov.dev quest id.
Positions are kept as game coordinates [x, y (height), z].
"""
import json
import sys
import time
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

BASE = "https://json.tarkov.dev"
DEV_MAPS = "https://raw.githubusercontent.com/the-hideout/tarkov-dev/main/src/data/maps.json"
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "data" / "maps.json"

# tarkov.dev map name -> the map name the quests use; None = not a raid map (same as fetch_extracts.py)
VARIANTS = {
    "Night Factory": "Factory",
    "Infected Factory": "Factory",
    "Ground Zero 21+": "Ground Zero",
    "Ground Zero Tutorial": None,
    "The Lab (Dark)": "The Lab",
    "Infected Lab": "The Lab",
}
# interactive map settings kept as they are
MAP_FIELDS = ["transform", "coordinateRotation", "bounds", "svgPath", "svgBounds", "svgLayer",
              "tilePath", "tileSize", "minZoom", "maxZoom", "heightRange"]


def fetch(url):
    req = urllib.request.Request(url, headers={"User-Agent": "TarkovQuestTracker/0.1"})
    for attempt in range(5):
        try:
            with urllib.request.urlopen(req, timeout=120) as r:
                return json.loads(r.read().decode("utf-8"))
        except Exception as e:  # noqa: BLE001 - network, retry with backoff
            wait = 3 * 2 ** attempt
            print(f"  {url}: {e} - retry in {wait}s", file=sys.stderr)
            time.sleep(wait)
    return None


def get(path):
    data = fetch(f"{BASE}/{path}")
    return data["data"] if data else None


def xyz(p):
    return [round(p["x"], 2), round(p["y"], 2), round(p["z"], 2)]


def interactive(dev_maps):
    """normalizedName (and its variants) -> tarkov-dev interactive map entry"""
    found = {}
    for group in dev_maps:
        for m in group.get("maps") or []:
            if m.get("projection") != "interactive" or not m.get("transform"):
                continue
            if not m.get("svgPath") and not m.get("tilePath"):
                continue
            for name in [group["normalizedName"], m["key"], *(m.get("altMaps") or [])]:
                found.setdefault(name, m)
    return found


def geometry(m):
    out = {k: m[k] for k in MAP_FIELDS if m.get(k) is not None}
    out["layers"] = [
        {k: v for k, v in {
            "name": layer["name"],
            "svgLayer": layer.get("svgLayer"),
            "tilePath": layer.get("tilePath"),
            "extents": [{"height": e["height"],
                         "bounds": [b[:2] for b in e.get("bounds") or []]}
                        for e in layer.get("extents") or []],
        }.items() if v is not None}
        for layer in m.get("layers") or []
        if layer.get("svgLayer") or layer.get("tilePath")
    ]
    return out


def xyz1(p):
    return [round(p["x"], 1), round(p["y"], 1), round(p["z"], 1)]


class Names:
    """English + Turkish names of the keys, loose loot items, bosses and container types the maps use.
    `table` is {id: [en, tr]} (tr left out when it is the same); `items` are the loose loot item ids, which
    the maps' loose spots point into by index."""

    def __init__(self, maps_en, maps_tr, items_en, items_tr):
        self.maps_en, self.maps_tr, self.items_en, self.items_tr = maps_en, maps_tr, items_en, items_tr
        self.table = {}
        self.items = []
        self._index = {}

    def _put(self, key, en, tr):
        if key in self.table or not en:
            return
        self.table[key] = [en] if not tr or tr == en else [en, tr]

    def item(self, item_id):
        """An item (key or loot) by its id; its short name too, for the search box."""
        en = self.items_en.get(f"{item_id} Name")
        self._put(item_id, en, self.items_tr.get(f"{item_id} Name"))
        short = self.items_en.get(f"{item_id} ShortName")
        if en and short and short != en:
            self._put(f"{item_id} short", short, self.items_tr.get(f"{item_id} ShortName"))
        return item_id

    def loose(self, item_id):
        if item_id not in self._index:
            self._index[item_id] = len(self.items)
            self.items.append(self.item(item_id))
        return self._index[item_id]

    def label(self, key, text_key):
        """A boss or container type, by the key the maps dump's translation tables use."""
        self._put(key, self.maps_en.get(text_key, key), self.maps_tr.get(text_key))
        return key


# spawn sides/categories -> who spawns there; boss spots are in the boss list
def spawn_kind(s):
    cats, sides = s.get("categories") or [], s.get("sides") or []
    if "botpmc" in cats:
        return "aipmc"
    if ("pmc" in sides or "all" in sides) and "player" in cats:
        return "pmc"  # "all": a spot PMCs and player scavs share (Factory)
    if "scav" in sides and ({"bot", "player", "all"} & set(cats)):
        return "scav"
    return None


def escorts(boss):
    """Most escorts the boss can come with."""
    return sum(max((a.get("count") or 0) for a in e.get("amount") or [{}]) for e in boss.get("escorts") or [])


def layers(m, dump, names):
    """The optional map layers: spawns, bosses, locked doors, containers, loose loot, hazards."""
    spawns = m.get("spawns") or []
    out = {"spawns": [], "bosses": [], "locks": [], "containers": [], "loose": [], "hazards": []}
    for s in spawns:
        kind = spawn_kind(s)
        if kind and s.get("position"):
            out["spawns"].append({"kind": kind, "zone": s.get("zoneName") or "", "position": xyz1(s["position"])})

    bosses = {}
    for b in m.get("bosses") or []:
        mob = b.get("mob")
        if not mob or b.get("spawnChance", 0) <= 0:
            continue
        positions = []
        for loc in b.get("spawnLocations") or []:
            # no spots of its own: the zone's boss spots, else any spot in the zone
            zone = [s for s in spawns if s.get("zoneName") == loc.get("spawnKey") and s.get("position")]
            spots = (loc.get("positions")
                     or [s["position"] for s in zone if "boss" in (s.get("categories") or [])]
                     or [s["position"] for s in zone])
            positions.extend(xyz1(p) for p in spots)
        if not positions:
            continue
        entry = bosses.setdefault(mob, {"name": names.label(mob, mob), "chance": 0, "escorts": 0, "positions": []})
        entry["chance"] = max(entry["chance"], round(b["spawnChance"], 2))
        entry["escorts"] = max(entry["escorts"], escorts(b))
        entry["positions"] = [list(p) for p in dict.fromkeys(map(tuple, entry["positions"] + positions))]
    out["bosses"] = list(bosses.values())

    for lock in m.get("locks") or []:
        if not lock.get("position") or not lock.get("key"):
            continue
        out["locks"].append({"type": lock.get("lockType") or "door", "key": names.item(lock["key"]),
                             "power": bool(lock.get("needsPower")), "position": xyz1(lock["position"])})

    types = dump.get("lootContainers") or {}
    for c in m.get("lootContainers") or []:
        t = types.get(c.get("lootContainer"))
        if t and c.get("position"):
            out["containers"].append({"type": names.label(t["normalizedName"], t["name"]),
                                      "position": xyz1(c["position"])})

    for spot in m.get("lootLoose") or []:
        if spot.get("position") and spot.get("items"):
            out["loose"].append({"position": xyz1(spot["position"]),
                                 "items": [names.loose(i) for i in dict.fromkeys(spot["items"])]})

    for h in m.get("hazards") or []:
        if h.get("hazardType") and h.get("outline"):
            out["hazards"].append({"type": h["hazardType"],
                                   "outline": [[round(p["x"], 1), round(p["z"], 1)] for p in h["outline"]]})
    return out


def main():
    dev_maps = fetch(DEV_MAPS)
    dump = get("regular/maps")
    en = get("regular/maps_en") or {}
    tasks = get("regular/tasks")
    tasks_en = get("regular/tasks_en") or {}
    if dev_maps is None or dump is None:
        print("FAILED (tarkov-dev maps.json or json.tarkov.dev maps unavailable)")
        sys.exit(1)
    settings = interactive(dev_maps)
    names = Names(en, get("regular/maps_tr") or {}, get("regular/items_en") or {}, get("regular/items_tr") or {})

    maps = {}      # our map name -> entry
    by_id = {}     # tarkov.dev map id -> our map name
    for m in dump["maps"].values():
        title = en.get(m["name"], m["name"])
        title = VARIANTS.get(title, title)
        if title is None:
            continue
        by_id[m["id"]] = title
        if title in maps:
            continue  # a variant: the base map has the same layout
        setting = settings.get(m["normalizedName"])
        if setting is None:
            print(f"  {title}: no interactive map on tarkov.dev, skipped", file=sys.stderr)
            continue
        entry = {"key": setting["key"], **geometry(setting)}
        entry["extracts"] = [{"name": en.get(x["name"], x["name"]), "faction": x.get("faction") or "shared",
                              "position": xyz(x["position"])}
                             for x in m.get("extracts") or [] if x.get("position")]
        entry["transits"] = [{"name": en.get(x["description"], x["description"]), "position": xyz(x["position"])}
                             for x in m.get("transits") or [] if x.get("position")]
        entry["objectives"] = []
        entry.update(layers(m, dump, names))
        maps[title] = entry

    task_list = (tasks or {}).get("tasks") or {}
    task_list = task_list.values() if isinstance(task_list, dict) else task_list
    for t in task_list:
        for o in t.get("objectives") or []:
            text = tasks_en.get(o.get("description"), o.get("description") or "")
            spots = {}  # map -> positions (one objective repeats a zone for each map variant)
            for z in o.get("zones") or []:
                if z.get("position") and z.get("map") in by_id:
                    spots.setdefault(by_id[z["map"]], []).append(xyz(z["position"]))
            for loc in o.get("possibleLocations") or []:
                if loc.get("map") in by_id:
                    spots.setdefault(by_id[loc["map"]], []).extend(xyz(p) for p in loc.get("positions") or [])
            for title, positions in spots.items():
                if title not in maps:
                    continue
                unique = list(dict.fromkeys(map(tuple, positions)))
                maps[title]["objectives"].append({"quest": t["id"], "text": text,
                                                  "positions": [list(p) for p in unique]})

    data = {
        "fetchedAt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "source": f"{DEV_MAPS} + {BASE}",
        "items": names.items,
        "names": names.table,
        "maps": dict(sorted(maps.items())),
    }
    OUT.write_text(json.dumps(data, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")
    for k, v in data["maps"].items():
        print(f"{k}: {len(v['extracts'])} extracts, {len(v['transits'])} transits, "
              f"{len(v['objectives'])} objectives, {len(v['layers'])} floors, "
              f"{len(v['spawns'])} spawns, {len(v['bosses'])} bosses, {len(v['locks'])} locks, "
              f"{len(v['containers'])} containers, {len(v['loose'])} loose, {len(v['hazards'])} hazards, "
              f"{'svg' if 'svgPath' in v else 'tiles'}")
    print(f"{len(names.items)} loose items, {OUT.stat().st_size // 1024} KB")
    sys.exit(0 if tasks is not None else 1)


if __name__ == "__main__":
    main()
