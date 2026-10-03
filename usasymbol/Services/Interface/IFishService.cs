using USASymbol.Models.Content;

namespace USASymbol.Services.Interface
{
    public interface IFishService
    {
        Task<FishContent?> GetFishContentAsync(string stateSlug, string contentFileName = "fish.yaml");
    }
}
