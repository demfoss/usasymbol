namespace USASymbol.Models.ViewModels
{
    public class StateRankRow
    {
        public string StateName { get; set; } = "";
        public string StateSlug { get; set; } = "";
        public string Abbreviation { get; set; } = "";
        public double Value { get; set; }
        public string FormattedValue { get; set; } = "";
        public int Rank { get; set; }
        public string? FlagImageUrl { get; set; }
    }
}
