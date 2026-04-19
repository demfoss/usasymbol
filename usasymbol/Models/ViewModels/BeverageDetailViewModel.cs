using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class BeverageDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasStoryBlocks, IHasVisualAssets, IHasStoryBlockPlacement
    {
        public BeverageContent? BeverageContent { get; set; }

        public string? ContentTitle => BeverageContent?.Title ?? Symbol?.Name;
        public string? ContentIntroText => BeverageContent?.IntroText;
        public override string? Author => BeverageContent?.Author;
        public override DateTime? DateModified => BeverageContent?.DateModified;
        public int? AdoptedYear => BeverageContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? WikidataId => BeverageContent?.WikidataId ?? Symbol?.WikidataId;
        public override string? Legislation => BeverageContent?.Legislation ?? Symbol?.Legislation;
        public override string? Meaning => BeverageContent?.Meaning ?? Symbol?.Meaning;

        public List<IContentSection>? Sections => BeverageContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => BeverageContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => BeverageContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Orange;

        public string SymbolTypeName => "State Beverage";
        public string SymbolTypeSlug => "beverage";
        public string SymbolTypePlural => "beverages";
        public string SymbolTypeIcon => "🥤";

        public override IReadOnlyList<QuickFactItem>? QuickFacts
        {
            get => BeverageContent?.QuickFacts;
            set { }
        }

        public BigStatViewModel? BigStat { get; init; }
        public IReadOnlyList<TimelineEventViewModel>? Timeline { get; init; }
        public ExpertQuoteViewModel? ExpertQuote { get; init; }
        public string? BigStatAfterSectionId => BeverageContent?.BigStatAfterSectionId;
        public string? TimelineAfterSectionId => BeverageContent?.TimelineAfterSectionId;
        public string? ExpertQuoteAfterSectionId => BeverageContent?.ExpertQuoteAfterSectionId;
        public IReadOnlyList<VisualAsset>? VisualAssets => BeverageContent?.VisualAssets;
    }
}
