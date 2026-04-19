using System;
using System.Collections.Generic;
using System.Linq;
using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class BirdDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasStoryBlocks, IHasVisualAssets, IHasStoryBlockPlacement
    {
        public BirdContent? BirdContent { get; set; }

        public string? ContentTitle => BirdContent?.Title ?? Symbol?.Name;
        public string? ContentIntroText => BirdContent?.IntroText;
        public override string? Author => BirdContent?.Author;
        public override DateTime? DateModified => BirdContent?.DateModified;
        public int? AdoptedYear => BirdContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? WikidataId => BirdContent?.WikidataId ?? Symbol?.WikidataId;
        public override string? Legislation => BirdContent?.Legislation ?? Symbol?.Legislation;
        public override string? Meaning => BirdContent?.Meaning ?? Symbol?.Meaning;

        public List<IContentSection>? Sections =>
            BirdContent?.Sections?.Cast<IContentSection>().ToList();

        public List<ISource>? Sources =>
            BirdContent?.Sources?.Cast<ISource>().ToList();

        public List<IFaqItem>? Faq =>
            BirdContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;
        public bool HasSharedStates => BirdContent?.SharedStates?.Any() == true;
        public bool HasContent => BirdContent != null && !string.IsNullOrEmpty(BirdContent.HtmlContent);

        public string SymbolTypeName => "State Bird";
        public string SymbolTypeSlug => "bird";
        public string SymbolTypePlural => "birds";
        public string SymbolTypeIcon => "???";

        public SymbolColorScheme Colors => SymbolColorScheme.Blue;

        public override IReadOnlyList<QuickFactItem>? QuickFacts
        {
            get => BirdContent?.QuickFacts;
            set { }
        }

        public BigStatViewModel? BigStat { get; init; }
        public IReadOnlyList<TimelineEventViewModel>? Timeline { get; init; }
        public ExpertQuoteViewModel? ExpertQuote { get; init; }
        public string? BigStatAfterSectionId => BirdContent?.BigStatAfterSectionId;
        public string? TimelineAfterSectionId => BirdContent?.TimelineAfterSectionId;
        public string? ExpertQuoteAfterSectionId => BirdContent?.ExpertQuoteAfterSectionId;

        public IReadOnlyList<VisualAsset>? VisualAssets => BirdContent?.VisualAssets;
    }
}
