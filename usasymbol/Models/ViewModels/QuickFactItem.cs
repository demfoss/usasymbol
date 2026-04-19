namespace USASymbol.Models.ViewModels
{
    public sealed class QuickFactItem
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";

        public bool Italic { get; set; } = false;


        public string? Url { get; set; }
    }
}
