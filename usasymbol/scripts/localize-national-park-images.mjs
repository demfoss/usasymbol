#!/usr/bin/env node

import { existsSync, mkdirSync, readFileSync, readdirSync, statSync, writeFileSync } from 'fs';
import { dirname, extname, join } from 'path';
import { fileURLToPath } from 'url';
import http from 'http';
import https from 'https';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = join(__dirname, '..');
const PARKS_DIR = join(ROOT, 'Content', 'parks', 'national');
const IMAGES_DIR = join(ROOT, 'wwwroot', 'images', 'parks', 'national');
const TODAY = new Date().toISOString().slice(0, 10);

const args = process.argv.slice(2);
const force = args.includes('--force');
const slugs = args.filter(arg => !arg.startsWith('--'));

function slugify(text) {
    return (text || 'image')
        .normalize('NFKD')
        .replace(/[^\x00-\x7F]/g, '')
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-+|-+$/g, '')
        .slice(0, 60) || 'image';
}

function fetchBuffer(url) {
    return new Promise((resolve, reject) => {
        function get(currentUrl, redirects = 0) {
            const mod = currentUrl.startsWith('https') ? https : http;
            mod.get(currentUrl, { headers: { 'User-Agent': 'USASymbol-localizer/1.0' } }, (res) => {
                if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
                    if (redirects > 10) return reject(new Error(`Too many redirects for ${url}`));
                    const next = new URL(res.headers.location, currentUrl).toString();
                    return get(next, redirects + 1);
                }
                if (res.statusCode !== 200) {
                    return reject(new Error(`HTTP ${res.statusCode} for ${currentUrl}`));
                }
                const chunks = [];
                res.on('data', chunk => chunks.push(chunk));
                res.on('end', () => resolve({
                    buffer: Buffer.concat(chunks),
                    contentType: res.headers['content-type'] || '',
                    finalUrl: currentUrl,
                }));
            }).on('error', reject);
        }
        get(url);
    });
}

function pickExtension(url, contentType) {
    const cleanPath = new URL(url).pathname;
    const ext = extname(cleanPath).toLowerCase();
    if (ext && ['.jpg', '.jpeg', '.png', '.webp', '.gif'].includes(ext)) {
        return ext === '.jpeg' ? '.jpg' : ext;
    }
    if (contentType.includes('png')) return '.png';
    if (contentType.includes('webp')) return '.webp';
    if (contentType.includes('gif')) return '.gif';
    return '.jpg';
}

function normalizeImageUrl(url) {
    const parsed = new URL(url);
    if (parsed.hostname.endsWith('nps.gov')) {
        parsed.search = '';
    }
    return parsed.toString();
}

