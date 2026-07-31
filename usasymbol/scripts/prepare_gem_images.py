from pathlib import Path

import yaml
from PIL import Image, ImageOps


ROOT = Path(__file__).resolve().parents[1]
SOURCE_DIR = ROOT / "wwwroot" / "images" / "gems"

SIMPLE_ASSIGNMENTS = {
    ("gemstone", "Alabama"): "star-blue-quartz.webp",
    ("gemstone", "Alaska"): "Nephrite jade.jpg",
    ("gemstone", "Arkansas"): "Diamond.jpg",
    ("gemstone", "California"): "Benitoite.jpg",
    ("gemstone", "Colorado"): "Aquamarine.jpg",
    ("gemstone", "Florida"): "Moonstone.jpg",
    ("gemstone", "Georgia"): "quartz.jpg",
    ("gemstone", "Hawaii"): "Black coral .jpg",
    ("gemstone", "Idaho"): "Star garnet.jpg",
    ("gemstone", "Kansas"): "Jelenite.jpg",
    ("gemstone", "Kentucky"): "Kentucky agate.jpg",
    ("gemstone", "Louisiana"): "LaPearlite.webp",
    ("gemstone", "Maryland"): "Patuxent River stone.jpg",
    ("gemstone", "Massachusetts"): "Rhodonite.jpg",
    ("gemstone", "Michigan"): "Chlorastrolite.jpg",
    ("gemstone", "Minnesota"): "Lake Superior agate.jpg",
    ("gemstone", "Mississippi"): "Mississippi Opal.jpg",
    ("gemstone", "Nebraska"): "Blue chalcedony.jpg",
    ("gemstone", "New Hampshire"): "Smoky quartz.jpg",
    ("gemstone", "New Mexico"): "Turquoise.jpg",
    ("gemstone", "New York"): "Garnet.jpg",
    ("gemstone", "North Carolina"): "Emerald.jfif",
    ("gemstone", "Ohio"): "Ohio flint.jpg",
    ("gemstone", "Oregon"): "Oregon sunstone.jpg",
    ("gemstone", "South Carolina"): "Amethyst.jpg",
    ("gemstone", "South Dakota"): "Fairburn Agate.jpg",
    ("gemstone", "Utah"): "Topaz.jfif",
    ("gemstone", "Vermont"): "Grossular garnet.jpg",
    ("gemstone", "Washington"): "Petrified wood.jpg",
    (
        "gemstone",
        "West Virginia",
    ): "Mississippian Lithostrotionella fossil coral.webp",
    ("gemstone", "Wyoming"): "Wyoming nephrite jade.jpg",
    ("mineral", "Alabama"): "Hematite.jpg",
    ("mineral", "Alaska"): "Gold.jpg",
    ("mineral", "Arizona"): "Wulfenite jpg.jpg",
    ("mineral", "Arkansas"): "quartz.jpg",
    ("mineral", "California"): "Gold.jpg",
    ("mineral", "Colorado"): "Rhodochrosite.jpg",
    ("mineral", "Connecticut"): "Almandine garnet.jpg",
    ("mineral", "Delaware"): "Sillimanite.jpg",
    ("mineral", "Georgia"): "Staurolite.jpg",
    ("mineral", "Illinois"): "Fluorite.jfif",
    ("mineral", "Kansas"): "Galena.jpg",
    ("mineral", "Kentucky"): "Calcite.jpg",
    ("mineral", "Louisiana"): "Agate.jpg",
    ("mineral", "Maine"): "Tourmaline.jpg",
    ("mineral", "Maryland"): "Chromite.jpg",
    ("mineral", "Massachusetts"): "Babingtonite.jpg",
    ("mineral", "Missouri"): "Galena.jfif",
    ("mineral", "Nevada"): "Silver.jpg",
    ("mineral", "New Hampshire"): "Beryl.jpg",
    ("mineral", "New Jersey"): "Franklinite.jpg",
    ("mineral", "North Carolina"): "Gold.jpg",
    ("mineral", "Rhode Island"): "Bowenite.jfif",
    ("mineral", "South Dakota"): "Rose quartz.jpg",
    ("mineral", "Tennessee"): "Tennessee agate.jpg",
    ("mineral", "Texas"): "Silver.jpg",
    ("mineral", "Utah"): "Copper.jpg",
    ("mineral", "Vermont"): "Talc.jpg",
    ("mineral", "Wisconsin"): "Galena.jpg",
    ("rock", "Alabama"): "Marble.jpg",
    ("rock", "Arkansas"): "Bauxite.jpg",
    ("rock", "California"): "Serpentine.jpg",
    ("rock", "Colorado"): "Yule marble.jpg",
    ("rock", "Florida"): "Agatized Coral.jpg",
    ("rock", "Illinois"): "Dolostone.jfif",
    ("rock", "Indiana"): "Salem limestone.jfif",
    ("rock", "Iowa"): "Geode.jpg",
    ("rock", "Kansas"): "Greenhorn Limestone.jpg",
    ("rock", "Kentucky"): "Coal.jfif",
    ("rock", "Maine"): "Granitic pegmatite.jpg",
    ("rock", "Massachusetts"): "Roxbury puddingstone.jpg",
    ("rock", "Michigan"): "Petoskey stone.jpg",
    ("rock", "Mississippi"): "Petrified wood.jpg",
    ("rock", "Missouri"): "Mozarkite.jpg",
    ("rock", "Nebraska"): "Prairie agate.jpg",
    ("rock", "Nevada"): "Sandstone.jpg",
    ("rock", "New Hampshire"): "Granite.jpg",
    ("rock", "North Dakota"): "Knife River Flint.jpg",
    ("rock", "Oklahoma"): "Barite Rose.jpg",
    ("rock", "Oregon"): "Thunderegg.jpg",
    ("rock", "Rhode Island"): "Cumberlandite.jpg",
    ("rock", "South Carolina"): "Blue granite.jpg",
    ("rock", "Tennessee"): "Limestone.jpg",
    ("rock", "Texas"): "Oligocene petrified palmwood.jpg",
    ("rock", "Utah"): "Coal.jfif",
    ("rock", "Virginia"): "Nelsonite.jpg",
    ("rock", "West Virginia"): "Bituminous coal.jpg",
    ("rock", "Wisconsin"): "Red granite.jpg",
}

