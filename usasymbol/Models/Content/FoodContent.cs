using USASymbol.Models.ViewModels;

namespace USASymbol.Models.Content
{
    public class FoodContent
    {
        public string Type { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string StateFips { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
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

        public List<FoodSection> Sections { get; set; } = new();
        public List<FoodFaq> Faq { get; set; } = new();
        public List<FoodSource> Sources { get; set; } = new();
        public List<QuickFactItem> QuickFacts { get; set; } = new();
        public List<VisualAsset> VisualAssets { get; set; } = new();
    }

    public class FoodSection : IContentSection
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

        // Where the food comes from (location section only, optional), shown on a Leaflet map
        public List<FoodSite> Sites { get; set; } = new();
    }

    public class FoodSite
    {
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string Note { get; set; } = string.Empty;
        public string Type { get; set; } = "primary"; // primary | secondary
    }

    public class FoodFaq : IFaqItem
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }

    public class FoodSource : ISource
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