function collectTargets(lines) {
    const targets = [];
    let inHighlights = false;
    let inBestThings = false;
    let inSectionImages = false;
    let currentBestThing = '';
    let highlightIndex = 0;

    for (let i = 0; i < lines.length; i++) {
        const line = lines[i];

        if (/^[^\s].*:\s*$/.test(line) && !/^section_images:\s*$/.test(line) && !/^media:\s*$/.test(line) && !/^sections:\s*$/.test(line)) {
            inHighlights = false;
            inBestThings = false;
            inSectionImages = false;
            currentBestThing = '';
        }

        if (/^  highlights:\s*$/.test(line)) {
            inHighlights = true;
            inBestThings = false;
            inSectionImages = false;
            continue;
        }
        if (/^  best_things_to_see_items:\s*$/.test(line)) {
            inHighlights = false;
            inBestThings = true;
            inSectionImages = false;
            continue;
        }
        if (/^section_images:\s*$/.test(line)) {
            inHighlights = false;
            inBestThings = false;
            inSectionImages = true;
            currentBestThing = '';
            continue;
        }

        const heroMatch = line.match(/^  hero_image:\s*"([^"]+)"/);
        if (heroMatch && /^https?:\/\//.test(heroMatch[1])) {
            targets.push({ lineIndex: i, url: heroMatch[1], label: 'hero-image' });
            continue;
        }

        if (inHighlights) {
            const match = line.match(/^    - image:\s*"([^"]+)"/);
            if (match && /^https?:\/\//.test(match[1])) {
                highlightIndex += 1;
                targets.push({ lineIndex: i, url: match[1], label: `highlight-${String(highlightIndex).padStart(2, '0')}` });
            }
            continue;
        }

        if (inBestThings) {
            const nameMatch = line.match(/^    - name:\s*"([^"]+)"/);
            if (nameMatch) {
                currentBestThing = nameMatch[1];
                continue;
            }
            const imageMatch = line.match(/^      image:\s*"([^"]+)"/);
            if (imageMatch && /^https?:\/\//.test(imageMatch[1])) {
                targets.push({ lineIndex: i, url: imageMatch[1], label: currentBestThing || 'best-thing' });
            }
            continue;
        }

        if (inSectionImages) {
            const match = line.match(/^  ([a-z_]+):\s*"([^"]+)"/);
            if (match && /^https?:\/\//.test(match[2])) {
                targets.push({ lineIndex: i, url: match[2], label: match[1] });
            }
        }
    }

    return targets;
}

async function localizeFile(filePath) {
    if (!existsSync(filePath)) {
        console.log(`Missing file: ${filePath}`);
        return { changed: false, downloads: 0 };
    }

    const slug = filePath.replace(/\\/g, '/').split('/').pop().replace(/\.yml$/, '');
    const outDir = join(IMAGES_DIR, slug);
    mkdirSync(outDir, { recursive: true });

    const original = readFileSync(filePath, 'utf8');
    const lines = original.split(/\r?\n/);
    const targets = collectTargets(lines);
    if (targets.length === 0) {
        return { changed: false, downloads: 0 };
    }

    const urlMap = new Map();
    let sequence = 0;
    let downloads = 0;

    for (const target of targets) {
        if (!urlMap.has(target.url)) {
            sequence += 1;
            const normalizedUrl = normalizeImageUrl(target.url);
            const label = `${String(sequence).padStart(2, '0')}-${slugify(target.label)}`;
            const fetched = await fetchBuffer(normalizedUrl);
            const ext = pickExtension(normalizedUrl, fetched.contentType);
            const fileName = `${label}${ext}`;
            const localPath = join(outDir, fileName);
            if (force || !existsSync(localPath) || statSync(localPath).size === 0) {
                writeFileSync(localPath, fetched.buffer);
                downloads += 1;
            }
            urlMap.set(target.url, `/images/parks/national/${slug}/${fileName}`);
        }
    }

    for (const target of targets) {
        lines[target.lineIndex] = lines[target.lineIndex].replace(target.url, urlMap.get(target.url));
    }

    const dateIndex = lines.findIndex(line => /^date_modified:\s*"/.test(line));
    if (dateIndex >= 0) {
        lines[dateIndex] = `date_modified: "${TODAY}"`;
    }

    const updated = lines.join('\n');
    if (updated !== original) {
        writeFileSync(filePath, updated, 'utf8');
        return { changed: true, downloads };
    }
    return { changed: false, downloads };
}

const files = slugs.length > 0
    ? slugs.map(slug => join(PARKS_DIR, `${slug}.yml`))
    : readdirSync(PARKS_DIR)
        .filter(name => name.endsWith('.yml'))
        .map(name => join(PARKS_DIR, name));

let changedFiles = 0;
let totalDownloads = 0;

for (const file of files) {
    const result = await localizeFile(file);
    if (result.changed) {
        changedFiles += 1;
        totalDownloads += result.downloads;
        console.log(`Localized ${file.replace(`${ROOT}\\`, '')} (${result.downloads} downloads)`);
    }
}

console.log(`Done. Changed files: ${changedFiles}. Downloads: ${totalDownloads}.`);
