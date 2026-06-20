using USASymbol.Models;

namespace USASymbol.Models.ViewModels
{
    public class StateAbbreviationGuideViewModel : IStateScopedViewModel
    {
        public State State { get; set; } = new();
        public string TraditionalAbbreviation { get; set; } = string.Empty;
        public string FormationSummary { get; set; } = string.Empty;
        public string FormationDetail { get; set; } = string.Empty;
        public string SearchIntentSummary { get; set; } = string.Empty;
        public string SimilarCodesSummary { get; set; } = string.Empty;
        public string CapitalZip { get; set; } = string.Empty;
        public string ApStyleNote { get; set; } = string.Empty;
        public string ConfusionWarning { get; set; } = string.Empty;
        public string? StateSpecificTitle { get; set; }
        public List<string> StateSpecificParagraphs { get; set; } = new();
        public List<State> SimilarStates { get; set; } = new();
        public List<State> RegionalStates { get; set; } = new();
        public List<State> AlphabeticalNeighbors { get; set; } = new();
    }
}
