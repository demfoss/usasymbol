using USASymbol.Models;
using USASymbol.Models.ViewModels;
using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class MammalDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasStoryBlocks, IHasVisualAssets, IHasStoryBlockPlacement
    {
        public MammalContent? MammalContent { get; set; }

        public string? ContentTitle => MammalContent?.Title ?? Symbol?.Name;
        public string? ContentIntroText => MammalContent?.IntroText;
        public override string? Author => MammalContent?.Author;
        public override DateTime? DateModified => MammalContent?.DateModified;
        public int? AdoptedYear => MammalContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? WikidataId => MammalContent?.WikidataId ?? Symbol?.WikidataId;
        public override string? Legislation => MammalContent?.Legislation ?? Symbol?.Legislation;
        public override string? Meaning => MammalContent?.Meaning ?? Symbol?.Meaning;

        public List<IContentSection>? Sections =>
            MammalContent?.Sections?.Cast<IContentSection>().ToList();

        public List<ISource>? Sources =>
            MammalContent?.Sources?.Cast<ISource>().ToList();

        public List<IFaqItem>? Faq =>
            MammalContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Green;

        public string SymbolTypeName => "State Mammal";
        public string SymbolTypeSlug => "mammal";
        public string SymbolTypePlural => "mammals";
        public string SymbolTypeIcon => "??";


        public bool HasContent => MammalContent != null && !string.IsNullOrEmpty(MammalContent.HtmlContent);

        public bool HasQuickFacts => !string.IsNullOrEmpty(MammalContent?.Status) ||
                                     !string.IsNullOrEmpty(MammalContent?.HabitatInState) ||
                                     !string.IsNullOrEmpty(MammalContent?.KnownFor);

        public bool HasPhysicalData => !string.IsNullOrEmpty(MammalContent?.Size) ||
                                       !string.IsNullOrEmpty(MammalContent?.Weight) ||
                                       !string.IsNullOrEmpty(MammalContent?.Coloration);

        public bool HasConservationInfo => !string.IsNullOrEmpty(MammalContent?.ConservationStatus);

        public bool IsExtinct => MammalContent?.Status?.ToLower().Contains("extinct") == true;

        public bool IsEndangered => MammalContent?.Status?.ToLower().Contains("endangered") == true;

        public BigStatViewModel? BigStat { get; init; }
        public IReadOnlyList<TimelineEventViewModel>? Timeline { get; init; }
        public ExpertQuoteViewModel? ExpertQuote { get; init; }
        public string? BigStatAfterSectionId => MammalContent?.BigStatAfterSectionId;
        public string? TimelineAfterSectionId => MammalContent?.TimelineAfterSectionId;
        public string? ExpertQuoteAfterSectionId => MammalContent?.ExpertQuoteAfterSectionId;

        public List<string>? QuickSummary => MammalContent?.QuickSummary;
        public string? DidYouKnow => MammalContent?.DidYouKnow;
        public IReadOnlyList<VisualAsset>? VisualAssets => MammalContent?.VisualAssets;
    }
}
