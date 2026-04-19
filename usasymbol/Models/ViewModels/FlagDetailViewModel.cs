using System;
using System.Collections.Generic;
using System.Linq;
using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class FlagDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasStoryBlocks, IHasVisualAssets, IHasStoryBlockPlacement
    {
        public FlagContent? FlagContent { get; set; }


        public string? ContentTitle => FlagContent?.Name ?? Symbol?.Name;
        public string? ContentIntroText => FlagContent?.IntroText;
        public override string? Author => FlagContent?.Author;
        public override DateTime? DateModified => FlagContent?.DateModified;
        public int? AdoptedYear => FlagContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? WikidataId => FlagContent?.WikidataId ?? Symbol?.WikidataId;
        public override string? Legislation => FlagContent?.Legislation ?? Symbol?.Legislation;
        public override string? Meaning => FlagContent?.Meaning ?? Symbol?.Meaning;

        public List<IContentSection>? Sections => FlagContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => FlagContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => FlagContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Red;

        public string SymbolTypeName => "State Flag";
        public string SymbolTypeSlug => "flag";
        public string SymbolTypePlural => "flags";
        public string SymbolTypeIcon => "??";


        public bool HasContent => FlagContent != null && !string.IsNullOrEmpty(FlagContent.HtmlContent);
        public bool HasIntro => !string.IsNullOrEmpty(FlagContent?.IntroText);
        public bool HasVersions => FlagContent?.Sections?.Any(s => s.Versions?.Any() == true) == true;
        public bool HasSymbols => FlagContent?.Sections?.Any(s => s.Symbols?.Any() == true) == true;
        public bool HasStandardizedYear => FlagContent?.StandardizedYear.HasValue == true;
        public override IReadOnlyList<QuickFactItem>? QuickFacts
        {
            get => FlagContent?.QuickFacts;
            set { }
        }

        public BigStatViewModel? BigStat { get; init; }
        public IReadOnlyList<TimelineEventViewModel>? Timeline { get; init; }
        public ExpertQuoteViewModel? ExpertQuote { get; init; }
        public string? BigStatAfterSectionId => FlagContent?.BigStatAfterSectionId;
        public string? TimelineAfterSectionId => FlagContent?.TimelineAfterSectionId;
        public string? ExpertQuoteAfterSectionId => FlagContent?.ExpertQuoteAfterSectionId;
        public IReadOnlyList<VisualAsset>? VisualAssets => FlagContent?.VisualAssets;
    }
}
