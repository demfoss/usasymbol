using USASymbol.Models.Content;

namespace USASymbol.Services.Interface
{
    public interface IBorderService
    {
        Task<BorderContent?> GetBorderContentAsync(string stateSlug);
    }
}