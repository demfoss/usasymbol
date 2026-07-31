#!/usr/bin/env node

import {
    copyFileSync,
    existsSync,
    mkdirSync,
    readFileSync,
    readdirSync,
    renameSync,
    statSync,
    unlinkSync,
    writeFileSync
} from 'fs';
import { basename, dirname, extname, join, relative } from 'path';
import sharp from 'sharp';
import { parse } from 'yaml';

const ROOT = join(import.meta.dirname, '..');
const STATES_DIR = join(ROOT, 'Content', 'states');
const PARK_CONTENT_DIR = join(ROOT, 'Content', 'parks', 'national');
const WWWROOT = join(ROOT, 'wwwroot');
const ARTIFACT_DIR = join(ROOT, 'artifacts', 'geology-image-sources');
const PREVIOUS_ASSET_DIR = join(ARTIFACT_DIR, 'previous-assets');
const MANIFEST_PATH = join(ARTIFACT_DIR, 'manifest.json');
const KINDS = ['mineral', 'rock', 'gemstone'];
const FOLDERS = {
    mineral: 'minerals',
    rock: 'rocks',
    gemstone: 'gemstones'
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

function collectPages() {
    const pages = [];

    for (const stateSlug of readdirSync(STATES_DIR).sort()) {
        const stateDir = join(STATES_DIR, stateSlug);
        if (!statSync(stateDir).isDirectory()) continue;

        for (const kind of KINDS) {
            const yamlPath = join(stateDir, `${kind}.yaml`);
            if (!existsSync(yamlPath)) continue;

            const text = readFileSync(yamlPath, 'utf8');
            const data = parse(text);
            const sections = Array.isArray(data.sections) ? data.sections : [];
            const contextSection = sections.some((section) => section.id === 'location')
                ? 'location'
                : sections.some((section) => section.id === 'why-chose')
                    ? 'why-chose'
                    : 'what-is';
            const materialSlug = slugify(data.name);
            const folder = FOLDERS[kind];
            const baseWebPath = `/images/${folder}/${stateSlug}`;

            pages.push({
                key: `${stateSlug}/${kind}`,
                kind,
                folder,
                stateSlug,
                state: data.state,
                name: data.name,
                yamlPath,
                text,
                data,
                contextSection,
                oldAssets: Array.isArray(data.visual_assets) ? data.visual_assets : [],
                specimenWebPath: `${baseWebPath}/${stateSlug}-${materialSlug}-detail.webp`,
                contextWebPath: `${baseWebPath}/${stateSlug}-${materialSlug}-context.webp`
            });
        }
    }

    return pages;
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
            if (existsSync(diskPath)) {
                const fileName = basename(child).toLowerCase();
                const searchable = `${fileName} ${value.alt || ''}`.toLowerCase();
                const geologyScore = /(geology|rock|cave|canyon|mountain|cliff|dune|badland|volcano|lava|river|lake|coast|glacier|peak|valley|desert|formation|landscape)/.test(searchable)
                    ? 10
                    : 0;
                output.push({
                    provider: 'local-park',
                    identity: `park:${child}`,
                    diskPath,
                    webPath: child,
                    pageUrl: `/parks/national/${park.slug}`,
                    credit: value.credit || park.credit || 'National Park Service',
                    alt: value.alt || `${park.name} landscape`,
                    parkName: park.name,
                    score: geologyScore
                });
            }
        } else {
            collectParkImages(child, park, output);
        }
    }
}

function loadParkPools() {
    const pools = new Map();
    if (!existsSync(PARK_CONTENT_DIR)) return pools;

    for (const fileName of readdirSync(PARK_CONTENT_DIR).filter((name) => /\.ya?ml$/i.test(name))) {
        const parkData = parse(readFileSync(join(PARK_CONTENT_DIR, fileName), 'utf8'));
        const stateText = parkData?.location?.state;
        if (typeof stateText !== 'string' || !parkData.media) continue;

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

        const states = stateText
            .split(/[,/&]/)
            .map((state) => state.trim())
            .filter(Boolean);
        for (const state of states) {
            if (!pools.has(state)) pools.set(state, []);
            pools.get(state).push(...images);
        }
    }

    return pools;
}

async function fetchJson(url, options = {}) {
    const response = await fetch(url, options);
    if (!response.ok) {
        throw new Error(`Image search request failed with HTTP ${response.status}`);
    }
    return response.json();
}

function compactSearchName(name) {
    return name
        .replace(/\([^)]*\)/g, ' ')
        .replace(/\b(and|or)\b.*$/i, ' ')
        .replace(/[^\p{L}\p{N}\s-]/gu, ' ')
        .replace(/\s+/g, ' ')
        .trim();
}

