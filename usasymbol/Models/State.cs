namespace USASymbol.Models
{
    public class State
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Abbreviation { get; set; } = string.Empty;
        public string Capital { get; set; } = string.Empty;
        public int? Population { get; set; }
        public string? FlagImageUrl { get; set; }
        public string? Region { get; set; }
        public DateTime? StateHoodDate { get; set; }


        public List<Symbol> Symbols { get; set; } = new();
    }
}