using USASymbol.Models.Content;

namespace usasymbol.Services.Interface
{
    public interface IFirearmService
    {
        Task<FirearmContent?> GetFirearmContentAsync(string stateSlug);
    }
}
