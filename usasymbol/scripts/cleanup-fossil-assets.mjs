#!/usr/bin/env node

import { readdirSync, readFileSync, rmSync, writeFileSync, existsSync } from 'fs';
import { join } from 'path';
import { fileURLToPath } from 'url';

const __dirname = fileURLToPath(new URL('.', import.meta.url));
const ROOT = join(__dirname, '..');
const STATES_DIR = join(ROOT, 'Content', 'states');
const FOSSILS_DIR = join(ROOT, 'wwwroot', 'images', 'fossils');

const KEEP_DETAIL_BASENAMES = new Set([
  'alabama-fossil',
  'alaska-fossil',
  'arizona-fossil',
  'california-fossil',
  'delaware-fossil',
  'kansas-fossil-flying',
  'kansas-fossil-marine',
  'kentucky-fossil',
  'missouri-fossil',
  'ohio-fossil-fish',
  'ohio-fossil-invertebrate',
  'south-dakota-fossil',
  'vermont-fossil-marine',
  'vermont-fossil-terrestrial',
  'wyoming-fossil'
]);

function walk(dir) {
  const out = [];
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) out.push(...walk(full));
    else out.push(full);
  }
  return out;
}

const fossilYamlFiles = walk(STATES_DIR).filter((f) => /fossil.*\.ya?ml$/i.test(f));

let yamlCleaned = 0;
for (const file of fossilYamlFiles) {
  const text = readFileSync(file, 'utf8');
  const m = text.match(/src:\s+\/images\/fossils\/([^\s]+)-detail\.webp/);
  if (!m) continue;
  const base = m[1];
  if (KEEP_DETAIL_BASENAMES.has(base)) continue;

  const cleaned = text.replace(/\r?\nvisual_assets:\r?\n(?:  - .*\r?\n(?:    .*\r?\n)*)/m, '');
  if (cleaned !== text) {
    writeFileSync(file, cleaned);
    yamlCleaned += 1;
  }
}

let deletedDetails = 0;
for (const file of walk(FOSSILS_DIR)) {
  const normalized = file.replaceAll('\\', '/');
  const m = normalized.match(/\/([^/]+)-detail\.webp$/);
  if (!m) continue;
  if (KEEP_DETAIL_BASENAMES.has(m[1])) continue;
  rmSync(file, { force: true });
  deletedDetails += 1;
}

let deletedLegacy = 0;
for (const file of walk(FOSSILS_DIR)) {
  if (!/\.(jpe?g|png)$/i.test(file)) continue;
  const webp = file.replace(/\.(jpe?g|png)$/i, '.webp');
  if (!existsSync(webp)) continue;
  rmSync(file, { force: true });
  deletedLegacy += 1;
}

console.log(`Cleaned YAML visual_assets blocks: ${yamlCleaned}`);
console.log(`Deleted fake detail webp files: ${deletedDetails}`);
console.log(`Deleted legacy jpg/png files with webp equivalents: ${deletedLegacy}`);
