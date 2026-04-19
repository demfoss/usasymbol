using System;
using System.Collections.Generic;
using USASymbol.Models.ViewModels;

namespace USASymbol.Models.Content
{
    public class FlowerContent
    {

        public string Type { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ScientificName { get; set; } = string.Empty;
        public int? AdoptedYear { get; set; }
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


        public List<FlowerSection> Sections { get; set; } = new();


        public BigStatData? BigStat { get; set; }
        public List<TimelineEvent> Timeline { get; set; } = new();
        public ExpertQuoteData? ExpertQuote { get; set; }
        public string BigStatAfterSectionId { get; set; } = string.Empty;
        public string TimelineAfterSectionId { get; set; } = string.Empty;
        public string ExpertQuoteAfterSectionId { get; set; } = string.Empty;

        public List<QuickFactItem> QuickFacts { get; set; } = new();


        public List<FlowerFaq> Faq { get; set; } = new();


        public List<FlowerSource> Sources { get; set; } = new();


        public string HtmlContent { get; set; } = string.Empty;


        public DateTime LastModified { get; set; }


        public List<VisualAsset> VisualAssets { get; set; } = new();
    }

    public class FlowerSection : IContentSection
    {
        public string Id { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Img { get; set; }
        public List<string> Paragraphs { get; set; } = new();
        public List<string> Facts { get; set; } = new();

        public List<IContentSubsection>? Subsections { get; set; }

        public List<FlowerSubsection> FlowerSubsections { get; set; } = new();

        public List<string>? ListItems { get; set; }
    }

    public class FlowerSubsection : IContentSubsection
    {
        public string Subtitle { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<string>? ListItems { get; set; } = new();
        public LinkData? Link { get; set; }
    }

    public class FlowerFaq : IFaqItem
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }

    public class FlowerSource : ISource
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
