using USASymbol.Models.ViewModels;

namespace USASymbol.Models.Content
{
    public class SoilContent
    {
        public string Type { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string StateFips { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? AdoptedYear { get; set; }
        public bool IsOfficial { get; set; }

        public string Legislation { get; set; } = string.Empty;

        public string Author { get; set; } = string.Empty;
        public DateTime? DatePublished { get; set; }
        public DateTime? DateModified { get; set; }
        public DateTime LastModified { get; set; }

        public string SeoTitle { get; set; } = string.Empty;
        public string SeoDescription { get; set; } = string.Empty;
        public string HeroImage { get; set; } = string.Empty;
        public string HeroImageAlt { get; set; } = string.Empty;
        public string HeroImageCaption { get; set; } = string.Empty;
        public string IntroText { get; set; } = string.Empty;

        public string BigStatAfterSectionId { get; set; } = string.Empty;
        public string ExpertQuoteAfterSectionId { get; set; } = string.Empty;

        public List<SoilSection> Sections { get; set; } = new();
        public List<SoilFaq> Faq { get; set; } = new();
        public List<SoilSource> Sources { get; set; } = new();
        public List<QuickFactItem> QuickFacts { get; set; } = new();

        public BigStatData? BigStat { get; set; }
        public ExpertQuoteData? ExpertQuote { get; set; }

        public List<VisualAsset> VisualAssets { get; set; } = new();
    }

    public class SoilSection : IContentSection
    {
        public string Id { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;
        public string? Img { get; set; }
        public List<string> Paragraphs { get; set; } = new();
        public List<string> Facts { get; set; } = new();
        public List<IContentSubsection>? Subsections { get; set; }
        public List<string>? ListItems { get; set; }

        // Soil profile horizons (profile section)
        public List<SoilLayer> Layers { get; set; } = new();

        // County distribution (location section)
        public List<string> Counties { get; set; } = new();
    }

    public class SoilLayer
    {
        public string Horizon { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DepthIn { get; set; } = string.Empty;
        public string ColorHex { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public string Texture { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }

    public class SoilSubsection : IContentSubsection
    {
        public string Subtitle { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<string> ListItems { get; set; } = new();
        public LinkData? Link { get; set; }
    }

    public class SoilFaq : IFaqItem
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }

    public class SoilSource : ISource
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
