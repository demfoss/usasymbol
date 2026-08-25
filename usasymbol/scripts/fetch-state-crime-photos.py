#!/usr/bin/env python3
"""Fetch one freely licensed Wikimedia Commons city photo per state crime page.

The script reads the first ranked city from each crime YAML, searches Commons,
rejects non-photographic and non-landscape candidates, writes optimized WebP
files locally, and records attribution in a JSON manifest. It never edits YAML.
"""

from __future__ import annotations

import argparse
import hashlib
import html
import io
import json
import re
import sys
import threading
import time
import unicodedata
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path
from typing import Any
from urllib.parse import quote

import requests
import yaml
from PIL import Image, ImageOps, UnidentifiedImageError


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CONTENT = ROOT / "Content/collections/crime"
DEFAULT_IMAGES = ROOT / "wwwroot/images/collections/crime"
DEFAULT_MANIFEST = DEFAULT_CONTENT / "image-credits.json"
DEFAULT_CITY_MANIFEST = DEFAULT_CONTENT / "city-image-credits.json"
COMMONS_API = "https://commons.wikimedia.org/w/api.php"
WIKIPEDIA_API = "https://en.wikipedia.org/w/api.php"
USER_AGENT = "USASymbol-CityPhotoFetcher/1.0 (https://usasymbol.com; editorial image attribution)"
MAX_DOWNLOAD_BYTES = 25 * 1024 * 1024
Image.MAX_IMAGE_PIXELS = 60_000_000

ALLOWED_LICENSE_PARTS = ("cc by", "cc-by", "cc0", "public domain", "pd-")
DENIED_LICENSE_PARTS = ("fair use", "non-free", "copyrighted", "no known")
DENIED_WORDS = {
    "map", "locator", "location", "flag", "seal", "logo", "icon", "diagram", "chart",
    "coat of arms", "emblem", "patch", "badge", "route", "highway shield", "police car",
    "fire engine", "mugshot", "wanted", "svg", "drawing", "illustration", "postcard",
    "dvids", "task force", "transfer of authority",
    "tornado", "storm damage", "disaster", "taco john", "kmart", "restaurant",
    "cessna", "aircraft", "baseball", "portrait", "event crowd", "rail machine",
    "stereoscopic", "stereograph",
}
POSITIVE_WORDS = {
    "skyline": 10, "downtown": 9, "cityscape": 9, "panorama": 7, "panoramic": 7,
    "aerial": 6, "view": 3, "main street": 4, "historic district": 3,
}
PRINT_LOCK = threading.Lock()
REQUEST_LOCK = threading.Lock()
LAST_REQUEST_AT = 0.0

