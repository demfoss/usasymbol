namespace USASymbol.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<State> FeaturedStates { get; set; } = new();
        public List<SymbolCategory> SymbolCategories { get; set; } = new();
    }

    public class SymbolCategory
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}