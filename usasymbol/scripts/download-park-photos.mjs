#!/usr/bin/env node
/**
 * Downloads photos from the NPS API for a national park and saves them locally.
 * Prints a YAML snippet for pasting into the park's media.highlights section.
 *
 * Usage:
 *   node scripts/download-park-photos.mjs grand-canyon-national-park
 *   node scripts/download-park-photos.mjs grand-canyon-national-park --limit 10
 *   NPS_API_KEY=your_key node scripts/download-park-photos.mjs grand-canyon-national-park
 *
 * After running:
 *   1. Compress downloaded images to max 300 KB (use squoosh.app or ffmpeg)
 *   2. Upload to Bunny CDN storage
 *   3. Paste the printed YAML into the park's YAML file under media.highlights
 *      and/or into best_things_to_see_items[].image
 */

import { readFileSync, createWriteStream, mkdirSync, existsSync, statSync, unlinkSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import https from 'https';
import http from 'http';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = join(__dirname, '..');

// ── Args ──────────────────────────────────────────────────────────────
const slug = process.argv[2];
if (!slug || slug.startsWith('--')) {
    console.error('Usage: node scripts/download-park-photos.mjs PARK_SLUG [--limit N]');
    console.error('Example: node scripts/download-park-photos.mjs grand-canyon-national-park');
    process.exit(1);
}

const rawArgs = process.argv.slice(3);
const API_KEY = process.env.NPS_API_KEY || getArg(rawArgs, '--api-key') || '6pExC6Ujf8taPxbUYCKzr2g5QlNVkXdkjGgUCmY5';
const LIMIT   = parseInt(getArg(rawArgs, '--limit') || '12', 10);

function getArg(args, flag) {
    const i = args.indexOf(flag);
    return i >= 0 ? args[i + 1] : null;
}

// ── Read park YAML to get nps_code ────────────────────────────────────
const yamlPath = join(ROOT, 'Content', 'parks', 'national', `${slug}.yml`);
if (!existsSync(yamlPath)) {
    console.error(`Park YAML not found: ${yamlPath}`);
    process.exit(1);
}

const yamlText  = readFileSync(yamlPath, 'utf8');
const codeMatch = yamlText.match(/^nps_code:\s*["']?([a-zA-Z]+)["']?/im);
if (!codeMatch) {
    console.error('Could not find nps_code in YAML');
    process.exit(1);
}
const npsCode = codeMatch[1].toLowerCase();
console.log(`\nPark : ${slug}`);
console.log(`Code : ${npsCode}`);
console.log(`Limit: ${LIMIT} images\n`);

// ── Output directory ──────────────────────────────────────────────────
const outDir  = join(ROOT, 'wwwroot', 'images', 'parks', 'national', slug);
const webBase = `/images/parks/national/${slug}`;
mkdirSync(outDir, { recursive: true });
console.log(`Saving to: ${outDir}\n`);

// ── HTTP fetch with redirect follow ──────────────────────────────────
function fetchJson(url) {
    return new Promise((resolve, reject) => {
        const mod = url.startsWith('https') ? https : http;
        mod.get(url, { headers: { 'User-Agent': 'USASymbol-photos/1.0' } }, (res) => {
            if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
                return fetchJson(res.headers.location).then(resolve).catch(reject);
            }
            let body = '';
            res.on('data', c => body += c);
            res.on('end', () => {
                try { resolve(JSON.parse(body)); }
                catch (e) { reject(new Error(`JSON parse error at ${url}: ${e.message}`)); }
            });
        }).on('error', reject);
    });
}

function downloadFile(url, dest) {
    return new Promise((resolve, reject) => {
        const file = createWriteStream(dest);
        function get(u) {
            const mod = u.startsWith('https') ? https : http;
            mod.get(u, { headers: { 'User-Agent': 'USASymbol-photos/1.0' } }, (res) => {
                if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
                    return get(res.headers.location);
                }
                if (res.statusCode !== 200) {
                    file.close();
                    try { unlinkSync(dest); } catch {}
                    return reject(new Error(`HTTP ${res.statusCode} for ${u}`));
                }
                res.pipe(file);
                file.on('finish', () => file.close(resolve));
                file.on('error', (err) => {
                    try { unlinkSync(dest); } catch {}
                    reject(err);
                });
            }).on('error', (err) => {
                file.close();
                try { unlinkSync(dest); } catch {}
                reject(err);
            });
        }
        get(url);
    });
}

function slugify(str) {
    return (str || 'photo')
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-+|-+$/g, '')
        .slice(0, 40);
}

function sizeKb(path) {
    try { return Math.round(statSync(path).size / 1024); }
    catch { return 0; }
}

