using System.Globalization;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;

namespace Usasymbol.Helpers
{
    public sealed class TakeawayState
    {
        public string Name { get; init; } = "";
        public string Slug { get; init; } = "";
    }

    public sealed class TakeawayFact
    {
        public string Text { get; init; } = "";
        public List<TakeawayState> States { get; init; } = new();
    }

    public sealed class TakeawayStateOption
    {
        public string Name { get; init; } = "";
        public string Slug { get; init; } = "";
        public int Rank { get; init; }
        public string RankLabel { get; init; } = "";
        public double Number { get; init; }
        public string Statement { get; init; } = "";
        public string Context { get; init; } = "";
        public double Position { get; init; }
        public bool IsTypical { get; init; }
        public string Postal { get; init; } = "";
    }

    public sealed class RankingTakeaways
    {
        public string Summary { get; init; } = "";
        public List<TakeawayStateOption> Options { get; init; } = new();
        public List<TakeawayFact> Facts { get; init; } = new();
        public string TypicalStatement { get; init; } = "";
        public double TypicalPosition { get; init; }
        public TakeawayStateOption Leader { get; init; } = null!;
        public TakeawayStateOption Alternative { get; init; } = null!;
        public bool HasSpread { get; init; }
        public List<double> DotPositions { get; init; } = new();
        public string MinLabel { get; init; } = "";
        public string MaxLabel { get; init; } = "";
        public bool LogScale { get; init; }
    }

    /// <summary>Shared state-ranking insights. Optional facts require explicit metric metadata.</summary>
    public static class RankingTakeawaysBuilder
    {
        private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

