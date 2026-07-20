using System;
using System.Collections.Generic;
using USASymbol.Models.ViewModels;

namespace USASymbol.Models.Content
{
    public class FlagContent
    {

        public string Type { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? AdoptedYear { get; set; }
        public int? StandardizedYear { get; set; }
        public bool IsOfficial { get; set; }


        public string WikidataId { get; set; } = string.Empty;
        public string Legislation { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;


        public DateTime? DatePublished { get; set; }
        public DateTime? DateModified { get; set; }
        public string Author { get; set; } = string.Empty;


        public string SeoTitle { get; set; } = string.Empty;
        public string SeoDescription { get; set; } = string.Empty;


        public string IntroText { get; set; } = string.Empty;


        public List<FlagSection> Sections { get; set; } = new();


        public BigStatData? BigStat { get; set; }
        public List<TimelineEvent> Timeline { get; set; } = new();
        public ExpertQuoteData? ExpertQuote { get; set; }
        public string BigStatAfterSectionId { get; set; } = string.Empty;
        public string TimelineAfterSectionId { get; set; } = string.Empty;
        public string ExpertQuoteAfterSectionId { get; set; } = string.Empty;

        public List<QuickFactItem> QuickFacts { get; set; } = new();


        public List<FlagFaq> Faq { get; set; } = new();


        public List<FlagSource> Sources { get; set; } = new();


        public string HtmlContent { get; set; } = string.Empty;


        public DateTime LastModified { get; set; }


        public List<VisualAsset> VisualAssets { get; set; } = new();
        public ComparisonCardsBlock? ComparisonCards { get; set; }
        public string ComparisonCardsAfterSectionId { get; set; } = string.Empty;
    }

    public class FlagSection : IContentSection
    {
        public string Id { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;
        public string? Img { get; set; }
        public List<string> Paragraphs { get; set; } = new();
        public List<string> Facts { get; set; } = new();
        public List<IContentSubsection>? Subsections { get; set; }


        public List<FlagVersion> Versions { get; set; } = new();
        public List<FlagSymbol> Symbols { get; set; } = new();
        public List<FlagColor> Colors { get; set; } = new();

        public List<string>? ListItems { get; set; }
    }

    public class FlagSubsection : IContentSubsection
    {
        public string Subtitle { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<string> ListItems { get; set; } = new();
        public LinkData? Link { get; set; }
        public string? Image { get; set; }
        public string? ImageCaption { get; set; }
    }

    public class FlagVersion
    {
        public string Name { get; set; } = string.Empty;
        public string Years { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class FlagSymbol
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string? ClipRegion { get; set; }
        public List<string> Paragraphs { get; set; } = new();
    }

    public class FlagFaq : IFaqItem
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }

    public class FlagSource : ISource
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
    public class FlagColor
    {
        public string Name { get; set; } = string.Empty;
        public string Hex { get; set; } = string.Empty;
        public string Pantone { get; set; } = string.Empty;
        public string Cable { get; set; } = string.Empty;
    }

    public class ComparisonCardsBlock
    {
        public string Heading { get; set; } = string.Empty;
        public string Framing { get; set; } = string.Empty;
        public List<ComparisonCardItem> Cards { get; set; } = new();
    }

    public class ComparisonCardItem
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string Alt { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CrossColor { get; set; } = string.Empty;
        public string FieldColor { get; set; } = string.Empty;
        public string Adopted { get; set; } = string.Empty;
    }
}
