
using usasymbol.Models;

namespace USASymbol.Models.ViewModels
{

    public class StateBordersListViewModel
    {
        public List<StateBorderItem> Items { get; set; } = new();
        public int TotalStates { get; set; }
        public int CoastalStates { get; set; }
        public int LandlockedStates { get; set; }
        public int GreatLakesStates { get; set; }
        public int InternationalBorderStates { get; set; }
    }

    public class StateBorderItem
    {
        public State State { get; set; } = new();
        public int BorderCount { get; set; }
        public string NeighborsList { get; set; } = "";
        public bool IsLandlocked { get; set; }
        public bool HasOcean { get; set; }
        public bool HasGreatLakes { get; set; }
        public bool HasInternational { get; set; }
        public string MapImage { get; set; } = "";
    }
}
