namespace USASymbol.Models.ViewModels
{
    public class StatePairComparisonViewModel
    {
        public State StateA { get; set; } = null!;
        public State StateB { get; set; } = null!;

        public StateStats? StatsA { get; set; }
        public StateStats? StatsB { get; set; }

        /// <summary>Results for every available metric.</summary>
        public List<MetricComparisonResult> MetricResults { get; set; } = new();

        /// <summary>All metric definitions (for building deep-dive links).</summary>
        public List<ComparisonMetricDefinition> AllMetrics { get; set; } = new();

        /// <summary>Canonical URL pair slug, always alphabetically sorted.</summary>
        public string PairSlug => $"{StateA.Slug}-vs-{StateB.Slug}";

        /// <summary>Related pairs to link from the bottom of the page.</summary>
        public List<(State A, State B)> RelatedPairs { get; set; } = new();
    }
}
