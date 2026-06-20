using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class SportDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasVisualAssets
    {
        public SportContent? SportContent { get; set; }

        public string? ContentTitle => FirstNonEmpty(SportContent?.Name, Symbol?.Name);
        public string? ContentIntroText => SportContent?.IntroText;
        public override string? Author => SportContent?.Author;
        public override DateTime? DateModified => SportContent?.DateModified;
        public int? AdoptedYear => SportContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? Legislation => SportContent?.Legislation ?? Symbol?.Legislation;

        public List<IContentSection>? Sections => SportContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => SportContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => SportContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Amber;

        public string SymbolTypeName { get; set; } = "State Sport";
        public string SymbolTypeSlug { get; set; } = "sport";
        public string SymbolTypePlural { get; set; } = "sports";
        public string SymbolTypeIcon { get; set; } = "🏅";
        public string DefaultDesignation { get; set; } = "State Sport";
        public string HeroFallbackIconClass { get; set; } = "fa-solid fa-medal";
        public string OverviewIconClass { get; set; } = "fa-solid fa-medal";
        public string AssetBasePath { get; set; } = "/images/sports";
        public string EmptySectionsMessage { get; set; } = "No sections rendered yet for this state sport.";

        public override IReadOnlyList<QuickFactItem>? QuickFacts
        {
            get => SportContent?.QuickFacts;
            set { }
        }

        public IReadOnlyList<VisualAsset>? VisualAssets => SportContent?.VisualAssets;
    }
}
