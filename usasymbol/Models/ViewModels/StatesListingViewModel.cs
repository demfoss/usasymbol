using USASymbol.Models;

namespace USASymbol.Models.ViewModels
{
    public class StatesListingViewModel
    {
        public List<State> States { get; set; } = new();
        public string? SelectedRegion { get; set; }
        public List<string> Regions { get; set; } = new();
    }
}