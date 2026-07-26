using usasymbol.Services.Interface;
using USASymbol.Models;
using USASymbol.Models.ViewModels;
using USASymbol.Services.Interface;

namespace USASymbol.Services
{
    public interface IStateMatchService
    {
        Task<StateMatchPageViewModel> BuildAsync();
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
            new MetricBinding("cost", "cost-of-living", "Affordability", "Lower everyday costs", "Cost-of-living index where 100 is the national average.", "fa-solid fa-cart-shopping", 60),
            new MetricBinding("income", "median-income", "Income", "Higher household income", "Median household income before taxes.", "fa-solid fa-sack-dollar", 55),
            new MetricBinding("jobs", "unemployment-rate", "Jobs", "Lower unemployment", "State unemployment rate; lower values score better.", "fa-solid fa-briefcase", 50),
            new MetricBinding("housing", "home-value", "Housing", "Lower home prices", "Median residential home value.", "fa-solid fa-house", 55),
            new MetricBinding("safety", "violent-crime", "Safety", "Lower violent crime", "Violent crime incidents per 100,000 residents.", "fa-solid fa-shield-halved", 60),
            new MetricBinding("health", "best-healthcare", "Healthcare", "Better quality and access", "Composite healthcare score covering outcomes, cost, and access.", "fa-solid fa-briefcase-medical", 50),
            new MetricBinding("education", "public-school-rank", "Education", "Stronger public schools", "Public school system ranking; a lower rank is better.", "fa-solid fa-graduation-cap", 45),
            new MetricBinding("warmth", "average-temperature", "Warm climate", "Higher annual temperature", "Average annual statewide temperature.", "fa-solid fa-sun", 35),
            new MetricBinding("taxes", "tax-burden", "Low taxes", "Lower total tax burden", "State and local tax burden as a share of personal income.", "fa-solid fa-scale-balanced", 45)
        };

        private readonly IStateService _stateService;
        private readonly IComparisonStatsService _statsService;

        public StateMatchService(IStateService stateService, IComparisonStatsService statsService)
        {
            _stateService = stateService;
            _statsService = statsService;
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
                    DefaultWeight = binding.DefaultWeight
                })
                .ToList();

            return new StateMatchPageViewModel
            {
                Places = places.OrderBy(place => place.Name).ToList(),
                MetricOptions = options
            };
        }
    }
}
