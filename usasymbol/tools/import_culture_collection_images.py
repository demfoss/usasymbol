"""Replace placeholder art on six culture collections with licensed real images.

Images are taken only from Wikimedia Commons.  The importer prefers the free
lead image attached to the matching English Wikipedia article, verifies the
license in Commons metadata, writes a per-collection attribution manifest, and
removes an image reference from a card when no trustworthy match is available.
"""

from __future__ import annotations

import html
import io
import json
import pathlib
import re
import sys
import time
import urllib.parse

import requests
import yaml
from PIL import Image, ImageOps


if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")


ROOT = pathlib.Path(__file__).resolve().parents[1]
CONTENT_ROOT = ROOT / "Content" / "collections" / "culture"
IMAGE_ROOT = ROOT / "wwwroot" / "images" / "collections" / "culture"
USER_AGENT = "USA-Symbol-image-curation/1.0 (https://usasymbol.com/)"
HEADERS = {"User-Agent": USER_AGENT, "Accept": "application/json"}
WIKIPEDIA_API = "https://en.wikipedia.org/w/api.php"
COMMONS_API = "https://commons.wikimedia.org/w/api.php"

COLLECTIONS = {
    "famous-inventions-by-state": "invention",
    "grammy-winners-by-state": "person",
    "most-famous-movie-set-in-each-state": "movie",
    "most-famous-tv-show-set-in-each-state": "television",
    "most-haunted-places-by-state": "place",
    "oscar-winning-actors-by-state": "person",
}

HERO_SEARCHES = {
    "grammy-winners-by-state": "concert stage microphone audience photograph",
    "most-famous-movie-set-in-each-state": "cinema film projector movie theater photograph",
    "most-famous-tv-show-set-in-each-state": "television studio camera photograph",
    "most-haunted-places-by-state": "old abandoned mansion night photograph",
    "oscar-winning-actors-by-state": "movie theater red carpet photograph",
}

INVENTION_ARTICLES = {
    "The Telephone": ["Telephone"],
    "The Revolver": ["Colt Paterson", "Revolver"],
    "The Moving Assembly Line": ["Assembly line"],
    "The Modern Typewriter": ["Typewriter"],
    "The Ferris Wheel": ["Ferris Wheel", "Ferris wheel"],
    "Scotch Tape": ["Scotch Tape"],
    "Coca-Cola": ["Coca-Cola"],
    "Cotton Candy": ["Cotton candy"],
    "The Integrated Circuit": ["Integrated circuit"],
    "The Apple I Computer": ["Apple I"],
    "Electronic Television": ["Television", "Philo Farnsworth"],
    "The Ice Cream Cone": ["Ice cream cone"],
    "The Electric Self-Starter": ["Starter (engine)"],
    "The Safety Elevator": ["Elevator", "Elisha Otis"],
}

EXTRA_ARTICLES = {
    "edison-menlo-park.jpg": ["Thomas Edison National Historical Park", "Thomas Edison"],
    "wright-flyer-kitty-hawk.jpg": ["Wright Flyer"],
    "eniac-university-of-pennsylvania.jpg": ["ENIAC"],
    "atanasoff-berry-computer-iowa-state.jpg": ["Atanasoff–Berry computer"],
}

SPECIAL_ARTICLES = {
    ("famous-inventions-by-state", "edison-menlo-park.jpg"): ["Thomas Edison National Historical Park"],
    ("grammy-winners-by-state", "Prince"): ["Prince (musician)"],
    ("grammy-winners-by-state", "texas.jpg"): ["Beyoncé"],
    ("most-haunted-places-by-state", "hero.jpg"): ["Winchester Mystery House"],
    ("most-haunted-places-by-state", "The Marshall House"): ["The Marshall House (Savannah, Georgia)"],
    ("most-haunted-places-by-state", "Marshall House"): ["The Marshall House (Savannah, Georgia)"],
    ("most-haunted-places-by-state", "Fort Knox"): ["Fort Knox (Maine)"],
    ("oscar-winning-actors-by-state", "hero.jpg"): ["Dolby Theatre"],
}

