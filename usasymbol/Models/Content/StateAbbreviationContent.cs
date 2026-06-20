namespace USASymbol.Models.Content
{
    public class StateAbbreviationContent
    {
        public string Type { get; set; } = string.Empty;
        public string StateSlug { get; set; } = string.Empty;
        public string TraditionalAbbreviation { get; set; } = string.Empty;
        public string FormationSummary { get; set; } = string.Empty;
        public string FormationDetail { get; set; } = string.Empty;
        public string SearchIntentSummary { get; set; } = string.Empty;
        public string SimilarCodesSummary { get; set; } = string.Empty;
        public string CapitalZip { get; set; } = string.Empty;
        public string ApStyleNote { get; set; } = string.Empty;
        public string ConfusionWarning { get; set; } = string.Empty;
        public string StateSpecificTitle { get; set; } = string.Empty;
        public List<string> StateSpecificParagraphs { get; set; } = new();
    }

    public class StateAbbreviationIndexContent
    {
        public List<string> HighIntentSlugs { get; set; } = new();
    }
}
