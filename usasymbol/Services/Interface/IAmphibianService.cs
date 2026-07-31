using USASymbol.Models.Content;

namespace USASymbol.Services.Interface
{
    public interface IAmphibianService
    {
        Task<AmphibianContent?> GetAmphibianContentAsync(string stateSlug, string contentFileName = "amphibian.yaml");
    }
}
