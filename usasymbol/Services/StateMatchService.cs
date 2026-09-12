using Microsoft.AspNetCore.Hosting;
using usasymbol.Services.Interface;
using USASymbol.Models;
using USASymbol.Models.ViewModels;
using USASymbol.Services.Interface;

namespace USASymbol.Services
{
    public interface IStateMatchService
    {
        Task<StateMatchPageViewModel> BuildAsync();

        /// <summary>
        /// Scores and ranks every place for a given set of metric weights — the same
        /// normalize → weight → rank pipeline the client runs in state-match.js, run
        /// server-side so preset landing pages can render a real top-10 in the initial HTML.
        /// Pass an already-built <paramref name="page"/> to avoid rebuilding it twice per request.
        /// </summary>
        Task<IReadOnlyList<StateMatchRankedPlace>> RankAsync(
            IReadOnlyDictionary<string, int> weights,
            StateMatchPageViewModel? page = null);

        /// <summary>
        /// Raw contents of Content/state-match/occupation-median-salary.json — sourced median/mean
        /// salary by occupation and state, embedded as-is into the client payload so the "How much
        /// would you keep?" calculator can use a real per-state income instead of one flat number.
        /// </summary>
        string GetOccupationSalariesJson();
    }

    public sealed class StateMatchService : IStateMatchService
    {
        private sealed record MetricBinding(
            string Key,
            string ComparisonSlug,
            string Name,
            string Hint,
            string Description,
            string Icon,
            int DefaultWeight);

        private static readonly IReadOnlyList<MetricBinding> Bindings = new[]
        {
            new MetricBinding("cost", "cost-of-living", "Affordability", "Lower everyday costs", "Cost-of-living index where 100 is the national average.", "fa-solid fa-cart-shopping", 15),
            new MetricBinding("income", "median-income", "Income", "Higher household income", "Median household income before taxes.", "fa-solid fa-sack-dollar", 15),
            new MetricBinding("jobs", "unemployment-rate", "Jobs", "Lower unemployment", "State unemployment rate; lower values score better.", "fa-solid fa-briefcase", 15),
            new MetricBinding("housing", "home-value", "Housing", "Lower home prices", "Median residential home value.", "fa-solid fa-house", 15),
            new MetricBinding("safety", "violent-crime", "Safety", "Lower violent crime", "Violent crime incidents per 100,000 residents.", "fa-solid fa-shield-halved", 15),
            new MetricBinding("health", "best-healthcare", "Healthcare", "Better quality and access", "Composite healthcare score covering outcomes, cost, and access.", "fa-solid fa-briefcase-medical", 15),
            new MetricBinding("education", "public-school-rank", "Education", "Stronger public schools", "Public school system ranking; a lower rank is better.", "fa-solid fa-graduation-cap", 10),
            new MetricBinding("warmth", "average-temperature", "Warm climate", "Higher annual temperature", "Average annual statewide temperature.", "fa-solid fa-sun", 10),
            new MetricBinding("incometax", "income-tax", "Income tax", "Lower state income tax", "Top marginal state income tax rate. 0% = no state income tax.", "fa-solid fa-file-invoice-dollar", 5),
            new MetricBinding("propertytax", "property-tax", "Property tax", "Lower property tax rate", "Effective real-estate property tax rate as a percent of home value.", "fa-solid fa-house-circle-check", 5),
            new MetricBinding("salestax", "sales-tax", "Sales tax", "Lower state sales tax", "State-level sales tax rate. 0% = no state sales tax (local taxes may apply).", "fa-solid fa-receipt", 5)
        };

        private readonly IStateService _stateService;
        private readonly IComparisonStatsService _statsService;
        private readonly INormalizer _normalizer;
        private readonly IWebHostEnvironment _env;

        private static string? _cachedOccupationSalariesJson;
        private static readonly object OccupationSalariesLock = new();

        public StateMatchService(
            IStateService stateService,
            IComparisonStatsService statsService,
            INormalizer normalizer,
            IWebHostEnvironment env)
        {
            _stateService = stateService;
            _statsService = statsService;
            _normalizer = normalizer;
            _env = env;
        }

        public string GetOccupationSalariesJson()
        {
            if (_cachedOccupationSalariesJson != null)
                return _cachedOccupationSalariesJson;

            lock (OccupationSalariesLock)
            {
                if (_cachedOccupationSalariesJson != null)
                    return _cachedOccupationSalariesJson;

                var path = Path.Combine(_env.ContentRootPath, "Content", "state-match", "occupation-median-salary.json");
                _cachedOccupationSalariesJson = File.Exists(path) ? File.ReadAllText(path) : "{}";
            }

            return _cachedOccupationSalariesJson;
        }

