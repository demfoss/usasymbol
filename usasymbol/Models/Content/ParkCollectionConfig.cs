namespace USASymbol.Models.Content
{
    public class ParkCollectionConfig
    {
        public string Slug { get; set; } = "";
        public string H1 { get; set; } = "";
        public string SeoTitle { get; set; } = "";
        public string SeoDescription { get; set; } = "";
        public string Intro { get; set; } = "";
        public string QuickAnswer { get; set; } = "";

        // Sort
        public string SortBy { get; set; } = "name";     // visitation_rank | area_acres | established_year | name
        public string SortDir { get; set; } = "asc";     // asc | desc

        // Optional filter applied on top of sort
        public string? FilterField { get; set; }          // free | activity | pets_allowed | dark_sky | state | region
        public string? FilterValue { get; set; }

        // Table column for the ranked metric
        public string MetricLabel { get; set; } = "";
        public string MetricFormat { get; set; } = "text"; // number | acres | year | rank | fee | text

        public string? Updated { get; set; }
    }
}
