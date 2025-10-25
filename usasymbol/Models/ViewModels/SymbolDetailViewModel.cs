namespace USASymbol.Models.ViewModels
{
    public class SymbolDetailViewModel
    {
        public State State { get; set; } = new();
        public Symbol Symbol { get; set; } = new();
        public SymbolContent Content { get; set; } = new();
        public List<Symbol> RelatedSymbols { get; set; } = new();
    }
}