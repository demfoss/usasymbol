using USASymbol.Models;
using USASymbol.Models.ViewModels;

namespace usasymbol.Services.Interface
{
    public interface ISymbolService
    {
        Task<List<Symbol>> GetSymbolsByStateAsync(int stateId);
        Task<Symbol?> GetSymbolAsync(int stateId, string symbolType);
        Task<List<SymbolWithState>> GetSymbolsByTypeAsync(string type);
        Task<Symbol?> GetSymbolBySlugAsync(int stateId, string slug);
        Task<List<string>> GetAllSymbolTypesAsync();
    }
}