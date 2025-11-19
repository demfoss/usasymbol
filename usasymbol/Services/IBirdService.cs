using USASymbol.Models.Content;

namespace USASymbol.Services
{
    public interface IBirdService
    {
        Task<BirdContent?> GetBirdContentAsync(string stateSlug);
    }
}