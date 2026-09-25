// Turns State Match's raw metrics into plain-language comparisons — "Homes 22% cheaper
// than IL", "No state income tax", "#4 of 50 for schools" — instead of "+10 Affordability"
// or "index 103.4". Every fact is a real value plus what it's compared with: the 50-state
// average or the viewer's own state. Used by the ranking chips and the state panel.
(function () {
    "use strict";

    var core = window.StateMatchCore;
    if (!core) {
        return;
    }

    // Below this normalized gap a difference isn't worth calling a pro or a con.
    var MIN_ADVANTAGE = 0.04;

    function createFacts(places, metrics, scorer) {
        var withData = places.filter(function (place) { return place.metrics.length > 0; });
        var stats = new Map();

        metrics.forEach(function (metric) {
            var values = [];
            withData.forEach(function (place) {
                var value = metricOf(place, metric.key);
                if (value) {
                    values.push(value);
                }
            });
            var mean = values.reduce(function (sum, value) { return sum + value.raw; }, 0) / (values.length || 1);
            stats.set(metric.key, {
                values: values,
                // The cost-of-living index is defined against the national average (100).
                mean: metric.key === "cost" ? 100 : mean,
                direction: values.length ? values[0].direction : 1
            });
        });

        function metricOf(place, key) {
            for (var i = 0; i < place.metrics.length; i++) {
                if (place.metrics[i].key === key) {
                    return place.metrics[i];
                }
            }
            return null;
        }

        function rankOf(place, key) {
            var value = metricOf(place, key);
            var stat = stats.get(key);
            if (!value || !stat) {
                return null;
            }
            var better = stat.values.filter(function (other) {
                return value.direction > 0 ? other.raw > value.raw : other.raw < value.raw;
            }).length;
            return { rank: better + 1, of: stat.values.length };
        }

        /** The comparison point: the viewer's state, or the 50-state average. */
        function baseline(key, home) {
            if (home) {
                var value = metricOf(home, key);
                return value ? { raw: value.raw, name: home.name, short: home.abbreviation, isHome: true } : null;
            }
            var stat = stats.get(key);
            return stat ? { raw: stat.mean, name: "the US average", short: "US avg", isHome: false } : null;
        }

        function normalizeRaw(key, raw, direction) {
            var range = scorer.ranges.get(key);
            if (!range || range.max <= range.min) {
                return 0.5;
            }
            var normalized = (raw - range.min) / (range.max - range.min);
            return direction >= 0 ? normalized : 1 - normalized;
        }

        function pct(value, base) {
            return base ? Math.round(Math.abs(value - base) / Math.abs(base) * 100) : 0;
        }

        function money(value) {
            return "$" + Math.round(value).toLocaleString("en-US");
        }

        function shortMoney(value) {
            return value >= 1000000 ? "$" + (value / 1000000).toFixed(2).replace(/0$/, "") + "M" : "$" + Math.round(value / 1000) + "k";
        }

        function rankText(rank) {
            return rank ? "#" + rank.rank + " of " + rank.of : "";
        }

        /**
         * One comparable fact about a place for one metric. `good` is from the viewer's point of
         * view; `advantage` is the normalized gap to the baseline (positive = better) used to
         * pick which facts to show.
         */
        function fact(place, key, home) {
            var value = metricOf(place, key);
            var base = baseline(key, home);
            if (!value || !base) {
                return null;
            }
            var v = value.raw;
            var b = base.raw;
            var rank = rankOf(place, key);
            var baseRank = home ? rankOf(home, key) : null;
            var advantage = normalizeRaw(key, v, value.direction) - normalizeRaw(key, b, value.direction);
            var good = advantage > 0;
            var p = pct(v, b);
            var vsShort = base.short;
            var vsLong = base.name;
            var out = { key: key, good: good, advantage: advantage, big: "", label: "", sub: "", chip: "" };

            switch (key) {
                case "cost":
                    out.big = p < 2 ? "≈ " + (home ? home.abbreviation : "US avg") : p + "% " + (v < b ? "lower" : "higher");
                    out.label = p < 2 ? "everyday costs (index " + v.toFixed(1) + ")" : "everyday costs than " + vsLong;
                    out.sub = "Cost index " + v.toFixed(1) + " · " + rankText(rank);
                    out.chip = "Everyday costs " + p + "% " + (v < b ? "below " : "above ") + vsShort;
                    break;
                case "income":
                    out.big = money(v);
                    out.label = "median household income";
                    out.sub = rankText(rank) + " · " + p + "% " + (v >= b ? "above " : "below ") + vsShort;
                    out.chip = "Incomes " + p + "% " + (v >= b ? "higher" : "lower") + " than " + vsShort;
                    break;
                case "jobs":
                    out.big = v.toFixed(1) + "%";
                    out.label = "unemployment rate";
                    out.sub = rankText(rank) + " · " + vsShort + " " + b.toFixed(1) + "%";
                    out.chip = "Unemployment " + v.toFixed(1) + "% (" + vsShort + " " + b.toFixed(1) + "%)";
                    break;
                case "housing":
                    out.big = shortMoney(v);
                    out.label = "median home value";
                    out.sub = p + "% " + (v < b ? "cheaper" : "pricier") + " than " + vsShort + " · " + rankText(rank);
                    out.chip = "Homes " + p + "% " + (v < b ? "cheaper" : "pricier") + (home ? " than " + vsShort : "");
                    break;
                case "safety":
                    out.big = p + "% " + (v < b ? "less" : "more");
                    out.label = "violent crime than " + vsLong;
                    out.sub = Math.round(v) + " per 100k residents · " + rankText(rank);
                    out.chip = "Violent crime " + p + "% " + (v < b ? "below " : "above ") + vsShort;
                    break;
                case "health":
                    out.big = rank ? "#" + rank.rank : "—";
                    out.label = "for healthcare quality & access";
                    out.sub = home && baseRank ? home.name + " is #" + baseRank.rank : "of " + (rank ? rank.of : 50) + " states";
                    out.chip = "Healthcare #" + (rank ? rank.rank : "?") + (home && baseRank ? " (" + vsShort + " #" + baseRank.rank + ")" : " of " + (rank ? rank.of : 50));
                    break;
                case "education":
                    out.big = rank ? "#" + rank.rank : "—";
                    out.label = "public school system";
                    out.sub = home && baseRank ? home.name + " is #" + baseRank.rank : "of " + (rank ? rank.of : 50) + " states";
                    out.chip = "Schools #" + (rank ? rank.rank : "?") + (home && baseRank ? " (" + vsShort + " #" + baseRank.rank + ")" : " of " + (rank ? rank.of : 50));
                    break;
                case "warmth":
                    var degrees = Math.round(Math.abs(v - b));
                    out.big = Math.round(v) + "°F";
                    out.label = "average year-round temperature";
                    out.sub = degrees < 1 ? "About the same as " + vsShort : degrees + "°F " + (v > b ? "warmer" : "colder") + " than " + vsShort;
                    out.chip = degrees < 1 ? "Climate like " + vsShort : degrees + "°F " + (v > b ? "warmer" : "colder") + " than " + vsShort;
                    break;
                case "incometax":
                    out.big = v === 0 ? "None" : v.toFixed(2).replace(/0$/, "") + "%";
                    out.label = v === 0 ? "state income tax on wages" : "top state income tax rate";
                    out.sub = v === 0 ? "One of " + stats.get(key).values.filter(function (x) { return x.raw === 0; }).length + " states with none" : rankText(rank) + " · " + vsShort + " " + b.toFixed(2).replace(/0$/, "") + "%";
                    out.chip = v === 0 ? "No state income tax" : (good ? "Income tax tops out at " : "Income tax up to ") + v.toFixed(2).replace(/0$/, "") + "%";
                    break;
                case "propertytax":
                    out.big = v.toFixed(2) + "%";
                    out.label = "effective property tax rate";
                    out.sub = rankText(rank) + " · " + vsShort + " " + b.toFixed(2) + "%";
                    out.chip = "Property tax " + v.toFixed(2) + "% (" + vsShort + " " + b.toFixed(2) + "%)";
                    break;
                case "salestax":
                    out.big = v === 0 ? "None" : v.toFixed(2) + "%";
                    out.label = v === 0 ? "statewide sales tax" : "state sales tax rate";
                    out.sub = v === 0 ? "Local sales taxes may still apply" : rankText(rank) + " · " + vsShort + " " + b.toFixed(2) + "%";
                    out.chip = v === 0 ? "No state sales tax" : "Sales tax " + v.toFixed(2) + "% (" + vsShort + " " + b.toFixed(2) + "%)";
                    break;
                default:
                    return null;
            }
            return out;
        }

        /**
         * Pros and cons for a place under the viewer's weights, strongest first. Metrics the
         * viewer set to 0 are skipped — their sliders say they don't care.
         */
        function prosAndCons(place, weights, home) {
            var all = metrics
                .filter(function (metric) { return (weights[metric.key] || 0) > 0; })
                .map(function (metric) {
                    var item = fact(place, metric.key, home);
                    if (item) {
                        item.weighted = item.advantage * weights[metric.key];
                    }
                    return item;
                })
                .filter(Boolean);

            return {
                pros: all.filter(function (item) { return item.advantage >= MIN_ADVANTAGE; })
                    .sort(function (a, b) { return b.weighted - a.weighted; }),
                cons: all.filter(function (item) { return item.advantage <= -MIN_ADVANTAGE; })
                    .sort(function (a, b) { return a.weighted - b.weighted; })
            };
        }

        return {
            fact: fact,
            prosAndCons: prosAndCons,
            rankOf: rankOf,
            metricOf: metricOf,
            hasData: function (place) { return place.metrics.length > 0; }
        };
    }

    core.createFacts = createFacts;
})();
