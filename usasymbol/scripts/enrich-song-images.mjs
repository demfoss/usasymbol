#!/usr/bin/env node

import {
    existsSync,
    mkdirSync,
    readFileSync,
    readdirSync,
    writeFileSync
} from 'fs';
import { createHash } from 'crypto';
import { basename, dirname, join, relative } from 'path';
import sharp from 'sharp';
import { parse } from 'yaml';

const ROOT = join(import.meta.dirname, '..');
const CONTENT_DIR = join(ROOT, 'Content', 'states');
const HUB_PATH = join(ROOT, 'Content', 'symbols', 'songs.yml');
const OUTPUT_DIR = join(ROOT, 'wwwroot', 'images', 'songs');
const LIVING_DIR = join(ROOT, 'wwwroot', 'images', 'state-living');
const PARKS_DIR = join(ROOT, 'wwwroot', 'images', 'parks', 'national');
const MANIFEST_PATH = join(ROOT, 'artifacts', 'song-image-sources', 'manifest.json');
const WIDTH = 1600;
const HEIGHT = 900;

const PARK_BY_STATE = {
    alaska: 'denali-national-park-preserve',
    arizona: 'grand-canyon-national-park',
    arkansas: 'hot-springs-national-park',
    california: 'yosemite-national-park',
    colorado: 'rocky-mountain-national-park',
    florida: 'everglades-national-park',
    hawaii: 'hawai-i-volcanoes-national-park',
    idaho: 'yellowstone-national-park',
    indiana: 'indiana-dunes-national-park',
    kentucky: 'mammoth-cave-national-park',
    maine: 'acadia-national-park',
    michigan: 'isle-royale-national-park',
    minnesota: 'voyageurs-national-park',
    missouri: 'gateway-arch-national-park',
    montana: 'glacier-national-park',
    nevada: 'great-basin-national-park',
    'new-mexico': 'white-sands-national-park',
    'north-carolina': 'great-smoky-mountains-national-park',
    'north-dakota': 'theodore-roosevelt-national-park',
    ohio: 'cuyahoga-valley-national-park',
    oregon: 'crater-lake-national-park',
    'south-carolina': 'congaree-national-park',
    'south-dakota': 'badlands-national-park',
    texas: 'big-bend-national-park',
    utah: 'zion-national-park',
    virginia: 'shenandoah-national-park',
    washington: 'mount-rainier-national-park',
    'west-virginia': 'new-river-gorge-national-park-preserve',
    wyoming: 'grand-teton-national-park'
};

function yamlQuote(value) {
    return JSON.stringify(String(value).replace(/\s+/g, ' ').trim());
}

function slugify(value) {
    return String(value)
        .toLowerCase()
        .normalize('NFKD')
        .replace(/[\u0300-\u036f]/g, '')
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/^-|-$/g, '');
}

function imageFiles(directory) {
    if (!existsSync(directory)) return [];
    return readdirSync(directory)
        .filter((name) => /\.(?:jpe?g|png|webp)$/i.test(name))
        .sort((a, b) => a.localeCompare(b, undefined, { numeric: true }))
        .map((name) => join(directory, name));
}

function sourcePool(stateSlug) {
    const living = imageFiles(join(LIVING_DIR, stateSlug));
    if (living.length >= 3) {
        return { kind: 'state-living', label: stateSlug, files: living };
    }
    const parkSlug = PARK_BY_STATE[stateSlug];
    if (!parkSlug) throw new Error(`${stateSlug}: no local image source mapping`);
    const park = imageFiles(join(PARKS_DIR, parkSlug));
    if (park.length < 3) throw new Error(`${stateSlug}: ${parkSlug} has fewer than three images`);
    return { kind: 'national-park', label: parkSlug, files: park };
}

function collectPages() {
    const pages = [];
    for (const stateSlug of readdirSync(CONTENT_DIR).sort()) {
        const songDir = join(CONTENT_DIR, stateSlug, 'song');
        if (!existsSync(songDir)) continue;
        for (const fileName of readdirSync(songDir).filter((name) => name.endsWith('.yaml')).sort()) {
            const yamlPath = join(songDir, fileName);
            const text = readFileSync(yamlPath, 'utf8');
            const data = parse(text);
            if (!data || data.type !== 'State Song') continue;
            if (!Array.isArray(data.visual_assets) || data.visual_assets.length !== 2) {
                throw new Error(`${relative(ROOT, yamlPath)} must contain exactly two visual_assets`);
            }
            pages.push({
                stateSlug,
                songSlug: fileName.replace(/\.yaml$/i, ''),
                yamlPath,
                text,
                data,
                pool: sourcePool(stateSlug)
            });
        }
    }
    if (pages.length !== 48) throw new Error(`Expected 48 song pages, found ${pages.length}`);
    return pages;
}

async function render(sourcePath, role) {
    const position = role === 'hero' ? 'attention' : role === 'detail' ? 'west' : 'east';
    return sharp(sourcePath, { failOn: 'error' })
        .rotate()
        .resize(WIDTH, HEIGHT, { fit: 'cover', position, withoutEnlargement: false })
        .webp({ quality: 82, effort: 5 })
        .toBuffer();
}

function sourceDescription(page) {
    if (page.pool.kind === 'national-park') {
        const parkName = page.pool.label
            .split('-')
            .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
            .join(' ')
            .replace('Hawai I', "Hawai'i");
        return `${parkName} scenery in or associated with ${page.data.state}`;
    }
    return `${page.data.state} scenery`;
}

