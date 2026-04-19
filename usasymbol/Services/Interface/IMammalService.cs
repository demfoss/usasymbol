using USASymbol.Models.Content;

namespace usasymbol.Services.Interface
{
    public interface IMammalService
    {
        Task<MammalContent?> GetMammalContentAsync(string stateSlug, string symbolSlug);
    }
}
