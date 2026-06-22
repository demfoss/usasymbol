namespace Usasymbol.ViewModels
{
    public record MapEntryDetail(string Label, string Value, string? Group = null);

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
        string? FlagImageUrl = null,
        IReadOnlyList<StateMapEntryItem>? Items = null,
        string? Summary = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? Filters = null
    );
}
