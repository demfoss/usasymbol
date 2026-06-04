namespace USASymbol.Models.ViewModels
{
    public class MetricComparisonViewModel
    {
        public State StateA { get; set; } = null!;
        public State StateB { get; set; } = null!;

        public StateStats? StatsA { get; set; }
        public StateStats? StatsB { get; set; }

        public ComparisonMetricDefinition Metric { get; set; } = null!;
        public MetricComparisonResult Result { get; set; } = null!;

        public string PairSlug => $"{StateA.Slug}-vs-{StateB.Slug}";

        /// <summary>Links to other metrics for the same pair.</summary>
        public List<ComparisonMetricDefinition> OtherMetrics { get; set; } = new();

        /// <summary>All 50 states ranked for this metric (best rank first). Empty for text metrics.</summary>
        public List<StateRankRow> AllStateRanks { get; set; } = new();

        /// <summary>Whether a higher numeric value is the better outcome for this metric.</summary>
        public bool HigherIsBetter { get; set; } = true;
    }
}