// ── Fetch images from NPS API ─────────────────────────────────────────
async function fetchImages() {
    // Primary: galleries/assets endpoint (replaces removed multimedia/images endpoint)
    const multimediaUrl =
        `https://developer.nps.gov/api/v1/multimedia/galleries/assets` +
        `?parkCode=${npsCode}&api_key=${API_KEY}&limit=${LIMIT}&start=0`;

    console.log('Fetching from NPS multimedia/galleries/assets endpoint…');
    try {
        const data = await fetchJson(multimediaUrl);
        const images = (data.data || []).filter(img => {
            const url = img.fileInfo?.url || img.url;
            const fileType = img.fileInfo?.fileType || '';
            return url && fileType.startsWith('image/');
        });
        if (images.length > 0) {
            console.log(`  Found ${data.total} total images, fetching first ${images.length}\n`);
            return images.map(img => ({
                url:    img.fileInfo?.url || img.url,
                title:  img.title || '',
                alt:    img.altText || img.description || img.caption || img.title || '',
                credit: img.credit || img.creditLine || '',
            }));
        }
    } catch (err) {
        console.log(`  Multimedia endpoint failed: ${err.message}`);
    }

    // Fallback: parks endpoint with fields=images
    console.log('Falling back to parks?fields=images endpoint…');
    const parksUrl =
        `https://developer.nps.gov/api/v1/parks` +
        `?parkCode=${npsCode}&api_key=${API_KEY}&fields=images`;
    const data = await fetchJson(parksUrl);
    const park = (data.data || [])[0];
    const images = (park?.images || []).slice(0, LIMIT);
    console.log(`  Found ${images.length} images\n`);
    return images.map(img => ({
        url:    img.url,
        title:  img.title || '',
        alt:    img.altText || img.caption || img.title || '',
        credit: img.credit || '',
    }));
}

// ── Main ──────────────────────────────────────────────────────────────
const images = await fetchImages();
if (images.length === 0) {
    console.error('No images found. Check the NPS code or API key.');
    process.exit(1);
}

const downloaded = [];

for (let i = 0; i < images.length; i++) {
    const img = images[i];
    if (!img.url) { console.log(`  [${i+1}/${images.length}] Skipped — no URL`); continue; }

    const ext      = (img.url.match(/\.(jpg|jpeg|png|webp)/i)?.[1] || 'jpg').toLowerCase().replace('jpeg', 'jpg');
    const baseName = `${String(i + 1).padStart(2, '0')}-${slugify(img.title)}.${ext}`;
    const outPath  = join(outDir, baseName);
    const webPath  = `${webBase}/${baseName}`;

    process.stdout.write(`  [${i+1}/${images.length}] ${baseName} … `);
    try {
        await downloadFile(img.url, outPath);
        const kb = sizeKb(outPath);
        console.log(`${kb} KB${kb > 300 ? ' ⚠ compress to <300 KB' : ' ✓'}`);
        downloaded.push({ webPath, ...img });
    } catch (err) {
        console.log(`FAILED: ${err.message}`);
    }
}

// ── Print YAML snippets ───────────────────────────────────────────────
console.log('\n' + '─'.repeat(70));
console.log('YAML snippet — paste into media.highlights:\n');
console.log('  highlights:');
for (const img of downloaded) {
    const alt    = (img.alt    || img.title).replace(/"/g, "'");
    const credit = (img.credit || '').replace(/"/g, "'");
    console.log(`    - image: "${img.webPath}"`);
    console.log(`      alt: "${alt}"`);
    if (credit) console.log(`      credit: "${credit}"`);
}

console.log('\n' + '─'.repeat(70));
console.log('YAML snippet — paste into sections.best_things_to_see_items');
console.log('(assign image path matching the attraction):\n');
console.log('  best_things_to_see_items:');
for (const img of downloaded) {
    const alt   = (img.alt || img.title).replace(/"/g, "'");
    const name  = (img.title || '').replace(/"/g, "'");
    console.log(`    - name: "${name}"`);
    console.log(`      description: ""`);
    console.log(`      image: "${img.webPath}"`);
    console.log(`      alt: "${alt}"`);
}

console.log('\n' + '─'.repeat(70));
console.log(`\nDone. Downloaded ${downloaded.length}/${images.length} images.`);
console.log(`Next steps:`);
console.log(`  1. Compress images to <300 KB (squoosh.app or sharp CLI)`);
console.log(`  2. Upload to Bunny CDN storage (or run during deploy)`);
console.log(`  3. Paste YAML snippets above into ${slug}.yml`);
console.log(`  4. For best_things_to_see_items: fill in the description field per attraction\n`);
