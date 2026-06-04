using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class DinosaurDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasStoryBlocks, IHasVisualAssets, IHasStoryBlockPlacement
    {
        public DinosaurContent? DinosaurContent { get; set; }

        public string? ContentTitle => FirstNonEmpty(DinosaurContent?.Title, Symbol?.Name);
        public string? ContentIntroText => DinosaurContent?.IntroText;
        public override string? Author => DinosaurContent?.Author;
        public override DateTime? DateModified => DinosaurContent?.DateModified;
        public int? AdoptedYear => DinosaurContent?.AdoptedYear ?? Symbol?.AdoptedYear;
        public new string? WikidataId => DinosaurContent?.WikidataId ?? Symbol?.WikidataId;
        public new string? Legislation => DinosaurContent?.Legislation ?? Symbol?.Legislation;
        public new string? Meaning => DinosaurContent?.Meaning ?? Symbol?.Meaning;

        public List<IContentSection>? Sections => DinosaurContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => DinosaurContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => DinosaurContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Amber;
        public BigStatViewModel? BigStat { get; init; }
        public IReadOnlyList<TimelineEventViewModel>? Timeline { get; init; }
        public ExpertQuoteViewModel? ExpertQuote { get; init; }
        public string? BigStatAfterSectionId => DinosaurContent?.BigStatAfterSectionId;
        public string? TimelineAfterSectionId => DinosaurContent?.TimelineAfterSectionId;
        public string? ExpertQuoteAfterSectionId => DinosaurContent?.ExpertQuoteAfterSectionId;
        public string SymbolTypeName => "State Dinosaur";
        public string SymbolTypeSlug => "dinosaur";
        public string SymbolTypePlural => "dinosaurs";
        public string SymbolTypeIcon => "🦖";
        public IReadOnlyList<VisualAsset>? VisualAssets => DinosaurContent?.VisualAssets;
    }
}
