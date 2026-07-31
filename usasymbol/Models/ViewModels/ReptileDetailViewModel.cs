using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class ReptileDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasVisualAssets
    {
        public ReptileContent? ReptileContent { get; set; }

        public string? ContentTitle => FirstNonEmpty(ReptileContent?.Name, Symbol?.Name);
        public string? ContentIntroText => ReptileContent?.IntroText;
        public override string? Author => ReptileContent?.Author;
        public override DateTime? DateModified => ReptileContent?.DateModified;
        public int? AdoptedYear => ReptileContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? Legislation => ReptileContent?.Legislation ?? Symbol?.Legislation;

        public List<IContentSection>? Sections => ReptileContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => ReptileContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => ReptileContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Orange;

        public string SymbolTypeName { get; set; } = "State Reptile";
        public string SymbolTypeSlug { get; set; } = "reptile";
        public string SymbolTypePlural { get; set; } = "reptiles";
        public string SymbolTypeIcon { get; set; } = "🐢";
        public string DefaultDesignation { get; set; } = "State Reptile";
        public string HeroFallbackIconClass { get; set; } = "fa-solid fa-turtle";
        public string OverviewIconClass { get; set; } = "fa-solid fa-turtle";
        public string AssetBasePath { get; set; } = "/images/reptiles";
        public string EmptySectionsMessage { get; set; } = "No sections rendered yet for this state reptile.";

        public override IReadOnlyList<QuickFactItem>? QuickFacts
        {
            get => ReptileContent?.QuickFacts;
            set { }
        }

        public IReadOnlyList<VisualAsset>? VisualAssets => ReptileContent?.VisualAssets;
    }
}
