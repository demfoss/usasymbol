using Usasymbol.ViewModels;

namespace USASymbol.Services.Interface
{
    public interface IMapPngService
    {
        Task<string?> EnsureMapPngAsync(string slug, IReadOnlyList<StateMapEntry> entries);

        /// <summary>
        /// A download-ready picture: title, map, colour legend and a credit line with the page URL baked in,
        /// so a screenshot or repost still carries the source. Cached on disk by content hash.
        /// </summary>
        Task<string?> EnsureSharePngAsync(string slug, string title, IReadOnlyList<StateMapEntry> entries,
            IReadOnlyList<MapLegendGroup> legend, string? sourceName, string pageUrl);
    }
}
