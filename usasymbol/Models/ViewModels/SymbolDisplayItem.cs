namespace USASymbol.Models.ViewModels
{
    public sealed class SymbolDisplayItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Image { get; set; } = "";
        public IReadOnlyList<string> Paragraphs { get; set; } = Array.Empty<string>();
    }
}
