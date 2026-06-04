#!/usr/bin/env node

import { readFileSync, readdirSync, statSync, writeFileSync } from 'fs';
import { join, relative } from 'path';
import { fileURLToPath } from 'url';
import sharp from 'sharp';

const __dirname = fileURLToPath(new URL('.', import.meta.url));
const ROOT = join(__dirname, '..');
const PARKS_DIR = join(ROOT, 'Content', 'parks', 'national');
const IMAGES_DIR = join(ROOT, 'wwwroot', 'images', 'parks', 'national');

const HERO_TARGET_BYTES = 450 * 1024;
const NON_HERO_TARGET_BYTES = 200 * 1024;
const HERO_MAX_WIDTH = 2200;
const NON_HERO_MAX_WIDTH = 1600;
const MIN_WIDTH = 480;
const WORKERS = 6;

const LOCAL_IMAGE_PATTERN = /"?(\/images\/parks\/national\/[^"\s]+)"?/g;
const HERO_PATTERN = /^\s*hero_image:\s*"(\/images\/parks\/national\/[^"]+)"/gm;

function walk(dir) {
    const entries = [];
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
        const fullPath = join(dir, entry.name);
        if (entry.isDirectory()) {
            entries.push(...walk(fullPath));
        } else {
            entries.push(fullPath);
        }
    }
    return entries;
}

function collectReferencedImages() {
    const heroPaths = new Set();
    const allPaths = new Set();

    for (const fileName of readdirSync(PARKS_DIR)) {
        if (!fileName.endsWith('.yml')) continue;
        const text = readFileSync(join(PARKS_DIR, fileName), 'utf8');

        for (const match of text.matchAll(HERO_PATTERN)) {
            heroPaths.add(match[1]);
            allPaths.add(match[1]);
        }

        for (const match of text.matchAll(LOCAL_IMAGE_PATTERN)) {
            allPaths.add(match[1]);
        }
    }

    return { heroPaths, allPaths };
}

async function encode(buffer, format, width, quality) {
    let pipeline = sharp(buffer, { failOn: 'none' }).rotate();

    if (width > 0) {
        pipeline = pipeline.resize({ width, withoutEnlargement: true });
    }

    if (format === 'png') {
        return pipeline
            .png({
                compressionLevel: 9,
                palette: true,
                quality,
                effort: 10
            })
            .toBuffer();
    }

    return pipeline
        .jpeg({
            quality,
            mozjpeg: true,
            progressive: true,
            chromaSubsampling: '4:2:0'
        })
        .toBuffer();
}

async function compressImage(filePath, { targetBytes, maxWidth }) {
    const originalBuffer = readFileSync(filePath);
    const image = sharp(originalBuffer, { failOn: 'none' }).rotate();
    const metadata = await image.metadata();

    if (!metadata.width || !metadata.height) {
        return { changed: false, skipped: true, reason: 'missing-metadata' };
    }

    const ext = filePath.toLowerCase().endsWith('.png') ? 'png' : filePath.toLowerCase().endsWith('.gif') ? 'gif' : 'jpg';
    if (ext === 'gif') {
        return { changed: false, skipped: true, reason: 'gif' };
    }

    const originalSize = originalBuffer.length;
    if (originalSize <= targetBytes && metadata.width <= maxWidth) {
        return { changed: false, skipped: true, reason: 'already-small', originalSize, finalSize: originalSize, hitTarget: true };
    }

    const cappedWidth = Math.min(metadata.width, maxWidth);
    const widthSteps = [];
    for (const scale of [1, 0.9, 0.82, 0.74, 0.66, 0.58, 0.5, 0.42, 0.34]) {
        const width = Math.max(MIN_WIDTH, Math.round(cappedWidth * scale));
        if (!widthSteps.includes(width)) widthSteps.push(width);
        if (width === MIN_WIDTH) break;
    }

    const qualitySteps = ext === 'png'
        ? [90, 82, 74, 66, 58, 50, 42]
        : [82, 76, 70, 64, 58, 52, 46, 40, 34];

    let bestBuffer = originalBuffer;
    let bestScore = Number.POSITIVE_INFINITY;

    for (const width of widthSteps) {
        for (const quality of qualitySteps) {
            const candidate = await encode(originalBuffer, ext, width, quality);
            const score = candidate.length <= targetBytes
                ? (targetBytes - candidate.length)
                : (candidate.length - targetBytes) + 10_000_000;

            if (score < bestScore) {
                bestScore = score;
                bestBuffer = candidate;
            }

            if (candidate.length <= targetBytes) {
                writeFileSync(filePath, candidate);
                return { changed: true, originalSize, finalSize: candidate.length, hitTarget: true };
            }
        }
    }

    if (bestBuffer.length < originalSize) {
        writeFileSync(filePath, bestBuffer);
        return { changed: true, originalSize, finalSize: bestBuffer.length, hitTarget: bestBuffer.length <= targetBytes };
    }

    return { changed: false, originalSize, finalSize: originalSize, hitTarget: originalSize <= targetBytes };
}

const { heroPaths, allPaths } = collectReferencedImages();
const diskFiles = walk(IMAGES_DIR)
    .filter(path => /\.(jpe?g|png|gif)$/i.test(path));

let changed = 0;
let skipped = 0;
let targetMisses = 0;
let bytesSaved = 0;

async function processDiskPath(diskPath) {
    const webPath = '/' + relative(ROOT, diskPath).replaceAll('\\', '/');
    const isHero = heroPaths.has(webPath);
    const isReferenced = allPaths.has(webPath);
    const targetBytes = isHero ? HERO_TARGET_BYTES : NON_HERO_TARGET_BYTES;
    const maxWidth = isHero ? HERO_MAX_WIDTH : NON_HERO_MAX_WIDTH;

    const result = await compressImage(diskPath, { targetBytes, maxWidth });
    if (result.skipped) {
        skipped += 1;
        return;
    }

    if (result.changed) {
        changed += 1;
        bytesSaved += Math.max(0, (result.originalSize ?? 0) - (result.finalSize ?? 0));
    }

    if (!isHero && isReferenced && !(result.hitTarget ?? false)) {
        targetMisses += 1;
        console.log(`OVER TARGET: ${webPath} -> ${Math.round((result.finalSize ?? 0) / 1024)} KB`);
    }
}

let cursor = 0;
const workers = Array.from({ length: Math.min(WORKERS, diskFiles.length) }, async () => {
    while (cursor < diskFiles.length) {
        const index = cursor++;
        await processDiskPath(diskFiles[index]);
    }
});

await Promise.all(workers);

console.log(`Compressed files: ${changed}`);
console.log(`Skipped files: ${skipped}`);
console.log(`Non-hero files still over 200 KB: ${targetMisses}`);
console.log(`Saved: ${Math.round(bytesSaved / 1024 / 1024)} MB`);
