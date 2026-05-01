using USASymbol.Models.ContentPipeline;

namespace USASymbol.Services.ContentPipeline.Runners;

public sealed class WriterRunner
{
    private readonly BuildPacketService _buildPacketService;
    private readonly PromptTemplateRendererService _promptTemplateRendererService;

    public WriterRunner(
        BuildPacketService buildPacketService,
        PromptTemplateRendererService promptTemplateRendererService)
    {
        _buildPacketService = buildPacketService;
        _promptTemplateRendererService = promptTemplateRendererService;
    }

    public async Task<string> RunAsync(PromptPayloadModel payload, CancellationToken cancellationToken = default)
    {
        var template = await _buildPacketService.ReadPromptTemplateAsync("generator-prompt.txt", cancellationToken);
        return _promptTemplateRendererService.RenderGenerator(template.Trim(), NormalizePayload(payload));
    }

    internal static PromptPayloadModel NormalizePayload(PromptPayloadModel payload)
    {
        return new PromptPayloadModel
        {
            Category = payload.Category,
            PrimaryKeyword = payload.PrimaryKeyword,
            TopicOrState = payload.TopicOrState,
            YamlSkeleton = payload.YamlSkeleton,
            SourceNotes = payload.SourceNotes,
            OptionalExtraInstruction = payload.OptionalExtraInstruction,
            ImagesEnabled = payload.ImagesEnabled,
            DefaultSecondaryQueries = payload.DefaultSecondaryQueries.Select(x => ReplaceStatePlaceholder(x, payload.TopicOrState)).ToArray(),
            TitleExamples = payload.TitleExamples.Select(x => ReplaceStatePlaceholder(x, payload.TopicOrState)).ToArray(),
            SeoDescriptionExamples = payload.SeoDescriptionExamples.Select(x => ReplaceStatePlaceholder(x, payload.TopicOrState)).ToArray(),
            InternalLinks = payload.InternalLinks,
            RecentPatternWarnings = payload.RecentPatternWarnings,
            SuggestedOutputPath = payload.SuggestedOutputPath
        };
    }

    private static string ReplaceStatePlaceholder(string value, string topicOrState)
    {
        return (value ?? string.Empty)
            .Replace("[STATE]", topicOrState, StringComparison.OrdinalIgnoreCase)
            .Replace("[State]", topicOrState, StringComparison.OrdinalIgnoreCase);
    }
}
