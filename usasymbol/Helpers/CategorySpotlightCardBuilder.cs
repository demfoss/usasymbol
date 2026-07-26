using USASymbol.Models;
using USASymbol.Models.ViewModels;

namespace Usasymbol.Helpers
{
    /// <summary>
    /// Builds "Key differences" spotlight cards for a category-scoped state-pair comparison,
    /// reusing the natural-language summaries ComparisonMetricsConfig already generates per metric
    /// instead of hand-writing copy per category (see Views/Compare/Overview.cshtml for the
    /// hand-curated, cross-category equivalent this intentionally does not replicate per-category).
    /// </summary>
    public static class CategorySpotlightCardBuilder
    {
        public static List<CategorySpotlightCard> Build(IEnumerable<MetricComparisonResult> results, string pairSlug, int take = 6)
        {
            return results
                .Where(r => r.Metric.Type == MetricType.Numeric && r.WinnerSlug != null && !string.IsNullOrWhiteSpace(r.SummaryText))
                .OrderBy(r => r.Metric.SortOrder)
                .Take(take)
                .Select(r => new CategorySpotlightCard
                {
                    Icon = r.Metric.Icon,
                    Eyebrow = r.Metric.Name,
                    Body = r.SummaryText!,
                    Link = $"/compare/{pairSlug}/{r.Metric.Slug}"
                })
                .ToList();
        }
    }
}
