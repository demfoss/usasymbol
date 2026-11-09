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
    var topHost = document.getElementById("sm-top");
    var darkHorseEl = document.getElementById("sm-dark-horse");
    var shortlistEl = document.getElementById("sm-shortlist");
    var emptyEl = document.getElementById("sm-empty");
    var fineToggle = document.getElementById("sm-fine-toggle");
    var fineLabel = document.getElementById("sm-fine-label");
    var tabList = document.getElementById("sm-tab-list");
    var tabMap = document.getElementById("sm-tab-map");
    var viewList = document.getElementById("sm-view-list");
    var viewMap = document.getElementById("sm-view-map");
    var compareLabel = document.getElementById("sm-compare-label");
    var controlsSummary = document.getElementById("sm-controls-summary");
    var homeHint = document.getElementById("sm-home-hint");
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

    // Nine forced-choice pairs covering the core starter metrics. Additional metrics remain
    // opt-in sliders so the quick quiz stays short. Weight math in
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
    var openTop = null;
    var saved = [];
    var prevRanks = null;
    var currentRanks = null;
    var moveTimer = null;
    var activePresetName = null;
    var view = "list";
    // Keep identical to StateMatchPhrases.cs so server-rendered card text matches this.
    var PHRASES = {
        cost: ["low everyday costs", "high everyday costs"],
        income: ["high incomes", "lower incomes"],
        jobs: ["a strong job market", "a tighter job market"],
        housing: ["affordable homes", "pricey homes"],
        safety: ["low crime", "higher crime"],
        health: ["good healthcare", "weaker healthcare"],
        education: ["strong schools", "weaker schools"],
        warmth: ["a warm climate", "cold winters"],
        incometax: ["low income tax", "high income tax"],
        propertytax: ["low property tax", "high property tax"],
        salestax: ["low sales tax", "high sales tax"],
        jobgrowth: ["fast job growth", "slow job growth"],
        childcare: ["cheaper childcare", "costly childcare"],
        commute: ["short commutes", "long commutes"],
        rent: ["low rent", "high rent"],
        lifeexpectancy: ["long life expectancy", "shorter life expectancy"],
        sunny: ["lots of sunshine", "fewer sunny days"]
    };

    var PRESET_NAMES = { balanced: "Balanced", budget: "Budget", career: "Career", family: "Family", warm: "Warmth" };
    var LEVELS = [
        { label: "Skip", value: 0 },
        { label: "Some", value: 25 },
        { label: "High", value: 60 },
        { label: "Top", value: 100 }
    ];
    var FILTER_LABELS = { noIncomeTax: "No state income tax", popMillion: "Population over 1 million", warmClimate: "Warm climate" };
    var BOOKMARK = '<svg width="16" height="16" viewBox="0 0 16 16" aria-hidden="true"><path d="M4 2.5h8v11l-4-3-4 3z" fill="FILL" stroke="currentColor" stroke-width="1.4" stroke-linejoin="round"></path></svg>';

    var SLIDER_GROUPS = [
        { name: "Economy", icon: "fa-solid fa-sack-dollar", keys: ["cost", "income", "jobs", "housing"] },
        { name: "Community", icon: "fa-solid fa-people-roof", keys: ["safety", "health", "education"] },
        { name: "Climate & taxes", icon: "fa-solid fa-sun", keys: ["warmth", "incometax", "propertytax", "salestax"] },
        { name: "Work & daily life", icon: "fa-solid fa-briefcase", keys: ["jobgrowth", "rent", "childcare", "commute"], optional: true },
        { name: "Wellbeing", icon: "fa-solid fa-heart-pulse", keys: ["lifeexpectancy", "sunny"], optional: true }
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
        var url = new URL(window.location.href);

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

        var encoded = url.searchParams.get("w");
        if (!encoded) {
            var focusedKey = url.searchParams.get("focus");
            if (focusedKey && metricByKey.has(focusedKey)) {
                metrics.forEach(function (metric) { result[metric.key] = 0; });
                result[focusedKey] = 100;
            }
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

    function levelIndex(value) {
        var best = 0;
        var bestDistance = Infinity;
        LEVELS.forEach(function (level, index) {
            var distance = Math.abs(level.value - value);
            if (distance < bestDistance) {
                best = index;
                bestDistance = distance;
            }
        });
        return best;
    }

    function buildSliders() {
        var grouped = {};
        SLIDER_GROUPS.forEach(function (group) { group.keys.forEach(function (key) { grouped[key] = true; }); });
        var groups = SLIDER_GROUPS.map(function (group) {
            return {
                name: group.name,
                optional: Boolean(group.optional),
                metrics: group.keys.map(function (key) { return metricByKey.get(key); }).filter(Boolean)
            };
        });
        var leftover = metrics.filter(function (metric) { return !grouped[metric.key]; });
        if (leftover.length) {
            groups.push({ name: "Other", optional: true, metrics: leftover });
        }

        var visibleGroups = groups.filter(function (group) { return group.metrics.length && !group.optional; });
        var optionalGroups = groups.filter(function (group) { return group.metrics.length && group.optional; });

        function groupHtml(group) {
            return '<div class="smx-level-group" role="group" aria-label="' + escapeHtml(group.name) + '">' +
                '<p class="smx-level-title">' + escapeHtml(group.name) + '</p>' +
                group.metrics.map(levelHtml).join("") + '</div>';
        }

        function levelHtml(metric) {
            var safeKey = escapeHtml(metric.key);
            return '<div class="smx-level">' +
                '<div class="smx-level-head"><span class="smx-level-name" id="sm-level-name-' + safeKey + '">' + escapeHtml(metric.name) + '</span>' +
                '<span class="smx-level-hint">' + escapeHtml(metric.hint) + '</span></div>' +
                '<div class="smx-seg" role="radiogroup" aria-labelledby="sm-level-name-' + safeKey + '" data-key="' + safeKey + '">' +
                LEVELS.map(function (level, index) {
                    return '<button type="button" role="radio" class="smx-seg-btn" aria-checked="false" data-index="' + index + '">' + level.label + '</button>';
                }).join("") + '</div></div>';
        }

        sliderHost.innerHTML = visibleGroups.map(groupHtml).join("") +
            (optionalGroups.length
                ? '<details class="smx-more"><summary>More priorities</summary>' + optionalGroups.map(groupHtml).join("") + '</details>'
                : '');

        sliderHost.addEventListener("click", function (event) {
            var button = event.target.closest(".smx-seg-btn");
            if (!button) {
                return;
            }
            var key = button.parentElement.dataset.key;
            trackMoves();
            // Snap every priority to its nearest level first, so what the buttons show is exactly what is scored.
            Object.keys(weights).forEach(function (name) { weights[name] = LEVELS[levelIndex(weights[name] || 0)].value; });
            weights[key] = LEVELS[Number(button.dataset.index)].value;
            syncSliderValues();
            setActivePreset(null);
            track("sm_level_change", { metric: key, level: LEVELS[Number(button.dataset.index)].label });
            renderAll();
            scheduleUrlUpdate();
        });

        syncSliderValues();
        syncPresetState();
    }

    function bindControls() {
        controlsBody.querySelectorAll(".sm-preset").forEach(function (button) {
            button.addEventListener("click", function () {
                var name = button.dataset.preset;
                if (!presets[name]) {
                    return;
                }
                trackMoves();
                weights = Object.assign({}, presets[name]);
                syncSliderValues();
                setActivePreset(name);
                track("sm_preset", { preset: name });
                renderAll();
                scheduleUrlUpdate();
            });
        });

        resetButton.addEventListener("click", function () {
            trackMoves();
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
                trackMoves();
                weights[sensitivitySuggestion.key] = sensitivitySuggestion.weight;
                track("sm_nudge_try", { metric: sensitivitySuggestion.key });
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

        var sheetBackdrop = document.getElementById("sm-sheet-backdrop");
        function setSheet(open) {
            controlsBody.classList.toggle("is-open", open);
            sheetBackdrop.classList.toggle("is-open", open);
            mobileToggle.setAttribute("aria-expanded", String(open));
            document.body.classList.toggle("sm-drawer-lock", open);
            if (open) {
                track("sm_sheet_open");
            } else {
                mobileToggle.focus({ preventScroll: true });
            }
        }
        mobileToggle.addEventListener("click", function () { setSheet(!controlsBody.classList.contains("is-open")); });
        sheetBackdrop.addEventListener("click", function () { setSheet(false); });
        document.getElementById("sm-sheet-close").addEventListener("click", function () { setSheet(false); });
        document.getElementById("sm-sheet-done").addEventListener("click", function () { setSheet(false); });
        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape" && controlsBody.classList.contains("is-open")) {
                setSheet(false);
            }
        });

        fineToggle.addEventListener("click", function () {
            var open = sliderHost.hidden;
            sliderHost.hidden = !open;
            fineToggle.setAttribute("aria-expanded", String(open));
            fineLabel.textContent = open ? "Hide" : (activePresetName ? "Show" : "Adjusted");
            if (open) {
                track("sm_fine_tune_open");
            }
        });

        tabList.addEventListener("click", function () { setView("list"); });
        tabMap.addEventListener("click", function () { setView("map"); });

        // Save buttons (cards + rows) and shortlist chips are rendered by JS, so delegate.
        document.addEventListener("click", function (event) {
            var save = event.target.closest("[data-save]");
            if (save) {
                event.preventDefault();
                toggleSaved(save.dataset.save, save);
                return;
            }
            var open = event.target.closest("[data-open]");
            if (open) {
                openDetail(open.dataset.open, true);
                return;
            }
            var clear = event.target.closest("[data-clear-filter]");
            if (clear) {
                filters[clear.dataset.clearFilter] = false;
                document.querySelectorAll('.sm-filter-toggle input[data-filter="' + clear.dataset.clearFilter + '"]').forEach(function (input) { input.checked = false; });
                renderAll();
                scheduleUrlUpdate();
            }
        });
    }

    function bindFilters() {
        document.querySelectorAll(".sm-filter-toggle input").forEach(function (input) {
            var key = input.dataset.filter;
            input.checked = Boolean(filters[key]);
            input.addEventListener("change", function () {
                trackMoves();
                filters[key] = input.checked;
                track("sm_filter", { filter: key, on: input.checked });
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
        track("sm_quiz_start");
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

        trackMoves();
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
        sliderHost.querySelectorAll(".smx-seg").forEach(function (group) {
            var active = levelIndex(weights[group.dataset.key] || 0);
            group.querySelectorAll(".smx-seg-btn").forEach(function (button) {
                button.setAttribute("aria-checked", String(Number(button.dataset.index) === active));
            });
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
        activePresetName = name;
        controlsBody.querySelectorAll(".sm-preset").forEach(function (button) {
            var on = button.dataset.preset === name;
            button.classList.toggle("is-active", on);
            button.setAttribute("aria-pressed", String(on));
        });
    }

    function track(name, data) {
        try {
            window.dataLayer = window.dataLayer || [];
            window.dataLayer.push(Object.assign({ event: name }, data || {}));
        } catch (_) { /* analytics is optional */ }
    }

    /** Remember the current order so the next render can show how far each state moved. */
    function trackMoves() {
        prevRanks = currentRanks;
        window.clearTimeout(moveTimer);
        moveTimer = window.setTimeout(function () {
            prevRanks = null;
            renderAll();
        }, 2600);
    }

    function moveHtml(slug) {
        if (!prevRanks || !prevRanks[slug] || !currentRanks || !currentRanks[slug]) {
            return "";
        }
        var diff = prevRanks[slug] - currentRanks[slug];
        if (diff === 0) {
            return "";
        }
        return '<span class="smx-move ' + (diff > 0 ? "is-up" : "is-down") + '">' + (diff > 0 ? "↑" : "↓") + Math.abs(diff) + '</span>';
    }

    function lowerFirst(text) {
        return /^[A-Z][a-z]/.test(text) ? text.charAt(0).toLowerCase() + text.slice(1) : text;
    }

    /**
     * One short sentence: up to two strengths, then the main trade-off. With no home state it
     * ranks metrics by their 0–1 score (same rule as StateMatchService); with one it uses the
     * real gap to that state.
     */
    function summaryText(row, list) {
        var strong = [];
        var weak = null;
        if (baselineFor(row.place)) {
            strong = list.pros.slice(0, 2).map(function (item) { return item.key; });
            weak = list.cons[0] ? list.cons[0].key : null;
        } else {
            var items = [];
            row.place.metrics.forEach(function (metric) {
                if ((weights[metric.key] || 0) > 0 && row.normalized[metric.key] != null && metricRanges.get(metric.key).max > metricRanges.get(metric.key).min) {
                    items.push({ key: metric.key, value: row.normalized[metric.key] });
                }
            });
            strong = items.slice().sort(function (x, y) { return y.value - x.value; })
                .filter(function (item) { return item.value >= 0.6; }).slice(0, 2).map(function (item) { return item.key; });
            var low = items.slice().sort(function (x, y) { return x.value - y.value; })[0];
            weak = low && low.value <= 0.4 ? low.key : null;
        }
        var good = strong.filter(function (key) { return PHRASES[key]; }).map(function (key) { return PHRASES[key][0]; });
        var bad = weak && PHRASES[weak] ? PHRASES[weak][1] : null;
        if (!good.length) {
            return bad ? "A balanced match across your priorities, though " + bad + "." : "A balanced match across your priorities.";
        }
        var text = good.join(" and ");
        return text.charAt(0).toUpperCase() + text.slice(1) + (bad ? ", but " + bad : "") + ".";
    }

    function homeLineText(place) {
        if (homePlace() === place) {
            return "This is where you live now.";
        }
        var base = baselineFor(place);
        if (!base) {
            return "";
        }
        var parts = ["housing", "income", "warmth"].map(function (key) {
            var item = facts.fact(place, key, base);
            return item ? item.chip : null;
        }).filter(Boolean);
        return parts.length ? "vs " + base.name + ": " + parts.join(" · ") : "";
    }

    function saveButton(slug, name) {
        var on = saved.indexOf(slug) !== -1;
        return '<button type="button" class="smx-save" data-save="' + escapeHtml(slug) + '" aria-pressed="' + on +
            '" aria-label="' + (on ? "Remove " : "Save ") + escapeHtml(name) + (on ? " from" : " to") + ' shortlist">' +
            BOOKMARK.replace("FILL", on ? "currentColor" : "none") + '</button>';
    }

    function toggleSaved(slug, source) {
        var index = saved.indexOf(slug);
        if (index !== -1) {
            saved.splice(index, 1);
        } else {
            if (saved.length >= 3) {
                saved.shift();
            }
            saved.push(slug);
            track("sm_save", { state: slug });
        }

        // Update the bookmarks in place (no full re-render) and keep the tapped button where the
        // finger is: the shortlist block appears above the list and would otherwise push it away.
        var anchor = source && source.isConnected ? source : null;
        var before = anchor ? anchor.getBoundingClientRect().top : 0;
        document.querySelectorAll("[data-save]").forEach(function (button) {
            if (!button.classList.contains("smx-save")) {
                return;
            }
            var on = saved.indexOf(button.dataset.save) !== -1;
            var place = placeBySlug.get(button.dataset.save);
            button.setAttribute("aria-pressed", String(on));
            button.setAttribute("aria-label", (on ? "Remove " : "Save ") + place.name + (on ? " from" : " to") + " shortlist");
            button.innerHTML = BOOKMARK.replace("FILL", on ? "currentColor" : "none");
        });
        renderShortlist();
        if (anchor && anchor.isConnected) {
            var shift = anchor.getBoundingClientRect().top - before;
            if (shift) {
                window.scrollBy(0, shift);
            }
        }
    }

    function setView(next) {
        view = next;
        var isList = next === "list";
        viewList.hidden = !isList;
        viewMap.hidden = isList;
        tabList.classList.toggle("is-active", isList);
        tabMap.classList.toggle("is-active", !isList);
        tabList.setAttribute("aria-selected", String(isList));
        tabMap.setAttribute("aria-selected", String(!isList));
        if (!isList) {
            track("sm_map_view");
        }
    }

    function renderAll() {
        scored = scorePlaces();
        var eligible = scored.filter(function (row) { return row.eligible; });
        currentRanks = {};
        scored.forEach(function (row) { currentRanks[row.place.slug] = row.rank; });

        var presetLabel = activePresetName ? PRESET_NAMES[activePresetName] : "Custom priorities";
        var mine = homePlace();
        compareLabel.textContent = presetLabel + " · compared with " + (mine ? mine.name : "US average");
        controlsSummary.textContent = presetLabel;
        homeHint.textContent = mine
            ? "Every comparison below is against " + mine.name + "."
            : "Optional. We’ll compare every state with yours instead of the US average.";
        fineLabel.textContent = sliderHost.hidden ? (activePresetName ? "Show" : "Adjusted") : "Hide";

        renderTop(eligible);
        renderDarkHorse(eligible);
        renderRanking();
        updateTileGrid();
        renderShortlist();

        var activeWeight = metrics.reduce(function (sum, metric) {
            return sum + (weights[metric.key] || 0);
        }, 0);
        var excludedCount = scored.length - eligible.length;
        var summary = activeWeight > 0
            ? eligible.length + " of " + scored.length + " places scored"
            : "Choose at least one priority";
        if (excludedCount > 0) {
            summary += " · " + excludedCount + " removed by your must-haves";
        }
        if (noDataPlaces.length) {
            summary += " · " + noDataPlaces.map(function (place) { return place.abbreviation; }).join(", ") + ": not enough data";
        }
        resultSummary.textContent = summary;

        if (currentSlug) {
            renderDrawer(currentSlug);
        }

        // Carry the current priorities over as "your" side of the couples tool.
        if (coupleLink) {
            coupleLink.href = "/tools/where-should-we-move?a=" + encodeURIComponent(weightVector());
        }
    }

    function renderTop(eligible) {
        if (!eligible.length || !hasActiveWeights()) {
            topHost.innerHTML = "";
            return;
        }
        topHost.innerHTML = eligible.slice(0, 3).map(function (row, index) {
            var place = row.place;
            var base = baselineFor(place);
            var list = facts.prosAndCons(place, weights, base);
            var homeLine = homeLineText(place);
            var isOpen = openTop === place.slug;
            var chips = list.pros.slice(0, 2).map(function (item) { return factChip(item, true); })
                .concat(list.cons.slice(0, 1).map(function (item) { return factChip(item, false); })).join("");
            return '<article class="smx-top-card' + (index === 0 ? " is-first" : "") + '" data-slug="' + escapeHtml(place.slug) + '">' +
                '<p class="smx-top-rank">' + (index === 0 ? "Best match" : "#" + (index + 1)) + ' ' + moveHtml(place.slug) + '</p>' +
                '<div class="smx-top-main"><div><h3>' + escapeHtml(place.name) + '</h3>' +
                '<p class="smx-top-region">' + escapeHtml(place.region || "United States") + '</p></div>' +
                '<div class="smx-top-side"><span class="smx-top-score">' + row.score + '</span>' + saveButton(place.slug, place.name) + '</div></div>' +
                '<p class="smx-top-summary">' + escapeHtml(summaryText(row, list)) + '</p>' +
                (homeLine ? '<p class="smx-home-line">' + escapeHtml(homeLine) + '</p>' : '') +
                (isOpen ? '<div class="smx-top-chips">' + chips + '</div>' : '') +
                '<div class="smx-top-actions">' +
                '<button type="button" class="smx-btn smx-btn-ghost" data-toggle-top="' + escapeHtml(place.slug) + '" aria-expanded="' + isOpen + '">' + (isOpen ? "Hide details" : "See details") + '</button>' +
                (isOpen ? '<button type="button" class="smx-btn smx-btn-ghost" data-open="' + escapeHtml(place.slug) + '">Full breakdown</button>' +
                    '<a class="smx-link" href="/states/' + encodeURIComponent(place.slug) + '/living">' + escapeHtml(place.name) + ' living guide</a>' : '') +
                '</div></article>';
        }).join("");

        topHost.querySelectorAll("[data-toggle-top]").forEach(function (button) {
            button.addEventListener("click", function () {
                openTop = openTop === button.dataset.toggleTop ? null : button.dataset.toggleTop;
                if (openTop) {
                    track("sm_details_open", { state: openTop });
                }
                renderTop(scored.filter(function (row) { return row.eligible; }));
            });
        });
    }

    /** A state outside the top 3 that is unusually strong on your single biggest priority. */
    function renderDarkHorse(eligible) {
        darkHorseEl.hidden = true;
        if (!hasActiveWeights() || eligible.length < 8) {
            return;
        }
        var max = 0;
        metrics.forEach(function (metric) { max = Math.max(max, weights[metric.key] || 0); });
        var leaders = metrics.filter(function (metric) { return (weights[metric.key] || 0) === max; });
        if (leaders.length !== 1) {
            return; // no single top priority (e.g. Balanced) means nothing to be "unexpected" about
        }
        var metric = leaders[0];
        var best = null;
        eligible.slice(3, 15).forEach(function (row) {
            var rank = facts.rankOf(row.place, metric.key);
            if (rank && rank.rank <= 5 && (!best || rank.rank < best.rank.rank)) {
                best = { row: row, rank: rank };
            }
        });
        if (!best) {
            return;
        }
        darkHorseEl.hidden = false;
        darkHorseEl.innerHTML = '<div><p class="smx-dark-label">Unexpected match</p>' +
            '<p class="smx-dark-text"><strong>' + escapeHtml(best.row.place.name) + '</strong> is only #' + best.row.rank +
            ' overall, but #' + best.rank.rank + ' of ' + best.rank.of + ' for ' + escapeHtml(metric.name.toLowerCase()) + ', your top priority.</p></div>' +
            '<button type="button" class="smx-btn smx-btn-ghost" data-open="' + escapeHtml(best.row.place.slug) + '">Take a look</button>';
    }

    function renderShortlist() {
        if (!saved.length) {
            shortlistEl.hidden = true;
            shortlistEl.innerHTML = "";
            return;
        }
        shortlistEl.hidden = false;
        var chips = saved.map(function (slug) {
            var place = placeBySlug.get(slug);
            return '<span class="smx-chip-saved">' + escapeHtml(place.name) +
                '<button type="button" class="smx-chip-x" data-save="' + escapeHtml(slug) + '" aria-label="Remove ' + escapeHtml(place.name) + '">×</button></span>';
        }).join("");
        var compare;
        if (saved.length < 2) {
            compare = '<span class="smx-btn smx-btn-gold is-disabled" aria-disabled="true">Save one more to compare</span>';
        } else {
            compare = saved.slice(1).map(function (slug, index) {
                var a = placeBySlug.get(saved[index]);
                var b = placeBySlug.get(slug);
                return '<a class="smx-btn smx-btn-gold" href="/compare/' + [a.slug, b.slug].sort().join("-vs-") + '">Compare ' +
                    escapeHtml(a.abbreviation) + ' vs ' + escapeHtml(b.abbreviation) + '</a>';
            }).join("");
        }
        shortlistEl.innerHTML = '<span class="smx-shortlist-label">Shortlist</span>' + chips + '<span class="smx-shortlist-go">' + compare + '</span>';
    }

    function renderRanking() {
        var query = searchInput.value.trim().toLowerCase();
        var eligible = scored.filter(function (row) { return row.eligible; });
        var filtered = eligible.filter(function (row) {
            return !query ||
                row.place.name.toLowerCase().includes(query) ||
                row.place.abbreviation.toLowerCase().includes(query);
        });
        var shown = query ? filtered : filtered.slice(3);
        var visible = query || showAll ? shown : shown.slice(0, 9);
        var mine = homePlace();

        // Must-have filters can leave very few states; say so and offer the way back.
        var activeFilters = FILTER_KEYS.filter(function (key) { return filters[key]; });
        if (activeFilters.length && eligible.length < 4) {
            emptyEl.hidden = false;
            emptyEl.innerHTML = '<p>' + (eligible.length
                ? 'Only ' + eligible.length + (eligible.length === 1 ? ' state passes' : ' states pass') + ' your must-haves.'
                : 'No state passes all your must-haves.') + ' Remove one to see more:</p><div class="smx-empty-actions">' +
                activeFilters.map(function (key) {
                    return '<button type="button" class="smx-btn smx-btn-ghost" data-clear-filter="' + key + '">Remove “' + escapeHtml(FILTER_LABELS[key]) + '”</button>';
                }).join("") + '</div>';
        } else {
            emptyEl.hidden = true;
            emptyEl.innerHTML = "";
        }

        if (!visible.length) {
            rankingHost.innerHTML = query ? '<li class="smx-row-empty">No state matches that search.</li>' : "";
        } else {
            rankingHost.innerHTML = visible.map(function (row) {
                var list = facts.prosAndCons(row.place, weights, baselineFor(row.place));
                var chips = list.pros.slice(0, 2).map(function (item) { return factChip(item, true); })
                    .concat(list.cons.slice(0, 1).map(function (item) { return factChip(item, false); }))
                    .join("");
                return '<li class="smx-row" data-slug="' + escapeHtml(row.place.slug) + '">' +
                    '<button class="smx-row-main" type="button" data-slug="' + escapeHtml(row.place.slug) + '"' +
                    ' aria-label="Open ' + escapeHtml(row.place.name) + ', rank ' + row.rank + ', ' + row.score + ' percent match">' +
                    '<span class="smx-row-rank">' + String(row.rank).padStart(2, "0") + '</span>' +
                    '<span class="smx-row-name"><strong>' + escapeHtml(row.place.name) + (mine === row.place ? ' <em class="sm-home-tag">Your state</em>' : '') + '</strong>' +
                    '<small>' + escapeHtml(row.place.region || "United States") + '</small></span>' +
                    '<span class="smx-row-move">' + moveHtml(row.place.slug) + '</span>' +
                    '<span class="smx-row-bar"><i style="width:' + row.score + '%"></i></span>' +
                    '<span class="smx-row-score">' + row.score + '</span></button>' +
                    saveButton(row.place.slug, row.place.name) +
                    (chips ? '<span class="smx-row-chips">' + chips + '</span>' : '') +
                    '</li>';
            }).join("");
        }

        rankingHost.querySelectorAll(".smx-row-main").forEach(function (button) {
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

        showAllButton.hidden = Boolean(query) || shown.length <= 9;
        showAllButton.innerHTML = showAll
            ? 'Show fewer <i class="fa-solid fa-chevron-up" aria-hidden="true"></i>'
            : 'Show all ' + eligible.length + ' states <i class="fa-solid fa-chevron-down" aria-hidden="true"></i>';

        renderSensitivity(eligible);
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

                for (var step = 0; step < LEVELS.length; step++) {
                    var trialWeight = LEVELS[step].value;
                    if (trialWeight <= currentWeight) {
                        continue;
                    }
                    var trialWeights = Object.assign({}, weights);
                    trialWeights[metric.key] = trialWeight;
                    var scoreIn = scoreWithWeights(inRank.place, trialWeights);
                    var scoreOut = scoreWithWeights(outRank.place, trialWeights);
                    if (scoreOut > scoreIn) {
                        var delta = trialWeight - currentWeight;
                        if (!best || delta < best.delta) {
                            best = { metric: metric, delta: delta, trialWeight: trialWeight, label: LEVELS[step].label };
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
            '<span class="sm-sensitivity-text"><strong>Close call:</strong> set ' + escapeHtml(best.metric.name) + ' to “' + best.label +
            '” and <strong>' + escapeHtml(outRank.place.name) + '</strong> (#11) passes ' +
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
        // Stretch the colour over the scores actually on screen, so 61 and 68 look different.
        var live = scored.filter(function (row) { return row.eligible; }).map(function (row) { return row.score; });
        var min = live.length ? Math.min.apply(null, live) : 0;
        var max = live.length ? Math.max.apply(null, live) : 100;
        document.getElementById("sm-legend-min").textContent = min;
        document.getElementById("sm-legend-max").textContent = max;
        var mine = homePlace();

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
            var excluded = !row.eligible;
            var active = activeWeights && !excluded;
            var t = max > min ? (row.score - min) / (max - min) : 0.5;
            tile.style.background = active
                ? "rgb(" + Math.round(217 + (27 - 217) * t) + "," + Math.round(226 + (47 - 226) * t) + "," + Math.round(235 + (78 - 235) * t) + ")"
                : "";
            tile.classList.toggle("is-strong", active && t > 0.5);
            tile.classList.toggle("is-excluded", excluded);
            tile.classList.toggle("is-top", active && row.rank <= 5);
            tile.classList.toggle("is-home", Boolean(mine) && mine === row.place);
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
        track("sm_detail_open", { state: slug });
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
                close.focus({ preventScroll: true });
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
            lastFocused.focus({ preventScroll: true });
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
