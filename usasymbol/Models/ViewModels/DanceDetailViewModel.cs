using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class DanceDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasVisualAssets
    {
        public DanceContent? DanceContent { get; set; }

        public string? ContentTitle => FirstNonEmpty(DanceContent?.Name, Symbol?.Name);
        public string? ContentIntroText => DanceContent?.IntroText;
        public override string? Author => DanceContent?.Author;
        public override DateTime? DateModified => DanceContent?.DateModified;
        public int? AdoptedYear => DanceContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? Legislation => DanceContent?.Legislation ?? Symbol?.Legislation;

        public List<IContentSection>? Sections => DanceContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => DanceContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => DanceContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public string? VideoUrl => DanceContent?.VideoUrl;
        public string? VideoTitle => DanceContent?.VideoTitle;
        public string? VideoCaption => DanceContent?.VideoCaption;

        public SymbolColorScheme Colors => SymbolColorScheme.Pink;

        public string SymbolTypeName { get; set; } = "State Dance";
        public string SymbolTypeSlug { get; set; } = "dance";
        public string SymbolTypePlural { get; set; } = "dances";
        public string SymbolTypeIcon { get; set; } = "💃";
        public string DefaultDesignation { get; set; } = "State Dance";
        public string HeroFallbackIconClass { get; set; } = "fa-solid fa-music";
        public string OverviewIconClass { get; set; } = "fa-solid fa-music";
        public string AssetBasePath { get; set; } = "/images/dances";
        public string EmptySectionsMessage { get; set; } = "No sections rendered yet for this state dance.";

        public override IReadOnlyList<QuickFactItem>? QuickFacts
        {
            get => DanceContent?.QuickFacts;
            set { }
        }

        public IReadOnlyList<VisualAsset>? VisualAssets => DanceContent?.VisualAssets;
    }
}
