#!/usr/bin/env python3
"""Fetch three Wikimedia Commons images per state fish page and wire them into the YAML.

For each Content/states/*/fish*.yaml page:
  hero    -> /images/fish/{state}/{state}-{file}-hero.webp      (species photo)
  detail  -> /images/fish/{state}/{state}-{file}-detail.webp    (second species photo, `about` section)
  habitat -> /images/fish/{state}/{state}-{file}-habitat.webp   (the first mapped water body,
             falling back to a third species photo), `location` section

Public-domain and CC0 files are preferred. CC BY / CC BY-SA files are used only when nothing
free of attribution is available, and then the author and license are added to the caption.
Every file is recorded in artifacts/fish-images/manifest.json. Each Commons file is used once.

Usage: python scripts/fetch-fish-images.py [--only alabama,hawaii] [--force]
"""

import argparse
import glob
import html
import io
import json
import os
import re
import time
from pathlib import Path

import requests
import yaml
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
WWW = ROOT / "wwwroot"
MANIFEST = ROOT / "artifacts/fish-images/manifest.json"
API = "https://commons.wikimedia.org/w/api.php"
HEADERS = {"User-Agent": "USASymbolBot/1.0 (https://usasymbol.com; demfoss@gmail.com)"}
WIDTH = 1600
BAD_NAME = re.compile(r"drawing|illustration|map|range|stamp|logo|diagram|skeleton|coin|plate|label|"
                      r"specimen|museum|jar|fossil|chart|svg|egg|larva|fry\b|dead|cooked|fillet|food|market",
                      re.I)
PEOPLE = re.compile(r"parade|truck|crowd|festival|sign|wall|exhibit|stocking|hatchery|survey|electrofish|"
                    r"volunteer|team|staff|workers|students|building|street|boat ramp|smoked|chart|graph|"
                    r"plot|weight|brisket|puree|dish|recipe|card|cigarette|cast net|pond|\.png$", re.I)
FREE = ("public domain", "pd", "cc0", "no restrictions")
ART = re.compile(r"drawing|illustrat|plate|engraving|lithograph|painting|biodiversity heritage library|"
                 r"\bbook|bulletin|report of|fishes of|page \d|\bart\b|scan|night|preserved|"
                 r"specimen|naturalis|museum|\bcollection", re.I)

session = requests.Session()
session.headers.update(HEADERS)


def license_rank(meta):
    short = (meta.get("LicenseShortName", {}).get("value") or "").lower()
    if any(short.startswith(f) or f in short for f in FREE):
        return 0, short
    if short.startswith("cc by") and "nc" not in short and "nd" not in short:
        return 1, short
    return None, short


def search(query, limit=40):
    params = {
        "action": "query", "format": "json", "generator": "search", "gsrnamespace": 6,
        "gsrsearch": f"{query} filetype:bitmap", "gsrlimit": limit,
        "prop": "imageinfo", "iiprop": "url|size|mime|extmetadata", "iiurlwidth": WIDTH,
    }
    for attempt in range(4):
        r = session.get(API, params=params, timeout=60)
        if r.status_code == 429:
            time.sleep(10 * (attempt + 1))
            continue
        r.raise_for_status()
        pages = (r.json().get("query") or {}).get("pages") or {}
        return sorted(pages.values(), key=lambda p: p.get("index", 0))
    return []


def candidates(query, used, min_width=1000, must=None):
    out = []
    for p in search(query):
        title = p.get("title", "")
        info = (p.get("imageinfo") or [{}])[0]
        if title in used or BAD_NAME.search(title):
            continue
        if must and (not must.search(title) or PEOPLE.search(title)):
            continue
        if info.get("mime") not in ("image/jpeg", "image/png"):
            continue
        w, h = info.get("width", 0), info.get("height", 0)
        if w < min_width or not h or not (1.15 <= w / h <= 2.4):
            continue
        rank, short = license_rank(info.get("extmetadata", {}))
        if rank is None:
            continue
        meta = info["extmetadata"]
        art_text = " ".join([title, meta.get("Categories", {}).get("value", ""),
                             re.sub(r"<[^>]+>", " ", meta.get("ImageDescription", {}).get("value", ""))])
        is_art = 1 if ART.search(art_text) else 0
        artist = re.sub(r"<[^>]+>", "", html.unescape(meta.get("Artist", {}).get("value", ""))).strip()
        out.append({"title": title, "rank": rank, "art": is_art, "license": meta.get("LicenseShortName", {}).get("value", ""),
                    "artist": artist[:120], "page": info.get("descriptionurl"),
                    "url": info.get("thumburl") or info.get("url")})
    out.sort(key=lambda c: (c["art"], c["rank"]))
    return out


def pick(queries, used, must=None):
    for q in queries:
        found = candidates(q, used, must=must)
        if found:
            used.add(found[0]["title"])
            return found[0]
    return None


def save(c, web_path):
    r = session.get(c["url"], timeout=120)
    r.raise_for_status()
    img = Image.open(io.BytesIO(r.content)).convert("RGB")
    if img.width > WIDTH:
        img = img.resize((WIDTH, round(img.height * WIDTH / img.width)), Image.LANCZOS)
    dest = WWW / web_path.lstrip("/")
    dest.parent.mkdir(parents=True, exist_ok=True)
    img.save(dest, "WEBP", quality=82, method=6)


def credit(c):
    if c["rank"] == 0:
        return ""
    who = c["artist"] or "Unknown author"
    return f" Photo by {who}, {c['license']}."


def species_queries(doc):
    sci = (doc.get("binomial_name") or "").strip()
    name = re.sub(r"\(.*?\)", "", doc["name"]).strip()
    two = " ".join(sci.split()[:2])
    qs = [f'"{sci}"', f'"{two}"', f'"{name}" fish', f"{name}"]
    return [q for q in dict.fromkeys(qs) if q.strip('" ')]


