using USASymbol.Models.ViewModels;

namespace USASymbol.Models.Content
{
    public class DinosaurContent
    {
        public string Title { get; set; } = "";
        public int? AdoptedYear { get; set; }
        public string WikidataId { get; set; } = "";
        public string Legislation { get; set; } = "";
        public string Meaning { get; set; } = "";
        public string ScientificName { get; set; } = "";
        public string Period { get; set; } = "";
        public string DiscoveredIn { get; set; } = "";
        public string Diet { get; set; } = "";
        public string Length { get; set; } = "";
        public string Weight { get; set; } = "";
        public string NamedBy { get; set; } = "";
        public string FossilSites { get; set; } = "";
        public string Author { get; set; } = "";
        public DateTime? DatePublished { get; set; }
        public DateTime? DateModified { get; set; }
        public DateTime? LastModified { get; set; }
        public string SeoTitle { get; set; } = "";
        public string SeoDescription { get; set; } = "";
        public string IntroText { get; set; } = "";
        public List<DinosaurSection> Sections { get; set; } = new();
        public List<DinosaurSource> Sources { get; set; } = new();
        public List<DinosaurFaq> Faq { get; set; } = new();
        public BigStatData? BigStat { get; set; }
        public List<TimelineEvent> Timeline { get; set; } = new();
        public ExpertQuoteData? ExpertQuote { get; set; }
        public string BigStatAfterSectionId { get; set; } = string.Empty;
        public string TimelineAfterSectionId { get; set; } = string.Empty;
        public string ExpertQuoteAfterSectionId { get; set; } = string.Empty;
        public List<VisualAsset> VisualAssets { get; set; } = new();
        public List<QuickFactItem> QuickFacts { get; set; } = new();
    }

    public class DinosaurSection : IContentSection
    {
        public string Id { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Style { get; set; }
        public List<string>? Paragraphs { get; set; }
        public List<DinosaurSubsection>? Subsections { get; set; }
        List<IContentSubsection>? IContentSection.Subsections => Subsections?.Cast<IContentSubsection>().ToList();
        public string? Img { get; set; }
        public List<string>? Facts { get; set; }
        public List<string>? ListItems { get; set; }
    }

    public class DinosaurSubsection : IContentSubsection
    {
        public string Subtitle { get; set; } = "";
        public string Text { get; set; } = "";
        public List<string>? ListItems { get; set; }
        public LinkData? Link { get; set; }
    }

    public class DinosaurSource : ISource
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class DinosaurFaq : IFaqItem
    {
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
    }
}
