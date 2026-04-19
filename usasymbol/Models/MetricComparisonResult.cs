namespace USASymbol.Models
{
    public class MetricComparisonResult
    {
        public ComparisonMetricDefinition Metric { get; set; } = null!;

        public string? DisplayValueA { get; set; }
        public string? DisplayValueB { get; set; }

        public double? NumericA { get; set; }
        public double? NumericB { get; set; }

        /// <summary>Slug of the winning state, or null for ties / text metrics.</summary>
        public string? WinnerSlug { get; set; }

        public string? SummaryText { get; set; }

        /// <summary>Human-readable difference, e.g. "2.3x larger" or "+5,000,000 people".</summary>
        public string? DifferenceText { get; set; }
    }
}