def species_must(doc):
    sci = (doc.get("binomial_name") or "").split()
    words = [w for w in re.sub(r"\(.*?\)", "", doc["name"]).split() if len(w) > 3]
    keys = ([sci[0]] if sci else []) + words[-1:]
    return re.compile("|".join(re.escape(k) for k in keys), re.I)


def redo_hero(path, manifest, used):
    doc = yaml.safe_load(io.open(path, encoding="utf-8"))
    state, stem = Path(path).parent.name, Path(path).stem
    key = f"{state}/{stem}"
    hero = pick(species_queries(doc), used, must=species_must(doc))
    if not hero:
        return f"{key}: no better hero"
    save(hero, f"/images/fish/{state}/{state}-{stem}-hero.webp")
    caption = re.sub(r" Photo by .*$", "", doc.get("hero_image_caption") or "")
    doc["hero_image_caption"] = caption + (credit(hero) if hero["rank"] == 1 else "")
    header = "\n".join(l for l in io.open(path, encoding="utf-8").read().splitlines()
                       if l.startswith("# verification"))
    body = yaml.safe_dump(doc, sort_keys=False, allow_unicode=True, width=10_000)
    io.open(path, "w", encoding="utf-8", newline="").write((header + "\n" if header else "") + body)
    manifest.setdefault(key, {})["hero"] = hero
    return f"{key}: new hero {hero['title']} ({'PD' if hero['rank'] == 0 else hero['license']})"


def process(path, manifest, used, force):
    doc = yaml.safe_load(io.open(path, encoding="utf-8"))
    state = Path(path).parent.name
    stem = Path(path).stem
    key = f"{state}/{stem}"
    if key in manifest and not force:
        return f"{key}: skipped"
    base = f"/images/fish/{state}/{state}-{stem}"
    name = doc["name"]

    hero = pick(species_queries(doc), used)
    detail = pick(species_queries(doc), used)
    site = next((s for s in doc["sections"] if s["id"] == "location"), {})
    sites = site.get("sites") or []
    habitat, habitat_is_site = None, False
    if sites:
        water = sites[0]["name"]
        habitat = pick([f'"{water}" {doc["state"]}', f'"{water}"'], used)
        habitat_is_site = habitat is not None
    if habitat is None:
        habitat = pick(species_queries(doc), used)
    if not hero:
        return f"{key}: NO HERO FOUND"

    save(hero, base + "-hero.webp")
    doc["hero_image"] = base + "-hero.webp"
    if hero["rank"] == 1:
        doc["hero_image_caption"] = (doc.get("hero_image_caption") or "").rstrip() + credit(hero)

    assets = []
    if detail:
        save(detail, base + "-detail.webp")
        assets.append({"id": f"{state}-{stem}-detail", "src": base + "-detail.webp",
                       "alt": f"Another view of the {name}",
                       "caption": f"Another look at the {name}." + credit(detail),
                       "section": "about", "layout": "full-width-tall"})
    if habitat:
        save(habitat, base + "-habitat.webp")
        if habitat_is_site:
            alt = f"{sites[0]['name']} in {doc['state']}"
            cap = f"{sites[0]['name']}, one of the waters where the {name} lives in {doc['state']}."
        else:
            alt, cap = f"The {name}", f"The {name}."
        assets.append({"id": f"{state}-{stem}-habitat", "src": base + "-habitat.webp", "alt": alt,
                       "caption": cap + credit(habitat), "section": "location", "layout": "full-width-tall"})
    doc["visual_assets"] = assets

    header = "\n".join(l for l in io.open(path, encoding="utf-8").read().splitlines()
                       if l.startswith("# verification"))
    body = yaml.safe_dump(doc, sort_keys=False, allow_unicode=True, width=10_000)
    io.open(path, "w", encoding="utf-8", newline="").write((header + "\n" if header else "") + body)

    manifest[key] = {k: v for k, v in (("hero", hero), ("detail", detail), ("habitat", habitat)) if v}
    picked = [f"{k}:{'PD' if v['rank'] == 0 else v['license']}" for k, v in manifest[key].items()]
    return f"{key}: " + " ".join(picked) + ("" if habitat_is_site else " (habitat=species)")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only")
    ap.add_argument("--force", action="store_true")
    ap.add_argument("--redo-hero", help="comma-separated state/file keys whose hero should be replaced")
    args = ap.parse_args()
    MANIFEST.parent.mkdir(parents=True, exist_ok=True)
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8")) if MANIFEST.exists() else {}
    used = {c["title"] for entry in manifest.values() for c in entry.values()}
    paths = sorted(glob.glob(str(ROOT / "Content/states/*/fish*.yaml")))
    if args.only:
        wanted = set(args.only.split(","))
        paths = [p for p in paths if Path(p).parent.name in wanted]
    if args.redo_hero:
        for key in args.redo_hero.split(","):
            state, stem = key.split("/")
            p = str(ROOT / "Content/states" / state / f"{stem}.yaml")
            print(redo_hero(p, manifest, used), flush=True)
            MANIFEST.write_text(json.dumps(manifest, indent=1, ensure_ascii=False), encoding="utf-8")
        return
    for p in paths:
        try:
            print(process(p, manifest, used, args.force), flush=True)
        except Exception as e:  # keep going; report at the end of the line
            print(f"{Path(p).parent.name}/{Path(p).stem}: ERROR {e}", flush=True)
        MANIFEST.write_text(json.dumps(manifest, indent=1, ensure_ascii=False), encoding="utf-8")
        time.sleep(1)


if __name__ == "__main__":
    main()
