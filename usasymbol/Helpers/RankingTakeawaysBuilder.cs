using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using USASymbol.Models.ViewModels;

namespace Usasymbol.Helpers
{
    public sealed class TakeawayState
    {
        public string Name { get; init; } = "";
        public string Slug { get; init; } = "";
    }

    public sealed class TakeawayCard
    {
        /// <summary>"Highest", "#3", "Lowest", "Median".</summary>
        public string Role { get; init; } = "";
        /// <summary>high | low | mid | neutral - drives colour; see <see cref="RankingTakeaways.HigherIsBetter"/>.</summary>
        public string Tone { get; init; } = "neutral";
        public List<TakeawayState> States { get; init; } = new();
        public int ExtraStates { get; init; }
        public string Value { get; init; } = "";
        public string Context { get; init; } = "";
        public double BarPercent { get; init; }
    }

    public sealed class TakeawayStateOption
    {
        public string Name { get; init; } = "";
        public string Slug { get; init; } = "";
        public int Rank { get; init; }
        public double Number { get; init; }
        public string Value { get; init; } = "";
        public string Context { get; init; } = "";
    }

    public sealed class RankingTakeaways
    {
        public string Summary { get; init; } = "";
        public string Unit { get; init; } = "";
        public bool? HigherIsBetter { get; init; }
        public int RankedCount { get; init; }
        public List<TakeawayCard> Cards { get; init; } = new();
        public List<TakeawayStateOption> Options { get; init; } = new();
        public string MedianValue { get; init; } = "";
    }

    /// <summary>
    /// Builds the ranking "Key takeaways" block from the structured table (state, value, rank)
    /// instead of parsing prose, so subjects, numbers and ties are always consistent with the table.
    /// </summary>
    public static class RankingTakeawaysBuilder
    {
        private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

