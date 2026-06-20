#!/usr/bin/env node

import { createHash } from 'crypto';
import { existsSync, readFileSync, readdirSync, writeFileSync } from 'fs';
import { join } from 'path';
import { fileURLToPath } from 'url';
import sharp from 'sharp';

const __dirname = fileURLToPath(new URL('.', import.meta.url));
const ROOT = join(__dirname, '..');
const STATES_DIR = join(ROOT, 'Content', 'states');
const WEBROOT = join(ROOT, 'wwwroot');

function walk(dir) {
  const out = [];
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) out.push(...walk(full));
    else out.push(full);
  }
  return out;
}

function sha(buffer) {
  return createHash('sha256').update(buffer).digest('hex');
}

function parseScalar(text, key) {
  const m = text.match(new RegExp(`^${key}:\\s*(.+)$`, 'm'));
  return m ? m[1].trim().replace(/^"|"$/g, '') : '';
}

function parseFirstSite(text) {
  const m = text.match(/^  - name:\s*(.+)$/m);
  return m ? m[1].trim().replace(/^"|"$/g, '') : '';
}

async function fetchText(url) {
  const res = await fetch(url, {
    headers: { 'user-agent': 'USASymbol Fossil Assets/1.0' }
  });
  if (!res.ok) throw new Error(`${res.status} ${url}`);
  return await res.text();
}

async function fetchBuffer(url) {
  const res = await fetch(url, {
    headers: { 'user-agent': 'USASymbol Fossil Assets/1.0' }
  });
  if (!res.ok) throw new Error(`${res.status} ${url}`);
  return Buffer.from(await res.arrayBuffer());
}

async function getWikiOgImage(title) {
  const url = `https://en.wikipedia.org/wiki/${encodeURIComponent(title.replace(/\s+/g, '_'))}`;
  try {
    const html = await fetchText(url);
    const m = html.match(/property="og:image" content="([^"]+)"/);
    return m ? m[1] : null;
  } catch {
    return null;
  }
}

async function resolveCandidateImage(candidates) {
  for (const c of candidates) {
    if (!c) continue;
    const image = await getWikiOgImage(c);
    if (image) return { title: c, image };
  }
  return null;
}

function toDiskPath(webPath) {
  return join(WEBROOT, webPath.trimStart('/').replaceAll('/', '\\'));
}

const fossilFiles = walk(STATES_DIR).filter((f) => /fossil.*\.ya?ml$/i.test(f));
let downloaded = 0;
let skippedExisting = 0;
let noCandidate = 0;
let sameAsHero = 0;

for (const file of fossilFiles) {
  let text = readFileSync(file, 'utf8');
  if (/^visual_assets:/m.test(text)) {
    skippedExisting += 1;
    continue;
  }

  const stateSlug = file.split('\\states\\')[1].split('\\')[0];
  const heroImage = parseScalar(text, 'hero_image');
  const heroPath = heroImage ? toDiskPath(heroImage) : null;
  let heroHash = null;
  if (heroPath && existsSync(heroPath)) {
    heroHash = sha(readFileSync(heroPath));
  }

  const state = parseScalar(text, 'state');
  const name = parseScalar(text, 'name');
  const common = parseScalar(text, 'common_name');
  const binomial = parseScalar(text, 'binomial_name');
  const site = parseFirstSite(text);
  const baseName = file.split('\\').pop().replace(/\.ya?ml$/i, '');
  const detailName = `${stateSlug}-${baseName}-detail.webp`;
  const detailWebPath = `/images/fossils/${detailName}`;
  const detailDiskPath = toDiskPath(detailWebPath);

  const siteShort = site.replace(/\s*\(.*?\)/g, '').replace(/,.*$/, '').trim();
  const genus = binomial.split(/\s+/)[0] || '';
  const candidates = [
    site,
    siteShort,
    binomial,
    genus,
    common,
    name
  ].filter(Boolean);

  const picked = await resolveCandidateImage(candidates);
  if (!picked) {
    noCandidate += 1;
    continue;
  }

  let sourceBuffer;
  try {
    sourceBuffer = await fetchBuffer(picked.image);
  } catch {
    noCandidate += 1;
    continue;
  }

  if (heroHash && sha(sourceBuffer) === heroHash) {
    sameAsHero += 1;
    continue;
  }

  await sharp(sourceBuffer, { failOn: 'none' })
    .rotate()
    .resize({ width: 1600, withoutEnlargement: true })
    .webp({ quality: 76, effort: 6 })
    .toFile(detailDiskPath);

  const section = site ? 'history' : 'about';
  const id = `${stateSlug}-${(site ? siteShort : name).toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')}`;
  const alt = site
    ? `${siteShort} in ${state}`
    : `${name} fossil or reconstruction`;
  const caption = site
    ? `${siteShort} is associated with ${name} in ${state}.`
    : `${name} appears here in a reference image related to this fossil.`;

  const block = [
    'visual_assets:',
    `  - id: ${id}`,
    `    src: ${detailWebPath}`,
    `    alt: "${alt.replace(/"/g, '\\"')}"`,
    `    caption: "${caption.replace(/"/g, '\\"')}"`,
    `    section: ${section}`,
    '    layout: right',
    ''
  ].join('\n');

  text = text.replace(/\nfaq:/, `\n${block}\nfaq:`);
  writeFileSync(file, text);
  downloaded += 1;
}

console.log(`Downloaded new detail images: ${downloaded}`);
console.log(`Skipped files that already had visual_assets: ${skippedExisting}`);
console.log(`No candidate image found: ${noCandidate}`);
console.log(`Rejected because same as hero: ${sameAsHero}`);
