using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class FishDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasVisualAssets
    {
        public FishContent? FishContent { get; set; }

        public string? ContentTitle => FirstNonEmpty(FishContent?.Name, Symbol?.Name);
        public string? ContentIntroText => FishContent?.IntroText;
        public override string? Author => FishContent?.Author;
        public override DateTime? DateModified => FishContent?.DateModified;
        public int? AdoptedYear => FishContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? Legislation => FishContent?.Legislation ?? Symbol?.Legislation;

        public List<IContentSection>? Sections => FishContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => FishContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => FishContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Cyan;

        public string SymbolTypeName { get; set; } = "State Fish";
        public string SymbolTypeSlug { get; set; } = "fish";
        public string SymbolTypePlural { get; set; } = "fish";
        public string SymbolTypeIcon { get; set; } = "🐸";
        public string DefaultDesignation { get; set; } = "State Fish";
        public string HeroFallbackIconClass { get; set; } = "fa-solid fa-frog";
        public string OverviewIconClass { get; set; } = "fa-solid fa-frog";
        public string AssetBasePath { get; set; } = "/images/fish";
        public string EmptySectionsMessage { get; set; } = "No sections rendered yet for this state fish.";

        public override IReadOnlyList<QuickFactItem>? QuickFacts
        {
            get => FishContent?.QuickFacts;
            set { }
        }

        public IReadOnlyList<VisualAsset>? VisualAssets => FishContent?.VisualAssets;
    }
}
