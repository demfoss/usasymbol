namespace USASymbol.Models.ViewModels
{
    public class SymbolCategoryViewModel
    {

        public string Type { get; set; } = string.Empty;


        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;


        public int StateCount { get; set; }


        public string Icon { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;

        public string? ImageUrl { get; set; } = string.Empty;
    }

}