# These names resolve to a different place or have no verifiable free photo.
# Their cards intentionally fall back to the clean text-only layout.
SKIP_IMAGES = {
    ("most-haunted-places-by-state", "Allen House"),
    ("most-haunted-places-by-state", "Bara-Hack"),
    ("most-haunted-places-by-state", "Bailey House Museum"),
    ("most-haunted-places-by-state", "Summerwind Mansion"),
}

ALLOWED_LICENSE_MARKERS = (
    "public domain",
    "cc0",
    "cc by",
    "cc-by",
    "cc by-sa",
    "cc-by-sa",
)
BAD_FILE_WORDS = (
    "logo", "poster", "map", "icon", "diagram", "signature", "wordmark",
    "flag", "seal", "coat of arms", "dvd", "blu-ray", "cover",
)


def clean_html(value: str | None) -> str:
    text = re.sub(r"<[^>]+>", " ", value or "")
    return re.sub(r"\s+", " ", html.unescape(text)).strip()


def strip_year(subject: str) -> tuple[str, str | None]:
    match = re.search(r"\s*\((\d{4})\)\s*$", subject)
    if not match:
        return subject.strip(), None
    return subject[: match.start()].strip(), match.group(1)


def article_candidates(subject: str, kind: str) -> list[str]:
    title, year = strip_year(subject)
    if kind == "person" or kind == "place":
        return [title]
    if kind == "movie":
        candidates = []
        if year:
            candidates.append(f"{title} ({year} film)")
        candidates.extend((f"{title} (film)", title))
        return list(dict.fromkeys(candidates))
    if kind == "television":
        return [f"{title} (TV series)", f"{title} (American TV series)", title]
    if kind == "invention":
        return INVENTION_ARTICLES.get(title, [title])
    return [title]


def request_json(session: requests.Session, url: str, params: dict) -> dict:
    for attempt in range(8):
        response = session.get(url, params=params, headers=HEADERS, timeout=45)
        if response.status_code == 429:
            delay = min(10 * (attempt + 1), 50)
            print(f"  Wikimedia rate limit; retrying in {delay}s", flush=True)
            time.sleep(delay)
            continue
        response.raise_for_status()
        return response.json()
    raise RuntimeError(f"Rate limited by {url}")


def free_lead_filename(session: requests.Session, title: str) -> str | None:
    payload = request_json(
        session,
        WIKIPEDIA_API,
        {
            "action": "query",
            "format": "json",
            "formatversion": 2,
            "redirects": 1,
            "prop": "pageprops",
            "ppprop": "page_image_free",
            "titles": title,
        },
    )
    pages = payload.get("query", {}).get("pages", [])
    if not pages or pages[0].get("missing"):
        return None
    return pages[0].get("pageprops", {}).get("page_image_free")


def commons_file(session: requests.Session, filename: str) -> dict | None:
    payload = request_json(
        session,
        COMMONS_API,
        {
            "action": "query",
            "format": "json",
            "formatversion": 2,
            "prop": "imageinfo",
            "iiprop": "url|extmetadata|mime|size",
            "iiurlwidth": 1800,
            "titles": "File:" + filename.removeprefix("File:"),
        },
    )
    pages = payload.get("query", {}).get("pages", [])
    if not pages or pages[0].get("missing") or not pages[0].get("imageinfo"):
        return None
    page = pages[0]
    info = page["imageinfo"][0]
    meta = info.get("extmetadata", {})
    license_name = clean_html(meta.get("LicenseShortName", {}).get("value"))
    usage_terms = clean_html(meta.get("UsageTerms", {}).get("value"))
    license_blob = f"{license_name} {usage_terms}".lower()
    if not any(marker in license_blob for marker in ALLOWED_LICENSE_MARKERS):
        return None
    mime = info.get("mime", "")
    if not mime.startswith("image/") or mime == "image/svg+xml":
        return None
    return {
        "filename": page.get("title", "File:" + filename).removeprefix("File:"),
        "download_url": info.get("thumburl") or info.get("url"),
        "source_url": info.get("descriptionurl"),
        "author": clean_html(meta.get("Artist", {}).get("value")) or "Wikimedia Commons contributor",
        "license": license_name or usage_terms,
        "license_url": meta.get("LicenseUrl", {}).get("value", ""),
        "description": clean_html(meta.get("ImageDescription", {}).get("value")),
    }


