namespace USASymbol.Models.ViewModels;

public sealed class CountyIndexViewModel
{
    public State State { get; init; } = new();
    public string StateFips { get; init; } = string.Empty;
    public IReadOnlyList<CountyListItemViewModel> Counties { get; init; } = Array.Empty<CountyListItemViewModel>();
    public CountyListItemViewModel? LargestCounty { get; init; }
    public long MedianPopulation { get; init; }
    public int PublishedCount { get; init; }
    public string GeneratedOn { get; init; } = string.Empty;
    public IReadOnlyList<CountySourceViewModel> Sources { get; init; } = Array.Empty<CountySourceViewModel>();
}

public sealed class CountyListItemViewModel
{
    public string Fips { get; init; } = string.Empty;
    public string ParentFips { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public long Population { get; init; }
    public double? MedianHouseholdIncome { get; init; }
    public double? MedianHomeValue { get; init; }
    public double? MedianGrossRent { get; init; }
    public double? CollegeEducatedRate { get; init; }
    public double? UnemploymentRate { get; init; }
    public double? LifeExpectancy { get; init; }
    public int StatePopulationRank { get; init; }
    public bool Published { get; init; }
    public int AvailableMetricCount { get; init; }
}

public sealed class CountyProfileViewModel
{
    public State State { get; init; } = new();
    public CountyPlaceViewModel County { get; init; } = new();
    public int CountyCount { get; init; }
    public int StatePopulationRank { get; init; }
    public int NationalPopulationRank { get; init; }
    public long StateCountyMedianPopulation { get; init; }
    public double StatePopulationShare { get; init; }
    public double PopulationRangePosition { get; init; }
    public CountyListItemViewModel? LargestCounty { get; init; }
    public IReadOnlyList<CountyListItemViewModel> NearbyPopulationCounties { get; init; } = Array.Empty<CountyListItemViewModel>();
    public IReadOnlyList<CountyMetricComparisonViewModel> MetricComparisons { get; init; } =
        Array.Empty<CountyMetricComparisonViewModel>();
    public IReadOnlyList<CountySourceViewModel> Sources { get; init; } = Array.Empty<CountySourceViewModel>();
    public string GeneratedOn { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string MovingOverview { get; init; } = string.Empty;
    public string CostOverview { get; init; } = string.Empty;
    public string BestForOverview { get; init; } = string.Empty;
    public IReadOnlyList<string> Pros { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Cons { get; init; } = Array.Empty<string>();
    public IReadOnlyList<CountyFaqViewModel> Faqs { get; init; } = Array.Empty<CountyFaqViewModel>();
}

public sealed class CountyPlaceViewModel : IPlaceMetricsViewModel
{
    public PlaceKind Kind { get; init; } = PlaceKind.County;
    public string Fips { get; init; } = string.Empty;
    public string? ParentFips { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public IReadOnlyList<MetricValue> Metrics { get; init; } = Array.Empty<MetricValue>();
    public bool Published { get; init; }
    public long Population { get; init; }
}

public sealed class CountyMetricComparisonViewModel
{
    public MetricValue Metric { get; init; } = new();
    public int StateRank { get; init; }
    public int AvailableCountyCount { get; init; }
    public double RangePosition { get; init; }
    public string Context { get; init; } = string.Empty;
}

public sealed class CountySourceViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Period { get; init; } = string.Empty;
}

public sealed class CountyFaqViewModel
{
    public string Question { get; init; } = string.Empty;
    public string Answer { get; init; } = string.Empty;
}

public sealed class CountyMatchPageViewModel
{
    public IReadOnlyList<CountyMatchItemViewModel> Counties { get; init; } = Array.Empty<CountyMatchItemViewModel>();
    public IReadOnlyList<CountyMatchStateViewModel> States { get; init; } = Array.Empty<CountyMatchStateViewModel>();
    public IReadOnlyList<CountyMatchMetricOptionViewModel> MetricOptions { get; init; } =
        Array.Empty<CountyMatchMetricOptionViewModel>();
    public IReadOnlyList<CountySourceViewModel> Sources { get; init; } = Array.Empty<CountySourceViewModel>();
    public string GeneratedOn { get; init; } = string.Empty;
}

public sealed class CountyMatchItemViewModel
{
    public string Fips { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string StateName { get; init; } = string.Empty;
    public string StateSlug { get; init; } = string.Empty;
    public string StateAbbreviation { get; init; } = string.Empty;
    public bool Published { get; init; }
    public IReadOnlyDictionary<string, double?> Metrics { get; init; } =
        new Dictionary<string, double?>();
}

public sealed class CountyMatchStateViewModel
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Abbreviation { get; init; } = string.Empty;
}

public sealed class CountyMatchMetricOptionViewModel
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Hint { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public int DefaultWeight { get; init; }
}

public sealed class CountyRankingsPageViewModel
{
    public IReadOnlyList<CountyRankingSectionViewModel> Sections { get; init; } =
        Array.Empty<CountyRankingSectionViewModel>();
    public IReadOnlyList<CountyMatchStateViewModel> States { get; init; } =
        Array.Empty<CountyMatchStateViewModel>();
    public string? SelectedStateSlug { get; init; }
    public string GeneratedOn { get; init; } = string.Empty;
}

public sealed class CountyRankingSectionViewModel
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public IReadOnlyList<CountyRankingItemViewModel> Counties { get; init; } =
        Array.Empty<CountyRankingItemViewModel>();
}

public sealed class CountyRankingItemViewModel
{
    public int Rank { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string StateName { get; init; } = string.Empty;
    public string StateSlug { get; init; } = string.Empty;
    public bool Published { get; init; }
    public string DisplayValue { get; init; } = string.Empty;
    public double RawValue { get; init; }
}

public sealed class StateCountyHighlightsViewModel
{
    public int CountyCount { get; init; }
    public IReadOnlyList<CountyRankingSectionViewModel> Sections { get; init; } =
        Array.Empty<CountyRankingSectionViewModel>();
}
