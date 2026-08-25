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
const HUB_PATH = join(ROOT, 'Content', 'symbols', 'dances.yml');
const WWWROOT = join(ROOT, 'wwwroot');
const SOURCE_DIRS = [
    join(WWWROOT, 'images', 'dance'),
    join(ROOT, 'artifacts', 'dance-image-sources', 'originals')
];
const MANIFEST_PATH = join(ROOT, 'artifacts', 'dance-image-sources', 'manifest.json');
const WIDTH = 1600;
const HEIGHT = 900;

const VIDEO_BY_DANCE = {
    'Square Dance': {
        tutorial: ['3xhThzLEjrI', 'Beautiful Square Dance Demonstration'],
        performance: ['Ebhm_qiAT38', '2017 Texas State Square Dance Festival']
    },
    'West Coast Swing': {
        tutorial: ['zsle7AwtEYk', 'Learn to Dance West Coast Swing in 5 Minutes'],
        performance: ['FIawVClaMgI', 'West Coast Swing Champions Jack and Jill Performance']
    },
    'Hand Dancing': {
        tutorial: ['NDhjm7kbEak', 'Hand Dance for Beginners, Part 1'],
        performance: ['bdAhvq4_TY8', 'An Evening of DC Hand Dancing']
    },
    Hula: {
        tutorial: ['1qO10aSsWSU', 'How to Hula Dance'],
        performance: ['gA67SROwp7A', 'Hawaiian Hula Dance Performance']
    },
    Clogging: {
        tutorial: ['tLZzIrFvnYQ', 'Clog Dancing Tutorial by Hannah James'],
        performance: ['zor8efwULf8', 'Clogging Nationals First-Place Performance']
    },
    Polka: {
        tutorial: ['fYm-N2qiV_Q', 'American-Style Polka Dancing Lesson'],
        performance: ['U0zIw9RKAl4', 'Romany Polka Performance']
    },
    'The Shag': {
        tutorial: ['EqwnLuaFo4o', 'Carolina Shag Basic Step Lesson'],
        performance: ['8l5pczCZw04', 'Carolina Shag Grand Nationals Performance']
    }
};

const THIRD_SOURCE_BY_DANCE = {
    Hula: ['Hula3.jpg', 'gA67SROwp7A'],
    'Hand Dancing': ['Hand dancing3.jpg', 'bdAhvq4_TY8'],
    'The Shag': ['Shag3.jpg', '8l5pczCZw04'],
    'West Coast Swing': ['West Coast Swing3.jpg', 'FIawVClaMgI']
};

function yamlQuote(value) {
    return JSON.stringify(String(value).replace(/\s+/g, ' ').trim());
}

function webpPath(value) {
    return value.replace(/\.(?:jpe?g|jfif|png)$/i, '.webp');
}

function danceForSource(fileName) {
    const name = fileName.toLowerCase();
    if (name.startsWith('square')) return 'Square Dance';
    if (name.startsWith('hula')) return 'Hula';
    if (name.startsWith('clogging')) return 'Clogging';
    if (name.startsWith('polka')) return 'Polka';
    if (name.startsWith('shag')) return 'The Shag';
    if (name.startsWith('west coast swing')) return 'West Coast Swing';
    if (name.startsWith('hand dancing')) return 'Hand Dancing';
    return null;
}

function collectSources() {
    const sourceDir = SOURCE_DIRS.find((directory) => existsSync(directory));
    if (!sourceDir) throw new Error('Dance source directory not found');
    const pools = new Map();
    for (const fileName of readdirSync(sourceDir).sort((a, b) => a.localeCompare(b, undefined, { numeric: true }))) {
        if (!/\.(?:jpe?g|jfif|png|webp)$/i.test(fileName)) continue;
        const dance = danceForSource(fileName);
        if (!dance) continue;
        if (!pools.has(dance)) pools.set(dance, []);
        pools.get(dance).push(join(sourceDir, fileName));
    }
    const expected = {
        'Square Dance': 5,
        Hula: 3,
        Clogging: 3,
        Polka: 4,
        'The Shag': 3,
        'West Coast Swing': 3,
        'Hand Dancing': 3
    };
    for (const [dance, minimum] of Object.entries(expected)) {
        const found = pools.get(dance)?.length ?? 0;
        if (found < minimum) throw new Error(`${dance}: expected ${minimum} sources, found ${found}`);
    }
    return { pools, sourceDir };
}

