using Microsoft.Extensions.Options;
using USASymbol.Models.ContentPipeline;
using YamlDotNet.RepresentationModel;

namespace USASymbol.Services.ContentPipeline.Utils;

public sealed class FileScanUtility
{
    private readonly IWebHostEnvironment _environment;
    private readonly ContentPipelineOptions _options;

    public FileScanUtility(IWebHostEnvironment environment, IOptions<ContentPipelineOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public IReadOnlyList<ContentIndexEntryModel> ScanYamlContentIndex()
    {
        var contentRoot = Path.Combine(_environment.ContentRootPath, "Content");
        if (!Directory.Exists(contentRoot))
        {
            return Array.Empty<ContentIndexEntryModel>();
        }

        return Directory
            .EnumerateFiles(contentRoot, "*.yaml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(contentRoot, "*.yml", SearchOption.AllDirectories))
            .Where(path => !path.Contains(_options.RootDirectory.Replace('/', Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            .Select(ParseContentIndexEntry)
            .Where(entry => entry is not null)
            .Cast<ContentIndexEntryModel>()
            .ToArray();
    }

    private ContentIndexEntryModel? ParseContentIndexEntry(string filePath)
    {
        try
        {
            using var reader = File.OpenText(filePath);
            var yaml = new YamlStream();
            yaml.Load(reader);

            if (yaml.Documents.Count == 0 || yaml.Documents[0].RootNode is not YamlMappingNode root)
            {
                return BuildFallbackEntry(filePath);
            }

            var slug = GetScalar(root, "slug");
            var category = GetScalar(root, "category");
            var route = GetScalar(root, "url");
            var title = GetScalar(root, "title");
            var hero = GetMapping(root, "hero");
            var heroTitle = hero is null ? string.Empty : GetScalar(hero, "title");
            var heroH1 = hero is null ? string.Empty : GetScalar(hero, "h1");
            var state = GetScalar(root, "state");
            var tags = GetSequenceValues(root, "tags");

            var inferred = InferFromPath(filePath);
            var finalSlug = string.IsNullOrWhiteSpace(slug) ? inferred.slug : slug;
            var finalCategory = string.IsNullOrWhiteSpace(category) ? inferred.category : category;
            var finalStateSlug = inferred.stateSlug;
            var finalState = string.IsNullOrWhiteSpace(state) ? HumanizeSlug(finalStateSlug) : state;
            var finalRoute = !string.IsNullOrWhiteSpace(route)
                ? route
                : BuildRoute(finalCategory, finalStateSlug, finalSlug, filePath);

            return new ContentIndexEntryModel
            {
                FilePath = filePath,
                Route = finalRoute,
                State = finalState,
                StateSlug = finalStateSlug,
                Category = finalCategory,
                Slug = finalSlug,
                Title = FirstNonEmpty(title, heroTitle, HumanizeSlug(finalSlug)),
                H1 = FirstNonEmpty(heroH1, heroTitle, title, HumanizeSlug(finalSlug)),
                Tags = tags
            };
        }
        catch
        {
            return BuildFallbackEntry(filePath);
        }
    }

    private ContentIndexEntryModel BuildFallbackEntry(string filePath)
    {
        var inferred = InferFromPath(filePath);
        var slug = inferred.slug;
        var route = BuildRoute(inferred.category, inferred.stateSlug, slug, filePath);

        return new ContentIndexEntryModel
        {
            FilePath = filePath,
            Route = route,
            State = HumanizeSlug(inferred.stateSlug),
            StateSlug = inferred.stateSlug,
            Category = inferred.category,
            Slug = slug,
            Title = HumanizeSlug(slug),
            H1 = HumanizeSlug(slug)
        };
    }

    private static string GetScalar(YamlMappingNode node, string key)
    {
        if (!node.Children.TryGetValue(new YamlScalarNode(key), out var value))
        {
            return string.Empty;
        }

        return (value as YamlScalarNode)?.Value?.Trim() ?? string.Empty;
    }

    private static YamlMappingNode? GetMapping(YamlMappingNode node, string key)
    {
        if (!node.Children.TryGetValue(new YamlScalarNode(key), out var value))
        {
            return null;
        }

        return value as YamlMappingNode;
    }

    private static IReadOnlyList<string> GetSequenceValues(YamlMappingNode node, string key)
    {
        if (!node.Children.TryGetValue(new YamlScalarNode(key), out var value) || value is not YamlSequenceNode sequence)
        {
            return Array.Empty<string>();
        }

        return sequence.Children
            .OfType<YamlScalarNode>()
            .Select(x => x.Value?.Trim() ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
    }

    private static (string category, string stateSlug, string slug) InferFromPath(string filePath)
    {
        var relativePath = filePath.Replace('\\', '/');
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var folder = Path.GetFileName(Path.GetDirectoryName(filePath) ?? string.Empty).ToLowerInvariant();

        if (relativePath.Contains("/Content/borders/", StringComparison.OrdinalIgnoreCase))
        {
            return ("borders", fileName.ToLowerInvariant(), fileName.ToLowerInvariant());
        }

        if (relativePath.Contains("/Content/collections/", StringComparison.OrdinalIgnoreCase))
        {
            var parts = relativePath.Split('/');
            var collectionsIndex = Array.FindIndex(parts, x => string.Equals(x, "collections", StringComparison.OrdinalIgnoreCase));
            var collectionCategory = collectionsIndex >= 0 && collectionsIndex + 1 < parts.Length ? parts[collectionsIndex + 1].ToLowerInvariant() : folder;
            var stateSlug = ExtractTrailingStateSlug(fileName);
            return (collectionCategory, stateSlug, fileName.ToLowerInvariant());
        }

        if (relativePath.Contains("/Content/quizzes/", StringComparison.OrdinalIgnoreCase))
        {
            return ("quizzes", string.Empty, fileName.ToLowerInvariant());
        }

        if (relativePath.Contains("/Content/test/", StringComparison.OrdinalIgnoreCase))
        {
            return (folder, ExtractStateHintFromSlug(fileName), fileName.ToLowerInvariant());
        }

        return (folder, ExtractStateHintFromSlug(fileName), fileName.ToLowerInvariant());
    }

    private static string BuildRoute(string category, string stateSlug, string slug, string filePath)
    {
        if (filePath.Replace('\\', '/').Contains("/Content/borders/", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(stateSlug))
        {
            return $"/states/{stateSlug}/borders";
        }

        if (!string.IsNullOrWhiteSpace(stateSlug) && !string.IsNullOrWhiteSpace(category) && category is not "quizzes")
        {
            return $"/{slug.TrimStart('/')}";
        }

        if (!string.IsNullOrWhiteSpace(category) && !string.IsNullOrWhiteSpace(slug))
        {
            return $"/{category}/{slug}";
        }

        return $"/{slug}";
    }

    private static string ExtractTrailingStateSlug(string slug)
    {
        var parts = slug.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return string.Join("-", parts.Skip(Math.Max(0, parts.Length - 2)));
        }

        return ExtractStateHintFromSlug(slug);
    }

    private static string ExtractStateHintFromSlug(string slug)
    {
        var states = new[]
        {
            "alabama","alaska","arizona","arkansas","california","colorado","connecticut","delaware","florida","georgia",
            "hawaii","idaho","illinois","indiana","iowa","kansas","kentucky","louisiana","maine","maryland","massachusetts",
            "michigan","minnesota","mississippi","missouri","montana","nebraska","nevada","new-hampshire","new-jersey",
            "new-mexico","new-york","north-carolina","north-dakota","ohio","oklahoma","oregon","pennsylvania","rhode-island",
            "south-carolina","south-dakota","tennessee","texas","utah","vermont","virginia","washington","west-virginia",
            "wisconsin","wyoming"
        };

        return states.FirstOrDefault(stateSlug => slug.Contains(stateSlug, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
    }

    private static string HumanizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return string.Empty;
        }

        return string.Join(" ", slug.Split('-', StringSplitOptions.RemoveEmptyEntries).Select(Capitalize));
    }

    private static string Capitalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : char.ToUpperInvariant(value[0]) + value[1..];
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;
    }
}
