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
            string? FillColor
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
                if (IsCategorical(map))
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
                return new StateMapEntry(x.PostalCode, x.StateName, x.StateSlug, x.NumericValue, x.Rank, x.DisplayValue, fill, x.Details, x.ImageUrl);
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
                        GetExplicitFill(r.GetString("fill_color")) ?? GetExplicitFill(map?.FillColor) ?? FlatColor,
                        map != null ? BuildDetails(r, map) : Array.Empty<MapEntryDetail>(),
                        map != null ? GetImageUrl(r, map) : null
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
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var colorByCategory = categories
                .Select((category, index) => new
                {
                    Category = category,
                    Color = CategoricalPalette[index % CategoricalPalette.Length]
                })
                .ToDictionary(x => x.Category, x => x.Color, StringComparer.OrdinalIgnoreCase);

            var entries = rows
                .Select(r =>
                {
                    var display = r.DisplayValue;
                    var fill = GetExplicitFill(r.FillColor)
                        ?? GetExplicitFill(map.FillColor)
                        ?? GetSemanticFill(map.MetricKey, display)
                        ?? (!string.IsNullOrWhiteSpace(display) && colorByCategory.TryGetValue(display, out var color)
                            ? color
                            : NoDataColor);

                    return new StateMapEntry(
                        r.PostalCode,
                        r.StateName,
                        r.StateSlug,
                        null,
                        r.Rank,
                        string.IsNullOrWhiteSpace(display) ? "No value" : display,
                        fill,
                        r.Details,
                        r.ImageUrl
                    );
                })
                .GroupBy(e => e.PostalCode, StringComparer.OrdinalIgnoreCase)
                .Select(CombineEntries)
                .ToList();

            return new ChoroplethResult { Entries = entries };
        }

        private static bool IsCategorical(PageMap map) =>
            string.Equals(map.ColorScale, "categorical", StringComparison.OrdinalIgnoreCase);

        private static string? GetExplicitFill(string? color)
        {
            if (string.IsNullOrWhiteSpace(color)) return null;
            var trimmed = color.Trim();
            return trimmed.StartsWith('#') && (trimmed.Length == 7 || trimmed.Length == 4)
                ? trimmed
                : null;
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
                Items = items
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
                    r.GetString("fill_color")
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

        private static IReadOnlyList<MapEntryDetail> BuildDetails(TableRow row, PageMap map)
        {
            var keys = GetDetailKeys(map);

            return keys
                .Select(key => (Key: key, Value: row.GetString(key)))
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => new MapEntryDetail(FormatLabel(x.Key), x.Value))
                .ToList();
        }

        private static IReadOnlyList<string> GetDetailKeys(PageMap map)
        {
            if (map.DetailKeys.Count > 0)
                return map.DetailKeys;

            return string.IsNullOrWhiteSpace(map.NameKey)
                ? Array.Empty<string>()
                : new[] { map.NameKey };
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
