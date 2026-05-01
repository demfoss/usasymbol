using USASymbol.Models.ContentPipeline;
using USASymbol.Services.ContentPipeline.Utils;

namespace USASymbol.Services.ContentPipeline;

public sealed class InternalLinksService
{
    private readonly ContentIndexService _contentIndexService;
    private readonly SlugUtility _slugUtility;

    private static readonly Dictionary<string, string> CategoryHubs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["surnames"] = "/surnames",
        ["ranking"] = "/rankings",
        ["state-seals"] = "/state-seals"
    };

    public InternalLinksService(ContentIndexService contentIndexService, SlugUtility slugUtility)
    {
        _contentIndexService = contentIndexService;
        _slugUtility = slugUtility;
    }

    public async Task<IReadOnlyList<InternalLinkCandidateModel>> SelectCandidatesAsync(
        ManualInputModel input,
        CancellationToken cancellationToken = default)
    {
        var index = await _contentIndexService.BuildAsync(cancellationToken);
        var topicSlug = _slugUtility.ToSlug(input.TopicOrState);
        var keywordSlug = _slugUtility.ToSlug(input.PrimaryKeyword);
        var results = new List<InternalLinkCandidateModel>();

        if (!string.IsNullOrWhiteSpace(topicSlug))
        {
            AddUnique(results, new InternalLinkCandidateModel
            {
                Title = $"{input.TopicOrState} hub",
                Url = $"/states/{topicSlug}",
                AnchorHint = input.TopicOrState,
                Reason = "State hub for natural contextual linking."
            });
        }

        if (CategoryHubs.TryGetValue(input.Category, out var categoryHubUrl))
        {
            AddUnique(results, new InternalLinkCandidateModel
            {
                Title = $"{input.Category} hub",
                Url = categoryHubUrl,
                AnchorHint = input.Category.Replace('-', ' '),
                Reason = "Category hub for broader navigation."
            });
        }

        var candidates = index
            .Where(x => !string.IsNullOrWhiteSpace(x.Route))
            .Where(x => !string.Equals(x.Slug, keywordSlug, StringComparison.OrdinalIgnoreCase))
            .Select(x => new
            {
                Entry = x,
                Score = ScoreEntry(x, input.Category, topicSlug, input.PrimaryKeyword)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Entry.Title, StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToArray();

        foreach (var candidate in candidates.Take(5))
        {
            AddUnique(results, new InternalLinkCandidateModel
            {
                Title = candidate.Entry.Title,
                Url = candidate.Entry.Route,
                AnchorHint = FirstNonEmpty(candidate.Entry.H1, candidate.Entry.Title),
                Reason = BuildReason(candidate.Entry, input.Category, topicSlug)
            });
        }

        return results.Take(5).ToArray();
    }

    private static int ScoreEntry(ContentIndexEntryModel entry, string category, string topicSlug, string primaryKeyword)
    {
        var score = 0;
        if (!string.IsNullOrWhiteSpace(topicSlug) && string.Equals(entry.StateSlug, topicSlug, StringComparison.OrdinalIgnoreCase))
        {
            score += 7;
        }

        if (!string.IsNullOrWhiteSpace(category) && string.Equals(entry.Category, category, StringComparison.OrdinalIgnoreCase))
        {
            score += 5;
        }

        if (!string.IsNullOrWhiteSpace(primaryKeyword) &&
            (entry.Title.Contains(primaryKeyword, StringComparison.OrdinalIgnoreCase) ||
             entry.H1.Contains(primaryKeyword, StringComparison.OrdinalIgnoreCase)))
        {
            score += 3;
        }

        if (entry.Route.Contains("by-state", StringComparison.OrdinalIgnoreCase) ||
            entry.Route.Contains("/rankings/", StringComparison.OrdinalIgnoreCase))
        {
            score += 2;
        }

        if (entry.Tags.Count > 0)
        {
            score += 1;
        }

        return score;
    }

    private static string BuildReason(ContentIndexEntryModel entry, string category, string topicSlug)
    {
        if (!string.IsNullOrWhiteSpace(topicSlug) && string.Equals(entry.StateSlug, topicSlug, StringComparison.OrdinalIgnoreCase))
        {
            return "Same state context.";
        }

        if (!string.IsNullOrWhiteSpace(category) && string.Equals(entry.Category, category, StringComparison.OrdinalIgnoreCase))
        {
            return "Same category context.";
        }

        return "Broader related page.";
    }

    private static void AddUnique(List<InternalLinkCandidateModel> results, InternalLinkCandidateModel candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate.Url))
        {
            return;
        }

        if (results.Any(x => string.Equals(x.Url, candidate.Url, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        results.Add(candidate);
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;
    }
}
