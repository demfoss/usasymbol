using USASymbol.Models;

namespace USASymbol.Models.ViewModels
{
    public class SurnamesHubViewModel
    {
        public IReadOnlyList<SurnamesHubItem> Items { get; set; } = [];
        public int AvailableCount => Items.Count(i => i.HasData);
    }

    public class SurnamesHubItem
    {
        public required State State { get; set; }
        public bool HasData { get; set; }
        public string? TopSurname { get; set; }
    }
}
