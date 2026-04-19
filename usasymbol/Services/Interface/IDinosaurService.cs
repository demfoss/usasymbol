using USASymbol.Models.Content;

namespace usasymbol.Services.Interface
{
    public interface IDinosaurService
    {
        Task<DinosaurContent?> GetDinosaurContentAsync(string stateSlug);
    }
}
