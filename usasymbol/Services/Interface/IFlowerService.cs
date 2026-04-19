using USASymbol.Models.Content;

namespace usasymbol.Services.Interface
{
    public interface IFlowerService
    {
        Task<FlowerContent?> GetFlowerContentAsync(string stateSlug);
    }
}