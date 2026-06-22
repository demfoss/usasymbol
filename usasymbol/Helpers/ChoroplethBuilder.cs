using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using USASymbol.Models.Content;
using Usasymbol.ViewModels;

namespace Usasymbol.Helpers
{
    public class ChoroplethResult
    {
        public List<StateMapEntry> Entries { get; set; } = new();
        public string LightColor { get; set; } = "#dbeafe";
        public string DarkColor { get; set; } = "#1e3a8a";
        public string MinDisplayValue { get; set; } = string.Empty;
        public string MaxDisplayValue { get; set; } = string.Empty;
        public List<(string Color, string Label)> LegendSteps { get; set; } = new();
    }

    public static class ChoroplethBuilder
    {
        private sealed record MapSourceRow(
            string PostalCode,
            string StateName,
            string StateSlug,
            int? Rank,
            double? NumericValue,
            string DisplayValue,
            IReadOnlyList<MapEntryDetail> Details,
            string? ImageUrl,
            string? FillColor,
            string? Summary,
            IReadOnlyDictionary<string, IReadOnlyList<string>> Filters
        );

        private static readonly Dictionary<string, ((int r, int g, int b) light, (int r, int g, int b) dark)> Schemes = new()
        {
            ["blue"]   = ((219, 234, 254), (30,  58,  138)),
            ["green"]  = ((220, 252, 231), (20,  83,  45)),
            ["orange"] = ((255, 237, 213), (124, 45,  18)),
            ["purple"] = ((243, 232, 255), (74,  4,   78)),
            ["red"]    = ((254, 226, 226), (127, 29,  29)),
            ["warm"]   = ((254, 249, 195), (120, 53,  15)),
        };

        private const string NoDataColor = "#e2e8f0";
        private const string FlatColor   = "#bfdbfe";

        private static readonly string[] CategoricalPalette =
        {
            "#2563eb", "#dc2626", "#16a34a", "#9333ea", "#ea580c",
            "#0891b2", "#ca8a04", "#db2777", "#4f46e5", "#0f766e",
            "#65a30d", "#b45309", "#7c3aed", "#be123c", "#0369a1"
        };

        private static readonly Dictionary<string, string> NamedColors = new(StringComparer.OrdinalIgnoreCase)
        {
            ["blue-dark"] = "#0b7a5c",
            ["blue"] = "#0f766e",
            ["yellow"] = "#f6c64d",
            ["orange-light"] = "#ffb64c",
            ["orange"] = "#ff7a45",
            ["red"] = "#b1135b",
            ["red-dark"] = "#4d183e",
            ["green-dark"] = "#0b7a5c",
            ["green"] = "#169873",
            ["slate"] = "#475569",
            ["gray"] = "#94a3b8",
            ["grey"] = "#94a3b8",
            ["white"] = "#ffffff",
            ["black"] = "#111827",
        };

        private static readonly HashSet<string> SkipKeys = new(StringComparer.OrdinalIgnoreCase)
            { "state", "state_slug", "postal_code", "postal", "rank", "order", "year", "type", "date" };

        private static readonly Dictionary<string, string> SlugToPostal = new(StringComparer.OrdinalIgnoreCase)
        {
            ["alabama"] = "AL", ["alaska"] = "AK", ["arizona"] = "AZ", ["arkansas"] = "AR",
            ["california"] = "CA", ["colorado"] = "CO", ["connecticut"] = "CT", ["delaware"] = "DE",
            ["florida"] = "FL", ["georgia"] = "GA", ["hawaii"] = "HI", ["idaho"] = "ID",
            ["illinois"] = "IL", ["indiana"] = "IN", ["iowa"] = "IA", ["kansas"] = "KS",
            ["kentucky"] = "KY", ["louisiana"] = "LA", ["maine"] = "ME", ["maryland"] = "MD",
            ["massachusetts"] = "MA", ["michigan"] = "MI", ["minnesota"] = "MN", ["mississippi"] = "MS",
            ["missouri"] = "MO", ["montana"] = "MT", ["nebraska"] = "NE", ["nevada"] = "NV",
            ["new hampshire"] = "NH", ["new jersey"] = "NJ", ["new mexico"] = "NM", ["new york"] = "NY",
            ["north carolina"] = "NC", ["north dakota"] = "ND", ["ohio"] = "OH", ["oklahoma"] = "OK",
            ["oregon"] = "OR", ["pennsylvania"] = "PA", ["rhode island"] = "RI", ["south carolina"] = "SC",
            ["south dakota"] = "SD", ["tennessee"] = "TN", ["texas"] = "TX", ["utah"] = "UT",
            ["vermont"] = "VT", ["virginia"] = "VA", ["washington"] = "WA", ["west virginia"] = "WV",
            ["wisconsin"] = "WI", ["wyoming"] = "WY",
        };

