using USASymbol.Models;
using USASymbol.Models.ViewModels;

namespace USASymbol.Services
{
    public interface ISymbolService
    {
        Task<List<Symbol>> GetSymbolsByStateAsync(int stateId);
        Task<Symbol?> GetSymbolAsync(int stateId, string symbolType);
        Task<List<SymbolWithState>> GetSymbolsByTypeAsync(string type);
        Task<List<string>> GetAllSymbolTypesAsync();
    }
}