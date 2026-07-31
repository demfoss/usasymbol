#!/usr/bin/env node

import {
    existsSync,
    mkdirSync,
    readFileSync,
    readdirSync,
    statSync,
    writeFileSync
} from 'fs';
import { basename, dirname, join, relative } from 'path';
import sharp from 'sharp';
import { parse } from 'yaml';

const ROOT = join(import.meta.dirname, '..');
const STATES_DIR = join(ROOT, 'Content', 'states');
const WWWROOT = join(ROOT, 'wwwroot');
const ARTIFACT_DIR = join(ROOT, 'artifacts', 'food-detail-image-sources');
const MANIFEST_PATH = join(ARTIFACT_DIR, 'manifest.json');
const GEOLOGY_MANIFEST_PATH = join(
    ROOT,
    'artifacts',
    'geology-image-sources',
    'manifest.json'
);
const HUBS = ['fruits', 'desserts', 'dishes', 'vegetables', 'crops', 'nuts'];
const EXPECTED_COUNTS = {
    fruits: 37,
    desserts: 20,
    dishes: 21,
    vegetables: 18,
    crops: 16,
    nuts: 6
};
const TARGET_WIDTH = 1600;
const TARGET_HEIGHT = 900;
const WEBP_QUALITY = 82;
const PIXABAY_KEY = process.env.PIXABAY_API_KEY ?? '';
const PEXELS_KEY = process.env.PEXELS_API_KEY ?? '';

const delay = (milliseconds) =>
    new Promise((resolve) => setTimeout(resolve, milliseconds));

function slugify(value) {
    return value
        .normalize('NFKD')
        .replace(/\p{Diacritic}/gu, '')
        .toLowerCase()
        .replace(/&/g, ' and ')
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/-+/g, '-')
        .replace(/^-|-$/g, '');
}

function yamlQuote(value) {
    return JSON.stringify(String(value).replace(/\s+/g, ' ').trim());
}

function compactSearchName(name) {
    return name
        .replace(/\([^)]*\)/g, ' ')
        .replace(/\b(and|or)\b.*$/i, ' ')
        .replace(/[^\p{L}\p{N}\s-]/gu, ' ')
        .replace(/\s+/g, ' ')
        .trim();
}

function collectFoodPagesByHero() {
    const pages = new Map();

    for (const stateSlug of readdirSync(STATES_DIR).sort()) {
        const stateDir = join(STATES_DIR, stateSlug);
        if (!statSync(stateDir).isDirectory()) continue;

        for (const fileName of readdirSync(stateDir).filter((name) => /^food-.*\.yaml$/i.test(name))) {
            const yamlPath = join(stateDir, fileName);
            const text = readFileSync(yamlPath, 'utf8');
            const data = parse(text);
            if (typeof data.hero_image !== 'string') continue;
            pages.set(data.hero_image, {
                stateSlug,
                fileName,
                yamlPath,
                text,
                data
            });
        }
    }

    return pages;
}