        public async Task<StateMatchPageViewModel> BuildAsync()
        {
            var states = await _stateService.GetAllStatesAsync();
            var statsBySlug = await _statsService.GetAllStatsAsync();
            var places = new List<StateMatchPlaceViewModel>(states.Count);

            foreach (var state in states)
            {
                if (!StateFipsCatalog.TryGetFips(state.Abbreviation, out var fips))
                    continue;

                statsBySlug.TryGetValue(state.Slug, out var stats);
                var metrics = new List<MetricValue>(Bindings.Count);

                foreach (var binding in Bindings)
                {
                    var definition = ComparisonMetricsConfig.GetBySlug(binding.ComparisonSlug);
                    var raw = definition?.GetNumericValue?.Invoke(state, stats);
                    if (definition == null || !raw.HasValue)
                        continue;

                    metrics.Add(new MetricValue
                    {
                        Key = binding.Key,
                        Name = binding.Name,
                        Raw = raw.Value,
                        DisplayValue = definition.GetDisplayValue(state, stats) ?? raw.Value.ToString("N1"),
                        Direction = definition.HigherIsBetter ? 1 : -1,
                        Unit = definition.Unit,
                        SourceId = definition.Slug,
                        SourceName = definition.SourceName
                    });
                }

                places.Add(new StateMatchPlaceViewModel
                {
                    Kind = PlaceKind.State,
                    Fips = fips,
                    ParentFips = null,
                    Name = state.Name,
                    Slug = state.Slug,
                    Metrics = metrics,
                    Published = true,
                    HasCounties = !string.Equals(state.Abbreviation, "DC", StringComparison.OrdinalIgnoreCase),
                    Abbreviation = state.Abbreviation.ToUpperInvariant(),
                    Capital = state.Capital,
                    Region = state.Region ?? string.Empty,
                    Population = stats?.PopulationEstimate2025 ?? state.Population,
                    FlagImageUrl = string.IsNullOrWhiteSpace(state.FlagImageUrl)
                        ? $"/images/states/flags/medium/{state.Abbreviation.ToLowerInvariant()}.webp"
                        : state.FlagImageUrl
                });
            }

            var options = Bindings
                .Select(binding => new StateMatchMetricOption
                {
                    Key = binding.Key,
                    Name = binding.Name,
                    Hint = binding.Hint,
                    Description = binding.Description,
                    Icon = binding.Icon,
                    DefaultWeight = binding.DefaultWeight,
                    ComparisonSlug = binding.ComparisonSlug
                })
                .ToList();

            return new StateMatchPageViewModel
            {
                Places = places.OrderBy(place => place.Name).ToList(),
                MetricOptions = options
            };
        }

        public async Task<IReadOnlyList<StateMatchRankedPlace>> RankAsync(
            IReadOnlyDictionary<string, int> weights,
            StateMatchPageViewModel? page = null)
        {
            page ??= await BuildAsync();

            var ranges = new Dictionary<string, (double Min, double Max)>();
            foreach (var binding in Bindings)
            {
                var values = page.Places
                    .SelectMany(place => place.Metrics)
                    .Where(metric => metric.Key == binding.Key)
                    .Select(metric => metric.Raw)
                    .ToList();
                ranges[binding.Key] = values.Count > 0 ? (values.Min(), values.Max()) : (0, 0);
            }

            var hasAnyWeight = weights.Values.Any(weight => weight > 0);

            var ranked = page.Places.Select(place =>
            {
                double weightedTotal = 0;
                double usedWeight = 0;
                MetricValue? strongest = null;
                var strongestValue = double.MinValue;
                MetricValue? weakest = null;
                var weakestValue = double.MaxValue;

                foreach (var metric in place.Metrics)
                {
                    if (!weights.TryGetValue(metric.Key, out var weight) || weight <= 0)
                        continue;
                    if (!ranges.TryGetValue(metric.Key, out var range) || range.Max <= range.Min)
                        continue;

                    var normalized = _normalizer.Normalize(
                        metric.Key, metric.Raw, range.Min, range.Max, metric.Direction, NormalizationFrame.Nation50States);

                    weightedTotal += normalized * weight;
                    usedWeight += weight;

                    if (normalized > strongestValue)
                    {
                        strongestValue = normalized;
                        strongest = metric;
                    }
                    if (normalized < weakestValue)
                    {
                        weakestValue = normalized;
                        weakest = metric;
                    }
                }

                var score = hasAnyWeight && usedWeight > 0
                    ? (int)Math.Round(weightedTotal / usedWeight * 100)
                    : 0;

                return new StateMatchRankedPlace
                {
                    Place = place,
                    Score = score,
                    StrongestMetricName = strongest?.Name,
                    WeakestMetricName = weakest?.Name
                };
            })
            .OrderByDescending(row => row.Score)
            .ThenBy(row => row.Place.Name, StringComparer.Ordinal)
            .ToList();

            for (var i = 0; i < ranked.Count; i++)
            {
                ranked[i].Rank = i + 1;
            }

            return ranked;
        }
    }
}
