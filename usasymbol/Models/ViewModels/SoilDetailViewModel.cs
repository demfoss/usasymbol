using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class SoilDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasStoryBlocks, IHasVisualAssets, IHasStoryBlockPlacement
    {
        public SoilContent? SoilContent { get; set; }

        public string? ContentTitle => FirstNonEmpty(SoilContent?.Name, Symbol?.Name);
        public string? ContentIntroText => SoilContent?.IntroText;
        public override string? Author => SoilContent?.Author;
        public override DateTime? DateModified => SoilContent?.DateModified;
        public int? AdoptedYear => SoilContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? Legislation => SoilContent?.Legislation ?? Symbol?.Legislation;

        public List<IContentSection>? Sections => SoilContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => SoilContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => SoilContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Indigo;

        public string SymbolTypeName { get; set; } = "State Soil";
        public string SymbolTypeSlug { get; set; } = "soil";
        public string SymbolTypePlural { get; set; } = "soils";
        public string SymbolTypeIcon { get; set; } = "🌱";
        public string DefaultDesignation { get; set; } = "State Soil";
        public string HeroFallbackIconClass { get; set; } = "fa-solid fa-mountain";
        public string OverviewIconClass { get; set; } = "fa-solid fa-mountain";
        public string AssetBasePath { get; set; } = "/images/soils";
        public string EmptySectionsMessage { get; set; } = "No sections rendered yet for this state soil.";
        public bool ShowQuizPromo { get; set; } = false;

        public override IReadOnlyList<QuickFactItem>? QuickFacts
        {
            get => SoilContent?.QuickFacts;
            set { }
        }

        public BigStatViewModel? BigStat { get; init; }
        public ExpertQuoteViewModel? ExpertQuote { get; init; }
        public string? BigStatAfterSectionId => SoilContent?.BigStatAfterSectionId;
        public string? TimelineAfterSectionId => null;
        public string? ExpertQuoteAfterSectionId => SoilContent?.ExpertQuoteAfterSectionId;
        public IReadOnlyList<VisualAsset>? VisualAssets => SoilContent?.VisualAssets;

        // Not applicable to soil
        public IReadOnlyList<TimelineEventViewModel>? Timeline => null;
    }
}