SPECIAL_ASSIGNMENTS = {
    ("gemstone", "Arizona"): {
        "hero": ["Turquoise.webp"],
        "details": [("Turquoise.jpg", 0)],
    },
    ("gemstone", "Montana"): {
        "hero": ["Sapphire.jpg", "Montana Agate.jpg"],
        "details": [("Sapphire.jpg", 0), ("Montana Agate.jpg", 1)],
    },
    ("gemstone", "Nevada"): {
        "hero": ["Virgin Valley black fire opal.jpg", "Nevada turquoise.jpg"],
        "details": [
            ("Virgin Valley black fire opal.jpg", 0),
            ("Nevada turquoise.jpg", 1),
        ],
    },
    ("gemstone", "Tennessee"): {
        "hero": ["Tennessee River Pearl.jpg"],
        "details": [("Freshwater pearl.jfif", 0)],
    },
    ("gemstone", "Texas"): {
        "hero": ["Lone Star Cut.jpg"],
        "details": [("Lone Star Cut.jpg", 0)],
        "extra": [
            (
                "Texas blue topaz.jpg",
                "/images/gemstones/texas/texas-blue-topaz-rough.webp",
            )
        ],
    },
    ("mineral", "Oregon"): {
        "hero": ["Oregonite.jfif", "Josephinite.jpg"],
        "detail_composite": 0,
    },
    ("mineral", "Oklahoma"): {
        "hero": ["Hourglass selenite.webp"],
        "details": [("Hourglass selenite.jpg", 0)],
    },
    ("rock", "Vermont"): {
        "hero": ["Granite.jpg", "Marble.jpg", "Slate.jpg"],
        "detail_composite": 0,
    },
}

MISSING_PAGES = set()


def state_slug(state):
    return state.lower().replace(" ", "-")


def load_page(kind, state):
    path = ROOT / "Content" / "states" / state_slug(state) / f"{kind}.yaml"
    with path.open("r", encoding="utf-8") as stream:
        return yaml.safe_load(stream)


def output_path(web_path):
    return ROOT / "wwwroot" / web_path.lstrip("/")


def open_rgb(filename):
    with Image.open(SOURCE_DIR / filename) as image:
        return ImageOps.exif_transpose(image).convert("RGB")


def resize_max(image, max_side):
    output = image.copy()
    if max(output.size) > max_side:
        output.thumbnail((max_side, max_side), Image.Resampling.LANCZOS)
    return output


def save_webp(image, path, quality):
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, "WEBP", quality=quality, method=4)


