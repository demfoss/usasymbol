using USASymbol.Models.ViewModels;

namespace USASymbol.Models.Content
{
    public class FirearmContent
    {

        public string Title { get; set; } = "";
        public int? AdoptedYear { get; set; }


        public string WikidataId { get; set; } = "";
        public string Legislation { get; set; } = "";
        public string Meaning { get; set; } = "";


        public string ActionType { get; set; } = "";
        public string Caliber { get; set; } = "";
        public string YearDesigned { get; set; } = "";
        public string Designer { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public string Weight { get; set; } = "";
        public string BarrelLength { get; set; } = "";
        public string Governor { get; set; } = "";
        public string Museum { get; set; } = "";


        public string Author { get; set; } = "";
        public DateTime? DatePublished { get; set; }
        public DateTime? DateModified { get; set; }
        public DateTime? LastModified { get; set; }
        public string SeoTitle { get; set; } = "";
        public string SeoDescription { get; set; } = "";
        public string IntroText { get; set; } = "";


        public List<FirearmSection> Sections { get; set; } = new();
        public List<FirearmSource> Sources { get; set; } = new();
        public List<FirearmFaq> Faq { get; set; } = new();


        public BigStatData? BigStat { get; set; }
        public List<TimelineEvent> Timeline { get; set; } = new();
        public ExpertQuoteData? ExpertQuote { get; set; }
        public string BigStatAfterSectionId { get; set; } = string.Empty;
        public string TimelineAfterSectionId { get; set; } = string.Empty;
        public string ExpertQuoteAfterSectionId { get; set; } = string.Empty;


        public List<VisualAsset> VisualAssets { get; set; } = new();


        public List<QuickFactItem> QuickFacts { get; set; } = new();
    }

    public class FirearmSection : IContentSection
    {
        public string Id { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Style { get; set; }

        public List<string>? Paragraphs { get; set; }
        public List<FirearmSubsection>? Subsections { get; set; }

        List<IContentSubsection>? IContentSection.Subsections =>
            Subsections?.Cast<IContentSubsection>().ToList();

        public string? Img { get; set; }
        public List<string>? Facts { get; set; }
        public List<string>? ListItems { get; set; }
        public List<CaliberCard>? CalibersGrid { get; set; }
    }

    public class CaliberCard
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class FirearmSubsection : IContentSubsection
    {
        public string Subtitle { get; set; } = "";
        public string Text { get; set; } = "";
        public List<string>? ListItems { get; set; }
        public LinkData? Link { get; set; }
    }

    public class FirearmSource : ISource
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class FirearmFaq : IFaqItem
    {
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
    }
}
