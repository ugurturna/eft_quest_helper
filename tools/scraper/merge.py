"""Merge tarkov.dev data into data/tasks_en.json, write data/diff_report.md
and seed data/tasks_tr.json with tarkov.dev Turkish names.

    python tools/scraper/merge.py

Wiki stays the base. tarkov.dev fills what the wiki lacks (kappa flags, levels,
missing prerequisites, PvE differences). Every filled field is recorded in task.sources.
Existing translations in tasks_tr.json are never overwritten.
"""
import json
import re
import urllib.parse
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / "data"
SRC = DATA / "sources"
COLLECTOR = "collector"


def load(name):
    p = SRC / name
    return json.loads(p.read_text(encoding="utf-8")) if p.exists() else None


def wiki_key(url):
    if not url or "/wiki/" not in url:
        return None
    return urllib.parse.unquote(url.split("/wiki/", 1)[1]).replace("_", " ").strip().lower()


def name_key(name):
    return re.sub(r"[^a-z0-9]+", "", (name or "").lower().replace("&", "and"))


def closure(by_id, start):
    seen, stack = set(), [start]
    while stack:
        for p in by_id[stack.pop()]["prerequisites"]:
            if p not in seen:
                seen.add(p)
                stack.append(p)
    return seen


def main():
    tasks = json.loads((DATA / "tasks_en.json").read_text(encoding="utf-8"))
    by_id = {t["id"]: t for t in tasks}
    by_wiki = {wiki_key(t["wikiUrl"]): t for t in tasks}
    by_name = {name_key(t["name"]): t for t in tasks}

    src = {m: load(f"tarkovdev_{m}_en.json") for m in ("pvp", "pve")}
    tr = {m: load(f"tarkovdev_{m}_tr.json") for m in ("pvp", "pve")}
    if not src["pvp"]:
        print("no tarkov.dev data - run fetch_tarkovdev.py first")
        return

    # tarkov.dev id -> (our id, td task), per mode
    match, unmatched = {"pvp": {}, "pve": {}}, {"pvp": [], "pve": []}
    by_our = {"pvp": {}, "pve": {}}
    for mode, lst in src.items():
        for td in lst or []:
            t = by_wiki.get(wiki_key(td.get("wikiLink"))) or by_name.get(name_key(td["name"]))
            if t:
                match[mode][td["id"]] = (t["id"], td)
                by_our[mode][t["id"]] = td
            else:
                unmatched[mode].append(td["name"])

    def td_prereqs(mode, td):
        out = set()
        for r in td.get("taskRequirements") or []:
            if r.get("task") and "complete" in (r.get("status") or []) and r["task"]["id"] in match[mode]:
                out.add(match[mode][r["task"]["id"]][0])
        return out

    stats = {"kappa": 0, "level": 0}
    log = {"prereq": [], "level_diff": [], "kappa_diff": [], "pve": [], "modes": []}
    for t in tasks:
        pvp, pve = by_our["pvp"].get(t["id"]), by_our["pve"].get(t["id"])
        td = pvp or pve
        if not td:
            continue
        t["tarkovDevId"] = td["id"]
        t["lightkeeperRequired"] = bool(td.get("lightkeeperRequired"))
        t["sources"]["lightkeeperRequired"] = "tarkov.dev"

        lvl = td.get("minPlayerLevel") or 0
        if t["minLevel"] is None and lvl > 1:
            t["minLevel"] = lvl
            t["sources"]["minLevel"] = "tarkov.dev"
            stats["level"] += 1
        elif t["minLevel"] is not None and lvl and lvl != t["minLevel"]:
            log["level_diff"].append(f"- {t['name']}: wiki {t['minLevel']} / tarkov.dev {lvl}")

        if not t["map"] and td.get("map"):
            t["map"] = [td["map"]["name"]]
            t["sources"]["map"] = "tarkov.dev"

        if src["pve"]:
            modes = [m for m, x in (("pvp", pvp), ("pve", pve)) if x]
            if modes != t["gameModes"]:
                log["modes"].append(f"- {t['name']}: wiki {t['gameModes']} / tarkov.dev {modes}")
                t["gameModes"] = modes
                t["sources"]["gameModes"] = "tarkov.dev"

    # prerequisites: add what tarkov.dev knows and wiki missed (skip anything that would cycle)
    for t in tasks:
        pvp = by_our["pvp"].get(t["id"])
        if not pvp:
            continue
        for p in sorted(td_prereqs("pvp", pvp) - set(t["prerequisites"])):
            if p == t["id"] or t["id"] in closure(by_id, p):
                continue
            t["prerequisites"].append(p)
            t["sources"].setdefault("prerequisitesAdded", []).append(p)
            log["prereq"].append(f"- {t['name']}: + {by_id[p]['name']}")

    # Kappa = Collector and everything it needs; the wiki's per-quest "reqkappa" predates the change
    # that left only Collector's own chain required (tarkov.dev agrees)
    kappa = closure(by_id, COLLECTOR) | {COLLECTOR} if COLLECTOR in by_id else set()
    for t in tasks:
        new = t["id"] in kappa
        if t["kappaRequired"] != new:
            log["kappa_diff"].append(f"- {t['name']}: wiki {t['kappaRequired']} -> {new}")
        t["kappaRequired"] = new
        t["sources"]["kappaRequired"] = "collector chain"
    stats["kappa"] = len(kappa)

    # PvE overrides: only where tarkov.dev's PvE data differs from PvP
    for t in tasks:
        pvp, pve = by_our["pvp"].get(t["id"]), by_our["pve"].get(t["id"])
        if not (pvp and pve):
            continue
        ov = t["modeOverrides"].setdefault("pve", {})
        pp, ep = td_prereqs("pvp", pvp), td_prereqs("pve", pve)
        if pp != ep:
            ov["prerequisites"] = sorted((set(t["prerequisites"]) - (pp - ep)) | (ep - pp))
            log["pve"].append(f"- {t['name']}: prerequisites differ {sorted(pp ^ ep)}")
        if (pve.get("minPlayerLevel") or 0) != (pvp.get("minPlayerLevel") or 0) and "minLevel" not in ov:
            ov["minLevel"] = pve["minPlayerLevel"]
            log["pve"].append(f"- {t['name']}: level PvP {pvp['minPlayerLevel']} / PvE {pve['minPlayerLevel']}")
        if bool(pve.get("kappaRequired")) != bool(pvp.get("kappaRequired")):
            ov["kappaRequired"] = bool(pve.get("kappaRequired"))
            log["pve"].append(f"- {t['name']}: kappa PvP {pvp['kappaRequired']} / PvE {pve['kappaRequired']}")
        if [o["description"] for o in pvp["objectives"]] != [o["description"] for o in pve["objectives"]]:
            ov["objectivesTarkovDev"] = [o["description"] for o in pve["objectives"]]
            log["pve"].append(f"- {t['name']}: objectives differ")
        if ov:
            t["sources"]["modeOverrides.pve"] = "fandom+tarkov.dev"
        else:
            del t["modeOverrides"]["pve"]

    dangling = [(t["id"], p) for t in tasks for p in t["prerequisites"] if p not in by_id]
    (DATA / "tasks_en.json").write_text(json.dumps(tasks, ensure_ascii=False, indent=1), encoding="utf-8")

    report = ["# Merge report: fandom wiki + tarkov.dev", "",
              f"- wiki tasks: {len(tasks)}",
              f"- tarkov.dev tasks: PvP {len(src['pvp'] or [])}, PvE {len(src['pve'] or [])}",
              f"- matched: PvP {len(match['pvp'])}, PvE {len(match['pve'])}",
              f"- kappa required: {stats['kappa']}",
              f"- levels filled from tarkov.dev: {stats['level']}",
              f"- dangling prerequisite ids: {len(dangling)}", ""]
    for title, body in [("Prerequisites added from tarkov.dev", log["prereq"]),
                        ("Level disagreements", log["level_diff"]),
                        ("Kappa changes (Collector chain)", log["kappa_diff"]),
                        ("Game mode availability changes", log["modes"]),
                        ("PvE differences", log["pve"]),
                        ("tarkov.dev PvP tasks not on wiki", [f"- {n}" for n in unmatched["pvp"]]),
                        ("tarkov.dev PvE tasks not on wiki", [f"- {n}" for n in unmatched["pve"]])]:
        report += [f"## {title} ({len(body)})", ""] + (body or ["(none)"]) + [""]
    (DATA / "diff_report.md").write_text("\n".join(report), encoding="utf-8")
    print("\n".join(report[2:8]))

    seed_tr(tasks, tr)


