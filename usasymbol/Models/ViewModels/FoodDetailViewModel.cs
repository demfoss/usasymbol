using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class FoodDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasVisualAssets
    {
        public FoodContent? FoodContent { get; set; }

        public string? ContentTitle => FirstNonEmpty(FoodContent?.Name, Symbol?.Name);
        public string? ContentIntroText => FoodContent?.IntroText;
        public override string? Author => FoodContent?.Author;
        public override DateTime? DateModified => FoodContent?.DateModified;
        public int? AdoptedYear => FoodContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? Legislation => FoodContent?.Legislation ?? Symbol?.Legislation;

        public List<IContentSection>? Sections => FoodContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => FoodContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => FoodContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Red;

        public string SymbolTypeName { get; set; } = "State Food";
        public string SymbolTypeSlug { get; set; } = "food";
        public string SymbolTypePlural { get; set; } = "foods";
        public string SymbolTypeIcon { get; set; } = "🍽️";
        public string DefaultDesignation { get; set; } = "State Food";
        public string HeroFallbackIconClass { get; set; } = "fa-solid fa-utensils";
        public string OverviewIconClass { get; set; } = "fa-solid fa-utensils";
        public string AssetBasePath { get; set; } = "/images/foods";
        public string EmptySectionsMessage { get; set; } = "No sections rendered yet for this state food.";

        public override IReadOnlyList<QuickFactItem>? QuickFacts
        {
            get => FoodContent?.QuickFacts;
            set { }
        }

        public IReadOnlyList<VisualAsset>? VisualAssets => FoodContent?.VisualAssets;
    }
}
