using USASymbol.Models.Content;

namespace usasymbol.Services.Interface
{
    public interface IMottoService
    {
            Task<MottoContent?> GetMottoContentAsync(string stateSlug);
    }
}
