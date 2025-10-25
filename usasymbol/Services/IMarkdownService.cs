using USASymbol.Models;

namespace USASymbol.Services
{
    public interface IMarkdownService
    {
        Task<SymbolContent?> GetSymbolContentAsync(string state, string symbolType);
    }
}