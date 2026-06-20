using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using USASymbol.Models.Content;
using Usasymbol.ViewModels;

namespace Usasymbol.Helpers
{
    public static class MapBlockHelper
    {
        public static async Task<IHtmlContent> RenderMapBlockAsync(
            this IHtmlHelper html,
            PageMap? map,
            PageTable? table = null,
            string? slug = null)
        {
            if (map == null) return HtmlString.Empty;

            var model = new MapBlockViewModel
            {
                Slug          = slug ?? string.Empty,
                Title         = map.Title,
                Image         = map.Image,
                ImageAlt      = map.ImageAlt,
                Caption       = map.Caption ?? string.Empty,
                IsCategorical = ChoroplethBuilder.IsCategoricalMap(map),
            };

            if (table?.Rows?.Count > 0)
            {
                ChoroplethResult result;

                if (!string.IsNullOrWhiteSpace(map.MetricKey))
                    result = ChoroplethBuilder.Build(map, table.Rows);
                else
                    result = ChoroplethBuilder.BuildFlat(table.Rows);

                model.Entries         = result.Entries;
                model.LightColor      = result.LightColor;
                model.DarkColor       = result.DarkColor;
                model.MinDisplayValue = result.MinDisplayValue;
                model.MaxDisplayValue = result.MaxDisplayValue;
                model.MetricLabel     = map.MetricLabel
                    ?? (!string.IsNullOrWhiteSpace(map.MetricKey)
                        ? ChoroplethBuilder.FormatLabel(map.MetricKey)
                        : string.Empty);
                model.LegendSteps     = result.LegendSteps;
            }

            var viewContext    = html.ViewContext;
            var sp             = viewContext.HttpContext.RequestServices;
            var componentHelper = sp.GetRequiredService<IViewComponentHelper>();

            if (componentHelper is IViewContextAware ctx)
                ctx.Contextualize(viewContext);

            return await componentHelper.InvokeAsync("MapBlock", model);
        }
    }
}
