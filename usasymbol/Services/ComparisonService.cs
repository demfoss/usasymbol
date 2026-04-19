using USASymbol.Models;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using USASymbol.Services.Interface;
using usasymbol.Services.Interface;

namespace USASymbol.Services
{
    public class ComparisonService : IComparisonService
    {
        public static readonly IReadOnlyList<(string Slug1, string Slug2)> TopComparisonPairs = new List<(string, string)>
        {
            ("california", "texas"),
            ("california", "florida"),
            ("california", "new-york"),
            ("california", "arizona"),
            ("california", "nevada"),
            ("florida", "texas"),
            ("florida", "new-york"),
            ("florida", "georgia"),
            ("new-york", "texas"),
            ("texas", "arizona"),
            ("texas", "georgia"),
            ("washington", "oregon"),
            ("illinois", "ohio"),
            ("georgia", "north-carolina"),
            ("maryland", "virginia"),
            ("new-jersey", "new-york"),
            ("connecticut", "massachusetts"),
            ("colorado", "utah"),
            ("north-carolina", "tennessee"),
            ("alaska", "hawaii"),
        };

        public static readonly IReadOnlyList<(string Slug1, string Slug2)> TierOneComparisonPairs = new List<(string, string)>
        {
            ("california", "texas"),
            ("california", "florida"),
            ("california", "new-york"),
            ("florida", "texas"),
            ("florida", "new-york"),
            ("new-york", "texas"),
            ("washington", "oregon"),
            ("maryland", "virginia"),
        };

        private readonly IStateService _stateService;
        private readonly IComparisonStatsService _statsService;
        private readonly IRankingsContentService _rankingsContentService;

        public ComparisonService(
            IStateService stateService,
            IComparisonStatsService statsService,
            IRankingsContentService rankingsContentService)
        {
            _stateService = stateService;
            _statsService = statsService;
            _rankingsContentService = rankingsContentService;
        }

        public async Task<CompareHubViewModel> GetHubViewModelAsync()
        {
            var states = await _stateService.GetAllStatesAsync();
            var stateLookup = states.ToDictionary(s => s.Slug, s => s, StringComparer.OrdinalIgnoreCase);
            var featured = TopComparisonPairs
                .Select(pair => TryMapFeaturedPair(pair, stateLookup))
                .Where(pair => pair.HasValue)
                .Select(pair => pair!.Value)
                .Take(9)
                .ToList();

            return new CompareHubViewModel
            {
                States = states,
                FeaturedPairs = featured
            };
        }

        public async Task<StatePairComparisonViewModel?> GetPairComparisonAsync(string slugA, string slugB)
        {
            var (stateA, stateB) = await LoadStatePairAsync(slugA, slugB);
            if (stateA == null || stateB == null) return null;

            var (statsA, statsB) = await LoadStatsPairAsync(slugA, slugB);
            await EnrichStatsWithHighestPointsAsync(stateA, statsA, stateB, statsB);

            var metrics = ComparisonMetricsConfig.All
                .OrderBy(m => m.GroupOrder)
                .ThenBy(m => m.SortOrder)
                .ToList();

            var results = metrics
                .Select(metric => BuildResult(metric, stateA, statsA, stateB, statsB))
                .ToList();

            var related = await BuildRelatedPairsAsync(stateA, stateB);

            return new StatePairComparisonViewModel
            {
                StateA = stateA,
                StateB = stateB,
                StatsA = statsA,
                StatsB = statsB,
                MetricResults = results,
                AllMetrics = metrics,
                RelatedPairs = related
            };
        }

        public async Task<MetricComparisonViewModel?> GetMetricComparisonAsync(string slugA, string slugB, string metricSlug)
        {
            var metric = ComparisonMetricsConfig.GetBySlug(metricSlug);
            if (metric == null) return null;

            var (stateA, stateB) = await LoadStatePairAsync(slugA, slugB);
            if (stateA == null || stateB == null) return null;

            var (statsA, statsB) = await LoadStatsPairAsync(slugA, slugB);
            await EnrichStatsWithHighestPointsAsync(stateA, statsA, stateB, statsB);

            var metrics = ComparisonMetricsConfig.All
                .OrderBy(m => m.GroupOrder)
                .ThenBy(m => m.SortOrder)
                .ToList();

            return new MetricComparisonViewModel
            {
                StateA = stateA,
                StateB = stateB,
                StatsA = statsA,
                StatsB = statsB,
                Metric = metric,
                Result = BuildResult(metric, stateA, statsA, stateB, statsB),
                OtherMetrics = metrics.Where(m => m.Slug != metricSlug).ToList()
            };
        }

        // ── private helpers ──────────────────────────────────────────────────

        private async Task<(State?, State?)> LoadStatePairAsync(string slugA, string slugB)
        {
            var taskA = _stateService.GetStateBySlugAsync(slugA);
            var taskB = _stateService.GetStateBySlugAsync(slugB);
            await Task.WhenAll(taskA, taskB);
            return (taskA.Result, taskB.Result);
        }

        private async Task<(StateStats?, StateStats?)> LoadStatsPairAsync(string slugA, string slugB)
        {
            var taskA = _statsService.GetStatsAsync(slugA);
            var taskB = _statsService.GetStatsAsync(slugB);
            await Task.WhenAll(taskA, taskB);
            return (taskA.Result, taskB.Result);
        }

