using Microsoft.AspNetCore.WebUtilities;

namespace USASymbol.Models.ViewModels;

public sealed record CollectionsDirectoryEntry(
    string Title, string Url, string Description, string CategoryId, string CategoryTitle,
    string Topic, string Source, DateTime? Updated, int FeaturedOrder, string HeroImage);

public sealed record CollectionsDirectoryCategory(string Id, string Title, string Icon, string Description,
    IReadOnlyList<CollectionsDirectoryEntry> Entries);

public sealed class CollectionsDirectoryViewModel
{
    public IReadOnlyList<CollectionsDirectoryCategory> Categories { get; init; } = [];
    public CollectionsDirectoryCategory? Category { get; init; }
    public IReadOnlyList<CollectionsDirectoryEntry> Items { get; init; } = [];
    public IReadOnlyList<CollectionsDirectoryEntry> Featured { get; init; } = [];
    public IReadOnlyList<RankingDirectoryFilter> Topics { get; init; } = [];
    public string Query { get; init; } = "";
    public string Topic { get; init; } = "";
    public string Group { get; init; } = "";
    public string Sort { get; init; } = "featured";
    public int Page { get; init; } = 1;
    public int TotalCount { get; init; }
    public int ResultCount { get; init; }
    public const int PageSize = 24;
    public int PageCount => Math.Max(1, (ResultCount + PageSize - 1) / PageSize);
    public string BasePath => Category == null ? "/collections" : $"/collections/{Category.Id}";
    public string Title => Category == null ? "U.S. State Collections" : $"{Category.Title} Collections";
    public bool HasFilters => Query.Length > 0 || Topic.Length > 0 || Group.Length > 0;
    public int ActiveFilterCount => new[] { Topic, Group }.Count(value => value.Length > 0);
    public bool ShowOverview => Page == 1 && !HasFilters && Sort == "featured";

    public string Link(int page = 1, string? topic = null, string? group = null)
    {
        var values = new Dictionary<string, string?>();
        if (Query.Length > 0) values["q"] = Query;
        if ((topic ?? Topic).Length > 0) values["topic"] = topic ?? Topic;
        if ((group ?? Group).Length > 0) values["group"] = group ?? Group;
        if (Sort != "featured") values["sort"] = Sort;
        if (page > 1) values["page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return QueryHelpers.AddQueryString(BasePath, values);
    }
}
