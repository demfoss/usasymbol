using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Text.Json;
using USASymbol.Models;
using USASymbol.Models.ViewModels;
using USASymbol.Services.Interface;
using usasymbol.Services.Interface;

namespace USASymbol.Services;

public sealed class CountyService : ICountyService
{
    private const string CacheKey = "county-directory:official-metrics:v1";

    private static readonly IReadOnlyDictionary<string, MetricDefinition> MetricDefinitions =
        new Dictionary<string, MetricDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["population"] = new("Population", "Residents counted by ACS.", 0),
            ["medianHouseholdIncome"] = new("Median household income", "Median annual household income.", 1),
            ["medianHomeValue"] = new("Median home value", "Median value of owner-occupied housing units.", -1),
            ["medianGrossRent"] = new("Median gross rent", "Median monthly gross rent.", -1),
            ["collegeEducatedRate"] = new("Bachelor’s degree or higher", "Share of adults age 25+ with a bachelor’s degree or higher.", 1),
            ["unemploymentRate"] = new("Unemployment rate", "Annual average unemployment rate.", -1),
            ["employment"] = new("Employment", "Annual average number of employed residents.", 0),
            ["laborForce"] = new("Labor force", "Annual average civilian labor force.", 0),
            ["lifeExpectancy"] = new("Life expectancy", "Expected years of life at birth.", 1),
            ["poorFairHealthRate"] = new("Poor or fair health", "Share of adults reporting poor or fair health.", -1),
            ["uninsuredRate"] = new("Uninsured", "Share of residents under age 65 without health insurance.", -1),
            ["primaryCareRatio"] = new("Primary care access", "Residents per primary care physician.", -1),
            ["mentalHealthProviderRatio"] = new("Mental health access", "Residents per mental health provider.", -1)
        };

    private static readonly string[] ProfileMetricOrder =
    {
        "population", "medianHouseholdIncome", "medianHomeValue", "medianGrossRent",
        "unemploymentRate", "collegeEducatedRate", "lifeExpectancy",
        "poorFairHealthRate", "uninsuredRate", "primaryCareRatio", "mentalHealthProviderRatio"
    };

    private readonly IStateService _stateService;
    private readonly IWebHostEnvironment _environment;
    private readonly IMemoryCache _cache;

    public CountyService(
        IStateService stateService,
        IWebHostEnvironment environment,
        IMemoryCache cache)
    {
        _stateService = stateService;
        _environment = environment;
        _cache = cache;
    }

    public async Task<CountyIndexViewModel?> GetIndexAsync(string stateSlug)
    {
        var state = await _stateService.GetStateBySlugAsync(stateSlug);
        if (state is null || !StateFipsCatalog.TryGetFips(state.Abbreviation, out var stateFips))
            return null;

        var data = await LoadAsync();
        var counties = BuildStateCounties(stateFips, data);
        if (counties.Count == 0)
            return null;

        return new CountyIndexViewModel
        {
            State = state,
            StateFips = stateFips,
            Counties = counties,
            LargestCounty = counties[0],
            MedianPopulation = Median(counties.Select(county => county.Population)),
            PublishedCount = counties.Count(county => county.Published),
            GeneratedOn = data.GeneratedOn,
            Sources = BuildSources(data)
        };
    }

    public async Task<CountyProfileViewModel?> GetProfileAsync(string stateSlug, string countySlug)
    {
        var index = await GetIndexAsync(stateSlug);
        if (index is null)
            return null;

        var countyListItem = index.Counties.FirstOrDefault(item =>
            string.Equals(item.Slug, countySlug, StringComparison.OrdinalIgnoreCase));
        if (countyListItem is null || !countyListItem.Published)
            return null;

        var data = await LoadAsync();
        if (!data.Counties.TryGetValue(countyListItem.Fips, out var countyRecord))
            return null;

        var stateRecords = data.Counties
            .Where(item => string.Equals(item.Value.ParentFips, countyListItem.ParentFips, StringComparison.Ordinal))
            .Select(item => item.Value)
            .ToList();
        var metricValues = BuildMetricValues(countyRecord, data);
        var metricComparisons = BuildComparisons(countyRecord, metricValues, stateRecords);
        var editorial = BuildEditorial(index.State, countyListItem, metricValues, metricComparisons);
        var nationalPopulationRank = data.Counties.Values.Count(item =>
            GetRaw(item, "population") > countyListItem.Population) + 1;
        var minPopulation = index.Counties.Min(item => item.Population);
        var maxPopulation = index.Counties.Max(item => item.Population);
        var rangePosition = maxPopulation <= minPopulation
            ? 50
            : 100d * (countyListItem.Population - minPopulation) / (maxPopulation - minPopulation);
        var statePopulationTotal = index.Counties.Sum(item => item.Population);
        var stateShare = statePopulationTotal <= 0
            ? 0
            : 100d * countyListItem.Population / statePopulationTotal;
        var countyIndex = index.Counties.ToList().FindIndex(item => item.Fips == countyListItem.Fips);
        var nearby = index.Counties
            .Skip(Math.Max(0, countyIndex - 2))
            .Take(5)
            .Where(item => item.Fips != countyListItem.Fips)
            .ToList();

        return new CountyProfileViewModel
        {
            State = index.State,
            County = new CountyPlaceViewModel
            {
                Fips = countyListItem.Fips,
                ParentFips = countyListItem.ParentFips,
                Name = countyListItem.Name,
                Slug = countyListItem.Slug,
                Published = true,
                Population = countyListItem.Population,
                Metrics = metricValues
            },
            CountyCount = index.Counties.Count,
            StatePopulationRank = countyListItem.StatePopulationRank,
            NationalPopulationRank = nationalPopulationRank,
            StateCountyMedianPopulation = index.MedianPopulation,
            StatePopulationShare = stateShare,
            PopulationRangePosition = Math.Clamp(rangePosition, 0, 100),
            LargestCounty = index.LargestCounty,
            NearbyPopulationCounties = nearby,
            MetricComparisons = metricComparisons,
            Sources = index.Sources,
            GeneratedOn = index.GeneratedOn,
            Summary = editorial.Summary,
            MovingOverview = editorial.MovingOverview,
            CostOverview = editorial.CostOverview,
            BestForOverview = editorial.BestForOverview,
            Pros = editorial.Pros,
            Cons = editorial.Cons,
            Faqs = editorial.Faqs
        };
    }

    public async Task<CountyMatchPageViewModel> GetMatcherAsync()
    {
        var states = (await _stateService.GetAllStatesAsync())
            .Where(state => !string.Equals(state.Abbreviation, "DC", StringComparison.OrdinalIgnoreCase))
            .OrderBy(state => state.Name)
            .ToList();
        var stateByFips = states
            .Select(state => (State: state, Fips: StateFipsCatalog.GetFips(state.Abbreviation)))
            .Where(item => item.Fips is not null)
            .ToDictionary(item => item.Fips!, item => item.State, StringComparer.Ordinal);
        var data = await LoadAsync();
        var counties = data.Counties
            .Where(item => stateByFips.ContainsKey(item.Value.ParentFips))
            .Select(item =>
            {
                var state = stateByFips[item.Value.ParentFips];
                return new CountyMatchItemViewModel
                {
                    Fips = item.Key,
                    Name = item.Value.Name,
                    Slug = item.Value.Slug,
                    StateName = state.Name,
                    StateSlug = state.Slug,
                    StateAbbreviation = state.Abbreviation,
                    Published = item.Value.Published,
                    Metrics = new Dictionary<string, double?>
                    {
                        ["income"] = GetRaw(item.Value, "medianHouseholdIncome"),
                        ["homeValue"] = GetRaw(item.Value, "medianHomeValue"),
                        ["rent"] = GetRaw(item.Value, "medianGrossRent"),
                        ["unemployment"] = GetRaw(item.Value, "unemploymentRate"),
                        ["education"] = GetRaw(item.Value, "collegeEducatedRate"),
                        ["lifeExpectancy"] = GetRaw(item.Value, "lifeExpectancy"),
                        ["poorFairHealth"] = GetRaw(item.Value, "poorFairHealthRate"),
                        ["uninsured"] = GetRaw(item.Value, "uninsuredRate")
                    }
                };
            })
            .OrderBy(item => item.StateName)
            .ThenBy(item => item.Name)
            .ToList();

        return new CountyMatchPageViewModel
        {
            Counties = counties,
            States = states.Select(state => new CountyMatchStateViewModel
            {
                Name = state.Name,
                Slug = state.Slug,
                Abbreviation = state.Abbreviation
            }).ToList(),
            MetricOptions = new[]
            {
                new CountyMatchMetricOptionViewModel { Key = "affordability", Name = "Housing affordability", Hint = "Lower home value and rent", Icon = "fa-solid fa-house", DefaultWeight = 70 },
                new CountyMatchMetricOptionViewModel { Key = "income", Name = "Household income", Hint = "Higher median income", Icon = "fa-solid fa-wallet", DefaultWeight = 55 },
                new CountyMatchMetricOptionViewModel { Key = "jobs", Name = "Job market", Hint = "Lower unemployment", Icon = "fa-solid fa-briefcase", DefaultWeight = 60 },
                new CountyMatchMetricOptionViewModel { Key = "education", Name = "Education", Hint = "Bachelor’s degree or higher", Icon = "fa-solid fa-graduation-cap", DefaultWeight = 40 },
                new CountyMatchMetricOptionViewModel { Key = "health", Name = "Health", Hint = "Outcomes and insurance coverage", Icon = "fa-solid fa-heart-pulse", DefaultWeight = 50 }
            },
            Sources = BuildSources(data),
            GeneratedOn = data.GeneratedOn
        };
    }

    public async Task<CountyRankingsPageViewModel> GetRankingsAsync(string? stateSlug = null)
    {
        var states = (await _stateService.GetAllStatesAsync())
            .Where(state => !string.Equals(state.Abbreviation, "DC", StringComparison.OrdinalIgnoreCase))
            .OrderBy(state => state.Name)
            .ToList();
        string? selectedFips = null;
        if (!string.IsNullOrWhiteSpace(stateSlug))
        {
            var selectedState = states.FirstOrDefault(state =>
                string.Equals(state.Slug, stateSlug, StringComparison.OrdinalIgnoreCase));
            if (selectedState is not null)
                selectedFips = StateFipsCatalog.GetFips(selectedState.Abbreviation);
        }

        var data = await LoadAsync();
        return new CountyRankingsPageViewModel
        {
            Sections = await BuildRankingSectionsAsync(data, states, selectedFips, 10),
            States = states.Select(state => new CountyMatchStateViewModel
            {
                Name = state.Name,
                Slug = state.Slug,
                Abbreviation = state.Abbreviation
            }).ToList(),
            SelectedStateSlug = selectedFips is null ? null : stateSlug,
            GeneratedOn = data.GeneratedOn
        };
    }

    public async Task<StateCountyHighlightsViewModel?> GetHighlightsAsync(string stateSlug)
    {
        var states = (await _stateService.GetAllStatesAsync())
            .Where(state => !string.Equals(state.Abbreviation, "DC", StringComparison.OrdinalIgnoreCase))
            .OrderBy(state => state.Name)
            .ToList();
        var state = states.FirstOrDefault(item =>
            string.Equals(item.Slug, stateSlug, StringComparison.OrdinalIgnoreCase));
        if (state is null || !StateFipsCatalog.TryGetFips(state.Abbreviation, out var stateFips))
            return null;

        var data = await LoadAsync();
        var countyCount = data.Counties.Values.Count(item => item.ParentFips == stateFips);
        return new StateCountyHighlightsViewModel
        {
            CountyCount = countyCount,
            Sections = await BuildRankingSectionsAsync(data, states, stateFips, 3)
        };
    }

    public async Task<IReadOnlyList<string>> GetPublishedPathsAsync()
    {
        var states = await _stateService.GetAllStatesAsync();
        var data = await LoadAsync();
        var paths = new List<string>();

        foreach (var state in states.Where(state =>
                     !string.Equals(state.Abbreviation, "DC", StringComparison.OrdinalIgnoreCase)))
        {
            if (!StateFipsCatalog.TryGetFips(state.Abbreviation, out var stateFips))
                continue;
            var counties = BuildStateCounties(stateFips, data);
            if (counties.Count == 0)
                continue;

            paths.Add($"/states/{state.Slug}/counties");
            paths.AddRange(counties
                .Where(county => county.Published)
                .Select(county => $"/states/{state.Slug}/counties/{county.Slug}"));
        }

        return paths;
    }

    private async Task<CountyDataFile> LoadAsync()
    {
        if (_cache.TryGetValue(CacheKey, out CountyDataFile? cached) && cached is not null)
            return cached;

        var path = Path.Combine(
            _environment.ContentRootPath,
            "Content",
            "places",
            "counties",
            "county-metrics.json");
        if (!File.Exists(path))
            return new CountyDataFile();

        await using var stream = File.OpenRead(path);
        var data = await JsonSerializer.DeserializeAsync<CountyDataFile>(
                       stream,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new CountyDataFile();
        _cache.Set(CacheKey, data, TimeSpan.FromHours(12));
        return data;
    }

    private static List<CountyListItemViewModel> BuildStateCounties(
        string stateFips,
        CountyDataFile data)
    {
        return data.Counties
            .Where(item => string.Equals(item.Value.ParentFips, stateFips, StringComparison.Ordinal))
            .OrderByDescending(item => GetRaw(item.Value, "population") ?? 0)
            .ThenBy(item => item.Value.Name, StringComparer.OrdinalIgnoreCase)
            .Select((item, index) => new CountyListItemViewModel
            {
                Fips = item.Key,
                ParentFips = stateFips,
                Name = item.Value.Name,
                Slug = item.Value.Slug,
                Population = ToLong(GetRaw(item.Value, "population")),
                MedianHouseholdIncome = GetRaw(item.Value, "medianHouseholdIncome"),
                MedianHomeValue = GetRaw(item.Value, "medianHomeValue"),
                MedianGrossRent = GetRaw(item.Value, "medianGrossRent"),
                CollegeEducatedRate = GetRaw(item.Value, "collegeEducatedRate"),
                UnemploymentRate = GetRaw(item.Value, "unemploymentRate"),
                LifeExpectancy = GetRaw(item.Value, "lifeExpectancy"),
                StatePopulationRank = index + 1,
                Published = item.Value.Published,
                AvailableMetricCount = item.Value.Metrics.Count
            })
            .ToList();
    }

    private static IReadOnlyList<MetricValue> BuildMetricValues(
        CountyDataRecord county,
        CountyDataFile data)
    {
        var metrics = new List<MetricValue>();
        foreach (var key in ProfileMetricOrder)
        {
            if (!county.Metrics.TryGetValue(key, out var record) ||
                !MetricDefinitions.TryGetValue(key, out var definition))
            {
                continue;
            }

            data.Sources.TryGetValue(record.SourceId, out var source);
            metrics.Add(new MetricValue
            {
                Key = key,
                Name = definition.Name,
                Raw = record.Raw,
                DisplayValue = FormatMetric(key, record.Raw),
                Direction = record.Direction,
                Unit = record.Unit,
                SourceId = record.SourceId,
                SourceName = source?.Name ?? record.SourceId
            });
        }
        return metrics;
    }

    private static IReadOnlyList<CountyMetricComparisonViewModel> BuildComparisons(
        CountyDataRecord county,
        IReadOnlyList<MetricValue> metrics,
        IReadOnlyList<CountyDataRecord> stateRecords)
    {
        var comparisons = new List<CountyMetricComparisonViewModel>();
        foreach (var metric in metrics)
        {
            var values = stateRecords
                .Select(record => GetRaw(record, metric.Key))
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .OrderBy(value => value)
                .ToArray();
            if (values.Length == 0)
                continue;

            var min = values[0];
            var max = values[^1];
            var position = max <= min ? 50 : 100d * (metric.Raw - min) / (max - min);
            var rank = metric.Direction < 0
                ? values.Count(value => value < metric.Raw) + 1
                : values.Count(value => value > metric.Raw) + 1;
            var median = values.Length % 2 == 0
                ? (values[values.Length / 2 - 1] + values[values.Length / 2]) / 2
                : values[values.Length / 2];
            var relation = Math.Abs(metric.Raw - median) < .00001
                ? "at"
                : metric.Raw > median ? "above" : "below";

            comparisons.Add(new CountyMetricComparisonViewModel
            {
                Metric = metric,
                StateRank = rank,
                AvailableCountyCount = values.Length,
                RangePosition = Math.Clamp(position, 0, 100),
                Context = $"{ToSentenceCase(FormatMetric(metric.Key, metric.Raw))}; {relation} the state county median of {FormatMetric(metric.Key, median)}."
            });
        }
        return comparisons;
    }

    private static Task<IReadOnlyList<CountyRankingSectionViewModel>> BuildRankingSectionsAsync(
        CountyDataFile data,
        IReadOnlyList<State> states,
        string? stateFips,
        int take)
    {
        var stateByFips = states
            .Select(state => (State: state, Fips: StateFipsCatalog.GetFips(state.Abbreviation)))
            .Where(item => item.Fips is not null)
            .ToDictionary(item => item.Fips!, item => item.State, StringComparer.Ordinal);
        var records = data.Counties
            .Where(item => stateByFips.ContainsKey(item.Value.ParentFips) &&
                           (stateFips is null || item.Value.ParentFips == stateFips))
            .ToList();

        var ranges = new Dictionary<string, (double Min, double Max)>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in new[]
                 {
                     "medianHouseholdIncome", "medianHomeValue", "medianGrossRent",
                     "unemploymentRate", "collegeEducatedRate", "lifeExpectancy",
                     "poorFairHealthRate", "uninsuredRate"
                 })
        {
            var values = records.Select(item => GetRaw(item.Value, key))
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToArray();
            if (values.Length > 0)
                ranges[key] = (values.Min(), values.Max());
        }

        var definitions = new[]
        {
            new RankingDefinition(
                "affordable",
                "Most affordable counties",
                "A transparent average of lower median home value and lower median gross rent.",
                "fa-solid fa-house",
                record => AverageAvailable(
                    Normalize(GetRaw(record, "medianHomeValue"), ranges.GetValueOrDefault("medianHomeValue"), false),
                    Normalize(GetRaw(record, "medianGrossRent"), ranges.GetValueOrDefault("medianGrossRent"), false)),
                record =>
                {
                    var home = GetRaw(record, "medianHomeValue");
                    var rent = GetRaw(record, "medianGrossRent");
                    return $"Home {FormatOptional("medianHomeValue", home)} · Rent {FormatOptional("medianGrossRent", rent)}";
                }),
            new RankingDefinition(
                "income",
                "Highest household income",
                "Counties ordered by ACS median household income.",
                "fa-solid fa-wallet",
                record => GetRaw(record, "medianHouseholdIncome"),
                record => FormatOptional("medianHouseholdIncome", GetRaw(record, "medianHouseholdIncome"))),
            new RankingDefinition(
                "jobs",
                "Lowest unemployment",
                "Counties ordered by the BLS LAUS annual-average unemployment rate.",
                "fa-solid fa-briefcase",
                record => Negate(GetRaw(record, "unemploymentRate")),
                record => FormatOptional("unemploymentRate", GetRaw(record, "unemploymentRate"))),
            new RankingDefinition(
                "education",
                "Highest college attainment",
                "Counties ordered by the share of adults age 25+ with a bachelor’s degree or higher.",
                "fa-solid fa-graduation-cap",
                record => GetRaw(record, "collegeEducatedRate"),
                record => FormatOptional("collegeEducatedRate", GetRaw(record, "collegeEducatedRate"))),
            new RankingDefinition(
                "health",
                "Strongest health profile",
                "Average of life expectancy, lower poor-or-fair health, and lower uninsured rate when at least two fields are available.",
                "fa-solid fa-heart-pulse",
                record => AverageAvailableRequired(
                    2,
                    Normalize(GetRaw(record, "lifeExpectancy"), ranges.GetValueOrDefault("lifeExpectancy"), true),
                    Normalize(GetRaw(record, "poorFairHealthRate"), ranges.GetValueOrDefault("poorFairHealthRate"), false),
                    Normalize(GetRaw(record, "uninsuredRate"), ranges.GetValueOrDefault("uninsuredRate"), false)),
                BuildHealthDisplay)
        };

        IReadOnlyList<CountyRankingSectionViewModel> sections = definitions.Select(definition =>
        {
            var ranked = records
                .Select(item => new
                {
                    Fips = item.Key,
                    Record = item.Value,
                    Score = definition.Score(item.Value)
                })
                .Where(item => item.Score.HasValue)
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Record.Name, StringComparer.OrdinalIgnoreCase)
                .Take(take)
                .Select((item, index) =>
                {
                    var state = stateByFips[item.Record.ParentFips];
                    return new CountyRankingItemViewModel
                    {
                        Rank = index + 1,
                        Name = item.Record.Name,
                        Slug = item.Record.Slug,
                        StateName = state.Name,
                        StateSlug = state.Slug,
                        Published = item.Record.Published,
                        DisplayValue = definition.Display(item.Record),
                        RawValue = item.Score!.Value
                    };
                })
                .ToList();
            return new CountyRankingSectionViewModel
            {
                Key = definition.Key,
                Title = definition.Title,
                Description = definition.Description,
                Icon = definition.Icon,
                Counties = ranked
            };
        }).ToList();
        return Task.FromResult(sections);
    }

    private static CountyEditorialContent BuildEditorial(
        State state,
        CountyListItemViewModel county,
        IReadOnlyList<MetricValue> metrics,
        IReadOnlyList<CountyMetricComparisonViewModel> comparisons)
    {
        var byKey = metrics.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);
        var directional = comparisons
            .Where(item => item.Metric.Direction != 0 && item.AvailableCountyCount > 1)
            .Select(item => new
            {
                Comparison = item,
                Score = 100d * (item.AvailableCountyCount - item.StateRank) /
                        (item.AvailableCountyCount - 1)
            })
            .ToList();
        var strengths = directional.OrderByDescending(item => item.Score).Take(3).ToList();
        var tradeoffs = directional.OrderBy(item => item.Score).Take(3).ToList();

        var income = byKey.GetValueOrDefault("medianHouseholdIncome");
        var home = byKey.GetValueOrDefault("medianHomeValue");
        var rent = byKey.GetValueOrDefault("medianGrossRent");
        var jobs = byKey.GetValueOrDefault("unemploymentRate");
        var education = byKey.GetValueOrDefault("collegeEducatedRate");
        var life = byKey.GetValueOrDefault("lifeExpectancy");

        var summaryParts = new List<string>
        {
            $"{county.Name} has {county.Population:N0} residents and ranks #{county.StatePopulationRank} by population among {state.Name} county equivalents."
        };
        if (income is not null)
            summaryParts.Add($"Median household income is {income.DisplayValue}.");
        if (jobs is not null)
            summaryParts.Add($"The annual-average unemployment rate is {jobs.DisplayValue}.");
        if (strengths.Count > 0)
            summaryParts.Add($"Its clearest relative strength is {strengths[0].Comparison.Metric.Name.ToLowerInvariant()}.");

        var costOverview = home is null && rent is null
            ? $"County-level housing values are unavailable for {county.Name}; compare local listings before estimating a moving budget."
            : $"Housing benchmarks are {(home is null ? "not available" : home.DisplayValue + " median home value")} and {(rent is null ? "rent not available" : rent.DisplayValue + " median gross rent")}. " +
              (income is null
                  ? "Household income is unavailable, so housing-to-income fit should be checked locally."
                  : $"Set those costs against a median household income of {income.DisplayValue}, while remembering that household circumstances and municipalities vary.");

        var bestFor = strengths.Count == 0
            ? $"{county.Name} may suit people whose location needs align with its municipalities, commute patterns, and housing inventory; the available data does not support a strong relative claim."
            : $"{county.Name} may be worth a closer look for movers prioritizing {JoinNatural(strengths.Select(item => item.Comparison.Metric.Name.ToLowerInvariant()))}. " +
              (education is not null ? $"College attainment is {education.DisplayValue}" : "Education data is unavailable") +
              (life is not null ? $" and life expectancy is {life.DisplayValue}." : ".");

        var pros = strengths.Select(item =>
                $"{item.Comparison.Metric.Name}: {item.Comparison.Metric.DisplayValue}, rank #{item.Comparison.StateRank} of {item.Comparison.AvailableCountyCount} available {state.Name} counties.")
            .ToList();
        var cons = tradeoffs.Select(item =>
                $"{item.Comparison.Metric.Name}: {item.Comparison.Metric.DisplayValue}, rank #{item.Comparison.StateRank} of {item.Comparison.AvailableCountyCount}; investigate local variation before moving.")
            .ToList();
        if (pros.Count == 0)
            pros.Add("The profile provides county-specific figures rather than copying statewide averages.");
        if (cons.Count == 0)
            cons.Add("County averages can hide differences between municipalities and neighborhoods.");

        var faqs = new List<CountyFaqViewModel>
        {
            new() { Question = $"Is {county.Name} a good place to live?", Answer = bestFor },
            new() { Question = $"What should I know before moving to {county.Name}?", Answer = $"Start with housing, employment, commute, schools, healthcare access, and the specific municipality. {costOverview}" },
            new() { Question = $"What is the population of {county.Name}?", Answer = $"{county.Name} has {county.Population:N0} residents in the 2020–2024 ACS 5-year dataset." },
            new() { Question = $"What is the FIPS code for {county.Name}?", Answer = $"The five-digit county FIPS code is {county.Fips}; the parent state FIPS is {county.ParentFips}." }
        };
        if (income is not null)
            faqs.Add(new CountyFaqViewModel { Question = $"What is the median household income in {county.Name}?", Answer = $"The ACS median household income is {income.DisplayValue}." });
        if (home is not null || rent is not null)
            faqs.Add(new CountyFaqViewModel { Question = $"How much does housing cost in {county.Name}?", Answer = costOverview });
        if (jobs is not null)
            faqs.Add(new CountyFaqViewModel { Question = $"What is the unemployment rate in {county.Name}?", Answer = $"The BLS LAUS annual-average unemployment rate is {jobs.DisplayValue}." });

        return new CountyEditorialContent(
            string.Join(" ", summaryParts),
            $"Before moving, compare municipalities inside {county.Name}, because county averages do not describe every neighborhood. Review current housing inventory, commute destinations, school districts, healthcare access, and job locations. {summaryParts.Last()}",
            costOverview,
            bestFor,
            pros,
            cons,
            faqs);
    }

    private static double? Normalize(double? value, (double Min, double Max) range, bool higherIsBetter)
    {
        if (!value.HasValue || range.Max <= range.Min)
            return null;
        var normalized = 100d * (value.Value - range.Min) / (range.Max - range.Min);
        return higherIsBetter ? normalized : 100d - normalized;
    }

    private static double? AverageAvailable(params double?[] values)
    {
        var available = values.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
        return available.Length == 0 ? null : available.Average();
    }

    private static double? AverageAvailableRequired(int minimum, params double?[] values)
    {
        var available = values.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
        return available.Length < minimum ? null : available.Average();
    }

    private static double? Negate(double? value) => value.HasValue ? -value.Value : null;

    private static string FormatOptional(string key, double? value) =>
        value.HasValue ? FormatMetric(key, value.Value) : "n/a";

    private static string BuildHealthDisplay(CountyDataRecord record)
    {
        var parts = new List<string>();
        var life = GetRaw(record, "lifeExpectancy");
        var poorFair = GetRaw(record, "poorFairHealthRate");
        var uninsured = GetRaw(record, "uninsuredRate");
        if (life.HasValue)
            parts.Add($"Life {FormatMetric("lifeExpectancy", life.Value)}");
        if (poorFair.HasValue)
            parts.Add($"Poor/fair health {FormatMetric("poorFairHealthRate", poorFair.Value)}");
        if (uninsured.HasValue)
            parts.Add($"Uninsured {FormatMetric("uninsuredRate", uninsured.Value)}");
        return string.Join(" · ", parts);
    }

    private static string JoinNatural(IEnumerable<string> values)
    {
        var items = values.ToArray();
        return items.Length switch
        {
            0 => "local fit",
            1 => items[0],
            2 => $"{items[0]} and {items[1]}",
            _ => $"{string.Join(", ", items[..^1])}, and {items[^1]}"
        };
    }

    private static IReadOnlyList<CountySourceViewModel> BuildSources(CountyDataFile data) =>
        data.Sources.Select(item => new CountySourceViewModel
        {
            Id = item.Key,
            Name = item.Value.Name,
            Url = item.Value.Url,
            Period = item.Value.Period
        }).ToList();

    private static double? GetRaw(CountyDataRecord county, string key) =>
        county.Metrics.TryGetValue(key, out var metric) ? metric.Raw : null;

    private static long ToLong(double? value) =>
        value.HasValue ? Convert.ToInt64(Math.Round(value.Value)) : 0;

    private static string FormatMetric(string key, double value) => key switch
    {
        "population" or "employment" or "laborForce" =>
            value.ToString("N0", CultureInfo.GetCultureInfo("en-US")),
        "medianHouseholdIncome" or "medianHomeValue" =>
            value.ToString("$#,0", CultureInfo.GetCultureInfo("en-US")),
        "medianGrossRent" =>
            $"{value.ToString("$#,0", CultureInfo.GetCultureInfo("en-US"))}/mo",
        "collegeEducatedRate" or "unemploymentRate" or "poorFairHealthRate" or "uninsuredRate" =>
            $"{value:0.#}%",
        "lifeExpectancy" =>
            $"{value:0.#} years",
        "primaryCareRatio" or "mentalHealthProviderRatio" =>
            $"1 per {value:N0} residents",
        _ => value.ToString("0.##", CultureInfo.InvariantCulture)
    };

    private static string ToSentenceCase(string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToUpperInvariant(value[0]) + value[1..];

    private static long Median(IEnumerable<long> values)
    {
        var ordered = values.OrderBy(value => value).ToArray();
        if (ordered.Length == 0)
            return 0;
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0
            ? (ordered[middle - 1] + ordered[middle]) / 2
            : ordered[middle];
    }

    private sealed record MetricDefinition(string Name, string Description, int Direction);
    private sealed record RankingDefinition(
        string Key,
        string Title,
        string Description,
        string Icon,
        Func<CountyDataRecord, double?> Score,
        Func<CountyDataRecord, string> Display);
    private sealed record CountyEditorialContent(
        string Summary,
        string MovingOverview,
        string CostOverview,
        string BestForOverview,
        IReadOnlyList<string> Pros,
        IReadOnlyList<string> Cons,
        IReadOnlyList<CountyFaqViewModel> Faqs);

    private sealed class CountyDataFile
    {
        public int SchemaVersion { get; init; }
        public string GeneratedOn { get; init; } = string.Empty;
        public Dictionary<string, CountySourceRecord> Sources { get; init; } =
            new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, CountyDataRecord> Counties { get; init; } =
            new(StringComparer.Ordinal);
    }

    private sealed class CountySourceRecord
    {
        public string Name { get; init; } = string.Empty;
        public string Url { get; init; } = string.Empty;
        public string Period { get; init; } = string.Empty;
        public string Release { get; init; } = string.Empty;
    }

    private sealed class CountyDataRecord
    {
        public string ParentFips { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public bool Published { get; init; }
        public Dictionary<string, CountyMetricRecord> Metrics { get; init; } =
            new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class CountyMetricRecord
    {
        public double Raw { get; init; }
        public string Unit { get; init; } = string.Empty;
        public int Direction { get; init; }
        public string SourceId { get; init; } = string.Empty;
    }
}
