using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public sealed class RankingNumericPoint
    {
        public TableRow Row { get; init; } = null!;
        public double Value { get; init; }
        public string State { get; init; } = "";
        public string StateSlug { get; init; } = "";
        public string DisplayValue { get; init; } = "";
        public double BarBottomPercent { get; init; }
        public double BarHeightPercent { get; init; }
    }

    public sealed class RankingNumericInsightsViewModel
    {
        private static readonly HashSet<string> StateSlugs = new(StringComparer.OrdinalIgnoreCase)
        {
            "alabama", "alaska", "arizona", "arkansas", "california", "colorado",
            "connecticut", "delaware", "florida", "georgia", "hawaii", "idaho",
            "illinois", "indiana", "iowa", "kansas", "kentucky", "louisiana",
            "maine", "maryland", "massachusetts", "michigan", "minnesota",
            "mississippi", "missouri", "montana", "nebraska", "nevada",
            "new-hampshire", "new-jersey", "new-mexico", "new-york",
            "north-carolina", "north-dakota", "ohio", "oklahoma", "oregon",
            "pennsylvania", "rhode-island", "south-carolina", "south-dakota",
            "tennessee", "texas", "utah", "vermont", "virginia", "washington",
            "west-virginia", "wisconsin", "wyoming"
        };

        public string MetricKey { get; init; } = "";
        public string MetricLabel { get; init; } = "";
        public RankingNumericPoint Highest { get; init; } = null!;
        public RankingNumericPoint Lowest { get; init; } = null!;
        public double Median { get; init; }
        public string MedianDisplay { get; init; } = "";
        public string RangeSummary { get; init; } = "";
        public string DistributionSummary { get; init; } = "";
        public IReadOnlyList<RankingNumericPoint> Distribution { get; init; } = Array.Empty<RankingNumericPoint>();
        public bool HasFiftyStateDistribution => Distribution.Count == 50;
        public int RankedCount { get; init; }

        public static RankingNumericInsightsViewModel? Create(PageTable? table)
        {
            if (table == null || table.Rows.Count < 3)
                return null;

            var metricColumn = !string.IsNullOrWhiteSpace(table.DefaultColumn)
                ? table.Columns.FirstOrDefault(c =>
                    string.Equals(c.Key, table.DefaultColumn, StringComparison.OrdinalIgnoreCase))
                : table.Columns.FirstOrDefault(c =>
                    string.Equals(c.Type, "number", StringComparison.OrdinalIgnoreCase));

            if (metricColumn == null ||
                !string.Equals(metricColumn.Type, "number", StringComparison.OrdinalIgnoreCase))
                return null;

            var points = table.Rows
                .Select(row => TryCreatePoint(row, metricColumn))
                .Where(point => point != null)
                .Cast<RankingNumericPoint>()
                .ToList();

            if (points.Count < 3)
                return null;

            var ordered = points
                .OrderByDescending(point => point.Value)
                .ThenBy(point => point.State, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var middle = ordered.Count / 2;
            var median = ordered.Count % 2 == 0
                ? (ordered[middle - 1].Value + ordered[middle].Value) / 2d
                : ordered[middle].Value;

            var statePoints = ordered
                .Where(point => StateSlugs.Contains(point.StateSlug))
                .GroupBy(point => point.StateSlug, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            var distribution = statePoints.Count == 50
                ? BuildDistribution(statePoints)
                : new List<RankingNumericPoint>();

            var highest = ordered[0];
            var lowest = ordered[^1];

            return new RankingNumericInsightsViewModel
            {
                MetricKey = metricColumn.Key,
                MetricLabel = metricColumn.Label,
                Highest = highest,
                Lowest = lowest,
                Median = median,
                MedianDisplay = FormatValue(median, metricColumn),
                RangeSummary = BuildRangeSummary(highest.Value, lowest.Value, metricColumn),
                DistributionSummary = distribution.Count == 50
                    ? BuildDistributionSummary(distribution, metricColumn)
                    : "",
                Distribution = distribution,
                RankedCount = points.Count
            };
        }

        private static RankingNumericPoint? TryCreatePoint(TableRow row, TableColumn column)
        {
            if (!row.Data.TryGetValue(column.Key, out var rawValue) ||
                !TryParseNumber(rawValue, out var value))
                return null;

            var state = row.GetString("state").Trim();
            var stateSlug = row.GetString("state_slug").Trim();
            if (string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(stateSlug))
                return null;

            return new RankingNumericPoint
            {
                Row = row,
                Value = value,
                State = state,
                StateSlug = stateSlug,
                DisplayValue = FormatValue(value, column)
            };
        }

        private static List<RankingNumericPoint> BuildDistribution(
            IReadOnlyCollection<RankingNumericPoint> points)
        {
            var ordered = points
                .OrderByDescending(point => point.Value)
                .ThenBy(point => point.State, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var domainMin = Math.Min(0d, ordered.Min(point => point.Value));
            var domainMax = Math.Max(0d, ordered.Max(point => point.Value));
            var range = domainMax - domainMin;
            if (range <= 0)
                range = 1;

            var zeroPercent = (0d - domainMin) / range * 100d;

            return ordered.Select(point =>
            {
                var valuePercent = (point.Value - domainMin) / range * 100d;
                var bottom = Math.Min(zeroPercent, valuePercent);
                var height = Math.Abs(valuePercent - zeroPercent);

                return new RankingNumericPoint
                {
                    Row = point.Row,
                    Value = point.Value,
                    State = point.State,
                    StateSlug = point.StateSlug,
                    DisplayValue = point.DisplayValue,
                    BarBottomPercent = bottom,
                    BarHeightPercent = height
                };
            }).ToList();
        }

        private static string BuildRangeSummary(double highest, double lowest, TableColumn column)
        {
            if (Math.Abs(highest - lowest) < 0.0000001d)
                return $"All ranked states share the same {column.Label.ToLowerInvariant()} value.";

            if (lowest > 0)
            {
                var ratio = highest / lowest;
                var ratioText = ratio >= 10
                    ? ratio.ToString("N0", CultureInfo.GetCultureInfo("en-US"))
                    : ratio.ToString("N1", CultureInfo.GetCultureInfo("en-US"));
                return $"The highest value is {ratioText}× the lowest.";
            }

            var spread = highest - lowest;
            return $"The full range spans {FormatValue(spread, column)} from highest to lowest.";
        }

        private static string BuildDistributionSummary(
            IReadOnlyList<RankingNumericPoint> distribution,
            TableColumn column)
        {
            if (distribution.All(point => point.Value >= 0))
            {
                var total = distribution.Sum(point => point.Value);
                if (total > 0)
                {
                    var topThreeShare = distribution.Take(3).Sum(point => point.Value) / total * 100d;
                    return $"The top three states account for {topThreeShare.ToString("N1", CultureInfo.GetCultureInfo("en-US"))}% of the combined total.";
                }
            }

            var positive = distribution.Count(point => point.Value > 0);
            var negative = distribution.Count(point => point.Value < 0);
            if (positive > 0 && negative > 0)
                return $"{positive} states are above zero and {negative} are below zero on this measure.";

            var values = distribution.Select(point => point.Value).OrderBy(value => value).ToList();
            var lowerQuartile = values[12];
            var upperQuartile = values[37];
            return $"The middle half of states runs from {FormatValue(lowerQuartile, column)} to {FormatValue(upperQuartile, column)}.";
        }

        private static bool TryParseNumber(object? rawValue, out double value)
        {
            value = 0;
            if (rawValue == null)
                return false;

            if (rawValue is IConvertible convertible &&
                rawValue is not string)
            {
                try
                {
                    value = convertible.ToDouble(CultureInfo.InvariantCulture);
                    return !double.IsNaN(value) && !double.IsInfinity(value);
                }
                catch
                {
                    // Fall through to the string parser.
                }
            }

            var raw = rawValue.ToString()?.Trim() ?? "";
            var isParenthesized = raw.StartsWith("(") && raw.EndsWith(")");
            raw = raw
                .Replace(",", "")
                .Replace("$", "")
                .Replace("%", "")
                .Replace("−", "-")
                .Trim('(', ')', ' ');

            if (!double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                return false;

            if (isParenthesized)
                value = -value;

            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static string FormatValue(double value, TableColumn column)
        {
            var culture = CultureInfo.GetCultureInfo("en-US");
            try
            {
                return value.ToString(column.Format ?? "N2", culture);
            }
            catch (FormatException)
            {
                return value.ToString("N2", culture);
            }
        }
    }
}
