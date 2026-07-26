using USASymbol.Models;
using USASymbol.Models.ViewModels;

namespace USASymbol.Services.Interface
{
    public interface IComparisonService
    {
        Task<StatePairComparisonViewModel?> GetPairComparisonAsync(string slugA, string slugB);
        Task<MetricComparisonViewModel?> GetMetricComparisonAsync(string slugA, string slugB, string metricSlug);
        Task<CompareHubViewModel> GetHubViewModelAsync();

        /// <summary>All 50 states as rows, one column per metric in the given category (ComparisonMetricDefinition.GroupSlug), plus best/worst highlights per numeric metric. Table is null if the category slug is unknown.</summary>
        Task<(TableSectionViewModel? Table, List<CategoryExtreme> Highlights)> GetCategoryStatesTableAsync(string categorySlug);
    }
}
