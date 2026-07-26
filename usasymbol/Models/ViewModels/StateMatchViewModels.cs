namespace USASymbol.Models.ViewModels
{
    public enum PlaceKind
    {
        State,
        County
    }

    public enum NormalizationFrame
    {
        Nation50States,
        WithinState,
        NationAllCounties
    }

    /// <summary>
    /// Shared shape for a state or county that can participate in matching and metric rendering.
    /// County implementations can be added without changing the State Match UI contract.
    /// </summary>
    public interface IPlaceMetricsViewModel
    {
        PlaceKind Kind { get; }
        string Fips { get; }
        string? ParentFips { get; }
        string Name { get; }
        string Slug { get; }
        IReadOnlyList<MetricValue> Metrics { get; }
        bool Published { get; }
    }

    public sealed class MetricValue
    {
        public string Key { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public double Raw { get; init; }
        public string DisplayValue { get; init; } = string.Empty;
        public int Direction { get; init; } = 1;
        public string Unit { get; init; } = string.Empty;
        public string SourceId { get; init; } = string.Empty;
        public string SourceName { get; init; } = string.Empty;
    }

    public sealed class StateMatchPlaceViewModel : IPlaceMetricsViewModel
    {
        public PlaceKind Kind { get; init; } = PlaceKind.State;
        public string Fips { get; init; } = string.Empty;
        public string? ParentFips { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public IReadOnlyList<MetricValue> Metrics { get; init; } = Array.Empty<MetricValue>();
        public bool Published { get; init; } = true;
        public bool HasCounties { get; init; }
        public string Abbreviation { get; init; } = string.Empty;
        public string Capital { get; init; } = string.Empty;
        public string Region { get; init; } = string.Empty;
        public int? Population { get; init; }
        public string FlagImageUrl { get; init; } = string.Empty;
    }

    public sealed class StateMatchMetricOption
    {
        public string Key { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Hint { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Icon { get; init; } = string.Empty;
        public int DefaultWeight { get; init; } = 50;
    }

    public sealed class StateMatchPageViewModel
    {
        public IReadOnlyList<StateMatchPlaceViewModel> Places { get; init; } = Array.Empty<StateMatchPlaceViewModel>();
        public IReadOnlyList<StateMatchMetricOption> MetricOptions { get; init; } = Array.Empty<StateMatchMetricOption>();
    }
}
