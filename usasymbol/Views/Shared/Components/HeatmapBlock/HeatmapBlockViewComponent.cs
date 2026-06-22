using Microsoft.AspNetCore.Mvc;
using USASymbol.Models.Content;

namespace Usasymbol.Views.Shared.Components.HeatmapBlock
{
    public class HeatmapBlockViewModel
    {
        public string Title { get; set; } = "";
        public string? Caption { get; set; }
        public string DataUrl { get; set; } = "";
        public string Gradient { get; set; } = "hot";
        public int Radius { get; set; } = 18;
        public int Blur { get; set; } = 22;
        public int PointsPerCluster { get; set; } = 1;
    }

    public class HeatmapBlockViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(PageHeatmap heatmap)
        {
            var model = new HeatmapBlockViewModel
            {
                Title            = heatmap.Title,
                Caption          = heatmap.Caption,
                DataUrl          = heatmap.DataUrl,
                Gradient         = heatmap.Gradient,
                Radius           = heatmap.Radius,
                Blur             = heatmap.Blur,
                PointsPerCluster = heatmap.PointsPerCluster,
            };
            return View(model);
        }
    }
}
