using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class ParkCollectionViewModel
    {
        public ParkCollectionConfig Config { get; set; } = new();
        public List<ParkContent> Parks { get; set; } = new();

        public string? StateDisplayName { get; set; }
        public string? RegionDisplayName { get; set; }

        public bool IsStatePage => StateDisplayName != null;

        // Show all as cards when small list; cap at 10 for large ranking pages
        public int CardsLimit => (IsStatePage || Parks.Count <= 15) ? Parks.Count : 10;

        // Only show #N rank badge when sort order is meaningful (not plain alphabetical)
        public bool ShowRankBadge => Config.SortBy != "name";

        public string PageTitle => StateDisplayName != null
            ? $"National Parks in {StateDisplayName}"
            : RegionDisplayName != null
                ? $"National Parks in the {RegionDisplayName}"
                : Config.H1;
    }
}
