using USASymbol.Models.Content;

namespace usasymbol.Services.Interface
{
    public interface IFlagService
    {
        Task<FlagContent?> GetFlagContentAsync(string stateSlug);
    }
}