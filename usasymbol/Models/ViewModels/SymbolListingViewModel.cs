namespace USASymbol.Models.ViewModels
{
    public class SymbolListingViewModel
    {
        public string SymbolType { get; set; } = string.Empty;
        public string SymbolTypeName { get; set; } = string.Empty;
        public List<SymbolWithState> Symbols { get; set; } = new();
        public List<string> AvailableStates { get; set; } = new();
        public string? SelectedState { get; set; }
    }

    public class SymbolWithState
    {
        public Symbol Symbol { get; set; } = new();
        public State State { get; set; } = new();
    }
}