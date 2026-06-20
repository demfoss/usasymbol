#!/usr/bin/env node

import { readFileSync, writeFileSync, existsSync, readdirSync } from 'fs';
import { join, dirname, basename, extname } from 'path';
import { fileURLToPath } from 'url';
import sharp from 'sharp';

const __dirname = fileURLToPath(new URL('.', import.meta.url));
const ROOT = join(__dirname, '..');
const FOSSILS_DIR = join(ROOT, 'wwwroot', 'images', 'fossils');
const STATES_DIR = join(ROOT, 'Content', 'states');

const FOSSIL_YAML_RE = /fossil.*\.ya?ml$/i;
const DETAIL_REF_RE = /(src:\s*)(\/images\/fossils\/([A-Za-z0-9._/-]+)-detail)\.(jpg|png|webp)/g;
const HERO_REF_RE = /(hero_image:\s*)(\/images\/fossils\/[A-Za-z0-9._/-]+)\.(jpg|png|webp)/g;

function walk(dir) {
  const out = [];
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) out.push(...walk(full));
    else out.push(full);
  }
  return out;
}

function fossilYamlFiles() {
  return walk(STATES_DIR).filter((f) => FOSSIL_YAML_RE.test(f));
}

function toDiskPath(webPath) {
  return join(ROOT, 'wwwroot', webPath.replace(/^\/images\//, 'images/').replaceAll('/', '\\'));
}

function fileExists(webPathWithExt) {
  return existsSync(toDiskPath(webPathWithExt));
}

function fallbackForDetail(detailBase) {
  const fileName = basename(detailBase);
  const stateSlug = fileName.split('-fossil')[0];
  const candidates = [
    `/images/fossils/${stateSlug}.jpg`,
    `/images/fossils/${stateSlug}.png`,
    `/images/fossils/${stateSlug}.webp`
  ];

  for (const candidate of candidates) {
    if (fileExists(candidate)) return candidate;
  }

  return null;
}

async function encodeWebp(inputPath, outputPath, { maxWidth = 1600, quality = 76 } = {}) {
  const image = sharp(inputPath, { failOn: 'none' }).rotate();
  const meta = await image.metadata();
  const width = meta.width ? Math.min(meta.width, maxWidth) : maxWidth;

  await image
    .resize({ width, withoutEnlargement: true })
    .webp({ quality, effort: 6 })
    .toFile(outputPath);
}

async function ensureWebpFromWebPath(sourceWebPath, targetWebPath, options) {
  const sourceDisk = toDiskPath(sourceWebPath);
  const targetDisk = toDiskPath(targetWebPath);
  if (!existsSync(sourceDisk)) return false;
  await encodeWebp(sourceDisk, targetDisk, options);
  return true;
}

const yamlFiles = fossilYamlFiles();
let updatedYamlFiles = 0;
let createdWebps = 0;
const missingSources = [];

for (const yamlFile of yamlFiles) {
  let text = readFileSync(yamlFile, 'utf8');
  let changed = false;

  const detailMatches = [...text.matchAll(DETAIL_REF_RE)];
  for (const match of detailMatches) {
    const original = `${match[2]}.${match[4]}`;
    const target = `${match[2]}.webp`;
    const source = fileExists(original) ? original : fallbackForDetail(match[2]);

    if (!source) {
      missingSources.push({ yamlFile, original });
      continue;
    }

    const ok = await ensureWebpFromWebPath(source, target, { maxWidth: 1600, quality: 74 });
    if (ok) createdWebps += 1;
    text = text.replace(original, target);
    changed = true;
  }

  const heroMatches = [...text.matchAll(HERO_REF_RE)];
  for (const match of heroMatches) {
    const original = `${match[2]}.${match[3]}`;
    const target = `${match[2]}.webp`;
    if (!fileExists(original)) {
      missingSources.push({ yamlFile, original });
      continue;
    }

    const ok = await ensureWebpFromWebPath(original, target, { maxWidth: 2200, quality: 78 });
    if (ok) createdWebps += 1;
    text = text.replace(original, target);
    changed = true;
  }

  if (changed) {
    writeFileSync(yamlFile, text);
    updatedYamlFiles += 1;
  }
}

const allFossilImages = walk(FOSSILS_DIR).filter((file) => /\.(jpe?g|png)$/i.test(file));
for (const file of allFossilImages) {
  const target = file.replace(/\.(jpe?g|png)$/i, '.webp');
  await encodeWebp(file, target, { maxWidth: 2200, quality: 76 });
}

console.log(`Updated YAML files: ${updatedYamlFiles}`);
console.log(`Created/overwrote referenced WEBP files: ${createdWebps}`);
console.log(`Converted all fossil JPG/PNG files to WEBP: ${allFossilImages.length}`);
if (missingSources.length) {
  console.log('Missing sources:');
  for (const item of missingSources) {
    console.log(`${item.yamlFile} -> ${item.original}`);
  }
}
