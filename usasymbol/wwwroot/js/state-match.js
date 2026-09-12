(function () {
    "use strict";

    var payload = window.stateMatchData;
    if (!payload || !Array.isArray(payload.places) || !Array.isArray(payload.metrics)) {
        return;
    }

    var places = payload.places;
    var metrics = payload.metrics;
    var metricByKey = new Map(metrics.map(function (metric) { return [metric.key, metric]; }));
    var placeBySlug = new Map(places.map(function (place) { return [place.slug, place]; }));

    var sliderHost = document.getElementById("sm-sliders");
    var rankingHost = document.getElementById("sm-ranking-list");
    var showAllButton = document.getElementById("sm-show-all");
    var searchInput = document.getElementById("sm-search");
    var resetButton = document.getElementById("sm-reset");
    var shareButton = document.getElementById("sm-share");
    var shareStatus = document.getElementById("sm-share-status");
    var resultSummary = document.getElementById("sm-result-summary");
    var drawer = document.getElementById("sm-drawer");
    var drawerContent = document.getElementById("sm-drawer-content");
    var drawerBackdrop = document.getElementById("sm-drawer-backdrop");
    var mobileToggle = document.getElementById("sm-mobile-toggle");
    var controlsBody = document.getElementById("sm-controls-body");
    var exportCsvButton = document.getElementById("sm-export-csv");
    var quizLaunchButton = document.getElementById("sm-quiz-launch");
    var quizEl = document.getElementById("sm-quiz");
    var quizOptionsHost = document.getElementById("sm-quiz-options");
    var quizEqualButton = document.getElementById("sm-quiz-equal");
    var quizCancelButton = document.getElementById("sm-quiz-cancel");
    var quizStepLabel = document.getElementById("sm-quiz-step");
    var quizProgressBar = document.getElementById("sm-quiz-progress-bar");
    var manualControlsEl = document.getElementById("sm-manual-controls");
    var calcIncomeInput = document.getElementById("sm-calc-income");
    var calcHousingInput = document.getElementById("sm-calc-housing");
    var calcOwnsCheckbox = document.getElementById("sm-calc-owns");
    var calcHomePriceField = document.getElementById("sm-calc-home-price-field");
    var calcHomePriceInput = document.getElementById("sm-calc-home-price");
    var calcListHost = document.getElementById("sm-calc-list");
    var calcOccupationSelect = document.getElementById("sm-calc-occupation");
    var calcOccupationNote = document.getElementById("sm-calc-occupation-note");
    var occupationSalaries = (payload.occupationSalaries && payload.occupationSalaries.occupations) || {};
    var partnerPresetsHost = document.getElementById("sm-partner-presets");
    var partnerResultHost = document.getElementById("sm-partner-result");
    var partnerPreset = null;

    var presets = {
        balanced: defaults(),
        budget: preset({ cost: 100, housing: 95, incometax: 35, propertytax: 25, salestax: 25, jobs: 52, income: 36, safety: 45, health: 30, education: 25, warmth: 20 }),
        career: preset({ income: 100, jobs: 96, education: 68, cost: 42, housing: 35, safety: 35, health: 40, incometax: 15, propertytax: 10, salestax: 10, warmth: 15 }),
        family: preset({ safety: 100, education: 95, health: 88, housing: 62, cost: 60, jobs: 55, income: 48, incometax: 15, propertytax: 10, salestax: 10, warmth: 20 }),
        warm: preset({ warmth: 100, cost: 58, housing: 52, jobs: 45, safety: 42, health: 40, incometax: 15, propertytax: 10, salestax: 10, income: 30, education: 25 })
    };

    // Nine forced-choice pairs covering all 11 metrics at least once. Weight math in
    // finishQuiz() scores each metric by win-rate (wins ÷ appearances), so a metric asked
    // about twice isn't unfairly advantaged over one asked about once.
    var QUIZ_QUESTIONS = [
        { a: "housing", b: "safety" },
        { a: "income", b: "jobs" },
        { a: "incometax", b: "health" },
        { a: "warmth", b: "cost" },
        { a: "education", b: "propertytax" },
        { a: "safety", b: "warmth" },
        { a: "income", b: "housing" },
        { a: "salestax", b: "jobs" },
        { a: "health", b: "cost" }
    ];
    var quizAppearances = {};
    QUIZ_QUESTIONS.forEach(function (question) {
        quizAppearances[question.a] = (quizAppearances[question.a] || 0) + 1;
        quizAppearances[question.b] = (quizAppearances[question.b] || 0) + 1;
    });
    var quizStep = 0;
    var quizTally = {};

    // Grid position (column, row — 1-indexed) for each state on a 12x8 tile grid,
    // approximating real geography so neighbors on the tile grid are neighbors on the map.
    // Source layout: github.com/kristw/gridmap-layout-usa
    // Declared here (not down by buildTileGrid) because it must be assigned before the
    // top-level init calls below run — `var` hoists the declaration but not the assignment.
    var STATE_GRID = {
        AK: [1, 1], ME: [12, 1],
        VT: [10, 2], NH: [11, 2], MA: [12, 2],
        WA: [2, 3], MT: [3, 3], ND: [4, 3], SD: [5, 3], MN: [6, 3], WI: [7, 3], MI: [8, 3], NY: [10, 3], CT: [11, 3], RI: [12, 3],
        OR: [2, 4], ID: [3, 4], WY: [4, 4], NE: [5, 4], IA: [6, 4], IL: [7, 4], IN: [8, 4], OH: [9, 4], PA: [10, 4], NJ: [11, 4],
        CA: [1, 5], NV: [2, 5], UT: [3, 5], CO: [4, 5], KS: [5, 5], MO: [6, 5], KY: [7, 5], WV: [8, 5], DC: [9, 5], MD: [10, 5], DE: [11, 5],
        AZ: [3, 6], NM: [4, 6], OK: [5, 6], AR: [6, 6], TN: [7, 6], VA: [8, 6], NC: [9, 6],
        TX: [4, 7], LA: [5, 7], MS: [6, 7], AL: [7, 7], GA: [8, 7], SC: [9, 7],
        HI: [1, 8], FL: [8, 8]
    };

    // Match-strength color scale: paper at score 0, dark teal-ink at score 100. Wider than a
    // straight paper→teal mix so mid-range scores stay visually distinguishable. Declared here
    // (not down by matchColor) for the same reason as STATE_GRID above — it must be assigned
    // before the top-level init calls run, since `var` only hoists the declaration.
    var MATCH_LOW = { r: 0xf5, g: 0xf1, b: 0xe8 };
    var MATCH_HIGH = { r: 0x12, g: 0x33, b: 0x3a };

    var FILTER_KEYS = ["noIncomeTax", "popMillion", "warmClimate"];

    var filters = readFiltersFromUrl();
    var weights = readWeightsFromUrl();
    var metricRanges = buildMetricRanges();
    var scored = [];
    var showAll = false;
    var currentSlug = null;
    var lastFocused = null;
    var tileReady = false;
    var tileBySlug = new Map();
    var updateUrlTimer = null;

    buildSliders();
    bindControls();
    bindFilters();
    bindQuiz();
    bindCalculator();
    bindPartner();
    renderAll();
    buildTileGrid();

    var initialState = new URL(window.location.href).searchParams.get("state");
    if (initialState && placeBySlug.has(initialState)) {
        openDetail(initialState, false);
    }

    function defaults() {
        var result = {};
        metrics.forEach(function (metric) {
            result[metric.key] = clampNumber(metric.defaultWeight, 0, 100);
        });
        return result;
    }

    function preset(values) {
        var result = defaults();
        Object.keys(values).forEach(function (key) {
            if (metricByKey.has(key)) {
                result[key] = clampNumber(values[key], 0, 100);
            }
        });
        return result;
    }

    function readFiltersFromUrl() {
        var result = { noIncomeTax: false, popMillion: false, warmClimate: false };
        var encoded = new URL(window.location.href).searchParams.get("f");
        if (!encoded) {
            return result;
        }
        encoded.split(",").forEach(function (key) {
            if (FILTER_KEYS.indexOf(key) !== -1) {
                result[key] = true;
            }
        });
        return result;
    }

    function readWeightsFromUrl() {
        var result = defaults();

        // A preset landing page (e.g. /match/low-taxes) seeds its own weights server-side.
        // A shared "?w=" link (checked next) still wins over the page's own preset default.
        if (payload.initialWeights) {
            Object.keys(payload.initialWeights).forEach(function (key) {
                if (!metricByKey.has(key)) {
                    return;
                }
                var value = Number(payload.initialWeights[key]);
                if (Number.isFinite(value)) {
                    result[key] = clampNumber(value, 0, 100);
                }
            });
        }

        var encoded = new URL(window.location.href).searchParams.get("w");
        if (!encoded) {
            return result;
        }

        encoded.split(",").forEach(function (part) {
            var pieces = part.split(":");
            if (pieces.length !== 2 || !metricByKey.has(pieces[0])) {
                return;
            }

            var value = Number(pieces[1]);
            if (Number.isFinite(value)) {
                result[pieces[0]] = clampNumber(value, 0, 100);
            }
        });

        return result;
    }

    function buildMetricRanges() {
        var ranges = new Map();
        metrics.forEach(function (metric) {
            var values = places
                .map(function (place) { return getPlaceMetric(place, metric.key); })
                .filter(Boolean)
                .map(function (value) { return value.raw; });

            ranges.set(metric.key, {
                min: values.length ? Math.min.apply(null, values) : 0,
                max: values.length ? Math.max.apply(null, values) : 0
            });
        });
        return ranges;
    }

    function getPlaceMetric(place, key) {
        return place.metrics.find(function (metric) { return metric.key === key; }) || null;
    }

    function normalizedValue(metricValue) {
        var range = metricRanges.get(metricValue.key);
        if (!range || range.max <= range.min) {
            return 0.5;
        }

        var normalized = (metricValue.raw - range.min) / (range.max - range.min);
        return metricValue.direction >= 0 ? normalized : 1 - normalized;
    }

    function rawPosition(metricValue) {
        var range = metricRanges.get(metricValue.key);
        if (!range || range.max <= range.min) {
            return 50;
        }
        return clampNumber(((metricValue.raw - range.min) / (range.max - range.min)) * 100, 0, 100);
    }

    function hasActiveWeights() {
        return metrics.some(function (metric) { return weights[metric.key] > 0; });
    }

    function matchColor(score) {
        var t = clampNumber(score, 0, 100) / 100;
        var r = Math.round(MATCH_LOW.r + (MATCH_HIGH.r - MATCH_LOW.r) * t);
        var g = Math.round(MATCH_LOW.g + (MATCH_HIGH.g - MATCH_LOW.g) * t);
        var b = Math.round(MATCH_LOW.b + (MATCH_HIGH.b - MATCH_LOW.b) * t);
        return {
            fill: "rgb(" + r + "," + g + "," + b + ")",
            isDark: relativeLuminance(r, g, b) < 0.42
        };
    }

    function relativeLuminance(r, g, b) {
        var channel = function (value) {
            var normalized = value / 255;
            return normalized <= 0.03928 ? normalized / 12.92 : Math.pow((normalized + 0.055) / 1.055, 2.4);
        };
        return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);
    }

    function scorePlaces() {
        var hasAnyWeight = hasActiveWeights();
        var rows = places.map(function (place) {
            var weightedTotal = 0;
            var usedWeight = 0;
            var normalized = {};

            place.metrics.forEach(function (metricValue) {
                var weight = weights[metricValue.key] || 0;
                var value = normalizedValue(metricValue);
                normalized[metricValue.key] = value;
                if (weight <= 0) {
                    return;
                }
                weightedTotal += value * weight;
                usedWeight += weight;
            });

            return {
                place: place,
                score: hasAnyWeight && usedWeight > 0 ? Math.round((weightedTotal / usedWeight) * 100) : 0,
                normalized: normalized,
                usedWeight: usedWeight,
                eligible: passesFilters(place),
                rank: 0
            };
        });

        rows.sort(function (a, b) {
            if (a.eligible !== b.eligible) {
                return a.eligible ? -1 : 1;
            }
            return b.score - a.score || a.place.name.localeCompare(b.place.name);
        });

        rows.forEach(function (row, index) {
            row.rank = index + 1;
        });

        return rows;
    }

    function passesFilters(place) {
        if (filters.noIncomeTax) {
            var incomeTax = getPlaceMetric(place, "incometax");
            if (!incomeTax || incomeTax.raw > 0) {
                return false;
            }
        }
        if (filters.popMillion && (!place.population || place.population < 1000000)) {
            return false;
        }
        if (filters.warmClimate) {
            var warmth = getPlaceMetric(place, "warmth");
            if (!warmth || warmth.raw < 60) {
                return false;
            }
        }
        return true;
    }

    function buildSliders() {
        sliderHost.innerHTML = metrics.map(function (metric) {
            var safeKey = escapeHtml(metric.key);
            return [
                '<div class="sm-metric-control">',
                '<label class="sm-metric-head" for="sm-weight-' + safeKey + '">',
                '<i class="' + escapeHtml(metric.icon) + ' sm-metric-icon" aria-hidden="true"></i>',
                '<span><span class="sm-metric-name">' + escapeHtml(metric.name) + '</span>',
                '<span class="sm-metric-hint">' + escapeHtml(metric.hint) + '</span></span>',
                '<output class="sm-metric-value" id="sm-value-' + safeKey + '" for="sm-weight-' + safeKey + '">' + weights[metric.key] + '</output>',
                '</label>',
                '<div class="sm-range-row">',
                '<span class="sm-range-endpoint">0</span>',
                '<input class="sm-range" id="sm-weight-' + safeKey + '" type="range" min="0" max="100" step="5" value="' + weights[metric.key] + '"',
                ' data-key="' + safeKey + '" aria-label="' + escapeHtml(metric.name) + ' importance, 0 to 100. 0 removes it from your score." title="' + escapeHtml(metric.description) + '">',
                '<span class="sm-range-endpoint">100</span>',
                '</div>',
                '</div>'
            ].join("");
        }).join("");

        sliderHost.querySelectorAll(".sm-range").forEach(function (input) {
            updateRangeFill(input);
            applyZeroState(input.dataset.key);
            input.addEventListener("input", function () {
                var key = input.dataset.key;
                weights[key] = Number(input.value);
                document.getElementById("sm-value-" + key).textContent = input.value;
                updateRangeFill(input);
                applyZeroState(key);
                setActivePreset(null);
                renderAll();
                scheduleUrlUpdate();
            });
        });

        syncPresetState();
    }

    function updateRangeFill(input) {
        var min = Number(input.min) || 0;
        var max = Number(input.max) || 100;
        var percent = ((Number(input.value) - min) / (max - min)) * 100;
        input.style.setProperty("--sm-range-fill", percent + "%");
    }

    function applyZeroState(key) {
        var output = document.getElementById("sm-value-" + key);
        if (output) {
            output.classList.toggle("is-zero", weights[key] === 0);
        }
    }

    function bindControls() {
        document.querySelectorAll(".sm-preset").forEach(function (button) {
            button.addEventListener("click", function () {
                var name = button.dataset.preset;
                if (!presets[name]) {
                    return;
                }
                weights = Object.assign({}, presets[name]);
                syncSliderValues();
                setActivePreset(name);
                renderAll();
                scheduleUrlUpdate();
            });
        });

        resetButton.addEventListener("click", function () {
            weights = defaults();
            syncSliderValues();
            setActivePreset("balanced");
            renderAll();
            scheduleUrlUpdate();
        });

        showAllButton.addEventListener("click", function () {
            showAll = !showAll;
            renderRanking();
        });

        searchInput.addEventListener("input", renderRanking);

        shareButton.addEventListener("click", copyShareLink);
        if (exportCsvButton) {
            exportCsvButton.addEventListener("click", exportCsv);
        }
        drawerBackdrop.addEventListener("click", closeDetail);

        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape" && currentSlug) {
                closeDetail();
            }
        });

        mobileToggle.addEventListener("click", function () {
            var collapsed = controlsBody.classList.toggle("is-collapsed");
            mobileToggle.setAttribute("aria-expanded", String(!collapsed));
        });
    }

    function bindFilters() {
        document.querySelectorAll(".sm-filter-toggle input").forEach(function (input) {
            var key = input.dataset.filter;
            input.checked = Boolean(filters[key]);
            input.addEventListener("change", function () {
                filters[key] = input.checked;
                renderAll();
                scheduleUrlUpdate();
            });
        });
    }

    function bindQuiz() {
        if (!quizLaunchButton || !quizEl) {
            return;
        }
        quizLaunchButton.addEventListener("click", startQuiz);
        quizEqualButton.addEventListener("click", function () {
            var question = QUIZ_QUESTIONS[quizStep];
            quizTally[question.a] = (quizTally[question.a] || 0) + 0.5;
            quizTally[question.b] = (quizTally[question.b] || 0) + 0.5;
            advanceQuiz();
        });
        quizCancelButton.addEventListener("click", closeQuiz);
    }

    function startQuiz() {
        quizStep = 0;
        quizTally = {};
        quizLaunchButton.hidden = true;
        manualControlsEl.hidden = true;
        quizEl.hidden = false;
        renderQuizStep();
    }

    function renderQuizStep() {
        var question = QUIZ_QUESTIONS[quizStep];
        var optionA = metricByKey.get(question.a);
        var optionB = metricByKey.get(question.b);

        quizStepLabel.textContent = "Question " + (quizStep + 1) + " of " + QUIZ_QUESTIONS.length;
        quizProgressBar.style.width = Math.round((quizStep / QUIZ_QUESTIONS.length) * 100) + "%";

        quizOptionsHost.innerHTML = [optionA, optionB].map(function (option) {
            return '<button type="button" class="sm-quiz-option" data-key="' + escapeHtml(option.key) + '">' +
                '<i class="' + escapeHtml(option.icon) + '" aria-hidden="true"></i>' +
                '<span><strong>' + escapeHtml(option.name) + '</strong><small>' + escapeHtml(option.hint) + '</small></span>' +
                '</button>';
        }).join('<span class="sm-quiz-vs">or</span>');

        quizOptionsHost.querySelectorAll(".sm-quiz-option").forEach(function (button) {
            button.addEventListener("click", function () {
                var winner = button.dataset.key;
                var loser = winner === question.a ? question.b : question.a;
                quizTally[winner] = (quizTally[winner] || 0) + 1;
                advanceQuiz();
            });
        });
    }

    function advanceQuiz() {
        quizStep++;
        if (quizStep >= QUIZ_QUESTIONS.length) {
            finishQuiz();
        } else {
            renderQuizStep();
        }
    }

    function finishQuiz() {
        var result = defaults();
        metrics.forEach(function (metric) {
            var appearances = quizAppearances[metric.key] || 0;
            if (appearances > 0) {
                var winRate = (quizTally[metric.key] || 0) / appearances;
                result[metric.key] = clampNumber(Math.round((winRate * 100) / 5) * 5, 0, 100);
            }
        });

        weights = result;
        syncSliderValues();
        setActivePreset(null);
        renderAll();
        scheduleUrlUpdate();
        closeQuiz();
    }

    function closeQuiz() {
        quizEl.hidden = true;
        manualControlsEl.hidden = false;
        quizLaunchButton.hidden = false;
    }

    function bindCalculator() {
        if (!calcIncomeInput || !calcListHost) {
            return;
        }

        if (calcOccupationSelect) {
            Object.keys(occupationSalaries).forEach(function (key) {
                var occupation = occupationSalaries[key];
                var option = document.createElement("option");
                option.value = key;
                option.textContent = occupation.label || key;
                calcOccupationSelect.appendChild(option);
            });
            calcOccupationSelect.addEventListener("change", function () {
                calcIncomeInput.disabled = Boolean(calcOccupationSelect.value);
                computeMoneyLeft();
            });
        }

        [calcIncomeInput, calcHousingInput, calcHomePriceInput].forEach(function (input) {
            input.addEventListener("input", computeMoneyLeft);
        });
        calcOwnsCheckbox.addEventListener("change", function () {
            calcHomePriceField.hidden = !calcOwnsCheckbox.checked;
            computeMoneyLeft();
        });
        computeMoneyLeft();
    }

    function computeMoneyLeft() {
        var occupationKey = calcOccupationSelect ? calcOccupationSelect.value : "";
        var occupation = occupationKey ? occupationSalaries[occupationKey] : null;
        var flatIncome = Math.max(0, Number(calcIncomeInput.value) || 0);
        var housingAnnual = Math.max(0, Number(calcHousingInput.value) || 0) * 12;
        var owns = calcOwnsCheckbox.checked;
        var homePrice = owns ? Math.max(0, Number(calcHomePriceInput.value) || 0) : 0;

        var skipped = 0;
        var rows = [];

        places.forEach(function (place) {
            var income = flatIncome;
            if (occupation) {
                var occupationIncome = occupation.byState ? occupation.byState[place.abbreviation] : null;
                if (occupationIncome == null) {
                    skipped++;
                    return;
                }
                income = occupationIncome;
            }

            var incomeTax = getPlaceMetric(place, "incometax");
            var propertyTax = getPlaceMetric(place, "propertytax");
            var salesTax = getPlaceMetric(place, "salestax");
            var taxableSpending = income * 0.2;

            var incomeTaxCost = incomeTax ? income * (incomeTax.raw / 100) : 0;
            var propertyTaxCost = owns && propertyTax ? homePrice * (propertyTax.raw / 100) : 0;
            var salesTaxCost = salesTax ? taxableSpending * (salesTax.raw / 100) : 0;
            var left = income - incomeTaxCost - housingAnnual - propertyTaxCost - salesTaxCost;

            rows.push({ place: place, income: income, left: Math.round(left) });
        });

        rows.sort(function (a, b) { return b.left - a.left; });

        if (calcOccupationNote) {
            calcOccupationNote.hidden = !occupation;
            if (occupation) {
                calcOccupationNote.textContent = "Using " + occupation.label.toLowerCase() + " pay by state (" + occupation.source + ")" +
                    (skipped > 0 ? " — " + skipped + " state" + (skipped === 1 ? "" : "s") + " excluded, no published figure." : ".");
            }
        }

        renderMoneyLeft(rows.slice(0, 10), Boolean(occupation));
    }

    function renderMoneyLeft(rows, showIncome) {
        calcListHost.innerHTML = rows.map(function (row, index) {
            return [
                '<div class="sm-calc-row">',
                '<span class="sm-calc-rank">' + String(index + 1).padStart(2, "0") + '</span>',
                '<img class="sm-row-flag" src="' + escapeHtml(row.place.flagImageUrl) + '" alt="" width="34" height="23" loading="lazy">',
                '<span class="sm-calc-name">' + escapeHtml(row.place.name) +
                    (showIncome ? '<small> ' + formatCurrency(row.income) + ' pay</small>' : '') + '</span>',
                '<span class="sm-calc-amount">' + formatCurrency(row.left) + '</span>',
                '</div>'
            ].join("");
        }).join("");
    }

    function formatCurrency(value) {
        var sign = value < 0 ? "-$" : "$";
        return sign + Math.round(Math.abs(value)).toLocaleString("en-US");
    }

    function bindPartner() {
        if (!partnerPresetsHost) {
            return;
        }
        partnerPresetsHost.querySelectorAll(".sm-preset").forEach(function (button) {
            button.addEventListener("click", function () {
                partnerPreset = button.dataset.preset;
                partnerPresetsHost.querySelectorAll(".sm-preset").forEach(function (candidate) {
                    candidate.classList.toggle("is-active", candidate === button);
                });
                renderPartnerComparison();
            });
        });
    }

    function renderPartnerComparison() {
        if (!partnerResultHost) {
            return;
        }
        if (!partnerPreset || !presets[partnerPreset]) {
            partnerResultHost.innerHTML = "";
            return;
        }

        var partnerWeights = presets[partnerPreset];
        var eligibleRows = scored.filter(function (row) { return row.eligible; });

        var myTop10 = eligibleRows.slice(0, 10).map(function (row) { return row.place.slug; });

        var partnerRanked = eligibleRows
            .map(function (row) { return { place: row.place, score: scoreWithWeights(row.place, partnerWeights) }; })
            .sort(function (a, b) { return b.score - a.score || a.place.name.localeCompare(b.place.name); });
        var partnerTop10 = partnerRanked.slice(0, 10).map(function (row) { return row.place.slug; });

        var overlap = myTop10.filter(function (slug) { return partnerTop10.indexOf(slug) !== -1; });
        var myTopPlace = eligibleRows[0];
        var partnerTopRow = partnerRanked[0];

        var overlapHtml = overlap.length
            ? '<p class="sm-partner-overlap"><i class="fa-solid fa-heart" aria-hidden="true"></i> ' +
                overlap.length + ' of your top 10 also make theirs: ' +
                overlap.map(function (slug) { return escapeHtml(placeBySlug.get(slug).name); }).join(", ") + '.</p>'
            : '<p class="sm-partner-overlap"><i class="fa-solid fa-triangle-exclamation" aria-hidden="true"></i> No overlap between your top 10s right now — very different priorities.</p>';

        partnerResultHost.innerHTML = [
            '<div class="sm-partner-grid">',
            '<div><span class="sm-partner-label">Your top state</span><strong>' + (myTopPlace ? escapeHtml(myTopPlace.place.name) : "—") + '</strong></div>',
            '<div><span class="sm-partner-label">Their top state</span><strong>' + (partnerTopRow ? escapeHtml(partnerTopRow.place.name) : "—") + '</strong></div>',
            '</div>',
            overlapHtml
        ].join("");
    }

    function syncSliderValues() {
        metrics.forEach(function (metric) {
            var input = document.getElementById("sm-weight-" + metric.key);
            var output = document.getElementById("sm-value-" + metric.key);
            if (input) {
                input.value = weights[metric.key];
                updateRangeFill(input);
            }
            if (output) {
                output.textContent = weights[metric.key];
            }
            applyZeroState(metric.key);
        });
    }

    function syncPresetState() {
        var matched = Object.keys(presets).find(function (name) {
            return metrics.every(function (metric) {
                return weights[metric.key] === presets[name][metric.key];
            });
        });
        setActivePreset(matched || null);
    }

    function setActivePreset(name) {
        document.querySelectorAll(".sm-preset").forEach(function (button) {
            button.classList.toggle("is-active", button.dataset.preset === name);
        });
    }

    function renderAll() {
        scored = scorePlaces();
        renderRanking();
        updateTileGrid();

        var eligible = scored.filter(function (row) { return row.eligible; });
        var activeWeight = metrics.reduce(function (sum, metric) {
            return sum + (weights[metric.key] || 0);
        }, 0);
        var excludedCount = scored.length - eligible.length;
        var summary = activeWeight > 0
            ? eligible.length + " of " + scored.length + " places scored · top match " + (eligible[0] ? eligible[0].place.name : "—")
            : "Choose at least one priority";
        if (excludedCount > 0) {
            summary += " · " + excludedCount + " excluded by your filters";
        }
        resultSummary.textContent = summary;

        if (currentSlug) {
            renderDrawer(currentSlug);
        }

        renderPartnerComparison();
    }

    function renderRanking() {
        var query = searchInput.value.trim().toLowerCase();
        var filtered = scored.filter(function (row) {
            return row.eligible && (!query ||
                row.place.name.toLowerCase().includes(query) ||
                row.place.abbreviation.toLowerCase().includes(query));
        });
        var visible = query || showAll ? filtered : filtered.slice(0, 10);

        if (!visible.length) {
            rankingHost.innerHTML = query
                ? '<p class="sm-empty">No state matches that search.</p>'
                : '<p class="sm-empty">No state passes your filters. Try loosening one.</p>';
        } else {
            rankingHost.innerHTML = visible.map(function (row) {
                var strongest = strongestMetric(row);
                return [
                    '<button class="sm-state-row" type="button" data-slug="' + escapeHtml(row.place.slug) + '"',
                    ' aria-label="Open ' + escapeHtml(row.place.name) + ', rank ' + row.rank + ', ' + row.score + ' percent match">',
                    '<span class="sm-row-rank">' + String(row.rank).padStart(2, "0") + '</span>',
                    '<span class="sm-row-state">',
                    '<img class="sm-row-flag" src="' + escapeHtml(row.place.flagImageUrl) + '" alt="" width="34" height="23" loading="lazy">',
                    '<span class="sm-row-name"><strong>' + escapeHtml(row.place.name) + '</strong>',
                    '<small>' + escapeHtml(row.place.region || "United States") + ' · ' + escapeHtml(row.place.abbreviation) + '</small></span>',
                    '</span>',
                    '<span class="sm-row-fit">' + (strongest ? escapeHtml(strongest.name) : "Balanced") + '</span>',
                    '<span class="sm-row-score">',
                    '<span class="sm-score-number">' + row.score + '</span>',
                    '<span class="sm-score-track"><i class="sm-score-dot" style="left:' + row.score + '%;background:' + matchColor(row.score).fill + '"></i></span>',
                    '</span>',
                    '</button>'
                ].join("");
            }).join("");
        }

        rankingHost.querySelectorAll(".sm-state-row").forEach(function (button) {
            button.addEventListener("click", function () {
                openDetail(button.dataset.slug, true);
            });
            button.addEventListener("pointerenter", function () {
                highlightTile(button.dataset.slug, true);
            });
            button.addEventListener("pointerleave", function () {
                highlightTile(button.dataset.slug, false);
            });
        });

        showAllButton.hidden = Boolean(query) || filtered.length <= 10;
        showAllButton.innerHTML = showAll
            ? 'Show top 10 <i class="fa-solid fa-chevron-up" aria-hidden="true"></i>'
            : 'Show all states <i class="fa-solid fa-chevron-down" aria-hidden="true"></i>';

        renderSensitivity(scored.filter(function (row) { return row.eligible; }));
    }

    // "One slider from changing": finds the smallest weight increase on a single metric
    // that would swap the state just outside your top 10 with the one just inside it —
    // the concrete "raise X and Y falls out, Z gets in" scenario, computed for real
    // instead of asserted.
    function renderSensitivity(eligibleRows) {
        var panel = document.getElementById("sm-sensitivity");
        if (!panel) {
            return;
        }

        var inRank = eligibleRows[9];
        var outRank = eligibleRows[10];
        var best = null;

        if (inRank && outRank && hasActiveWeights()) {
            metrics.forEach(function (metric) {
                var currentWeight = weights[metric.key] || 0;
                var normalizedIn = inRank.normalized[metric.key];
                var normalizedOut = outRank.normalized[metric.key];
                if (normalizedIn == null || normalizedOut == null || normalizedOut <= normalizedIn) {
                    return;
                }

                for (var trialWeight = currentWeight + 5; trialWeight <= 100; trialWeight += 5) {
                    var trialWeights = Object.assign({}, weights);
                    trialWeights[metric.key] = trialWeight;
                    var scoreIn = scoreWithWeights(inRank.place, trialWeights);
                    var scoreOut = scoreWithWeights(outRank.place, trialWeights);
                    if (scoreOut > scoreIn) {
                        var delta = trialWeight - currentWeight;
                        if (!best || delta < best.delta) {
                            best = { metric: metric, delta: delta, trialWeight: trialWeight };
                        }
                        break;
                    }
                }
            });
        }

        if (!best) {
            panel.hidden = true;
            return;
        }

        panel.hidden = false;
        panel.innerHTML = '<i class="fa-solid fa-arrows-left-right" aria-hidden="true"></i> ' +
            'One slider from changing: raise <strong>' + escapeHtml(best.metric.name) + '</strong> by +' + best.delta +
            ' (to ' + best.trialWeight + ') and <strong>' + escapeHtml(outRank.place.name) + '</strong> (#11) would pass ' +
            '<strong>' + escapeHtml(inRank.place.name) + '</strong> (#10) for your top 10.';
    }

    function scoreWithWeights(place, weightSet) {
        var weightedTotal = 0;
        var usedWeight = 0;
        place.metrics.forEach(function (metricValue) {
            var weight = weightSet[metricValue.key] || 0;
            if (weight <= 0) {
                return;
            }
            weightedTotal += normalizedValue(metricValue) * weight;
            usedWeight += weight;
        });
        return usedWeight > 0 ? Math.round((weightedTotal / usedWeight) * 100) : 0;
    }

    function strongestMetric(row) {
        var candidates = metrics
            .filter(function (metric) { return weights[metric.key] > 0 && row.normalized[metric.key] != null; })
            .map(function (metric) {
                return { name: metric.name, value: row.normalized[metric.key], key: metric.key };
            })
            .sort(function (a, b) { return b.value - a.value; });
        return candidates[0] || null;
    }

    function weakestMetric(row) {
        var candidates = metrics
            .filter(function (metric) { return weights[metric.key] > 0 && row.normalized[metric.key] != null; })
            .map(function (metric) {
                return { name: metric.name, value: row.normalized[metric.key], key: metric.key };
            })
            .sort(function (a, b) { return a.value - b.value; });
        return candidates[0] || null;
    }

    function buildTileGrid() {
        var host = document.getElementById("sm-tilegrid");
        if (!host) {
            return;
        }

        host.innerHTML = places.map(function (place) {
            var position = STATE_GRID[place.abbreviation];
            if (!position) {
                return "";
            }
            return [
                '<button type="button" class="sm-tile" data-slug="' + escapeHtml(place.slug) + '"',
                ' style="grid-column:' + position[0] + ';grid-row:' + position[1] + '"',
                ' aria-label="Open ' + escapeHtml(place.name) + '">',
                '<span class="sm-tile-code">' + escapeHtml(place.abbreviation) + '</span>',
                '<span class="sm-tile-score">—</span>',
                '</button>'
            ].join("");
        }).join("");

        tileBySlug = new Map();
        host.querySelectorAll(".sm-tile").forEach(function (tile) {
            tileBySlug.set(tile.dataset.slug, tile);
            tile.addEventListener("click", function () {
                openDetail(tile.dataset.slug, true);
            });
            tile.addEventListener("pointerenter", function (event) {
                showTileTooltip(event);
                highlightRow(tile.dataset.slug, true);
            });
            tile.addEventListener("pointermove", showTileTooltip);
            tile.addEventListener("pointerleave", function () {
                hideMapTooltip();
                highlightRow(tile.dataset.slug, false);
            });
        });

        tileReady = true;
        updateTileGrid();
    }

    function highlightTile(slug, active) {
        var tile = tileBySlug.get(slug);
        if (tile) {
            tile.classList.toggle("is-linked", active);
        }
    }

    function highlightRow(slug, active) {
        var row = rankingHost.querySelector('[data-slug="' + CSS.escape(slug) + '"]');
        if (row) {
            row.classList.toggle("is-linked", active);
        }
    }

    function updateTileGrid() {
        if (!tileReady) {
            return;
        }

        var activeWeights = hasActiveWeights();
        var bySlug = new Map(scored.map(function (row) { return [row.place.slug, row]; }));
        document.querySelectorAll(".sm-tile").forEach(function (tile) {
            var row = bySlug.get(tile.dataset.slug);
            var scoreEl = tile.querySelector(".sm-tile-score");
            var excluded = Boolean(row && !row.eligible);
            var active = Boolean(row && activeWeights && !excluded);
            var color = active ? matchColor(row.score) : null;
            tile.style.background = color ? color.fill : "";
            tile.classList.toggle("is-strong", Boolean(color && color.isDark));
            tile.classList.toggle("is-excluded", excluded);
            scoreEl.textContent = excluded ? "×" : (active ? row.score : "—");
            tile.classList.toggle("is-selected", tile.dataset.slug === currentSlug);
        });
    }

    function showTileTooltip(event) {
        var slug = event.currentTarget.dataset.slug;
        var row = scored.find(function (item) { return item.place.slug === slug; });
        if (!row) {
            return;
        }

        var tooltip = document.getElementById("sm-map-tooltip");
        tooltip.innerHTML = '<strong>' + escapeHtml(row.place.name) + '</strong><span>' + row.score + '% match · #' + row.rank + '</span>';
        tooltip.style.left = (event.clientX + 14) + "px";
        tooltip.style.top = (event.clientY + 14) + "px";
        tooltip.classList.add("is-visible");
    }

    function hideMapTooltip() {
        document.getElementById("sm-map-tooltip").classList.remove("is-visible");
    }

    function openDetail(slug, updateUrl) {
        if (!placeBySlug.has(slug)) {
            return;
        }

        lastFocused = document.activeElement;
        currentSlug = slug;
        renderDrawer(slug);
        drawer.classList.add("is-open");
        drawerBackdrop.classList.add("is-open");
        drawer.setAttribute("aria-hidden", "false");
        document.body.classList.add("sm-drawer-lock");
        updateTileGrid();

        if (updateUrl) {
            updateUrlState();
        }

        window.setTimeout(function () {
            var close = document.getElementById("sm-drawer-close");
            if (close) {
                close.focus();
            }
        }, 30);
    }

    function closeDetail() {
        if (!currentSlug) {
            return;
        }

        currentSlug = null;
        drawer.classList.remove("is-open");
        drawerBackdrop.classList.remove("is-open");
        drawer.setAttribute("aria-hidden", "true");
        document.body.classList.remove("sm-drawer-lock");
        updateTileGrid();
        updateUrlState();

        if (lastFocused && typeof lastFocused.focus === "function") {
            lastFocused.focus();
        }
    }

    function renderDrawer(slug) {
        var row = scored.find(function (item) { return item.place.slug === slug; });
        if (!row) {
            return;
        }

        var place = row.place;
        var strongest = strongestMetric(row);
        var weakest = weakestMetric(row);
        var metricRows = metrics.map(function (option) {
            var metricValue = getPlaceMetric(place, option.key);
            if (!metricValue) {
                return [
                    '<div class="sm-strip">',
                    '<div class="sm-strip-head"><span class="sm-strip-name">' + escapeHtml(option.name) + '</span>',
                    '<span class="sm-strip-value">Not available</span></div>',
                    '</div>'
                ].join("");
            }

            var range = metricRanges.get(option.key);
            var rank = metricRank(metricValue);
            var lowerClass = metricValue.direction < 0 ? " is-lower" : "";
            return [
                '<div class="sm-strip' + lowerClass + '">',
                '<div class="sm-strip-head"><span class="sm-strip-name">' + escapeHtml(option.name) + '</span>',
                '<span class="sm-strip-value">' + escapeHtml(metricValue.displayValue) + '</span></div>',
                '<div class="sm-strip-track"><i class="sm-strip-marker" style="left:' + rawPosition(metricValue) + '%"></i></div>',
                '<div class="sm-strip-meta"><span>' + compactNumber(range.min) + ' — ' + compactNumber(range.max) + '</span>',
                '<span>#' + rank + ' of ' + placesWithMetric(option.key) + ' · ' + (metricValue.direction > 0 ? "higher" : "lower") + ' is better</span></div>',
                '</div>'
            ].join("");
        }).join("");

        var overview = strongest
            ? place.name + " ranks #" + row.rank + " for your current priorities. Its strongest fit is " +
                strongest.name.toLowerCase() + (weakest ? ", while " + weakest.name.toLowerCase() + " is the main trade-off in this result." : ".")
            : "Raise at least one priority slider to calculate how " + place.name + " fits your preferences.";
        if (!row.eligible) {
            overview = "This state is excluded by one of your hard filters, so it's ranked last regardless of score. " + overview;
        }

        var breakdown = metrics
            .filter(function (option) { return (weights[option.key] || 0) > 0 && row.normalized[option.key] != null; })
            .map(function (option) {
                var contribution = row.usedWeight > 0
                    ? Math.round((row.normalized[option.key] * weights[option.key] / row.usedWeight) * 100)
                    : 0;
                return { name: option.name, contribution: contribution };
            })
            .sort(function (a, b) { return b.contribution - a.contribution; });

        var breakdownSection = breakdown.length
            ? [
                '<section class="sm-drawer-section"><h3>How this ' + row.score + ' adds up</h3>',
                '<div class="sm-breakdown-bar">',
                breakdown.map(function (item, index) {
                    return '<i style="width:' + Math.max(item.contribution, 0) + '%" class="' + (index % 2 === 0 ? "is-a" : "is-b") + '" title="' + escapeHtml(item.name) + ': ' + item.contribution + ' pts"></i>';
                }).join(""),
                '</div>',
                '<ul class="sm-breakdown-list">',
                breakdown.map(function (item) {
                    return '<li><span>' + escapeHtml(item.name) + '</span><span>' + (item.contribution >= 0 ? "+" : "") + item.contribution + '</span></li>';
                }).join(""),
                '</ul>',
                '</section>'
            ].join("")
            : "";
        var countyAction = place.hasCounties
            ? '<a href="/states/' + encodeURIComponent(place.slug) + '/counties?w=' + encodeURIComponent(weightVector()) + '">Browse counties <i class="fa-solid fa-arrow-right" aria-hidden="true"></i></a>'
            : '<button type="button" disabled title="County matching is planned for phase 2"><i class="fa-solid fa-lock" aria-hidden="true"></i> Counties · coming next</button>';

        drawerContent.innerHTML = [
            '<div class="sm-drawer-hero">',
            '<button class="sm-drawer-close" id="sm-drawer-close" type="button" aria-label="Close state details"><i class="fa-solid fa-xmark" aria-hidden="true"></i></button>',
            '<div class="sm-drawer-state">',
            '<img class="sm-drawer-flag" src="' + escapeHtml(place.flagImageUrl) + '" alt="' + escapeHtml(place.name) + ' flag" width="60" height="40">',
            '<div class="sm-drawer-title"><h2 id="sm-drawer-title">' + escapeHtml(place.name) + '</h2>',
            '<p>' + escapeHtml(place.region || "United States") + ' · ' + escapeHtml(place.abbreviation) + '</p></div>',
            '<div class="sm-match-badge"><strong>' + row.score + '</strong><span>match</span></div>',
            '</div>',
            '<div class="sm-facts">',
            '<span class="sm-fact"><b>Capital</b> ' + escapeHtml(place.capital || "—") + '</span>',
            '<span class="sm-fact"><b>Population</b> ' + formatPopulation(place.population) + '</span>',
            '<span class="sm-fact"><b>Rank</b> #' + row.rank + ' of ' + scored.length + '</span>',
            '<span class="sm-fact"><b>FIPS</b> ' + escapeHtml(place.fips) + '</span>',
            '</div>',
            '</div>',
            '<div class="sm-drawer-actions">',
            '<a href="/states/' + encodeURIComponent(place.slug) + '/living">Living guide <i class="fa-solid fa-arrow-right" aria-hidden="true"></i></a>',
            '<a href="/states/' + encodeURIComponent(place.slug) + '">Symbols & facts <i class="fa-solid fa-landmark" aria-hidden="true"></i></a>',
            countyAction,
            '</div>',
            '<section class="sm-drawer-section"><h3>Why it matches</h3><p class="sm-overview">' + escapeHtml(overview) + '</p></section>',
            breakdownSection,
            '<section class="sm-drawer-section"><h3>Where it stands · nationwide</h3>' + metricRows + '</section>'
        ].join("");

        document.getElementById("sm-drawer-close").addEventListener("click", closeDetail);
    }

    function metricRank(metricValue) {
        var values = places
            .map(function (place) { return getPlaceMetric(place, metricValue.key); })
            .filter(Boolean);
        var better = values.filter(function (candidate) {
            return metricValue.direction > 0
                ? candidate.raw > metricValue.raw
                : candidate.raw < metricValue.raw;
        }).length;
        return better + 1;
    }

    function placesWithMetric(key) {
        return places.filter(function (place) { return Boolean(getPlaceMetric(place, key)); }).length;
    }

    function scheduleUrlUpdate() {
        window.clearTimeout(updateUrlTimer);
        updateUrlTimer = window.setTimeout(updateUrlState, 180);
    }

    function updateUrlState() {
        var url = new URL(window.location.href);
        url.searchParams.set("w", weightVector());

        var activeFilters = FILTER_KEYS.filter(function (key) { return filters[key]; });
        if (activeFilters.length) {
            url.searchParams.set("f", activeFilters.join(","));
        } else {
            url.searchParams.delete("f");
        }

        if (currentSlug) {
            url.searchParams.set("state", currentSlug);
        } else {
            url.searchParams.delete("state");
        }

        window.history.replaceState({}, document.title, url.pathname + "?" + url.searchParams.toString() + url.hash);
    }

    function weightVector() {
        return metrics.map(function (metric) {
            return metric.key + ":" + weights[metric.key];
        }).join(",");
    }

    async function copyShareLink() {
        updateUrlState();
        var value = window.location.href;
        var copied = false;
        try {
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(value);
                copied = true;
            } else {
                var textarea = document.createElement("textarea");
                textarea.value = value;
                textarea.style.position = "fixed";
                textarea.style.opacity = "0";
                document.body.appendChild(textarea);
                textarea.select();
                copied = document.execCommand("copy");
                textarea.remove();
            }
        } catch (_) {
            copied = false;
        }

        // A quiet status line is easy to miss, so flash the button itself too.
        var originalLabel = shareButton.innerHTML;
        if (copied) {
            shareStatus.textContent = "Link copied";
            shareButton.innerHTML = '<i class="fa-solid fa-check" aria-hidden="true"></i> Copied';
        } else {
            shareStatus.textContent = "Couldn't copy automatically — copy the URL from your browser's address bar.";
        }
        window.setTimeout(function () {
            shareStatus.textContent = "";
            shareButton.innerHTML = originalLabel;
        }, 2500);
    }

    function exportCsv() {
        var header = ["Rank", "State", "Abbreviation", "Score"].concat(metrics.map(function (metric) { return metric.name; }));
        var lines = [header.map(csvCell).join(",")];

        scored.filter(function (row) { return row.eligible; }).forEach(function (row) {
            var cells = [row.rank, row.place.name, row.place.abbreviation, row.score];
            metrics.forEach(function (metric) {
                var metricValue = getPlaceMetric(row.place, metric.key);
                cells.push(metricValue ? metricValue.displayValue : "");
            });
            lines.push(cells.map(csvCell).join(","));
        });

        var blob = new Blob(["﻿" + lines.join("\r\n")], { type: "text/csv;charset=utf-8;" });
        var url = URL.createObjectURL(blob);
        var link = document.createElement("a");
        link.href = url;
        link.download = "state-match-results.csv";
        document.body.appendChild(link);
        link.click();
        link.remove();
        window.setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
    }

    function csvCell(value) {
        var text = String(value == null ? "" : value);
        return /[",\r\n]/.test(text) ? '"' + text.replace(/"/g, '""') + '"' : text;
    }

    function formatPopulation(value) {
        var number = Number(value);
        return Number.isFinite(number) ? number.toLocaleString("en-US") : "—";
    }

    function compactNumber(value) {
        if (!Number.isFinite(value)) {
            return "—";
        }
        if (Math.abs(value) >= 1000) {
            return new Intl.NumberFormat("en-US", { notation: "compact", maximumFractionDigits: 1 }).format(value);
        }
        return Number.isInteger(value) ? String(value) : value.toFixed(1);
    }

    function clampNumber(value, min, max) {
        var numeric = Number(value);
        return Math.min(max, Math.max(min, Number.isFinite(numeric) ? numeric : min));
    }

    function escapeHtml(value) {
        return String(value == null ? "" : value).replace(/[&<>"']/g, function (character) {
            return {
                "&": "&amp;",
                "<": "&lt;",
                ">": "&gt;",
                '"': "&quot;",
                "'": "&#039;"
            }[character];
        });
    }
})();
