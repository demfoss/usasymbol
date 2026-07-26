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

            // Migration corridors (high out-migration states → popular destinations)
            ("california", "idaho"),
            ("california", "colorado"),
            ("california", "washington"),
            ("new-york", "north-carolina"),
            ("new-york", "pennsylvania"),
            ("new-york", "connecticut"),
            ("illinois", "texas"),
            ("illinois", "florida"),
            ("illinois", "indiana"),
            ("new-jersey", "pennsylvania"),
            ("new-jersey", "florida"),
            ("massachusetts", "new-hampshire"),
            ("massachusetts", "florida"),
            ("michigan", "ohio"),
            ("michigan", "illinois"),
            ("pennsylvania", "ohio"),
            ("pennsylvania", "florida"),
            ("virginia", "north-carolina"),
            ("virginia", "tennessee"),
            ("tennessee", "kentucky"),
            ("tennessee", "georgia"),
            ("texas", "oklahoma"),
            ("texas", "colorado"),
            ("texas", "tennessee"),
            ("florida", "tennessee"),
            ("florida", "south-carolina"),
            ("georgia", "south-carolina"),
            ("georgia", "alabama"),
            ("arizona", "nevada"),
            ("arizona", "colorado"),
            ("nevada", "utah"),
            ("washington", "idaho"),
            ("oregon", "idaho"),
            ("colorado", "wyoming"),
            ("utah", "idaho"),
            ("missouri", "kansas"),
            ("minnesota", "wisconsin"),
            ("ohio", "indiana"),
            ("north-carolina", "south-carolina"),
            ("maryland", "delaware"),
            ("new-hampshire", "vermont"),
            ("rhode-island", "massachusetts"),
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

            // High single-metric search intent (migration + no-income-tax comparisons)
            ("california", "idaho"),
            ("california", "nevada"),
            ("new-jersey", "pennsylvania"),
            ("massachusetts", "new-hampshire"),
            ("illinois", "texas"),
            ("illinois", "florida"),
            ("texas", "oklahoma"),
            ("texas", "tennessee"),
            ("florida", "tennessee"),
            ("arizona", "nevada"),
            ("new-york", "north-carolina"),
            ("virginia", "north-carolina"),
            ("tennessee", "kentucky"),
            ("michigan", "ohio"),
            ("missouri", "kansas"),
            ("colorado", "utah"),
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

            var allStateRanks = new List<StateRankRow>();
            if (metric.Type != MetricType.Text && metric.GetNumericValue != null)
            {
                var allStates = await _stateService.GetAllStatesAsync();
                var allStatsMap = await _statsService.GetAllStatsAsync();

                allStateRanks = allStates
                    .Select(s =>
                    {
                        allStatsMap.TryGetValue(s.Slug, out var st);
                        var val = metric.GetNumericValue(s, st);
                        if (!val.HasValue) return null;
                        return new { State = s, Value = val.Value, Display = metric.GetDisplayValue(s, st) ?? val.Value.ToString() };
                    })
                    .Where(x => x != null)
                    .Select(x => x!)
                    .OrderByDescending(x => metric.HigherIsBetter ? x.Value : -x.Value)
                    .Select((x, i) => new StateRankRow
                    {
                        StateName = x.State.Name,
                        StateSlug = x.State.Slug,
                        Abbreviation = x.State.Abbreviation,
                        Value = x.Value,
                        FormattedValue = x.Display,
                        Rank = i + 1,
                        FlagImageUrl = x.State.FlagImageUrl
                    })
                    .ToList();
            }

            return new MetricComparisonViewModel
            {
                StateA = stateA,
                StateB = stateB,
                StatsA = statsA,
                StatsB = statsB,
                Metric = metric,
                Result = BuildResult(metric, stateA, statsA, stateB, statsB),
                OtherMetrics = metrics.Where(m => m.Slug != metric.Slug).ToList(),
                AllStateRanks = allStateRanks,
                HigherIsBetter = metric.HigherIsBetter
            };
        }

        public async Task<(TableSectionViewModel? Table, List<CategoryExtreme> Highlights)> GetCategoryStatesTableAsync(string categorySlug)
        {
            var metrics = ComparisonMetricsConfig.All
                .Where(m => string.Equals(m.GroupSlug, categorySlug, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.SortOrder)
                .ToList();
            if (metrics.Count == 0) return (null, new List<CategoryExtreme>());

            var allStates = await _stateService.GetAllStatesAsync();
            var allStats = await _statsService.GetAllStatsAsync();

            var table = new PageTable
            {
                Searchable = true,
                Sortable = true,
                DefaultColumn = metrics.FirstOrDefault(m => m.Type == MetricType.Numeric)?.Slug ?? "state"
            };
            table.Columns.Add(new TableColumn { Key = "rank", Label = "Rank", Type = "rank", Sortable = true });
            table.Columns.Add(new TableColumn { Key = "state", Label = "State", Type = "state-link", Sortable = true });
            foreach (var m in metrics)
            {
                table.Columns.Add(new TableColumn
                {
                    Key = m.Slug,
                    Label = m.Name,
                    Type = m.Type == MetricType.Numeric ? "number" : "text",
                    Sortable = m.Type == MetricType.Numeric
                });
            }

            var rankMetric = metrics.FirstOrDefault(m => m.Type == MetricType.Numeric);
            var ordered = allStates
                .Select(s =>
                {
                    allStats.TryGetValue(s.Slug, out var st);
                    return (State: s, Stats: st);
                })
                .OrderByDescending(x => rankMetric?.GetNumericValue == null
                    ? 0
                    : (rankMetric.HigherIsBetter ? rankMetric.GetNumericValue(x.State, x.Stats) : -rankMetric.GetNumericValue(x.State, x.Stats)) ?? double.MinValue)
                .ThenBy(x => x.State.Name)
                .ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                var (state, stats) = ordered[i];
                var row = new TableRow();
                row.Data["rank"] = i + 1;
                row.Data["state"] = state.Name;
                row.Data["state_slug"] = state.Slug;
                foreach (var m in metrics)
                {
                    row.Data[m.Slug] = m.Type == MetricType.Numeric
                        ? (object?)m.GetNumericValue?.Invoke(state, stats)
                        : m.GetDisplayValue(state, stats);
                }
                table.Rows.Add(row);
            }

            var highlights = new List<CategoryExtreme>();
            foreach (var m in metrics.Where(m => m.Type == MetricType.Numeric && m.GetNumericValue != null))
            {
                var withValues = ordered
                    .Select(x => (x.State, x.Stats, Value: m.GetNumericValue!(x.State, x.Stats)))
                    .Where(x => x.Value.HasValue)
                    .ToList();
                if (withValues.Count == 0) continue;

                var best = withValues.OrderByDescending(x => m.HigherIsBetter ? x.Value!.Value : -x.Value!.Value).First();
                var worst = withValues.OrderBy(x => m.HigherIsBetter ? x.Value!.Value : -x.Value!.Value).First();

                highlights.Add(new CategoryExtreme
                {
                    MetricName = m.Name,
                    Icon = m.Icon,
                    HigherIsBetter = m.HigherIsBetter,
                    BestStateName = best.State.Name,
                    BestStateSlug = best.State.Slug,
                    BestValue = m.GetDisplayValue(best.State, best.Stats),
                    WorstStateName = worst.State.Name,
                    WorstStateSlug = worst.State.Slug,
                    WorstValue = m.GetDisplayValue(worst.State, worst.Stats)
                });
            }

            var tableSection = new TableSectionViewModel
            {
                Tables = new List<PageTable> { table },
                Searchable = true,
                TotalRowsCount = table.Rows.Count
            };

            return (tableSection, highlights);
        }

        // ── private helpers ──────────────────────────────────────────────────

        private async Task<(State?, State?)> LoadStatePairAsync(string slugA, string slugB)
        {
            // StateService uses the request-scoped AppDbContext. EF Core does not
            // support concurrent queries on the same context, which caused compare
            // pages to fail intermittently against Azure SQL. Keep these queries
            // sequential while they share that scoped dependency.
            var stateA = await _stateService.GetStateBySlugAsync(slugA);
            var stateB = await _stateService.GetStateBySlugAsync(slugB);
            return (stateA, stateB);
        }

        private async Task<(StateStats?, StateStats?)> LoadStatsPairAsync(string slugA, string slugB)
        {
            var allStats = await _statsService.GetAllStatsAsync();
            allStats.TryGetValue(slugA, out var statsA);
            allStats.TryGetValue(slugB, out var statsB);
            return (statsA, statsB);
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
                        "cents-gal" => $"{diff:F2} c/gal",
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
