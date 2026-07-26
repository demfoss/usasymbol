using USASymbol.Models;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using USASymbol.Services.Interface;
using usasymbol.Services.Interface;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace USASymbol.Services;

public sealed class StateLivingService : IStateLivingService
{
    private const string DataReviewedOn = "July 23, 2026";

    private static readonly string[] KeyMetricSlugs =
    {
        "cost-of-living", "median-income", "median-rent",
        "violent-crime", "best-healthcare", "public-school-rank"
    };

    private static readonly (string Name, string Icon, string[] Slugs)[] Groups =
    {
        ("Income & jobs", "fa-solid fa-briefcase", new[]
        {
            "median-income", "single-person-living-wage", "unemployment-rate",
            "job-growth", "poverty-rate", "purchasing-power"
        }),
        ("Cost, housing & taxes", "fa-solid fa-house", new[]
        {
            "cost-of-living", "home-value", "median-rent", "homeownership-rate",
            "tax-burden", "income-tax", "property-tax", "gas-price"
        }),
        ("Daily life & climate", "fa-solid fa-sun", new[]
        {
            "livability-score", "commute-time", "average-temperature",
            "sunny-days", "annual-precipitation", "water-quality", "road-quality"
        }),
        ("Safety & health", "fa-solid fa-shield-heart", new[]
        {
            "violent-crime", "property-crime", "life-expectancy",
            "best-healthcare", "uninsured-rate", "overdose-death-rate"
        }),
        ("Education", "fa-solid fa-graduation-cap", new[]
        {
            "public-school-rank", "hs-graduation-rate", "college-educated",
            "student-teacher-ratio", "school-spending", "teacher-salary"
        })
    };

    private readonly IStateService _stateService;
    private readonly IComparisonStatsService _statsService;
    private readonly IParkService _parkService;
    private readonly ICountyService _countyService;
    private readonly IWebHostEnvironment _environment;
    private readonly IMemoryCache _cache;

    public StateLivingService(
        IStateService stateService,
        IComparisonStatsService statsService,
        IParkService parkService,
        ICountyService countyService,
        IWebHostEnvironment environment,
        IMemoryCache cache)
    {
        _stateService = stateService;
        _statsService = statsService;
        _parkService = parkService;
        _countyService = countyService;
        _environment = environment;
        _cache = cache;
    }

    public async Task<StateLivingViewModel?> GetAsync(string stateSlug)
    {
        var state = await _stateService.GetStateBySlugAsync(stateSlug);
        if (state is null)
            return null;

        var states = await _stateService.GetAllStatesAsync();
        var statsBySlug = await _statsService.GetAllStatsAsync();
        statsBySlug.TryGetValue(state.Slug, out var stateStats);

        var metricCache = new Dictionary<string, StateLivingMetricViewModel>(StringComparer.OrdinalIgnoreCase);
        foreach (var slug in Groups.SelectMany(group => group.Slugs).Concat(KeyMetricSlugs).Distinct())
        {
            var metric = BuildMetric(slug, state, stateStats, states, statsBySlug);
            if (metric is not null)
                metricCache[slug] = metric;
        }

        var keyMetrics = KeyMetricSlugs
            .Where(metricCache.ContainsKey)
            .Select(slug => metricCache[slug])
            .ToList();

        var groups = Groups
            .Select(group => new StateLivingMetricGroupViewModel
            {
                Name = group.Name,
                Icon = group.Icon,
                Metrics = group.Slugs.Where(metricCache.ContainsKey).Select(slug => metricCache[slug]).ToList()
            })
            .Where(group => group.Metrics.Count > 0)
            .ToList();

        var parks = (await _parkService.GetAllNationalParksAsync())
            .Where(park => IsParkInState(park, state.Abbreviation))
            .OrderByDescending(park => park.Rankings.OverallScore)
            .ThenBy(park => park.Name)
            .ToList();
        var photos = BuildPhotos(parks).ToList();
        if (photos.Count < 4)
        {
            var importedPhotos = await GetImportedPhotosAsync(state.Slug);
            var seenPhotos = new HashSet<string>(photos.Select(photo => photo.ImageUrl), StringComparer.OrdinalIgnoreCase);
            photos.AddRange(importedPhotos.Where(photo => seenPhotos.Add(photo.ImageUrl)).Take(4 - photos.Count));
        }

        var scored = metricCache.Values
            .Where(metric => metric.AvailableStateCount > 1)
            .Select(metric => new ScoredMetric(
                metric,
                metric.AvailableStateCount <= 1
                    ? 50d
                    : 100d * (metric.AvailableStateCount - metric.Rank) / (metric.AvailableStateCount - 1)))
            .ToList();
        var strengths = scored.OrderByDescending(item => item.Score).Take(3).ToList();
        var tradeoffs = scored.OrderBy(item => item.Score).Take(3).ToList();
        var overview = BuildOverview(state, metricCache, strengths, tradeoffs);
        var movingOverview = BuildMovingOverview(state, metricCache, strengths, tradeoffs);
        var costSummary = BuildCostOfLivingSummary(state, metricCache);
        var bestForSummary = BuildBestForSummary(state, metricCache, strengths);
        var verdict = BuildVerdict(state, scored);
        var faqs = BuildFaqs(state, verdict, overview, movingOverview, costSummary, metricCache);
        var sources = metricCache.Values
            .Where(metric => !string.IsNullOrWhiteSpace(metric.SourceName))
            .GroupBy(metric => $"{metric.SourceName}|{metric.SourceUrl}|{metric.DataYear}", StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var metric = group.First();
                return new StateLivingSourceViewModel
                {
                    Name = metric.SourceName,
                    Url = metric.SourceUrl,
                    DataPeriod = metric.DataYear,
                    ReviewedOn = metric.ReviewedOn
                };
            })
            .OrderBy(source => source.Name)
            .ToList();
        var countyHighlights = string.Equals(state.Abbreviation, "DC", StringComparison.OrdinalIgnoreCase)
            ? null
            : await _countyService.GetHighlightsAsync(state.Slug);

