using System.Globalization;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;

namespace Usasymbol.Helpers;

public sealed record RankingPlacement(int Rank, int TieCount, int TotalCount, bool IsLast)
{
    public string Label => TieCount > 1 ? $"T-{Rank}" : Rank.ToString(CultureInfo.InvariantCulture);
    public string Tooltip => TieCount > 1
        ? $"Tied with {TieCount - 1} other {(TieCount == 2 ? "state" : "states")}."
        : $"Ranked #{Rank} of {TotalCount} states.";
    public string Context => TieCount == TotalCount
        ? $"All {TotalCount} states have the same figure."
        : TieCount > 1 ? $"Tied for {(Rank == 1 ? "first" : IsLast ? "last" : $"#{Rank}")} with {TieCount - 1} other {(TieCount == 2 ? "state" : "states")}."
        : IsLast ? $"Ranked last among {TotalCount} states." : Tooltip;
}

/// <summary>One competition rank and tie order for tables, cards, pickers and extremes.</summary>
public sealed class RankingPresentation
{
    public RankingNumericInsightsViewModel Insights { get; }
    public bool Ascending { get; }
    public IReadOnlyList<RankingNumericPoint> Ordered { get; }
    private readonly Dictionary<TableRow, RankingPlacement> placements = new();

    private RankingPresentation(PageTable table, RankingNumericInsightsViewModel insights)
    {
        Insights = insights;
        Ascending = IsAscending(table, insights.Ranked);
        Ordered = (Ascending
                ? insights.Ranked.OrderBy(p => p.Value)
                : insights.Ranked.OrderByDescending(p => p.Value))
            .ThenBy(p => p.State, StringComparer.OrdinalIgnoreCase).ToList();

        var position = 1;
        foreach (var group in Ordered.GroupBy(p => p.Value))
        {
            var placement = new RankingPlacement(position, group.Count(), Ordered.Count,
                group.Key == Ordered[^1].Value);
            foreach (var point in group) placements[point.Row] = placement;
            position += group.Count();
        }
    }

    public RankingPlacement? For(TableRow row) => placements.GetValueOrDefault(row);

    // Preserve nonnumeric/unranked rows, which do not participate in competition ranks.
    public IEnumerable<TableRow> Rows(PageTable table) => Ordered.Select(p => p.Row)
        .Concat(table.Rows.Where(row => !placements.ContainsKey(row)));

    public static RankingPresentation? Create(PageTable? table)
    {
        var insights = RankingNumericInsightsViewModel.Create(table);
        return insights == null ? null : new RankingPresentation(table!, insights);
    }

    private static bool IsAscending(PageTable table, IReadOnlyList<RankingNumericPoint> points)
    {
        if (string.Equals(table.SortDefault, "asc", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(table.SortDefault, "desc", StringComparison.OrdinalIgnoreCase)) return false;
        var firstRanked = points.OrderBy(p => int.TryParse(p.Row.GetString("rank"), out var rank)
            ? rank : int.MaxValue).First();
        if (int.TryParse(firstRanked.Row.GetString("rank"), out _))
            return firstRanked.Value == points[^1].Value && points[0].Value != points[^1].Value;
        var source = table.Rows.Where(row => points.Any(p => p.Row == row)).ToList();
        return points.First(p => p.Row == source[0]).Value < points.First(p => p.Row == source[^1]).Value;
    }
}

public sealed record RankingExtremeGroup(double Value, string Heading, IReadOnlyList<RankingNumericPoint> States);
public sealed record RankingExtremeListModel(RankingExtremesPresentation Presentation, RankingExtremeSide Side, PageTable Table, int MobileRows = 5,
    IReadOnlyDictionary<string, string>? Colors = null);
public sealed record RankingExtremeRowModel(RankingExtremesPresentation Presentation, RankingNumericPoint Point, PageTable Table, int Index,
    IReadOnlyDictionary<string, string>? Colors = null);

public sealed record RankingExtremeSide(bool Highest, bool Grouped,
    IReadOnlyList<RankingNumericPoint> Rows, IReadOnlyList<RankingExtremeGroup> Groups)
{
    public string Label => Highest ? "Highest" : "Lowest";
    public string Id => Highest ? "ranking-extremes-highest" : "ranking-extremes-lowest";
}

public sealed class RankingExtremesPresentation
{
    public RankingPresentation Ranking { get; }
    public RankingExtremeSide? Top { get; }
    public RankingExtremeSide? Bottom { get; }
    public string UnitLabel { get; }
    public bool Diverging { get; }
    public double Mean { get; }
    public string MeanDisplay { get; }
    public NationalReferenceView? National { get; }
    public double BaselinePercent { get; }
    private readonly double scale;