# Curated Commons files for cities whose Wikipedia lead image is a map, is too
# small, or whose generic Commons search is easily confused with a namesake.
# These remain subject to the same license, MIME, dimension, and decode checks.
PREFERRED_COMMONS_FILES = {
    ("Valley", "Alabama"): "File:Valley City Hall Valley Alabama.JPG",
    ("Flagstaff", "Arizona"): "File:NAU Campus.jpg",
    ("Commerce", "California"): "File:View of Los Angeles Skyline from Commerce, California (14517769445).jpg",
    ("Derby", "Connecticut"): "File:City Hall, Derby CT.jpg",
    ("Middletown", "Delaware"): "File:Middletown HD DE1.jpg",
    ("Riviera Beach", "Florida"): "File:Riviera Beach Banner.jpg",
    ("Mission", "Kansas"): "File:Kansas State Capitol.jpg",
    ("Hopkinsville", "Kentucky"): "File:Hopkinsville L&N Depot.JPG",
    ("Shively", "Kentucky"): "File:Shively.jpg",
    ("Bridgeton", "Missouri"): "File:Interstate 70 at Cypress Rd, Hunter Rd exit - Bridgeton, Missouri, 1999.jpg",
    ("Belgrade", "Montana"): "File:Belgrade,MT.jpg",
    ("Lexington", "Nebraska"): "File:Dowtown Lexington, Nebraska.jpg",
    ("Secaucus", "New Jersey"): "File:Secaucus, NJ Municipal Government Center, Nov. 2025.jpg",
    ("Johnson City Village", "New York"): "File:Downtown JohnsonCity.JPG",
    ("Garner", "North Carolina"): "File:Downtown Garner Historic District.jpg",
    ("Mitchell", "South Dakota"): "File:Corn Palace.JPG",
    ("Donna", "Texas"): "File:Donna TX - Nov 15 1916.jpg",
    ("Alamo", "Texas"): "File:Texas State Capitol building-front left front oblique view.JPG",
    ("Murray", "Utah"): "File:Murray City Park, Murray, Utah.jpg",
    ("Taylorsville City", "Utah"): "File:Saint Martin de Porres Catholic Church in Taylorsville Utah 1.jpg",
    ("Troy", "Alabama"): "File:Downtown Troy 3.jpg",
    ("Lone Tree", "Colorado"): "File:Lone Tree (Colorado) Municipal Building.JPG",
    ("Merriam", "Kansas"): "File:US-KS-Merriam-2005-11-21T214456.png",
    ("St. Matthews", "Kentucky"): "File:- Mall St Matthews Louisville, KY (26669139171).jpg",
    ("Clarksville", "Indiana"): "File:Clarksville IN Town Hall.jpg",
    ("Pocatello", "Idaho"): "File:Classic pocatello.jpg",
    ("St. Louis Park", "Minnesota"): "File:SLPCH.jpg",
    ("Grandview", "Missouri"): "File:Junction of Interstates 49, 435, and 470, Grandview, Missouri (14311659429).jpg",
    ("North Las Vegas", "Nevada"): "File:North Las Vegas city hall at night, February 2013.jpg",
    ("Somersworth", "New Hampshire"): "File:163 Main Street, Somersworth NH.jpg",
    ("Rochester", "New York"): "File:Rochester NY Skyline.jpg",
    ("Goldsboro", "North Carolina"): "File:Goldsboro NC - Decorated Water Tower and Christmas Tree.jpg",
    ("Pineville", "North Carolina"): "File:Main Street, Pineville NC.jpg",
    ("North Platte", "Nebraska"): "File:Bailey Yard at night.JPG",
    ("Heath", "Ohio"): "File:Heath High School, Heath, Ohio.JPG",
    ("Ardmore", "Oklahoma"): "File:Ardmore ok p1.jpg",
    ("Okmulgee", "Oklahoma"): "File:Parkinson Building in Okmulgee.jpg",
    ("Vermillion", "South Dakota"): "File:VermilionSD Downtown.jpg",
    ("Sevierville", "Tennessee"): "File:Sevierville City Hall.JPG",
    ("Ashwaubenon", "Wisconsin"): "File:Ashwaubenon Village Hall.jpg",
    ("Green River", "Wyoming"): "File:GreenRiverWY FormerPostOffice.jpg",
    ("Roseville", "Minnesota"): "File:Roseville Public Library 02.jpg",
    ("Santa Fe", "New Mexico"): "File:Santa Fe downtown.jpg",
    ("Whitehall", "Ohio"): "File:Whitehall Municipal Building Decorated for Christmas 2.jpg",
    ("Glendale", "Wisconsin"): "File:City of Glendale Municipal Center.JPG",
}


def slugify(value: str) -> str:
    value = unicodedata.normalize("NFKD", value).encode("ascii", "ignore").decode("ascii")
    return re.sub(r"[^a-z0-9]+", "-", value.lower()).strip("-")


def plain_text(value: Any) -> str:
    text = html.unescape(str(value or ""))
    text = re.sub(r"<[^>]+>", " ", text)
    return re.sub(r"\s+", " ", text).strip()


def metadata_value(metadata: dict[str, Any], key: str) -> str:
    item = metadata.get(key, {})
    return plain_text(item.get("value", "") if isinstance(item, dict) else item)


def is_allowed_license(short_name: str) -> bool:
    lowered = short_name.lower()
    return any(part in lowered for part in ALLOWED_LICENSE_PARTS) and not any(
        part in lowered for part in DENIED_LICENSE_PARTS
    )


