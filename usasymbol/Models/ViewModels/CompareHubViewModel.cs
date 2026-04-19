namespace USASymbol.Models.ViewModels
{
    public class CompareHubViewModel
    {
        public List<State> States { get; set; } = new();

        /// <summary>Popular comparison pairs shown as quick links.</summary>
        public List<(string Slug1, string Name1, string Slug2, string Name2)> FeaturedPairs { get; set; } = new();
    }
}
