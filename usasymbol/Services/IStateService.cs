using USASymbol.Models;

namespace USASymbol.Services
{
    public interface IStateService
    {
        Task<List<State>> GetAllStatesAsync();
        Task<State?> GetStateBySlugAsync(string slug);
        Task<List<State>> GetStatesByRegionAsync(string region);
        Task<List<State>> GetFeaturedStatesAsync(int count = 5);
    }
}