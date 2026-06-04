using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class FossilDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel
    {
        public FossilContent? FossilContent { get; set; }

        public string? ContentTitle => FirstNonEmpty(FossilContent?.Name, Symbol?.Name);
        public string? ContentIntroText => FossilContent?.IntroText;
        public override string? Author => FossilContent?.Author;
        public override DateTime? DateModified => FossilContent?.DateModified;
        public int? AdoptedYear => FossilContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? Legislation => Symbol?.Legislation;
        public override string? Meaning => null;

        public List<IContentSection>? Sections => FossilContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => FossilContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => FossilContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Amber;

        public string SymbolTypeName { get; set; } = "State Fossil";
        public string SymbolTypeSlug { get; set; } = "fossil";
        public string SymbolTypePlural { get; set; } = "fossils";
        public string SymbolTypeIcon { get; set; } = "🦴";
        public string DefaultDesignation { get; set; } = "State Fossil";
        public string HeroFallbackIconClass { get; set; } = "fa-solid fa-bone";
        public string OverviewIconClass { get; set; } = "fa-solid fa-bone";
        public string AssetBasePath { get; set; } = "/images/fossils";
        public string EmptySectionsMessage { get; set; } = "No sections rendered yet for this state fossil.";

        public override IReadOnlyList<QuickFactItem>? QuickFacts
        {
            get => FossilContent?.QuickFacts;
            set { }
        }
    }
}
