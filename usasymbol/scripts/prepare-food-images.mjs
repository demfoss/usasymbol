#!/usr/bin/env node

import { existsSync, mkdirSync, readdirSync, readFileSync, statSync } from 'fs';
import { basename, dirname, extname, join, relative } from 'path';
import sharp from 'sharp';
import { parse } from 'yaml';

const ROOT = join(import.meta.dirname, '..');
const FOOD_IMAGE_DIR = join(ROOT, 'wwwroot', 'images', 'foods');
const ARCHIVED_SOURCE_DIR = join(ROOT, 'artifacts', 'food-image-sources', 'originals');
const SOURCE_DIR = readdirSync(FOOD_IMAGE_DIR).some(
    (name) =>
        statSync(join(FOOD_IMAGE_DIR, name)).isFile() &&
        /\.(?:jpe?g|jfif|png|webp)$/i.test(name)
)
    ? FOOD_IMAGE_DIR
    : ARCHIVED_SOURCE_DIR;
const STATES_DIR = join(ROOT, 'Content', 'states');
const HUB_NAMES = [
    'vegetables',
    'nuts',
    'fruits',
    'dishes',
    'desserts',
    'crops'
];

const HERO_MAX_SIDE = 1600;
const HERO_QUALITY = 82;
const COMPOSITE_WIDTH = 1440;
const COMPOSITE_HEIGHT = 810;
const COMPOSITE_GAP = 8;

const SPECIAL_ASSIGNMENTS = new Map([
    ['arkansas/food-grape.yaml', ['Cynthiana (Vitis aestivalis).jpg']],
    [
        'california/food-nut.yaml',
        ['Almond.jpg', 'walnut.jpg', 'Pistachio.jpg', 'Pecan.jpg']
    ],
    ['louisiana/food-jellies.yaml', ['Mayhaw jelly.jpg', 'Louisiana sugar cane jelly.webp']],
    ['maine/food-dessert.yaml', ['Blueberry pie.jpg']],
    ['mississippi/food-fruit.yaml', ['Blueberryjpg.jpg']],
    ['missouri/food-grape.yaml', ['Cynthiana (Vitis aestivalis).jpg']],
    ['new-jersey/food-sandwich.yaml', ['Pork roll egg and cheese.jfif']],
    ['new-mexico/food-vegetable.yaml', ['New Mexico chile.jpg', 'pinto beans.jpg']],
    ['north-carolina/food-blue-berry.yaml', ['Blueberryjpg.jpg']],
    ['north-carolina/food-fruit.yaml', ['Scuppernong grape.webp']],
    ['ohio/food-fruit.yaml', ['South Arkansas vine ripe pink tomato.jpg']],
    [
        'oklahoma/food-meal.yaml',
        ['Chicken-fried steak.jpg', 'cornbread.jpg', 'black-eyed peas.jfif', 'fried okra.jpg']
    ],
    ['south-carolina/food-picnic-cuisine.yaml', ['Barbecue.jfif']],
    ['tennessee/food-fruit.yaml', ['South Arkansas vine ripe pink tomato.jpg']],
    ['texas/food-snack.yaml', ['Tortilla chips.jpg', 'salsa.jpg']],
    ['vermont/food-flavor.yaml', ['Pure Maine maple syrup.jpg']],
    ['vermont/food-pie.yaml', ['Apple pie.png', 'Cheese.jpg']],
    ['wisconsin/food-grain.yaml', ['Corn.jpg']]
]);

const INTENTIONALLY_MISSING = new Set([
    // No source image for this page exists in wwwroot/images/foods.
    'alabama/food-spirit.yaml'
]);

function normalize(value) {
    return value
        .normalize('NFKD')
        .replace(/\p{Diacritic}/gu, '')
        .toLowerCase()
        .replace(/&/g, 'and')
        .replace(/[^a-z0-9]+/g, ' ')
        .trim();
}