        public static ChoroplethResult Build(PageMap map, List<TableRow> rows)
        {
            var schemeName = map.ColorScheme?.ToLower() ?? "blue";
            var scheme = Schemes.ContainsKey(schemeName) ? Schemes[schemeName] : Schemes["blue"];
            var useLog = string.Equals(map.ColorScale, "log", StringComparison.OrdinalIgnoreCase);
            var key = map.MetricKey!;

            var parsed = ToMapRows(rows, map, key, r => FormatValue(r, key));

            var values = parsed.Where(x => x.NumericValue.HasValue).Select(x => x.NumericValue!.Value).ToList();
            if (!values.Any())
            {
                if (IsCategoricalMap(map))
                    return BuildCategorical(parsed, map);

                return BuildFlat(rows, key, map);
            }

            double min = values.Min();
            double max = values.Max();

            var entries = parsed.Select(x =>
            {
                string fill;
                if (!x.NumericValue.HasValue || min >= max)
                    fill = NoDataColor;
                else
                {
                    double t = useLog ? LogScale(x.NumericValue.Value, min, max) : LinearScale(x.NumericValue.Value, min, max);
                    fill = Lerp(t, scheme.light, scheme.dark);
                }
                return new StateMapEntry(
                    x.PostalCode,
                    x.StateName,
                    x.StateSlug,
                    x.NumericValue,
                    x.Rank,
                    x.DisplayValue,
                    fill,
                    x.Details,
                    x.ImageUrl,
                    null,
                    null,
                    x.Summary,
                    x.Filters);
            })
            .GroupBy(e => e.PostalCode, StringComparer.OrdinalIgnoreCase)
            .Select(CombineEntries)
            .ToList();

            var minEntry = entries.Where(e => e.NumericValue.HasValue).OrderBy(e => e.NumericValue).First();
            var maxEntry = entries.Where(e => e.NumericValue.HasValue).OrderByDescending(e => e.NumericValue).First();

            var steps = new List<(string Color, string Label)>();
            for (int i = 0; i < 5; i++)
            {
                double t = i / 4.0;
                double value = useLog
                    ? Math.Exp(Math.Log(Math.Max(min, 1)) + t * (Math.Log(Math.Max(max, 1)) - Math.Log(Math.Max(min, 1))))
                    : min + t * (max - min);
                steps.Add((Lerp(t, scheme.light, scheme.dark), FormatStepValue(value)));
            }

            return new ChoroplethResult
            {
                Entries          = entries,
                LightColor       = ToHex(scheme.light),
                DarkColor        = ToHex(scheme.dark),
                MinDisplayValue  = minEntry.DisplayValue,
                MaxDisplayValue  = maxEntry.DisplayValue,
                LegendSteps      = steps,
            };
        }

