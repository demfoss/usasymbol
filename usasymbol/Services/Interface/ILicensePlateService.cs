using USASymbol.Models;
using USASymbol.Models.Content;

namespace usasymbol.Services.Interface
{
    public interface ILicensePlateService
    {
        Task<LicensePlateContent?> GetLicensePlateContentAsync(string stateSlug, string symbolSlug);
        LicensePlateContent BuildFallbackContent(State state, Symbol symbol, IReadOnlyCollection<Symbol> stateSlogans);
    }
}