function slugify(value) {
    return value
        .normalize('NFKD')
        .replace(/\p{Diacritic}/gu, '')
        .toLowerCase()
        .replaceAll(' ', '-')
        .replace(/['’ʻ`,.]/g, '')
        .replace(/[^a-z0-9-]/g, '')
        .replace(/-+/g, '-')
        .replace(/^-|-$/g, '');
}

function listSourceFiles() {
    if (!existsSync(SOURCE_DIR)) {
        throw new Error(`Food image source directory does not exist: ${SOURCE_DIR}`);
    }
    return readdirSync(SOURCE_DIR)
        .filter((name) => {
            const path = join(SOURCE_DIR, name);
            return statSync(path).isFile() && /\.(?:jpe?g|jfif|png|webp)$/i.test(name);
        })
        .sort((left, right) => left.localeCompare(right));
}

function listFoodPages() {
    const pages = [];

    for (const stateSlug of readdirSync(STATES_DIR).sort()) {
        const stateDir = join(STATES_DIR, stateSlug);
        if (!statSync(stateDir).isDirectory()) continue;

        for (const fileName of readdirSync(stateDir).sort()) {
            if (!/^food-.*\.yaml$/i.test(fileName)) continue;
            const filePath = join(stateDir, fileName);
            const data = parse(readFileSync(filePath, 'utf8'));
            pages.push({
                key: `${stateSlug}/${fileName}`,
                filePath,
                data
            });
        }
    }

    return pages;
}

function buildAssignments(pages, sourceFiles) {
    const sourceByName = new Map(sourceFiles.map((name) => [name, join(SOURCE_DIR, name)]));
    const normalizedSources = new Map();

    for (const fileName of sourceFiles) {
        const key = normalize(basename(fileName, extname(fileName)));
        if (!normalizedSources.has(key)) normalizedSources.set(key, []);
        normalizedSources.get(key).push(fileName);
    }

    const assignments = new Map();
    const errors = [];

    for (const page of pages) {
        if (INTENTIONALLY_MISSING.has(page.key)) continue;

        let names = SPECIAL_ASSIGNMENTS.get(page.key);
        if (!names) {
            const matches = normalizedSources.get(normalize(page.data.name)) ?? [];
            if (matches.length !== 1) {
                errors.push(
                    `${page.key}: expected one source for "${page.data.name}", found ${matches.length} (${matches.join(', ')})`
                );
                continue;
            }
            names = matches;
        }

        const missingSources = names.filter((name) => !sourceByName.has(name));
        if (missingSources.length > 0) {
            errors.push(`${page.key}: missing source files: ${missingSources.join(', ')}`);
            continue;
        }

        if (
            typeof page.data.hero_image !== 'string' ||
            !page.data.hero_image.startsWith('/images/foods/') ||
            !page.data.hero_image.endsWith('.webp')
        ) {
            errors.push(`${page.key}: invalid hero_image: ${page.data.hero_image}`);
            continue;
        }

        assignments.set(page.key, {
            page,
            sources: names.map((name) => sourceByName.get(name)),
            output: join(ROOT, 'wwwroot', page.data.hero_image.replace(/^\//, ''))
        });
    }

    if (errors.length > 0) {
        throw new Error(`Image mapping failed:\n${errors.join('\n')}`);
    }

    return assignments;
}

function validateHubPaths(assignments) {
    const pagesByHeroPath = new Map(
        [...assignments.values()].map(({ page }) => [page.data.hero_image, page])
    );
    const hubImagePaths = new Set();
    const errors = [];

    for (const hubName of HUB_NAMES) {
        const hubPath = join(ROOT, 'Content', 'symbols', `${hubName}.yml`);
        const hub = parse(readFileSync(hubPath, 'utf8'));
        const rows = hub?.table?.rows;
        if (!Array.isArray(rows)) {
            errors.push(`${hubName}.yml: table.rows is missing`);
            continue;
        }

        for (const row of rows) {
            const imagePath = row.symbol_image;
            hubImagePaths.add(imagePath);
            const page = pagesByHeroPath.get(imagePath);
            if (!page) {
                errors.push(`${hubName}.yml: no food page hero matches ${imagePath}`);
                continue;
            }
            const expectedSlug = slugify(page.data.name);
            if (row.food_slug !== expectedSlug) {
                errors.push(
                    `${hubName}.yml: ${row.state} food_slug is ${row.food_slug}, expected ${expectedSlug}`
                );
            }
        }
    }

    const unlisted = [...pagesByHeroPath.keys()].filter((path) => !hubImagePaths.has(path));
    if (unlisted.length > 0) {
        errors.push(`Food page heroes absent from the six hubs: ${unlisted.join(', ')}`);
    }

    if (errors.length > 0) {
        throw new Error(`Hub validation failed:\n${errors.join('\n')}`);
    }

    return hubImagePaths.size;
}

async function renderSimple(source, output) {
    await sharp(source, { failOn: 'error' })
        .rotate()
        .resize({
            width: HERO_MAX_SIDE,
            height: HERO_MAX_SIDE,
            fit: 'inside',
            withoutEnlargement: true
        })
        .webp({ quality: HERO_QUALITY, effort: 5 })
        .toFile(output);
}

function compositeLayout(count) {
    if (count === 2) return { columns: 2, rows: 1 };
    if (count === 3 || count === 4) return { columns: 2, rows: 2 };
    throw new Error(`Unsupported composite image count: ${count}`);
}

async function renderComposite(sources, output) {
    const { columns, rows } = compositeLayout(sources.length);
    const panelWidth = Math.floor(
        (COMPOSITE_WIDTH - COMPOSITE_GAP * (columns - 1)) / columns
    );
    const panelHeight = Math.floor(
        (COMPOSITE_HEIGHT - COMPOSITE_GAP * (rows - 1)) / rows
    );
    const panels = [];

    for (let index = 0; index < sources.length; index += 1) {
        const column = index % columns;
        const row = Math.floor(index / columns);
        const buffer = await sharp(sources[index], { failOn: 'error' })
            .rotate()
            .resize(panelWidth, panelHeight, {
                fit: 'cover',
                position: 'attention'
            })
            .toBuffer();
        panels.push({
            input: buffer,
            left: column * (panelWidth + COMPOSITE_GAP),
            top: row * (panelHeight + COMPOSITE_GAP)
        });
    }

    await sharp({
        create: {
            width: COMPOSITE_WIDTH,
            height: COMPOSITE_HEIGHT,
            channels: 3,
            background: '#f2f2ef'
        }
    })
        .composite(panels)
        .webp({ quality: HERO_QUALITY, effort: 5 })
        .toFile(output);
}

async function main() {
    const sourceFiles = listSourceFiles();
    const pages = listFoodPages();
    const assignments = buildAssignments(pages, sourceFiles);
    const hubRows = validateHubPaths(assignments);
    const usedSources = new Set();
    let sourceBytes = 0;
    let outputBytes = 0;

    for (const { sources, output } of assignments.values()) {
        mkdirSync(dirname(output), { recursive: true });
        for (const source of sources) usedSources.add(basename(source));

        if (sources.length === 1) {
            await renderSimple(sources[0], output);
        } else {
            await renderComposite(sources, output);
        }

        const metadata = await sharp(output).metadata();
        if (
            metadata.format !== 'webp' ||
            !metadata.width ||
            !metadata.height ||
            metadata.width > HERO_MAX_SIDE ||
            metadata.height > HERO_MAX_SIDE
        ) {
            throw new Error(
                `Invalid output ${relative(ROOT, output)}: ${metadata.format} ${metadata.width}x${metadata.height}`
            );
        }
        outputBytes += statSync(output).size;
    }

    for (const name of usedSources) {
        sourceBytes += statSync(join(SOURCE_DIR, name)).size;
    }

    const unusedSources = sourceFiles.filter((name) => !usedSources.has(name));
    const missingPages = [...INTENTIONALLY_MISSING].filter((key) =>
        pages.some((page) => page.key === key)
    );

    console.log(`food_pages=${pages.length}`);
    console.log(`generated_heroes=${assignments.size}`);
    console.log(`hub_rows=${hubRows}`);
    console.log(`unique_sources_used=${usedSources.size}`);
    console.log(`source_bytes=${sourceBytes}`);
    console.log(`output_bytes=${outputBytes}`);
    console.log(`missing_pages=${missingPages.join('; ') || '(none)'}`);
    console.log(`unused_sources=${unusedSources.join('; ') || '(none)'}`);
}

await main();
