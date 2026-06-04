using USASymbol.Models.Content;

namespace USASymbol.Services.Interface
{
    public interface IParkService
    {
        Task<ParkContent?> GetParkAsync(string parkSlug);
        Task<List<ParkContent>> GetAllNationalParksAsync();
        Task<ParkCollectionConfig?> GetCollectionConfigAsync(string slug);
        Task<List<ParkContent>> GetCollectionParksAsync(ParkCollectionConfig config);
    }
}
