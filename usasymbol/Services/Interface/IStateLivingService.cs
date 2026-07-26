using USASymbol.Models.ViewModels;

namespace USASymbol.Services.Interface;

public interface IStateLivingService
{
    Task<StateLivingViewModel?> GetAsync(string stateSlug);
    Task<StateLivingHubViewModel> GetHubAsync();
}
