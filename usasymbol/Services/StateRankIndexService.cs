using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Usasymbol.Helpers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace USASymbol.Services;

public sealed record StateRankEntry(
    string Category,
    string Slug,
    string Title,
    string Url,
    int Rank,
    int Total,
    string Value,
    bool IsLowest,
    int TieCount,
    bool HigherIsBetter,
    string SourceName,
    string SourceUrl,
    DateTime? DateModified,
    string HeroImage,
    string HeroImageAlt)
{
    public bool IsHighest => Rank == 1;
    public bool IsGoodNews => (IsHighest && HigherIsBetter) || (IsLowest && !HigherIsBetter);
}

public sealed record StateTeaser(
    string Title,
    string Url,
    string Badge,
    string Value,
    string HeroImage,
    string HeroImageAlt);

public sealed record StateTeaserSet(string StateName, string StateSlug, IReadOnlyList<StateTeaser> Items);

/// <summary>
/// One in-memory index "state -> its place in every numeric ranking", built once from the ranking YAML
/// (lightly parsed: only the table, title and metric). It feeds the "Kentucky also ranks #1 in..." links.
/// The teaser selection is computed for all rankings at once, in a fixed order, so it is deterministic
/// (crawlers see the same links every time) and each ranking receives a capped number of inbound teasers.
/// </summary>
public sealed class StateRankIndexService(IWebHostEnvironment environment, IMemoryCache cache,
    ILogger<StateRankIndexService> logger)
{
    private const string CacheKey = "state-rank-index-v2";
    private const int MinStates = 30;           // below this "top 3" means little
    private const int MaxInboundPerRanking = 15; // spreads link weight across the section
    private const int TeasersPerPage = 3;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly IDeserializer _yaml = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance).IgnoreUnmatchedProperties().Build();

    private sealed record Built(
        Dictionary<string, List<StateRankEntry>> ByState,
        Dictionary<string, StateTeaserSet> Teasers);

    public async Task<StateTeaserSet?> GetTeasersAsync(string category, string slug)
    {
        var built = await GetBuiltAsync();
        return built.Teasers.GetValueOrDefault($"{category}/{slug}");
    }

    /// <summary>
    /// Directional, sufficiently complete ranking facts for one state. The returned rows are
    /// suitable inputs for an editorial press pack, but callers should still describe them as
    /// "highest" or "lowest" rather than treating every numerical #1 as good news.
    /// </summary>
    public async Task<IReadOnlyList<StateRankEntry>> GetStateEntriesAsync(string stateSlug)
    {
        if (string.IsNullOrWhiteSpace(stateSlug)) return Array.Empty<StateRankEntry>();

        var built = await GetBuiltAsync();
        if (!built.ByState.TryGetValue(stateSlug, out var entries))
            return Array.Empty<StateRankEntry>();

        return entries
            .Where(IsExtreme)
            .OrderBy(entry => entry.IsLowest ? 1 : 0)
            .ThenBy(entry => entry.IsLowest ? 0 : entry.Rank)
            .ThenBy(entry => entry.Category, StringComparer.Ordinal)
            .ThenBy(entry => entry.Title, StringComparer.Ordinal)
            .ToList();
    }

    public async Task WarmUpAsync() => await GetBuiltAsync();

    private async Task<Built> GetBuiltAsync()
    {
        if (cache.TryGetValue(CacheKey, out Built? cached) && cached != null) return cached;
        await _lock.WaitAsync();
        try
        {
            if (cache.TryGetValue(CacheKey, out cached) && cached != null) return cached;
            var built = await Task.Run(Build);
            var options = new MemoryCacheEntryOptions()
                .AddExpirationToken(environment.ContentRootFileProvider.Watch("Content/rankings/**/*"));
            cache.Set(CacheKey, built, options);
            return built;
        }
        finally { _lock.Release(); }
    }

    private Built Build()
    {
        var root = Path.Combine(environment.ContentRootPath, "Content", "rankings");
        var byState = new Dictionary<string, List<StateRankEntry>>(StringComparer.OrdinalIgnoreCase);
        var leaders = new List<(string Key, string Category, StateRankEntry Leader, string LeaderName)>();
        if (!Directory.Exists(root)) return new Built(byState, new());

        var files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        foreach (var file in files)
        {
            try
            {
                var category = Path.GetFileName(Path.GetDirectoryName(file)!);
                var slug = Path.GetFileNameWithoutExtension(file);
                var parsed = ReadRanking(file, category, slug);
                if (parsed == null) continue;

                foreach (var (stateSlug, entry) in parsed.Value.Entries)
                {
                    if (!byState.TryGetValue(stateSlug, out var list)) byState[stateSlug] = list = new();
                    list.Add(entry);
                }
                leaders.Add(($"{category}/{slug}", category, parsed.Value.Leader, parsed.Value.LeaderName));
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "State rank index skipped {File}", file);
            }
        }

        // Fixed processing order => deterministic output; inbound counter caps link concentration.
        var inbound = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var teasers = new Dictionary<string, StateTeaserSet>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, category, leader, leaderName) in leaders.OrderBy(l => l.Key, StringComparer.Ordinal))
        {
            var stateSlug = leaderName; // see ReadRanking: LeaderName carries the leader's slug
            if (!byState.TryGetValue(stateSlug, out var others)) continue;

            var chosen = others
                .Where(o => !(o.Category == category && o.Slug == leader.Slug) && IsExtreme(o))
                .Select(o => (Entry: o, Score: Extremity(o), Same: o.Category == category ? 0 : 1))
                .OrderBy(x => x.Same).ThenBy(x => x.Score).ThenBy(x => x.Entry.Slug, StringComparer.Ordinal)
                .Where(x => inbound.GetValueOrDefault($"{x.Entry.Category}/{x.Entry.Slug}") < MaxInboundPerRanking)
                .Take(TeasersPerPage)
                .ToList();
            if (chosen.Count == 0) continue;

            foreach (var c in chosen)
            {
                var k = $"{c.Entry.Category}/{c.Entry.Slug}";
                inbound[k] = inbound.GetValueOrDefault(k) + 1;
            }

            teasers[key] = new StateTeaserSet(
                DisplayName(stateSlug), stateSlug,
                chosen.Select(c => new StateTeaser(
                    c.Entry.Title, c.Entry.Url,
                    c.Entry.IsLowest && c.Entry.Rank > 3 ? "Lowest" : $"#{c.Entry.Rank}",
                    c.Entry.Value,
                    c.Entry.HeroImage,
                    c.Entry.HeroImageAlt)).ToList());
        }

        logger.LogInformation("State rank index built: {Rankings} rankings, {States} states, {Teasers} pages with teasers",
            leaders.Count, byState.Count, teasers.Count);
        return new Built(byState, teasers);
    }

    private static bool IsExtreme(StateRankEntry e) => e.Total >= MinStates && e.TieCount <= 2 && (e.Rank <= 3 || e.IsLowest);
    private static int Extremity(StateRankEntry e) => e.IsLowest ? 1 : e.Rank;

    private static string DisplayName(string slug) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(slug.Replace('-', ' '));

    private ((string, StateRankEntry)[] Entries, StateRankEntry Leader, string LeaderName)? ReadRanking(string file, string category, string slug)
    {
        var root = _yaml.Deserialize<Dictionary<string, object?>>(File.ReadAllText(file));
        if (root == null || !root.TryGetValue("table", out var tObj) || tObj is not Dictionary<object, object> table) return null;
        if (table.ContainsKey("display_mode")) return null;
        // Only explicitly directional metrics are suitable for "stands out" superlatives.
        // Neutral counts and facts deliberately omit this flag in ranking YAML.
        if (!bool.TryParse(table.GetValueOrDefault("higher_is_better")?.ToString(), out var higherIsBetter)) return null;
        // Some directional metrics are still poor cross-page comparisons: raw totals mostly
        // measure population, and a few legacy titles do not describe their numeric metric.
        if (bool.TryParse(table.GetValueOrDefault("state_index")?.ToString(), out var includeInIndex) && !includeInIndex)
            return null;

        var metric = (table.GetValueOrDefault("default_column") ?? table.GetValueOrDefault("metric_default"))?.ToString();
        if (string.IsNullOrWhiteSpace(metric) || table.GetValueOrDefault("rows") is not List<object> rows) return null;

        var points = new List<(string Slug, double Value, string Raw, string? Rank)>();
        foreach (var r in rows.OfType<Dictionary<object, object>>())
        {
            var stateSlug = r.GetValueOrDefault("state_slug")?.ToString();
            if (string.IsNullOrWhiteSpace(stateSlug))
                stateSlug = r.GetValueOrDefault("state")?.ToString()?.Trim().ToLowerInvariant().Replace(' ', '-');
            if (!KeyTakeawaysBuilder.IsState(stateSlug)) continue;
            var rawObj = r.GetValueOrDefault(metric);
            var raw = Convert.ToString(rawObj, CultureInfo.InvariantCulture)?.Trim() ?? "";
            var clean = raw.Replace(",", "").Replace("$", "").Replace("%", "");
            if (!double.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) continue;
            points.Add((stateSlug!, v, raw, r.GetValueOrDefault("rank")?.ToString()));
        }
        if (points.Count < MinStates) return null;

        var max = points.Max(p => p.Value);
        var min = points.Min(p => p.Value);
        if (max - min < 1e-9) return null;
        // Few distinct values means a categorical or count-style table, where "#1" is mostly a tie.
        if (points.Select(p => p.Value).Distinct().Count() < 10) return null;
        // Rank-valued tables (rank 1 is not the largest number) would invert "top"/"bottom"; skip them.
        var rankOne = points.FirstOrDefault(p => p.Rank == "1");
        if (rankOne.Slug != null && Math.Abs(rankOne.Value - max) > 1e-9) return null;

        var title = (root.GetValueOrDefault("page") as Dictionary<object, object>)?.GetValueOrDefault("h1")?.ToString();
        if (string.IsNullOrWhiteSpace(title)) title = slug.Replace('-', ' ');
        var heroImage = root.GetValueOrDefault("hero_image")?.ToString()?.Trim() ?? "";
        var heroImageAlt = root.GetValueOrDefault("hero_image_alt")?.ToString()?.Trim() ?? title!;
        var url = root.GetValueOrDefault("url")?.ToString();
        if (string.IsNullOrWhiteSpace(url)) url = $"/rankings/{category}/{slug}";
        var page = root.GetValueOrDefault("page") as Dictionary<object, object>;
        var source = (page?.GetValueOrDefault("sources") as List<object>)?
            .OfType<Dictionary<object, object>>()
            .FirstOrDefault();
        var sourceName = source?.GetValueOrDefault("name")?.ToString()?.Trim() ?? "";
        var sourceUrl = source?.GetValueOrDefault("url")?.ToString()?.Trim() ?? "";
        DateTime? dateModified = DateTime.TryParse(
            root.GetValueOrDefault("date_modified")?.ToString(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var parsedDate)
                ? parsedDate
                : null;

        var ordered = points.OrderByDescending(p => p.Value).ToList();
        var entries = new List<(string, StateRankEntry)>();
        StateRankEntry? leader = null;
        string leaderSlug = "";
        foreach (var p in points)
        {
            var rank = 1 + ordered.Count(o => o.Value > p.Value);
            var entry = new StateRankEntry(
                category, slug, title!, url!, rank, points.Count, p.Raw,
                Math.Abs(p.Value - min) < 1e-9,
                ordered.Count(o => o.Value == p.Value),
                higherIsBetter,
                sourceName,
                sourceUrl,
                dateModified,
                heroImage,
                heroImageAlt);
            entries.Add((p.Slug, entry));
            if (rank == 1 && (leader == null || string.CompareOrdinal(p.Slug, leaderSlug) < 0))
            {
                leader = entry; leaderSlug = p.Slug;
            }
        }
        if (leader == null) return null;
        // A three-way tie at the top has no single "leader" to tease.
        if (points.Count(p => p.Value == max) > 2) return null;

        return (entries.ToArray(), leader, leaderSlug);
    }
}