    public RankingExtremesPresentation(PageContent content, RankingPresentation ranking, bool showTop, bool showBottom, int sideLimit = 10)
    {
        Ranking = ranking;
        var table = content.Table!;
        var column = ranking.Insights.MetricColumn;
        UnitLabel = string.Equals(content.Map?.MetricKey, column.Key, StringComparison.OrdinalIgnoreCase)
            ? content.Map?.MetricLabel ?? column.Label : column.Label;
        var points = ranking.Insights.Ranked;
        National = NationalReferenceResolver.Resolve(table, ranking.Insights);
        Mean = National?.Value ?? points.Average(p => p.Value);
        MeanDisplay = National?.Display ?? Mean.ToString("N1", CultureInfo.GetCultureInfo("en-US"));
        // Rates are comparisons, not additive totals. Label the unweighted state mean explicitly.
        Diverging = column.Key.ToLowerInvariant().Split('_').Any(part => part is "rate" or "rates" or "pct" or "percent" or "percentage")
            || column.Label.Contains('%');
        var min = Math.Min(0, points.Min(p => p.Value));
        var max = Math.Max(0, points.Max(p => p.Value));
        scale = Diverging ? points.Max(p => Math.Abs(p.Value - Mean)) : max - min;
        BaselinePercent = Diverging ? 50 : scale > 0 ? -min / scale * 100 : 0;
        if (showTop) Top = SelectSide(!ranking.Ascending);
        if (showBottom) Bottom = SelectSide(ranking.Ascending);

        RankingExtremeSide SelectSide(bool highest)
        {
            var ordered = (highest ? points.OrderByDescending(p => p.Value) : points.OrderBy(p => p.Value))
                .ThenBy(p => p.State, StringComparer.OrdinalIgnoreCase).ToList();
            var limit = Math.Min(sideLimit, ordered.Count);
            var boundary = ordered[limit - 1].Value;
            var selected = ordered.TakeWhile((point, index) => index < limit || point.Value == boundary).ToList();
            var groups = selected.GroupBy(p => p.Value).Select(group =>
            {
                var value = group.First().DisplayValue;
                var unit = group.Key == 1 ? table.Metric?.UnitSingular : table.Metric?.UnitPlural;
                var heading = string.IsNullOrWhiteSpace(unit)
                    ? $"{column.Label}: {value} ({group.Count()} states)"
                    : $"States with {(group.Key == 1 && !highest ? "just " : "")}{value} {unit} ({group.Count()})";
                return new RankingExtremeGroup(group.Key, heading, group.ToList());
            }).ToList();
            return new RankingExtremeSide(highest, selected.Count > limit || groups.Count < 3, selected, groups);
        }
    }

    public (double Left, double Width) Bar(double value)
    {
        if (scale <= 0) return (BaselinePercent, 0);
        var end = Diverging ? 50 + (value - Mean) / scale * 50 : BaselinePercent + value / scale * 100;
        end = Math.Clamp(end, 0, 100);
        return (Math.Min(end, BaselinePercent), Math.Abs(end - BaselinePercent));
    }

    public string Tone(PageTable table, bool highest) => table.HigherIsBetter is not bool higherIsBetter
        ? "" : higherIsBetter == highest ? "ranking-extreme-list--good" : "ranking-extreme-list--bad";

    public string BarTone(PageTable table, double value) => Diverging ? Tone(table, value >= Mean) : "";
}
