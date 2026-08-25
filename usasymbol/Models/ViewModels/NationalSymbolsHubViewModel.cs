using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public sealed class NationalSymbolsHubViewModel
    {
        public IReadOnlyList<PageCategoryItem> Items { get; init; } = Array.Empty<PageCategoryItem>();
    }
}
