namespace USASymbol.Models.ViewModels
{
    public class HomeStateMapItem
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Abbreviation { get; set; } = string.Empty;
        public string Capital { get; set; } = string.Empty;
        public int? Population { get; set; }
    }

    public class HomeViewModel
    {
        public List<State> FeaturedStates { get; set; } = new();
        public List<SymbolCategoryViewModel> SymbolCategories { get; set; } = new();
        public List<SymbolWithState> SymbolOfTheDayPool { get; set; } = new();
        public List<HomeStateMapItem> StateMapItems { get; set; } = new();
        public string HomeMapSvg { get; set; } = string.Empty;
    }
}