async function ensureThirdSources() {
    const sourceDir = SOURCE_DIRS.find((directory) => existsSync(directory));
    if (!sourceDir) throw new Error('Dance source directory not found');
    for (const [dance, [fileName, videoId]] of Object.entries(THIRD_SOURCE_BY_DANCE)) {
        const existing = readdirSync(sourceDir)
            .filter((name) => danceForSource(name) === dance);
        if (existing.length >= 3) continue;
        const response = await fetch(
            `https://img.youtube.com/vi/${videoId}/maxresdefault.jpg`
        );
        if (!response.ok) {
            throw new Error(`${dance}: YouTube frame failed with HTTP ${response.status}`);
        }
        const buffer = Buffer.from(await response.arrayBuffer());
        const metadata = await sharp(buffer).metadata();
        if (metadata.width < 1000 || metadata.height < 600) {
            throw new Error(`${dance}: YouTube frame is too small`);
        }
        writeFileSync(join(sourceDir, fileName), buffer);
        console.log(`added third source: ${dance}`);
    }
}

function collectPages() {
    const pages = [];
    for (const stateSlug of readdirSync(CONTENT_DIR).sort()) {
        const danceDir = join(CONTENT_DIR, stateSlug, 'dance');
        if (!existsSync(danceDir)) continue;
        for (const fileName of readdirSync(danceDir).filter((name) => name.endsWith('.yaml')).sort()) {
            const yamlPath = join(danceDir, fileName);
            const text = readFileSync(yamlPath, 'utf8');
            const data = parse(text);
            const assets = data.visual_assets ?? [];
            if (assets.length !== 2) {
                throw new Error(`${relative(ROOT, yamlPath)} must contain two visual_assets`);
            }
            if (!VIDEO_BY_DANCE[data.name]) {
                throw new Error(`${relative(ROOT, yamlPath)}: unsupported dance ${data.name}`);
            }
            const originalPaths = [
                data.hero_image,
                assets[0].src,
                assets[1].src
            ];
            data.hero_image = webpPath(data.hero_image);
            assets[0].src = webpPath(assets[0].src);
            assets[1].src = webpPath(assets[1].src);
            pages.push({
                key: `${stateSlug}/dance/${fileName}`,
                stateSlug,
                yamlPath,
                text,
                data,
                originalPaths,
                files: [
                    { role: 'hero', webPath: data.hero_image },
                    { role: 'detail', webPath: assets[0].src },
                    { role: 'context', webPath: assets[1].src }
                ]
            });
        }
    }
    if (pages.length !== 32) throw new Error(`Expected 32 dance pages, found ${pages.length}`);
    return pages;
}

function stateOffset(stateSlug) {
    return [...stateSlug].reduce((sum, character) => sum + character.charCodeAt(0), 0);
}

async function render(sourcePath, role, repeatedSource) {
    let pipeline = sharp(sourcePath, { failOn: 'error' }).rotate();
    if (repeatedSource && role === 'context') {
        pipeline = pipeline
            .resize(1760, 990, { fit: 'cover', position: 'east' })
            .extract({ left: 80, top: 45, width: WIDTH, height: HEIGHT });
    } else {
        const position = role === 'detail' ? 'west' : 'attention';
        pipeline = pipeline.resize(WIDTH, HEIGHT, {
            fit: 'cover',
            position,
            withoutEnlargement: false
        });
    }
    return pipeline.webp({ quality: 82, effort: 5 }).toBuffer();
}

