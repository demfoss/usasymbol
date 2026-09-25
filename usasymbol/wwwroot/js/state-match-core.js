// Shared pieces of State Match used by /state-match and the tools built on top of it
// (/tools/where-should-we-move, /tools/take-home-pay-by-state), so presets, the tile-grid
// layout, and the normalize → weight → score math can't drift between pages.
(function () {
    "use strict";

    var PRESET_VALUES = {
        balanced: {},
        budget: { cost: 100, housing: 95, incometax: 35, propertytax: 25, salestax: 25, jobs: 52, income: 36, safety: 45, health: 30, education: 25, warmth: 20 },
        career: { income: 100, jobs: 96, education: 68, cost: 42, housing: 35, safety: 35, health: 40, incometax: 15, propertytax: 10, salestax: 10, warmth: 15 },
        family: { safety: 100, education: 95, health: 88, housing: 62, cost: 60, jobs: 55, income: 48, incometax: 15, propertytax: 10, salestax: 10, warmth: 20 },
        warm: { warmth: 100, cost: 58, housing: 52, jobs: 45, safety: 42, health: 40, incometax: 15, propertytax: 10, salestax: 10, income: 30, education: 25 }
    };

    var PRESET_LABELS = {
        balanced: "Balanced",
        budget: "Budget",
        career: "Career",
        family: "Family",
        warm: "Warmth"
    };

    // Grid position (column, row — 1-indexed) for each state on a 12x8 tile grid,
    // approximating real geography. Source layout: github.com/kristw/gridmap-layout-usa
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

    // Paper at score 0, dark teal-ink at score 100 — same scale as the State Match map.
    var MATCH_LOW = { r: 0xf5, g: 0xf1, b: 0xe8 };
    var MATCH_HIGH = { r: 0x12, g: 0x33, b: 0x3a };

    // Slider value -> scoring weight. Squared so a priority you push to 100 really dominates:
    // 100 vs 15 is 44x, not 6.7x — with a straight average of 11 metrics, states that are
    // decent at everything (New Hampshire) stayed #1 no matter which slider moved.
    // Keep in sync with StateMatchService.EffectiveWeight on the server.
    function effectiveWeight(slider) {
        var value = Number(slider) || 0;
        return value > 0 ? value * value / 100 : 0;
    }

    function clampNumber(value, min, max) {
        var numeric = Number(value);
        return Math.min(max, Math.max(min, Number.isFinite(numeric) ? numeric : min));
    }

    function escapeHtml(value) {
        return String(value == null ? "" : value).replace(/[&<>"']/g, function (character) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;" }[character];
        });
    }

    function relativeLuminance(r, g, b) {
        var channel = function (value) {
            var normalized = value / 255;
            return normalized <= 0.03928 ? normalized / 12.92 : Math.pow((normalized + 0.055) / 1.055, 2.4);
        };
        return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);
    }

    function matchColor(score) {
        var t = clampNumber(score, 0, 100) / 100;
        var r = Math.round(MATCH_LOW.r + (MATCH_HIGH.r - MATCH_LOW.r) * t);
        var g = Math.round(MATCH_LOW.g + (MATCH_HIGH.g - MATCH_LOW.g) * t);
        var b = Math.round(MATCH_LOW.b + (MATCH_HIGH.b - MATCH_LOW.b) * t);
        return { fill: "rgb(" + r + "," + g + "," + b + ")", isDark: relativeLuminance(r, g, b) < 0.42 };
    }

    /** Builds the scoring helpers for one places + metrics payload. */
    function createScorer(places, metrics) {
        var metricByKey = new Map(metrics.map(function (metric) { return [metric.key, metric]; }));
        var ranges = new Map();

        metrics.forEach(function (metric) {
            var values = [];
            places.forEach(function (place) {
                place.metrics.forEach(function (value) {
                    if (value.key === metric.key) {
                        values.push(value.raw);
                    }
                });
            });
            ranges.set(metric.key, {
                min: values.length ? Math.min.apply(null, values) : 0,
                max: values.length ? Math.max.apply(null, values) : 0
            });
        });

        function normalizedValue(metricValue) {
            var range = ranges.get(metricValue.key);
            if (!range || range.max <= range.min) {
                return 0.5;
            }
            var normalized = (metricValue.raw - range.min) / (range.max - range.min);
            return metricValue.direction >= 0 ? normalized : 1 - normalized;
        }

        function score(place, weightSet) {
            var weightedTotal = 0;
            var usedWeight = 0;
            place.metrics.forEach(function (metricValue) {
                var weight = effectiveWeight(weightSet[metricValue.key]);
                if (weight <= 0) {
                    return;
                }
                weightedTotal += normalizedValue(metricValue) * weight;
                usedWeight += weight;
            });
            return usedWeight > 0 ? Math.round((weightedTotal / usedWeight) * 100) : 0;
        }

        function defaults() {
            var result = {};
            metrics.forEach(function (metric) {
                result[metric.key] = clampNumber(metric.defaultWeight, 0, 100);
            });
            return result;
        }

        function preset(name) {
            var result = defaults();
            var values = PRESET_VALUES[name] || {};
            Object.keys(values).forEach(function (key) {
                if (metricByKey.has(key)) {
                    result[key] = clampNumber(values[key], 0, 100);
                }
            });
            return result;
        }

        function matchingPreset(weightSet) {
            return Object.keys(PRESET_VALUES).find(function (name) {
                var candidate = preset(name);
                return metrics.every(function (metric) { return weightSet[metric.key] === candidate[metric.key]; });
            }) || null;
        }

        /** Parses the "cost:15,income:40,..." vector used by ?w=, ?a= and ?b= on top of the defaults. */
        function parseWeightVector(encoded) {
            var result = defaults();
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

        function weightVector(weightSet) {
            return metrics.map(function (metric) { return metric.key + ":" + weightSet[metric.key]; }).join(",");
        }

        return {
            metricByKey: metricByKey,
            ranges: ranges,
            normalizedValue: normalizedValue,
            score: score,
            defaults: defaults,
            preset: preset,
            matchingPreset: matchingPreset,
            parseWeightVector: parseWeightVector,
            weightVector: weightVector
        };
    }

    /**
     * Paycheck math over Content/state-match/state-income-tax-2025.json — shared by the
     * Take-Home Pay tool and State Match's "Money check" so both give the same number.
     */
    function createPaycheck(taxes) {
        function bracketTax(taxable, brackets) {
            var tax = 0;
            for (var i = 0; i < brackets.length; i++) {
                var floor = brackets[i][0];
                var ceiling = i + 1 < brackets.length ? brackets[i + 1][0] : Infinity;
                if (taxable <= floor) {
                    break;
                }
                tax += (Math.min(taxable, ceiling) - floor) * brackets[i][1] / 100;
            }
            return tax;
        }

        function phasedAmount(amount, phaseout, gross) {
            if (!amount) {
                return 0;
            }
            if (!phaseout) {
                return amount;
            }
            return Math.max(0, amount - Math.max(0, gross - phaseout.start) * phaseout.rate / 100);
        }

        function federalTax(gross, status) {
            var schedule = taxes.federal[status];
            return bracketTax(Math.max(0, gross - schedule.deduction), schedule.brackets);
        }

        function payrollTax(gross, status) {
            var fed = taxes.federal;
            var socialSecurity = Math.min(gross, fed.socialSecurityWageBase) * fed.socialSecurityRate / 100;
            var medicare = gross * fed.medicareRate / 100 +
                Math.max(0, gross - fed.additionalMedicareThreshold[status]) * fed.additionalMedicareRate / 100;
            return socialSecurity + medicare;
        }

        /** Null when the state has no schedule on file; 0 when it doesn't tax wages. */
        function stateIncomeTax(abbr, gross, status) {
            var entry = taxes.states[abbr];
            if (!entry) {
                return null;
            }
            if (entry.none) {
                return 0;
            }
            var schedule = entry[status];
            var deduction = phasedAmount(schedule.deduction, schedule.deductionPhaseout, gross);
            var exemption = phasedAmount(schedule.exemption, schedule.exemptionPhaseout, gross);
            return bracketTax(Math.max(0, gross - deduction - exemption), schedule.brackets);
        }

        return { federalTax: federalTax, payrollTax: payrollTax, stateIncomeTax: stateIncomeTax };
    }

    window.StateMatchCore = {
        PRESET_VALUES: PRESET_VALUES,
        PRESET_LABELS: PRESET_LABELS,
        STATE_GRID: STATE_GRID,
        clampNumber: clampNumber,
        escapeHtml: escapeHtml,
        matchColor: matchColor,
        createScorer: createScorer,
        createPaycheck: createPaycheck,
        effectiveWeight: effectiveWeight
    };
})();
