using USASymbol.Models;
using USASymbol.Models.ViewModels;

namespace USASymbol.Services.Interface
{
    public interface IComparisonService
    {
        Task<StatePairComparisonViewModel?> GetPairComparisonAsync(string slugA, string slugB);
        Task<MetricComparisonViewModel?> GetMetricComparisonAsync(string slugA, string slugB, string metricSlug);
        Task<CompareHubViewModel> GetHubViewModelAsync();
    }
}
