using USASymbol.Models.ViewModels;

namespace USASymbol.Models.Content
{
    public class FossilContent
    {
        public string Type { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string StateFips { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CommonName { get; set; } = string.Empty;
        public string BinomialName { get; set; } = string.Empty;
        public string FossilCategory { get; set; } = string.Empty;
        public int? AdoptedYear { get; set; }

        // Creature stats
        public string GeologicalAge { get; set; } = string.Empty;
        public string LivedWhen { get; set; } = string.Empty;
        public string ExtinctWhen { get; set; } = string.Empty;
        public string Length { get; set; } = string.Empty;
        public string Diet { get; set; } = string.Empty;

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

        public List<FossilSection> Sections { get; set; } = new();
        public List<FossilFaq> Faq { get; set; } = new();
        public List<FossilSource> Sources { get; set; } = new();
        public List<QuickFactItem> QuickFacts { get; set; } = new();
        public List<VisualAsset> VisualAssets { get; set; } = new();
    }

    public class FossilSection : IContentSection
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

        // Fossil find sites for Leaflet map (location section only)
        public List<FossilSite> Sites { get; set; } = new();
    }

    public class FossilSite
    {
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string Note { get; set; } = string.Empty;
        public string Type { get; set; } = "primary"; // primary | secondary
    }

    public class FossilFaq : IFaqItem
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }

    public class FossilSource : ISource
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
