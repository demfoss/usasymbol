using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public interface IParkDetailViewModel
    {
        ParkContent Park { get; }
        IReadOnlyList<QuickFactItem>? QuickFacts { get; }
        bool HasFaq { get; }
        bool HasSources { get; }
        SymbolColorScheme Colors { get; }
    }

    public class ParkDetailViewModel : IParkDetailViewModel
    {
        public ParkContent Park { get; set; } = new();

        public IReadOnlyList<QuickFactItem>? QuickFacts => Park.QuickFacts.Count > 0 ? Park.QuickFacts : null;

        public bool HasFaq => Park.Faq.Count > 0;
        public bool HasSources => Park.Sources.Count > 0;

        public SymbolColorScheme Colors => SymbolColorScheme.Green;

        // Rank computed by OverallScore among all ranked parks
        public int ScoreRank { get; set; }
        public int TotalRanked { get; set; }
    }
}
