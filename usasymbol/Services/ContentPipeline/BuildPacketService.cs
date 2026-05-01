using Microsoft.Extensions.Options;
using USASymbol.Models.ContentPipeline;
using USASymbol.Services.ContentPipeline.Utils;

namespace USASymbol.Services.ContentPipeline;

public sealed class BuildPacketService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ContentPipelineOptions _options;
    private readonly CategoryConfigService _categoryConfigService;
    private readonly InternalLinksService _internalLinksService;
    private readonly PatternMemoryService _patternMemoryService;
    private readonly SlugUtility _slugUtility;

    public BuildPacketService(
        IWebHostEnvironment environment,
        IOptions<ContentPipelineOptions> options,
        CategoryConfigService categoryConfigService,
        InternalLinksService internalLinksService,
        PatternMemoryService patternMemoryService,
        SlugUtility slugUtility)
    {
        _environment = environment;
        _options = options.Value;
        _categoryConfigService = categoryConfigService;
        _internalLinksService = internalLinksService;
        _patternMemoryService = patternMemoryService;
        _slugUtility = slugUtility;
    }

    public async Task<PromptPayloadModel> BuildPromptPayloadAsync(ManualInputModel input, CancellationToken cancellationToken = default)
    {
        var category = await _categoryConfigService.LoadAsync(input.Category, cancellationToken) ?? new CategoryConfigModel
        {
            Key = input.Category,
            DisplayName = input.Category
        };

        var recentWarnings = (await _patternMemoryService.LoadAsync(input.Category, cancellationToken))
            .Where(x => !string.IsNullOrWhiteSpace(x.Summary))
            .Take(3)
            .Select(x => x.Summary.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new PromptPayloadModel
        {
            Category = input.Category,
            PrimaryKeyword = input.PrimaryKeyword,
            TopicOrState = input.TopicOrState,
            YamlSkeleton = input.YamlSkeleton,
            SourceNotes = input.SourceNotes,
            OptionalExtraInstruction = input.OptionalExtraInstruction,
            ImagesEnabled = input.ImagesEnabled,
            DefaultSecondaryQueries = category.DefaultSecondaryQueries,
            TitleExamples = category.TitleExamples,
            SeoDescriptionExamples = category.SeoDescriptionExamples,
            InternalLinks = await _internalLinksService.SelectCandidatesAsync(input, cancellationToken),
            RecentPatternWarnings = recentWarnings,
            SuggestedOutputPath = BuildSuggestedOutputPath(input, category)
        };
    }

    public string GetPromptTemplatePath(string fileName)
    {
        return Path.Combine(_environment.ContentRootPath, _options.RootDirectory, _options.PromptsDirectory, fileName);
    }

    public async Task<string> ReadPromptTemplateAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var path = GetPromptTemplatePath(fileName);
        return File.Exists(path)
            ? await File.ReadAllTextAsync(path, cancellationToken)
            : string.Empty;
    }

    private string BuildSuggestedOutputPath(ManualInputModel input, CategoryConfigModel category)
    {
        var slug = _slugUtility.ToSlug(input.PrimaryKeyword);
        var topicSlug = _slugUtility.ToSlug(input.TopicOrState);
        var contentRoot = Path.Combine(_environment.ContentRootPath, "Content");

        if (!string.IsNullOrWhiteSpace(category.OutputPathPattern))
        {
            var relativePattern = category.OutputPathPattern
                .Replace("{category}", input.Category, StringComparison.OrdinalIgnoreCase)
                .Replace("{topic_slug}", topicSlug, StringComparison.OrdinalIgnoreCase)
                .Replace("{state_slug}", topicSlug, StringComparison.OrdinalIgnoreCase)
                .Replace("{slug}", slug, StringComparison.OrdinalIgnoreCase);

            var relativePath = relativePattern
                .Replace('/', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            return Path.Combine(contentRoot, relativePath);
        }

        return Path.Combine(contentRoot, "generated", input.Category, $"{slug}.yaml");
    }
}
