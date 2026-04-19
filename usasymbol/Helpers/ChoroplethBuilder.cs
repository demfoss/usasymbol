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

            var nameKey = map.NameKey;
            var parsed = rows
                .Select(r => (
                    postal:   GetPostal(r),
                    name:     r.GetString("state"),
                    slug:     r.GetString("state_slug"),
                    rank:     TryInt(r, "rank") ?? TryInt(r, "order"),
                    raw:      TryDouble(r, key),
                    display:  FormatValue(r, key),
                    subLabel: !string.IsNullOrWhiteSpace(nameKey) ? r.GetString(nameKey) : null
                ))
                .Where(x => !string.IsNullOrEmpty(x.postal))
                .ToList();

            var values = parsed.Where(x => x.raw.HasValue).Select(x => x.raw!.Value).ToList();
            if (!values.Any()) return BuildFlat(rows);

            double min = values.Min();
            double max = values.Max();

            var entries = parsed.Select(x =>
            {
                string fill;
                if (!x.raw.HasValue || min >= max)
                    fill = NoDataColor;
                else
                {
                    double t = useLog ? LogScale(x.raw.Value, min, max) : LinearScale(x.raw.Value, min, max);
                    fill = Lerp(t, scheme.light, scheme.dark);
                }
                return new StateMapEntry(x.postal, x.name, x.slug, x.raw, x.rank, x.display, fill, x.subLabel);
            }).ToList();

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

        public static ChoroplethResult BuildFlat(List<TableRow> rows)
        {
            var entries = rows
                .Select(r =>
                {
                    var postal = GetPostal(r);
                    if (string.IsNullOrEmpty(postal)) return null;

                    var displayKey = r.Data.Keys
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
                        FlatColor
                    );
                })
                .Where(e => e != null)
                .Cast<StateMapEntry>()
                .ToList();

            return new ChoroplethResult { Entries = entries };
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
