using USASymbol.Models.Content;

namespace usasymbol.Services.Interface
{
    public interface ITreeService
    {
        Task<TreeContent?> GetTreeContentAsync(string stateSlug);
    }
}