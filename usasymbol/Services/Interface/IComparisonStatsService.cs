using USASymbol.Models;

namespace USASymbol.Services.Interface
{
    public interface IComparisonStatsService
    {
        Task<StateStats?> GetStatsAsync(string stateSlug);
        Task<Dictionary<string, StateStats>> GetAllStatsAsync();
    }
}
