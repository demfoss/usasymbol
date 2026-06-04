using USASymbol.Models.Content;

namespace USASymbol.Services.Interface
{
    public interface IStateAbbreviationContentService
    {
        Task<StateAbbreviationContent?> GetContentAsync(string stateSlug);
        Task<IReadOnlyList<string>> GetHighIntentSlugsAsync();
    }
}
