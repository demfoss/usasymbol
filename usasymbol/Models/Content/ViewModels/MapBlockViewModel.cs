using System.Collections.Generic;
using System.Linq;

namespace Usasymbol.ViewModels
{
    public class MapBlockViewModel
    {
        public class FilterGroup
        {
            public string Key { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
            public string Style { get; set; } = "chips";
            public string AccentColor { get; set; } = "#0f766e";
            public bool UseColors { get; set; }
        }

        public string Slug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string ImageAlt { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;

        public List<StateMapEntry> Entries { get; set; } = new();
        public string MetricLabel { get; set; } = string.Empty;
        public string LightColor { get; set; } = "#dbeafe";
        public string DarkColor { get; set; } = "#1e3a8a";
        public string MinDisplayValue { get; set; } = string.Empty;
        public string MaxDisplayValue { get; set; } = string.Empty;
        public string SvgContent { get; set; } = string.Empty;
        public string ColorCss { get; set; } = string.Empty;

        // Set by MapBlockViewComponent after PNG is generated/cached on disk
        public string? GeneratedImagePath { get; set; }

        public List<(string Color, string Label)> LegendSteps { get; set; } = new();
        public bool IsCategorical { get; set; }
        public List<FilterGroup> FilterGroups { get; set; } = new();

        public bool ShowTextOverlay { get; set; }

        public bool HasChoropleth => Entries.Count > 0;
        public bool IsNumeric => Entries.Any(e => e.NumericValue.HasValue);
    }
}
