using USASymbol.Models.Content;

namespace USASymbol.Services.Interface
{
    public interface ISurnamesService
    {
        Task<SurnamesContent?> GetSurnamesContentAsync(string stateSlug);
        IReadOnlyList<string> GetAvailableSlugs();
    }
}
