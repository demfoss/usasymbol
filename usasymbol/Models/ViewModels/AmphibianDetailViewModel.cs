using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class AmphibianDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasVisualAssets
    {
        public AmphibianContent? AmphibianContent { get; set; }

        public string? ContentTitle => FirstNonEmpty(AmphibianContent?.Name, Symbol?.Name);
        public string? ContentIntroText => AmphibianContent?.IntroText;
        public override string? Author => AmphibianContent?.Author;
        public override DateTime? DateModified => AmphibianContent?.DateModified;
        public int? AdoptedYear => AmphibianContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? Legislation => AmphibianContent?.Legislation ?? Symbol?.Legislation;

        public List<IContentSection>? Sections => AmphibianContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => AmphibianContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => AmphibianContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Cyan;

        public string SymbolTypeName { get; set; } = "State Amphibian";
        public string SymbolTypeSlug { get; set; } = "amphibian";
        public string SymbolTypePlural { get; set; } = "amphibians";
        public string SymbolTypeIcon { get; set; } = "🐸";
        public string DefaultDesignation { get; set; } = "State Amphibian";
        public string HeroFallbackIconClass { get; set; } = "fa-solid fa-frog";
        public string OverviewIconClass { get; set; } = "fa-solid fa-frog";
        public string AssetBasePath { get; set; } = "/images/amphibians";
        public string EmptySectionsMessage { get; set; } = "No sections rendered yet for this state amphibian.";

        public override IReadOnlyList<QuickFactItem>? QuickFacts
        {
            get => AmphibianContent?.QuickFacts;
            set { }
        }

        public IReadOnlyList<VisualAsset>? VisualAssets => AmphibianContent?.VisualAssets;
    }
}