async function searchPixabay(page, usedIdentities) {
    if (!PIXABAY_KEY) {
        throw new Error('PIXABAY_API_KEY is required for geology image enrichment');
    }

    const compactName = compactSearchName(page.name);
    const kindTerm = page.kind === 'gemstone'
        ? 'gemstone crystal'
        : page.kind === 'mineral'
            ? 'mineral specimen'
            : 'rock geology';
    const queries = [
        `${compactName} ${kindTerm}`,
        `${compactName} stone`,
        kindTerm
    ];

    for (const query of queries) {
        const params = new URLSearchParams({
            key: PIXABAY_KEY,
            q: query.slice(0, 100),
            image_type: 'photo',
            orientation: 'horizontal',
            safesearch: 'true',
            order: 'popular',
            per_page: '20'
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
            alt: `${page.name} material detail`,
            searchQuery: query,
            sourceTags: hit.tags
        };
    }

    throw new Error(`No unused Pixabay image found for ${page.key}: ${page.name}`);
}

async function searchPexelsContext(page, usedIdentities) {
    if (!PEXELS_KEY) {
        throw new Error('PEXELS_API_KEY is required when no local park context image is available');
    }

    const queries = [
        `${page.state} geology landscape rocks`,
        `${page.state} mountain desert landscape`,
        'American geology landscape'
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
            alt: photo.alt || `${page.state} geologic landscape`,
            searchQuery: query
        };
    }

    throw new Error(`No unused Pexels context image found for ${page.key}`);
}

function chooseParkContext(page, parkPools, usedIdentities) {
    const candidates = parkPools.get(page.state) ?? [];
    return candidates.find(
        (candidate) => candidate.score > 0 && !usedIdentities.has(candidate.identity)
    );
}