function collectTargetPages() {
    const pagesByHero = collectFoodPagesByHero();
    const selected = [];
    const seen = new Set();
    const counts = {};

    for (const hubName of HUBS) {
        const hub = parse(
            readFileSync(join(ROOT, 'Content', 'symbols', `${hubName}.yml`), 'utf8')
        );
        const rows = hub?.table?.rows;
        if (!Array.isArray(rows)) {
            throw new Error(`${hubName}.yml has no table.rows`);
        }

        const targetRows = rows.filter((row) => row.state_slug !== 'alabama');
        counts[hubName] = targetRows.length;
        if (targetRows.length !== EXPECTED_COUNTS[hubName]) {
            throw new Error(
                `${hubName}.yml: expected ${EXPECTED_COUNTS[hubName]} non-Alabama rows, found ${targetRows.length}`
            );
        }

        for (const row of targetRows) {
            const page = pagesByHero.get(row.symbol_image);
            if (!page) {
                throw new Error(
                    `${hubName}.yml: no detail YAML matches ${row.symbol_image}`
                );
            }
            const key = `${page.stateSlug}/${page.fileName}`;
            if (seen.has(key)) {
                throw new Error(`Food page appears in multiple target hubs: ${key}`);
            }
            seen.add(key);

            const sectionIds = new Set(
                (page.data.sections ?? []).map((section) => section.id)
            );
            const detailSection = sectionIds.has('about')
                ? 'about'
                : sectionIds.has('overview')
                    ? 'overview'
                    : [...sectionIds][0];
            const contextSection = sectionIds.has('location')
                ? 'location'
                : sectionIds.has('selection')
                    ? 'selection'
                    : sectionIds.has('reason')
                        ? 'reason'
                        : [...sectionIds].find((id) => id !== detailSection);
            if (!detailSection || !contextSection || detailSection === contextSection) {
                throw new Error(`${key}: cannot select two different H2 sections`);
            }

            const nameSlug = slugify(page.data.name);
            selected.push({
                ...page,
                key,
                hubName,
                detailSection,
                contextSection,
                detailWebPath:
                    `/images/foods/${page.stateSlug}/${page.stateSlug}-${nameSlug}-detail.webp`,
                contextWebPath:
                    `/images/foods/${page.stateSlug}/${page.stateSlug}-${nameSlug}-context.webp`
            });
        }
    }

    if (selected.length !== 118) {
        throw new Error(`Expected 118 target food pages, found ${selected.length}`);
    }

    return { pages: selected, counts };
}

async function fetchJson(url, options = {}) {
    const response = await fetch(url, options);
    if (!response.ok) {
        throw new Error(`Image search request failed with HTTP ${response.status}`);
    }
    return response.json();
}

async function searchPixabay(page, usedIdentities) {
    if (!PIXABAY_KEY) {
        throw new Error('PIXABAY_API_KEY is required for food image enrichment');
    }

    const compactName = compactSearchName(page.data.name);
    const queries = [
        `${compactName} food`,
        `${compactName} cooking`,
        `${compactName} fresh`
    ];

    for (const query of queries) {
        const params = new URLSearchParams({
            key: PIXABAY_KEY,
            q: query.slice(0, 100),
            image_type: 'photo',
            orientation: 'horizontal',
            safesearch: 'true',
            order: 'popular',
            per_page: '30'
        });
        const data = await fetchJson(`https://pixabay.com/api/?${params}`);
        await delay(650);

        const hit = (data.hits ?? []).find((candidate) => {
            const identity = `pixabay:${candidate.id}`;
            return (
                !usedIdentities.has(identity) &&
                candidate.largeImageURL &&
                candidate.imageWidth >= 1000 &&
                candidate.imageHeight >= 600
            );
        });
        if (!hit) continue;

        return {
            provider: 'pixabay',
            identity: `pixabay:${hit.id}`,
            downloadUrl: hit.largeImageURL,
            pageUrl: hit.pageURL,
            credit: `Pixabay / ${hit.user}`,
            alt: `${page.data.name} food detail`,
            searchQuery: query,
            sourceTags: hit.tags
        };
    }

    throw new Error(`No unused Pixabay image found for ${page.key}`);
}

async function searchPexels(page, usedIdentities) {
    if (!PEXELS_KEY) {
        throw new Error('PEXELS_API_KEY is required for food image enrichment');
    }

    const compactName = compactSearchName(page.data.name);
    const queries = [
        `${compactName} cooking food`,
        `${compactName} farm harvest`,
        `${compactName} food`
    ];

    for (const query of queries) {
        const params = new URLSearchParams({
            query,
            orientation: 'landscape',
            size: 'large',
            per_page: '30'
        });
        const data = await fetchJson(
            `https://api.pexels.com/v1/search?${params}`,
            { headers: { Authorization: PEXELS_KEY } }
        );
        await delay(250);

        const photo = (data.photos ?? []).find((candidate) => {
            const identity = `pexels:${candidate.id}`;
            return (
                !usedIdentities.has(identity) &&
                candidate.src?.large2x &&
                candidate.width >= 1200 &&
                candidate.height >= 700
            );
        });
        if (!photo) continue;

        return {
            provider: 'pexels',
            identity: `pexels:${photo.id}`,
            downloadUrl: photo.src.large2x,
            pageUrl: photo.url,
            credit: `Pexels / ${photo.photographer}`,
            alt: photo.alt || `${page.data.name} food or agricultural context`,
            searchQuery: query
        };
    }

    throw new Error(`No unused Pexels image found for ${page.key}`);
}

