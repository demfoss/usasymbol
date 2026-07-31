using USASymbol.Models.Content;

namespace USASymbol.Services.Interface
{
    public interface IMineralService
    {
        Task<MineralContent?> GetMineralContentAsync(string stateSlug, string contentFileName = "mineral.yaml");
    }
}
