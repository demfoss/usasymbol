using USASymbol.Models.Content;

namespace USASymbol.Services.Interface
{
    public interface IFossilService
    {
        Task<FossilContent?> GetFossilContentAsync(string stateSlug, string contentFileName = "fossil.yaml");
    }
}