        public static RankingTakeaways? Build(PageDetailViewModel model)
        {
            var content = model?.Content;
            var insights = RankingNumericInsightsViewModel.Create(content?.Table);
            if (insights == null || insights.Ranked.Count < 4)
                return null;

            var ranked = insights.Ranked;
            var column = insights.MetricColumn;
            var max = ranked[0].Value;
            var min = ranked[^1].Value;
            if (Math.Abs(max - min) < 1e-9)
                return null;

            // Small ordinal/categorical scales produce technically correct but
            // unhelpful superlatives (for example, governor age requirements).
            if (ranked.Select(p => p.Value).Distinct().Count() < 10)
                return null;

            // Rank-valued metrics (lower = better, e.g. best-states-for-fishing) would make
            // "Highest" point at the wrong state: if table rank #1 is not the max value, skip.
            var rankOne = ranked.FirstOrDefault(p => p.Row.GetString("rank").Trim() == "1");
            if (rankOne != null && Math.Abs(rankOne.Value - max) > 1e-9)
                return null;

            var unit = ResolveUnit(content!, column);
            var hib = content!.Table?.HigherIsBetter;
            var median = insights.Median;

            string Show(RankingNumericPoint p) => DisplayFor(p, column);
            string Delta(double v) => DeltaText(v, median);

            // Competition ranking by value: ties share a rank, the next distinct value skips ahead.
            var groups = ranked.GroupBy(p => p.Value).OrderByDescending(g => g.Key).ToList();
            var rankOfGroup = new Dictionary<double, int>();
            var running = 1;
            foreach (var g in groups)
            {
                rankOfGroup[g.Key] = running;
                running += g.Count();
            }

            var top = groups[0].ToList();
            var bottom = groups[^1].ToList();
            var lowestIsCrowded = bottom.Count >= 3;

            var cards = new List<TakeawayCard>
            {
                new()
                {
                    Role = "Highest",
                    Tone = ToneFor(true, hib),
                    States = ToStates(top),
                    ExtraStates = Math.Max(0, top.Count - 2),
                    Value = Show(top[0]),
                    Context = Delta(max),
                    BarPercent = BarPercent(max, min, max)
                }
            };

            if (groups.Count >= 3)
            {
                var second = groups[1].ToList();
                cards.Add(new TakeawayCard
                {
                    Role = "#" + rankOfGroup[groups[1].Key],
                    Tone = "neutral",
                    States = ToStates(second),
                    ExtraStates = Math.Max(0, second.Count - 2),
                    Value = Show(second[0]),
                    Context = Delta(second[0].Value),
                    BarPercent = BarPercent(second[0].Value, min, max)
                });
            }

            if (lowestIsCrowded)
            {
                // "Lowest" is meaningless when many states share the floor value (e.g. one area code each).
                var nearest = ranked.OrderBy(p => Math.Abs(p.Value - median)).First();
                cards.Add(new TakeawayCard
                {
                    Role = "Median",
                    Tone = "mid",
                    States = new List<TakeawayState> { new() { Name = nearest.State, Slug = nearest.StateSlug } },
                    Value = insights.MedianDisplay,
                    Context = $"{bottom.Count} states share the lowest value ({Show(bottom[0])})",
                    BarPercent = BarPercent(median, min, max)
                });
            }
            else
            {
                cards.Add(new TakeawayCard
                {
                    Role = "Lowest",
                    Tone = ToneFor(false, hib),
                    States = ToStates(bottom),
                    ExtraStates = Math.Max(0, bottom.Count - 2),
                    Value = Show(bottom[0]),
                    Context = Delta(min),
                    BarPercent = BarPercent(min, min, max)
                });
            }

            var options = ranked
                .GroupBy(p => p.StateSlug, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(p => p.State, StringComparer.OrdinalIgnoreCase)
                .Select(p => new TakeawayStateOption
                {
                    Name = p.State,
                    Slug = p.StateSlug,
                    Rank = rankOfGroup[p.Value],
                    Number = p.Value,
                    Value = Show(p),
                    Context = Delta(p.Value)
                })
                .ToList();

            return new RankingTakeaways
            {
                Summary = BuildSummary(top, bottom, groups.Count, max, min, unit, column, ranked, lowestIsCrowded, Show),
                Unit = unit,
                HigherIsBetter = hib,
                RankedCount = ranked.Count,
                Cards = cards,
                Options = options,
                MedianValue = insights.MedianDisplay
            };
        }

        // Sentences are assembled from computed facts (ties, gap, spread) so they differ page to page.
        private static string BuildSummary(
            List<RankingNumericPoint> top, List<RankingNumericPoint> bottom, int distinctValues,
            double max, double min, string unit, USASymbol.Models.Content.TableColumn column,
            IReadOnlyList<RankingNumericPoint> ranked, bool lowestIsCrowded, Func<RankingNumericPoint, string> show)
        {
            var unitPart = string.IsNullOrWhiteSpace(unit) ? "" : $" ({unit})";
            var lead = top.Count == 1
                ? $"{top[0].State} leads at {show(top[0])}{unitPart}"
                : $"{JoinNames(top)} tie for first at {show(top[0])}{unitPart}";

            string tail;
            if (lowestIsCrowded)
            {
                tail = $"{bottom.Count} states share the lowest value, {show(bottom[0])}";
            }
            else
            {
                tail = bottom.Count == 1
                    ? $"{bottom[0].State} is lowest at {show(bottom[0])}"
                    : $"{JoinNames(bottom)} are lowest at {show(bottom[0])}";
            }

            var gap = "";
            if (min > 0 && max / min >= 1.15 && !lowestIsCrowded)
            {
                var ratio = max / min;
                gap = ratio >= 10
                    ? $" The top value is {ratio.ToString("N0", En)}× the bottom."
                    : $" The top value is {ratio.ToString("N1", En)}× the bottom.";
            }
            else if (max - min > 0 && ranked.Count >= 3)
            {
                gap = $" The median state sits at {show(ranked[ranked.Count / 2])}.";
            }

            return $"{lead}; {tail}.{gap}";
        }

        private static string JoinNames(List<RankingNumericPoint> pts)
        {
            var names = pts.Select(p => p.State).ToList();
            if (names.Count <= 2) return string.Join(" and ", names);
            return $"{names[0]}, {names[1]} and {names.Count - 2} more";
        }

        private static List<TakeawayState> ToStates(List<RankingNumericPoint> pts) =>
            pts.Take(2).Select(p => new TakeawayState { Name = p.State, Slug = p.StateSlug }).ToList();

        private static string ToneFor(bool isHigh, bool? higherIsBetter)
        {
            // Counts and descriptive facts have no good/bad direction. Keep both
            // extremes visually neutral until the content explicitly opts in.
            if (higherIsBetter == null) return "neutral";
            var good = isHigh == higherIsBetter.Value;
            return good ? "good" : "bad";
        }

        private static string ResolveUnit(USASymbol.Models.Content.PageContent content, USASymbol.Models.Content.TableColumn column)
        {
            var map = content.Map;
            if (map != null && !string.IsNullOrWhiteSpace(map.MetricLabel) &&
                string.Equals(map.MetricKey, column.Key, StringComparison.OrdinalIgnoreCase))
                return map.MetricLabel!.Trim();
            return column.Label?.Trim() ?? "";
        }

        private static string DisplayFor(RankingNumericPoint p, USASymbol.Models.Content.TableColumn column)
        {
            // Keep "$", "%" and similar decorations that the plain number format would drop.
            if (p.Row.Data.TryGetValue(column.Key, out var raw) && raw is string s &&
                (s.Contains('$') || s.Contains('%')))
                return s.Trim();
            return p.DisplayValue;
        }

        private static string DeltaText(double value, double median)
        {
            if (Math.Abs(median) < 1e-9 || median < 0 || value < 0) return "";
            var pct = (value - median) / median * 100d;
            if (Math.Abs(pct) < 0.5) return "At the median";
            if (value / median >= 2d)
                return $"{(value / median).ToString("0.#", En)}× the median state";
            var rounded = Math.Round(Math.Abs(pct), 0);
            return pct > 0
                ? $"+{rounded.ToString("N0", En)}% vs median state"
                : $"−{rounded.ToString("N0", En)}% vs median state";
        }

        private static double BarPercent(double value, double min, double max)
        {
            if (min >= 0) return max <= 0 ? 0 : Math.Clamp(value / max * 100d, 4, 100);
            var range = max - min;
            return range <= 0 ? 0 : Math.Clamp((value - min) / range * 100d, 4, 100);
        }
    }
}
