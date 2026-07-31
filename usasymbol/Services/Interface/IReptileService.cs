using USASymbol.Models.Content;

namespace USASymbol.Services.Interface
{
    public interface IReptileService
    {
        Task<ReptileContent?> GetReptileContentAsync(string stateSlug, string contentFileName = "reptile.yaml");
    }
}
