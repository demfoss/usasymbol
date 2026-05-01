using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class SealDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasStoryBlocks, IHasVisualAssets, IHasStoryBlockPlacement
    {
        public SealContent? SealContent { get; set; }

        public string? ContentTitle => SealContent?.Name ?? Symbol?.Name;
        public string? ContentIntroText => SealContent?.IntroText;
        public override string? Author => SealContent?.Author;
        public override DateTime? DateModified => SealContent?.DateModified;
        public int? AdoptedYear => SealContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? WikidataId => SealContent?.WikidataId ?? Symbol?.WikidataId;
        public override string? Legislation => SealContent?.Legislation ?? Symbol?.Legislation;
        public override string? Meaning => SealContent?.Meaning ?? Symbol?.Meaning;

        public List<IContentSection>? Sections => SealContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => SealContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => SealContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Indigo;

        public string SymbolTypeName => "State Seal";
        public string SymbolTypeSlug => "state-seal";
        public string SymbolTypePlural => "state-seals";
        public string SymbolTypeIcon => "🔖";

        public override IReadOnlyList<QuickFactItem>? QuickFacts
        {
            get => SealContent?.QuickFacts;
            set { }
        }

        public BigStatViewModel? BigStat { get; init; }
        public IReadOnlyList<TimelineEventViewModel>? Timeline { get; init; }
        public ExpertQuoteViewModel? ExpertQuote { get; init; }
        public string? BigStatAfterSectionId => SealContent?.BigStatAfterSectionId;
        public string? TimelineAfterSectionId => SealContent?.TimelineAfterSectionId;
        public string? ExpertQuoteAfterSectionId => SealContent?.ExpertQuoteAfterSectionId;
        public IReadOnlyList<VisualAsset>? VisualAssets => SealContent?.VisualAssets;
    }
}
