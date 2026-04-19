using System.Collections.Generic;
using System.Linq;

namespace Usasymbol.ViewModels
{
    public class MapBlockViewModel
    {
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

        public List<(string Color, string Label)> LegendSteps { get; set; } = new();

        public bool HasChoropleth => Entries.Count > 0;
        public bool IsNumeric => Entries.Any(e => e.NumericValue.HasValue);
    }
}
