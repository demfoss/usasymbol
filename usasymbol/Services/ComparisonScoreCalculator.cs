using USASymbol.Models;
using USASymbol.Models.ViewModels;

namespace USASymbol.Services
{
    /// <summary>Per-metric-group win tally for a scored state pair, e.g. "AL wins Housing".</summary>
    public class ComparisonGroupWin
    {
        public string Group { get; set; } = "";
        public State Winner { get; set; } = null!;
        public int WinsA { get; set; }
        public int WinsB { get; set; }
        public int Total { get; set; }
    }

    /// <summary>Overall "X / Y metrics won" score for a state pair, used by the Score card on comparison pages.</summary>
    public class ComparisonScoreResult
    {
        public State StateA { get; set; } = null!;
        public State StateB { get; set; } = null!;
        public int WinsA { get; set; }
        public int WinsB { get; set; }
        public int Total { get; set; }
        public State? Winner { get; set; }
        public List<ComparisonGroupWin> GroupWins { get; set; } = new();
    }

    /// <summary>Computes the overall win/loss score shown on state comparison pages (Compare/Overview and the CategoryHub featured-comparison card).</summary>
    public static class ComparisonScoreCalculator
    {
        // Only numeric metrics with an actual winner count; geography-only pairs will have 0 wins.
        private static readonly HashSet<string> ScorableSlugs = new()
        {
            "population","density","college-educated","median-income","minimum-wage","poverty-rate",
            "employment-population-ratio","regional-price-parity","livability-score","cost-of-living",
            "gas-price","electricity-rates","average-temperature","summer-temperature","winter-temperature","sunny-days","annual-precipitation",
            "commute-time","home-value","median-rent","owner-costs-with-mortgage",
            "owner-costs-without-mortgage","homeownership-rate","home-value-to-income",
            "rent-to-income","owner-costs-to-income","income-tax","sales-tax","property-tax","land-area","highest-point",
            "life-expectancy","violent-crime"
        };

        public static ComparisonScoreResult Calculate(StatePairComparisonViewModel pair)
        {
            var scorable = pair.MetricResults
                .Where(r => ScorableSlugs.Contains(r.Metric.Slug) && r.WinnerSlug != null)
                .ToList();

            var winsA = scorable.Count(r => r.WinnerSlug == pair.StateA.Slug);
            var winsB = scorable.Count(r => r.WinnerSlug == pair.StateB.Slug);
            var total = winsA + winsB;

            State? winner = total == 0 ? null
                : winsA > winsB ? pair.StateA
                : winsB > winsA ? pair.StateB
                : null; // tie

            var groupWins = scorable
                .GroupBy(r => new { r.Metric.GroupSlug, r.Metric.GroupName })
                .Select(g => new
                {
                    Group = g.Key.GroupName,
                    WinsA = g.Count(r => r.WinnerSlug == pair.StateA.Slug),
                    WinsB = g.Count(r => r.WinnerSlug == pair.StateB.Slug),
                    Total = g.Count()
                })
                .Where(g => g.WinsA != g.WinsB) // skip tied groups
                .OrderByDescending(g => g.Total)
                .Select(g => new ComparisonGroupWin
                {
                    Group = g.Group,
                    Winner = g.WinsA > g.WinsB ? pair.StateA : pair.StateB,
                    WinsA = g.WinsA,
                    WinsB = g.WinsB,
                    Total = g.Total
                })
                .ToList();

            return new ComparisonScoreResult
            {
                StateA = pair.StateA,
                StateB = pair.StateB,
                WinsA = winsA,
                WinsB = winsB,
                Total = total,
                Winner = winner,
                GroupWins = groupWins
            };
        }

        /// <summary>Same score card, scoped to metrics within a single category (e.g. for a "Featured Comparison" card on a category hub page). No group-win chips, since every scored metric already belongs to the same group.</summary>
        public static ComparisonScoreResult CalculateForCategory(StatePairComparisonViewModel pair, string categorySlug)
        {
            var scorable = pair.MetricResults
                .Where(r => string.Equals(r.Metric.GroupSlug, categorySlug, StringComparison.OrdinalIgnoreCase) && r.WinnerSlug != null)
                .ToList();

            var winsA = scorable.Count(r => r.WinnerSlug == pair.StateA.Slug);
            var winsB = scorable.Count(r => r.WinnerSlug == pair.StateB.Slug);
            var total = winsA + winsB;

            State? winner = total == 0 ? null
                : winsA > winsB ? pair.StateA
                : winsB > winsA ? pair.StateB
                : null; // tie

            return new ComparisonScoreResult
            {
                StateA = pair.StateA,
                StateB = pair.StateB,
                WinsA = winsA,
                WinsB = winsB,
                Total = total,
                Winner = winner,
                GroupWins = new List<ComparisonGroupWin>()
            };
        }
    }
}