function updatePage(page, paths) {
    const songName = page.data.name;
    const state = page.data.state;
    const sourceLabel = sourceDescription(page);
    const first = page.data.visual_assets[0];
    const second = page.data.visual_assets[1];
    const firstSection = first.section || 'what-is';
    const secondSection = second.section || 'why-chose';
    const visualBlock = [
        'visual_assets:',
        `  - id: ${page.stateSlug}-${page.songSlug}-landscape`,
        `    src: ${paths.detail}`,
        `    alt: ${yamlQuote(`${sourceLabel} connected with the state song ${songName}`)}`,
        `    caption: ${yamlQuote(`${state} scenery reflects the sense of place celebrated by ${songName}.`)}`,
        `    section: ${firstSection}`,
        '    layout: full-width-tall',
        `  - id: ${page.stateSlug}-${page.songSlug}-state-view`,
        `    src: ${paths.context}`,
        `    alt: ${yamlQuote(`A second view of ${state} connected with the official state song ${songName}`)}`,
        `    caption: ${yamlQuote(`${state}'s landscape provides the setting and identity behind its official state song.`)}`,
        `    section: ${secondSection}`,
        '    layout: full-width-tall',
        ''
    ].join('\n');

    let text = page.text
        .replace(/^hero_image:.*$/m, `hero_image: ${paths.hero}`)
        .replace(/^hero_image_alt:.*$/m, `hero_image_alt: ${yamlQuote(`${sourceLabel} associated with ${songName}`)}`)
        .replace(
            /^hero_image_caption:.*$/m,
            `hero_image_caption: ${yamlQuote(`${songName} expresses the character and identity of ${state}.`)}`
        )
        .replace(/^date_modified:.*$/m, 'date_modified: "2026-07-31"');
    const visualMatch = text.match(/^visual_assets:\r?\n[\s\S]*?(?=^faq:)/m);
    if (!visualMatch) throw new Error(`${relative(ROOT, page.yamlPath)}: visual_assets block not found`);
    text = text.replace(visualMatch[0], `${visualBlock}\n`);
    writeFileSync(page.yamlPath, text, 'utf8');
}

function updateHub(pages) {
    let text = readFileSync(HUB_PATH, 'utf8')
        .replace(/^date_modified:.*$/m, 'date_modified: "2026-07-31"');
    for (const page of pages) {
        const heroPath = `/images/songs/${page.stateSlug}/${page.songSlug}-hero.webp`;
        const rowPattern = new RegExp(
            `(    - state: [^\\r\\n]+\\r?\\n(?:(?!    - state:)[\\s\\S])*?      state_slug: "${page.stateSlug}"(?:(?!    - state:)[\\s\\S])*?)(?=    - state:|$)`
        );
        const match = text.match(rowPattern);
        if (!match) throw new Error(`Hub row not found for ${page.stateSlug}`);
        let block = match[1];
        if (/^      symbol_image:/m.test(block)) {
            block = block.replace(/^      symbol_image:.*$/m, `      symbol_image: "${heroPath}"`);
        } else {
            block = block.replace(
                /^(      symbol_url:.*)$/m,
                `$1\n      symbol_image: "${heroPath}"`
            );
        }
        text = text.replace(match[1], block);
    }
    writeFileSync(HUB_PATH, text, 'utf8');
}

async function main() {
    const pages = collectPages();
    const manifest = [];
    for (const page of pages) {
        const output = join(OUTPUT_DIR, page.stateSlug);
        mkdirSync(output, { recursive: true });
        const sources = page.pool.kind === 'national-park'
            ? [
                page.pool.files[0],
                page.pool.files[Math.floor(page.pool.files.length / 2)],
                page.pool.files[page.pool.files.length - 1]
            ]
            : page.pool.files.slice(0, 3);
        const paths = {
            hero: `/images/songs/${page.stateSlug}/${page.songSlug}-hero.webp`,
            detail: `/images/songs/${page.stateSlug}/${page.songSlug}-landscape.webp`,
            context: `/images/songs/${page.stateSlug}/${page.songSlug}-state-view.webp`
        };
        for (const [index, role] of ['hero', 'detail', 'context'].entries()) {
            const buffer = await render(sources[index], role);
            const target = join(ROOT, 'wwwroot', paths[role].replace(/^\//, ''));
            mkdirSync(dirname(target), { recursive: true });
            writeFileSync(target, buffer);
            manifest.push({
                state: page.data.state,
                song: page.data.name,
                role,
                source: relative(ROOT, sources[index]).replaceAll('\\', '/'),
                source_sha256: createHash('sha256').update(readFileSync(sources[index])).digest('hex'),
                output: paths[role],
                output_sha256: createHash('sha256').update(buffer).digest('hex'),
                width: WIDTH,
                height: HEIGHT
            });
        }
        updatePage(page, paths);
        console.log(`${page.stateSlug}: ${basename(page.yamlPath)}`);
    }
    updateHub(pages);
    mkdirSync(dirname(MANIFEST_PATH), { recursive: true });
    writeFileSync(MANIFEST_PATH, `${JSON.stringify(manifest, null, 2)}\n`, 'utf8');
    console.log(`complete: ${pages.length} pages, ${manifest.length} WebP images`);
}

main().catch((error) => {
    console.error(error.stack || error.message);
    process.exitCode = 1;
});
