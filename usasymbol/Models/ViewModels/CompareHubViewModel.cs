namespace USASymbol.Models.ViewModels
{
    public class CompareHubViewModel
    {
        public List<State> States { get; set; } = new();

        /// <summary>Popular comparison pairs shown as quick links.</summary>
        public List<(string Slug1, string Name1, string Slug2, string Name2)> FeaturedPairs { get; set; } = new();

        /// <summary>Neighboring states and common moving routes (second row of quick links).</summary>
        public List<(string Slug1, string Name1, string Slug2, string Name2)> NeighborPairs { get; set; } = new();

        /// <summary>"At a glance" rows for the example comparison on the hub.</summary>
        public List<CompareGlanceRow> GlanceRows { get; set; } = new();
        public string GlanceSlugA { get; set; } = "";
        public string GlanceNameA { get; set; } = "";
        public string GlanceSlugB { get; set; } = "";
        public string GlanceNameB { get; set; } = "";
    }

    public record CompareGlanceRow(string Label, string ValueA, string ValueB, string MetricSlug);
}