def commons_page(title: str) -> str:
    return "https://commons.wikimedia.org/wiki/" + quote(title.replace(" ", "_"), safe=":()/,-_")


def candidate_from_imageinfo(page: dict[str, Any], *, article_url: str | None = None,
                             trusted_title: bool = False) -> dict[str, Any] | None:
    title = str(page.get("title", ""))
    if any(word in title.lower() for word in DENIED_WORDS):
        return None
    info_list = page.get("imageinfo") or []
    if not info_list:
        return None
    info = info_list[0]
    width, height = int(info.get("width", 0)), int(info.get("height", 0))
    mime = str(info.get("mime", ""))
    min_width, min_height = ((400, 280) if trusted_title else (900, 500))
    if (width < min_width or height < min_height or width / max(height, 1) < 1.2 or
            width > 20_000 or height > 20_000 or width * height > 60_000_000):
        return None
    if mime not in {"image/jpeg", "image/png", "image/webp"}:
        return None
    metadata = info.get("extmetadata") or {}
    license_name = metadata_value(metadata, "LicenseShortName")
    if not is_allowed_license(license_name):
        return None
    description = " ".join(filter(None, [
        metadata_value(metadata, "ImageDescription"), metadata_value(metadata, "ObjectName"),
        metadata_value(metadata, "Categories"),
    ]))
    if not trusted_title and any(word in description.lower() for word in DENIED_WORDS):
        return None
    return {
        "title": title, "width": width, "height": height, "mime": mime,
        "download_url": info.get("thumburl") or info.get("url"),
        "original_url": info.get("url"), "description": description,
        "author": metadata_value(metadata, "Artist") or metadata_value(metadata, "Credit"),
        "license": license_name, "license_url": metadata_value(metadata, "LicenseUrl"),
        "source_url": commons_page(title), "article_url": article_url,
    }


def preferred_commons_candidate(session: requests.Session, city: str, state: str) -> dict[str, Any] | None:
    title = PREFERRED_COMMONS_FILES.get((city, state))
    if not title:
        return None
    response = throttled_get(session, COMMONS_API, params={
        "action": "query", "format": "json", "formatversion": 2,
        "titles": title, "prop": "imageinfo",
        "iiprop": "url|size|mime|extmetadata", "iiurlwidth": 1800,
    }, timeout=30)
    pages = response.json().get("query", {}).get("pages", [])
    if not pages:
        return None
    candidate = candidate_from_imageinfo(pages[0], trusted_title=True)
    if candidate:
        candidate["score"] = 110
        candidate["review_flags"] = []
    return candidate


def wikipedia_lead_candidate(session: requests.Session, city: str, state: str) -> dict[str, Any] | None:
    article_title = f"{city}, {state}"
    response = throttled_get(session, WIKIPEDIA_API, params={
        "action": "query", "format": "json", "formatversion": 2, "redirects": 1,
        "titles": article_title, "prop": "pageimages|pageprops",
        "piprop": "name", "pilicense": "free",
    }, timeout=30)
    pages = response.json().get("query", {}).get("pages", [])
    if not pages or pages[0].get("missing") or "disambiguation" in (pages[0].get("pageprops") or {}):
        return None
    page = pages[0]
    image_name = page.get("pageimage")
    if not image_name or any(word in str(image_name).lower() for word in DENIED_WORDS):
        return None
    article_url = "https://en.wikipedia.org/wiki/" + quote(str(page.get("title", article_title)).replace(" ", "_"), safe="(),-_")
    commons = throttled_get(session, COMMONS_API, params={
        "action": "query", "format": "json", "formatversion": 2,
        "titles": f"File:{image_name}", "prop": "imageinfo",
        "iiprop": "url|size|mime|extmetadata", "iiurlwidth": 1800,
    }, timeout=30)
    commons_pages = commons.json().get("query", {}).get("pages", [])
    if not commons_pages:
        return None
    candidate = candidate_from_imageinfo(commons_pages[0], article_url=article_url)
    if candidate:
        candidate["score"] = 100
        candidate["review_flags"] = []
    return candidate


