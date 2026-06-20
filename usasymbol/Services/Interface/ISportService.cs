using USASymbol.Models.Content;

namespace USASymbol.Services.Interface
{
    public interface ISportService
    {
        Task<SportContent?> GetSportContentAsync(string stateSlug, string contentFileName = "sport.yaml");
    }
}