async function downloadBuffer(url) {
    let lastError;
    for (let attempt = 1; attempt <= 3; attempt += 1) {
        try {
            const response = await fetch(url);
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const arrayBuffer = await response.arrayBuffer();
            if (arrayBuffer.byteLength < 10_000) {
                throw new Error(`download too small: ${arrayBuffer.byteLength} bytes`);
            }
            return Buffer.from(arrayBuffer);
        } catch (error) {
            lastError = error;
            await delay(attempt * 750);
        }
    }
    throw new Error(`Image download failed after retries: ${lastError?.message}`);
}

async function renderRemote(source, outputPath) {
    const buffer = await downloadBuffer(source.downloadUrl);
    mkdirSync(dirname(outputPath), { recursive: true });
    await sharp(buffer, { failOn: 'error' })
        .rotate()
        .resize(TARGET_WIDTH, TARGET_HEIGHT, {
            fit: 'cover',
            position: 'attention',
            withoutEnlargement: false
        })
        .webp({ quality: WEBP_QUALITY, effort: 5 })
        .toFile(outputPath);
}

function renderVisualAssetsBlock(page, selection) {
    const nameSlug = slugify(page.data.name);

    return [
        'visual_assets:',
        `  - id: ${page.stateSlug}-${nameSlug}-detail`,
        `    src: ${page.detailWebPath}`,
        `    alt: ${yamlQuote(`A separate food photograph illustrating ${page.data.name}`)}`,
        `    caption: ${yamlQuote(`A separate view of ${page.data.name}, selected to show the food, crop, or serving in a different setting from the hero image.`)}`,
        `    section: ${page.detailSection}`,
        '    layout: full-width-tall',
        `  - id: ${page.stateSlug}-${nameSlug}-context`,
        `    src: ${page.contextWebPath}`,
        `    alt: ${yamlQuote(selection.context.alt || `${page.data.name} food context`)}`,
        `    caption: ${yamlQuote(`A second photograph provides culinary or agricultural context for ${page.data.name}.`)}`,
        `    section: ${page.contextSection}`,
        '    layout: full-width-tall',
        ''
    ].join('\n');
}

function updateYaml(page, selection) {
    const block = renderVisualAssetsBlock(page, selection);
    if (/^visual_assets:/m.test(page.text)) {
        const pattern = /^visual_assets:\r?\n[\s\S]*?(?=^faq:)/m;
        if (!pattern.test(page.text)) {
            throw new Error(`Cannot replace visual_assets in ${relative(ROOT, page.yamlPath)}`);
        }
        writeFileSync(page.yamlPath, page.text.replace(pattern, `${block}\n`), 'utf8');
        return;
    }
    if (!/^faq:/m.test(page.text)) {
        throw new Error(`faq marker not found in ${relative(ROOT, page.yamlPath)}`);
    }
    writeFileSync(
        page.yamlPath,
        page.text.replace(/^faq:/m, `${block}\nfaq:`),
        'utf8'
    );
}

async function imageHash(filePath) {
    const { data } = await sharp(filePath)
        .rotate()
        .resize(9, 8, { fit: 'fill' })
        .greyscale()
        .raw()
        .toBuffer({ resolveWithObject: true });
    let bits = '';
    for (let row = 0; row < 8; row += 1) {
        for (let column = 0; column < 8; column += 1) {
            const offset = row * 9 + column;
            bits += data[offset] > data[offset + 1] ? '1' : '0';
        }
    }
    return bits;
}

function hammingDistance(left, right) {
    let distance = 0;
    for (let index = 0; index < left.length; index += 1) {
        if (left[index] !== right[index]) distance += 1;
    }
    return distance;
}