def commons_search(session: requests.Session, query: str) -> dict | None:
    payload = request_json(
        session,
        COMMONS_API,
        {
            "action": "query",
            "format": "json",
            "formatversion": 2,
            "generator": "search",
            "gsrsearch": query + " filetype:bitmap",
            "gsrnamespace": 6,
            "gsrlimit": 16,
            "prop": "imageinfo",
            "iiprop": "url|extmetadata|mime|size",
            "iiurlwidth": 1800,
        },
    )
    query_tokens = {token for token in re.findall(r"[a-z0-9]+", query.lower()) if len(token) > 2}
    ranked: list[tuple[int, dict]] = []
    for page in payload.get("query", {}).get("pages", []):
        title = page.get("title", "")
        lowered = title.lower()
        if any(word in lowered for word in BAD_FILE_WORDS):
            continue
        info_list = page.get("imageinfo") or []
        if not info_list:
            continue
        info = info_list[0]
        meta = info.get("extmetadata", {})
        license_name = clean_html(meta.get("LicenseShortName", {}).get("value"))
        usage_terms = clean_html(meta.get("UsageTerms", {}).get("value"))
        license_blob = f"{license_name} {usage_terms}".lower()
        if not any(marker in license_blob for marker in ALLOWED_LICENSE_MARKERS):
            continue
        if not info.get("mime", "").startswith("image/") or info.get("mime") == "image/svg+xml":
            continue
        title_tokens = set(re.findall(r"[a-z0-9]+", lowered))
        score = len(query_tokens & title_tokens) * 10
        width, height = info.get("width", 0), info.get("height", 0)
        if width >= 900 and height >= 600:
            score += 4
        ranked.append(
            (
                score,
                {
                    "filename": title.removeprefix("File:"),
                    "download_url": info.get("thumburl") or info.get("url"),
                    "source_url": info.get("descriptionurl"),
                    "author": clean_html(meta.get("Artist", {}).get("value")) or "Wikimedia Commons contributor",
                    "license": license_name or usage_terms,
                    "license_url": meta.get("LicenseUrl", {}).get("value", ""),
                    "description": clean_html(meta.get("ImageDescription", {}).get("value")),
                },
            )
        )
    if not ranked:
        return None
    ranked.sort(key=lambda item: item[0], reverse=True)
    return ranked[0][1]


def find_image(session: requests.Session, candidates: list[str], allow_search: bool) -> tuple[dict | None, str | None]:
    for title in candidates:
        filename = free_lead_filename(session, title)
        if filename:
            record = commons_file(session, filename)
            if record:
                return record, title
    if allow_search and candidates:
        record = commons_search(session, candidates[0] + " photograph")
        if record:
            return record, candidates[0]
    return None, None


def save_image(session: requests.Session, record: dict, target: pathlib.Path, hero: bool) -> None:
    response = session.get(record["download_url"], headers=HEADERS, timeout=90)
    response.raise_for_status()
    with Image.open(io.BytesIO(response.content)) as source:
        image = ImageOps.exif_transpose(source).convert("RGB")
        if hero:
            image = ImageOps.fit(image, (1600, 900), Image.Resampling.LANCZOS, centering=(0.5, 0.45))
        else:
            image = ImageOps.fit(image, (700, 700), Image.Resampling.LANCZOS, centering=(0.5, 0.38))
        target.parent.mkdir(parents=True, exist_ok=True)
        image.save(target, "JPEG", quality=86, optimize=True, progressive=True)


def remove_missing_card_references(yaml_path: pathlib.Path, missing_paths: set[str]) -> None:
    if not missing_paths:
        return
    lines = yaml_path.read_text(encoding="utf-8").splitlines(keepends=True)
    kept = []
    image_line = re.compile(r'^\s+image:\s+["\']?([^"\'\r\n]+)["\']?\s*$')
    for line in lines:
        match = image_line.match(line.rstrip("\r\n"))
        if match and match.group(1).strip() in missing_paths:
            continue
        kept.append(line)
    yaml_path.write_text("".join(kept), encoding="utf-8", newline="")


