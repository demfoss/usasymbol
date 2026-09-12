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

        /// <summary>Slug into ComparisonMetricsConfig — lets the methodology page cite each metric's real source without duplicating that data per state.</summary>
        public string ComparisonSlug { get; init; } = string.Empty;
    }

    public sealed class StateMatchPageViewModel
    {
        public IReadOnlyList<StateMatchPlaceViewModel> Places { get; init; } = Array.Empty<StateMatchPlaceViewModel>();
        public IReadOnlyList<StateMatchMetricOption> MetricOptions { get; init; } = Array.Empty<StateMatchMetricOption>();
    }

    /// <summary>One scored, ranked place — the server-side twin of the "scored" rows state-match.js builds in the browser.</summary>
    public sealed class StateMatchRankedPlace
    {
        public StateMatchPlaceViewModel Place { get; init; } = null!;
        public int Score { get; init; }
        public int Rank { get; set; }
        public string? StrongestMetricName { get; init; }
        public string? WeakestMetricName { get; init; }
    }

    /// <summary>Cross-link to another preset landing page, shown at the bottom of each one.</summary>
    public sealed record StateMatchPresetLinkViewModel(string Slug, string NavLabel);

    /// <summary>A tiny, iframe-friendly "top 5" card meant to be embedded on other sites — its own bare layout, no site chrome.</summary>
    public sealed class StateMatchWidgetViewModel
    {
        public string? PresetSlug { get; init; }
        public string PresetLabel { get; init; } = "Balanced";
        public IReadOnlyList<StateMatchRankedPlace> Top5 { get; init; } = Array.Empty<StateMatchRankedPlace>();
        public IReadOnlyList<StateMatchPresetLinkViewModel> QuickPresets { get; init; } = Array.Empty<StateMatchPresetLinkViewModel>();
    }

    /// <summary>A pre-scored State Match landing page — SEO copy and a real top-10 rendered server-side, plus the interactive tool below it.</summary>
    public sealed class StateMatchPresetPageViewModel
    {
        public StateMatchPageViewModel Tool { get; init; } = new();
        public string Slug { get; init; } = string.Empty;
        public string Heading { get; init; } = string.Empty;
        public string Lede { get; init; } = string.Empty;
        public IReadOnlyDictionary<string, int> InitialWeights { get; init; } = new Dictionary<string, int>();
        public IReadOnlyList<StateMatchRankedPlace> Top10 { get; init; } = Array.Empty<StateMatchRankedPlace>();
        public IReadOnlyList<StateMatchPresetLinkViewModel> OtherPresets { get; init; } = Array.Empty<StateMatchPresetLinkViewModel>();
    }
}
