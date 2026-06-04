using USASymbol.Models.Content;

namespace USASymbol.Services.Interface
{
    public interface ISoilService
    {
        Task<SoilContent?> GetSoilContentAsync(string stateSlug, string contentFileName = "soil.yaml");
    }
}
