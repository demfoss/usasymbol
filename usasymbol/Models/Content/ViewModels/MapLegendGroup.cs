namespace Usasymbol.ViewModels;

public sealed record MapLegendGroup(
    string Key,
    string Label,
    string Range,
    string Color,
    IReadOnlyList<string> StateCodes,
    bool IsNoData = false,
    string? Description = null)
{
    public string Tooltip => Description ?? (IsNoData
        ? $"No data: {string.Join(", ", StateCodes)}"
        : $"{(string.IsNullOrEmpty(Range) ? Label : Range)}: {StateCodes.Count} {(StateCodes.Count == 1 ? "state" : "states")}");
}
