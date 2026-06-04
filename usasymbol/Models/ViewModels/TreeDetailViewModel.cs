using System;
using System.Collections.Generic;
using System.Linq;
using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class TreeDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasStoryBlocks, IHasVisualAssets, IHasStoryBlockPlacement
    {
        public TreeContent? TreeContent { get; set; }


        public string? ContentTitle => FirstNonEmpty(TreeContent?.Name, Symbol?.Name);
        public string? ContentIntroText => TreeContent?.IntroText;
        public override string? Author => TreeContent?.Author;
        public override DateTime? DateModified => TreeContent?.DateModified;
        public int? AdoptedYear => TreeContent?.AdoptedYear ?? Symbol?.AdoptedYear;


        public override string? WikidataId => TreeContent?.WikidataId ?? Symbol?.WikidataId;
        public override string? Legislation => TreeContent?.Legislation ?? Symbol?.Legislation;
        public override string? Meaning => TreeContent?.Meaning ?? Symbol?.Meaning;

        public List<IContentSection>? Sections => TreeContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => TreeContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => TreeContent?.Faq?.Cast<IFaqItem>().ToList();

        bool ISymbolDetailViewModel.HasSections => Sections?.Any() == true;
        bool ISymbolDetailViewModel.HasSources => Sources?.Any() == true;
        bool ISymbolDetailViewModel.HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Green;

        public string SymbolTypeName => "State Tree";
        public string SymbolTypeSlug => "tree";
        public string SymbolTypePlural => "trees";
        public string SymbolTypeIcon => "??";


        public bool HasContent => TreeContent != null && !string.IsNullOrEmpty(TreeContent.HtmlContent);
        public bool HasSources => TreeContent?.Sources?.Any() == true;
        public bool HasSections => TreeContent?.Sections?.Any() == true;
        public bool HasFaq => TreeContent?.Faq?.Any() == true;
        public bool HasIntro => !string.IsNullOrEmpty(TreeContent?.IntroText);
        public override IReadOnlyList<QuickFactItem>? QuickFacts
        {
            get => TreeContent?.QuickFacts;
            set { }
        }
        public BigStatViewModel? BigStat { get; init; }
        public IReadOnlyList<TimelineEventViewModel>? Timeline { get; init; }
        public ExpertQuoteViewModel? ExpertQuote { get; init; }
        public string? BigStatAfterSectionId => TreeContent?.BigStatAfterSectionId;
        public string? TimelineAfterSectionId => TreeContent?.TimelineAfterSectionId;
        public string? ExpertQuoteAfterSectionId => TreeContent?.ExpertQuoteAfterSectionId;
        public IReadOnlyList<VisualAsset>? VisualAssets => TreeContent?.VisualAssets;
    }
}
