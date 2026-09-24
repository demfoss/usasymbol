using Microsoft.AspNetCore.WebUtilities;

namespace USASymbol.Models.ViewModels;

public sealed record RankingDirectoryEntry(
    string Title, string Url, string Description, string CategoryId, string CategoryTitle,
    string Topic, string Source, int? DataYear, DateTime? Updated, int FeaturedOrder, string HeroImage,
    IReadOnlyList<string>? Tags = null, IReadOnlyList<string>? RelatedUrls = null)
{
    public IReadOnlyList<string> TagList => Tags ?? [];
    public IReadOnlyList<string> RelatedUrlList => RelatedUrls ?? [];
}

public sealed record RankingCategoryGroup(string CategoryId, string CategoryTitle, IReadOnlyList<RankingDirectoryEntry> Entries);

/// <summary>Hand-written content for a sub-hub, stored in Content/ranking-hubs/*.yml. Only sub-hubs with this content are indexable.</summary>
public sealed class RankingHubContent
{
    public string Tag { get; set; } = "";
    public string? Category { get; set; }
    public string? H1 { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public List<string>? Intro { get; set; }
    public List<string>? Order { get; set; }
}

public sealed record RankingTagPage(string Label, string Slug, string Blurb,
    IReadOnlyList<RankingDirectoryEntry> Entries, IReadOnlyList<RankingCategoryGroup> Groups,
    IReadOnlyList<RankingChip> RelatedTags, RankingHubContent? Hub, string CanonicalSlug, RankingChip? Parent)
{
    public const int MinIndexableEntries = 3;
    public const double DuplicateSimilarity = 0.8;
    public string Url => $"/rankings/tag/{Slug}";
    public string CanonicalUrl => $"/rankings/tag/{CanonicalSlug}";
    public bool IsDuplicate => !string.Equals(CanonicalSlug, Slug, StringComparison.Ordinal);
    public bool IsIndexable => Hub != null && Entries.Count >= MinIndexableEntries && !IsDuplicate;
    public string Heading => string.IsNullOrWhiteSpace(Hub?.H1) ? $"{Label} Rankings by State" : Hub!.H1!;
}

public sealed record RankingChip(string Label, string Url, bool IsHashtag);

public sealed record RankingPageLinks(IReadOnlyList<RankingChip> Chips, string RelatedHeading,
    IReadOnlyList<RankingDirectoryEntry> Related, RankingChip? SubHub = null);

public sealed record RankingDirectoryCategory(string Id, string Title, string Icon, string Description,
    IReadOnlyList<RankingDirectoryEntry> Entries);

public sealed record RankingDirectoryFilter(string Value, string Label, int Count);

public sealed class RankingDirectoryViewModel
{
    public IReadOnlyList<RankingDirectoryCategory> Categories { get; init; } = [];
    public RankingDirectoryCategory? Category { get; init; }
    public IReadOnlyList<RankingDirectoryEntry> Items { get; init; } = [];
    public IReadOnlyList<RankingDirectoryEntry> Featured { get; init; } = [];
    public IReadOnlyList<RankingDirectoryFilter> Topics { get; init; } = [];
    public IReadOnlyList<int> Years { get; init; } = [];
    public string Query { get; init; } = "";
    public string Topic { get; init; } = "";
    public string Group { get; init; } = "";
    public string Year { get; init; } = "";
    public string Sort { get; init; } = "featured";
    public int Page { get; init; } = 1;
    public int TotalCount { get; init; }
    public int ResultCount { get; init; }
    public const int PageSize = 24;
    public int PageCount => Math.Max(1, (ResultCount + PageSize - 1) / PageSize);
    public string BasePath => Category == null ? "/rankings" : $"/rankings/{Category.Id}";
    public string Title => Category == null ? "U.S. State Rankings" : $"{Category.Title} Rankings";
    public bool HasFilters => Query.Length > 0 || Topic.Length > 0 || Group.Length > 0 || Year.Length > 0;
    public int ActiveFilterCount => new[] { Topic, Group, Year }.Count(value => value.Length > 0);
    public bool ShowOverview => Page == 1 && !HasFilters;

    public string Link(int page = 1, string? topic = null, string? group = null, string? year = null)
    {
        var values = new Dictionary<string, string?>();
        if (Query.Length > 0) values["q"] = Query;
        if ((topic ?? Topic).Length > 0) values["topic"] = topic ?? Topic;
        if ((group ?? Group).Length > 0) values["group"] = group ?? Group;
        if ((year ?? Year).Length > 0) values["year"] = year ?? Year;
        if (Sort != "featured") values["sort"] = Sort;
        if (page > 1) values["page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return QueryHelpers.AddQueryString(BasePath, values);
    }
}
