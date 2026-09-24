using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using USASymbol.Models.ViewModels;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace USASymbol.Services;

/// <summary>A cached metadata index; hub requests never build article tables, maps, or image galleries.</summary>
public sealed class RankingDirectoryService(IWebHostEnvironment environment, IMemoryCache cache,
    ILogger<RankingDirectoryService> logger) : IDisposable
{
    private const string CacheKey = "ranking-directory-v1";
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly IDeserializer _hubReader = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance).IgnoreUnmatchedProperties().Build();
    private readonly IDeserializer _reader = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance).IgnoreUnmatchedProperties().Build();

    private static readonly Dictionary<string, (string Icon, string Description)> CategoryCopy = new()
    {
        ["agriculture"] = ("fa-wheat-awn", "Crops, livestock, farmland, trade, and the agricultural economy."),
        ["cars-and-roads"] = ("fa-car-side", "Driving laws, vehicle requirements, insurance, and road rules across the states."),
        ["crime"] = ("fa-shield-halved", "Crime, public safety, policing, and justice across the states."),
        ["culture"] = ("fa-masks-theater", "Arts, traditions, entertainment, and American life."),
        ["demographics"] = ("fa-users", "Population, age, households, migration, and communities."),
        ["economy"] = ("fa-chart-line", "Income, jobs, housing costs, business, and affordability."),
        ["education"] = ("fa-graduation-cap", "Schools, colleges, literacy, and educational outcomes."),
        ["food"] = ("fa-utensils", "Food habits, restaurants, drinks, and regional tastes."),
        ["geography"] = ("fa-earth-americas", "Land, water, climate, and the shape of the United States."),
        ["government"] = ("fa-landmark", "Elections, civic life, public policy, and government."),
        ["health"] = ("fa-heart-pulse", "Health outcomes, access to care, and well-being."),
        ["infrastructure"] = ("fa-road", "Roads, transportation, energy, and connectivity."),
        ["law"] = ("fa-scale-balanced", "State laws, rights, regulations, and legal differences."),
        ["nature"] = ("fa-mountain-sun", "Wildlife, weather, natural hazards, and the environment."),
        ["religion"] = ("fa-place-of-worship", "Religious communities, beliefs, and places of worship."),
        ["sports"] = ("fa-medal", "Teams, participation, outdoor recreation, and fan culture."),
        ["taxes"] = ("fa-receipt", "Income, property, sales, and other state taxes.")
    };
    private static readonly Dictionary<string, string> CategoryTitles = new()
    {
        ["cars-and-roads"] = "Cars & Roads"
    };
    private static readonly string[] FeaturedSlugs =
    [
        "states-by-population", "states-by-cost-of-living", "crime-rate-by-state",
        "agricultural-gdp-by-state", "agricultural-exports-by-state", "farmland-acre-value-by-state"
    ];

    public async Task<IReadOnlyList<RankingDirectoryCategory>> GetCategoriesAsync()
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyList<RankingDirectoryCategory>? cached)) return cached!;
        await _loadLock.WaitAsync();
        try
        {
            if (cache.TryGetValue(CacheKey, out cached)) return cached!;
            var root = Path.Combine(environment.ContentRootPath, "Content", "rankings");
            var categories = new List<RankingDirectoryCategory>();
            var additionalHubEntries = new System.Collections.Concurrent.ConcurrentDictionary<string,
                System.Collections.Concurrent.ConcurrentBag<RankingDirectoryEntry>>(StringComparer.OrdinalIgnoreCase);
            var changeToken = environment.ContentRootFileProvider.Watch("Content/rankings/**/*");
            if (!Directory.Exists(root)) return categories;

            foreach (var folder in Directory.EnumerateDirectories(root).Order(StringComparer.Ordinal))
            {
                var id = Path.GetFileName(folder);
                var title = GetCategoryTitle(id);
                var copy = CategoryCopy.GetValueOrDefault(id, ("fa-chart-column", $"Explore {title.ToLowerInvariant()} across U.S. states."));
                var files = Directory.EnumerateFiles(folder).Where(file =>
                    file.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)).ToList();

                // Files are read concurrently: with hundreds of small YAML files per
                // category, sequential opens make this scan dominated by per-file I/O
                // latency rather than parsing time (slow disks, network-backed content).
                var entries = new System.Collections.Concurrent.ConcurrentBag<RankingDirectoryEntry>();
                await Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = 16 }, async (file, token) =>
                {
                    try
                    {
                        // Deserialize only summary fields. Large tables and article bodies are skipped.
                        var text = await File.ReadAllTextAsync(file, token);
                        var meta = _reader.Deserialize<RankingMetadata>(new StringReader(text));
                        if (meta == null || string.IsNullOrWhiteSpace(meta.Page?.H1)) return;
                        var slug = Path.GetFileNameWithoutExtension(file);
                        var topic = GetTopic(id, slug, meta.Subcategory);
                        var tags = RankingTopicTags.Derive(slug, meta.Tags).ToList();
                        if (topic == "General" && tags.Count > 0) topic = tags[0];
                        else if (topic != "General" && topic != "Other Agriculture" &&
                                 !tags.Contains(topic, StringComparer.OrdinalIgnoreCase)) tags.Insert(0, topic);
                        var relatedUrls = (meta.Related ?? []).Select(r => r.Url?.Trim() ?? "")
                            .Where(u => u.StartsWith('/')).ToList();
                        var source = meta.Page.Sources?.FirstOrDefault()?.Name ?? "";
                        var dataYear = meta.DataYear ?? SingleYear(meta.Table?.Note)
                            ?? SingleYear(meta.Page.Methodology);
                        var priority = Array.IndexOf(FeaturedSlugs, slug);
                        var entry = new RankingDirectoryEntry(meta.Page.H1,
                            $"/rankings/{id}/{slug}", meta.Seo?.Description ?? "", id, title,
                            topic, source, dataYear, meta.DateModified ?? meta.DatePublished,
                            priority < 0 ? int.MaxValue : priority, meta.HeroImage ?? "", tags, relatedUrls);
                        entries.Add(entry);

                        foreach (var hubId in meta.HubCategories ?? [])
                        {
                            var normalizedHubId = hubId.Trim().ToLowerInvariant();
                            if (normalizedHubId.Length == 0 || string.Equals(normalizedHubId, id, StringComparison.OrdinalIgnoreCase)) continue;
                            var hubTitle = GetCategoryTitle(normalizedHubId);
                            var hubEntry = entry with { CategoryId = normalizedHubId, CategoryTitle = hubTitle };
                            additionalHubEntries.GetOrAdd(normalizedHubId, _ => []).Add(hubEntry);
                        }
                    }
                    catch (Exception exception) when (exception is YamlDotNet.Core.YamlException or IOException)
                    {
                        logger.LogWarning(exception, "Could not index ranking metadata: {File}", file);
                    }
                });
                if (entries.Count > 0) categories.Add(new(id, title, "fa-solid " + copy.Item1, copy.Item2,
                    entries.OrderBy(e => e.Title, StringComparer.OrdinalIgnoreCase).ToList()));
            }

            foreach (var (hubId, hubEntries) in additionalHubEntries)
            {
                var existingIndex = categories.FindIndex(category =>
                    string.Equals(category.Id, hubId, StringComparison.OrdinalIgnoreCase));
                var mergedEntries = (existingIndex >= 0 ? categories[existingIndex].Entries : [])
                    .Concat(hubEntries)
                    .GroupBy(entry => entry.Url, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .OrderBy(entry => entry.Title, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var title = GetCategoryTitle(hubId);
                var copy = CategoryCopy.GetValueOrDefault(hubId,
                    ("fa-chart-column", $"Explore {title.ToLowerInvariant()} across U.S. states."));
                var hubCategory = new RankingDirectoryCategory(hubId, title, "fa-solid " + copy.Item1,
                    copy.Item2, mergedEntries);
                if (existingIndex >= 0) categories[existingIndex] = hubCategory;
                else categories.Add(hubCategory);
            }

            categories = categories.OrderBy(category => category.Title, StringComparer.OrdinalIgnoreCase).ToList();

            cache.Set(CacheKey, (IReadOnlyList<RankingDirectoryCategory>)categories,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)).AddExpirationToken(changeToken));
            return categories;
        }
        finally { _loadLock.Release(); }
    }

    private static Dictionary<string, int> TagCounts(IEnumerable<RankingDirectoryEntry> entries) => entries
        .SelectMany(e => e.TagList.Distinct(StringComparer.OrdinalIgnoreCase))
        .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

    private static List<RankingDirectoryEntry> DistinctEntries(IReadOnlyList<RankingDirectoryCategory> categories) =>
        categories.SelectMany(c => c.Entries)
            .GroupBy(e => e.Url, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.FirstOrDefault(e => e.Url.StartsWith($"/rankings/{e.CategoryId}/", StringComparison.OrdinalIgnoreCase)) ?? g.First())
            .ToList();

    private async Task<IReadOnlyDictionary<string, RankingHubContent>> GetHubsAsync()
    {
        const string hubsKey = "ranking-hubs-v1";
        if (cache.TryGetValue(hubsKey, out IReadOnlyDictionary<string, RankingHubContent>? cached)) return cached!;

        var hubs = new Dictionary<string, RankingHubContent>(StringComparer.OrdinalIgnoreCase);
        var folder = Path.Combine(environment.ContentRootPath, "Content", "ranking-hubs");
        if (Directory.Exists(folder))
        {
            foreach (var file in Directory.EnumerateFiles(folder, "*.yml"))
            {
                try
                {
                    var hub = _hubReader.Deserialize<RankingHubContent>(await File.ReadAllTextAsync(file));
                    if (hub != null && !string.IsNullOrWhiteSpace(hub.Tag)) hubs[RankingTopicTags.Slug(hub.Tag)] = hub;
                }
                catch (Exception exception) when (exception is YamlDotNet.Core.YamlException or IOException)
                {
                    logger.LogWarning(exception, "Could not read ranking hub: {File}", file);
                }
            }
        }

        cache.Set(hubsKey, (IReadOnlyDictionary<string, RankingHubContent>)hubs, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
            .AddExpirationToken(environment.ContentRootFileProvider.Watch("Content/ranking-hubs/**/*")));
        return hubs;
    }

    private async Task<Dictionary<string, RankingTagPage>> BuildTagPagesAsync()
    {
        var categories = await GetCategoriesAsync();
        var hubs = await GetHubsAsync();
        var all = DistinctEntries(categories);
        var counts = TagCounts(all);
        var categoryTitles = categories.ToDictionary(c => c.Id, c => c.Title, StringComparer.OrdinalIgnoreCase);

        var members = counts.Keys.ToDictionary(
            label => label,
            label => all.Where(e => e.TagList.Contains(label, StringComparer.OrdinalIgnoreCase)).ToList(),
            StringComparer.OrdinalIgnoreCase);

        string CanonicalFor(string label)
        {
            var urls = members[label].Select(e => e.Url).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var slug = RankingTopicTags.Slug(label);
            var best = slug;
            var bestCount = urls.Count;
            foreach (var (other, otherEntries) in members)
            {
                var otherSlug = RankingTopicTags.Slug(other);
                if (otherSlug == slug) continue;
                var otherUrls = otherEntries.Select(e => e.Url).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var union = urls.Union(otherUrls, StringComparer.OrdinalIgnoreCase).Count();
                var jaccard = union == 0 ? 0 : (double)urls.Intersect(otherUrls, StringComparer.OrdinalIgnoreCase).Count() / union;
                var otherWins = otherUrls.Count > urls.Count || (otherUrls.Count == urls.Count && string.CompareOrdinal(otherSlug, slug) < 0);
                if (jaccard >= RankingTagPage.DuplicateSimilarity && otherWins && otherUrls.Count > bestCount)
                {
                    best = otherSlug;
                    bestCount = otherUrls.Count;
                }
            }
            return best;
        }

        var pages = new Dictionary<string, RankingTagPage>(StringComparer.OrdinalIgnoreCase);
        foreach (var (label, tagEntries) in members)
        {
            var slug = RankingTopicTags.Slug(label);
            hubs.TryGetValue(slug, out var hub);
            var order = (hub?.Order ?? []).Select((s, i) => (s, i)).ToDictionary(x => x.s, x => x.i, StringComparer.OrdinalIgnoreCase);
            var entries = tagEntries
                .OrderBy(e => order.GetValueOrDefault(e.Url[(e.Url.LastIndexOf('/') + 1)..], int.MaxValue))
                .ThenBy(e => e.FeaturedOrder).ThenBy(e => e.Title, StringComparer.OrdinalIgnoreCase).ToList();
            var groups = entries.GroupBy(e => e.CategoryId, StringComparer.OrdinalIgnoreCase)
                .Select(g => new RankingCategoryGroup(g.Key, g.First().CategoryTitle, g.ToList()))
                .OrderByDescending(g => g.Entries.Count).ThenBy(g => g.CategoryTitle, StringComparer.OrdinalIgnoreCase).ToList();
            var related = entries.SelectMany(e => e.TagList).Where(t => !string.Equals(t, label, StringComparison.OrdinalIgnoreCase))
                .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
                .Where(g => counts.GetValueOrDefault(g.Key) >= RankingTagPage.MinIndexableEntries)
                .OrderByDescending(g => hubs.ContainsKey(RankingTopicTags.Slug(g.Key))).ThenByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase).Take(8)
                .Select(g => new RankingChip(g.Key, $"/rankings/tag/{RankingTopicTags.Slug(g.Key)}", true)).ToList();
            RankingChip? parent = !string.IsNullOrWhiteSpace(hub?.Category) && categoryTitles.TryGetValue(hub.Category, out var parentTitle)
                ? new RankingChip(parentTitle, $"/rankings/{hub.Category}", false) : null;

            pages[slug] = new RankingTagPage(label, slug, RankingTopicTags.Blurb(label), entries, groups, related,
                hub, CanonicalFor(label), parent);
        }
        return pages;
    }

    public async Task<IReadOnlyList<string>> GetIndexableTagUrlsAsync() =>
        (await BuildTagPagesAsync()).Values.Where(p => p.IsIndexable).Select(p => p.Url)
            .OrderBy(u => u, StringComparer.Ordinal).ToList();

    public async Task<RankingTagPage?> GetTagPageAsync(string tagSlug)
    {
        var pages = await BuildTagPagesAsync();
        return pages.TryGetValue(tagSlug, out var page) ? page : null;
    }

    public async Task<RankingPageLinks> GetPageLinksAsync(string category, string slug, int take = 6)
    {
        var categories = await GetCategoriesAsync();
        var selfUrl = $"/rankings/{category}/{slug}";
        var all = DistinctEntries(categories);
        var self = all.FirstOrDefault(e => string.Equals(e.Url, selfUrl, StringComparison.OrdinalIgnoreCase));
        if (self == null) return new RankingPageLinks([], "", []);

        var hubs = await GetHubsAsync();
        var tagCounts = TagCounts(all);
        var linkableTags = self.TagList.Where(tag => tagCounts.GetValueOrDefault(tag) >= RankingTagPage.MinIndexableEntries)
            .OrderByDescending(tag => hubs.ContainsKey(RankingTopicTags.Slug(tag))).ToList();
        var chips = linkableTags
            .Select(tag => new RankingChip(tag, $"/rankings/tag/{RankingTopicTags.Slug(tag)}", true)).ToList();
        if (chips.Count == 0)
            chips.Add(new RankingChip(self.CategoryTitle, $"/rankings/{self.CategoryId}", true));

        RankingChip? subHub = null;
        foreach (var tag in linkableTags)
        {
            if (hubs.TryGetValue(RankingTopicTags.Slug(tag), out var hub) &&
                string.Equals(hub.Category, self.CategoryId, StringComparison.OrdinalIgnoreCase))
            {
                subHub = new RankingChip(tag, $"/rankings/tag/{RankingTopicTags.Slug(tag)}", false);
                break;
            }
        }

        var selfTokens = RankingTopicTags.SlugTokens(slug);
        var selfTags = self.TagList.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var explicitOrder = self.RelatedUrlList.Select((url, index) => (url, index))
            .ToDictionary(x => x.url, x => x.index, StringComparer.OrdinalIgnoreCase);

        var scored = all.Where(e => !string.Equals(e.Url, selfUrl, StringComparison.OrdinalIgnoreCase))
            .Select(e =>
            {
                var score = 0;
                if (explicitOrder.TryGetValue(e.Url, out var position)) score += 100 - position;
                else if (e.RelatedUrlList.Contains(selfUrl, StringComparer.OrdinalIgnoreCase)) score += 20;
                var shared = e.TagList.Count(tag => selfTags.Contains(tag));
                score += shared * 12;
                score += RankingTopicTags.SlugTokens(e.Url[(e.Url.LastIndexOf('/') + 1)..]).Count(selfTokens.Contains) * 3;
                if (string.Equals(e.CategoryId, self.CategoryId, StringComparison.OrdinalIgnoreCase)) score += 2;
                return (Entry: e, Score: score, Shared: shared);
            })
            .OrderByDescending(x => x.Score).ThenByDescending(x => x.Entry.Updated).ThenBy(x => x.Entry.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var related = scored.Where(x => x.Score >= 5).Take(take).ToList();
        if (related.Count < 3)
            related.AddRange(scored.Where(x => x.Score < 5 && x.Score >= 2).Take(3 - related.Count));

        var sharedTag = self.TagList.FirstOrDefault(tag =>
            related.Count(x => x.Entry.TagList.Contains(tag, StringComparer.OrdinalIgnoreCase)) >= 2);
        var heading = sharedTag != null ? $"Related {sharedTag} Rankings" : $"More {self.CategoryTitle} Rankings";

        return new RankingPageLinks(chips, heading, related.Select(x => x.Entry).ToList(), subHub);
    }

    public static RankingDirectoryViewModel Build(IReadOnlyList<RankingDirectoryCategory> categories,
        RankingDirectoryCategory? category, string? query, string? topic, string? group, string? year, string? sort, int page)
    {
        query = (query ?? "").Trim();
        if (query.Length > 200) query = query[..200];
        topic = (topic ?? "").Trim();
        group = category == null ? (group ?? "").Trim() : "";
        year = (year ?? "").Trim();
        sort = sort is "az" or "newest" or "updated" ? sort : "featured";
        var all = (category?.Entries ?? categories.SelectMany(item => item.Entries)
            .GroupBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
            .Select(items => items.First()).ToList()).ToList();
        IEnumerable<RankingDirectoryEntry> filtered = all;
        if (query.Length > 0)
        {
            var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            filtered = filtered.Where(item => terms.All(term =>
                $"{item.Title} {item.Description} {item.CategoryTitle} {item.Topic} {item.Source} {string.Join(' ', item.TagList)}"
                    .Contains(term, StringComparison.OrdinalIgnoreCase)));
        }
        if (topic.Length > 0) filtered = filtered.Where(item => string.Equals(item.Topic, topic, StringComparison.OrdinalIgnoreCase)
            || item.TagList.Contains(topic, StringComparer.OrdinalIgnoreCase));
        if (group.Length > 0)
        {
            var groupedCategory = categories.FirstOrDefault(item =>
                string.Equals(item.Id, group, StringComparison.OrdinalIgnoreCase));
            filtered = groupedCategory?.Entries ?? [];
        }
        if (year.Length > 0) filtered = filtered.Where(item => year == "unknown" ? item.DataYear == null : item.DataYear?.ToString(CultureInfo.InvariantCulture) == year);

        var ordered = sort switch
        {
            "az" => filtered.OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase),
            "newest" => filtered.OrderByDescending(item => item.DataYear).ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase),
            "updated" => filtered.OrderByDescending(item => item.Updated).ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase),
            _ => filtered.OrderBy(item => item.FeaturedOrder).ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
        };
        var result = ordered.ToList();
        page = Math.Max(1, page);
        return new RankingDirectoryViewModel
        {
            Categories = categories, Category = category, Query = query, Topic = topic, Group = group,
            Year = year, Sort = sort, Page = page, TotalCount = all.Count, ResultCount = result.Count,
            Items = result.Skip((int)Math.Min((long)(page - 1) * RankingDirectoryViewModel.PageSize, int.MaxValue)).Take(RankingDirectoryViewModel.PageSize).ToList(),
            Featured = all.OrderByDescending(item => item.Updated).ThenBy(item => item.Title).Take(6).ToList(),
            Topics = all.SelectMany(item => item.TagList.Append(item.Topic).Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(tag => (tag, item))).GroupBy(pair => pair.tag, StringComparer.OrdinalIgnoreCase)
                .Select(items => new RankingDirectoryFilter(items.Key, items.Key, items.Count())).OrderBy(item => item.Label).ToList(),
            Years = all.Where(item => item.DataYear.HasValue).Select(item => item.DataYear!.Value).Distinct().OrderDescending().ToList()
        };
    }

    private static int? SingleYear(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var years = Regex.Matches(text, @"\b(?:19|20)\d{2}\b").Select(match => int.Parse(match.Value, CultureInfo.InvariantCulture)).Distinct().ToArray();
        return years.Length == 1 ? years[0] : null;
    }

    private static string GetCategoryTitle(string id) => CategoryTitles.GetValueOrDefault(id,
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace('-', ' ')));

    private static string GetTopic(string category, string slug, string? explicitTopic)
    {
        if (!string.IsNullOrWhiteSpace(explicitTopic)) return explicitTopic;
        if (category != "agriculture") return "General";
        bool Has(params string[] terms) => terms.Any(term => slug.Contains(term, StringComparison.OrdinalIgnoreCase));
        if (Has("disaster", "insurance", "drought", "weather")) return "Disasters & Weather";
        if (Has("export", "import", "trade")) return "Trade & Exports";
        if (Has("gdp", "employment", "price", "ethanol", "storage")) return "Agricultural Economy";
        if (Has("farmland", "farmer", "farms", "own-a-ranch", "farming-friendly")) return "Farmland & Farming";
        if (Has("legal", "thc")) return "Laws & Policy";
        if (Has("beef", "beekeeping", "bison", "butter", "catfish", "cattle", "cheese", "chicken", "crawfish", "egg", "goat", "hog", "honey-production", "horse", "ice-cream", "lobster", "milk", "oyster", "sheep", "shrimp", "trout", "turkey", "wool", "yogurt")) return "Livestock & Aquaculture";
        if (Has("production", "belt", "vineyard", "planting")) return "Crops & Forestry";
        return "Other Agriculture";
    }

    public void Dispose() => _loadLock.Dispose();

    // These DTOs intentionally have no row, section, image, or map properties.
    public sealed class RankingMetadata
    {
        public string? Subcategory { get; set; }
        public List<string>? Tags { get; set; }
        public List<MetadataRelated>? Related { get; set; }
        public List<string>? HubCategories { get; set; }
        public DateTime? DatePublished { get; set; }
        public DateTime? DateModified { get; set; }
        public int? DataYear { get; set; }
        public string? HeroImage { get; set; }
        public MetadataSeo? Seo { get; set; }
        public MetadataPage? Page { get; set; }
        public MetadataTable? Table { get; set; }
    }
    public sealed class MetadataSeo { public string? Description { get; set; } }
    public sealed class MetadataPage
    {
        public string H1 { get; set; } = "";
        public string? Methodology { get; set; }
        public List<MetadataSource>? Sources { get; set; }
    }
    public sealed class MetadataSource { public string? Name { get; set; } }
    public sealed class MetadataRelated { public string? Url { get; set; } }
    public sealed class MetadataTable { public string? Note { get; set; } }
}
