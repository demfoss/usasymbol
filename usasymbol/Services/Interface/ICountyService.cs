using USASymbol.Models.ViewModels;

namespace USASymbol.Services.Interface;

public interface ICountyService
{
    Task<CountyIndexViewModel?> GetIndexAsync(string stateSlug);
    Task<CountyProfileViewModel?> GetProfileAsync(string stateSlug, string countySlug);
    Task<CountyMatchPageViewModel> GetMatcherAsync();
    Task<CountyRankingsPageViewModel> GetRankingsAsync(string? stateSlug = null);
    Task<StateCountyHighlightsViewModel?> GetHighlightsAsync(string stateSlug);
    Task<IReadOnlyList<string>> GetPublishedPathsAsync();
}