        public static RankingTakeaways? Build(PageDetailViewModel model)
        {
            var content = model.Content;
            var table = content?.Table;
            if (table == null) return null;
            var stateTable = new PageTable
            {
                Columns = table.Columns,
                DefaultColumn = table.DefaultColumn,
                SortDefault = table.SortDefault,
                Rows = table.Rows.Where(row => KeyTakeawaysBuilder.IsState(row.GetString("state_slug"))).ToList()
            };
            var presentation = RankingPresentation.Create(stateTable);
            var insights = presentation?.Insights;
            if (insights == null) return null;

            var ranked = insights.Ranked;
            // A state picker cannot represent multiple cities, items or years for the same state.
            if (ranked.Select(p => p.StateSlug).Distinct(StringComparer.OrdinalIgnoreCase).Count() != ranked.Count)
                return null;

            var column = insights.MetricColumn;
            var metric = table!.Metric;
            var max = ranked[0].Value;
            var min = ranked[^1].Value;
            var median = insights.Median;
            var ordered = presentation!.Ordered;

            var rawValues = ranked.Select(p => p.Row.GetString(column.Key)).ToList();
            var prefix = rawValues.Any(v => v.Contains('$')) && !Format(1, column).Contains('$') ? "$" : "";
            var suffix = rawValues.Any(v => v.Contains('%')) && !Format(1, column).Contains('%') ? "%" : "";
            string Show(double value) => prefix + Format(value, column) + suffix;
            string Amount(double value) => HasUnits(metric)
                ? $"{Show(value)} {Unit(metric, value)}" : Show(value);
            string SentenceAmount(double value) => HasUnits(metric)
                ? $"{ProseNumber(value, column)} {Unit(metric, value)}" : Show(value);
            // Strongly skewed positive data would pile every dot against the left edge on a linear axis.
            var logScale = min > 0 && max / min >= 40;
            double Scale(double value) => max == min ? 50 : logScale
                ? Math.Clamp((Math.Log(value) - Math.Log(min)) / (Math.Log(max) - Math.Log(min)) * 100, 0, 100)
                : Math.Clamp((value - Math.Min(0, min)) / (max - Math.Min(0, min)) * 100, 0, 100);
            string ShortLabel(double value) => prefix + Format(value, column) + suffix;
            string ContextFor(RankingPlacement placement, double value)
            {
                if (placement.TieCount > 1 || placement.IsLast || placement.Rank == 1) return placement.Context;
                var others = ranked.Count - 1;
                var beaten = placement.TotalCount - placement.Rank;
                return $"Ranked #{placement.Rank} of {placement.TotalCount}, ahead of {beaten} of the other {others} states.";
            }

            var options = ranked.OrderBy(p => p.State, StringComparer.OrdinalIgnoreCase)
                .Select(p => new TakeawayStateOption
                {
                    Name = p.State,
                    Slug = p.StateSlug,
                    Rank = presentation.For(p.Row)!.Rank,
                    RankLabel = presentation.For(p.Row)!.Label,
                    Number = p.Value,
                    Statement = HasUnits(metric)
                        ? $"{p.State} has {Amount(p.Value)}"
                        : $"{p.State} is at {Show(p.Value)}",
                    Context = ContextFor(presentation.For(p.Row)!, p.Value),
                    Position = Scale(p.Value),
                    IsTypical = Math.Abs(p.Value - median) < 1e-9,
                    Postal = p.Row.GetString("postal_code")
                }).ToList();

            var values = ranked.Select(p => p.Value).OrderBy(v => v).ToList();
            var q1 = Quantile(values, .25);
            var q3 = Quantile(values, .75);
            // For counts use inclusive whole-number bounds, rather than fractional objects.
            var wholeNumbers = values.All(v => v == Math.Truncate(v));
            if (wholeNumbers)
            {
                q1 = Math.Floor(q1);
                q3 = Math.Ceiling(q3);
            }
            var typical = wholeNumbers ? Math.Round(median, MidpointRounding.AwayFromZero) : median;
            // Plain wording: the leader, what a typical state has, and the range half of all states fall in.
            // The unit is named once, in the first sentence; later numbers stay bare.
            var range = $"{Show(q1)} and {Show(q3)}";
            var higherIsBad = table.HigherIsBetter == false;
            string summary;
            if (q1 == q3)
                summary = $"Half of all states share the same figure: {SentenceAmount(q1)}.";
            else if (!presentation.Ascending && min >= 0 && median > 0 && max != min &&
                     ordered.Count(p => p.Value == max) == 1)
                summary = (higherIsBad
                              ? $"{ordered[0].State} is highest at {SentenceAmount(max)}."
                              : $"{ordered[0].State} leads with {SentenceAmount(max)}.")
                          + $" A typical state has about {Show(typical)}, and half of all states fall between {range}.";
            else
                summary = $"Half of all states fall between {range}.";
            if (ranked.Count != table.Rows.Count)
                summary += $" Based on {ranked.Count} states with numeric data.";

            var facts = new List<TakeawayFact>();
            var floor = ranked.Where(p => p.Value == min).OrderBy(p => p.State).ToList();
            if (floor.Count > 1 && floor.Count < ranked.Count)
            {
                facts.Add(new TakeawayFact
                {
                    Text = HasUnits(metric)
                        ? $"{Capitalize(CountWord(floor.Count))} states each have {SentenceAmount(min)}."
                        : $"{Capitalize(CountWord(floor.Count))} states share the lowest figure of {Show(min)} for {ResolveLabel(content!, column)}.",
                    States = floor.Select(p => new TakeawayState { Name = p.State, Slug = p.StateSlug }).ToList()
                });
            }

            // Never sum rates, percentages, salaries, scores or ranks by guessing from their label.
            if (metric?.Additive == true && min >= 0 && ranked.Sum(p => p.Value) > 0)
            {
                var share = ranked.Take(3).Sum(p => p.Value) / ranked.Sum(p => p.Value) * 100;
                if (ranked.Count > 3 && max != min) facts.Add(new TakeawayFact
                {
                    Text = $"The three largest state totals account for {share.ToString("0.#", En)}% of the combined total across these states."
                });

                if (!string.IsNullOrWhiteSpace(metric.PopulationKey) &&
                    ranked.All(p => TryNumber(p.Row, metric.PopulationKey, out var population) && population > 0))
                {
                    var perPerson = ranked.Select(p =>
                    {
                        TryNumber(p.Row, metric.PopulationKey, out var population);
                        return new { Point = p, Population = population, Rate = p.Value / population };
                    }).OrderByDescending(p => p.Rate).ToList();
                    var first = perPerson[0];
                    // Do not turn another tie into an arbitrary per-person leader.
                    if (first.Rate > perPerson[1].Rate && first.Rate > 0 && HasUnits(metric))
                        facts.Add(new TakeawayFact
                        {
                            Text = $"Per person, {first.Point.State} has the most {metric.UnitPlural}: one {metric.UnitSingular} per {(first.Population / first.Point.Value).ToString("N0", En)} residents."
                        });
                }
            }

            if (!string.IsNullOrWhiteSpace(metric?.PreviousValueKey) && metric.PreviousYear.HasValue)
            {
                var changes = ranked.Select(p => new
                {
                    Point = p,
                    Previous = TryNumber(p.Row, metric.PreviousValueKey, out var previous) ? (double?)previous : null
                }).ToList();
                if (changes.All(p => p.Previous.HasValue))
                {
                    var biggest = changes.OrderByDescending(p => Math.Abs(p.Point.Value - p.Previous!.Value)).ToList();
                    var change = biggest[0].Point.Value - biggest[0].Previous!.Value;
                    if (change != 0 && Math.Abs(change) > Math.Abs(biggest[1].Point.Value - biggest[1].Previous!.Value))
                        facts.Add(new TakeawayFact
                        {
                            Text = $"{biggest[0].Point.State} has changed the most since {metric.PreviousYear}: from {Amount(biggest[0].Previous!.Value)} to {Amount(biggest[0].Point.Value)}."
                        });
                }
            }

            return new RankingTakeaways
            {
                Summary = summary,
                TypicalStatement = HasUnits(metric)
                    ? $"A typical state has about {SentenceAmount(typical)}."
                    : $"A typical state has about {Show(typical)}.",
                TypicalPosition = Scale(median),
                Options = options,
                Facts = facts,
                Leader = options.First(p => p.Slug == ordered[0].StateSlug),
                Alternative = options.First(p => p.Slug == (ordered.FirstOrDefault(p => p.Value != ordered[0].Value) ?? ordered[1]).StateSlug),
                HasSpread = max != min,
                DotPositions = ranked.Select(p => Math.Round(Scale(p.Value), 2)).ToList(),
                MinLabel = ShortLabel(min),
                MaxLabel = ShortLabel(max),
                LogScale = logScale
            };
        }

