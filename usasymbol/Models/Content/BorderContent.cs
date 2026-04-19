namespace USASymbol.Models.Content
{
    public class BorderContent
    {
        public string Type { get; set; } = "";
        public string State { get; set; } = "";
        public bool IsOfficial { get; set; }
        public string? Author { get; set; }
        public DateTime? DatePublished { get; set; }
        public DateTime? DateModified { get; set; }
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? IntroText { get; set; }
        public string? HtmlContent { get; set; }
        public DateTime? LastModified { get; set; }

        public BorderSummary? BorderSummary { get; set; }
        public BorderMap? Map { get; set; }
        public List<BorderCard> Cards { get; set; } = new();
        public List<BorderSection> Sections { get; set; } = new();
        public List<BorderFaq> Faq { get; set; } = new();
        public List<BorderSource> Sources { get; set; } = new();
    }

    public class BorderSummary
    {
        public int BorderingStatesCount { get; set; }
        public string BorderingStatesList { get; set; } = "";
        public string? CountryBorders { get; set; }
        public string? OceanBorders { get; set; }
        public string? GreatLakeBorders { get; set; }
        public string? MajorRiverBorders { get; set; }
        public bool Landlocked { get; set; }
    }

    public class BorderMap
    {
        public string Title { get; set; } = "";
        public string Image { get; set; } = "";
        public string ImageAlt { get; set; } = "";
        public string? Caption { get; set; }
    }

    public class BorderCard
    {
        public string Name { get; set; } = "";
        public string Badge { get; set; } = "";
        public string NeighborKind { get; set; } = "";
        public string BorderType { get; set; } = "";
        public string Features { get; set; } = "";
        public string? Anchor { get; set; }
    }

    public class BorderSection
    {
        public string Id { get; set; } = "";
        public string? Icon { get; set; }
        public string Title { get; set; } = "";
        public string? Style { get; set; }
        public List<string>? Paragraphs { get; set; }
        public List<string>? Bullets { get; set; }
        public BorderTable? Table { get; set; }
        public List<string>? Facts { get; set; }
        public List<BorderSubsection>? Subsections { get; set; }
    }

    public class BorderSubsection
    {
        public string? Id { get; set; }
        public string Subtitle { get; set; } = "";
        public string? Text { get; set; }
        public List<string>? Paragraphs { get; set; }
        public List<string>? Bullets { get; set; }
    }

    public class BorderTable
    {
        public List<string> Columns { get; set; } = new();
        public List<BorderTableRow> Rows { get; set; } = new();
    }

    public class BorderTableRow
    {
        public Dictionary<string, string> Data { get; set; } = new();
    }

    public class BorderFaq
    {
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
    }

    public class BorderSource
    {
        public string Name { get; set; } = "";
        public string? Url { get; set; }
        public string? Description { get; set; }
    }
}