using System;
using System.Collections.Generic;
using USASymbol.Models.ViewModels;

namespace USASymbol.Models.Content
{
    public class MottoContent
    {
        public string Title { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string EnglishTranslation { get; set; } = string.Empty;
        public int? AdoptedYear { get; set; }


        public string WikidataId { get; set; } = string.Empty;
        public string Legislation { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;

        public DateTime? DatePublished { get; set; }
        public DateTime? DateModified { get; set; }
        public string Author { get; set; } = string.Empty;
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? IntroText { get; set; }
        public List<QuickFactItem> QuickFacts { get; set; } = new();
        public List<MottoSection> Sections { get; set; } = new();
        public BigStatData? BigStat { get; set; }
        public List<TimelineEvent> Timeline { get; set; } = new();
        public ExpertQuoteData? ExpertQuote { get; set; }
        public string BigStatAfterSectionId { get; set; } = string.Empty;
        public string TimelineAfterSectionId { get; set; } = string.Empty;
        public string ExpertQuoteAfterSectionId { get; set; } = string.Empty;
        public List<MottoFaq> Faq { get; set; } = new();
        public List<MottoSource> Sources { get; set; } = new();
        public string HtmlContent { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }


        public List<VisualAsset> VisualAssets { get; set; } = new();
    }

    public class MottoSection : IContentSection
    {
        public string Id { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Style { get; set; }
        public string? Img { get; set; }
        public List<string>? Paragraphs { get; set; }
        public List<string>? Facts { get; set; }
        public List<IContentSubsection>? Subsections { get; set; }

        public List<string>? ListItems { get; set; }


        public List<MottoSubsection>? MottoSubsections { get; set; }
    }

    public class MottoSubsection : IContentSubsection
    {
        public string Subtitle { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<string>? ListItems { get; set; }

        public LinkData? Link { get; set; }
    }

    public class MottoSource : ISource
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class MottoFaq : IFaqItem
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }
}
