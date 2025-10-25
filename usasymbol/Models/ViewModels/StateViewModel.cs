namespace USASymbol.Models.ViewModels
{
    public class StateViewModel
    {
        public State State { get; set; } = new();
        public List<Symbol> Symbols { get; set; } = new();
        public List<State> RelatedStates { get; set; } = new();
    }
}