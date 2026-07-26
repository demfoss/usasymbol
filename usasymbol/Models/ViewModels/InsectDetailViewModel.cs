using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class InsectDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasVisualAssets
    {
        public InsectContent? InsectContent { get; set; }

        public string? ContentTitle => FirstNonEmpty(InsectContent?.Name, Symbol?.Name);
        public string? ContentIntroText => InsectContent?.IntroText;
        public override string? Author => InsectContent?.Author;
        public override DateTime? DateModified => InsectContent?.DateModified;
        public int? AdoptedYear => InsectContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? Legislation => InsectContent?.Legislation ?? Symbol?.Legislation;

        public List<IContentSection>? Sections => InsectContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => InsectContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => InsectContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Green;

        public string SymbolTypeName { get; set; } = "State Insect";
        public string SymbolTypeSlug { get; set; } = "insect";
        public string SymbolTypePlural { get; set; } = "insects";
        public string SymbolTypeIcon { get; set; } = "🦋";
        public string DefaultDesignation { get; set; } = "State Insect";
        public string HeroFallbackIconClass { get; set; } = "fa-solid fa-bug";
        public string OverviewIconClass { get; set; } = "fa-solid fa-bug";
        public string AssetBasePath { get; set; } = "/images/insects";
        public string EmptySectionsMessage { get; set; } = "No sections rendered yet for this state insect.";

        public override IReadOnlyList<QuickFactItem>? QuickFacts
        {
            get => InsectContent?.QuickFacts;
            set { }
        }

        public IReadOnlyList<VisualAsset>? VisualAssets => InsectContent?.VisualAssets;
    }
}