def image_tasks(data: dict, kind: str) -> list[dict]:
    tasks = []
    hero_path = data.get("hero_image")
    if hero_path:
        tasks.append({"path": hero_path, "hero": True, "subject": None, "kind": kind})
    for section in data.get("sections") or []:
        for highlight in section.get("highlights") or []:
            path = highlight.get("image")
            if not path:
                continue
            name = highlight.get("name", "")
            subject = name.split(": ", 1)[-1].strip()
            tasks.append({"path": path, "hero": False, "subject": subject, "kind": kind})
    for asset in data.get("visual_assets") or []:
        path = asset.get("src")
        if path and not any(task["path"] == path for task in tasks):
            tasks.append({"path": path, "hero": False, "subject": None, "kind": kind})
    deduped = {}
    for task in tasks:
        deduped.setdefault(task["path"], task)
    return list(deduped.values())


def run() -> int:
    session = requests.Session()
    session.headers.update(HEADERS)
    total_saved = 0
    total_removed = 0

    selected = set(sys.argv[1:])
    collections = {
        slug: kind for slug, kind in COLLECTIONS.items()
        if not selected or slug in selected
    }
    unknown = selected - set(COLLECTIONS)
    if unknown:
        raise SystemExit("Unknown collection(s): " + ", ".join(sorted(unknown)))

    for slug, kind in collections.items():
        yaml_path = CONTENT_ROOT / f"{slug}.yml"
        data = yaml.safe_load(yaml_path.read_text(encoding="utf-8")) or {}
        tasks = image_tasks(data, kind)
        credits = []
        missing_card_paths: set[str] = set()
        print(f"\n[{slug}] {len(tasks)} referenced images", flush=True)

        for index, task in enumerate(tasks, 1):
            rel_path = task["path"]
            target = ROOT / "wwwroot" / rel_path.lstrip("/")
            filename = target.name

            lookup_key = task["subject"] or filename
            special = SPECIAL_ARTICLES.get((slug, lookup_key))
            if (slug, lookup_key) in SKIP_IMAGES:
                record, matched_title = None, None
            elif special:
                record, matched_title = find_image(session, special, False)
            elif task["hero"] and slug in HERO_SEARCHES:
                record = commons_search(session, HERO_SEARCHES[slug])
                matched_title = HERO_SEARCHES[slug]
            else:
                candidates = EXTRA_ARTICLES.get(filename)
                if not candidates:
                    candidates = article_candidates(task["subject"] or filename.rsplit(".", 1)[0], kind)
                allow_search = kind in {"invention", "place"}
                record, matched_title = find_image(session, candidates, allow_search)

            if not record:
                if not task["hero"] and task["subject"]:
                    missing_card_paths.add(rel_path)
                    total_removed += 1
                print(f"  {index:02}/{len(tasks):02} REMOVE {task['subject'] or filename}: no verified free image", flush=True)
                continue

            try:
                save_image(session, record, target, task["hero"])
            except Exception as error:
                if not task["hero"] and task["subject"]:
                    missing_card_paths.add(rel_path)
                    total_removed += 1
                print(f"  {index:02}/{len(tasks):02} REMOVE {task['subject'] or filename}: download failed ({error})", flush=True)
                continue

            credits.append(
                {
                    "localPath": rel_path,
                    "subject": task["subject"] or "Collection hero",
                    "matchedArticleOrSearch": matched_title,
                    "commonsFile": record["filename"],
                    "author": record["author"],
                    "license": record["license"],
                    "licenseUrl": record["license_url"],
                    "sourceUrl": record["source_url"],
                }
            )
            total_saved += 1
            print(f"  {index:02}/{len(tasks):02} SAVE   {task['subject'] or filename} <- {record['filename']}", flush=True)
            time.sleep(0.12)

        remove_missing_card_references(yaml_path, missing_card_paths)
        manifest_path = IMAGE_ROOT / slug / "image-credits.json"
        manifest_path.write_text(
            json.dumps(
                {
                    "collection": slug,
                    "provider": "Wikimedia Commons",
                    "generated": "2026-09-20",
                    "images": credits,
                },
                ensure_ascii=False,
                indent=2,
            ) + "\n",
            encoding="utf-8",
        )
        print(f"  manifest: {manifest_path.relative_to(ROOT)} ({len(credits)} credits)", flush=True)

    print(f"\nDONE: {total_saved} real images saved; {total_removed} unverified card references removed", flush=True)
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(run())
    except KeyboardInterrupt:
        print("Interrupted", file=sys.stderr)
        raise SystemExit(130)
