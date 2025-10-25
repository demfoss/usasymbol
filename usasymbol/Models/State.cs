namespace USASymbol.Models
{
    public class State
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty; // illinois, california
        public string Abbreviation { get; set; } = string.Empty; // IL, CA
        public string Capital { get; set; } = string.Empty;
        public int? Population { get; set; }
        public string? FlagImageUrl { get; set; }
        public string? Region { get; set; } // Midwest, West, South, Northeast
        public DateTime? StateHoodDate { get; set; }

        // Navigation
        public List<Symbol> Symbols { get; set; } = new();
    }
}