function updatePage(page) {
    const video = VIDEO_BY_DANCE[page.data.name];
    const [tutorialId, tutorialTitle] = video.tutorial;
    const [videoId, videoTitle] = video.performance;
    let text = page.text;
    const normalizedPaths = page.files.map((file) => file.webPath);
    for (let index = 0; index < page.originalPaths.length; index += 1) {
        text = text.split(page.originalPaths[index]).join(normalizedPaths[index]);
    }
    if (/^tutorial_url:/m.test(text)) {
        text = text
            .replace(/^tutorial_url:.*$/m, `tutorial_url: ${yamlQuote(`https://www.youtube.com/watch?v=${tutorialId}`)}`)
            .replace(/^tutorial_title:.*$/m, `tutorial_title: ${yamlQuote(tutorialTitle)}`)
            .replace(
                /^tutorial_caption:.*$/m,
                `tutorial_caption: ${yamlQuote(`Follow this step-by-step ${page.data.name} tutorial before watching the full performance.`)}`
            );
    } else {
        const tutorialBlock = [
            `tutorial_url: ${yamlQuote(`https://www.youtube.com/watch?v=${tutorialId}`)}`,
            `tutorial_title: ${yamlQuote(tutorialTitle)}`,
            `tutorial_caption: ${yamlQuote(`Follow this step-by-step ${page.data.name} tutorial before watching the full performance.`)}`,
            ''
        ].join('\n');
        text = text.replace(/^video_url:/m, `${tutorialBlock}\nvideo_url:`);
    }
    text = text
        .replace(/^video_url:.*$/m, `video_url: ${yamlQuote(`https://www.youtube.com/watch?v=${videoId}`)}`)
        .replace(/^video_title:.*$/m, `video_title: ${yamlQuote(videoTitle)}`)
        .replace(
            /^video_caption:.*$/m,
            `video_caption: ${yamlQuote(`Watch a complete ${page.data.name} performance after learning the basic steps.`)}`
        )
        .replace(/^date_modified:.*$/m, 'date_modified: "2026-07-31"');
    const visualMatch = text.match(/^visual_assets:\r?\n[\s\S]*?(?=^faq:)/m);
    if (!visualMatch) throw new Error(`visual_assets not found in ${relative(ROOT, page.yamlPath)}`);
    text = text.replace(
        visualMatch[0],
        visualMatch[0].replace(/^(\s+layout:)\s*.*$/gm, '$1 full-width-tall')
    );
    writeFileSync(page.yamlPath, text, 'utf8');
}

function updateHub(pages) {
    const primaryByState = new Map();
    for (const page of pages) {
        if (!primaryByState.has(page.stateSlug)) primaryByState.set(page.stateSlug, []);
        primaryByState.get(page.stateSlug).push(page);
    }
    let text = readFileSync(HUB_PATH, 'utf8')
        .replace(/^date_modified:.*$/m, 'date_modified: "2026-07-31"');
    const hub = parse(text);
    for (const row of hub.table.rows) {
        const candidates = primaryByState.get(row.state_slug) ?? [];
        const page = candidates.find((candidate) => candidate.data.name === row.dance)
            ?? candidates[0];
        if (!page) throw new Error(`No detail page for hub row ${row.state_slug}`);
        const escaped = row.state_slug.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        const rowStart = new RegExp(
            `(    - state: [^\\r\\n]+\\r?\\n      state_slug: "${escaped}"\\r?\\n)(?:      symbol_image: [^\\r\\n]+\\r?\\n)?`
        );
        text = text.replace(
            rowStart,
            `$1      symbol_image: ${yamlQuote(page.data.hero_image)}\n`
        );
    }
    writeFileSync(HUB_PATH, text, 'utf8');
}

async function main() {
    await ensureThirdSources();
    const { pools, sourceDir } = collectSources();
    const pages = collectPages();
    const manifest = {
        version: 2,
        generatedAt: new Date().toISOString(),
        sourceDirectory: relative(ROOT, sourceDir),
        counts: { pages: pages.length, images: pages.length * 3 },
        items: {}
    };

    for (const [pageIndex, page] of pages.entries()) {
        const sources = pools.get(page.data.name);
        const offset = stateOffset(page.stateSlug) % sources.length;
        const selected = page.files.map((file, roleIndex) => ({
            ...file,
            sourcePath: sources[(offset + roleIndex) % sources.length]
        }));
        manifest.items[page.key] = {};
        for (const file of selected) {
            const repeatedSource = selected.filter((item) => item.sourcePath === file.sourcePath).length > 1;
            const output = await render(file.sourcePath, file.role, repeatedSource);
            const outputPath = join(WWWROOT, file.webPath.slice(1));
            mkdirSync(dirname(outputPath), { recursive: true });
            writeFileSync(outputPath, output);
            manifest.items[page.key][file.role] = {
                provider: 'local-user-source',
                identity: `local:${basename(file.sourcePath)}`,
                sourcePath: relative(ROOT, file.sourcePath),
                outputPath: file.webPath,
                sha256: createHash('sha256').update(output).digest('hex')
            };
        }
        updatePage(page);
        console.log(`${pageIndex + 1}/${pages.length}: ${page.key}`);
    }

    updateHub(pages);
    mkdirSync(dirname(MANIFEST_PATH), { recursive: true });
    writeFileSync(MANIFEST_PATH, `${JSON.stringify(manifest, null, 2)}\n`, 'utf8');
    console.log(`pages=${pages.length}`);
    console.log(`images=${pages.length * 3}`);
    console.log(`sources=${[...pools.values()].reduce((sum, pool) => sum + pool.length, 0)}`);
}

await main();
