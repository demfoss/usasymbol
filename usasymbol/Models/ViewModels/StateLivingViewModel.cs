using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels;

public class StateLivingViewModel
{
    public State State { get; init; } = new();
    public string FlagImageUrl { get; init; } = string.Empty;
    public string Verdict { get; init; } = string.Empty;
    public string Overview { get; init; } = string.Empty;
    public string MovingOverview { get; init; } = string.Empty;
    public string CostOfLivingSummary { get; init; } = string.Empty;
    public string BestForSummary { get; init; } = string.Empty;
    public IReadOnlyList<StateLivingMetricViewModel> KeyMetrics { get; init; } = Array.Empty<StateLivingMetricViewModel>();
    public IReadOnlyList<StateLivingMetricGroupViewModel> MetricGroups { get; init; } = Array.Empty<StateLivingMetricGroupViewModel>();
    public IReadOnlyList<string> Pros { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Cons { get; init; } = Array.Empty<string>();
    public IReadOnlyList<StateLivingFaqViewModel> Faqs { get; init; } = Array.Empty<StateLivingFaqViewModel>();
    public IReadOnlyList<StateLivingSourceViewModel> Sources { get; init; } = Array.Empty<StateLivingSourceViewModel>();
    public IReadOnlyList<StateLivingPhotoViewModel> Photos { get; init; } = Array.Empty<StateLivingPhotoViewModel>();
    public IReadOnlyList<ParkContent> Parks { get; init; } = Array.Empty<ParkContent>();
    public IReadOnlyList<State> RelatedStates { get; init; } = Array.Empty<State>();
    public string DataReviewedOn { get; init; } = string.Empty;
    public int StateCount { get; init; }
    public StateCountyHighlightsViewModel? CountyHighlights { get; init; }
}

public class StateLivingHubViewModel
{
    public IReadOnlyList<StateLivingCardViewModel> States { get; init; } = Array.Empty<StateLivingCardViewModel>();
    public string DataReviewedOn { get; init; } = string.Empty;
    public int StateCount => States.Count;
}

public class StateLivingCardViewModel
{
    public State State { get; init; } = new();
    public string FlagImageUrl { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public StateLivingMetricViewModel? CostOfLiving { get; init; }
    public StateLivingMetricViewModel? Safety { get; init; }
    public StateLivingMetricViewModel? Climate { get; init; }
    public StateLivingMetricViewModel? QualityOfLife { get; init; }
    public string ClimateLabel { get; init; } = string.Empty;
    public bool IsAffordable { get; init; }
    public bool IsSafer { get; init; }
    public bool IsWarm { get; init; }
    public bool IsHighQualityOfLife { get; init; }
}

public class StateLivingMetricGroupViewModel
{
    public string Name { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public IReadOnlyList<StateLivingMetricViewModel> Metrics { get; init; } = Array.Empty<StateLivingMetricViewModel>();
}

public class StateLivingMetricViewModel
{
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string DisplayValue { get; init; } = string.Empty;
    public double RawValue { get; init; }
    public double MinValue { get; init; }
    public double MaxValue { get; init; }
    public double RangePosition { get; init; }
    public int Rank { get; init; }
    public int AvailableStateCount { get; init; }
    public bool HigherIsBetter { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public string SourceUrl { get; init; } = string.Empty;
    public string DataYear { get; init; } = string.Empty;
    public string ReviewedOn { get; init; } = string.Empty;
}

public class StateLivingFaqViewModel
{
    public string Question { get; init; } = string.Empty;
    public string Answer { get; init; } = string.Empty;
}

public class StateLivingSourceViewModel
{
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string DataPeriod { get; init; } = string.Empty;
    public string ReviewedOn { get; init; } = string.Empty;
}

public class StateLivingPhotoViewModel
{
    public string ImageUrl { get; init; } = string.Empty;
    public string Alt { get; init; } = string.Empty;
    public string Credit { get; init; } = string.Empty;
    public string CreditUrl { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public string LocationUrl { get; init; } = string.Empty;
    public string SourceName { get; init; } = string.Empty;
}
