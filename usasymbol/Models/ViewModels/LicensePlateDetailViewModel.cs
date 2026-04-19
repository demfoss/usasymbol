using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class LicensePlateDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasStoryBlocks, IHasVisualAssets, IHasStoryBlockPlacement
    {
        public LicensePlateContent? LicensePlateContent { get; set; }

        public string? ContentTitle => LicensePlateContent?.Title ?? Symbol?.Name;
        public string? ContentIntroText => LicensePlateContent?.IntroText;
        public override string? Author => LicensePlateContent?.Author;
        public override DateTime? DateModified => LicensePlateContent?.DateModified;
        public int? AdoptedYear => LicensePlateContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? WikidataId => LicensePlateContent?.WikidataId ?? Symbol?.WikidataId;
        public override string? Legislation => LicensePlateContent?.Legislation ?? Symbol?.Legislation;
        public override string? Meaning => LicensePlateContent?.Meaning ?? Symbol?.Meaning;

        public List<IContentSection>? Sections => LicensePlateContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => LicensePlateContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => LicensePlateContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Cyan;

        public string SymbolTypeName => "License Plate Slogan";
        public string SymbolTypeSlug => "license-plate";
        public string SymbolTypePlural => "license-plate-slogans";
        public string SymbolTypeIcon => "🚗";

        public override IReadOnlyList<QuickFactItem>? QuickFacts
        {
            get => LicensePlateContent?.QuickFacts;
            set { }
        }

        public BigStatViewModel? BigStat { get; init; }
        public IReadOnlyList<TimelineEventViewModel>? Timeline { get; init; }
        public ExpertQuoteViewModel? ExpertQuote { get; init; }
        public string? BigStatAfterSectionId => LicensePlateContent?.BigStatAfterSectionId;
        public string? TimelineAfterSectionId => LicensePlateContent?.TimelineAfterSectionId;
        public string? ExpertQuoteAfterSectionId => LicensePlateContent?.ExpertQuoteAfterSectionId;
        public IReadOnlyList<VisualAsset>? VisualAssets => LicensePlateContent?.VisualAssets;
    }
}
