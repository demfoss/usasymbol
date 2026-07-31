using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    // Shared view model for State Mineral, State Rock (or Stone), and State Gemstone pages.
    // SymbolTypeName/Slug/Plural/Icon/etc are overridden per designation in SymbolController,
    // the same reuse pattern SealDetailViewModel uses for State Seal vs Coat of Arms.
    public class MineralDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasVisualAssets
    {
        public MineralContent? MineralContent { get; set; }

        public string? ContentTitle => FirstNonEmpty(MineralContent?.Name, Symbol?.Name);
        public string? ContentIntroText => MineralContent?.IntroText;
        public override string? Author => MineralContent?.Author;
        public override DateTime? DateModified => MineralContent?.DateModified;
        public int? AdoptedYear => MineralContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? Legislation => MineralContent?.Legislation ?? Symbol?.Legislation;
        public override string? Meaning => null;

        public List<IContentSection>? Sections => MineralContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => MineralContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => MineralContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Slate;

        public string SymbolTypeName { get; set; } = "State Mineral";
        public string SymbolTypeSlug { get; set; } = "mineral";
        public string SymbolTypePlural { get; set; } = "minerals";
        public string SymbolTypeIcon { get; set; } = "💎";
        public string DefaultDesignation { get; set; } = "State Mineral";
        public string HeroFallbackIconClass { get; set; } = "fa-solid fa-gem";
        public string OverviewIconClass { get; set; } = "fa-solid fa-gem";
        public string AssetBasePath { get; set; } = "/images/minerals";
        public string EmptySectionsMessage { get; set; } = "No sections rendered yet for this state mineral.";

        public override IReadOnlyList<QuickFactItem>? QuickFacts
        {
            get => MineralContent?.QuickFacts;
            set { }
        }

        public IReadOnlyList<VisualAsset>? VisualAssets => MineralContent?.VisualAssets;
    }
}
