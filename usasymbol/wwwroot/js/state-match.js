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
    var placeByFips = new Map(places.map(function (place) { return [String(place.fips).padStart(2, "0"), place]; }));

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

    var bandColors = {
        best: { fill: "#dcebdd", ink: "#3f7a4e" },
        mid: { fill: "#f2e3bd", ink: "#a66e20" },
        low: { fill: "#eed7d2", ink: "#ad554d" }
    };

    var presets = {
        balanced: defaults(),
        budget: preset({ cost: 100, housing: 95, taxes: 82, jobs: 52, income: 36, safety: 45, health: 30, education: 25, warmth: 20 }),
        career: preset({ income: 100, jobs: 96, education: 68, cost: 42, housing: 35, safety: 35, health: 40, taxes: 32, warmth: 15 }),
        family: preset({ safety: 100, education: 95, health: 88, housing: 62, cost: 60, jobs: 55, income: 48, taxes: 35, warmth: 20 }),
        warm: preset({ warmth: 100, cost: 58, housing: 52, jobs: 45, safety: 42, health: 40, taxes: 35, income: 30, education: 25 })
    };

    var weights = readWeightsFromUrl();
    var metricRanges = buildMetricRanges();
    var scored = [];
    var showAll = false;
    var currentSlug = null;
    var lastFocused = null;
    var mapReady = false;
    var mapSelection = null;
    var updateUrlTimer = null;

    buildSliders();
    bindControls();
    renderAll();
    initializeMap();

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

    function readWeightsFromUrl() {
        var result = defaults();
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

    function scorePlaces() {
        var hasAnyWeight = metrics.some(function (metric) { return weights[metric.key] > 0; });
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
                band: "mid",
                rank: 0
            };
        });

        rows.sort(function (a, b) {
            return b.score - a.score || a.place.name.localeCompare(b.place.name);
        });

        rows.forEach(function (row, index) {
            row.rank = index + 1;
            row.band = !hasAnyWeight
                ? "mid"
                : index < rows.length * 0.3
                    ? "best"
                    : index < rows.length * 0.66
                        ? "mid"
                        : "low";
        });

        return rows;
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
                '<input class="sm-range" id="sm-weight-' + safeKey + '" type="range" min="0" max="100" step="5" value="' + weights[metric.key] + '"',
                ' data-key="' + safeKey + '" aria-label="' + escapeHtml(metric.name) + ' importance" title="' + escapeHtml(metric.description) + '">',
                '</div>'
            ].join("");
        }).join("");

        sliderHost.querySelectorAll(".sm-range").forEach(function (input) {
            input.addEventListener("input", function () {
                var key = input.dataset.key;
                weights[key] = Number(input.value);
                document.getElementById("sm-value-" + key).textContent = input.value;
                setActivePreset(null);
                renderAll();
                scheduleUrlUpdate();
            });
        });

        syncPresetState();
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

    function syncSliderValues() {
        metrics.forEach(function (metric) {
            var input = document.getElementById("sm-weight-" + metric.key);
            var output = document.getElementById("sm-value-" + metric.key);
            if (input) {
                input.value = weights[metric.key];
            }
            if (output) {
                output.textContent = weights[metric.key];
            }
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
        updateMap();

        var activeWeight = metrics.reduce(function (sum, metric) {
            return sum + (weights[metric.key] || 0);
        }, 0);
        resultSummary.textContent = activeWeight > 0
            ? scored.length + " places scored · top match " + (scored[0] ? scored[0].place.name : "—")
            : "Choose at least one priority";

        if (currentSlug) {
            renderDrawer(currentSlug);
        }
    }

    function renderRanking() {
        var query = searchInput.value.trim().toLowerCase();
        var filtered = scored.filter(function (row) {
            return !query ||
                row.place.name.toLowerCase().includes(query) ||
                row.place.abbreviation.toLowerCase().includes(query);
        });
        var visible = query || showAll ? filtered : filtered.slice(0, 10);

        if (!visible.length) {
            rankingHost.innerHTML = '<p class="sm-empty">No state matches that search.</p>';
        } else {
            rankingHost.innerHTML = visible.map(function (row) {
                var strongest = strongestMetric(row);
                var color = bandColors[row.band].ink;
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
                    '<span class="sm-row-score" style="--score-color:' + color + '">',
                    '<span class="sm-score-track"><i style="width:' + row.score + '%"></i></span>',
                    '<span class="sm-score-number">' + row.score + '</span>',
                    '</span>',
                    '</button>'
                ].join("");
            }).join("");
        }

        rankingHost.querySelectorAll(".sm-state-row").forEach(function (button) {
            button.addEventListener("click", function () {
                openDetail(button.dataset.slug, true);
            });
        });

        showAllButton.hidden = Boolean(query) || filtered.length <= 10;
        showAllButton.innerHTML = showAll
            ? 'Show top 10 <i class="fa-solid fa-chevron-up" aria-hidden="true"></i>'
            : 'Show all states <i class="fa-solid fa-chevron-down" aria-hidden="true"></i>';
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

    async function initializeMap() {
        var loading = document.getElementById("sm-map-loading");
        if (!window.d3 || !window.topojson) {
            loading.innerHTML = '<i class="fa-solid fa-triangle-exclamation" aria-hidden="true"></i> Map library unavailable';
            return;
        }

        try {
            var topology = await window.d3.json(payload.mapUrl);
            var featureCollection = window.topojson.feature(topology, topology.objects.states);
            var svg = window.d3.select("#sm-map");
            var projection = window.d3.geoAlbersUsa().fitExtent([[24, 20], [936, 574]], featureCollection);
            var path = window.d3.geoPath(projection);

            mapSelection = svg.append("g")
                .selectAll("path")
                .data(featureCollection.features.filter(function (feature) {
                    return placeByFips.has(String(feature.id).padStart(2, "0"));
                }))
                .join("path")
                .attr("class", "sm-state-shape")
                .attr("d", path)
                .attr("tabindex", 0)
                .attr("role", "button")
                .attr("data-fips", function (feature) { return String(feature.id).padStart(2, "0"); })
                .attr("aria-label", function (feature) {
                    var place = placeByFips.get(String(feature.id).padStart(2, "0"));
                    return place ? "Open " + place.name : "State";
                })
                .on("click", function (_, feature) {
                    var place = placeByFips.get(String(feature.id).padStart(2, "0"));
                    if (place) {
                        openDetail(place.slug, true);
                    }
                })
                .on("keydown", function (event, feature) {
                    if (event.key === "Enter" || event.key === " ") {
                        event.preventDefault();
                        var place = placeByFips.get(String(feature.id).padStart(2, "0"));
                        if (place) {
                            openDetail(place.slug, true);
                        }
                    }
                })
                .on("pointerenter pointermove", showMapTooltip)
                .on("pointerleave", hideMapTooltip);

            svg.append("path")
                .datum(window.topojson.mesh(topology, topology.objects.states, function (a, b) { return a !== b; }))
                .attr("class", "sm-map-border")
                .attr("d", path);

            var labelOmissions = new Set(["09", "10", "11", "24", "25", "34", "44"]);
            svg.append("g")
                .selectAll("text")
                .data(featureCollection.features.filter(function (feature) {
                    var fips = String(feature.id).padStart(2, "0");
                    return placeByFips.has(fips) && !labelOmissions.has(fips);
                }))
                .join("text")
                .attr("class", "sm-map-label")
                .attr("transform", function (feature) {
                    var centroid = path.centroid(feature);
                    return "translate(" + centroid[0] + "," + centroid[1] + ")";
                })
                .attr("dy", ".35em")
                .text(function (feature) {
                    return placeByFips.get(String(feature.id).padStart(2, "0")).abbreviation;
                });

            mapReady = true;
            loading.classList.add("is-hidden");
            updateMap();
        } catch (error) {
            loading.innerHTML = '<i class="fa-solid fa-triangle-exclamation" aria-hidden="true"></i> Map could not load';
        }
    }

    function updateMap() {
        if (!mapReady || !mapSelection) {
            return;
        }

        var byFips = new Map(scored.map(function (row) { return [String(row.place.fips).padStart(2, "0"), row]; }));
        mapSelection
            .attr("fill", function (feature) {
                var row = byFips.get(String(feature.id).padStart(2, "0"));
                return row ? bandColors[row.band].fill : "#e5e0d6";
            })
            .classed("is-selected", function (feature) {
                var place = placeByFips.get(String(feature.id).padStart(2, "0"));
                return Boolean(place && place.slug === currentSlug);
            });
    }

    function showMapTooltip(event, feature) {
        var place = placeByFips.get(String(feature.id).padStart(2, "0"));
        var row = scored.find(function (item) { return item.place.slug === place.slug; });
        if (!place || !row) {
            return;
        }

        var tooltip = document.getElementById("sm-map-tooltip");
        tooltip.innerHTML = '<strong>' + escapeHtml(place.name) + '</strong><span>' + row.score + '% match · #' + row.rank + '</span>';
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
        updateMap();

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
        updateMap();
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
        var bandColor = bandColors[row.band].ink;
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
            '<div class="sm-match-badge" style="--band-color:' + bandColor + '"><strong>' + row.score + '</strong><span>match</span></div>',
            '</div>',
            '<div class="sm-facts">',
            '<span class="sm-fact"><b>Capital</b> ' + escapeHtml(place.capital || "—") + '</span>',
            '<span class="sm-fact"><b>Population</b> ' + formatPopulation(place.population) + '</span>',
            '<span class="sm-fact"><b>Rank</b> #' + row.rank + ' of ' + scored.length + '</span>',
            '<span class="sm-fact"><b>FIPS</b> ' + escapeHtml(place.fips) + '</span>',
            '</div>',
            '</div>',
            '<section class="sm-drawer-section"><h3>Why it matches</h3><p class="sm-overview">' + escapeHtml(overview) + '</p></section>',
            '<section class="sm-drawer-section"><h3>Where it stands · nationwide</h3>' + metricRows + '</section>',
            '<div class="sm-drawer-actions">',
            '<a href="/states/' + encodeURIComponent(place.slug) + '/living">Living guide <i class="fa-solid fa-arrow-right" aria-hidden="true"></i></a>',
            '<a href="/states/' + encodeURIComponent(place.slug) + '">Symbols & facts <i class="fa-solid fa-landmark" aria-hidden="true"></i></a>',
            countyAction,
            '</div>'
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
        try {
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(value);
            } else {
                var textarea = document.createElement("textarea");
                textarea.value = value;
                textarea.style.position = "fixed";
                textarea.style.opacity = "0";
                document.body.appendChild(textarea);
                textarea.select();
                document.execCommand("copy");
                textarea.remove();
            }
            shareStatus.textContent = "Link copied";
        } catch (_) {
            shareStatus.textContent = "Copy the URL from your browser";
        }
        window.setTimeout(function () { shareStatus.textContent = ""; }, 2500);
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
