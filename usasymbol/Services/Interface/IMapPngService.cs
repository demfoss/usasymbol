using Usasymbol.ViewModels;

namespace USASymbol.Services.Interface
{
    public interface IMapPngService
    {
        Task<string?> EnsureMapPngAsync(string slug, IReadOnlyList<StateMapEntry> entries);
    }
}
