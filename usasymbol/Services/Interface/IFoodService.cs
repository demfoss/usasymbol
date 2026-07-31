using USASymbol.Models.Content;

namespace USASymbol.Services.Interface
{
    public interface IFoodService
    {
        Task<FoodContent?> GetFoodContentAsync(string stateSlug, string contentFileName = "food.yaml");
    }
}
