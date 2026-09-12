import io
import pathlib
import time
import urllib.parse

import requests
import yaml
from PIL import Image


ROOT = pathlib.Path(__file__).resolve().parents[1]
PAGES = [
    "most-famous-athlete-by-state.yml",
    "most-famous-author-by-state.yml",
    "most-famous-comedian-by-state.yml",
    "most-famous-scientist-by-state.yml",
    "most-famous-chef-by-state.yml",
    "most-famous-criminal-by-state.yml",
]
API_HEADERS = {"User-Agent": "usasymbol-image-import/1.0", "Accept": "application/json"}
IMAGE_HEADERS = {"User-Agent": "Mozilla/5.0"}


def cards_to_download():
    cards = []
    for page_name in PAGES:
        page_path = ROOT / "Content" / "collections" / "culture" / page_name
        data = yaml.safe_load(page_path.read_text(encoding="utf-8")) or {}
        for section in data.get("sections") or []:
            for highlight in section.get("highlights") or []:
                target = ROOT / "wwwroot" / highlight["image"].lstrip("/")
                if not target.is_file():
                    cards.append((highlight["name"].split(": ", 1)[-1], target))
    return cards


def fetch_image(session, name):
    summary_url = "https://en.wikipedia.org/api/rest_v1/page/summary/" + urllib.parse.quote(name.replace(" ", "_"))
    response = session.get(summary_url, headers=API_HEADERS, timeout=30)
    if response.status_code == 429:
        return None, "rate-limit"
    if response.status_code == 404:
        return None, "not-found"
    response.raise_for_status()
    return response.json().get("thumbnail", {}).get("source"), None


def main():
    session = requests.Session()
    remaining = cards_to_download()
    print(f"Pending Wikimedia portraits: {len(remaining)}", flush=True)

    for index, (name, target) in enumerate(remaining, 1):
        while True:
            try:
                source, problem = fetch_image(session, name)
                if problem == "rate-limit":
                    print(f"Rate limited before {name}; waiting 90 seconds", flush=True)
                    time.sleep(90)
                    continue
                if problem == "not-found":
                    print(f"No Wikipedia article: {name}", flush=True)
                    break
                if not source:
                    print(f"No Wikipedia thumbnail: {name}", flush=True)
                    break

                image_response = session.get(source, headers=IMAGE_HEADERS, timeout=60)
                if image_response.status_code == 429:
                    print(f"Rate limited on {name}; waiting 90 seconds", flush=True)
                    time.sleep(90)
                    continue
                image_response.raise_for_status()
                image = Image.open(io.BytesIO(image_response.content)).convert("RGB")
                image.thumbnail((1000, 1000), Image.Resampling.LANCZOS)
                target.parent.mkdir(parents=True, exist_ok=True)
                image.save(target, "JPEG", quality=90, optimize=True)
                print(f"{index}/{len(remaining)} {name}", flush=True)
                time.sleep(2)
                break
            except requests.RequestException as error:
                print(f"Request failed for {name}: {error}; waiting 90 seconds", flush=True)
                time.sleep(90)


if __name__ == "__main__":
    main()
