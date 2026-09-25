// Take-Home Pay by State (/tools/take-home-pay-by-state).
// Every state is recalculated in the browser on each input change: gross pay → federal,
// FICA and state income tax on 2025 brackets → take-home, then housing, sales tax and
// property tax → money left. Inputs round-trip through the URL so results can be shared.
(function () {
    "use strict";

    var payload = window.takeHomeData;
    if (!payload || !Array.isArray(payload.places) || !payload.taxes || !payload.taxes.federal) {
        return;
    }

    var places = payload.places;
    var taxes = payload.taxes;
    var occupations = (payload.occupations && payload.occupations.occupations) || {};
    var placeByAbbr = new Map(places.map(function (place) { return [place.abbreviation, place]; }));

    var DEFAULTS = {
        mode: "salary",
        income: 75000,
        job: Object.keys(occupations)[0] || "",
        status: "single",
        housing: "rent",
        payment: 1800,
        price: 350000,
        home: "",
        spend: 20,
        period: "year",
        sort: "left",
        view: "table"
    };
    var MAX_MONEY = 10000000;

    var el = {
        salaryField: document.getElementById("th-salary-field"),
        salary: document.getElementById("th-salary"),
        jobField: document.getElementById("th-job-field"),
        job: document.getElementById("th-job"),
        housingHint: document.getElementById("th-housing-hint"),
        paymentField: document.getElementById("th-payment-field"),
        paymentLabel: document.getElementById("th-payment-label"),
        payment: document.getElementById("th-payment"),
        priceField: document.getElementById("th-price-field"),
        price: document.getElementById("th-price"),
        home: document.getElementById("th-home"),
        spend: document.getElementById("th-spend"),
        spendOut: document.getElementById("th-spend-out"),
        sort: document.getElementById("th-sort"),
        search: document.getElementById("th-search"),
        list: document.getElementById("th-list"),
        count: document.getElementById("th-count"),
        stats: document.getElementById("th-stats"),
        kicker: document.getElementById("th-table-kicker"),
        reset: document.getElementById("th-reset"),
        share: document.getElementById("th-share"),
        status: document.getElementById("th-status"),
        chips: document.querySelectorAll(".smt-chips [data-income]"),
        map: document.getElementById("th-map"),
        tableView: document.getElementById("th-table-view"),
        tiles: document.getElementById("th-tilegrid"),
        legendLow: document.getElementById("th-legend-low"),
        legendHigh: document.getElementById("th-legend-high"),
        results: document.querySelector(".smt-results"),
        mobileBar: document.getElementById("th-mobilebar"),
        mobileBarText: document.getElementById("th-mobilebar-text")
    };
    var core = window.StateMatchCore;

    var state = readStateFromUrl();
    var rows = [];
    var openAbbr = null;
    var urlTimer = null;

    populateJobs();
    buildTiles();
    writeStateToForm();
    bindForm();
    bindMobileBar();
    render();

    // ---------- state ----------

    function readStateFromUrl() {
        var params = new URL(window.location.href).searchParams;
        var result = Object.assign({}, DEFAULTS);

        pick(params.get("mode"), ["salary", "job"], "mode");
        pick(params.get("status"), ["single", "married"], "status");
        pick(params.get("housing"), ["rent", "fixed", "own"], "housing");
        pick(params.get("period"), ["year", "month"], "period");
        pick(params.get("sort"), ["left", "net", "tax"], "sort");
        pick(params.get("view"), ["table", "map"], "view");
        if (params.get("job") && occupations[params.get("job")]) {
            result.job = params.get("job");
        }
        if (params.get("home") && placeByAbbr.has(params.get("home").toUpperCase())) {
            result.home = params.get("home").toUpperCase();
        }
        ["income", "payment", "price"].forEach(function (key) {
            var value = Number(params.get(key));
            if (params.has(key) && Number.isFinite(value)) {
                result[key] = clamp(Math.round(value), 0, MAX_MONEY);
            }
        });
        var spend = Number(params.get("spend"));
        if (params.has("spend") && Number.isFinite(spend)) {
            result.spend = clamp(Math.round(spend / 5) * 5, 0, 50);
        }
        return result;

        function pick(value, allowed, key) {
            if (value && allowed.indexOf(value) !== -1) {
                result[key] = value;
            }
        }
    }

    function scheduleUrlUpdate() {
        window.clearTimeout(urlTimer);
        urlTimer = window.setTimeout(writeStateToUrl, 200);
    }

    function writeStateToUrl() {
        var params = new URLSearchParams();
        Object.keys(DEFAULTS).forEach(function (key) {
            if (key === "job" && state.mode !== "job") {
                return;
            }
            if (key === "income" && state.mode !== "salary") {
                return;
            }
            if ((key === "payment" && state.housing === "rent") || (key === "price" && state.housing !== "own")) {
                return;
            }
            if (state[key] !== DEFAULTS[key]) {
                params.set(key, String(state[key]));
            }
        });
        var query = params.toString();
        window.history.replaceState({}, document.title, window.location.pathname + (query ? "?" + query : ""));
    }

    // ---------- form ----------

    function populateJobs() {
        el.job.innerHTML = Object.keys(occupations).map(function (key) {
            return '<option value="' + escapeHtml(key) + '">' + escapeHtml(occupations[key].label || key) + '</option>';
        }).join("");
        if (!Object.keys(occupations).length) {
            var jobRadio = document.querySelector('input[name="th-pay-mode"][value="job"]');
            if (jobRadio) {
                jobRadio.closest("label").hidden = true;
            }
        }
    }

    function writeStateToForm() {
        setRadio("th-pay-mode", state.mode);
        setRadio("th-status", state.status);
        setRadio("th-housing", state.housing);
        setRadio("th-period", state.period);
        setRadio("th-view", state.view);
        el.salary.value = formatPlain(state.income);
        el.job.value = state.job;
        el.payment.value = formatPlain(state.payment);
        el.price.value = formatPlain(state.price);
        el.home.value = state.home;
        el.spend.value = state.spend;
        el.sort.value = state.sort;
        syncFormVisibility();
        syncSpend();
    }

    function syncFormVisibility() {
        el.salaryField.hidden = state.mode !== "salary";
        el.jobField.hidden = state.mode !== "job";
        el.paymentField.hidden = state.housing === "rent";
        el.priceField.hidden = state.housing !== "own";
        el.map.hidden = state.view !== "map";
        el.tableView.hidden = state.view !== "table";
        el.chips.forEach(function (chip) {
            var active = Number(chip.dataset.income) === state.income;
            chip.classList.toggle("is-active", active);
            chip.setAttribute("aria-pressed", String(active));
        });
        el.paymentLabel.textContent = state.housing === "own" ? "Monthly mortgage payment" : "Monthly rent";
        el.housingHint.textContent = {
            rent: "Each state's average monthly rent.",
            fixed: "The same rent in every state.",
            own: "Your mortgage payment, plus property tax at each state's rate."
        }[state.housing];
    }

    function syncSpend() {
        el.spendOut.textContent = state.spend + "%";
        el.spend.style.setProperty("--sm-range-fill", (state.spend / 50 * 100) + "%");
    }

    function bindForm() {
        document.getElementById("th-form").addEventListener("submit", function (event) {
            event.preventDefault();
        });

        bindRadio("th-pay-mode", "mode");
        bindRadio("th-status", "status");
        bindRadio("th-housing", "housing");
        bindRadio("th-period", "period");
        bindRadio("th-view", "view");

        el.chips.forEach(function (chip) {
            chip.addEventListener("click", function () {
                state.income = Number(chip.dataset.income);
                el.salary.value = formatPlain(state.income);
                syncFormVisibility();
                update();
            });
        });

        el.tiles.addEventListener("click", function (event) {
            var tile = event.target.closest(".th-tile");
            if (!tile || tile.classList.contains("is-missing")) {
                return;
            }
            openAbbr = tile.dataset.abbr;
            state.view = "table";
            setRadio("th-view", "table");
            el.search.value = "";
            syncFormVisibility();
            update();
            var row = el.list.querySelector('.th-row-toggle[data-abbr="' + openAbbr + '"]');
            if (row) {
                row.scrollIntoView({ block: "nearest" });
                row.focus({ preventScroll: true });
            }
        });

        bindMoney(el.salary, "income");
        bindMoney(el.payment, "payment");
        bindMoney(el.price, "price");

        el.job.addEventListener("change", function () {
            state.job = el.job.value;
            update();
        });
        el.home.addEventListener("change", function () {
            state.home = el.home.value;
            update();
        });
        el.spend.addEventListener("input", function () {
            state.spend = Number(el.spend.value);
            syncSpend();
            update();
        });
        el.sort.addEventListener("change", function () {
            state.sort = el.sort.value;
            update();
        });
        el.search.addEventListener("input", renderTable);

        el.reset.addEventListener("click", function () {
            state = Object.assign({}, DEFAULTS);
            openAbbr = null;
            el.search.value = "";
            writeStateToForm();
            update();
        });
        el.share.addEventListener("click", copyLink);

        el.list.addEventListener("click", function (event) {
            var button = event.target.closest(".th-row-toggle");
            if (!button) {
                return;
            }
            var abbr = button.dataset.abbr;
            openAbbr = openAbbr === abbr ? null : abbr;
            renderTable();
            var reopened = el.list.querySelector('.th-row-toggle[data-abbr="' + abbr + '"]');
            if (reopened) {
                reopened.focus();
            }
        });
    }

    function bindRadio(name, key) {
        document.querySelectorAll('input[name="' + name + '"]').forEach(function (input) {
            input.addEventListener("change", function () {
                if (!input.checked) {
                    return;
                }
                state[key] = input.value;
                syncFormVisibility();
                update();
            });
        });
    }

    function bindMoney(input, key) {
        input.addEventListener("input", function () {
            state[key] = parseMoney(input.value);
            syncFormVisibility();
            update();
        });
        input.addEventListener("blur", function () {
            input.value = formatPlain(state[key]);
        });
        input.addEventListener("focus", function () {
            input.select();
        });
    }

    function setRadio(name, value) {
        var input = document.querySelector('input[name="' + name + '"][value="' + value + '"]');
        if (input) {
            input.checked = true;
        }
    }

    function update() {
        render();
        scheduleUrlUpdate();
    }

    // ---------- math ----------

    // Tax math lives in state-match-core.js so State Match's "Money check" matches this tool.
    // Function declarations (not `var` aliases) so they exist when init runs above.
    var paycheckMath = null;
    function paycheck() {
        return paycheckMath || (paycheckMath = window.StateMatchCore.createPaycheck(taxes));
    }
    function federalTax(gross, status) { return paycheck().federalTax(gross, status); }
    function payrollTax(gross, status) { return paycheck().payrollTax(gross, status); }
    function stateIncomeTax(abbr, gross, status) { return paycheck().stateIncomeTax(abbr, gross, status); }

    function compute() {
        var occupation = state.mode === "job" ? occupations[state.job] : null;
        var skipped = [];
        var result = [];

        places.forEach(function (place) {
            var gross = state.income;
            if (occupation) {
                gross = occupation.byState ? occupation.byState[place.abbreviation] : null;
                if (gross == null) {
                    skipped.push(place.name);
                    return;
                }
            }

            var stateTax = stateIncomeTax(place.abbreviation, gross, state.status);
            var housing = state.housing === "rent" ? (place.averageRent != null ? place.averageRent * 12 : null) : state.payment * 12;
            if (stateTax == null || housing == null) {
                skipped.push(place.name);
                return;
            }

            var federal = federalTax(gross, state.status);
            var payroll = payrollTax(gross, state.status);
            var net = gross - federal - payroll - stateTax;
            var property = state.housing === "own" && place.propertyTaxRate != null ? state.price * place.propertyTaxRate / 100 : 0;
            var sales = Math.max(0, net) * state.spend / 100 * (place.salesTaxCombinedRate || 0) / 100;

            result.push({
                place: place,
                gross: gross,
                federal: federal,
                payroll: payroll,
                stateIncome: stateTax,
                sales: sales,
                property: property,
                stateTaxes: stateTax + sales + property,
                net: net,
                housing: housing,
                left: net - housing - sales - property
            });
        });

        var sortKey = state.sort;
        result.sort(function (a, b) {
            if (sortKey === "tax") {
                return a.stateTaxes - b.stateTaxes || a.place.name.localeCompare(b.place.name);
            }
            return b[sortKey] - a[sortKey] || a.place.name.localeCompare(b.place.name);
        });
        result.forEach(function (row, index) { row.rank = index + 1; });

        return { rows: result, skipped: skipped, occupation: occupation };
    }

    // ---------- render ----------

    function render() {
        var computed = compute();
        rows = computed.rows;
        el.kicker.textContent = {
            left: "Ranked by money left",
            net: "Ranked by take-home pay",
            tax: "Ranked by lowest state taxes"
        }[state.sort];
        renderStats(computed);
        renderTable();
        renderTiles();
    }

    function metricFor(row) {
        return state.sort === "tax" ? row.stateTaxes : row[state.sort];
    }

    function metricLabel() {
        return { left: "left over", net: "take-home", tax: "in state taxes" }[state.sort];
    }

    // Four slices of gross pay. Only the last three change from state to state — the federal
    // slice is there so the bar adds up to the whole paycheck.
    function segments(row) {
        return [
            { key: "fed", label: "Federal & payroll tax", value: row.federal + row.payroll },
            { key: "state", label: "State taxes", value: row.stateIncome + row.sales + row.property },
            { key: "housing", label: state.housing === "own" ? "Mortgage" : "Rent", value: row.housing },
            { key: "left", label: "Left over", value: Math.max(0, row.left) }
        ];
    }

    function segmentTotal(row) {
        return segments(row).reduce(function (sum, part) { return sum + part.value; }, 0);
    }

    function stackBar(row, scaleMax, extraClass) {
        return '<span class="th-stack ' + (extraClass || "") + '" role="img" aria-label="' + escapeHtml(row.place.name + ": " +
                segments(row).map(function (part) { return part.label + " " + formatMoney(perPeriod(part.value)); }).join(", ")) + '">' +
            segments(row).map(function (part) {
                var width = scaleMax > 0 ? part.value / scaleMax * 100 : 0;
                return width > 0
                    ? '<i class="th-seg th-seg-' + part.key + '" style="width:' + width.toFixed(2) + '%" title="' + escapeHtml(part.label + ": " + formatMoney(perPeriod(part.value))) + '"></i>'
                    : "";
            }).join("") +
            '</span>';
    }

    function legendHtml() {
        return '<ul class="th-legend">' + segments(rows[0]).map(function (part) {
            return '<li><i class="th-seg-' + part.key + '"></i>' + escapeHtml(part.label) + '</li>';
        }).join("") + '</ul>';
    }

    function renderStats(computed) {
        if (!rows.length) {
            el.stats.innerHTML = '<p class="smt-empty">No state has data for this combination. Try a different job or housing option.</p>';
            el.mobileBarText.textContent = "";
            return;
        }

        var best = rows[0];
        var worst = rows[rows.length - 1];
        var homeRow = state.home ? rows.find(function (row) { return row.place.abbreviation === state.home; }) : null;
        var compare = [{ tag: "Best", row: best }];
        if (homeRow && homeRow !== best && homeRow !== worst) {
            compare.push({ tag: "Yours", row: homeRow });
        }
        if (worst !== best) {
            compare.push({ tag: homeRow === worst ? "Yours · last" : "Worst", row: worst });
        }
        var scaleMax = Math.max.apply(null, compare.map(function (item) { return segmentTotal(item.row); }));
        var payLabel = computed.occupation
            ? "a " + computed.occupation.label.toLowerCase() + "'s pay"
            : "your " + formatMoney(perPeriod(state.income));

        var compareRows = compare.map(function (item) {
            var isHome = homeRow === item.row;
            return '<div class="th-cmp-row' + (isHome ? " is-home" : "") + '">' +
                '<span class="th-cmp-state">' +
                    '<span class="th-cmp-tag">' + item.tag + '</span>' +
                    '<img src="' + escapeHtml(item.row.place.flagImageUrl) + '" alt="" width="30" height="20">' +
                    '<strong>' + escapeHtml(item.row.place.name) + '</strong>' +
                '</span>' +
                stackBar(item.row, scaleMax, "th-stack-lg") +
                '<span class="th-cmp-left"><b class="' + (item.row.left < 0 ? "is-negative" : "") + '">' + formatMoney(perPeriod(item.row.left)) + '</b><small>left over</small></span>' +
                '</div>';
        }).join("");

        var notes = [];
        if (computed.occupation) {
            notes.push("Using " + escapeHtml(computed.occupation.label.toLowerCase()) + " " + escapeHtml(computed.occupation.wageType || "average") +
                " pay by state (" + escapeHtml(computed.occupation.source) + ").");
        }
        if (computed.skipped.length) {
            notes.push(computed.skipped.length + " excluded for missing data: " + escapeHtml(computed.skipped.join(", ")) + ".");
        }

        el.mobileBarText.innerHTML = '<b>#1 ' + escapeHtml(best.place.name) + '</b> ' + formatMoney(perPeriod(best.left)) + ' left' +
            (homeRow ? ' · ' + escapeHtml(homeRow.place.abbreviation) + ' #' + homeRow.rank : '');

        el.stats.innerHTML =
            '<section class="th-compare" aria-labelledby="th-compare-title">' +
                '<div class="th-compare-head">' +
                    '<div>' +
                        '<p class="sm-section-kicker">' + (state.period === "month" ? "Per month" : "Per year") + '</p>' +
                        '<h2 id="th-compare-title">Where ' + escapeHtml(payLabel) + ' goes</h2>' +
                    '</div>' +
                    legendHtml() +
                '</div>' +
                '<div class="th-cmp-rows">' + compareRows + '</div>' +
                '<p class="th-insight">' + insightHtml(best, worst, homeRow) + '</p>' +
                (notes.length ? '<p class="th-compare-note">' + notes.join(" ") + '</p>' : "") +
            '</section>';
    }

    // One plain-language sentence saying which slices account for the gap.
    function insightHtml(best, worst, homeRow) {
        var from = homeRow && homeRow !== best ? homeRow : worst;
        if (from === best) {
            return homeRow
                ? '<i class="fa-solid fa-trophy" aria-hidden="true"></i> ' + escapeHtml(homeRow.place.name) + ' already leaves you the most. Nowhere else comes out ahead.'
                : "";
        }
        var gap = best.left - from.left;
        var parts = [];
        var housingDiff = from.housing - best.housing;
        var taxDiff = from.stateTaxes - best.stateTaxes;
        if (Math.abs(housingDiff) >= 100) {
            parts.push(formatMoney(perPeriod(Math.abs(housingDiff))) + (housingDiff > 0 ? " less" : " more") + " on " + (state.housing === "own" ? "housing" : "rent"));
        }
        if (Math.abs(taxDiff) >= 100) {
            parts.push(formatMoney(perPeriod(Math.abs(taxDiff))) + (taxDiff > 0 ? " less" : " more") + " in state taxes");
        }
        if (state.mode === "job" && Math.abs(best.gross - from.gross) >= 100) {
            parts.push(formatMoney(perPeriod(Math.abs(best.gross - from.gross))) + (best.gross > from.gross ? " higher" : " lower") + " pay");
        }
        var lead = homeRow && homeRow !== best
            ? "Moving from " + escapeHtml(from.place.name) + " to <strong>" + escapeHtml(best.place.name) + "</strong> leaves you "
            : "<strong>" + escapeHtml(best.place.name) + "</strong> leaves you ";
        var tail = homeRow ? "" : " than " + escapeHtml(from.place.name);
        return '<i class="fa-solid fa-arrow-trend-up" aria-hidden="true"></i> ' + lead +
            '<strong class="th-insight-gap">' + formatMoney(perPeriod(gap)) + ' more</strong> ' +
            (state.period === "month" ? "a month" : "a year") + tail +
            (parts.length ? ": " + joinParts(parts) + "." : ".") +
            (homeRow ? "" : ' <span class="th-insight-hint">Pick the state you live in now to compare it.</span>');
    }

    function joinParts(parts) {
        return parts.length > 1 ? parts.slice(0, -1).join(", ") + " and " + parts[parts.length - 1] : parts[0];
    }

    function renderTable() {
        var query = el.search.value.trim().toLowerCase();
        var homeRow = state.home ? rows.find(function (row) { return row.place.abbreviation === state.home; }) : null;
        var visible = rows.filter(function (row) {
            return !query || row.place.name.toLowerCase().indexOf(query) !== -1 || row.place.abbreviation.toLowerCase() === query;
        });

        el.count.textContent = query ? visible.length + " of " + rows.length : rows.length + " states";

        if (!visible.length) {
            el.list.innerHTML = '<p class="smt-empty">' + (query ? "No state matches that search." : "No results.") + '</p>';
            return;
        }

        // One shared scale for every row, so bar length itself compares pay across states.
        var scaleMax = Math.max.apply(null, rows.map(segmentTotal));

        el.list.innerHTML = visible.map(function (row) {
            var abbr = row.place.abbreviation;
            var isOpen = openAbbr === abbr;
            var isHome = homeRow && homeRow === row;
            var delta = homeRow && !isHome ? row.left - homeRow.left : null;
            var deltaHtml = delta == null ? "" :
                '<small class="' + (delta >= 0 ? "is-up" : "is-down") + '">' + (delta >= 0 ? "+" : "−") + formatMoney(Math.abs(perPeriod(delta))) + ' vs. ' + escapeHtml(homeRow.place.abbreviation) + '</small>';

            return '<div class="th-item' + (isOpen ? " is-open" : "") + (isHome ? " is-home" : "") + '">' +
                '<button type="button" class="th-row th-row-toggle" data-abbr="' + escapeHtml(abbr) + '" aria-expanded="' + isOpen + '" aria-controls="th-detail-' + escapeHtml(abbr) + '">' +
                '<span class="th-rank">' + row.rank + '</span>' +
                '<span class="th-state"><img src="' + escapeHtml(row.place.flagImageUrl) + '" alt="" width="30" height="20" loading="lazy">' +
                    '<span><strong>' + escapeHtml(row.place.name) + '</strong>' + (isHome ? '<em>Your state</em>' : '<small>' + formatMoney(perPeriod(row.stateTaxes)) + ' state tax</small>') + '</span></span>' +
                '<span class="th-row-bar">' + stackBar(row, scaleMax) + '</span>' +
                '<span class="th-num th-left"><b class="' + (row.left < 0 ? "is-negative" : "") + '">' + formatMoney(perPeriod(row.left)) + '</b>' + deltaHtml + '</span>' +
                '</button>' +
                (isOpen ? renderDetail(row) : "") +
                '</div>';
        }).join("");
    }

    function renderDetail(row) {
        var place = row.place;
        var effective = row.gross > 0 ? ((row.federal + row.payroll + row.stateIncome) / row.gross * 100).toFixed(1) : "0.0";
        var lines = [
            ["Gross pay", row.gross, ""],
            ["Federal income tax", -row.federal, ""],
            ["Social Security + Medicare", -row.payroll, ""],
            ["State income tax", -row.stateIncome, row.stateIncome === 0 ? "No state tax on wages" : ""],
            ["Take-home pay", row.net, "is-subtotal"],
            [state.housing === "own" ? "Mortgage" : "Rent", -row.housing, state.housing === "rent" ? formatMoney(place.averageRent) + "/mo average" : ""],
            ["Sales tax (est.)", -row.sales, (place.salesTaxCombinedRate || 0).toFixed(2) + "% combined rate"],
            ["Property tax", -row.property, state.housing === "own" ? (place.propertyTaxRate != null ? place.propertyTaxRate.toFixed(2) + "% effective rate" : "No rate on file") : "Renting"],
            ["Left over", row.left, "is-total"]
        ];

        return '<div class="th-detail" id="th-detail-' + escapeHtml(place.abbreviation) + '">' +
            '<dl class="th-breakdown">' +
            lines.map(function (line) {
                var cls = line[2] === "is-subtotal" || line[2] === "is-total" ? line[2] : "";
                var note = cls ? "" : line[2];
                return '<div class="' + cls + '"><dt>' + line[0] + (note ? '<small>' + escapeHtml(note) + '</small>' : '') + '</dt>' +
                    '<dd>' + formatMoney(perPeriod(line[1])) + '</dd></div>';
            }).join("") +
            '</dl>' +
            '<div class="th-detail-side">' +
            '<p><strong>' + effective + '%</strong> of pay goes to income and payroll taxes in ' + escapeHtml(place.name) + '.</p>' +
            '<a href="/states/' + encodeURIComponent(place.slug) + '/living">' + escapeHtml(place.name) + ' living guide <i class="fa-solid fa-arrow-right" aria-hidden="true"></i></a>' +
            '<a href="/state-match?state=' + encodeURIComponent(place.slug) + '">See it in State Match <i class="fa-solid fa-arrow-right" aria-hidden="true"></i></a>' +
            '</div>' +
            '</div>';
    }

    function valueRange(key) {
        var values = rows.map(function (row) { return key === "tax" ? row.stateTaxes : row[key]; });
        return { min: Math.min.apply(null, values), max: Math.max.apply(null, values) };
    }

    function buildTiles() {
        if (!core) {
            return;
        }
        el.tiles.innerHTML = places.map(function (place) {
            var position = core.STATE_GRID[place.abbreviation];
            if (!position) {
                return "";
            }
            return '<button type="button" class="sm-tile th-tile" data-abbr="' + escapeHtml(place.abbreviation) + '"' +
                ' style="grid-column:' + position[0] + ';grid-row:' + position[1] + '">' +
                '<span class="sm-tile-code">' + escapeHtml(place.abbreviation) + '</span>' +
                '<span class="sm-tile-score">-</span></button>';
        }).join("");
    }

    // Colors are scaled to the current spread (not $0 to max) so a $3k gap still reads on the map.
    function renderTiles() {
        if (!core || !rows.length) {
            return;
        }
        var key = state.sort;
        var range = valueRange(key);
        var byAbbr = new Map(rows.map(function (row) { return [row.place.abbreviation, row]; }));
        el.legendLow.textContent = key === "tax" ? "More tax" : "Less";
        el.legendHigh.textContent = key === "tax" ? "Less tax" : "More";

        el.tiles.querySelectorAll(".th-tile").forEach(function (tile) {
            var row = byAbbr.get(tile.dataset.abbr);
            var scoreEl = tile.querySelector(".sm-tile-score");
            tile.classList.toggle("is-missing", !row);
            tile.classList.toggle("is-home", Boolean(row && row.place.abbreviation === state.home));
            if (!row) {
                tile.style.background = "";
                tile.classList.remove("is-strong");
                scoreEl.textContent = "n/a";
                tile.title = "No data";
                return;
            }
            var value = key === "tax" ? row.stateTaxes : row[key];
            var t = range.max > range.min ? (value - range.min) / (range.max - range.min) : 1;
            if (key === "tax") {
                t = 1 - t;
            }
            var color = core.matchColor(12 + t * 88);
            tile.style.background = color.fill;
            tile.classList.toggle("is-strong", color.isDark);
            scoreEl.textContent = compactMoney(perPeriod(value));
            tile.title = row.place.name + ": " + formatMoney(perPeriod(value)) + " " + metricLabel() + " (#" + row.rank + ")";
            tile.setAttribute("aria-label", tile.title);
        });
    }

    // Phones stack the form above the results, so keep the headline result in a bottom bar
    // until the results themselves scroll into view.
    function bindMobileBar() {
        var ticking = false;
        function check() {
            ticking = false;
            var rect = el.results.getBoundingClientRect();
            var show = window.innerWidth <= 760 && rect.top > window.innerHeight - 40;
            el.mobileBar.classList.toggle("is-visible", show);
        }
        window.addEventListener("scroll", function () {
            if (!ticking) {
                ticking = true;
                window.requestAnimationFrame(check);
            }
        }, { passive: true });
        window.addEventListener("resize", check);
        el.mobileBar.querySelector("a").addEventListener("click", function (event) {
            event.preventDefault();
            el.results.scrollIntoView({ behavior: "smooth", block: "start" });
        });
        check();
    }

    function compactMoney(value) {
        var abs = Math.abs(value);
        var text = abs >= 1000
            ? "$" + (abs / 1000).toFixed(abs >= 100000 ? 0 : 1).replace(/\.0$/, "") + "k"
            : "$" + Math.round(abs);
        return (value < 0 ? "\u2212" : "") + text;
    }

    // ---------- helpers ----------

    function perPeriod(value) {
        return state.period === "month" ? value / 12 : value;
    }

    function parseMoney(text) {
        var digits = String(text).replace(/[^0-9.]/g, "");
        var value = Math.round(Number(digits) || 0);
        return clamp(value, 0, MAX_MONEY);
    }

    function formatPlain(value) {
        return Math.round(value).toLocaleString("en-US");
    }

    function formatMoney(value) {
        var rounded = Math.round(value);
        return (rounded < 0 ? "−$" : "$") + Math.abs(rounded).toLocaleString("en-US");
    }

    function clamp(value, min, max) {
        return Math.min(max, Math.max(min, value));
    }

    function escapeHtml(value) {
        return String(value == null ? "" : value).replace(/[&<>"']/g, function (character) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;" }[character];
        });
    }

    async function copyLink() {
        writeStateToUrl();
        var copied = false;
        try {
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(window.location.href);
                copied = true;
            }
        } catch (_) {
            copied = false;
        }
        el.status.textContent = copied ? "Link copied." : "Copy the link from your browser's address bar.";
        window.setTimeout(function () { el.status.textContent = ""; }, 2500);
    }
})();
