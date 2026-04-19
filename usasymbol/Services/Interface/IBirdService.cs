using USASymbol.Models.Content;

namespace usasymbol.Services.Interface
{
    public interface IBirdService
    {
        Task<BirdContent?> GetBirdContentAsync(string stateSlug);
    }
}