namespace USASymbol.Models.ViewModels
{
    public class LatestContentRailItemViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Eyebrow { get; set; } = string.Empty;
        public string SectionLabel { get; set; } = string.Empty;
        public System.DateTime? DateModified { get; set; }
    }

    public class LatestContentRailViewModel
    {
        public string Title { get; set; } = "You Might Also Like";
        public IReadOnlyList<LatestContentRailItemViewModel> Items { get; set; } = new List<LatestContentRailItemViewModel>();
    }
}
