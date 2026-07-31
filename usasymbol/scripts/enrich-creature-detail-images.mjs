#!/usr/bin/env node

import {
    existsSync,
    mkdirSync,
    readFileSync,
    readdirSync,
    statSync,
    writeFileSync
} from 'fs';
import { createHash } from 'crypto';
import { basename, dirname, join, relative } from 'path';
import sharp from 'sharp';
import { parse } from 'yaml';

const ROOT = join(import.meta.dirname, '..');
const STATES_DIR = join(ROOT, 'Content', 'states');
const PARK_CONTENT_DIR = join(ROOT, 'Content', 'parks', 'national');
const WWWROOT = join(ROOT, 'wwwroot');
const ARTIFACT_DIR = join(ROOT, 'artifacts', 'creature-detail-image-sources');
const MANIFEST_PATH = join(ARTIFACT_DIR, 'manifest.json');
const RESERVED_MANIFESTS = [
    join(ROOT, 'artifacts', 'geology-image-sources', 'manifest.json'),
    join(ROOT, 'artifacts', 'food-detail-image-sources', 'manifest.json')
];
const KINDS = [
    {
        kind: 'amphibian',
        hub: 'amphibians',
        folder: 'amphibians',
        expected: 25
    },
    {
        kind: 'reptile',
        hub: 'reptiles',
        folder: 'reptiles',
        expected: 31
    }
];
const TARGET_WIDTH = 1600;
const TARGET_HEIGHT = 900;
const WEBP_QUALITY = 82;
const PIXABAY_KEY = process.env.PIXABAY_API_KEY ?? '';

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
        .replace(/[^\p{L}\p{N}\s-]/gu, ' ')
        .replace(/\s+/g, ' ')
        .trim();
}

function collectPagesByHero(kind) {
    const pages = new Map();
    for (const stateSlug of readdirSync(STATES_DIR).sort()) {
        const yamlPath = join(STATES_DIR, stateSlug, `${kind}.yaml`);
        if (!existsSync(yamlPath)) continue;
        const text = readFileSync(yamlPath, 'utf8');
        const data = parse(text);
        pages.set(data.hero_image, {
            stateSlug,
            yamlPath,
            text,
            data
        });
    }
    return pages;
}

function collectTargetPages() {
    const pages = [];
    const counts = {};

    for (const config of KINDS) {
        const pagesByHero = collectPagesByHero(config.kind);
        const hub = parse(
            readFileSync(
                join(ROOT, 'Content', 'symbols', `${config.hub}.yml`),
                'utf8'
            )
        );
        const rows = hub?.table?.rows;
        if (!Array.isArray(rows) || rows.length !== config.expected) {
            throw new Error(
                `${config.hub}.yml: expected ${config.expected} rows, found ${rows?.length ?? 0}`
            );
        }
        counts[config.hub] = rows.length;

        for (const row of rows) {
            const page = pagesByHero.get(row.symbol_image);
            if (!page) {
                throw new Error(
                    `${config.hub}.yml: no detail YAML matches ${row.symbol_image}`
                );
            }
            const nameSlug = slugify(page.data.name);
            pages.push({
                ...page,
                ...config,
                key: `${page.stateSlug}/${config.kind}`,
                detailWebPath:
                    `/images/${config.folder}/${page.stateSlug}/${page.stateSlug}-${nameSlug}-detail.webp`,
                contextWebPath:
                    `/images/${config.folder}/${page.stateSlug}/${page.stateSlug}-${nameSlug}-habitat.webp`
            });
        }
    }

    if (pages.length !== 56) {
        throw new Error(`Expected 56 creature pages, found ${pages.length}`);
    }
    return { pages, counts };
}

function collectParkImages(value, park, output) {
    if (Array.isArray(value)) {
        for (const item of value) collectParkImages(item, park, output);
        return;
    }
    if (!value || typeof value !== 'object') return;

    for (const [key, child] of Object.entries(value)) {
        if (
            typeof child === 'string' &&
            (key === 'image' || key === 'hero_image') &&
            child.startsWith('/images/parks/national/')
        ) {
            const diskPath = join(WWWROOT, child.slice(1));
            if (!existsSync(diskPath)) continue;
            const searchable = `${basename(child)} ${value.alt || ''}`.toLowerCase();
            const score = /(wildlife|animal|forest|wetland|river|lake|pond|coast|swamp|marsh|desert|grassland|prairie|habitat|water|woods|nature)/.test(searchable)
                ? 10
                : 0;
            output.push({
                provider: 'local-park',
                identity: `park:${child}`,
                diskPath,
                pageUrl: `/parks/national/${park.slug}`,
                credit: value.credit || park.credit || 'National Park Service',
                alt: value.alt || `${park.name} habitat landscape`,
                parkName: park.name,
                score
            });
        } else {
            collectParkImages(child, park, output);
        }
    }
}

