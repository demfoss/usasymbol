namespace Usasymbol.ViewModels
{
    public record MapEntryDetail(string Label, string Value);

    public record StateMapEntryItem(
        string DisplayValue,
        IReadOnlyList<MapEntryDetail> Details,
        string? ImageUrl = null
    );

    public record StateMapEntry(
        string PostalCode,
        string StateName,
        string StateSlug,
        double? NumericValue,
        int? Rank,
        string DisplayValue,
        string FillColor,
        IReadOnlyList<MapEntryDetail> Details,
        string? ImageUrl = null,
        IReadOnlyList<StateMapEntryItem>? Items = null
    );
}
