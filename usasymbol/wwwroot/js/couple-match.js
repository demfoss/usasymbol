// Where Should We Move? (/tools/where-should-we-move) — two independent State Match
// weight sets scored side by side. "?a=" / "?b=" carry each person's weights in the same
// vector format as State Match's "?w=", and "?na=" / "?nb=" carry their display names.
(function () {
    "use strict";

    var payload = window.stateMatchData;
    var core = window.StateMatchCore;
    if (!payload || !core || !Array.isArray(payload.places) || !Array.isArray(payload.metrics)) {
        return;
    }

    // Places without metric data (DC today) would score a meaningless 0 for both people.
    var places = payload.places.filter(function (place) { return place.metrics.length > 0; });
    var metrics = payload.metrics;
    var scorer = core.createScorer(places, metrics);
    var escapeHtml = core.escapeHtml;
    var PEOPLE = ["a", "b"];
    var DEFAULT_NAMES = { a: "You", b: "Partner" };

    var params = new URL(window.location.href).searchParams;
    var weights = {
        a: params.has("a") ? scorer.parseWeightVector(params.get("a")) : scorer.preset("balanced"),
        b: params.has("b") ? scorer.parseWeightVector(params.get("b")) : scorer.preset("family")
    };
    var names = {
        a: cleanName(params.get("na")) || DEFAULT_NAMES.a,
        b: cleanName(params.get("nb")) || DEFAULT_NAMES.b
    };
    var mode = params.get("rank") === "fair" ? "fair" : "average";
    var showAll = false;
    var openSlug = null;
    var rows = [];
    var urlTimer = null;

    var listHost = document.getElementById("cm-list");
    var showAllButton = document.getElementById("cm-show-all");
    var verdictHost = document.getElementById("cm-verdict");
    var splitHost = document.getElementById("cm-split");
    var modeHint = document.getElementById("cm-mode-hint");
    var tableKicker = document.getElementById("cm-table-kicker");
    var plotSvg = document.getElementById("cm-plot");
    var plotWrap = document.getElementById("cm-plot-wrap");
    var plotTip = document.getElementById("cm-plot-tip");
    var plotCaption = document.getElementById("cm-plot-caption");

    PEOPLE.forEach(buildPerson);
    bindPlot();
    bindGlobal();
    syncNames();
    render();

    // ---------- people ----------

    function buildPerson(key) {
        var section = document.querySelector('.cm-person[data-person="' + key + '"]');
        var sliderHost = section.querySelector('[data-sliders-for="' + key + '"]');
        var nameInput = section.querySelector(".cm-name");

        nameInput.value = names[key];
        nameInput.addEventListener("input", function () {
            names[key] = cleanName(nameInput.value) || DEFAULT_NAMES[key];
            syncNames();
            render();
        });
        nameInput.addEventListener("blur", function () {
            nameInput.value = names[key];
        });

        sliderHost.innerHTML = metrics.map(function (metric) {
            var id = "cm-" + key + "-" + escapeHtml(metric.key);
            return '<div class="cm-slider">' +
                '<label for="' + id + '"><i class="' + escapeHtml(metric.icon) + '" aria-hidden="true"></i>' +
                '<span>' + escapeHtml(metric.name) + '</span>' +
                '<output id="' + id + '-out" for="' + id + '">' + weights[key][metric.key] + '</output></label>' +
                '<input class="sm-range" type="range" min="0" max="100" step="5" id="' + id + '" data-metric="' + escapeHtml(metric.key) + '"' +
                ' value="' + weights[key][metric.key] + '" aria-label="' + escapeHtml(metric.name) + ' importance, 0 to 100">' +
                '</div>';
        }).join("");

        sliderHost.querySelectorAll(".sm-range").forEach(function (input) {
            fillRange(input);
            input.addEventListener("input", function () {
                weights[key][input.dataset.metric] = Number(input.value);
                document.getElementById(input.id + "-out").textContent = input.value;
                fillRange(input);
                syncPerson(key);
                render();
            });
        });

        section.querySelectorAll(".cm-preset").forEach(function (button) {
            button.addEventListener("click", function () {
                weights[key] = scorer.preset(button.dataset.preset);
                syncSliders(key);
                syncPerson(key);
                render();
            });
        });

        syncPerson(key);
    }

    function syncSliders(key) {
        metrics.forEach(function (metric) {
            var input = document.getElementById("cm-" + key + "-" + metric.key);
            if (input) {
                input.value = weights[key][metric.key];
                document.getElementById(input.id + "-out").textContent = input.value;
                fillRange(input);
            }
        });
    }

    function syncPerson(key) {
        var section = document.querySelector('.cm-person[data-person="' + key + '"]');
        var active = scorer.matchingPreset(weights[key]);
        section.querySelectorAll(".cm-preset").forEach(function (button) {
            var isActive = button.dataset.preset === active;
            button.classList.toggle("is-active", isActive);
            button.setAttribute("aria-pressed", String(isActive));
        });

        var top = metrics
            .filter(function (metric) { return weights[key][metric.key] > 0; })
            .sort(function (x, y) { return weights[key][y.key] - weights[key][x.key]; })
            .slice(0, 3)
            .map(function (metric) { return metric.name.toLowerCase(); });
        var summary = section.querySelector('[data-summary-for="' + key + '"]');
        summary.innerHTML = top.length
            ? '<span>' + (active ? escapeHtml(core.PRESET_LABELS[active]) + " · " : "Custom · ") + '</span>Cares most about ' + escapeHtml(joinList(top)) + '.'
            : 'No priorities set. Raise at least one slider.';
    }

    function syncNames() {
        document.querySelectorAll("[data-name-for]").forEach(function (node) {
            node.textContent = names[node.dataset.nameFor];
        });
    }

    // ---------- scoring ----------

    function computeRows() {
        var scoredRows = places.map(function (place) {
            var a = scorer.score(place, weights.a);
            var b = scorer.score(place, weights.b);
            return {
                place: place,
                a: a,
                b: b,
                together: Math.round((a + b) / 2),
                floor: Math.min(a, b)
            };
        });

        var byA = scoredRows.slice().sort(function (x, y) { return y.a - x.a || x.place.name.localeCompare(y.place.name); });
        var byB = scoredRows.slice().sort(function (x, y) { return y.b - x.b || x.place.name.localeCompare(y.place.name); });
        byA.forEach(function (row, index) { row.rankA = index + 1; });
        byB.forEach(function (row, index) { row.rankB = index + 1; });

        var primary = mode === "fair" ? "floor" : "together";
        var secondary = mode === "fair" ? "together" : "floor";
        scoredRows.sort(function (x, y) {
            return y[primary] - x[primary] || y[secondary] - x[secondary] || x.place.name.localeCompare(y.place.name);
        });
        scoredRows.forEach(function (row, index) { row.rank = index + 1; });
        return scoredRows;
    }

    function hasWeights(key) {
        return metrics.some(function (metric) { return weights[key][metric.key] > 0; });
    }

    // ---------- render ----------

    function render() {
        rows = computeRows();
        tableKicker.textContent = mode === "fair" ? "Ranked by the lower of your two scores" : "Ranked by combined score";
        modeHint.textContent = mode === "fair"
            ? "Fairest puts states first where neither of you scores low, even if neither of you loves them."
            : "Combined averages both scores. A state one of you loves can rank high even if the other is lukewarm.";
        renderVerdict();
        renderTable();
        renderPlot();
        renderSplit();
        scheduleUrlUpdate();
    }

    function renderVerdict() {
        if (!hasWeights("a") || !hasWeights("b")) {
            verdictHost.innerHTML = '<p class="cm-verdict-empty">Both people need at least one priority above zero to compare.</p>';
            return;
        }

        var shared = rows[0];
        var topA = rows.find(function (row) { return row.rankA === 1; });
        var topB = rows.find(function (row) { return row.rankB === 1; });
        var overlap = rows.filter(function (row) { return row.rankA <= 10 && row.rankB <= 10; })
            .sort(function (x, y) { return x.rank - y.rank; });
        var overlapText = overlap.length
            ? overlap.slice(0, 6).map(function (row) { return row.place.name; }).join(", ") + (overlap.length > 6 ? ", and " + (overlap.length - 6) + " more" : "")
            : "None yet. Your priorities pull in different directions.";

        verdictHost.innerHTML =
            '<div class="cm-verdict-main">' +
                '<p class="sm-section-kicker">Best state for both of you</p>' +
                '<div class="cm-verdict-state">' +
                    '<img src="' + escapeHtml(shared.place.flagImageUrl) + '" alt="" width="54" height="36">' +
                    '<strong>' + escapeHtml(shared.place.name) + '</strong>' +
                '</div>' +
                '<p class="cm-verdict-scores">' +
                    scoreChip("a", shared.a) + scoreChip("b", shared.b) +
                    '<span class="cm-chip cm-chip-together">Together ' + shared.together + '</span>' +
                '</p>' +
            '</div>' +
            '<dl class="cm-verdict-facts">' +
                '<div><dt>' + "#1 for " + escapeHtml(names.a) + '</dt><dd>' + escapeHtml(topA.place.name) + ' <small>' + topA.a + '</small></dd></div>' +
                '<div><dt>' + "#1 for " + escapeHtml(names.b) + '</dt><dd>' + escapeHtml(topB.place.name) + ' <small>' + topB.b + '</small></dd></div>' +
                '<div class="cm-overlap"><dt>Top 10 overlap</dt><dd><span class="cm-overlap-count">' + overlap.length + '<small>/10</small></span>' +
                    '<span class="cm-overlap-bar" aria-hidden="true"><i style="width:' + overlap.length * 10 + '%"></i></span>' +
                    '<small class="cm-overlap-list">' + escapeHtml(overlapText) + '</small></dd></div>' +
            '</dl>' +
            '<div class="cm-verdict-actions">' +
                '<button type="button" class="smt-btn smt-btn-dark" id="cm-share"><i class="fa-solid fa-paper-plane" aria-hidden="true"></i> Copy link for ' + escapeHtml(names.b === DEFAULT_NAMES.b ? "your partner" : names.b) + '</button>' +
                '<span class="smt-status" id="cm-share-status" role="status" aria-live="polite"></span>' +
            '</div>';
    }

    function scoreChip(key, value) {
        return '<span class="cm-chip cm-chip-' + key + '">' + escapeHtml(names[key]) + ' ' + value + '</span>';
    }

    function renderTable() {
        var visible = showAll ? rows : rows.slice(0, 10);
        listHost.innerHTML = visible.map(function (row) {
            var slug = row.place.slug;
            var isOpen = openSlug === slug;
            var value = mode === "fair" ? row.floor : row.together;
            return '<div class="cm-item' + (isOpen ? " is-open" : "") + '">' +
                '<button type="button" class="cm-row cm-row-toggle" data-slug="' + escapeHtml(slug) + '" aria-expanded="' + isOpen + '">' +
                '<span class="cm-rank">' + row.rank + '</span>' +
                '<span class="cm-state"><img src="' + escapeHtml(row.place.flagImageUrl) + '" alt="" width="30" height="20" loading="lazy">' +
                    '<span><strong>' + escapeHtml(row.place.name) + '</strong><small>' + escapeHtml(names.a) + ' #' + row.rankA + ' · ' + escapeHtml(names.b) + ' #' + row.rankB + '</small></span></span>' +
                '<span class="cm-num cm-score-a">' + row.a + '</span>' +
                '<span class="cm-num cm-score-b">' + row.b + '</span>' +
                '<span class="cm-num cm-together"><b>' + value + '</b>' +
                    '<span class="cm-pair" aria-hidden="true"><i class="cm-pair-a" style="left:' + row.a + '%"></i><i class="cm-pair-b" style="left:' + row.b + '%"></i>' +
                    '<i class="cm-pair-span" style="left:' + Math.min(row.a, row.b) + '%;width:' + Math.abs(row.a - row.b) + '%"></i></span></span>' +
                '</button>' +
                (isOpen ? renderDetail(row) : "") +
                '</div>';
        }).join("");

        showAllButton.hidden = rows.length <= 10;
        showAllButton.innerHTML = showAll
            ? 'Show top 10 <i class="fa-solid fa-chevron-up" aria-hidden="true"></i>'
            : 'Show all ' + rows.length + ' states <i class="fa-solid fa-chevron-down" aria-hidden="true"></i>';
    }

    function renderDetail(row) {
        return '<div class="cm-detail">' +
            PEOPLE.map(function (key) {
                var ranked = metrics
                    .filter(function (metric) { return weights[key][metric.key] > 0; })
                    .map(function (metric) {
                        var value = row.place.metrics.find(function (item) { return item.key === metric.key; });
                        return value ? { metric: metric, fit: scorer.normalizedValue(value), weight: weights[key][metric.key], display: value.displayValue } : null;
                    })
                    .filter(Boolean)
                    .sort(function (x, y) { return y.weight - x.weight; })
                    .slice(0, 4);
                return '<div class="cm-detail-person cm-detail-' + key + '">' +
                    '<h3>' + escapeHtml(names[key]) + ' <span>' + row[key] + '</span></h3>' +
                    '<ul>' + ranked.map(function (item) {
                        var fit = Math.round(item.fit * 100);
                        return '<li><span>' + escapeHtml(item.metric.name) + '<small>' + escapeHtml(item.display) + '</small></span>' +
                            '<span class="cm-fit"><i style="width:' + fit + '%"></i></span></li>';
                    }).join("") + '</ul>' +
                    '</div>';
            }).join("") +
            '<p class="cm-detail-links">' +
                '<a href="/states/' + encodeURIComponent(row.place.slug) + '/living">Living guide <i class="fa-solid fa-arrow-right" aria-hidden="true"></i></a>' +
                '<a href="/tools/take-home-pay-by-state">Take-home pay here <i class="fa-solid fa-arrow-right" aria-hidden="true"></i></a>' +
            '</p>' +
            '</div>';
    }

    function renderSplit() {
        var candidates = rows
            .filter(function (row) { return row.rankA <= 15 || row.rankB <= 15; })
            .sort(function (x, y) { return Math.abs(y.a - y.b) - Math.abs(x.a - x.b); })
            .slice(0, 5);

        if (!candidates.length || Math.abs(candidates[0].a - candidates[0].b) < 6) {
            splitHost.innerHTML = '<p class="cm-split-empty">No big disagreements. Your favorite states score about the same for both of you.</p>';
            return;
        }

        splitHost.innerHTML = candidates.map(function (row) {
            var fan = row.a >= row.b ? "a" : "b";
            return '<div class="cm-split-row">' +
                '<img src="' + escapeHtml(row.place.flagImageUrl) + '" alt="" width="27" height="18" loading="lazy">' +
                '<span class="cm-split-name"><strong>' + escapeHtml(row.place.name) + '</strong>' +
                    '<small>' + escapeHtml(names[fan]) + ' likes it more</small></span>' +
                '<span class="cm-split-scores">' + scoreChip("a", row.a) + scoreChip("b", row.b) + '</span>' +
                '</div>';
        }).join("");
    }

    // Scatter: first person's score across, second person's up, one dot per state. Both axes
    // share one domain so the dashed diagonal means "you both score it the same". The shaded
    // corner is exactly "in both top 10s" (above each person's 10th-best score).
    function renderPlot() {
        var PLOT = { width: 600, height: 440, left: 46, right: 18, top: 18, bottom: 46 };
        if (!rows.length) {
            plotSvg.innerHTML = "";
            return;
        }

        var all = [];
        rows.forEach(function (row) { all.push(row.a, row.b); });
        var lo = Math.max(0, Math.floor((Math.min.apply(null, all) - 2) / 10) * 10);
        var hi = Math.min(100, Math.ceil((Math.max.apply(null, all) + 2) / 10) * 10);
        var innerW = PLOT.width - PLOT.left - PLOT.right;
        var innerH = PLOT.height - PLOT.top - PLOT.bottom;
        var x = function (value) { return PLOT.left + (value - lo) / (hi - lo) * innerW; };
        var y = function (value) { return PLOT.top + innerH - (value - lo) / (hi - lo) * innerH; };

        var tenthA = rows.slice().sort(function (p, q) { return q.a - p.a; })[Math.min(9, rows.length - 1)].a;
        var tenthB = rows.slice().sort(function (p, q) { return q.b - p.b; })[Math.min(9, rows.length - 1)].b;

        var parts = [];

        // Sweet-spot corner
        parts.push('<rect class="cm-plot-zone" x="' + x(tenthA) + '" y="' + y(hi) + '" width="' + (x(hi) - x(tenthA)) + '" height="' + (y(tenthB) - y(hi)) + '"></rect>');
        parts.push('<text class="cm-plot-zone-label" x="' + (x(hi) - 8) + '" y="' + (y(hi) + 16) + '" text-anchor="end">Both top 10</text>');

        // Grid + ticks
        for (var tick = lo; tick <= hi; tick += 10) {
            parts.push('<line class="cm-plot-grid" x1="' + x(tick) + '" x2="' + x(tick) + '" y1="' + y(lo) + '" y2="' + y(hi) + '"></line>');
            parts.push('<line class="cm-plot-grid" x1="' + x(lo) + '" x2="' + x(hi) + '" y1="' + y(tick) + '" y2="' + y(tick) + '"></line>');
            parts.push('<text class="cm-plot-tick" x="' + x(tick) + '" y="' + (y(lo) + 16) + '" text-anchor="middle">' + tick + '</text>');
            parts.push('<text class="cm-plot-tick" x="' + (x(lo) - 8) + '" y="' + (y(tick) + 3) + '" text-anchor="end">' + tick + '</text>');
        }
        parts.push('<line class="cm-plot-axis" x1="' + x(lo) + '" x2="' + x(hi) + '" y1="' + y(lo) + '" y2="' + y(lo) + '"></line>');
        parts.push('<line class="cm-plot-axis" x1="' + x(lo) + '" x2="' + x(lo) + '" y1="' + y(lo) + '" y2="' + y(hi) + '"></line>');
        parts.push('<line class="cm-plot-diag" x1="' + x(lo) + '" y1="' + y(lo) + '" x2="' + x(hi) + '" y2="' + y(hi) + '"></line>');

        // Axis titles carry each person's color as a swatch, text stays ink.
        parts.push('<text class="cm-plot-title" x="' + x(hi) + '" y="' + (PLOT.height - 6) + '" text-anchor="end">' +
            '<tspan class="cm-plot-swatch-a">●</tspan> ' + escapeHtml(names.a) + '’s score →</text>');
        parts.push('<text class="cm-plot-title" transform="translate(12 ' + y(hi) + ') rotate(-90)" text-anchor="end">' +
            escapeHtml(names.b) + '’s score → <tspan class="cm-plot-swatch-b">●</tspan></text>');

        // Dots: others first so the highlighted ones sit on top.
        var ordered = rows.slice().sort(function (p, q) {
            var pBoth = p.rankA <= 10 && p.rankB <= 10 ? 1 : 0;
            var qBoth = q.rankA <= 10 && q.rankB <= 10 ? 1 : 0;
            return pBoth - qBoth || q.rank - p.rank;
        });
        ordered.forEach(function (row) {
            var both = row.rankA <= 10 && row.rankB <= 10;
            var cx = x(row.a).toFixed(1);
            var cy = y(row.b).toFixed(1);
            parts.push('<g class="cm-dot-g' + (both ? " is-both" : "") + (row.rank === 1 ? " is-top" : "") + (openSlug === row.place.slug ? " is-open" : "") + '" data-slug="' + escapeHtml(row.place.slug) + '" tabindex="0" role="button"' +
                ' aria-label="' + escapeHtml(row.place.name + ": " + names.a + " " + row.a + ", " + names.b + " " + row.b + ", shared rank " + row.rank) + '">' +
                '<circle class="cm-dot-hit" cx="' + cx + '" cy="' + cy + '" r="12"></circle>' +
                '<circle class="cm-dot-mark" cx="' + cx + '" cy="' + cy + '" r="' + (row.rank === 1 ? 8 : both ? 6 : 4.5) + '"></circle>' +
                '</g>');
        });

        // Selective direct labels: the shared top picks and each person's #1.
        var labelled = rows.filter(function (row) {
            return row.rank <= 4 || row.rankA === 1 || row.rankB === 1;
        });
        labelled.forEach(function (row) {
            var nearRight = x(row.a) > PLOT.width - PLOT.right - 40;
            parts.push('<text class="cm-dot-label' + (row.rank === 1 ? " is-top" : "") + '" x="' + (x(row.a) + (nearRight ? -11 : 11)) + '" y="' + (y(row.b) + 4) + '"' +
                (nearRight ? ' text-anchor="end"' : '') + '>' + escapeHtml(row.place.abbreviation) + '</text>');
        });

        plotSvg.innerHTML = parts.join("");

        var both = rows.filter(function (row) { return row.rankA <= 10 && row.rankB <= 10; }).length;
        var above = rows.filter(function (row) { return row.b > row.a; }).length;
        plotCaption.textContent = both + " state" + (both === 1 ? " lands" : "s land") + " in the shared corner. " +
            above + " of " + rows.length + " states suit " + names.b + " better than " + names.a + " (above the line).";
    }

    function bindPlot() {
        function showTip(group) {
            var row = rows.find(function (item) { return item.place.slug === group.dataset.slug; });
            if (!row) {
                return;
            }
            var mark = group.querySelector(".cm-dot-mark");
            var box = plotWrap.getBoundingClientRect();
            var dot = mark.getBoundingClientRect();
            plotTip.innerHTML = '<strong>' + escapeHtml(row.place.name) + '</strong>' +
                '<span><i class="cm-tip-a"></i>' + escapeHtml(names.a) + ' <b>' + row.a + '</b></span>' +
                '<span><i class="cm-tip-b"></i>' + escapeHtml(names.b) + ' <b>' + row.b + '</b></span>' +
                '<em>#' + row.rank + ' together</em>';
            var left = dot.left - box.left + dot.width / 2;
            plotTip.style.left = Math.min(Math.max(left, 70), box.width - 70) + "px";
            plotTip.style.top = (dot.top - box.top) + "px";
            plotTip.classList.add("is-visible");
        }
        function hideTip() {
            plotTip.classList.remove("is-visible");
        }
        function openRow(slug) {
            var row = rows.find(function (item) { return item.place.slug === slug; });
            if (!row) {
                return;
            }
            openSlug = slug;
            if (row.rank > 10) {
                showAll = true;
            }
            renderTable();
            renderPlot();
            var target = listHost.querySelector('.cm-row-toggle[data-slug="' + slug + '"]');
            if (target) {
                target.scrollIntoView({ behavior: "smooth", block: "center" });
                target.focus({ preventScroll: true });
            }
        }

        plotSvg.addEventListener("pointerover", function (event) {
            var group = event.target.closest(".cm-dot-g");
            if (group) {
                showTip(group);
            }
        });
        plotSvg.addEventListener("pointerleave", hideTip);
        plotSvg.addEventListener("focusin", function (event) {
            var group = event.target.closest(".cm-dot-g");
            if (group) {
                showTip(group);
            }
        });
        plotSvg.addEventListener("focusout", hideTip);
        plotSvg.addEventListener("click", function (event) {
            var group = event.target.closest(".cm-dot-g");
            if (group) {
                openRow(group.dataset.slug);
            }
        });
        plotSvg.addEventListener("keydown", function (event) {
            var group = event.target.closest(".cm-dot-g");
            if (group && (event.key === "Enter" || event.key === " ")) {
                event.preventDefault();
                openRow(group.dataset.slug);
            }
        });
    }

    // ---------- events & URL ----------

    function bindGlobal() {
        document.querySelectorAll('input[name="cm-mode"]').forEach(function (input) {
            input.checked = input.value === mode;
            input.addEventListener("change", function () {
                if (input.checked) {
                    mode = input.value;
                    render();
                }
            });
        });

        showAllButton.addEventListener("click", function () {
            showAll = !showAll;
            renderTable();
        });

        listHost.addEventListener("click", function (event) {
            var button = event.target.closest(".cm-row-toggle");
            if (!button) {
                return;
            }
            openSlug = openSlug === button.dataset.slug ? null : button.dataset.slug;
            renderTable();
            renderPlot();
            var again = listHost.querySelector('.cm-row-toggle[data-slug="' + button.dataset.slug + '"]');
            if (again) {
                again.focus();
            }
        });

        verdictHost.addEventListener("click", function (event) {
            if (event.target.closest("#cm-share")) {
                copyLink();
            }
        });
    }

    function scheduleUrlUpdate() {
        window.clearTimeout(urlTimer);
        urlTimer = window.setTimeout(writeUrl, 200);
    }

    function writeUrl() {
        var next = new URLSearchParams();
        next.set("a", scorer.weightVector(weights.a));
        next.set("b", scorer.weightVector(weights.b));
        if (names.a !== DEFAULT_NAMES.a) {
            next.set("na", names.a);
        }
        if (names.b !== DEFAULT_NAMES.b) {
            next.set("nb", names.b);
        }
        if (mode === "fair") {
            next.set("rank", "fair");
        }
        window.history.replaceState({}, document.title, window.location.pathname + "?" + next.toString());
    }

    async function copyLink() {
        writeUrl();
        var status = document.getElementById("cm-share-status");
        var copied = false;
        try {
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(window.location.href);
                copied = true;
            }
        } catch (_) {
            copied = false;
        }
        if (status) {
            status.textContent = copied
                ? "Link copied. They'll see both sides and can adjust theirs."
                : "Copy the link from your browser's address bar.";
            window.setTimeout(function () { status.textContent = ""; }, 3500);
        }
    }

    // ---------- helpers ----------

    function fillRange(input) {
        input.style.setProperty("--sm-range-fill", input.value + "%");
    }

    function cleanName(value) {
        return String(value || "").replace(/[<>]/g, "").trim().slice(0, 18);
    }

    function joinList(items) {
        if (items.length <= 1) {
            return items.join("");
        }
        return items.slice(0, -1).join(", ") + " and " + items[items.length - 1];
    }
})();
