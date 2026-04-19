namespace USASymbol.Models
{
    public enum MetricType { Numeric, Text, Date, Ordinal }

    public class ComparisonMetricDefinition
    {
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string GroupSlug { get; set; } = "general";
        public string GroupName { get; set; } = "General";
        public int GroupOrder { get; set; }
        public int SortOrder { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public MetricType Type { get; set; } = MetricType.Numeric;

        /// <summary>
        /// For numeric/ordinal metrics: true = higher value wins.
        /// For ordinal (statehood): false = lower order wins (earlier = first).
        /// Ignored for text/date where there's no winner.
        /// </summary>
        public bool HigherIsBetter { get; set; } = true;

        /// <summary>Gets the raw numeric value used for comparison (null if not applicable).</summary>
        public Func<State, StateStats?, double?>? GetNumericValue { get; set; }

        /// <summary>Gets the human-readable display string.</summary>
        public Func<State, StateStats?, string?> GetDisplayValue { get; set; } = (_, _) => null;

        /// <summary>Generates a one-sentence summary comparing the two states.</summary>
        public Func<State, StateStats?, State, StateStats?, string>? GenerateSummary { get; set; }
    }
}