def seed_tr(tasks, tr):
    """Create/extend tasks_tr.json with Turkish text from tarkov.dev; keep manual edits."""
    path = DATA / "tasks_tr.json"
    existing = {t["id"]: t for t in json.loads(path.read_text(encoding="utf-8"))} if path.exists() else {}
    tr_by_id = {td["id"]: td for td in (tr["pve"] or []) + (tr["pvp"] or [])}
    out = []
    for t in tasks:
        e = existing.get(t["id"]) or {"id": t["id"], "name": None, "objectives": [],
                                      "notes": None, "guide": None, "sources": {}}
        td = tr_by_id.get(t["tarkovDevId"])
        if td and not e.get("name") and td["name"] != t["name"]:
            e["name"] = td["name"]
            e["sources"]["name"] = "tarkov.dev"
        if td and not e.get("objectives"):
            descs = [o["description"] for o in td["objectives"]]
            if len(descs) == len(t["objectives"]):
                e["objectives"] = [{"id": o["id"], "text": d} for o, d in zip(t["objectives"], descs)]
                e["sources"]["objectives"] = "tarkov.dev"
            else:
                e["tarkovDevObjectives"] = descs
        out.append(e)
    path.write_text(json.dumps(out, ensure_ascii=False, indent=1), encoding="utf-8")
    print(f"tasks_tr.json: {sum(1 for e in out if e.get('name'))}/{len(out)} TR names")


if __name__ == "__main__":
    main()