        public static ChoroplethResult BuildFlat(List<TableRow> rows, string? preferredDisplayKey = null, PageMap? map = null)
        {
            var entries = rows
                .Select(r =>
                {
                    var postal = GetPostal(r);
                    if (string.IsNullOrEmpty(postal)) return null;

                    var displayKey = !string.IsNullOrWhiteSpace(preferredDisplayKey) && r.Data.ContainsKey(preferredDisplayKey)
                        ? preferredDisplayKey
                        : r.Data.Keys
                        .Cast<string>()
                        .FirstOrDefault(k => !SkipKeys.Contains(k));
                    var display = displayKey != null ? r.GetString(displayKey) : r.GetString("state");

                    return new StateMapEntry(
                        postal,
                        r.GetString("state"),
                        r.GetString("state_slug"),
                        null,
                        TryInt(r, "rank") ?? TryInt(r, "order"),
                        display,
                        ResolveColorToken(r.GetString("fill_color")) ?? ResolveColorToken(map?.FillColor) ?? FlatColor,
                        map != null ? BuildDetails(r, map) : Array.Empty<MapEntryDetail>(),
                        map != null ? GetImageUrl(r, map) : null,
                        null,
                        null,
                        map != null ? GetSummary(r, map) : null,
                        map != null ? BuildFilterValues(r, map, display) : new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
                    );
                })
                .Where(e => e != null)
                .Cast<StateMapEntry>()
                .GroupBy(e => e.PostalCode, StringComparer.OrdinalIgnoreCase)
                .Select(CombineEntries)
                .ToList();

            return new ChoroplethResult { Entries = entries };
        }

        private static ChoroplethResult BuildCategorical(List<MapSourceRow> rows, PageMap map)
        {
            var categories = rows
                .Select(r => r.DisplayValue)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .GroupBy(v => v, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.Key)
                .ToList();

            var colorByCategory = categories
                .Select((category, index) => new
                {
                    Category = category,
                    Color = index < CategoricalPalette.Length
                        ? CategoricalPalette[index]
                        : HslToHex((index - CategoricalPalette.Length) * 360.0 / Math.Max(1, categories.Count - CategoricalPalette.Length), 0.62, 0.50)
                })
                .ToDictionary(x => x.Category, x => x.Color, StringComparer.OrdinalIgnoreCase);

            var entries = rows
                .Select(r =>
                {
                    var display = r.DisplayValue;
                    var fill = ResolveColorToken(r.FillColor)
                        ?? (map.ColorMap.Count > 0 && map.ColorMap.TryGetValue(display, out var cmColor) ? ResolveColorToken(cmColor) : null)
                        ?? GetSemanticFill(map.MetricKey, display)
                        ?? (!string.IsNullOrWhiteSpace(display) && colorByCategory.TryGetValue(display, out var color)
                            ? color
                            : ResolveColorToken(map.FillColor) ?? NoDataColor);

                    return new StateMapEntry(
                        r.PostalCode,
                        r.StateName,
                        r.StateSlug,
                        null,
                        r.Rank,
                        string.IsNullOrWhiteSpace(display) ? "No value" : display,
                        fill,
                        r.Details,
                        r.ImageUrl,
                        null,
                        null,
                        r.Summary,
                        r.Filters
                    );
                })
                .GroupBy(e => e.PostalCode, StringComparer.OrdinalIgnoreCase)
                .Select(CombineEntries)
                .ToList();

            return new ChoroplethResult { Entries = entries, LegendSteps = BuildCategoricalLegend(entries, map) };
        }

        // A swatch legend only helps when colors mark real, named groups shared by
        // more than one state (e.g. "Football" vs "Basketball"). When nearly every
        // state has its own distinct value (e.g. each state's own unique slogan or
        // seal name), listing all of them just repeats the table below, so the
        // legend is left empty and the map shows a hover/tap hint instead.
        private const int MaxMeaningfulLegendGroups = 14;

