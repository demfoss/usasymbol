using System.Collections.Generic;
using System;
using USASymbol.Models.ViewModels;

namespace USASymbol.Models.Content
{
    public class BirdContent
    {

        public string Title { get; set; } = string.Empty;
        public string ScientificName { get; set; } = string.Empty;
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


        public List<string> SharedStates { get; set; } = new();


        public string Habitat { get; set; } = string.Empty;
        public string DistinctiveFeature { get; set; } = string.Empty;
        public string ConservationStatus { get; set; } = string.Empty;

        public string? SharedStatesText { get; set; } = string.Empty;
        public string? DistinctiveSong { get; set; } = string.Empty;
        public string? Coloration { get; set; } = string.Empty;
        public string? ColorationDetails { get; set; } = string.Empty;


        public string Size { get; set; } = string.Empty;
        public string Wingspan { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;


        public string Diet { get; set; } = string.Empty;
        public string Nesting { get; set; } = string.Empty;
        public string Range { get; set; } = string.Empty;


        public List<BirdSection> Sections { get; set; } = new();


        public List<BirdSource> Sources { get; set; } = new();

        public List<BirdFaq> Faq { get; set; } = new();


        public BigStatData? BigStat { get; set; }
        public List<TimelineEvent> Timeline { get; set; } = new();
        public ExpertQuoteData? ExpertQuote { get; set; }
        public string BigStatAfterSectionId { get; set; } = string.Empty;
        public string TimelineAfterSectionId { get; set; } = string.Empty;
        public string ExpertQuoteAfterSectionId { get; set; } = string.Empty;


        public List<QuickFactItem> QuickFacts { get; set; } = new();


        public string HtmlContent { get; set; } = string.Empty;


        public DateTime LastModified { get; set; }


        public List<VisualAsset> VisualAssets { get; set; } = new();
    }

    public class BirdSection : IContentSection
    {
        public string Id { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Style { get; set; }

        public string? Img { get; set; }
        public List<string>? Paragraphs { get; set; }
        public List<BirdSubsection>? Subsections { get; set; }

        List<IContentSubsection>? IContentSection.Subsections =>
            Subsections?.Cast<IContentSubsection>().ToList();

        public List<string>? Facts { get; set; }
        public List<string>? ListItems { get; set; }
    }

    public class BirdSubsection : IContentSubsection
    {
        public string Subtitle { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<string>? ListItems { get; set; }
        public LinkData? Link { get; set; }
    }

    public class BirdSource : ISource
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class BirdFaq : IFaqItem
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }
}
