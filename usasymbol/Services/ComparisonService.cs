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
            ("arizona", "california"),
            ("california", "nevada"),
            ("florida", "texas"),
            ("florida", "new-york"),
            ("florida", "georgia"),
            ("new-york", "texas"),
            ("arizona", "texas"),
            ("georgia", "texas"),
            ("oregon", "washington"),
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
            ("connecticut", "new-york"),
            ("illinois", "texas"),
            ("florida", "illinois"),
            ("illinois", "indiana"),
            ("new-jersey", "pennsylvania"),
            ("florida", "new-jersey"),
            ("massachusetts", "new-hampshire"),
            ("florida", "massachusetts"),
            ("michigan", "ohio"),
            ("illinois", "michigan"),
            ("ohio", "pennsylvania"),
            ("florida", "pennsylvania"),
            ("north-carolina", "virginia"),
            ("tennessee", "virginia"),
            ("kentucky", "tennessee"),
            ("georgia", "tennessee"),
            ("oklahoma", "texas"),
            ("colorado", "texas"),
            ("tennessee", "texas"),
            ("florida", "tennessee"),
            ("florida", "south-carolina"),
            ("georgia", "south-carolina"),
            ("alabama", "georgia"),
            ("arizona", "nevada"),
            ("arizona", "colorado"),
            ("nevada", "utah"),
            ("idaho", "washington"),
            ("idaho", "oregon"),
            ("colorado", "wyoming"),
            ("idaho", "utah"),
            ("kansas", "missouri"),
            ("minnesota", "wisconsin"),
            ("indiana", "ohio"),
            ("north-carolina", "south-carolina"),
            ("delaware", "maryland"),
            ("new-hampshire", "vermont"),
            ("massachusetts", "rhode-island"),
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

        /// <summary>Second row on the compare hub: neighbors and common moving routes.</summary>
        public static readonly IReadOnlyList<(string Slug1, string Slug2)> NeighborAndMovePairs = new List<(string, string)>
        {
            ("oregon", "washington"),
            ("maryland", "virginia"),
            ("new-jersey", "new-york"),
            ("michigan", "ohio"),
            ("arizona", "nevada"),
            ("california", "idaho"),
            ("new-york", "north-carolina"),
            ("florida", "illinois"),
        };

        /// <summary>Bordering states by postal code, used for the hub's "compare with a neighbor" chips.</summary>
        public static readonly IReadOnlyDictionary<string, string[]> StateNeighborsByAbbreviation = new Dictionary<string, string[]>
        {
            ["AL"] = new[] { "FL", "GA", "MS", "TN" },
            ["AZ"] = new[] { "CA", "NV", "UT", "CO", "NM" },
            ["AR"] = new[] { "LA", "MS", "TN", "MO", "OK", "TX" },
            ["CA"] = new[] { "OR", "NV", "AZ" },
            ["CO"] = new[] { "WY", "NE", "KS", "OK", "NM", "AZ", "UT" },
            ["CT"] = new[] { "NY", "MA", "RI" },
            ["DE"] = new[] { "MD", "PA", "NJ" },
            ["FL"] = new[] { "AL", "GA" },
            ["GA"] = new[] { "FL", "AL", "TN", "NC", "SC" },
            ["ID"] = new[] { "WA", "OR", "NV", "UT", "WY", "MT" },
            ["IL"] = new[] { "WI", "IA", "MO", "KY", "IN" },
            ["IN"] = new[] { "MI", "OH", "KY", "IL" },
            ["IA"] = new[] { "MN", "WI", "IL", "MO", "NE", "SD" },
            ["KS"] = new[] { "NE", "MO", "OK", "CO" },
            ["KY"] = new[] { "IL", "IN", "OH", "WV", "VA", "TN", "MO" },
            ["LA"] = new[] { "TX", "AR", "MS" },
            ["ME"] = new[] { "NH" },
            ["MD"] = new[] { "VA", "WV", "PA", "DE" },
            ["MA"] = new[] { "RI", "CT", "NY", "NH", "VT" },
            ["MI"] = new[] { "OH", "IN", "WI" },
            ["MN"] = new[] { "ND", "SD", "IA", "WI" },
            ["MS"] = new[] { "LA", "AR", "TN", "AL" },
            ["MO"] = new[] { "IA", "IL", "KY", "TN", "AR", "OK", "KS", "NE" },
            ["MT"] = new[] { "ID", "WY", "SD", "ND" },
            ["NE"] = new[] { "SD", "IA", "MO", "KS", "CO", "WY" },
            ["NV"] = new[] { "OR", "ID", "UT", "AZ", "CA" },
            ["NH"] = new[] { "ME", "MA", "VT" },
            ["NJ"] = new[] { "NY", "PA", "DE" },
            ["NM"] = new[] { "AZ", "UT", "CO", "OK", "TX" },
            ["NY"] = new[] { "NJ", "PA", "CT", "MA", "VT" },
            ["NC"] = new[] { "VA", "TN", "GA", "SC" },
            ["ND"] = new[] { "MN", "SD", "MT" },
            ["OH"] = new[] { "PA", "WV", "KY", "IN", "MI" },
            ["OK"] = new[] { "KS", "MO", "AR", "TX", "NM", "CO" },
            ["OR"] = new[] { "WA", "ID", "NV", "CA" },
            ["PA"] = new[] { "NY", "NJ", "DE", "MD", "WV", "OH" },
            ["RI"] = new[] { "CT", "MA" },
            ["SC"] = new[] { "NC", "GA" },
            ["SD"] = new[] { "ND", "MN", "IA", "NE", "WY", "MT" },
            ["TN"] = new[] { "KY", "VA", "NC", "GA", "AL", "MS", "AR", "MO" },
            ["TX"] = new[] { "NM", "OK", "AR", "LA" },
            ["UT"] = new[] { "ID", "WY", "CO", "NM", "AZ", "NV" },
            ["VT"] = new[] { "NH", "MA", "NY" },
            ["VA"] = new[] { "MD", "WV", "KY", "TN", "NC" },
            ["WA"] = new[] { "ID", "OR" },
            ["WV"] = new[] { "OH", "PA", "MD", "VA", "KY" },
            ["WI"] = new[] { "MI", "MN", "IA", "IL" },
            ["WY"] = new[] { "MT", "SD", "NE", "CO", "UT", "ID" },
        };

        private static readonly HashSet<string> IndexablePairKeys = new(
            TopComparisonPairs.Select(p => CanonicalPairSlug(p.Slug1, p.Slug2)), StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> TierOnePairKeys = new(
            TierOneComparisonPairs.Select(p => CanonicalPairSlug(p.Slug1, p.Slug2)), StringComparer.OrdinalIgnoreCase);

        /// <summary>Pair overview pages are indexable only for curated pairs with real demand (same set as the sitemap).</summary>
        public static bool IsIndexablePair(string slugA, string slugB) =>
            IndexablePairKeys.Contains(CanonicalPairSlug(slugA, slugB));

        /// <summary>
        /// Metrics listed in the compare sitemap (sitemap only; no page is noindex — GSC 2026-11 showed
        /// metric pages like public-school-rank / best-state-to-live-in / retirement-score earn the clicks).
        /// </summary>
        public static readonly IReadOnlySet<string> IndexableMetricSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "population", "density", "domestic-migration", "population-growth",
            "median-income", "unemployment-rate", "job-growth",
            "single-person-living-wage", "minimum-wage",
            "cost-of-living", "regional-price-parity", "purchasing-power",
            "gas-price", "electricity-rates",
            "average-temperature", "summer-temperature", "winter-temperature",
            "average-wind-speed", "lightning-density",
            "home-value", "median-rent", "owner-costs-with-mortgage",
            "homeownership-rate", "home-insurance",
            "income-tax", "sales-tax", "property-tax", "tax-burden",
            "grocery-tax", "death-tax", "vehicle-property-tax",
            "political-lean", "abortion-laws", "marijuana-legalization",
            "right-to-work", "gun-laws-status",
            "land-area", "life-expectancy", "violent-crime",
            "infant-mortality", "overdose-death-rate",
            "water-quality", "power-outages", "road-quality",
            "casinos", "ufo-sightings",
            "public-school-rank", "teacher-salary", "retirement-score",
            "best-state-to-live-in", "best-healthcare", "livability-score",
            "car-insurance", "unemployment-benefit",
            "annual-precipitation", "sunny-days"
        };

        /// <summary>Metric pages listed in the sitemap: tier-one pair AND a listed metric. Not used for noindex.</summary>
        public static bool IsIndexableMetricPage(string slugA, string slugB, string metricSlug) =>
            IsTierOnePair(slugA, slugB) && IndexableMetricSlugs.Contains(metricSlug);

        /// <summary>Metric and category deep-dive pages are indexable only for tier-one pairs (same set as the sitemap).</summary>
        public static bool IsTierOnePair(string slugA, string slugB) =>
            TierOnePairKeys.Contains(CanonicalPairSlug(slugA, slugB));

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
                .Take(8)
                .ToList();

            var neighborPairs = NeighborAndMovePairs
                .Select(pair => TryMapFeaturedPair(pair, stateLookup))
                .Where(pair => pair.HasValue)
                .Select(pair => pair!.Value)
                .ToList();

            return new CompareHubViewModel
            {
                States = states,
                FeaturedPairs = featured,
                NeighborPairs = neighborPairs
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
            var bySlug = all.ToDictionary(s => s.Slug, StringComparer.OrdinalIgnoreCase);
            var pairs = new List<(State, State)>();
            var seen = new HashSet<string> { CanonicalPairSlug(a.Slug, b.Slug) };

            void Add(State x, State y)
            {
                if (x.Slug == y.Slug) return;
                if (seen.Add(CanonicalPairSlug(x.Slug, y.Slug))) pairs.Add(OrderPair(x, y));
            }

            // 1. Curated pairs with real demand that share a state with this pair (list is demand-ordered).
            foreach (var (s1, s2) in TopComparisonPairs)
            {
                var involvesA = s1 == a.Slug || s2 == a.Slug;
                var involvesB = s1 == b.Slug || s2 == b.Slug;
                if (!involvesA && !involvesB) continue;
                if (bySlug.TryGetValue(s1, out var x) && bySlug.TryGetValue(s2, out var y)) Add(x, y);
                if (pairs.Count >= 6) return pairs;
            }

            // 2. Fill with neighbors of each state compared against the other one.
            foreach (var (self, other) in new[] { (a, b), (b, a) })
            {
                if (!StateNeighborsByAbbreviation.TryGetValue(other.Abbreviation, out var nbrs)) continue;
                foreach (var ab in nbrs)
                {
                    var n = all.FirstOrDefault(s => s.Abbreviation == ab);
                    if (n != null) Add(self, n);
                    if (pairs.Count >= 6) return pairs;
                }
            }

            return pairs;
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