        private static MetricComparisonResult BuildResult(
            ComparisonMetricDefinition metric,
            State a, StateStats? sa,
            State b, StateStats? sb)
        {
            var displayA = metric.GetDisplayValue(a, sa);
            var displayB = metric.GetDisplayValue(b, sb);
            var numA = metric.GetNumericValue?.Invoke(a, sa);
            var numB = metric.GetNumericValue?.Invoke(b, sb);

            string? winnerSlug = null;
            string? diffText = null;

            if (metric.Type != MetricType.Text && numA.HasValue && numB.HasValue)
            {
                if (numA != numB)
                {
                    bool aWins = metric.HigherIsBetter ? numA > numB : numA < numB;
                    winnerSlug = aWins ? a.Slug : b.Slug;
                }

                if (metric.Type == MetricType.Numeric && numA >= 0 && numB >= 0)
                {
                    var diff = Math.Abs(numA.Value - numB.Value);
                    diffText = metric.Unit switch
                    {
                        "people"   => $"+{diff:N0} people",
                        "sq mi"    => $"{diff:N0} sq mi",
                        "USD"      => $"${diff:N0}",
                        "per sq mi"=> $"{diff:N1} per sq mi",
                        "per 100k" => $"{diff:F1} per 100k",
                        "score"    => $"{diff:F2} points",
                        "index"    => $"{diff:F1} points",
                        "%"        => $"{diff:F2} percentage points",
                        "minutes"  => $"{diff:F1} minutes",
                        "ratio"    => $"{diff:F2}x",
                        "temp-f"   => $"{diff:F1}\u00B0F",
                        "days"     => $"{diff:N0} days",
                        "inches"   => $"{diff:F1} inches",
                        _          => null
                    };
                }
            }

            var summary = metric.GenerateSummary?.Invoke(a, sa, b, sb);

            return new MetricComparisonResult
            {
                Metric = metric,
                DisplayValueA = displayA,
                DisplayValueB = displayB,
                NumericA = numA,
                NumericB = numB,
                WinnerSlug = winnerSlug,
                SummaryText = summary,
                DifferenceText = diffText
            };
        }

        private async Task<List<(State, State)>> BuildRelatedPairsAsync(State a, State b)
        {
            var all = await _stateService.GetAllStatesAsync();
            var pairs = new List<(State, State)>();

            // Same region as A, paired with B
            var sameRegionAsA = all
                .Where(s => s.Region == a.Region && s.Slug != a.Slug && s.Slug != b.Slug)
                .Take(2);
            foreach (var s in sameRegionAsA)
                pairs.Add(OrderPair(s, b));

            // Same region as B, paired with A
            if (b.Region != a.Region)
            {
                var sameRegionAsB = all
                    .Where(s => s.Region == b.Region && s.Slug != a.Slug && s.Slug != b.Slug)
                    .Take(2);
                foreach (var s in sameRegionAsB)
                    pairs.Add(OrderPair(a, s));
            }

            return pairs.Take(4).ToList();
        }

        /// <summary>Returns (A, B) sorted alphabetically by slug — canonical order.</summary>
        public static (State, State) OrderPair(State x, State y) =>
            string.Compare(x.Slug, y.Slug, StringComparison.Ordinal) <= 0 ? (x, y) : (y, x);

        /// <summary>Returns the canonical pair slug (alphabetically sorted).</summary>
        public static string CanonicalPairSlug(string slugA, string slugB) =>
            string.Compare(slugA, slugB, StringComparison.Ordinal) <= 0
                ? $"{slugA}-vs-{slugB}"
                : $"{slugB}-vs-{slugA}";

        private static (string Slug1, string Name1, string Slug2, string Name2)? TryMapFeaturedPair(
            (string Slug1, string Slug2) pair,
            IReadOnlyDictionary<string, State> stateLookup)
        {
            if (!stateLookup.TryGetValue(pair.Slug1, out var state1) ||
                !stateLookup.TryGetValue(pair.Slug2, out var state2))
            {
                return null;
            }

            return (state1.Slug, state1.Name, state2.Slug, state2.Name);
        }

        private async Task EnrichStatsWithHighestPointsAsync(State stateA, StateStats? statsA, State stateB, StateStats? statsB)
        {
            var lookup = await LoadHighestPointLookupAsync();
            ApplyHighestPoint(stateA, statsA, lookup);
            ApplyHighestPoint(stateB, statsB, lookup);
        }

        private async Task<Dictionary<string, HighestPointInfo>> LoadHighestPointLookupAsync()
        {
            var content = await _rankingsContentService.GetContentAsync("geography", "highest-point-by-state");
            var table = content?.Table;
            if (table == null)
            {
                return new Dictionary<string, HighestPointInfo>(StringComparer.OrdinalIgnoreCase);
            }

            return table.Rows
                .Where(row => row.Has("state_slug") && row.Has("highest_point"))
                .GroupBy(row => row.GetString("state_slug"), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        var row = group.First();
                        return new HighestPointInfo
                        {
                            Name = row.GetString("highest_point"),
                            ElevationFt = ParseDouble(row.Data.TryGetValue("elevation_ft", out var value) ? value : null)
                        };
                    },
                    StringComparer.OrdinalIgnoreCase);
        }

        private static void ApplyHighestPoint(State state, StateStats? stats, IReadOnlyDictionary<string, HighestPointInfo> lookup)
        {
            if (stats == null || !lookup.TryGetValue(state.Slug, out var info))
            {
                return;
            }

            stats.HighestPointName = info.Name;
            stats.HighestPointElevationFt = info.ElevationFt;
        }

        private static double? ParseDouble(object? value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is double d)
            {
                return d;
            }

            if (value is int i)
            {
                return i;
            }

            if (double.TryParse(value.ToString(), out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private sealed class HighestPointInfo
        {
            public string Name { get; set; } = string.Empty;
            public double? ElevationFt { get; set; }
        }
    }
}