async function downloadBuffer(url) {
    let lastError;
    for (let attempt = 1; attempt <= 3; attempt += 1) {
        try {
            const response = await fetch(url);
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }
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

function archiveExistingAssets(pages) {
    for (const page of pages) {
        for (const asset of page.oldAssets) {
            if (typeof asset.src !== 'string' || !asset.src.startsWith('/images/')) continue;
            if (asset.src === page.data.hero_image) continue;
            const source = join(WWWROOT, asset.src.slice(1));
            if (!existsSync(source)) continue;
            const archive = join(PREVIOUS_ASSET_DIR, asset.src.slice(1));
            if (!existsSync(archive)) {
                mkdirSync(dirname(archive), { recursive: true });
                copyFileSync(source, archive);
            }
        }
    }
}

function renderVisualAssetsBlock(page, selection) {
    const specimenId = `${page.stateSlug}-${slugify(page.name)}-detail`;
    const contextId = `${page.stateSlug}-${slugify(page.name)}-context`;
    const contextCaption = selection.context.provider === 'local-park'
        ? `${selection.context.parkName} provides a second view of ${page.state}'s broader geologic landscape.`
        : `${page.state} landscape provides geographic context for ${page.name}.`;
    const contextAlt = selection.context.alt || `${page.state} geologic landscape`;

    return [
        'visual_assets:',
        `  - id: ${specimenId}`,
        `    src: ${page.specimenWebPath}`,
        `    alt: ${yamlQuote(`A separate material view illustrating ${page.name}`)}`,
        `    caption: ${yamlQuote(`A separate close view of ${page.name}, selected to show its color, texture, or finished appearance.`)}`,
        '    section: what-is',
        '    layout: full-width-tall',
        `  - id: ${contextId}`,
        `    src: ${page.contextWebPath}`,
        `    alt: ${yamlQuote(contextAlt)}`,
        `    caption: ${yamlQuote(contextCaption)}`,
        `    section: ${page.contextSection}`,
        '    layout: full-width-tall',
        ''
    ].join('\n');
}

function updateYaml(page, selection) {
    const block = renderVisualAssetsBlock(page, selection);
    const pattern = /^visual_assets:\r?\n[\s\S]*?(?=^faq:)/m;
    if (!pattern.test(page.text)) {
        throw new Error(`visual_assets block or faq marker not found in ${relative(ROOT, page.yamlPath)}`);
    }
    const updated = page.text.replace(pattern, `${block}\n`);
    writeFileSync(page.yamlPath, updated, 'utf8');
}

function removeObsoleteAssets(pages) {
    const retained = new Set(
        pages.flatMap((page) => [page.specimenWebPath, page.contextWebPath, page.data.hero_image])
    );

    for (const page of pages) {
        for (const asset of page.oldAssets) {
            if (
                typeof asset.src !== 'string' ||
                !asset.src.startsWith(`/images/${page.folder}/`) ||
                retained.has(asset.src)
            ) {
                continue;
            }
            const diskPath = join(WWWROOT, asset.src.slice(1));
            if (existsSync(diskPath)) unlinkSync(diskPath);
        }
    }
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
    const identities = new Set();
    const outputHashes = new Set();

    for (const page of pages) {
        const heroPath = join(WWWROOT, page.data.hero_image.slice(1));
        const specimenPath = join(WWWROOT, page.specimenWebPath.slice(1));
        const contextPath = join(WWWROOT, page.contextWebPath.slice(1));
        for (const filePath of [heroPath, specimenPath, contextPath]) {
            if (!existsSync(filePath)) {
                errors.push(`${page.key}: missing ${relative(ROOT, filePath)}`);
            }
        }
        if (errors.length > 0) continue;

        const [heroHash, specimenHash, contextHash] = await Promise.all([
            imageHash(heroPath),
            imageHash(specimenPath),
            imageHash(contextPath)
        ]);
        const distances = {
            heroSpecimen: hammingDistance(heroHash, specimenHash),
            heroContext: hammingDistance(heroHash, contextHash),
            specimenContext: hammingDistance(specimenHash, contextHash)
        };
        if (Math.min(...Object.values(distances)) < 5) {
            errors.push(`${page.key}: visually similar images ${JSON.stringify(distances)}`);
        }

        for (const filePath of [specimenPath, contextPath]) {
            const metadata = await sharp(filePath).metadata();
            if (
                metadata.format !== 'webp' ||
                metadata.width !== TARGET_WIDTH ||
                metadata.height !== TARGET_HEIGHT
            ) {
                errors.push(
                    `${page.key}: invalid output ${relative(ROOT, filePath)} ${metadata.format} ${metadata.width}x${metadata.height}`
                );
            }
            const hash = `${statSync(filePath).size}:${await imageHash(filePath)}`;
            if (outputHashes.has(hash)) {
                errors.push(`${page.key}: duplicate generated output ${relative(ROOT, filePath)}`);
            }
            outputHashes.add(hash);
        }
    }

    if (errors.length > 0) {
        throw new Error(`Geology image validation failed:\n${errors.join('\n')}`);
    }

    return { generatedImages: outputHashes.size, uniqueSources: identities.size };
}

async function main() {
    const pages = collectPages();
    if (pages.length !== 96) {
        throw new Error(`Expected 96 geology pages, found ${pages.length}`);
    }

    mkdirSync(ARTIFACT_DIR, { recursive: true });
    archiveExistingAssets(pages);

    const previousManifest = existsSync(MANIFEST_PATH)
        ? JSON.parse(readFileSync(MANIFEST_PATH, 'utf8'))
        : { version: 1, items: {} };
    const manifest = { version: 1, generatedAt: new Date().toISOString(), items: {} };
    const parkPools = loadParkPools();
    const usedIdentities = new Set();
    let remoteDownloads = 0;
    let parkCopies = 0;

    for (let index = 0; index < pages.length; index += 1) {
        const page = pages[index];
        const previousSelection = previousManifest.items?.[page.key];
        const specimen = previousSelection?.specimen?.identity &&
            !usedIdentities.has(previousSelection.specimen.identity)
            ? previousSelection.specimen
            : await searchPixabay(page, usedIdentities);
        usedIdentities.add(specimen.identity);

        const reusableContext = previousSelection?.context?.identity &&
            !usedIdentities.has(previousSelection.context.identity) &&
            (
                previousSelection.context.provider !== 'local-park' ||
                previousSelection.context.score > 0
            )
            ? previousSelection.context
            : null;
        const parkContext = reusableContext
            ? null
            : chooseParkContext(page, parkPools, usedIdentities);
        const context = reusableContext
            ?? parkContext
            ?? await searchPexelsContext(page, usedIdentities);
        usedIdentities.add(context.identity);
        const selection = { specimen, context };

        manifest.items[page.key] = selection;
        const specimenOutput = join(WWWROOT, page.specimenWebPath.slice(1));
        const contextOutput = join(WWWROOT, page.contextWebPath.slice(1));
        await renderSource(selection.specimen, specimenOutput);
        await renderSource(selection.context, contextOutput);
        remoteDownloads += selection.specimen.diskPath ? 0 : 1;
        remoteDownloads += selection.context.diskPath ? 0 : 1;
        parkCopies += selection.specimen.diskPath ? 1 : 0;
        parkCopies += selection.context.diskPath ? 1 : 0;

        console.log(
            `[${String(index + 1).padStart(2, '0')}/${pages.length}] ${page.key}: ${selection.specimen.provider} + ${selection.context.provider}`
        );
    }

    for (const page of pages) {
        updateYaml(page, manifest.items[page.key]);
    }
    removeObsoleteAssets(pages);
    writeFileSync(MANIFEST_PATH, `${JSON.stringify(manifest, null, 2)}\n`, 'utf8');

    const validation = await validateOutputs(pages);
    console.log(`pages=${pages.length}`);
    console.log(`generated_images=${validation.generatedImages}`);
    console.log(`unique_source_identities=${usedIdentities.size}`);
    console.log(`remote_downloads=${remoteDownloads}`);
    console.log(`park_copies=${parkCopies}`);
    console.log(`manifest=${relative(ROOT, MANIFEST_PATH)}`);
}

await main();
