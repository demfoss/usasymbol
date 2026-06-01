#!/usr/bin/env node
/**
 * Enriches all national park YAML files with factual data — run once after seeding.
 *
 * Fills in (empty/zero fields only — never overwrites existing values):
 *   stats.area_acres, stats.visitation_rank
 *   quick_facts: Established, Area, Annual visitors
 *   filters.landscapes  (from NPS API topics endpoint)
 *   filters.seasons     (derived from state code)
 *
 * Usage:
 *   NPS_API_KEY=your_key node scripts/enrich-national-parks.mjs
 *   node scripts/enrich-national-parks.mjs YOUR_KEY [--dry-run]
 *
 * Source: NPS 2023 Annual Visitation Statistics + Wikipedia
 */

import { readFileSync, writeFileSync, readdirSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import https from 'https';

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT      = join(__dirname, '..');
const PARKS_DIR = join(ROOT, 'Content', 'parks', 'national');

const API_KEY = process.env.NPS_API_KEY || process.argv[2];
const DRY_RUN = process.argv.includes('--dry-run');

if (!API_KEY || API_KEY.startsWith('--')) {
    console.error('Usage: NPS_API_KEY=your_key node scripts/enrich-national-parks.mjs');
    process.exit(1);
}

// ── Static dataset: nps_code → { area_acres, established, visitors_2023, rank_among_63 }
// Source: NPS Annual Visitation Report 2023 + NPS.gov + Wikipedia
// rank = 0 means the park has extremely low visitation (remote Alaska/Pacific)
const PARK_DATA = {
    'acad': { area: 49052,      est: 1919, visitors: 4009410,  rank: 5  },
    'arch': { area: 76679,      est: 1971, visitors: 1806865,  rank: 21 },
    'badl': { area: 242756,     est: 1978, visitors: 1115579,  rank: 30 },
    'bibe': { area: 801163,     est: 1944, visitors: 518805,   rank: 44 },
    'bisc': { area: 172971,     est: 1980, visitors: 564980,   rank: 43 },
    'blca': { area: 30780,      est: 1999, visitors: 506278,   rank: 45 },
    'brca': { area: 35835,      est: 1928, visitors: 2894540,  rank: 14 },
    'cany': { area: 337598,     est: 1964, visitors: 839696,   rank: 36 },
    'care': { area: 241904,     est: 1971, visitors: 1467765,  rank: 24 },
    'cave': { area: 46766,      est: 1930, visitors: 441000,   rank: 47 },
    'chis': { area: 249561,     est: 1980, visitors: 360283,   rank: 51 },
    'cong': { area: 26546,      est: 2003, visitors: 248882,   rank: 57 },
    'crla': { area: 183224,     est: 1902, visitors: 700000,   rank: 38 },
    'cuva': { area: 32572,      est: 2000, visitors: 2877760,  rank: 13 },
    'dena': { area: 6045153,    est: 1917, visitors: 601152,   rank: 41 },
    'deva': { area: 3422024,    est: 1994, visitors: 1062743,  rank: 31 },
    'drto': { area: 64701,      est: 1992, visitors: 81000,    rank: 61 },
    'ever': { area: 1508938,    est: 1934, visitors: 1177768,  rank: 27 },
    'gaar': { area: 7523898,    est: 1980, visitors: 10518,    rank: 0  },
    'glac': { area: 1013322,    est: 1910, visitors: 3001327,  rank: 11 },
    'glba': { area: 3283168,    est: 1980, visitors: 59699,    rank: 60 },
    'grba': { area: 77180,      est: 1986, visitors: 131802,   rank: 61 },
    'grca': { area: 1201647,    est: 1919, visitors: 5969811,  rank: 2  },
    'grsa': { area: 149028,     est: 2004, visitors: 609073,   rank: 42 },
    'grsm': { area: 522427,     est: 1934, visitors: 13295855, rank: 1  },
    'grte': { area: 310044,     est: 1929, visitors: 3374661,  rank: 9  },
    'gumo': { area: 86367,      est: 1972, visitors: 236000,   rank: 58 },
    'hale': { area: 33265,      est: 1916, visitors: 954789,   rank: 33 },
    'havo': { area: 325605,     est: 1916, visitors: 1501286,  rank: 23 },
    'hosp': { area: 5554,       est: 1921, visitors: 1669008,  rank: 17 },
    'indu': { area: 15349,      est: 2019, visitors: 2990456,  rank: 12 },
    'isro': { area: 571790,     est: 1940, visitors: 25405,    rank: 63 },
    'jeff': { area: 91,         est: 2018, visitors: 2201578,  rank: 16 },
    'jotr': { area: 795156,     est: 1994, visitors: 3100406,  rank: 10 },
    'katm': { area: 4093077,    est: 1980, visitors: 77000,    rank: 0  },
    'kefj': { area: 669984,     est: 1980, visitors: 387281,   rank: 49 },
    'kova': { area: 1750717,    est: 1980, visitors: 15766,    rank: 0  },
    'lacl': { area: 2619733,    est: 1980, visitors: 12000,    rank: 0  },
    'lavo': { area: 106589,     est: 1916, visitors: 515604,   rank: 46 },
    'maca': { area: 54011,      est: 1941, visitors: 686398,   rank: 39 },
    'meve': { area: 52485,      est: 1906, visitors: 613412,   rank: 40 },
    'mora': { area: 236381,     est: 1899, visitors: 1630000,  rank: 20 },
    'neri': { area: 70814,      est: 2020, visitors: 1631548,  rank: 19 },
    'noca': { area: 504643,     est: 1968, visitors: 30085,    rank: 0  },
    'npsa': { area: 8257,       est: 1988, visitors: 8495,     rank: 0  },
    'olym': { area: 922650,     est: 1938, visitors: 3991428,  rank: 6  },
    'pefo': { area: 221390,     est: 1962, visitors: 860065,   rank: 35 },
    'pinn': { area: 26686,      est: 2013, visitors: 216799,   rank: 59 },
    'redw': { area: 138999,     est: 1968, visitors: 433018,   rank: 48 },
    'romo': { area: 265807,     est: 1915, visitors: 4478001,  rank: 4  },
    'sagu': { area: 91440,      est: 1994, visitors: 1170812,  rank: 28 },
    'seki': { area: 865965,     est: 1890, visitors: 1990000,  rank: 15 }, // Sequoia & Kings Canyon
    'shen': { area: 199045,     est: 1935, visitors: 1869009,  rank: 18 },
    'thro': { area: 70447,      est: 1978, visitors: 783840,   rank: 37 },
    'viis': { area: 14689,      est: 1956, visitors: 374000,   rank: 50 },
    'voya': { area: 218222,     est: 1975, visitors: 264000,   rank: 56 },
    'whsa': { area: 146344,     est: 2019, visitors: 782469,   rank: 34 },
    'wica': { area: 33847,      est: 1903, visitors: 656397,   rank: 32 },
    'wrst': { area: 13175901,   est: 1980, visitors: 79047,    rank: 0  },
    'yell': { area: 2219791,    est: 1872, visitors: 3920526,  rank: 7  },
    'yose': { area: 747956,     est: 1890, visitors: 3865575,  rank: 8  },
    'zion': { area: 147242,     est: 1919, visitors: 4690151,  rank: 3  },
};

// ── NPS API topic name → landscape slug ──────────────────────────────
const TOPIC_TO_LANDSCAPE = {
    'Mountains':               'mountains',
    'Glaciers':                'glacier',
    'Deserts':                 'desert',
    'Coasts, Islands, Shores': 'coastline',
    'Forests & Woodlands':     'forest',
    'Waterfalls':              'waterfalls',
    'Volcanoes':               'volcano',
    'Wetlands':                'wetlands',
    'Rivers & Streams':        'river',
    'Caves & Caverns':         'cave',
    'Canyons':                 'canyon',
    'Islands':                 'island',
    'Tundra':                  'tundra',
    'Lakes':                   'lake',
    'Grasslands':              'grasslands',
    'Valleys':                 'valley',
    'Sand Dunes':              'dunes',
    'Geysers':                 'geysers',
    'Hot Springs':             'hot_springs',
    'Coral Reef':              'reef',
    'Coral Reefs':             'reef',
};

// ── Seasons by state code ────────────────────────────────────────────
function defaultSeasons(stateCode) {
    if (stateCode === 'AK')                                           return ['summer'];
    if (['HI','AS','GU','VI'].includes(stateCode))                    return ['spring','summer','fall','winter'];
    if (['AZ','NV'].includes(stateCode))                              return ['fall','winter','spring'];
    if (stateCode === 'TX' || stateCode === 'FL')                     return ['fall','winter','spring'];
    return ['spring','summer','fall'];
}

// ── Helpers ───────────────────────────────────────────────────────────
function fetchJson(url) {
    return new Promise((resolve, reject) => {
        https.get(url, { headers: { 'User-Agent': 'USASymbol-enrich/1.0' } }, (res) => {
            let data = '';
            res.on('data', c => data += c);
            res.on('end', () => {
                try { resolve(JSON.parse(data)); }
                catch (e) { reject(new Error(`JSON parse error: ${e.message}`)); }
            });
        }).on('error', reject);
    });
}

function formatVisitors(n) {
    if (n >= 1_000_000) {
        const m = (n / 1_000_000).toFixed(1).replace(/\.0$/, '');
        return `≈ ${m} million (2023)`;
    }
    if (n >= 1000) return `≈ ${Math.round(n / 1000)}K (2023)`;
    return `≈ ${n.toLocaleString()} (2023)`;
}

function formatAcres(n) {
    return `${n.toLocaleString()} acres`;
}

// ── Read a top-level scalar field from YAML text ──────────────────────
function readScalar(content, key) {
    const m = content.match(new RegExp(`^${key}:\\s*"?([^"\\n]+)"?`, 'm'));
    return m ? m[1].trim() : '';
}

// ── Replace stats.area_acres: 0 and stats.visitation_rank: 0 ─────────
function replaceStatField(content, field, value) {
    const pattern = new RegExp(`^(\\s+${field}:\\s*)0(\\s*(?:#[^\\n]*)?)$`, 'm');
    if (!pattern.test(content)) return content;
    return content.replace(pattern, `$1${value}$2`);
}

// ── Replace value: "" for a specific quick_facts label ────────────────
function replaceQuickFactValue(content, label, newValue) {
    const escaped = label.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    // Match: `- label: "Established"\n    value: ""`
    const pattern = new RegExp(
        `(- label: "${escaped}"\\n\\s+value:\\s*)""`,
        'm'
    );
    if (!pattern.test(content)) return content;
    const safeValue = newValue.replace(/\\/g, '\\\\').replace(/"/g, '\\"');
    return content.replace(pattern, `$1"${safeValue}"`);
}

// ── Replace landscapes: [] with a proper list ─────────────────────────
function replaceLandscapes(content, landscapes) {
    if (!landscapes.length || !content.includes('landscapes: []')) return content;
    const list = landscapes.map(l => `    - ${l}`).join('\n');
    return content.replace('landscapes: []', `landscapes:\n${list}`);
}

// ── Replace seasons block ─────────────────────────────────────────────
function replaceSeasons(content, seasons) {
    if (seasons.join(',') === 'spring,summer,fall') return content; // no change needed
    // Handle both LF and CRLF line endings
    return content
        .replace(/  seasons:\r?\n    - spring\r?\n    - summer\r?\n    - fall/,
                 '  seasons:\n' + seasons.map(s => `    - ${s}`).join('\n'));
}

// ── Main ──────────────────────────────────────────────────────────────
async function main() {
    const files = readdirSync(PARKS_DIR).filter(f => f.endsWith('.yml')).sort();
    console.log(`Found ${files.length} park files`);

    // One API call for all park topics
    let topicsMap = {};
    if (API_KEY) {
        console.log('Fetching topics from NPS API…');
        try {
            const data = await fetchJson(
                `https://developer.nps.gov/api/v1/parks?limit=500&api_key=${API_KEY}&fields=topics`
            );
            for (const park of data.data || []) {
                const landscapes = [...new Set(
                    (park.topics || [])
                        .map(t => TOPIC_TO_LANDSCAPE[t.name])
                        .filter(Boolean)
                )];
                if (landscapes.length) topicsMap[park.parkCode] = landscapes;
            }
            console.log(`Got topics for ${Object.keys(topicsMap).length} parks`);
        } catch (e) {
            console.warn(`Warning: topics fetch failed (${e.message}) — skipping landscapes`);
        }
    }

    console.log('');
    let updated = 0, unchanged = 0, unknown = 0;

    for (const file of files) {
        const filePath = join(PARKS_DIR, file);
        let content = readFileSync(filePath, 'utf8');
        const original = content;

        const npsCode    = readScalar(content, 'nps_code').toLowerCase().replace(/['"]/g, '');
        const stateCode  = readScalar(content, 'state_code').replace(/['"]/g, '');
        const parkData   = PARK_DATA[npsCode];

        if (!parkData && !topicsMap[npsCode]) {
            console.log(`  ??     ${file} (nps_code="${npsCode}" not in dataset)`);
            unknown++;
        }

        // ── Numeric stats ─────────────────────────────────────────
        if (parkData) {
            if (parkData.area  > 0) content = replaceStatField(content, 'area_acres',      parkData.area);
            if (parkData.rank  > 0) content = replaceStatField(content, 'visitation_rank', parkData.rank);

            // ── Quick facts ───────────────────────────────────────
            if (parkData.est)                   content = replaceQuickFactValue(content, 'Established',    String(parkData.est));
            if (parkData.area > 0)              content = replaceQuickFactValue(content, 'Area',           formatAcres(parkData.area));
            if (parkData.visitors > 0)          content = replaceQuickFactValue(content, 'Annual visitors',formatVisitors(parkData.visitors));
        }

        // ── Landscapes from NPS topics ────────────────────────────
        if (topicsMap[npsCode]) {
            content = replaceLandscapes(content, topicsMap[npsCode]);
        }

        // ── Seasons from state code ───────────────────────────────
        if (stateCode) {
            content = replaceSeasons(content, defaultSeasons(stateCode));
        }

        // ── Write ─────────────────────────────────────────────────
        if (content !== original) {
            if (!DRY_RUN) writeFileSync(filePath, content, 'utf8');
            console.log(`  UPDATE ${file}`);
            updated++;
        } else {
            console.log(`  ok     ${file}`);
            unchanged++;
        }
    }

    console.log('\n────────────────────────────────────────');
    console.log(`Updated: ${updated}  |  Already complete: ${unchanged}  |  Unknown code: ${unknown}`);
    if (DRY_RUN) console.log('(dry-run — nothing written)');
    if (unknown > 0) {
        console.log('\nParks with unknown NPS codes need manual data — check PARK_DATA in the script.');
    }
    console.log('\nNext: fill nearest_city, nearest_major_airport, reservation_status per park (Step 2).');
}

main().catch(e => { console.error(e); process.exit(1); });
