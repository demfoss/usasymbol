#!/usr/bin/env node

import { readdirSync, renameSync, rmSync } from 'fs';
import { join } from 'path';
import { fileURLToPath } from 'url';
import sharp from 'sharp';

const __dirname = fileURLToPath(new URL('.', import.meta.url));
const ROOT = join(__dirname, '..');
const FOSSILS_DIR = join(ROOT, 'wwwroot', 'images', 'fossils');

function walk(dir) {
  const out = [];
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) out.push(...walk(full));
    else out.push(full);
  }
  return out;
}

const files = walk(FOSSILS_DIR).filter((f) => f.toLowerCase().endsWith('.webp'));
let rewritten = 0;
for (const file of files) {
  const image = sharp(file, { failOn: 'none' }).rotate();
  const meta = await image.metadata();
  const width = meta.width ? Math.min(meta.width, 2200) : 1600;
  const tmp = `${file}.tmp.webp`;
  await image
    .resize({ width, withoutEnlargement: true })
    .webp({ quality: 76, effort: 6 })
    .toFile(tmp);
  rmSync(file, { force: true });
  renameSync(tmp, file);
  rewritten += 1;
}

console.log(`Normalized webp files: ${rewritten}`);
