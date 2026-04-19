using USASymbol.Models;

namespace usasymbol.Services.Interface
{
    public interface ISymbolCanonicalService
    {
        Task<Symbol?> ResolveCanonicalSymbolAsync(State state, string symbolType);
    }
}
