using USASymbol.Models.ViewModels;
using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class FirearmDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasStoryBlocks, IHasVisualAssets, IHasStoryBlockPlacement
    {
        public FirearmContent? FirearmContent { get; set; }

        public string? ContentTitle => FirstNonEmpty(FirearmContent?.Title, Symbol?.Name);
        public string? ContentIntroText => FirearmContent?.IntroText;
        public override string? Author => FirearmContent?.Author;
        public override DateTime? DateModified => FirearmContent?.DateModified;
        public int? AdoptedYear => FirearmContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public new string? WikidataId => FirearmContent?.WikidataId ?? Symbol?.WikidataId;
        public new string? Legislation => FirearmContent?.Legislation ?? Symbol?.Legislation;
        public new string? Meaning => FirearmContent?.Meaning ?? Symbol?.Meaning;

        public List<IContentSection>? Sections =>
            FirearmContent?.Sections?.Cast<IContentSection>().ToList();

        public List<ISource>? Sources =>
            FirearmContent?.Sources?.Cast<ISource>().ToList();

        public List<IFaqItem>? Faq =>
            FirearmContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Red;

        public BigStatViewModel? BigStat { get; init; }
        public IReadOnlyList<TimelineEventViewModel>? Timeline { get; init; }
        public ExpertQuoteViewModel? ExpertQuote { get; init; }
        public string? BigStatAfterSectionId => FirearmContent?.BigStatAfterSectionId;
        public string? TimelineAfterSectionId => FirearmContent?.TimelineAfterSectionId;
        public string? ExpertQuoteAfterSectionId => FirearmContent?.ExpertQuoteAfterSectionId;

        public string SymbolTypeName => "State Firearm";
        public string SymbolTypeSlug => "firearm";
        public string SymbolTypePlural => "firearms";
        public string SymbolTypeIcon => "🔫";
        public IReadOnlyList<VisualAsset>? VisualAssets => FirearmContent?.VisualAssets;
    }
}
