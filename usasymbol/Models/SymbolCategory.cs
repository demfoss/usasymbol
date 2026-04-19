namespace USASymbol.Models
{
    public class SymbolCategory
    {
        public int Id { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }
    }

}
