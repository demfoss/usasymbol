namespace USASymbol.Models
{
    public class SymbolContent
    {
        public string Html { get; set; } = string.Empty;
        public Dictionary<string, string>? Metadata { get; set; }
        public DateTime LastModified { get; set; }
    }
}