function loadParkPools() {
    const pools = new Map();
    for (const fileName of readdirSync(PARK_CONTENT_DIR).filter((name) => /\.ya?ml$/i.test(name))) {
        const parkData = parse(
            readFileSync(join(PARK_CONTENT_DIR, fileName), 'utf8')
        );
        const stateText = parkData?.location?.state;
        if (typeof stateText !== 'string') continue;
        const images = [];
        collectParkImages(
            parkData,
            {
                slug: parkData.slug,
                name: parkData.name,
                credit: parkData?.media?.hero_credit
            },
            images
        );
        images.sort((left, right) => right.score - left.score);
        for (const state of stateText.split(/[,/&]/).map((item) => item.trim()).filter(Boolean)) {
            if (!pools.has(state)) pools.set(state, []);
            pools.get(state).push(...images);
        }
    }
    return pools;
}

function loadReservedIdentities() {
    const identities = new Set();
    for (const manifestPath of RESERVED_MANIFESTS) {
        if (!existsSync(manifestPath)) continue;
        const manifest = JSON.parse(readFileSync(manifestPath, 'utf8'));
        for (const item of Object.values(manifest.items ?? {})) {
            for (const source of Object.values(item)) {
                if (source?.identity) identities.add(source.identity);
            }
        }
    }
    return identities;
}

async function fetchJson(url) {
    const response = await fetch(url);
    if (!response.ok) {
        throw new Error(`Pixabay search failed with HTTP ${response.status}`);
    }
    return response.json();
}

async function searchPixabay(page, usedIdentities) {
    if (!PIXABAY_KEY) {
        throw new Error('PIXABAY_API_KEY is required for creature image enrichment');
    }

    const name = compactSearchName(page.data.name);
    const binomial = compactSearchName(page.data.binomial_name || '');
    const generic = page.kind === 'amphibian'
        ? 'frog salamander amphibian'
        : 'turtle snake lizard reptile';
    const queries = [
        `${name} ${page.kind}`,
        binomial,
        name.split(' ').slice(-2).join(' '),
        generic
    ].filter(Boolean);

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
            alt: `${page.data.name} wildlife photograph`,
            searchQuery: query,
            sourceTags: hit.tags
        };
    }

    throw new Error(`No unused Pixabay image found for ${page.key}`);
}

