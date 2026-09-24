using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using USASymbol.Models.ViewModels;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace USASymbol.Services;

/// <summary>A cached metadata index; hub requests never build article tables, maps, or image galleries.</summary>
public sealed class CollectionsDirectoryService(IWebHostEnvironment environment, IMemoryCache cache,
    ILogger<CollectionsDirectoryService> logger) : IDisposable
{
    private const string CacheKey = "collections-directory-v1";
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly IDeserializer _reader = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance).IgnoreUnmatchedProperties().Build();

    private static readonly Dictionary<string, (string Icon, string Description)> CategoryCopy = new()
    {
        ["capitals"] = ("fa-building-columns", "Facts and history behind every U.S. state capital."),
        ["crime"] = ("fa-shield-halved", "Crime rates, gun violence, prisons, and public safety stories by state."),
        ["culture"] = ("fa-masks-theater", "Traditions, cuisine, and cultural identity across U.S. states."),
        ["flags"] = ("fa-flag", "Design, symbolism, and history behind every U.S. state flag."),
        ["geography"] = ("fa-earth-americas", "Land, water, climate, and the shape of the United States."),
        ["laws"] = ("fa-scale-balanced", "Unusual and noteworthy state laws and statutes."),
        ["nature"] = ("fa-mountain-sun", "Wildlife, weather, natural hazards, and the environment."),
        ["sports"] = ("fa-medal", "Sports participation, teams, and fan culture by state."),
    };

    private static readonly string[] FeaturedSlugs =
    [
        "strangest-food-laws-by-state", "best-and-worst-state-flags", "famous-inventions-by-state",
        "best-states-for-fall-foliage", "states-with-native-american-names", "smallest-state-capitals-by-population",
        "states-with-an-official-state-sport", "alabama"
    ];

    public async Task<IReadOnlyList<CollectionsDirectoryCategory>> GetCategoriesAsync()
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyList<CollectionsDirectoryCategory>? cached)) return cached!;
        await _loadLock.WaitAsync();
        try
        {
            if (cache.TryGetValue(CacheKey, out cached)) return cached!;
            var root = Path.Combine(environment.ContentRootPath, "Content", "collections");
            var categories = new List<CollectionsDirectoryCategory>();
            var changeToken = environment.ContentRootFileProvider.Watch("Content/collections/**/*");
            if (!Directory.Exists(root)) return categories;

            foreach (var folder in Directory.EnumerateDirectories(root).Order(StringComparer.Ordinal))
            {
                var id = Path.GetFileName(folder);
                var title = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace('-', ' '));
                var copy = CategoryCopy.GetValueOrDefault(id, ("fa-layer-group", $"Explore {title.ToLowerInvariant()} across U.S. states."));
                var files = Directory.EnumerateFiles(folder).Where(file =>
                    file.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)).ToList();

                // Files are read concurrently: sequential opens make this scan dominated
                // by per-file I/O latency rather than parsing time on slow disks.
                var entries = new System.Collections.Concurrent.ConcurrentBag<CollectionsDirectoryEntry>();
                await Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = 16 }, async (file, token) =>
                {
                    try
                    {
                        // Deserialize only summary fields. Large tables and article bodies are skipped.
                        var text = await File.ReadAllTextAsync(file, token);
                        var meta = _reader.Deserialize<CollectionMetadata>(new StringReader(text));
                        if (meta == null || string.IsNullOrWhiteSpace(meta.Page?.H1)) return;
                        var slug = Path.GetFileNameWithoutExtension(file);
                        var topic = string.IsNullOrWhiteSpace(meta.Subcategory) ? "General" : meta.Subcategory.Trim();
                        var source = meta.Page.Sources?.FirstOrDefault()?.Name ?? "";
                        var priority = Array.IndexOf(FeaturedSlugs, slug);
                        var url = string.IsNullOrWhiteSpace(meta.Url) ? $"/collections/{id}/{slug}" : meta.Url.Trim();
                        entries.Add(new CollectionsDirectoryEntry(meta.Page.H1,
                            url, meta.Seo?.Description ?? "", id, title,
                            topic, source, meta.DateModified ?? meta.DatePublished,
                            priority < 0 ? int.MaxValue : priority, meta.HeroImage ?? ""));
                    }
                    catch (Exception exception) when (exception is YamlDotNet.Core.YamlException or IOException)
                    {
                        logger.LogWarning(exception, "Could not index collection metadata: {File}", file);
                    }
                });
                if (entries.Count > 0) categories.Add(new(id, title, "fa-solid " + copy.Item1, copy.Item2,
                    entries.OrderBy(e => e.Title, StringComparer.OrdinalIgnoreCase).ToList()));
            }

            cache.Set(CacheKey, (IReadOnlyList<CollectionsDirectoryCategory>)categories,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)).AddExpirationToken(changeToken));
            return categories;
        }
        finally { _loadLock.Release(); }
    }

    public static CollectionsDirectoryViewModel Build(IReadOnlyList<CollectionsDirectoryCategory> categories,
        CollectionsDirectoryCategory? category, string? query, string? topic, string? group, string? sort, int page)
    {
        query = (query ?? "").Trim();
        if (query.Length > 200) query = query[..200];
        topic = (topic ?? "").Trim();
        group = category == null ? (group ?? "").Trim() : "";
        sort = sort is "az" or "updated" ? sort : "featured";
        var all = (category?.Entries ?? categories.SelectMany(item => item.Entries).ToList()).ToList();
        topic = all.FirstOrDefault(item => string.Equals(item.Topic, topic, StringComparison.OrdinalIgnoreCase))?.Topic ?? topic;
        group = categories.FirstOrDefault(item => string.Equals(item.Id, group, StringComparison.OrdinalIgnoreCase))?.Id ?? group;
        IEnumerable<CollectionsDirectoryEntry> filtered = all;
        if (query.Length > 0)
        {
            var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            filtered = filtered.Where(item => terms.All(term =>
                $"{item.Title} {item.Description} {item.CategoryTitle} {item.Topic} {item.Source}"
                    .Contains(term, StringComparison.OrdinalIgnoreCase)));
        }
        if (topic.Length > 0) filtered = filtered.Where(item => string.Equals(item.Topic, topic, StringComparison.OrdinalIgnoreCase));
        if (group.Length > 0) filtered = filtered.Where(item => string.Equals(item.CategoryId, group, StringComparison.OrdinalIgnoreCase));

        var ordered = sort switch
        {
            "az" => filtered.OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase),
            "updated" => filtered.OrderByDescending(item => item.Updated).ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase),
            _ => filtered.OrderBy(item => item.FeaturedOrder).ThenByDescending(item => item.Updated)
                .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
        };
        var result = ordered.ToList();
        page = Math.Max(1, page);
        return new CollectionsDirectoryViewModel
        {
            Categories = categories, Category = category, Query = query, Topic = topic, Group = group,
            Sort = sort, Page = page, TotalCount = all.Count, ResultCount = result.Count,
            Items = result.Skip((int)Math.Min((long)(page - 1) * CollectionsDirectoryViewModel.PageSize, int.MaxValue)).Take(CollectionsDirectoryViewModel.PageSize).ToList(),
            Featured = all.OrderBy(item => item.FeaturedOrder).ThenByDescending(item => item.Updated)
                .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase).Take(6).ToList(),
            Topics = all.GroupBy(item => item.Topic, StringComparer.OrdinalIgnoreCase)
                .Select(items => new RankingDirectoryFilter(items.Key, items.Key, items.Count()))
                .OrderBy(item => item.Label, StringComparer.OrdinalIgnoreCase).ToList(),
        };
    }

    public void Dispose() => _loadLock.Dispose();

    // These DTOs intentionally have no row, section, image, or map properties.
    public sealed class CollectionMetadata
    {
        public string? Url { get; set; }
        public string? Subcategory { get; set; }
        public DateTime? DatePublished { get; set; }
        public DateTime? DateModified { get; set; }
        public string? HeroImage { get; set; }
        public MetadataSeo? Seo { get; set; }
        public MetadataPage? Page { get; set; }
    }
    public sealed class MetadataSeo { public string? Description { get; set; } }
    public sealed class MetadataPage
    {
        public string H1 { get; set; } = "";
        public List<MetadataSource>? Sources { get; set; }
    }
    public sealed class MetadataSource { public string? Name { get; set; } }
}