async function validateOutputs(pages) {
    const errors = [];
    const outputHashes = new Set();

    for (const page of pages) {
        const files = [
            join(WWWROOT, page.data.hero_image.slice(1)),
            join(WWWROOT, page.detailWebPath.slice(1)),
            join(WWWROOT, page.contextWebPath.slice(1))
        ];
        for (const filePath of files) {
            if (!existsSync(filePath)) {
                errors.push(`${page.key}: missing ${relative(ROOT, filePath)}`);
            }
        }
        if (errors.length > 0) continue;

        const hashes = await Promise.all(files.map(imageHash));
        const distances = [
            hammingDistance(hashes[0], hashes[1]),
            hammingDistance(hashes[0], hashes[2]),
            hammingDistance(hashes[1], hashes[2])
        ];
        if (Math.min(...distances) < 5) {
            errors.push(`${page.key}: visually similar images ${distances.join('/')}`);
        }

        for (const filePath of files.slice(1)) {
            const metadata = await sharp(filePath).metadata();
            if (
                metadata.format !== 'webp' ||
                metadata.width !== TARGET_WIDTH ||
                metadata.height !== TARGET_HEIGHT
            ) {
                errors.push(
                    `${page.key}: invalid ${relative(ROOT, filePath)} ${metadata.format} ${metadata.width}x${metadata.height}`
                );
            }
            const signature = `${statSync(filePath).size}:${await imageHash(filePath)}`;
            if (outputHashes.has(signature)) {
                errors.push(`${page.key}: duplicate generated output`);
            }
            outputHashes.add(signature);
        }
    }

    if (errors.length > 0) {
        throw new Error(`Food image validation failed:\n${errors.join('\n')}`);
    }
}

function loadReservedIdentities() {
    const identities = new Set();
    if (!existsSync(GEOLOGY_MANIFEST_PATH)) return identities;
    const manifest = JSON.parse(readFileSync(GEOLOGY_MANIFEST_PATH, 'utf8'));
    for (const item of Object.values(manifest.items ?? {})) {
        for (const source of [item.specimen, item.context]) {
            if (source?.identity) identities.add(source.identity);
        }
    }
    return identities;
}

async function main() {
    const { pages, counts } = collectTargetPages();
    mkdirSync(ARTIFACT_DIR, { recursive: true });

    const previousManifest = existsSync(MANIFEST_PATH)
        ? JSON.parse(readFileSync(MANIFEST_PATH, 'utf8'))
        : { version: 1, items: {} };
    const manifest = {
        version: 1,
        generatedAt: new Date().toISOString(),
        counts,
        items: {}
    };
    const usedIdentities = loadReservedIdentities();

    for (let index = 0; index < pages.length; index += 1) {
        const page = pages[index];
        const previous = previousManifest.items?.[page.key];
        const detail = previous?.detail?.identity &&
            !usedIdentities.has(previous.detail.identity)
            ? previous.detail
            : await searchPixabay(page, usedIdentities);
        usedIdentities.add(detail.identity);

        const context = previous?.context?.identity &&
            !usedIdentities.has(previous.context.identity)
            ? previous.context
            : await searchPexels(page, usedIdentities);
        usedIdentities.add(context.identity);

        const selection = { detail, context };
        manifest.items[page.key] = selection;
        await renderRemote(
            detail,
            join(WWWROOT, page.detailWebPath.slice(1))
        );
        await renderRemote(
            context,
            join(WWWROOT, page.contextWebPath.slice(1))
        );
        console.log(
            `[${String(index + 1).padStart(3, '0')}/${pages.length}] ${page.key}: ${detail.provider} + ${context.provider}`
        );
    }

    for (const page of pages) {
        updateYaml(page, manifest.items[page.key]);
    }
    writeFileSync(MANIFEST_PATH, `${JSON.stringify(manifest, null, 2)}\n`, 'utf8');
    await validateOutputs(pages);

    console.log(`pages=${pages.length}`);
    console.log(`generated_images=${pages.length * 2}`);
    console.log(`counts=${JSON.stringify(counts)}`);
    console.log(`manifest=${relative(ROOT, MANIFEST_PATH)}`);
}

await main();
