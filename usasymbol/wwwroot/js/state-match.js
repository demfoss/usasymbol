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
    var coupleLink = document.getElementById("sm-couple-link");
    var sensitivityPanel = document.getElementById("sm-sensitivity");
    var placeByAbbr = new Map(places.map(function (place) { return [place.abbreviation, place]; }));

    var core = window.StateMatchCore;
    var scorer = core.createScorer(places, metrics);
    var facts = core.createFacts(places, metrics, scorer);
    // Places with no metric data (DC today) can't be scored; they show as "n/a" on the map.
    var scorablePlaces = places.filter(facts.hasData);
    var noDataPlaces = places.filter(function (place) { return !facts.hasData(place); });
    var presets = {};
    Object.keys(core.PRESET_VALUES).forEach(function (name) {
        presets[name] = scorer.preset(name);
    });

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

    var STATE_GRID = core.STATE_GRID;

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
    var home = readHome();
    var money = { data: null, paycheck: null, loading: null, failed: false, salary: readSalary() };
    var sensitivitySuggestion = null;

    var SLIDER_GROUPS = [
        { name: "Economy", icon: "fa-solid fa-sack-dollar", keys: ["cost", "income", "jobs", "housing"] },
        { name: "Community", icon: "fa-solid fa-people-roof", keys: ["safety", "health", "education"] },
        { name: "Climate & taxes", icon: "fa-solid fa-sun", keys: ["warmth", "incometax", "propertytax", "salestax"] }
    ];

    buildSliders();
    bindControls();
    bindFilters();
    bindQuiz();
    bindHome();
    renderAll();
    buildTileGrid();

    var initialState = new URL(window.location.href).searchParams.get("state");
    if (initialState && placeBySlug.has(initialState)) {
        openDetail(initialState, false);
    }

    function defaults() {
        return scorer.defaults();
    }

    function readHome() {
        var fromUrl = (new URL(window.location.href).searchParams.get("home") || "").toUpperCase();
        if (fromUrl && placeByAbbr.has(fromUrl)) {
            return fromUrl;
        }
        try {
            var saved = window.localStorage.getItem("sm-home");
            return saved && placeByAbbr.has(saved) ? saved : "";
        } catch (_) {
            return "";
        }
    }

    function readSalary() {
        try {
            var saved = Number(window.localStorage.getItem("sm-salary"));
            return Number.isFinite(saved) && saved > 0 ? saved : 75000;
        } catch (_) {
            return 75000;
        }
    }

    function homePlace() {
        return home ? placeByAbbr.get(home) || null : null;
    }

    /** The comparison point for a given place: your state, unless it IS your state. */
    function baselineFor(place) {
        var mine = homePlace();
        return mine && mine !== place && facts.hasData(mine) ? mine : null;
    }

    function setHome(abbr) {
        home = abbr && placeByAbbr.has(abbr) ? abbr : "";
        try {
            if (home) {
                window.localStorage.setItem("sm-home", home);
            } else {
                window.localStorage.removeItem("sm-home");
            }
        } catch (_) { /* per-viewer convenience only */ }
        syncHomeSelects();
        renderAll();
        updateMoney();
        scheduleUrlUpdate();
    }

    function syncHomeSelects() {
        document.querySelectorAll("[data-home-select]").forEach(function (select) {
            select.value = home;
        });
    }

    function bindHome() {
        document.addEventListener("change", function (event) {
            if (event.target.matches && event.target.matches("[data-home-select]")) {
                setHome(event.target.value);
            }
        });
        syncHomeSelects();
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
        return core.matchColor(score);
    }

    function scorePlaces() {
        var hasAnyWeight = hasActiveWeights();
        var rows = scorablePlaces.map(function (place) {
            var weightedTotal = 0;
            var usedWeight = 0;
            var normalized = {};

            place.metrics.forEach(function (metricValue) {
                var weight = core.effectiveWeight(weights[metricValue.key]);
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
        var grouped = {};
        SLIDER_GROUPS.forEach(function (group) { group.keys.forEach(function (key) { grouped[key] = true; }); });
        var groups = SLIDER_GROUPS.map(function (group) {
            return { name: group.name, icon: group.icon, metrics: group.keys.map(function (key) { return metricByKey.get(key); }).filter(Boolean) };
        });
        var leftover = metrics.filter(function (metric) { return !grouped[metric.key]; });
        if (leftover.length) {
            groups.push({ name: "Other", icon: "fa-solid fa-ellipsis", metrics: leftover });
        }

        sliderHost.innerHTML = groups.filter(function (group) { return group.metrics.length; }).map(function (group) {
            return '<div class="sm-slider-group" role="group" aria-label="' + escapeHtml(group.name) + '">' +
                '<p class="sm-slider-group-title"><i class="' + escapeHtml(group.icon) + '" aria-hidden="true"></i>' + escapeHtml(group.name) + '</p>' +
                group.metrics.map(sliderHtml).join("") +
                '</div>';
        }).join("");

        function sliderHtml(metric) {
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
        }

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
        controlsBody.querySelectorAll(".sm-preset").forEach(function (button) {
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

        if (sensitivityPanel) {
            sensitivityPanel.addEventListener("click", function (event) {
                if (!event.target.closest(".sm-try") || !sensitivitySuggestion) {
                    return;
                }
                weights[sensitivitySuggestion.key] = sensitivitySuggestion.weight;
                syncSliderValues();
                setActivePreset(null);
                renderAll();
                scheduleUrlUpdate();
            });
        }

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
        controlsBody.querySelectorAll(".sm-preset").forEach(function (button) {
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
        if (noDataPlaces.length) {
            summary += " · " + noDataPlaces.map(function (place) { return place.abbreviation; }).join(", ") + ": not enough data";
        }
        resultSummary.textContent = summary;

        if (currentSlug) {
            renderDrawer(currentSlug);
        }

        // Carry the current sliders over as "your" side of the couples tool.
        if (coupleLink) {
            coupleLink.href = "/tools/where-should-we-move?a=" + encodeURIComponent(weightVector());
        }
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
            var mine = homePlace();
            rankingHost.innerHTML = visible.map(function (row) {
                var list = facts.prosAndCons(row.place, weights, baselineFor(row.place));
                var chips = list.pros.slice(0, 2).map(function (item) { return factChip(item, true); })
                    .concat(list.cons.slice(0, 1).map(function (item) { return factChip(item, false); }))
                    .join("");
                var isHome = mine === row.place;
                return [
                    '<button class="sm-state-row" type="button" data-slug="' + escapeHtml(row.place.slug) + '"',
                    ' aria-label="Open ' + escapeHtml(row.place.name) + ', rank ' + row.rank + ', ' + row.score + ' percent match">',
                    '<span class="sm-row-rank">' + String(row.rank).padStart(2, "0") + '</span>',
                    '<span class="sm-row-state">',
                    '<img class="sm-row-flag" src="' + escapeHtml(row.place.flagImageUrl) + '" alt="" width="34" height="23" loading="lazy">',
                    '<span class="sm-row-name"><strong>' + escapeHtml(row.place.name) + '</strong>',
                    '<small>' + escapeHtml(row.place.region || "United States") + ' · ' + escapeHtml(row.place.abbreviation) +
                        (isHome ? ' <em class="sm-home-tag">Your state</em>' : '') + '</small></span>',
                    '</span>',
                    '<span class="sm-row-score">',
                    '<span class="sm-score-track"><i class="sm-score-fill" style="width:' + row.score + '%"></i></span>',
                    '<span class="sm-score-number">' + row.score + '</span>',
                    '</span>',
                    chips ? '<span class="sm-row-chips">' + chips + '</span>' : '',
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

    function factChip(item, isPro) {
        return '<span class="sm-chip ' + (isPro ? "is-pro" : "is-con") + '">' +
            // Label on the icon, not hidden text, so copying the list doesn't pick up "Plus:".
            '<i class="fa-solid ' + (isPro ? "fa-check" : "fa-minus") + '" role="img" aria-label="' + (isPro ? "Plus" : "Minus") + '"></i>' +
            escapeHtml(item.chip) + '</span>';
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
            sensitivitySuggestion = null;
            panel.hidden = true;
            return;
        }

        sensitivitySuggestion = { key: best.metric.key, weight: best.trialWeight };

        panel.hidden = false;
        // One text element + the button, so the flex row doesn't split the sentence into pieces.
        panel.innerHTML = '<i class="fa-solid fa-arrows-left-right" aria-hidden="true"></i>' +
            '<span class="sm-sensitivity-text"><strong>One slider from changing:</strong> raise ' + escapeHtml(best.metric.name) + ' by +' + best.delta +
            ' (to ' + best.trialWeight + ') and <strong>' + escapeHtml(outRank.place.name) + '</strong> (#11) passes ' +
            '<strong>' + escapeHtml(inRank.place.name) + '</strong> (#10) in your top 10.</span>' +
            '<button type="button" class="sm-try">Try it</button>';
    }

    function scoreWithWeights(place, weightSet) {
        var weightedTotal = 0;
        var usedWeight = 0;
        place.metrics.forEach(function (metricValue) {
            var weight = core.effectiveWeight(weightSet[metricValue.key]);
            if (weight <= 0) {
                return;
            }
            weightedTotal += normalizedValue(metricValue) * weight;
            usedWeight += weight;
        });
        return usedWeight > 0 ? Math.round((weightedTotal / usedWeight) * 100) : 0;
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
            if (!row) {
                tile.classList.add("is-nodata");
                tile.style.background = "";
                scoreEl.textContent = "n/a";
                tile.setAttribute("aria-label", placeBySlug.get(tile.dataset.slug).name + ": not enough data to score");
                tile.title = "Not enough data to score";
                return;
            }
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
        if (!placeBySlug.has(slug) || !facts.hasData(placeBySlug.get(slug))) {
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
        var mine = homePlace();
        var base = baselineFor(place);
        var baseName = base ? base.name : "the US average";
        var list = facts.prosAndCons(place, weights, base);
        var eligibleCount = scored.filter(function (item) { return item.eligible; }).length;
        var openSections = {};
        drawerContent.querySelectorAll("details[data-section]").forEach(function (section) {
            openSections[section.dataset.section] = section.open;
        });

        var whyCards = list.pros.slice(0, 4).map(function (item) {
            return '<div class="sm-why-card">' +
                '<strong>' + escapeHtml(item.big) + '</strong>' +
                '<span>' + escapeHtml(item.label) + '</span>' +
                '<small>' + escapeHtml(item.sub) + '</small>' +
                '</div>';
        }).join("");
        var whySection = whyCards
            ? '<div class="sm-why-grid">' + whyCards + '</div>'
            : '<p class="sm-why-empty">Nothing here clearly beats ' + escapeHtml(baseName) + ' on what you weighted. ' +
                escapeHtml(place.name) + ' ranks on balance, not standout strengths.</p>';

        var con = list.cons[0];
        var tradeoff = con
            ? '<div class="sm-tradeoff">' +
                '<strong>' + escapeHtml(con.big) + '</strong>' +
                '<p><b>The trade-off:</b> ' + escapeHtml(con.label) + '. <span>' + escapeHtml(con.sub) + '</span></p>' +
              '</div>'
            : '<div class="sm-tradeoff is-none"><i class="fa-solid fa-circle-check" aria-hidden="true"></i>' +
                '<p>No clear trade-off against ' + escapeHtml(baseName) + ' on anything you weighted.</p></div>';

        var excludedNote = row.eligible ? "" :
            '<p class="sm-drawer-note"><i class="fa-solid fa-filter" aria-hidden="true"></i> Excluded by one of your must-haves, so it ranks last whatever its score.</p>';

        var breakdown = metrics
            .filter(function (option) { return (weights[option.key] || 0) > 0 && row.normalized[option.key] != null; })
            .map(function (option) {
                var contribution = row.usedWeight > 0
                    ? Math.round((row.normalized[option.key] * core.effectiveWeight(weights[option.key]) / row.usedWeight) * 100)
                    : 0;
                return { name: option.name, contribution: contribution };
            })
            .sort(function (a, b) { return b.contribution - a.contribution; });

        var breakdownHtml = breakdown.length
            ? '<div class="sm-breakdown-bar">' +
                breakdown.map(function (item, index) {
                    return '<i style="width:' + Math.max(item.contribution, 0) + '%" class="' + (index % 2 === 0 ? "is-a" : "is-b") + '" title="' + escapeHtml(item.name) + ': ' + item.contribution + ' pts"></i>';
                }).join("") +
                '</div><ul class="sm-breakdown-list">' +
                breakdown.map(function (item) {
                    return '<li><span>' + escapeHtml(item.name) + '</span><span>+' + item.contribution + '</span></li>';
                }).join("") + '</ul>'
            : '<p class="sm-overview">Raise at least one priority to see how the score is built.</p>';

        var strips = metrics.map(function (option) {
            var metricValue = getPlaceMetric(place, option.key);
            if (!metricValue) {
                return '<div class="sm-strip"><div class="sm-strip-head"><span class="sm-strip-name">' + escapeHtml(option.name) + '</span>' +
                    '<span class="sm-strip-value">Not available</span></div></div>';
            }
            var range = metricRanges.get(option.key);
            return '<div class="sm-strip' + (metricValue.direction < 0 ? " is-lower" : "") + '">' +
                '<div class="sm-strip-head"><span class="sm-strip-name">' + escapeHtml(option.name) + '</span>' +
                '<span class="sm-strip-value">' + escapeHtml(metricValue.displayValue) + '</span></div>' +
                '<div class="sm-strip-track"><i class="sm-strip-marker" style="left:' + rawPosition(metricValue) + '%"></i></div>' +
                '<div class="sm-strip-meta"><span>' + compactNumber(range.min) + ' — ' + compactNumber(range.max) + '</span>' +
                '<span>#' + metricRank(metricValue) + ' of ' + placesWithMetric(option.key) + ' · ' + (metricValue.direction > 0 ? "higher" : "lower") + ' is better</span></div>' +
                '</div>';
        }).join("");

        var compareOptions = '<option value="">US average</option>' + scorablePlaces.map(function (candidate) {
            return '<option value="' + escapeHtml(candidate.abbreviation) + '"' + (candidate.abbreviation === home ? " selected" : "") + '>' +
                escapeHtml(candidate.name) + ' (my state)</option>';
        }).join("");

        var compareAction = base
            ? '<a class="is-primary" href="/compare/' + [place.slug, base.slug].sort().join("-vs-") + '">' +
                escapeHtml(place.name) + ' vs ' + escapeHtml(base.name) + ' <i class="fa-solid fa-arrow-right" aria-hidden="true"></i></a>'
            : '<a href="/compare-states">Compare two states <i class="fa-solid fa-arrow-right" aria-hidden="true"></i></a>';
        var countyAction = place.hasCounties
            ? '<a href="/states/' + encodeURIComponent(place.slug) + '/counties?w=' + encodeURIComponent(weightVector()) + '">Browse counties <i class="fa-solid fa-arrow-right" aria-hidden="true"></i></a>'
            : '';

        drawerContent.innerHTML = [
            '<div class="sm-drawer-hero">',
            '<button class="sm-drawer-close" id="sm-drawer-close" type="button" aria-label="Close state details"><i class="fa-solid fa-xmark" aria-hidden="true"></i></button>',
            '<div class="sm-drawer-state">',
            '<img class="sm-drawer-flag" src="' + escapeHtml(place.flagImageUrl) + '" alt="' + escapeHtml(place.name) + ' flag" width="64" height="42">',
            '<div class="sm-drawer-title"><h2 id="sm-drawer-title">' + escapeHtml(place.name) + '</h2>',
            '<p>' + escapeHtml(place.region || "United States") + (place.capital ? ' · Capital ' + escapeHtml(place.capital) : '') +
                (place.population ? ' · Pop. ' + formatPopulation(place.population) : '') + '</p></div>',
            '<div class="sm-match-badge"><strong>' + row.score + '</strong><span>#' + row.rank + ' of ' + eligibleCount + '</span></div>',
            '</div>',
            '<label class="sm-drawer-compare"><span>Compared with</span><select data-home-select>' + compareOptions + '</select></label>',
            mine === place ? '<p class="sm-drawer-note"><i class="fa-solid fa-house" aria-hidden="true"></i> This is your state, so facts compare it with the US average.</p>' : '',
            '</div>',
            excludedNote,
            '<section class="sm-drawer-section"><h3>Why you\'d like it</h3>' + whySection + '</section>',
            '<section class="sm-drawer-section sm-drawer-tight">' + tradeoff + '</section>',
            '<section class="sm-drawer-section sm-drawer-tight">' + moneyCheckHtml(place, base) + '</section>',
            '<details class="sm-drawer-more" data-section="score"' + (openSections.score ? " open" : "") + '>',
            '<summary>How this ' + row.score + ' adds up <i class="fa-solid fa-chevron-down" aria-hidden="true"></i></summary>',
            '<div class="sm-drawer-more-body">' + breakdownHtml + '</div></details>',
            '<details class="sm-drawer-more" data-section="stands"' + (openSections.stands ? " open" : "") + '>',
            '<summary>Where it stands nationwide <i class="fa-solid fa-chevron-down" aria-hidden="true"></i></summary>',
            '<div class="sm-drawer-more-body">' + strips + '</div></details>',
            '<div class="sm-drawer-actions">',
            compareAction,
            '<a href="/states/' + encodeURIComponent(place.slug) + '/living">Living guide <i class="fa-solid fa-arrow-right" aria-hidden="true"></i></a>',
            '<a href="/states/' + encodeURIComponent(place.slug) + '">Symbols &amp; facts <i class="fa-solid fa-landmark" aria-hidden="true"></i></a>',
            countyAction,
            '</div>'
        ].join("");

        document.getElementById("sm-drawer-close").addEventListener("click", closeDetail);
        bindMoneyCheck();
        updateMoney();
    }

    // ---------- Money check ----------
    // What a salary leaves after federal + state tax, typical rent, and sales tax — here vs
    // your state (or vs the average state). Data loads on first use; the math is shared with
    // /tools/take-home-pay-by-state through state-match-core.js.

    function moneyCheckHtml(place, base) {
        return '<div class="sm-money">' +
            '<h3>Money check</h3>' +
            '<div class="sm-money-line">' +
                '<label for="sm-money-salary">On a salary of</label>' +
                '<span class="sm-money-input"><b aria-hidden="true">$</b><input id="sm-money-salary" type="text" inputmode="numeric" autocomplete="off" value="' + Math.round(money.salary).toLocaleString("en-US") + '"></span>' +
                '<span>you\'d keep</span>' +
                '<output id="sm-money-result" class="sm-money-result" for="sm-money-salary">…</output>' +
            '</div>' +
            '<p class="sm-money-sub" id="sm-money-sub">' + (base ? "a year vs " + escapeHtml(base.name) : "a year vs the average state") + '</p>' +
            '<a class="sm-money-link" id="sm-money-link" href="/tools/take-home-pay-by-state">Full take-home breakdown <i class="fa-solid fa-arrow-right" aria-hidden="true"></i></a>' +
            '</div>';
    }

    function bindMoneyCheck() {
        var input = document.getElementById("sm-money-salary");
        if (!input) {
            return;
        }
        input.addEventListener("input", function () {
            var value = Math.round(Number(input.value.replace(/[^0-9.]/g, "")) || 0);
            money.salary = Math.min(10000000, value);
            try { window.localStorage.setItem("sm-salary", String(money.salary)); } catch (_) { /* optional */ }
            updateMoney();
        });
        input.addEventListener("blur", function () {
            input.value = Math.round(money.salary).toLocaleString("en-US");
        });
        input.addEventListener("focus", function () { input.select(); });
        ensureMoneyData();
    }

    function ensureMoneyData() {
        if (money.data || money.loading || money.failed) {
            return;
        }
        money.loading = window.fetch("/api/state-match/money-data")
            .then(function (response) {
                if (!response.ok) {
                    throw new Error("HTTP " + response.status);
                }
                return response.json();
            })
            .then(function (data) {
                money.data = data;
                money.paycheck = core.createPaycheck(data.taxes);
                updateMoney();
            })
            .catch(function () {
                money.failed = true;
                updateMoney();
            });
    }

    function leftOver(place, gross) {
        var cost = money.data.costs[place.abbreviation];
        var stateTax = money.paycheck.stateIncomeTax(place.abbreviation, gross, "single");
        if (!cost || cost.rent == null || stateTax == null) {
            return null;
        }
        var net = gross - money.paycheck.federalTax(gross, "single") - money.paycheck.payrollTax(gross, "single") - stateTax;
        var sales = Math.max(0, net) * 0.2 * (cost.sales || 0) / 100;
        return net - cost.rent * 12 - sales;
    }

    function updateMoney() {
        var result = document.getElementById("sm-money-result");
        var sub = document.getElementById("sm-money-sub");
        var link = document.getElementById("sm-money-link");
        var place = currentSlug ? placeBySlug.get(currentSlug) : null;
        if (!result || !place) {
            return;
        }
        if (money.failed) {
            result.textContent = "—";
            sub.textContent = "Couldn't load tax data. Try the full calculator instead.";
            return;
        }
        if (!money.data) {
            result.textContent = "…";
            return;
        }

        var base = baselineFor(place);
        var mine = leftOver(place, money.salary);
        var baseValue;
        if (base) {
            baseValue = leftOver(base, money.salary);
        } else {
            var all = scorablePlaces.map(function (candidate) { return leftOver(candidate, money.salary); })
                .filter(function (value) { return value != null; });
            baseValue = all.length ? all.reduce(function (sum, value) { return sum + value; }, 0) / all.length : null;
        }

        link.href = "/tools/take-home-pay-by-state?income=" + Math.round(money.salary) + (home ? "&home=" + encodeURIComponent(home) : "");

        if (mine == null || baseValue == null) {
            result.textContent = "—";
            result.className = "sm-money-result";
            sub.textContent = "No rent figure on file for this comparison.";
            return;
        }

        var diff = mine - baseValue;
        var rounded = Math.round(Math.abs(diff) / 10) * 10;
        result.textContent = (diff >= 0 ? "+" : "−") + "$" + rounded.toLocaleString("en-US") + "/yr";
        result.className = "sm-money-result " + (diff >= 0 ? "is-up" : "is-down");
        sub.textContent = (diff >= 0 ? "more" : "less") + " than in " + (base ? base.name : "the average state") +
            ", after federal and state tax, typical rent, and sales tax. About $" +
            Math.round(Math.max(0, mine) / 12).toLocaleString("en-US") + " a month left here.";
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

        if (home) {
            url.searchParams.set("home", home);
        } else {
            url.searchParams.delete("home");
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
