using System;
using System.Collections.Generic;
using USASymbol.Models.ViewModels;

namespace USASymbol.Models.Content
{
    public class NicknameContent
    {

        public string Title { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsOfficial { get; set; }
        public int? AdoptedYear { get; set; }
        public List<string> OtherNicknames { get; set; } = new();


        public string WikidataId { get; set; } = string.Empty;
        public string Legislation { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;


        public string Author { get; set; } = string.Empty;
        public DateTime? DatePublished { get; set; }
        public DateTime? DateModified { get; set; }
        public DateTime LastModified { get; set; }


        public string SeoTitle { get; set; } = string.Empty;
        public string SeoDescription { get; set; } = string.Empty;


        public string IntroText { get; set; } = string.Empty;
        public List<QuickFactItem> QuickFacts { get; set; } = new();
        public List<NicknameSection> Sections { get; set; } = new();
        public BigStatData? BigStat { get; set; }
        public List<TimelineEvent> Timeline { get; set; } = new();
        public ExpertQuoteData? ExpertQuote { get; set; }
        public string BigStatAfterSectionId { get; set; } = string.Empty;
        public string TimelineAfterSectionId { get; set; } = string.Empty;
        public string ExpertQuoteAfterSectionId { get; set; } = string.Empty;
        public List<NicknameFaq> Faq { get; set; } = new();

        public List<NicknameSource> Sources { get; set; } = new();


        public List<VisualAsset> VisualAssets { get; set; } = new();
        public ComparisonCardsBlock? ComparisonCards { get; set; }
        public string ComparisonCardsAfterSectionId { get; set; } = string.Empty;


    }

    public class NicknameSection : IContentSection
    {
        public string Id { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;

        public string? Img { get; set; }
        public List<string>? Paragraphs { get; set; }
        public List<string>? Facts { get; set; }

        public List<string>? ListItems { get; set; }

        public List<IContentSubsection>? Subsections { get; set; }


        public List<NicknameSubsection>? NicknameSubsections { get; set; }
    }

    public class NicknameSubsection : IContentSubsection
    {
        public string Subtitle { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<string>? ListItems { get; set; }
        public LinkData? Link { get; set; }
    }

    public class NicknameFaq : IFaqItem
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }

    public class NicknameSource : ISource
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
