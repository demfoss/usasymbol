using USASymbol.Models;
using USASymbol.Models.Content;

namespace usasymbol.Services.Interface
{
    public interface IBeverageService
    {
        Task<BeverageContent?> GetBeverageContentAsync(string stateSlug, string symbolSlug);
        BeverageContent BuildFallbackContent(State state, Symbol symbol, IReadOnlyCollection<Symbol> stateBeverages);
    }
}
