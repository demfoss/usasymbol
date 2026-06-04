using USASymbol.Models;
using USASymbol.Models.ViewModels;
using USASymbol.Models.Content;
using System.Collections.Generic;
using System.Linq;

namespace USASymbol.Models.ViewModels
{
    public class ColorDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasStoryBlocks, IHasVisualAssets, IHasStoryBlockPlacement
    {
        public ColorContent? ColorContent { get; set; }

        public string? ContentTitle => FirstNonEmpty(ColorContent?.Title, Symbol?.Name);
        public string? ContentIntroText => ColorContent?.IntroText;
        public override string? Author => ColorContent?.Author;
        public override DateTime? DateModified => ColorContent?.DateModified;
        public int? AdoptedYear => ColorContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? WikidataId => ColorContent?.WikidataId ?? Symbol?.WikidataId;
        public override string? Legislation => ColorContent?.Legislation ?? Symbol?.Legislation;
        public override string? Meaning => ColorContent?.Meaning ?? Symbol?.Meaning;

        public List<IContentSection>? Sections =>
            ColorContent?.Sections?.Cast<IContentSection>().ToList();

        public List<ISource>? Sources =>
            ColorContent?.Sources?.Cast<ISource>().ToList();

        public List<IFaqItem>? Faq =>
            ColorContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;


        public bool HasColorData => ColorContent?.ColorData?.Any() == true;


        public SymbolColorScheme Colors => SymbolColorScheme.Blue;

        public string SymbolTypeName => "State Colors";
        public string SymbolTypeSlug => "color";
        public string SymbolTypePlural => "colors";
        public string SymbolTypeIcon => "??";

        public bool HasContent => ColorContent != null && !string.IsNullOrEmpty(ColorContent.HtmlContent);


        public bool HasQuickFacts => !string.IsNullOrEmpty(ColorContent?.OfficialColors) ||
                                     !string.IsNullOrEmpty(ColorContent?.PrimaryUse) ||
                                     !string.IsNullOrEmpty(ColorContent?.KnownFor);

        public BigStatViewModel? BigStat { get; init; }
        public IReadOnlyList<TimelineEventViewModel>? Timeline { get; init; }
        public ExpertQuoteViewModel? ExpertQuote { get; init; }
        public string? BigStatAfterSectionId => ColorContent?.BigStatAfterSectionId;
        public string? TimelineAfterSectionId => ColorContent?.TimelineAfterSectionId;
        public string? ExpertQuoteAfterSectionId => ColorContent?.ExpertQuoteAfterSectionId;

        public List<string>? QuickSummary => ColorContent?.QuickSummary;
        public string? DidYouKnow => ColorContent?.DidYouKnow;
        public IReadOnlyList<VisualAsset>? VisualAssets => ColorContent?.VisualAssets;
    }
}