        return new StateLivingViewModel
        {
            State = state,
            FlagImageUrl = ResolveFlagUrl(state),
            Verdict = verdict,
            Overview = overview,
            MovingOverview = movingOverview,
            CostOfLivingSummary = costSummary,
            BestForSummary = bestForSummary,
            KeyMetrics = keyMetrics,
            MetricGroups = groups,
            Pros = strengths.Select(item => BuildPoint(item.Metric, positive: true)).ToList(),
            Cons = tradeoffs.Select(item => BuildPoint(item.Metric, positive: false)).ToList(),
            Faqs = faqs,
            Sources = sources,
            Parks = parks,
            Photos = photos,
            RelatedStates = states
                .Where(item => item.Id != state.Id && string.Equals(item.Region, state.Region, StringComparison.OrdinalIgnoreCase))
                .Take(4)
                .ToList(),
            DataReviewedOn = DataReviewedOn,
            StateCount = states.Count,
            CountyHighlights = countyHighlights
        };
    }

    public async Task<StateLivingHubViewModel> GetHubAsync()
    {
        var states = (await _stateService.GetAllStatesAsync())
            .Where(state => !string.Equals(state.Abbreviation, "DC", StringComparison.OrdinalIgnoreCase))
            .OrderBy(state => state.Name)
            .ToList();
        var statsBySlug = await _statsService.GetAllStatsAsync();
        var cards = new List<StateLivingCardViewModel>(states.Count);

        foreach (var state in states)
        {
            statsBySlug.TryGetValue(state.Slug, out var stateStats);
            var cost = BuildMetric("cost-of-living", state, stateStats, states, statsBySlug);
            var safety = BuildMetric("violent-crime", state, stateStats, states, statsBySlug);
            var climate = BuildMetric("average-temperature", state, stateStats, states, statsBySlug);
            var quality = BuildMetric("livability-score", state, stateStats, states, statsBySlug);

            cards.Add(new StateLivingCardViewModel
            {
                State = state,
                FlagImageUrl = ResolveFlagUrl(state),
                Summary = BuildHubSummary(state, cost, safety, climate, quality),
                CostOfLiving = cost,
                Safety = safety,
                Climate = climate,
                QualityOfLife = quality,
                ClimateLabel = BuildClimateLabel(climate?.RawValue),
                IsAffordable = cost is not null && cost.RawValue <= 100,
                IsSafer = safety is not null && safety.Rank <= Math.Ceiling(safety.AvailableStateCount / 3d),
                IsWarm = climate is not null && climate.RawValue >= 60,
                IsHighQualityOfLife = quality is not null && quality.Rank <= Math.Ceiling(quality.AvailableStateCount / 3d)
            });
        }

        return new StateLivingHubViewModel
        {
            States = cards,
            DataReviewedOn = DataReviewedOn
        };
    }

    private static StateLivingMetricViewModel? BuildMetric(
        string slug,
        State state,
        StateStats? stateStats,
        IReadOnlyList<State> states,
        IReadOnlyDictionary<string, StateStats> statsBySlug)
    {
        var definition = ComparisonMetricsConfig.All.FirstOrDefault(
            metric => string.Equals(metric.Slug, slug, StringComparison.OrdinalIgnoreCase));
        if (definition?.GetNumericValue is null)
            return null;

        var values = states
            .Select(item =>
            {
                statsBySlug.TryGetValue(item.Slug, out var stats);
                return (State: item, Value: definition.GetNumericValue(item, stats));
            })
            .Where(item => item.Value.HasValue)
            .Select(item => (item.State, Value: item.Value!.Value))
            .ToList();

        var current = definition.GetNumericValue(state, stateStats);
        if (!current.HasValue || values.Count == 0)
            return null;

        var min = values.Min(item => item.Value);
        var max = values.Max(item => item.Value);
        var better = values.Count(item => definition.HigherIsBetter
            ? item.Value > current.Value
            : item.Value < current.Value);
        var position = max <= min ? 50 : ((current.Value - min) / (max - min)) * 100;

        return new StateLivingMetricViewModel
        {
            Slug = definition.Slug,
            Name = definition.Name,
            Description = definition.Description,
            DisplayValue = definition.GetDisplayValue(state, stateStats) ?? current.Value.ToString("N1"),
            RawValue = current.Value,
            MinValue = min,
            MaxValue = max,
            RangePosition = Math.Clamp(position, 0, 100),
            Rank = better + 1,
            AvailableStateCount = values.Count,
            HigherIsBetter = definition.HigherIsBetter,
            SourceName = definition.SourceName,
            SourceUrl = definition.SourceUrl,
            DataYear = definition.DataYear,
            ReviewedOn = definition.UpdatedAt
        };
    }

    private static bool IsParkInState(ParkContent park, string abbreviation)
    {
        if (string.Equals(park.Location.StateCode, abbreviation, StringComparison.OrdinalIgnoreCase))
            return true;
        return park.Location.StateCodes.Any(code =>
            string.Equals(code, abbreviation, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<StateLivingPhotoViewModel> BuildPhotos(IReadOnlyList<ParkContent> parks)
    {
        var photos = new List<StateLivingPhotoViewModel>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var park in parks)
        {
            Add(park.Media.HeroImage, park.Media.HeroAlt, park.Media.HeroCredit, park);
            foreach (var highlight in park.Media.Highlights)
                Add(highlight.Image, highlight.Alt, highlight.Credit, park);
            foreach (var attraction in park.BestThingsToSeeItems)
                Add(attraction.Image, attraction.Alt, attraction.Credit, park);

            if (photos.Count >= 4)
                break;
        }

        return photos.Take(4).ToList();

        void Add(string? image, string? alt, string? credit, ParkContent park)
        {
            if (photos.Count >= 4 || string.IsNullOrWhiteSpace(image) || !seen.Add(image))
                return;
            photos.Add(new StateLivingPhotoViewModel
            {
                ImageUrl = image,
                Alt = string.IsNullOrWhiteSpace(alt) ? $"{park.Name} landscape" : alt,
                Credit = credit ?? string.Empty,
                LocationName = park.Name,
                LocationUrl = $"/national-parks/{park.Slug}",
                SourceName = "National park library"
            });
        }
    }

    private async Task<IReadOnlyList<StateLivingPhotoViewModel>> GetImportedPhotosAsync(string stateSlug)
    {
        const string cacheKey = "state-living:photo-manifest";
        if (!_cache.TryGetValue(cacheKey, out StateLivingPhotoManifest? manifest))
        {
            var path = Path.Combine(_environment.ContentRootPath, "Content", "state-living", "photos.json");
            if (!File.Exists(path))
                return Array.Empty<StateLivingPhotoViewModel>();

            try
            {
                var json = await File.ReadAllTextAsync(path);
                manifest = JsonSerializer.Deserialize<StateLivingPhotoManifest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                _cache.Set(cacheKey, manifest, TimeSpan.FromHours(6));
            }
            catch (JsonException)
            {
                return Array.Empty<StateLivingPhotoViewModel>();
            }
        }

        if (manifest?.States is null ||
            !manifest.States.TryGetValue(stateSlug, out var photos))
            return Array.Empty<StateLivingPhotoViewModel>();

        return photos;
    }

    private static string ResolveFlagUrl(State state)
    {
        if (!string.IsNullOrWhiteSpace(state.FlagImageUrl))
            return state.FlagImageUrl;
        return $"/images/flags/{state.Slug}/flag.webp";
    }

    private static string BuildVerdict(State state, IReadOnlyList<ScoredMetric> scored)
    {
        if (scored.Count == 0)
            return $"A practical, data-first look at living in {state.Name}.";
        var average = scored.Average(item => item.Score);
        return average switch
        {
            >= 67 => $"{state.Name} performs strongly across many everyday living measures, with a few trade-offs to check before moving.",
            >= 43 => $"{state.Name} offers a mixed but competitive living profile—the right fit depends on which costs and services matter most to you.",
            _ => $"{state.Name} has clear lifestyle advantages, but several statewide metrics deserve a closer look before moving."
        };
    }

    private static string BuildOverview(
        State state,
        IReadOnlyDictionary<string, StateLivingMetricViewModel> metrics,
        IReadOnlyList<ScoredMetric> strengths,
        IReadOnlyList<ScoredMetric> tradeoffs)
    {
        var cost = metrics.TryGetValue("cost-of-living", out var costMetric)
            ? $"Its cost-of-living index is {costMetric.DisplayValue}, compared with a national benchmark of 100."
            : "Cost-of-living data is not available for this place.";
        var strength = strengths.Count > 0 ? strengths[0].Metric.Name.ToLowerInvariant() : "several statewide measures";
        var tradeoff = tradeoffs.Count > 0 ? tradeoffs[0].Metric.Name.ToLowerInvariant() : "local conditions";
        return $"{cost} In the current dataset, {state.Name}'s strongest relative area is {strength}; its biggest statewide trade-off is {tradeoff}. Use these numbers as a screening tool, then compare the city, county, and neighborhood you are considering.";
    }

    private static string BuildMovingOverview(
        State state,
        IReadOnlyDictionary<string, StateLivingMetricViewModel> metrics,
        IReadOnlyList<ScoredMetric> strengths,
        IReadOnlyList<ScoredMetric> tradeoffs)
    {
        var region = string.IsNullOrWhiteSpace(state.Region) ? "the United States" : $"the {state.Region}";
        var housing = metrics.TryGetValue("home-value", out var home)
            ? $"a typical statewide home value of {home.DisplayValue}"
            : "housing costs that vary by market";
        var income = metrics.TryGetValue("median-income", out var incomeMetric)
            ? $"median household income of {incomeMetric.DisplayValue}"
            : "local income conditions";
        var strength = strengths.FirstOrDefault()?.Metric.Name.ToLowerInvariant() ?? "its lifestyle profile";
        var tradeoff = tradeoffs.FirstOrDefault()?.Metric.Name.ToLowerInvariant() ?? "local costs and services";

        return $"Moving to {state.Name} means comparing {housing} with {income}. Within {region}, the state stands out most for {strength}, while {tradeoff} is the first issue to investigate for the county and city on your shortlist. Before signing a lease or buying, compare commute patterns, insurance, taxes, schools, and neighborhood-level safety—not only the statewide average.";
    }

    private static string BuildCostOfLivingSummary(
        State state,
        IReadOnlyDictionary<string, StateLivingMetricViewModel> metrics)
    {
        if (!metrics.TryGetValue("cost-of-living", out var cost))
            return $"A comparable statewide cost-of-living index is not available for {state.Name}; use the housing, income, tax, and local price measures shown below.";

        var relation = cost.RawValue switch
        {
            < 95 => "below",
            > 105 => "above",
            _ => "close to"
        };
        var rent = metrics.TryGetValue("median-rent", out var rentMetric)
            ? $" Median gross rent is {rentMetric.DisplayValue}"
            : string.Empty;
        var home = metrics.TryGetValue("home-value", out var homeMetric)
            ? $", and the typical statewide home value is {homeMetric.DisplayValue}."
            : ".";
        var income = metrics.TryGetValue("median-income", out var incomeMetric)
            ? $" Compare those costs with median household income of {incomeMetric.DisplayValue}."
            : string.Empty;

        return $"The {state.Name} cost-of-living index is {cost.DisplayValue}, which is {relation} the national benchmark of 100.{rent}{home}{income}";
    }

    private static string BuildBestForSummary(
        State state,
        IReadOnlyDictionary<string, StateLivingMetricViewModel> metrics,
        IReadOnlyList<ScoredMetric> strengths)
    {
        var features = strengths
            .Take(2)
            .Select(item => item.Metric.Name.ToLowerInvariant())
            .ToList();
        var climate = metrics.TryGetValue("average-temperature", out var temperature)
            ? BuildClimateLabel(temperature.RawValue).ToLowerInvariant()
            : "varied climate";
        var featureText = features.Count switch
        {
            0 => "a balanced statewide profile",
            1 => features[0],
            _ => $"{features[0]} and {features[1]}"
        };

        return $"{state.Name} may fit people who value {featureText} and prefer a {climate}. It may be a weaker fit when the statewide trade-offs listed below conflict with a household's budget, work, health, or education priorities.";
    }

    private static IReadOnlyList<StateLivingFaqViewModel> BuildFaqs(
        State state,
        string verdict,
        string overview,
        string movingOverview,
        string costSummary,
        IReadOnlyDictionary<string, StateLivingMetricViewModel> metrics)
    {
        var safety = metrics.TryGetValue("violent-crime", out var safetyMetric)
            ? $"{safetyMetric.DisplayValue}, ranking #{safetyMetric.Rank} of {safetyMetric.AvailableStateCount} states with available data"
            : "not available in the current statewide dataset";
        var climate = metrics.TryGetValue("average-temperature", out var climateMetric)
            ? $"{climateMetric.DisplayValue} as a long-term statewide annual average"
            : "best checked through city-level climate normals";

        return new[]
        {
            new StateLivingFaqViewModel
            {
                Question = $"Is {state.Name} a good place to live?",
                Answer = $"{verdict} {overview}"
            },
            new StateLivingFaqViewModel
            {
                Question = $"What is the cost of living in {state.Name}?",
                Answer = costSummary
            },
            new StateLivingFaqViewModel
            {
                Question = $"What should I know before moving to {state.Name}?",
                Answer = movingOverview
            },
            new StateLivingFaqViewModel
            {
                Question = $"How safe is {state.Name}?",
                Answer = $"The statewide violent crime measure is {safety}. Crime can differ substantially by county, city, and neighborhood, so use this as a first comparison rather than a local safety forecast."
            },
            new StateLivingFaqViewModel
            {
                Question = $"What is the climate like in {state.Name}?",
                Answer = $"The climate measure used here is {climate}. Statewide averages smooth out elevation, coastal, urban, and north–south differences."
            }
        };
    }

    private static string BuildHubSummary(
        State state,
        StateLivingMetricViewModel? cost,
        StateLivingMetricViewModel? safety,
        StateLivingMetricViewModel? climate,
        StateLivingMetricViewModel? quality)
    {
        var parts = new List<string>();
        if (cost is not null)
            parts.Add(cost.RawValue <= 100 ? "below-average statewide costs" : "above-average statewide costs");
        if (safety is not null && safety.Rank <= Math.Ceiling(safety.AvailableStateCount / 3d))
            parts.Add("a top-third safety standing");
        if (quality is not null && quality.Rank <= Math.Ceiling(quality.AvailableStateCount / 3d))
            parts.Add("a top-third livability score");
        if (climate is not null)
            parts.Add(BuildClimateLabel(climate.RawValue).ToLowerInvariant());

        var summary = parts.Take(2).ToList();
        return summary.Count switch
        {
            0 => $"Open the {state.Name} guide for costs, safety, climate, and quality-of-life data.",
            1 => $"{state.Name} combines {summary[0]}.",
            _ => $"{state.Name} combines {summary[0]} with {summary[1]}."
        };
    }

    private static string BuildClimateLabel(double? averageTemperature)
    {
        if (!averageTemperature.HasValue)
            return "Climate data unavailable";
        return averageTemperature.Value switch
        {
            < 45 => "Cool annual climate",
            < 55 => "Four-season climate",
            < 65 => "Mild-to-warm climate",
            _ => "Warm annual climate"
        };
    }

    private static string BuildPoint(StateLivingMetricViewModel metric, bool positive)
    {
        var standing = metric.Rank <= Math.Max(5, metric.AvailableStateCount / 5)
            ? "top-tier"
            : metric.Rank > metric.AvailableStateCount * 4 / 5 ? "near the bottom of the range" : "better than many states";
        return positive
            ? $"{metric.Name}: {metric.DisplayValue}, ranking #{metric.Rank} of {metric.AvailableStateCount} ({standing})."
            : $"{metric.Name}: {metric.DisplayValue}, ranking #{metric.Rank} of {metric.AvailableStateCount}; check how this affects your household.";
    }

    private sealed record ScoredMetric(StateLivingMetricViewModel Metric, double Score);

    private sealed class StateLivingPhotoManifest
    {
        public Dictionary<string, List<StateLivingPhotoViewModel>> States { get; init; }
            = new(StringComparer.OrdinalIgnoreCase);
    }
}
