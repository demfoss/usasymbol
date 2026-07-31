using USASymbol.Models.Content;

namespace USASymbol.Services.Interface
{
    public interface IDanceService
    {
        Task<DanceContent?> GetDanceContentAsync(string stateSlug, string symbolSlug);
    }
}
