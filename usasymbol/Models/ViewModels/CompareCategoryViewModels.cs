namespace USASymbol.Models.ViewModels
{
    public class CompareCategoryHubViewModel
    {
        public string CategorySlug { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string Description { get; set; } = "";
        public List<State> States { get; set; } = new();
        public TableSectionViewModel StatesTable { get; set; } = null!;
        public List<CategoryExtreme> Highlights { get; set; } = new();
        public List<(string Slug1, string Name1, string Slug2, string Name2)> FeaturedPairs { get; set; } = new();

        /// <summary>Overall win/loss score for the first featured pair, shown as a "Featured Comparison" card. Null if it couldn't be computed.</summary>
        public USASymbol.Services.ComparisonScoreResult? FeaturedComparison { get; set; }

        /// <summary>Matching Rankings category, if one exists (see ComparisonCategoryCopy.RankingsCategoryMap).</summary>
        public string? RankingsCategoryId { get; set; }
        public string? RankingsCategoryTitle { get; set; }
        public int RankingsItemCount { get; set; }
    }

    /// <summary>Best/worst state for one metric within a category, shown as a highlight chip.</summary>
    public class CategoryExtreme
    {
        public string MetricName { get; set; } = "";
        public string Icon { get; set; } = "";
        public bool HigherIsBetter { get; set; } = true;
        public string BestStateName { get; set; } = "";
        public string BestStateSlug { get; set; } = "";
        public string? BestValue { get; set; }
        public string WorstStateName { get; set; } = "";
        public string WorstStateSlug { get; set; } = "";
        public string? WorstValue { get; set; }
    }

    public class CompareCategoryPairViewModel
    {
        public string CategorySlug { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public StatePairComparisonViewModel Pair { get; set; } = null!;

        /// <summary>MetricResults from Pair, pre-filtered to this category.</summary>
        public List<MetricComparisonResult> CategoryResults { get; set; } = new();
        public List<CategorySpotlightCard> Spotlights { get; set; } = new();
    }

    public class CategorySpotlightCard
    {
        public string Icon { get; set; } = "";
        public string Eyebrow { get; set; } = "";
        public string Body { get; set; } = "";
        public string Link { get; set; } = "";
    }
}
