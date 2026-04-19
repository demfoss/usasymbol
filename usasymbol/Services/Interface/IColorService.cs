using USASymbol.Models.Content;

namespace usasymbol.Services.Interface
{
    public interface IColorService
    {
        Task<ColorContent?> GetColorContentAsync(string stateSlug, string symbolSlug);
    }
}