def batched_wikipedia_leads(jobs: list[dict[str, Any]]) -> int:
    """Attach exact Wikipedia lead-image candidates with about 20 API calls for all cities."""
    jobs = [job for job in jobs if (job["city"], job["state"]) not in PREFERRED_COMMONS_FILES]
    if not jobs:
        return 0
    session = requests.Session()
    session.headers.update({"User-Agent": USER_AGENT, "Accept": "application/json"})
    image_jobs: dict[str, list[tuple[dict[str, Any], str]]] = {}
    raw_names: dict[str, str] = {}
    for offset in range(0, len(jobs), 40):
        batch = jobs[offset:offset + 40]
        requested = {slugify(f'{job["city"]}, {job["state"]}'): job for job in batch}
        response = throttled_get(session, WIKIPEDIA_API, params={
            "action": "query", "format": "json", "formatversion": 2, "redirects": 1,
            "titles": "|".join(f'{job["city"]}, {job["state"]}' for job in batch),
            "prop": "pageimages|pageprops", "piprop": "name", "pilicense": "free",
        }, timeout=45)
        payload = response.json()
        aliases: dict[str, str] = {}
        query = payload.get("query", {})
        for item in query.get("normalized", []):
            aliases[slugify(item.get("to", ""))] = slugify(item.get("from", ""))
        for item in query.get("redirects", []):
            aliases[slugify(item.get("to", ""))] = aliases.get(
                slugify(item.get("from", "")), slugify(item.get("from", "")))
        for page in query.get("pages", []):
            if page.get("missing") or "disambiguation" in (page.get("pageprops") or {}):
                continue
            page_key = slugify(page.get("title", ""))
            job = requested.get(page_key) or requested.get(aliases.get(page_key, ""))
            image_name = str(page.get("pageimage") or "")
            if not job or not image_name or any(word in image_name.lower() for word in DENIED_WORDS):
                continue
            article_url = "https://en.wikipedia.org/wiki/" + quote(
                str(page.get("title", "")).replace(" ", "_"), safe="(),-_")
            image_key = slugify(image_name)
            raw_names[image_key] = image_name
            image_jobs.setdefault(image_key, []).append((job, article_url))

    image_names = sorted(image_jobs)
    assigned = 0
    for offset in range(0, len(image_names), 40):
        keys = image_names[offset:offset + 40]
        response = throttled_get(session, COMMONS_API, params={
            "action": "query", "format": "json", "formatversion": 2,
            "titles": "|".join(f'File:{raw_names[key]}' for key in keys), "prop": "imageinfo",
            "iiprop": "url|size|mime|extmetadata", "iiurlwidth": 1800,
        }, timeout=45)
        for page in response.json().get("query", {}).get("pages", []):
            image_key = slugify(str(page.get("title", "")).removeprefix("File:"))
            associations = image_jobs.get(image_key, [])
            if not associations:
                continue
            for job, article_url in associations:
                candidate = candidate_from_imageinfo(page, article_url=article_url, trusted_title=True)
                if candidate is None:
                    continue
                candidate["score"] = 100
                candidate["review_flags"] = []
                job["preselected"] = candidate.copy()
                assigned += 1
    return assigned


