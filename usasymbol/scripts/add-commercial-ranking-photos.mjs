import fs from "node:fs/promises";
import path from "node:path";
import sharp from "sharp";
import { parse } from "yaml";

const pixabayKey = process.env.PIXABAY_KEY;
if (!pixabayKey) throw new Error("Missing PIXABAY_KEY environment variable");

const pages = [
  ["car-dealerships-by-state", ["car dealership showroom", "new cars dealership lot", "car salesperson showroom"]],
  ["grocery-stores-by-state", ["grocery store supermarket aisle", "fresh produce supermarket", "grocery shopping cart aisle"]],
  ["hotels-by-state", ["hotel building exterior", "hotel reception lobby", "hotel room interior"]],
  ["ace-hardware-locations-by-state", ["hardware store tools", "hardware tools shop", "small town hardware store"]],
  ["autozone-locations-by-state", ["auto parts store", "car parts shelves", "automotive parts tools"]],
  ["oreilly-auto-parts-locations-by-state", ["automotive parts store", "car repair parts", "auto mechanic parts"]],
  ["harbor-freight-locations-by-state", ["power tools hardware store", "tools workshop bench", "workshop power tools"]],
  ["petco-locations-by-state", ["dog pet shop", "dog food bowl", "cat pet toys"]],
  ["petsmart-locations-by-state", ["dog cat together", "dog toys", "cat food bowl"]],
  ["ulta-beauty-locations-by-state", ["cosmetics beauty store", "makeup store shelves", "beauty products retail"]],
];

const usedIds = new Set();
const manifest = [];
const tiles = [];

async function findPhoto(query) {
  const url = new URL("https://pixabay.com/api/");
  url.searchParams.set("key", pixabayKey);
  url.searchParams.set("q", query);
  url.searchParams.set("image_type", "photo");
  url.searchParams.set("orientation", "horizontal");
  url.searchParams.set("safesearch", "true");
  url.searchParams.set("per_page", "30");
  const response = await fetch(url, { headers: { "User-Agent": "USA-Symbol-Ranking-Assets/1.0" } });
  if (!response.ok) throw new Error(`Pixabay search failed for ${query}: ${response.status}`);
  const data = await response.json();
  const hit = data.hits?.find((item) => !usedIds.has(item.id) && (item.largeImageURL || item.webformatURL));
  if (!hit) throw new Error(`No unused Pixabay image for ${query}`);
  usedIds.add(hit.id);
  return hit;
}

async function download(hit, output) {
  const response = await fetch(hit.largeImageURL || hit.webformatURL, { headers: { "User-Agent": "USA-Symbol-Ranking-Assets/1.0" } });
  if (!response.ok) throw new Error(`Pixabay download failed: ${response.status}`);
  const input = Buffer.from(await response.arrayBuffer());
  await sharp(input).resize(1600, 900, { fit: "cover" }).jpeg({ quality: 85, mozjpeg: true }).toFile(output);
}

for (const [slug, queries] of pages) {
  const yamlPath = `Content/rankings/economy/${slug}.yml`;
  const page = parse(await fs.readFile(yamlPath, "utf8"));
  if (!page.hero_image || page.visual_assets?.length !== 2) throw new Error(`Expected one hero and two section assets in ${slug}`);
  const assets = [{ src: page.hero_image, alt: page.hero_image_alt, section: "hero" }, ...page.visual_assets];
  if (new Set(assets.map((asset) => asset.src)).size !== 3) throw new Error(`Duplicate paths in ${slug}`);
  for (let index = 0; index < assets.length; index++) {
    const asset = assets[index];
    const expectedPrefix = `/images/rankings/economy/${slug}/`;
    if (!asset.src.startsWith(expectedPrefix)) throw new Error(`Unexpected asset path in ${slug}: ${asset.src}`);
    const output = `wwwroot${asset.src}`;
    await fs.mkdir(path.dirname(output), { recursive: true });
    const hit = await findPhoto(queries[index]);
    await download(hit, output);
    const metadata = await sharp(output).metadata();
    if (metadata.width !== 1600 || metadata.height !== 900) throw new Error(`Invalid dimensions for ${output}`);
    const tile = await sharp(output).resize(320, 180).toBuffer();
    const tileIndex = tiles.length;
    tiles.push({ input: tile, left: (tileIndex % 3) * 320, top: Math.floor(tileIndex / 3) * 180 });
    manifest.push({
      page: slug,
      src: asset.src,
      alt: asset.alt,
      section: asset.section,
      source: hit.pageURL,
      image_id: hit.id,
      photographer: hit.user,
      license: "https://pixabay.com/service/license-summary/",
      illustrative: true,
    });
    console.log(`${slug}: ${path.basename(output)} (${hit.id})`);
  }
}

const artifactDir = "artifacts/commercial-ranking-photos";
await fs.mkdir(artifactDir, { recursive: true });
await fs.writeFile(`${artifactDir}/sources.json`, `${JSON.stringify(manifest, null, 2)}\n`);
await sharp({ create: { width: 960, height: Math.ceil(tiles.length / 3) * 180, channels: 3, background: "white" } })
  .composite(tiles)
  .jpeg({ quality: 85 })
  .toFile(`${artifactDir}/preview.jpg`);
console.log(`Saved ${manifest.length} unique images for ${pages.length} pages.`);
