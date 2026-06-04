using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class SealDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasStoryBlocks, IHasVisualAssets, IHasStoryBlockPlacement
    {
        public SealContent? SealContent { get; set; }

        public string? ContentTitle => FirstNonEmpty(SealContent?.Name, Symbol?.Name);
        public string? ContentIntroText => SealContent?.IntroText;
        public override string? Author => SealContent?.Author;
        public override DateTime? DateModified => SealContent?.DateModified;
        public int? AdoptedYear => SealContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? WikidataId => SealContent?.WikidataId ?? Symbol?.WikidataId;
        public override string? Legislation => SealContent?.Legislation ?? Symbol?.Legislation;
        public override string? Meaning => SealContent?.Meaning ?? Symbol?.Meaning;
        public string? MeaningAfterSectionId => SealContent?.MeaningAfterSectionId;
        public string? MeaningTitle => SealContent?.MeaningTitle;

        public List<IContentSection>? Sections => SealContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => SealContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => SealContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Indigo;

        public string SymbolTypeName { get; set; } = "State Seal";
        public string SymbolTypeSlug { get; set; } = "state-seal";
        public string SymbolTypePlural { get; set; } = "state-seals";
        public string SymbolTypeIcon { get; set; } = "🔖";
        public string DefaultDesignation { get; set; } = "State Seal";
        public string HeroFallbackIconClass { get; set; } = "fa-solid fa-stamp";
        public string OverviewIconClass { get; set; } = "fa-solid fa-stamp";
        public string AssetBasePath { get; set; } = "/images/state-seals";
        public string EmptySectionsMessage { get; set; } = "No sections rendered yet for this state symbol.";
        public bool ShowQuizPromo { get; set; } = true;
        public string QuizPromoId { get; set; } = "seals-quiz-promo";
        public string QuizPromoTitle { get; set; } = "Can You Identify All 50 State Seals?";
        public string QuizPromoSubtitle { get; set; } = "See a seal, pick the right state. Harder than it looks.";
        public string QuizPromoBody { get; set; } = "Most state seals share similar imagery — eagles, shields, agriculture, and Latin mottos. Telling them apart requires spotting the small details: a specific figure, a founding year, an unusual animal. The State Seals Quiz covers all 50 and shuffles both the questions and answer positions every round.";
        public string QuizPromoUrl { get; set; } = "/quizzes/state-seals-quiz";
        public string QuizPromoCta { get; set; } = "Take the State Seals Quiz";

        public override IReadOnlyList<QuickFactItem>? QuickFacts
        {
            get => SealContent?.QuickFacts;
            set { }
        }

        public BigStatViewModel? BigStat { get; init; }
        public IReadOnlyList<TimelineEventViewModel>? Timeline { get; init; }
        public ExpertQuoteViewModel? ExpertQuote { get; init; }
        public string? BigStatAfterSectionId => SealContent?.BigStatAfterSectionId;
        public string? TimelineAfterSectionId => SealContent?.TimelineAfterSectionId;
        public string? ExpertQuoteAfterSectionId => SealContent?.ExpertQuoteAfterSectionId;
        public IReadOnlyList<VisualAsset>? VisualAssets => SealContent?.VisualAssets;
    }
}