def convert_one(filename, web_path, max_side, quality):
    image = resize_max(open_rgb(filename), max_side)
    save_webp(image, output_path(web_path), quality)


def make_composite(filenames, size):
    canvas = Image.new("RGB", size, (242, 242, 239))
    gap = 10
    panel_width = (size[0] - gap * (len(filenames) - 1)) // len(filenames)

    for index, filename in enumerate(filenames):
        x_offset = index * (panel_width + gap)
        image = open_rgb(filename)
        fitted = ImageOps.contain(
            image,
            (panel_width - 36, size[1] - 36),
            Image.Resampling.LANCZOS,
        )
        x = x_offset + (panel_width - fitted.width) // 2
        y = (size[1] - fitted.height) // 2
        canvas.paste(fitted, (x, y))

    return canvas


def validate_mapping():
    expected = set()
    for kind in ("gemstone", "mineral", "rock"):
        for path in (ROOT / "Content" / "states").glob(f"*/{kind}.yaml"):
            with path.open("r", encoding="utf-8") as stream:
                data = yaml.safe_load(stream)
            expected.add((kind, data["state"]))

    mapped = set(SIMPLE_ASSIGNMENTS) | set(SPECIAL_ASSIGNMENTS) | MISSING_PAGES
    if mapped != expected:
        raise RuntimeError(
            f"Mapping mismatch: unmapped={sorted(expected - mapped)}, "
            f"extra={sorted(mapped - expected)}"
        )

    used_sources = set(SIMPLE_ASSIGNMENTS.values())
    for assignment in SPECIAL_ASSIGNMENTS.values():
        used_sources.update(assignment.get("hero", []))
        used_sources.update(name for name, _ in assignment.get("details", []))
        used_sources.update(name for name, _ in assignment.get("extra", []))

    invalid = [
        name
        for name in sorted(used_sources)
        if not (SOURCE_DIR / name).is_file()
        or (SOURCE_DIR / name).stat().st_size <= 1
    ]
    if invalid:
        raise RuntimeError(f"Missing or invalid mapped sources: {invalid}")
    return used_sources


def main():
    used_sources = validate_mapping()
    written = []

    for (kind, state), filename in sorted(SIMPLE_ASSIGNMENTS.items()):
        page = load_page(kind, state)
        hero_path = page["hero_image"]
        detail_path = page["visual_assets"][0]["src"]
        convert_one(filename, hero_path, 1600, 82)
        convert_one(filename, detail_path, 1200, 80)
        written.extend([output_path(hero_path), output_path(detail_path)])

    for (kind, state), assignment in sorted(SPECIAL_ASSIGNMENTS.items()):
        page = load_page(kind, state)
        hero_sources = assignment["hero"]

        if len(hero_sources) == 1:
            convert_one(hero_sources[0], page["hero_image"], 1600, 82)
        else:
            save_webp(
                make_composite(hero_sources, (1440, 810)),
                output_path(page["hero_image"]),
                82,
            )
        written.append(output_path(page["hero_image"]))

        if "detail_composite" in assignment:
            index = assignment["detail_composite"]
            detail_path = page["visual_assets"][index]["src"]
            save_webp(
                make_composite(hero_sources, (1320, 820)),
                output_path(detail_path),
                80,
            )
            written.append(output_path(detail_path))

        for filename, index in assignment.get("details", []):
            detail_path = page["visual_assets"][index]["src"]
            convert_one(filename, detail_path, 1200, 80)
            written.append(output_path(detail_path))

        for filename, web_path in assignment.get("extra", []):
            convert_one(filename, web_path, 1200, 80)
            written.append(output_path(web_path))

    source_bytes = sum((SOURCE_DIR / name).stat().st_size for name in used_sources)
    output_bytes = sum(path.stat().st_size for path in written)
    unused = sorted(
        path.name
        for path in SOURCE_DIR.iterdir()
        if path.is_file() and path.name not in used_sources
    )

    print(f"valid_pages={len(SIMPLE_ASSIGNMENTS) + len(SPECIAL_ASSIGNMENTS)}")
    print(f"output_files={len(written)}")
    print(f"unique_sources_used={len(used_sources)}")
    print(f"source_bytes={source_bytes}")
    print(f"output_bytes={output_bytes}")
    print(f"missing_pages={len(MISSING_PAGES)}")
    print(
        "missing="
        + "; ".join(f"{kind}:{state}" for kind, state in sorted(MISSING_PAGES))
    )
    print("unused=" + "; ".join(unused))


if __name__ == "__main__":
    main()
