namespace USASymbol.Models.ViewModels
{
    /// <summary>One category tab in the unified metric comparison list (General + one per metric group).</summary>
    public class MetricComparisonTab
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
        public string Icon { get; set; } = "";
        public List<MetricComparisonResult> Results { get; set; } = new();
        public string? LeaderSlug { get; set; }
        public string? CategorySlug { get; set; }
    }

    /// <summary>
    /// Model for _MetricComparisonList.cshtml — the single diverging-bar component that replaces the old
    /// spotlight cards, Full Comparison table, and Dive Deeper link grid on the compare Overview page.
    /// </summary>
    public class MetricComparisonListViewModel
    {
        public State StateA { get; set; } = null!;
        public State StateB { get; set; } = null!;
        public string PairSlug { get; set; } = "";
        public string AccentA { get; set; } = "";
        public string AccentB { get; set; } = "";
        public List<MetricComparisonTab> Tabs { get; set; } = new();
    }
}
