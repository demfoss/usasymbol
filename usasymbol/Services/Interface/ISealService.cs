using USASymbol.Models.Content;

namespace usasymbol.Services.Interface
{
    public interface ISealService
    {
        Task<SealContent?> GetSealContentAsync(string stateSlug, string contentFileName = "state-seal.yaml");
    }
}
