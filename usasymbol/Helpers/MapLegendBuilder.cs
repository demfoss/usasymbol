using System.Globalization;
using System.Text.RegularExpressions;
using USASymbol.Models.Content;
using Usasymbol.ViewModels;

namespace Usasymbol.Helpers;

/// <summary>One classification supplies map fills, accessible legend labels and filter membership.</summary>
public static class MapLegendBuilder
{
    public const string NoDataColor = "#e2e8f0";
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");
    private static readonly string[] CategoryColors =
        ["#2563eb", "#d97706", "#15803d", "#9333ea", "#dc2626", "#0891b2", "#be185d", "#4d7c0f", "#7c3aed", "#a16207", "#0369a1", "#475569", "#b45309", "#be123c"];

    public static bool IsMissing(string? value) => string.IsNullOrWhiteSpace(value)
        || new[] { "—", "–", "-", "N/A", "NA", "No value", "No data", "Not available", "Data not available", "null" }
            .Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);

    public static ChoroplethResult Numeric(List<StateMapEntry> entries, PageMap map, TableColumn? column)
    {
        var valid = entries.Where(e => e.NumericValue is double value && double.IsFinite(value)).ToList();
        var values = valid.Select(e => e.NumericValue!.Value).OrderBy(v => v).ToArray();
        var format = new MetricFormat(map, column, values, entries.Select(e => e.DisplayValue));
        var groups = new List<MapLegendGroup>();
        var referenceMode = map.LegendMode == "reference" && map.ReferenceValue is double reference && double.IsFinite(reference);
        var signed = string.Equals(map.ColorScheme, "diverging", StringComparison.OrdinalIgnoreCase);
        if (values.Length > 0)
        {
            var distinct = values.Distinct().ToArray();
            if (referenceMode || signed)
            {
                var center = referenceMode ? map.ReferenceValue!.Value : 0;
                var tolerance = map.ReferenceTolerance is > 0 && double.IsFinite(map.ReferenceTolerance.Value)
                    ? map.ReferenceTolerance.Value : Math.Max((values[^1] - values[0]) / 20, 0.000001);
                var cuts = new[] { center - 3 * tolerance, center - tolerance, center + tolerance, center + 3 * tolerance }
                    .Select(value => format.Integers ? Math.Ceiling(value) : value).ToArray();
                var labels = referenceMode
                    ? new[] { "Well below", "Below", "Near", "Above", "Well above" }
                    : new[] { "Strongly negative", "Negative", "Near zero", "Positive", "Strongly positive" };
                string[] colors = ["#b35806", "#f1a340", "#d8daeb", "#998ec3", "#542788"];
                for (var i = 0; i < 5; i++)
                {
                    var lower = i == 0 ? double.NegativeInfinity : cuts[i - 1];
                    var upper = i == 4 ? double.PositiveInfinity : cuts[i];
                    var members = valid.Where(e => e.NumericValue >= lower && e.NumericValue < upper).ToList();
                    if (members.Count > 0) Add(groups, labels[i], Range(lower, upper, format), colors[i], members);
                }
            }
            else if (format.Type == "rank_only" || map.MetricKey == "rank")
            {
                foreach (var start in Enumerable.Range(0, (int)Math.Ceiling(values[^1] / 10)).Select(i => i * 10 + 1))
                {
                    var members = valid.Where(e => e.NumericValue >= start && e.NumericValue < start + 10).ToList();
                    if (members.Count == 0) continue;
                    Add(groups, start == 1 ? "Top 10" : $"{start}–{start + 9}", "", "", members);
                }
            }
            else if (distinct.Length == 1 || (distinct.Length <= 5 && map.Breaks.Count == 0))
            {
                foreach (var value in distinct)
                    Add(groups, format.Value(value), "", "", valid.Where(e => e.NumericValue == value).ToList());
            }
            else
            {
                var cuts = map.Breaks.Count > 0
                    ? map.Breaks.Where(double.IsFinite).Select(value => format.Integers ? Math.Ceiling(value) : value)
                        .Where(value => value > values[0] && value <= values[^1]).Distinct().OrderBy(value => value).Take(4).ToArray()
                    : NiceCuts(values, format.Integers);
                var lower = double.NegativeInfinity;
                foreach (var upper in cuts.Append(double.PositiveInfinity))
                {
                    var members = valid.Where(e => e.NumericValue >= lower && e.NumericValue < upper).ToList();
                    // Remove empty intervals without introducing holes in the displayed thresholds.
                    if (members.Count > 0)
                    {
                        var range = format.Integers && members.Select(e => e.NumericValue).Distinct().Count() == 1
                            ? format.Value(members[0].NumericValue!.Value)
                            : double.IsNegativeInfinity(lower) && double.IsPositiveInfinity(upper)
                                ? format.Pair(values[0], values[^1], false) : Range(lower, upper, format);
                        Add(groups, "", range, "", members);
                        lower = upper;
                    }
                }
                for (var i = 0; i < groups.Count; i++)
                {
                    var year = format.Type == "year";
                    groups[i] = groups[i] with { Label = OrderedLabel(i, groups.Count, year) };
                }
            }

            var palette = Palette(map.ColorScheme);
            for (var i = 0; i < groups.Count; i++)
            {
                if (referenceMode || signed) continue;
                // All-equal data remains a real value, never the missing-data colour.
                var index = groups.Count == 1 ? 2 : (int)Math.Round(i * 4d / (groups.Count - 1));
                if (format.Type == "rank_only" || map.MetricKey == "rank") index = 4 - index;
                groups[i] = groups[i] with { Color = palette[index] };
            }
        }
        AddMissing(groups, entries.Where(e => e.NumericValue is not double value || !double.IsFinite(value)));
        var title = referenceMode ? $"{format.Title} · {map.ReferenceLabel ?? "Reference"}: {format.Value(map.ReferenceValue!.Value)}" : format.Title;
        return Result(entries.Select(e => e with
        {
            DisplayValue = e.NumericValue is double value && double.IsFinite(value) ? format.Value(value, false) : "No data"
        }).ToList(), groups, title);
    }

    public static ChoroplethResult Categories(List<StateMapEntry> entries, PageMap map)
    {
        IEnumerable<string> Values(StateMapEntry entry) => entry.Items?.Select(i => i.DisplayValue)
            ?? new[] { entry.DisplayValue };
        var categories = entries.SelectMany(Values).Where(v => !IsMissing(v))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var groups = new List<MapLegendGroup>();
        var configuredOrder = map.CategoryOrder.Concat(map.ColorMap.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var ordered = configuredOrder.Where(key => categories.Contains(key, StringComparer.OrdinalIgnoreCase))
            .Concat(categories.Where(key => !configuredOrder.Contains(key, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(key => entries.Count(e => Values(e).Contains(key, StringComparer.OrdinalIgnoreCase)))
                .ThenBy(key => key, StringComparer.OrdinalIgnoreCase)).ToList();
        if (ordered.Count <= CategoryColors.Length)
        {
            foreach (var category in ordered)
            {
                var color = map.ColorMap.TryGetValue(category, out var token)
                    ? ChoroplethBuilder.ResolveColorToken(token) : null;
                if (color == null && map.ValueType == "ordinal")
                    color = SequentialColor(map.ColorScheme, groups.Count, ordered.Count);
                Add(groups, category, "", color ?? CategoryColors[groups.Count],
                    entries.Where(e => Values(e).Contains(category, StringComparer.OrdinalIgnoreCase)).ToList());
            }
        }
        else
        {
            // Unique names are inspected by state; a fifty-item legend cannot explain a map.
            entries = entries.Select(e => e with { FillColor = "#6baed6" }).ToList();
        }
        AddMissing(groups, entries.Where(e => Values(e).All(IsMissing)));
        return Result(entries, groups, map.MetricLabel ?? ColumnLabel(map));
    }

    private static string ColumnLabel(PageMap map) => string.IsNullOrWhiteSpace(map.MetricKey)
        ? "" : ChoroplethBuilder.FormatLabel(map.MetricKey);

    private static void Add(List<MapLegendGroup> groups, string label, string range, string color, List<StateMapEntry> entries) =>
        groups.Add(new MapLegendGroup($"group-{groups.Count}", label, range, color,
            entries.Select(e => e.PostalCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList()));

    private static void AddMissing(List<MapLegendGroup> groups, IEnumerable<StateMapEntry> entries)
    {
        var missing = entries.OrderBy(e => e.StateName).ToList();
        var codes = missing.Select(e => e.PostalCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (codes.Count > 0) groups.Add(new MapLegendGroup("no-data", "No data", $"{codes.Count} {(codes.Count == 1 ? "state" : "states")}", NoDataColor, codes, true,
            $"No data: {string.Join(", ", missing.Select(e => e.StateName).Distinct())}"));
    }

    private static ChoroplethResult Result(List<StateMapEntry> entries, List<MapLegendGroup> groups, string title)
    {
        var colors = groups.SelectMany(group => group.StateCodes.Select(code => (code, group.Color)))
            .GroupBy(item => item.code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Color, StringComparer.OrdinalIgnoreCase);
        return new ChoroplethResult
        {
            Entries = entries.Select(e => e with { FillColor = colors.GetValueOrDefault(e.PostalCode, e.FillColor) }).ToList(),
            LegendGroups = groups,
            LegendTitle = title,
            LegendSteps = groups.Select(g => (g.Color, string.IsNullOrEmpty(g.Range) ? g.Label : g.Range)).ToList(),
            LightColor = groups.FirstOrDefault(g => !g.IsNoData)?.Color ?? NoDataColor,
            DarkColor = groups.LastOrDefault(g => !g.IsNoData)?.Color ?? NoDataColor
        };
    }

    private static double[] NiceCuts(double[] values, bool integers)
    {
        var span = values[^1] - values[0];
        var rough = span / 10;
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(rough)));
        var normalized = rough / magnitude;
        var step = (normalized < 1.5 ? 1 : normalized < 3.5 ? 2.5 : normalized < 7.5 ? 5 : 10) * magnitude;
        if (integers) step = Math.Max(1, Math.Round(step));
        var cuts = Enumerable.Range(1, 4).Select(i =>
        {
            var position = (values.Length - 1) * i / 5d;
            var lo = (int)Math.Floor(position);
            var quantile = values[lo] + (values[(int)Math.Ceiling(position)] - values[lo]) * (position - lo);
            return Math.Round(Math.Round(quantile / step, MidpointRounding.AwayFromZero) * step, 10);
        }).Where(cut => cut > values[0] && cut <= values[^1]);
        // A repeated minimum (e.g. eleven states with one area code) deserves its own group.
        if (integers && values.Count(value => value == values[0]) >= values.Length / 5d)
            cuts = cuts.Append(values.First(value => value > values[0]));
        return cuts.Distinct().OrderBy(cut => cut).Take(4).ToArray();
    }

    private static string Range(double lower, double upper, MetricFormat format)
    {
        if (double.IsNegativeInfinity(lower)) return $"Under {format.Value(upper)}";
        if (double.IsPositiveInfinity(upper)) return $"{format.Value(lower)}+";
        if (format.Integers)
        {
            // Large compact thresholds read as 250k–750k, not 250k–749.999k.
            if (upper >= 10_000 && upper - lower > 1) return format.Pair(lower, upper, false);
            return lower == upper - 1 ? format.Value(lower) : format.Pair(lower, upper - 1, false);
        }
        return format.Pair(lower, upper, true);
    }

    private static string OrderedLabel(int i, int count, bool year) => count switch
    {
        1 => "All values",
        2 => i == 0 ? (year ? "Earlier" : "Lower") : (year ? "Later" : "Higher"),
        3 => i == 0 ? (year ? "Earliest" : "Lowest") : i == 2 ? (year ? "Latest" : "Highest") : "Middle",
        4 => new[] { year ? "Earliest" : "Lowest", year ? "Earlier" : "Low", year ? "Later" : "High", year ? "Latest" : "Highest" }[i],
        _ => new[] { year ? "Earliest" : "Lowest", year ? "Earlier" : "Low", "Middle", year ? "Later" : "High", year ? "Latest" : "Highest" }[Math.Min(i, 4)]
    };

    private static string[] Palette(string scheme) => scheme.ToLowerInvariant() switch
    {
        "teal" or "green" => ["#99d8c9", "#66c2a4", "#2ca25f", "#13813d", "#005824"],
        "purple" => ["#bcbddc", "#9e9ac8", "#807dba", "#6a51a3", "#3f007d"],
        "red" or "pink" => ["#fcae91", "#fb6a4a", "#ef3b2c", "#cb181d", "#99000d"],
        "orange" or "amber" or "warm" or "yellow" => ["#fdd49e", "#fdbb84", "#fc8d59", "#d95f0e", "#993404"],
        _ => ["#9ecae1", "#6baed6", "#4292c6", "#2171b5", "#084594"]
    };

    private static string SequentialColor(string scheme, int index, int count)
    {
        var palette = Palette(scheme);
        var position = count == 1 ? 2 : index * 4d / (count - 1);
        var low = (int)Math.Floor(position);
        var high = Math.Min(4, low + 1);
        var fraction = position - low;
        var channels = new[] { 1, 3, 5 }.Select(offset =>
        {
            var a = Convert.ToInt32(palette[low].Substring(offset, 2), 16);
            var b = Convert.ToInt32(palette[high].Substring(offset, 2), 16);
            return ((int)Math.Round(a + (b - a) * fraction)).ToString("x2");
        });
        return "#" + string.Concat(channels);
    }

    private sealed class MetricFormat
    {
        public string Type { get; }
        public bool Integers { get; }
        public string Title { get; }
        private readonly int decimals;
        private readonly string currency;
        private readonly string suffix;
        private readonly bool fractionalPercent;

        public MetricFormat(PageMap map, TableColumn? column, double[] values, IEnumerable<string> displays)
        {
            var clues = $"{map.MetricKey} {map.MetricLabel} {map.UnitLabel} {column?.Label} {column?.Format}";
            var samples = string.Join(" ", displays.Take(50));
            fractionalPercent = Regex.IsMatch(column?.Format ?? "", @"^P\d*$", RegexOptions.IgnoreCase);
            Type = map.ValueType?.ToLowerInvariant() ?? (clues.Contains('%') || samples.Contains('%') || fractionalPercent || Regex.IsMatch(clues.Replace('_', ' '), @"\b(pct|percent|percentage)\b", RegexOptions.IgnoreCase)
                ? "percent" : clues.Contains('$') || samples.Contains('$') || column?.Format?.StartsWith("C", StringComparison.OrdinalIgnoreCase) == true
                ? "currency" : map.MetricKey == "rank" ? "rank_only"
                : Regex.IsMatch((map.MetricKey ?? "").Replace('_', ' '), @"\b(year|statehood)\b", RegexOptions.IgnoreCase)
                    && values.All(v => v is >= 1000 and <= 2200) ? "year" : "number");
            Integers = Type is "count" or "large_count" or "year" or "rank_only"
                || (Type == "number" && values.All(v => v == Math.Truncate(v)));
            decimals = Math.Clamp(map.Decimals ?? (Integers ? 0 : 10), 0, 10);
            currency = Type == "currency" ? "$" : "";
            suffix = Type == "percent" ? "%" : "";
            var label = map.MetricLabel ?? column?.Label ?? ColumnLabel(map);
            var unit = map.UnitLabel ?? (Type == "currency" ? "USD" : suffix);
            Title = string.IsNullOrWhiteSpace(unit) || label.Contains(unit, StringComparison.OrdinalIgnoreCase)
                ? label : $"{label}, {unit}";
        }

        private string Number(double value, int precision) => value.ToString(
            Type == "year" ? "0" : "#,0" + (precision > 0 ? "." + new string('#', precision) : ""), En);

        private static (double Divisor, string Suffix) Compact(double value) => Math.Abs(value) switch
        {
            >= 1_000_000_000 => (1_000_000_000, "B"),
            >= 1_000_000 => (1_000_000, "M"),
            >= 10_000 => (1_000, "k"),
            _ => (1, "")
        };

        public string Value(double value, bool boundary = true)
        {
            if (Type == "percent" && fractionalPercent) value *= 100;
            var compact = Type is "year" or "percent" or "rank_only" ? (1d, "") : Compact(value);
            // Compact counts still need fractional thousands/millions to preserve their thresholds.
            var number = compact.Item1 > 1 ? Math.Round(value / compact.Item1, 6).ToString("0.######", En) : Number(value, boundary ? 10 : decimals);
            return currency + number + compact.Item2 + suffix;
        }

        public string Pair(double lower, double upper, bool exclusive)
        {
            var left = Value(lower);
            var right = Value(upper);
            if (suffix.Length > 0) left = left[..^suffix.Length];
            if (currency.Length > 0) right = right[currency.Length..];
            return $"{left}–{(exclusive ? "<" : "")}{right}";
        }
    }
}
