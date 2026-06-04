using System;
using System.Collections.Generic;
using System.Linq;
using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class FlowerDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasStoryBlocks, IHasVisualAssets, IHasStoryBlockPlacement
    {
        public FlowerContent? FlowerContent { get; set; }

        public string? ContentTitle => FirstNonEmpty(FlowerContent?.Name, Symbol?.Name);
        public string? ContentIntroText => FlowerContent?.IntroText;
        public override string? Author => FlowerContent?.Author;
        public override DateTime? DateModified => FlowerContent?.DateModified;
        public int? AdoptedYear => FlowerContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? WikidataId => FlowerContent?.WikidataId ?? Symbol?.WikidataId;
        public override string? Legislation => FlowerContent?.Legislation ?? Symbol?.Legislation;
        public override string? Meaning => FlowerContent?.Meaning ?? Symbol?.Meaning;

        public List<IContentSection>? Sections => FlowerContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => FlowerContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => FlowerContent?.Faq?.Cast<IFaqItem>().ToList();

        bool ISymbolDetailViewModel.HasSections => Sections?.Any() == true;
        bool ISymbolDetailViewModel.HasSources => Sources?.Any() == true;
        bool ISymbolDetailViewModel.HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Pink;

        public string SymbolTypeName => "State Flower";
        public string SymbolTypeSlug => "flower";
        public string SymbolTypePlural => "flowers";
        public string SymbolTypeIcon => "??";

        public bool HasContent => FlowerContent != null && !string.IsNullOrEmpty(FlowerContent.HtmlContent);
        public bool HasSources => FlowerContent?.Sources?.Any() == true;
        public bool HasSections => FlowerContent?.Sections?.Any() == true;
        public bool HasFaq => FlowerContent?.Faq?.Any() == true;
        public bool HasIntro => !string.IsNullOrEmpty(FlowerContent?.IntroText);

        public override IReadOnlyList<QuickFactItem>? QuickFacts
        {
            get => FlowerContent?.QuickFacts;
            set { }
        }

        public BigStatViewModel? BigStat { get; init; }
        public IReadOnlyList<TimelineEventViewModel>? Timeline { get; init; }
        public ExpertQuoteViewModel? ExpertQuote { get; init; }
        public string? BigStatAfterSectionId => FlowerContent?.BigStatAfterSectionId;
        public string? TimelineAfterSectionId => FlowerContent?.TimelineAfterSectionId;
        public string? ExpertQuoteAfterSectionId => FlowerContent?.ExpertQuoteAfterSectionId;
        public IReadOnlyList<VisualAsset>? VisualAssets => FlowerContent?.VisualAssets;
    }
}