        private static bool HasUnits(RankingMetric? metric) =>
            !string.IsNullOrWhiteSpace(metric?.UnitSingular) && !string.IsNullOrWhiteSpace(metric.UnitPlural);
        private static string Unit(RankingMetric? metric, double value) => value == 1 ? metric!.UnitSingular! : metric!.UnitPlural!;
        private static string ResolveLabel(PageContent content, TableColumn column) =>
            (string.Equals(content.Map?.MetricKey, column.Key, StringComparison.OrdinalIgnoreCase)
                ? content.Map?.MetricLabel ?? column.Label : column.Label).Trim().ToLowerInvariant();
        private static string Format(double value, TableColumn column)
        {
            var format = column.Format ?? "N2";
            if (format == "N0" && value != Math.Truncate(value)) format = "N1";
            try { return value.ToString(format, En); }
            catch (FormatException) { return value.ToString("N2", En); }
        }
        private static string ProseNumber(double value, TableColumn column) =>
            value >= 0 && value <= 12 && value == Math.Truncate(value) ? CountWord((int)value) : Format(value, column);
        private static string CountWord(int count) => count switch
        {
            0 => "zero", 1 => "one", 2 => "two", 3 => "three", 4 => "four", 5 => "five",
            6 => "six", 7 => "seven", 8 => "eight", 9 => "nine", 10 => "ten", 11 => "eleven", 12 => "twelve",
            _ => count.ToString("N0", En)
        };
        private static string Capitalize(string text) => char.ToUpperInvariant(text[0]) + text[1..];
        private static double Quantile(IReadOnlyList<double> values, double fraction)
        {
            var index = (values.Count - 1) * fraction;
            var lower = (int)Math.Floor(index);
            var upper = (int)Math.Ceiling(index);
            return values[lower] + (values[upper] - values[lower]) * (index - lower);
        }
        private static bool TryNumber(TableRow row, string key, out double value) =>
            double.TryParse(row.GetString(key).Replace(",", "").Replace("$", "").Replace("%", "").Replace("−", "-"),
                NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);
    }
}
