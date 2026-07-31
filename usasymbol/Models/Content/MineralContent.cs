using USASymbol.Models.ViewModels;

namespace USASymbol.Models.Content
{
    // Shared content shape for State Mineral, State Rock (or Stone), and State Gemstone pages.
    // One model/service/view is reused for all three designations, the same way SealContent
    // is reused for both State Seal and Coat of Arms pages.
    public class MineralContent
    {
        public string Type { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string StateFips { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string DesignationLabel { get; set; } = string.Empty;
        public int? AdoptedYear { get; set; }
        public bool IsOfficial { get; set; }

        public string Legislation { get; set; } = string.Empty;

        // Physical/identifying properties - optional, filled only when verified for this material.
        public string Color { get; set; } = string.Empty;
        public string Hardness { get; set; } = string.Empty;
        public string CrystalSystem { get; set; } = string.Empty;
        public string FormationType { get; set; } = string.Empty;
        public string ChemicalFormula { get; set; } = string.Empty;
        public string PrimaryUse { get; set; } = string.Empty;

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

        public List<MineralSection> Sections { get; set; } = new();
        public List<MineralFaq> Faq { get; set; } = new();
        public List<MineralSource> Sources { get; set; } = new();
        public List<QuickFactItem> QuickFacts { get; set; } = new();
        public List<VisualAsset> VisualAssets { get; set; } = new();
    }

    public class MineralSection : IContentSection
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

        // Mine/quarry/deposit sites for the Leaflet map (location section only, optional)
        public List<MineralSite> Sites { get; set; } = new();
    }

    public class MineralSite
    {
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string Note { get; set; } = string.Empty;
        public string Type { get; set; } = "primary"; // primary | secondary
    }

    public class MineralFaq : IFaqItem
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }

    public class MineralSource : ISource
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