        private static List<(string Color, string Label)> BuildCategoricalLegend(List<StateMapEntry> entries, PageMap? map = null)
        {
            if (map?.ColorMap.Count > 0)
                return map.ColorMap
                    .Select(kv => (Color: ResolveColorToken(kv.Value) ?? kv.Value, Label: kv.Key))
                    .ToList();

            var groups = entries
                .Where(e => !string.IsNullOrWhiteSpace(e.DisplayValue) && e.DisplayValue != "No value")
                .GroupBy(e => e.FillColor, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (groups.Count == 0 || groups.Count > MaxMeaningfulLegendGroups)
                return new List<(string Color, string Label)>();

            return groups
                .Select(g =>
                {
                    var labels = g.Select(e => e.DisplayValue).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    var label = labels.Count == 1 ? labels[0] : $"{labels[0]} +{labels.Count - 1} more";
                    return (g.Key, label);
                })
                .ToList();
        }

        public static bool IsCategoricalMap(PageMap map) =>
            string.Equals(map.ColorScale, "categorical", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(map.ColorScheme, "categorical", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(map.ColorScheme, "diverging", StringComparison.OrdinalIgnoreCase) ||
            map.ColorMap.Count > 0;

        private static string? ResolveColorToken(string? color)
        {
            if (string.IsNullOrWhiteSpace(color)) return null;
            var trimmed = color.Trim();
            if (trimmed.StartsWith('#') && (trimmed.Length == 7 || trimmed.Length == 4))
                return trimmed;

            return NamedColors.TryGetValue(trimmed, out var named) ? named : null;
        }

        private static string? GetSemanticFill(string? metricKey, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            return (metricKey ?? string.Empty).ToLowerInvariant() switch
            {
                "beverage" => BeverageColor(value),
                "bird" => BirdColor(value),
                "colors" => OfficialColor(value),
                "tree" => TreeColor(value),
                "flower" => FlowerColor(value),
                "mammal" => MammalColor(value),
                _ => null,
            };
        }

        private static string? BeverageColor(string value)
        {
            if (Has(value, "milk")) return "#f8fafc";
            if (HasAny(value, "whiskey", "brandy", "picon", "cocktail", "old fashioned")) return "#fca5a5";
            if (HasAny(value, "orange", "lemonade", "juice", "kool-aid", "cider")) return "#fde68a";
            if (HasAny(value, "cranberry", "tomato")) return "#f9a8d4";
            if (HasAny(value, "coffee", "tea", "moxie")) return "#d6b48a";
            if (Has(value, "water")) return "#bfdbfe";
            if (Has(value, "awa") || Has(value, "ʻawa")) return "#c4b5fd";
            return null;
        }

        private static string? BirdColor(string value)
        {
            if (HasAny(value, "cardinal", "rhode island red")) return "#fca5a5";
            if (HasAny(value, "bluebird", "blue hen")) return "#93c5fd";
            if (HasAny(value, "goldfinch", "meadowlark", "yellowhammer")) return "#fde68a";
            if (HasAny(value, "oriole", "robin", "thrasher", "pelican")) return "#fdba74";
            if (HasAny(value, "mockingbird", "chickadee", "ptarmigan", "gull", "loon")) return "#cbd5e1";
            if (HasAny(value, "quail", "grouse", "pheasant", "roadrunner", "wren")) return "#d6b48a";
            if (HasAny(value, "nene", "goose")) return "#bae6fd";
            if (Has(value, "purple")) return "#c4b5fd";
            return null;
        }

        private static string? OfficialColor(string value)
        {
            var first = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(part => part.Split(" and ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .FirstOrDefault();

            var color = string.IsNullOrWhiteSpace(first) ? value : first;
            if (HasAny(color, "navy", "indigo", "old gold and blue")) return "#818cf8";
            if (Has(color, "blue")) return "#93c5fd";
            if (HasAny(color, "scarlet", "red")) return "#fca5a5";
            if (HasAny(color, "gold", "yellow", "maize")) return "#fde68a";
            if (Has(color, "green")) return "#86efac";
            if (Has(color, "orange")) return "#fdba74";
            if (Has(color, "white")) return "#f8fafc";
            if (Has(color, "black")) return "#a3a3a3";
            if (Has(color, "silver")) return "#cbd5e1";
            if (Has(color, "copper")) return "#d08b5b";
            if (Has(color, "brown")) return "#c4a484";
            if (Has(color, "cyan")) return "#67e8f9";
            if (Has(color, "buff")) return "#e7d8b1";
            return null;
        }

        private static string? TreeColor(string value)
        {
            if (HasAny(value, "pine", "spruce", "fir", "hemlock", "redwood")) return "#86efac";
            if (HasAny(value, "oak", "pecan", "buckeye")) return "#d6b48a";
            if (Has(value, "maple")) return "#fca5a5";
            if (HasAny(value, "dogwood", "magnolia", "holly", "birch")) return "#f8fafc";
            if (HasAny(value, "palm", "palmetto", "kukui", "palo verde")) return "#bbf7d0";
            if (HasAny(value, "cottonwood", "poplar", "tulip", "aspen", "elm", "redbud")) return "#bef264";
            if (Has(value, "cypress")) return "#99f6e4";
            return null;
        }

        private static string? FlowerColor(string value)
        {
            if (HasAny(value, "violet", "lilac", "iris", "bitterroot")) return "#c4b5fd";
            if (HasAny(value, "bluebonnet", "columbine", "forget-me-not")) return "#93c5fd";
            if (HasAny(value, "sunflower", "goldenrod", "yellow", "jessamine")) return "#fde68a";
            if (HasAny(value, "poppy", "orange")) return "#fdba74";
            if (HasAny(value, "rose", "carnation", "clover", "lady's slipper", "rhododendron", "laurel", "camellia", "peach")) return "#f9a8d4";
            if (HasAny(value, "magnolia", "dogwood", "mayflower", "yucca", "lily", "white")) return "#f8fafc";
            if (HasAny(value, "cactus", "sagebrush", "grape", "paintbrush", "hibiscus", "syringa")) return "#bbf7d0";
            if (Has(value, "black-eyed")) return "#fbbf24";
            return null;
        }

        private static string? MammalColor(string value)
        {
            if (HasAny(value, "whale", "dolphin", "seal", "manatee")) return "#bfdbfe";
            if (HasAny(value, "deer", "elk", "moose", "caribou", "antelope", "sheep", "goat", "longhorn")) return "#d6b48a";
            if (HasAny(value, "bear", "bison", "mule", "horse", "beaver")) return "#c4a484";
            if (HasAny(value, "squirrel", "fox", "cat", "ringtail", "armadillo")) return "#cbd5e1";
            if (HasAny(value, "panther", "bat")) return "#94a3b8";
            if (HasAny(value, "dog", "retriever", "husky", "hound", "spaniel", "terrier", "malamute", "chinook", "dane")) return "#fdba74";
            if (HasAny(value, "shelter", "rescue", "adoptable", "service", "seeing eye")) return "#c7d2fe";
            return null;
        }

        private static bool Has(string value, string fragment) =>
            value.Contains(fragment, StringComparison.OrdinalIgnoreCase);

        private static bool HasAny(string value, params string[] fragments) =>
            fragments.Any(fragment => Has(value, fragment));

        private static StateMapEntry CombineEntries(IGrouping<string, StateMapEntry> group)
        {
            var entries = group.ToList();
            var first = entries.First();
            var display = JoinDistinct(entries.Select(e => e.DisplayValue), skipNoValue: true);
            var details = group
                .SelectMany(e => e.Details)
                .GroupBy(d => $"{d.Label}\u001f{d.Value}", StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            var imageUrl = entries.Select(e => e.ImageUrl).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
            var summary = entries.Select(e => e.Summary).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
            var filters = entries
                .SelectMany(e => e.Filters ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase))
                .GroupBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<string>)group
                        .SelectMany(kv => kv.Value)
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList(),
                    StringComparer.OrdinalIgnoreCase);
            var items = entries.Count > 1
                ? entries
                    .Where(e => !string.IsNullOrWhiteSpace(e.DisplayValue) && e.DisplayValue != "No value")
                    .Select(e => new StateMapEntryItem(e.DisplayValue, e.Details, e.ImageUrl))
                    .ToList()
                : null;

            return first with
            {
                DisplayValue = string.IsNullOrWhiteSpace(display) ? first.DisplayValue : display,
                Details = details,
                ImageUrl = imageUrl ?? first.ImageUrl,
                Items = items,
                Summary = summary ?? first.Summary,
                Filters = filters
            };
        }

        private static List<MapSourceRow> ToMapRows(
            List<TableRow> rows,
            PageMap map,
            string metricKey,
            Func<TableRow, string> displaySelector)
        {
            return rows
                .Select(r => new MapSourceRow(
                    GetPostal(r),
                    r.GetString("state"),
                    r.GetString("state_slug"),
                    TryInt(r, "rank") ?? TryInt(r, "order"),
                    TryDouble(r, metricKey),
                    displaySelector(r),
                    BuildDetails(r, map),
                    GetImageUrl(r, map),
                    r.GetString("fill_color"),
                    GetSummary(r, map),
                    BuildFilterValues(r, map, displaySelector(r))
                ))
                .Where(r => !string.IsNullOrEmpty(r.PostalCode))
                .ToList();
        }

        private static string? GetImageUrl(TableRow row, PageMap map)
        {
            if (string.IsNullOrWhiteSpace(map.ImageKey)) return null;
            var value = row.GetString(map.ImageKey);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static string? GetSummary(TableRow row, PageMap map)
        {
            if (!string.IsNullOrWhiteSpace(map.SummaryKey))
            {
                var explicitSummary = row.GetString(map.SummaryKey);
                if (!string.IsNullOrWhiteSpace(explicitSummary))
                    return explicitSummary;
            }

            foreach (var key in new[] { "note", "notes", "summary", "description" })
            {
                var value = row.GetString(key);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return null;
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildFilterValues(TableRow row, PageMap map, string displayValue)
        {
            var filters = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(displayValue))
                filters["_category"] = new[] { displayValue };

            foreach (var filter in map.Filters)
            {
                if (string.IsNullOrWhiteSpace(filter.Key)) continue;

                var values = SplitFilterValues(row.GetString(filter.Key));
                if (values.Count > 0)
                    filters[filter.Key] = values;
            }

            return filters;
        }

        private static IReadOnlyList<string> SplitFilterValues(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();

            return raw
                .Split(new[] { ";", "|", "," }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IReadOnlyList<MapEntryDetail> BuildDetails(TableRow row, PageMap map)
        {
            var keys = GetDetailKeys(row, map);

            return keys
                .Select(key => (Key: key, Value: row.GetString(key)))
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => new MapEntryDetail(FormatLabel(x.Key), x.Value))
                .ToList();
        }

        private static IReadOnlyList<string> GetDetailKeys(TableRow row, PageMap map)
        {
            if (map.DetailKeys.Count > 0)
                return map.DetailKeys;

            var skipKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "state", "state_slug", "postal_code", "postal", "rank", "order",
                "custom_url", "symbol_url", "symbol_slug", "image", "hero_image",
                "classification",
                "note", "notes",
                map.NameKey ?? string.Empty,
                map.MetricKey ?? string.Empty,
                map.SummaryKey ?? string.Empty
            };

            return row.Data.Keys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Where(key => !skipKeys.Contains(key))
                .Where(key => key.IndexOf("image", StringComparison.OrdinalIgnoreCase) < 0)
                .Where(key => key.IndexOf("icon", StringComparison.OrdinalIgnoreCase) < 0)
                .Where(key => key.IndexOf("logo", StringComparison.OrdinalIgnoreCase) < 0)
                .Where(key => !key.EndsWith("_slug", StringComparison.OrdinalIgnoreCase))
                .Where(key => !string.IsNullOrWhiteSpace(row.GetString(key)))
                .ToList();
        }

        private static string JoinDistinct(IEnumerable<string?> values, bool skipNoValue = false)
        {
            return string.Join("; ", values
                .Where(v => !string.IsNullOrWhiteSpace(v) && (!skipNoValue || v != "No value"))
                .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        public static string FormatLabel(string key)
            => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(key.Replace('_', ' ').ToLower());

        private static string GetPostal(TableRow row)
        {
            var postal = row.GetString("postal_code");
            if (string.IsNullOrEmpty(postal)) postal = row.GetString("postal");
            if (string.IsNullOrEmpty(postal))
            {
                var slug = row.GetString("state_slug").Replace("-", " ");
                SlugToPostal.TryGetValue(slug, out postal);
            }
            return postal?.ToUpper() ?? string.Empty;
        }

        private static double LinearScale(double value, double min, double max)
            => (value - min) / (max - min);

        private static double LogScale(double value, double min, double max)
        {
            double sv = Math.Max(value, 1), sn = Math.Max(min, 1), sx = Math.Max(max, 1);
            return (Math.Log(sv) - Math.Log(sn)) / (Math.Log(sx) - Math.Log(sn));
        }

        // Used to extend the categorical palette beyond its 15 curated colors so
        // large category counts (e.g. 27+ candy types) never reuse a color.
        private static string HslToHex(double hue, double saturation, double lightness)
        {
            hue = ((hue % 360) + 360) % 360;
            double c = (1 - Math.Abs(2 * lightness - 1)) * saturation;
            double x = c * (1 - Math.Abs((hue / 60 % 2) - 1));
            double m = lightness - c / 2;

            (double r, double g, double b) = hue switch
            {
                < 60 => (c, x, 0.0),
                < 120 => (x, c, 0.0),
                < 180 => (0.0, c, x),
                < 240 => (0.0, x, c),
                < 300 => (x, 0.0, c),
                _ => (c, 0.0, x),
            };

            return ToHex((
                (int)Math.Round((r + m) * 255),
                (int)Math.Round((g + m) * 255),
                (int)Math.Round((b + m) * 255)
            ));
        }

        private static string Lerp(double t, (int r, int g, int b) light, (int r, int g, int b) dark)
        {
            t = Math.Clamp(t, 0, 1);
            int r = (int)(light.r + t * (dark.r - light.r));
            int g = (int)(light.g + t * (dark.g - light.g));
            int b = (int)(light.b + t * (dark.b - light.b));
            return $"#{r:x2}{g:x2}{b:x2}";
        }

        private static string ToHex((int r, int g, int b) c) => $"#{c.r:x2}{c.g:x2}{c.b:x2}";

        private static string FormatStepValue(double value)
        {
            if (value >= 1_000_000) return $"{value / 1_000_000:F1}M";
            if (value >= 1_000)     return ((long)Math.Round(value)).ToString("N0", CultureInfo.InvariantCulture);
            return value % 1 == 0   ? ((long)value).ToString() : value.ToString("G3", CultureInfo.InvariantCulture);
        }

        private static double? TryDouble(TableRow row, string key)
        {
            var raw = row.GetString(key);
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var clean = raw.Replace(",", "").Replace("%", "").Trim();
            return double.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
        }

        private static int? TryInt(TableRow row, string key)
        {
            var raw = row.GetString(key);
            return int.TryParse(raw, out var i) ? i : null;
        }

        private static string FormatValue(TableRow row, string key)
        {
            var raw = row.GetString(key);
            if (string.IsNullOrEmpty(raw)) return "—";
            var clean = raw.Replace(",", "").Replace("%", "").Trim();
            if (double.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            {
                if (d >= 1_000_000) return $"{d / 1_000_000:F1}M";
                if (d >= 1_000) return d.ToString("N0", CultureInfo.InvariantCulture);
                return d % 1 == 0 ? ((long)d).ToString() : d.ToString("G4", CultureInfo.InvariantCulture);
            }
            return raw;
        }
    }
}
