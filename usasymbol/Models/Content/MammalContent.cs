using USASymbol.Models.ViewModels;

namespace USASymbol.Models.Content
{
    public class MammalContent
    {

        public string Title { get; set; } = "";
        public string ScientificName { get; set; } = "";
        public int? AdoptedYear { get; set; }


        public string WikidataId { get; set; } = "";
        public string Legislation { get; set; } = "";
        public string Meaning { get; set; } = "";


        public string CommonName { get; set; } = "";
        public string OfficialSince { get; set; } = "";
        public string Status { get; set; } = "";
        public string HabitatInState { get; set; } = "";
        public string KnownFor { get; set; } = "";


        public string Size { get; set; } = "";
        public string Weight { get; set; } = "";
        public string Coloration { get; set; } = "";
        public string DistinctiveTraits { get; set; } = "";


        public string Habitat { get; set; } = "";
        public string RangeInState { get; set; } = "";


        public string ConservationStatus { get; set; } = "";
        public string WhereToFind { get; set; } = "";
        public string SymbolConnections { get; set; } = "";


        public string Author { get; set; } = "";
        public DateTime? DatePublished { get; set; }
        public DateTime? DateModified { get; set; }
        public DateTime? LastModified { get; set; }
        public string SeoTitle { get; set; } = "";
        public string SeoDescription { get; set; } = "";
        public string IntroText { get; set; } = "";
        public string HtmlContent { get; set; } = "";


        public List<MammalSection> Sections { get; set; } = new();
        public List<MammalSource> Sources { get; set; } = new();
        public List<MammalFaq> Faq { get; set; } = new();


        public List<string> QuickSummary { get; set; } = new();
        public string DidYouKnow { get; set; } = "";
        public BigStatData? BigStat { get; set; }
        public List<TimelineEvent> Timeline { get; set; } = new();
        public ExpertQuoteData? ExpertQuote { get; set; }
        public string BigStatAfterSectionId { get; set; } = string.Empty;
        public string TimelineAfterSectionId { get; set; } = string.Empty;
        public string ExpertQuoteAfterSectionId { get; set; } = string.Empty;

        public List<QuickFactItem> QuickFacts { get; set; } = new();


        public List<VisualAsset> VisualAssets { get; set; } = new();
    }


    public class BigStatData
    {
        public string Number { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class TimelineEvent
    {
        public string Year { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class ExpertQuoteData
    {
        public string Text { get; set; } = "";
        public string Source { get; set; } = "";
    }


    public class MammalSection : IContentSection
    {
        public string Id { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Title { get; set; } = "";
        public string Style { get; set; } = "";
        public string? Img { get; set; }
        public List<string> Paragraphs { get; set; } = new();
        public List<IContentSubsection> Subsections { get; set; } = new();
        public List<string> Facts { get; set; } = new();
        public List<string> ListItems { get; set; } = new();
    }

    public class MammalSubsection : IContentSubsection
    {
        public string Subtitle { get; set; } = "";
        public string Text { get; set; } = "";
        public List<string> ListItems { get; set; } = new();
        public LinkData? Link { get; set; }
    }

    public class MammalSource : ISource
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class MammalFaq : IFaqItem
    {
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
    }
}