function chooseParkContext(page, pools, usedIdentities) {
    return (pools.get(page.data.state) ?? []).find(
        (candidate) =>
            candidate.score > 0 &&
            !usedIdentities.has(candidate.identity)
    );
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

async function renderSource(source, outputPath) {
    const input = source.diskPath
        ? source.diskPath
        : await downloadBuffer(source.downloadUrl);
    mkdirSync(dirname(outputPath), { recursive: true });
    await sharp(input, { failOn: 'error' })
        .rotate()
        .resize(TARGET_WIDTH, TARGET_HEIGHT, {
            fit: 'cover',
            position: 'attention',
            withoutEnlargement: false
        })
        .webp({ quality: WEBP_QUALITY, effort: 5 })
        .toFile(outputPath);
}

function exactFileHash(filePath) {
    return createHash('sha256')
        .update(readFileSync(filePath))
        .digest('hex');
}

async function deduplicateHeroes(pages, manifest, usedIdentities) {
    const usedHashes = new Set();
    for (const page of pages) {
        for (const webPath of [page.detailWebPath, page.contextWebPath]) {
            usedHashes.add(exactFileHash(join(WWWROOT, webPath.slice(1))));
        }
    }

    for (const page of pages) {
        const heroPath = join(WWWROOT, page.data.hero_image.slice(1));
        let hero = manifest.items[page.key].hero;
        let hash = exactFileHash(heroPath);
        let attempt = 0;

        while (usedHashes.has(hash)) {
            usedIdentities.add(hero.identity);
            hero = await searchPixabay(page, usedIdentities);
            usedIdentities.add(hero.identity);
            await renderSource(hero, heroPath);
            hash = exactFileHash(heroPath);
            attempt += 1;
            if (attempt >= 10) {
                throw new Error(
                    `${page.key}: could not find a globally unique hero after ${attempt} attempts`
                );
            }
        }

        manifest.items[page.key].hero = hero;
        usedHashes.add(hash);
    }
}

function renderVisualAssetsBlock(page, selection) {
    const nameSlug = slugify(page.data.name);
    const habitatCaption = selection.context.provider === 'local-park'
        ? `${selection.context.parkName} provides a separate view of habitat in ${page.data.state}.`
        : `A separate habitat photograph provides range context for ${page.data.name}.`;

    return [
        'visual_assets:',
        `  - id: ${page.stateSlug}-${nameSlug}-detail`,
        `    src: ${page.detailWebPath}`,
        `    alt: ${yamlQuote(`A separate wildlife photograph illustrating ${page.data.name}`)}`,
        `    caption: ${yamlQuote(`A separate view of ${page.data.name}, distinct from the hero photograph.`)}`,
        '    section: about',
        '    layout: full-width-tall',
        `  - id: ${page.stateSlug}-${nameSlug}-habitat`,
        `    src: ${page.contextWebPath}`,
        `    alt: ${yamlQuote(selection.context.alt || `${page.data.state} wildlife habitat`)}`,
        `    caption: ${yamlQuote(habitatCaption)}`,
        '    section: location',
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
    for (const page of pages) {
        const files = [
            join(WWWROOT, page.data.hero_image.slice(1)),
            join(WWWROOT, page.detailWebPath.slice(1)),
            join(WWWROOT, page.contextWebPath.slice(1))
        ];
        if (files.some((file) => !existsSync(file))) {
            errors.push(`${page.key}: missing image`);
            continue;
        }
        const hashes = await Promise.all(files.map(imageHash));
        const distances = [
            hammingDistance(hashes[0], hashes[1]),
            hammingDistance(hashes[0], hashes[2]),
            hammingDistance(hashes[1], hashes[2])
        ];
        if (Math.min(...distances) < 5) {
            errors.push(`${page.key}: visually similar images ${distances.join('/')}`);
        }
        for (const file of files.slice(1)) {
            const metadata = await sharp(file).metadata();
            if (
                metadata.format !== 'webp' ||
                metadata.width !== TARGET_WIDTH ||
                metadata.height !== TARGET_HEIGHT
            ) {
                errors.push(`${page.key}: invalid output ${relative(ROOT, file)}`);
            }
        }
    }
    if (errors.length > 0) {
        throw new Error(`Creature image validation failed:\n${errors.join('\n')}`);
    }
}

async function main() {
    const { pages, counts } = collectTargetPages();
    mkdirSync(ARTIFACT_DIR, { recursive: true });
    const previousManifest = existsSync(MANIFEST_PATH)
        ? JSON.parse(readFileSync(MANIFEST_PATH, 'utf8'))
        : { items: {} };
    const manifest = {
        version: 1,
        generatedAt: new Date().toISOString(),
        counts,
        items: {}
    };
    const pools = loadParkPools();
    const usedIdentities = loadReservedIdentities();

    for (let index = 0; index < pages.length; index += 1) {
        const page = pages[index];
        const previous = previousManifest.items?.[page.key];
        const detail = previous?.detail?.identity &&
            !usedIdentities.has(previous.detail.identity)
            ? previous.detail
            : await searchPixabay(page, usedIdentities);
        usedIdentities.add(detail.identity);

        const reusableContext = previous?.context?.identity &&
            !usedIdentities.has(previous.context.identity)
            ? previous.context
            : null;
        const context = reusableContext
            ?? chooseParkContext(page, pools, usedIdentities)
            ?? await searchPixabay(page, usedIdentities);
        usedIdentities.add(context.identity);

        const hero = previous?.hero?.identity &&
            !usedIdentities.has(previous.hero.identity)
            ? previous.hero
            : await searchPixabay(page, usedIdentities);
        usedIdentities.add(hero.identity);

        const selection = { hero, detail, context };
        manifest.items[page.key] = selection;
        const detailOutput = join(WWWROOT, page.detailWebPath.slice(1));
        const contextOutput = join(WWWROOT, page.contextWebPath.slice(1));
        if (!existsSync(detailOutput)) {
            await renderSource(detail, detailOutput);
        }
        if (!existsSync(contextOutput)) {
            await renderSource(context, contextOutput);
        }
        await renderSource(
            hero,
            join(WWWROOT, page.data.hero_image.slice(1))
        );
        console.log(
            `[${String(index + 1).padStart(2, '0')}/${pages.length}] ${page.key}: hero ${hero.provider}, detail ${detail.provider}, habitat ${context.provider}`
        );
    }

    for (const page of pages) {
        updateYaml(page, manifest.items[page.key]);
    }
    await deduplicateHeroes(pages, manifest, usedIdentities);
    writeFileSync(MANIFEST_PATH, `${JSON.stringify(manifest, null, 2)}\n`, 'utf8');
    await validateOutputs(pages);
    console.log(`pages=${pages.length}`);
    console.log(`generated_images=${pages.length * 3}`);
    console.log(`counts=${JSON.stringify(counts)}`);
    console.log(`manifest=${relative(ROOT, MANIFEST_PATH)}`);
}

await main();
