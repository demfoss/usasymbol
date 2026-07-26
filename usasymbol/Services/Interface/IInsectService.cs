using USASymbol.Models.Content;

namespace USASymbol.Services.Interface
{
    public interface IInsectService
    {
        Task<InsectContent?> GetInsectContentAsync(string stateSlug, string contentFileName = "insect.yaml");
    }
}