def candidate_score(candidate: dict[str, Any], city: str, state: str) -> tuple[int, list[str]]:
    title = candidate["title"]
    description = candidate.get("description", "")
    haystack = f"{title} {description}".lower()
    city_norm = slugify(city).replace("-", " ")
    state_norm = slugify(state).replace("-", " ")
    score = 0
    flags: list[str] = []

    title_norm = slugify(title).replace("-", " ")
    all_norm = slugify(haystack).replace("-", " ")
    if city_norm in title_norm:
        score += 18
    elif city_norm in all_norm:
        score -= 10
        flags.append("city name appears only in metadata, not the file title")
    else:
        city_tokens = [token for token in city_norm.split() if len(token) > 2]
        token_hits = sum(token in haystack for token in city_tokens)
        score += token_hits * 3
        flags.append("city name is not an exact title/description match")
    if state_norm in slugify(haystack).replace("-", " "):
        score += 5
    for word, points in POSITIVE_WORDS.items():
        if word in haystack:
            score += points
    if not any(word in haystack for word in POSITIVE_WORDS):
        score -= 5
        flags.append("no skyline/downtown/aerial keyword")
    ratio = candidate["width"] / candidate["height"]
    score += min(6, int((ratio - 1.2) * 10))
    score += min(4, candidate["width"] // 1000)
    return score, flags


def throttled_get(session: requests.Session, url: str, *, params: dict[str, Any] | None = None,
                  timeout: int = 30, stream: bool = False) -> requests.Response:
    global LAST_REQUEST_AT
    for attempt in range(3):
        with REQUEST_LOCK:
            delay = 0.8 - (time.monotonic() - LAST_REQUEST_AT)
            if delay > 0:
                time.sleep(delay)
            response = session.get(url, params=params, timeout=timeout, stream=stream)
            LAST_REQUEST_AT = time.monotonic()
        if response.status_code != 429:
            response.raise_for_status()
            return response
        response.close()
        retry_after = int(response.headers.get("Retry-After", "0") or 0)
        time.sleep(min(20, max(retry_after, 6 * (attempt + 1))))
    raise requests.HTTPError("Wikimedia rate limit persisted after three retries")


def search_commons(session: requests.Session, city: str, state: str,
                   strict: bool = False) -> tuple[dict[str, Any] | None, list[str]]:
    preferred = preferred_commons_candidate(session, city, state)
    if preferred is not None:
        return preferred, [f"Curated Commons file for {city}, {state}"]
    lead = wikipedia_lead_candidate(session, city, state)
    if lead is not None:
        return lead, [f"Wikipedia lead image: {city}, {state}"]
    queries = [
        f'"{city}" "{state}" skyline',
        f'"{city}" "{state}" downtown',
        f'"{city}" "{state}" aerial city',
        f'"{city}" "{state}"',
    ]
    candidates: dict[str, dict[str, Any]] = {}
    for query in queries:
        response = throttled_get(session, COMMONS_API, params={
            "action": "query", "format": "json", "formatversion": 2,
            "generator": "search", "gsrnamespace": 6, "gsrlimit": 30, "gsrsearch": query,
            "prop": "imageinfo", "iiprop": "url|size|mime|extmetadata", "iiurlwidth": 1800,
        }, timeout=30)
        for page in response.json().get("query", {}).get("pages", []):
            candidate = candidate_from_imageinfo(page)
            if candidate is None:
                continue
            if strict:
                title_norm = slugify(candidate["title"]).replace("-", " ")
                all_norm = slugify(f'{candidate["title"]} {candidate.get("description", "")}').replace("-", " ")
                city_norm = slugify(city).replace("-", " ")
                state_norm = slugify(state).replace("-", " ")
                if city_norm not in title_norm or state_norm not in all_norm:
                    continue
            title = candidate["title"]
            candidate["score"], candidate["review_flags"] = candidate_score(candidate, city, state)
            candidates[title] = candidate
        if candidates and max(item["score"] for item in candidates.values()) >= 25:
            break
    if not candidates:
        return None, queries
    return max(candidates.values(), key=lambda item: (item["score"], item["width"])), queries


def download_image(session: requests.Session, url: str) -> bytes:
    response = throttled_get(session, url, timeout=60, stream=True)
    content_type = response.headers.get("Content-Type", "").lower()
    if not content_type.startswith("image/"):
        raise ValueError(f"Unexpected content type {content_type!r}")
    chunks: list[bytes] = []
    size = 0
    for chunk in response.iter_content(128 * 1024):
        if not chunk:
            continue
        size += len(chunk)
        if size > MAX_DOWNLOAD_BYTES:
            raise ValueError("Image exceeds 25 MiB safety limit")
        chunks.append(chunk)
    return b"".join(chunks)


def write_webp(raw: bytes, target: Path) -> tuple[int, int, str]:
    try:
        with Image.open(io.BytesIO(raw)) as opened:
            image = ImageOps.exif_transpose(opened)
            image.load()
            if image.width / max(image.height, 1) < 1.2:
                raise ValueError("Decoded image is not landscape")
            if image.mode not in {"RGB", "RGBA"}:
                image = image.convert("RGB")
            elif image.mode == "RGBA":
                background = Image.new("RGB", image.size, "white")
                background.paste(image, mask=image.getchannel("A"))
                image = background
            image.thumbnail((1800, 1200), Image.Resampling.LANCZOS)
            target.parent.mkdir(parents=True, exist_ok=True)
            image.save(target, "WEBP", quality=84, method=4)
            digest = hashlib.sha256(target.read_bytes()).hexdigest()
            return image.width, image.height, digest
    except (UnidentifiedImageError, Image.DecompressionBombError) as exc:
        raise ValueError(f"Unsafe or invalid image: {exc}") from exc


def read_jobs(content_dir: Path, all_cities: bool = False) -> list[dict[str, str]]:
    jobs = []
    for path in sorted(content_dir.glob("*.yml")):
        document = yaml.safe_load(path.read_text(encoding="utf-8"))
        rows = document.get("table", {}).get("rows", [])
        if not rows:
            raise ValueError(f"No table rows in {path}")
        state = str(document["page"]["h1"]).split(" in ")[-1]
        for row in rows if all_cities else rows[:1]:
            city = str(row["city"])
            city_slug = str(row.get("city_slug") or slugify(city))
            jobs.append({
                "state_slug": path.stem, "state": state, "city": city, "city_slug": city_slug,
                "manifest_key": f"{path.stem}/{city_slug}" if all_cities else path.stem,
                "strict": all_cities,
            })
    expected = 471 if all_cities else 50
    if len(jobs) != expected:
        raise ValueError(f"Expected {expected} crime city jobs, found {len(jobs)}")
    return jobs


def process_job(job: dict[str, str], image_root: Path, force: bool) -> tuple[str, dict[str, Any]]:
    session = requests.Session()
    session.headers.update({"User-Agent": USER_AGENT, "Accept": "application/json,image/*;q=0.9"})
    state_slug, city_slug = job["state_slug"], job["city_slug"]
    target = image_root / state_slug / f"{city_slug}.webp"
    with PRINT_LOCK:
        print(f"{state_slug}: searching {job['city']}", flush=True)
    selected = job.get("preselected")
    queries = ["Batched exact Wikipedia city article"] if selected else []
    if selected is None and not job.get("exact_only"):
        selected, queries = search_commons(session, job["city"], job["state"], bool(job.get("strict")))
    if selected is None:
        return job["manifest_key"], {key: value for key, value in job.items() if key not in {"preselected", "strict", "manifest_key"}} | {"status": "not_found", "queries": queries}
    # A scheduled job has no trusted manifest record (or was explicitly forced),
    # so always rewrite the file. This keeps attribution correct after an
    # interrupted run that may have left an orphan WebP on disk.
    raw = download_image(session, selected["download_url"])
    output_width, output_height, digest = write_webp(raw, target)
    local_path = f"/images/collections/crime/{state_slug}/{city_slug}.webp"
    result = {
        "state": job["state"], "state_slug": state_slug, "city": job["city"], "city_slug": city_slug,
        "local_path": local_path, "provider": "Wikimedia Commons",
        "author": selected["author"], "license": selected["license"],
        "source_url": selected["source_url"], "original_url": selected["original_url"],
        "width": output_width, "height": output_height, "title": selected["title"],
        "license_url": selected["license_url"], "sha256": digest,
        "review_flags": selected["review_flags"], "selection_score": selected["score"],
    }
    if selected.get("article_url"):
        result["article_url"] = selected["article_url"]
    with PRINT_LOCK:
        print(f"{state_slug}: {job['city']} <- {selected['title']} ({selected['score']})", flush=True)
    return job["manifest_key"], result


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--content", type=Path, default=DEFAULT_CONTENT)
    parser.add_argument("--images", type=Path, default=DEFAULT_IMAGES)
    parser.add_argument("--manifest", type=Path)
    parser.add_argument("--workers", type=int, default=6)
    parser.add_argument("--force", action="store_true")
    parser.add_argument("--states", help="Comma-separated state slugs to redo; other manifest entries are preserved")
    parser.add_argument("--cities", help="Comma-separated city slugs, normally combined with --states")
    parser.add_argument("--all-cities", action="store_true",
                        help="Fetch every ranked city into the separate city-image-credits manifest")
    parser.add_argument("--exact-only", action="store_true",
                        help="Use only batched exact Wikipedia article images; never run Commons search")
    return parser.parse_args()


def checkpoint_manifest(path: Path, manifest: dict[str, dict[str, Any]]) -> None:
    ordered = {key: manifest[key] for key in sorted(manifest)}
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + ".tmp")
    temporary.write_text(json.dumps(ordered, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    temporary.replace(path)


def main() -> int:
    args = parse_args()
    if args.manifest is None:
        args.manifest = DEFAULT_CITY_MANIFEST if args.all_cities else DEFAULT_MANIFEST
    jobs = read_jobs(args.content, args.all_cities)
    manifest: dict[str, dict[str, Any]] = {}
    if args.manifest.is_file():
        existing = json.loads(args.manifest.read_text(encoding="utf-8"))
        if isinstance(existing, dict):
            manifest.update(existing)
    if args.all_cities and DEFAULT_MANIFEST.is_file() and not args.force:
        leaders = json.loads(DEFAULT_MANIFEST.read_text(encoding="utf-8"))
        for job in jobs:
            leader = leaders.get(job["state_slug"], {})
            if (job["manifest_key"] not in manifest and leader.get("city_slug") == job["city_slug"]
                    and leader.get("local_path")):
                manifest[job["manifest_key"]] = {**leader, "state_slug": job["state_slug"]}
    requested = {item.strip() for item in (args.states or "").split(",") if item.strip()}
    if requested:
        jobs = [job for job in jobs if job["state_slug"] in requested]
    requested_cities = {item.strip() for item in (args.cities or "").split(",") if item.strip()}
    if requested_cities:
        jobs = [job for job in jobs if job["city_slug"] in requested_cities]
    if not args.force:
        jobs = [job for job in jobs if not (
            manifest.get(job["manifest_key"], {}).get("local_path") and
            (ROOT / "wwwroot" / manifest[job["manifest_key"]]["local_path"].lstrip("/")).is_file()
        )]
    if args.all_cities and jobs:
        if args.exact_only:
            for job in jobs:
                job["exact_only"] = True
        assigned = batched_wikipedia_leads(jobs)
        print(f"Batched exact Wikipedia leads: {assigned}/{len(jobs)}", flush=True)
    with ThreadPoolExecutor(max_workers=max(1, min(args.workers, 8))) as executor:
        futures = {executor.submit(process_job, job, args.images, args.force): job for job in jobs}
        for future in as_completed(futures):
            job = futures[future]
            try:
                manifest_key, result = future.result()
            except Exception as exc:
                manifest_key = job["manifest_key"]
                result = {key: value for key, value in job.items() if key not in {"preselected", "strict", "manifest_key"}}
                result.update({"status": "error", "error": f"{type(exc).__name__}: {exc}"})
                print(f"{manifest_key}: ERROR {result['error']}", file=sys.stderr, flush=True)
            manifest[manifest_key] = result
            checkpoint_manifest(args.manifest, manifest)
    checkpoint_manifest(args.manifest, manifest)
    ordered = {key: manifest[key] for key in sorted(manifest)}
    selected = [item for item in ordered.values() if item.get("local_path")]
    questionable = [item for item in selected if item.get("review_flags")]
    failed = [item for item in ordered.values() if not item.get("local_path")]
    print(json.dumps({"records": len(ordered), "downloaded": len(selected),
                      "questionable": len(questionable), "failed": len(failed),
                      "manifest": str(args.manifest)}))
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
