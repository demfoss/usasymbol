namespace Usasymbol.ViewModels
{
    public record StateMapEntry(
        string PostalCode,
        string StateName,
        string StateSlug,
        double? NumericValue,
        int? Rank,
        string DisplayValue,
        string FillColor,
        string? SubLabel = null
    );
}
