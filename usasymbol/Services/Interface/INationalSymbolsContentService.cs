using USASymbol.Models.Content;

namespace USASymbol.Services.Interface
{
    public interface INationalSymbolsContentService
    {
        Task<IReadOnlyList<PageCategoryItem>> GetAllAsync();
        Task<PageContent?> GetContentAsync(string slug);
        Task<T?> GetTypedContentAsync<T>(string slug) where T : class;
    }
}
