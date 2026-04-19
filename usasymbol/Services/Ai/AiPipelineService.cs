using USASymbol.Models.Ai;

namespace USASymbol.Services.Ai;

public sealed class AiPipelineService
{
    private readonly AiPipelineAccessService _accessService;
    private readonly PromptTemplateService _promptTemplateService;
    private readonly OpenAiTextClient _openAiTextClient;
    private readonly ClaudeTextClient _claudeTextClient;
    private readonly GeneratedContentFileService _fileService;

    public AiPipelineService(
        AiPipelineAccessService accessService,
        PromptTemplateService promptTemplateService,
        OpenAiTextClient openAiTextClient,
        ClaudeTextClient claudeTextClient,
        GeneratedContentFileService fileService)
    {
        _accessService = accessService;
        _promptTemplateService = promptTemplateService;
        _openAiTextClient = openAiTextClient;
        _claudeTextClient = claudeTextClient;
        _fileService = fileService;
    }

    public async Task<string> GenerateBriefAsync(AiPipelineRequest request, CancellationToken cancellationToken = default)
    {
        _accessService.EnsureEnabled();
        var examplesContext = await _promptTemplateService.BuildExamplesContextAsync(request.ExampleFilePath1, request.ExampleFilePath2);

        var briefPrompt = await _promptTemplateService.RenderAsync("brief.txt", new Dictionary<string, string>
        {
            ["topic"] = request.Topic,
            ["notes"] = request.Notes,
            ["examples_context"] = examplesContext
        });

        return request.UseClaudeOnlyMode
            ? await _claudeTextClient.GenerateAsync(briefPrompt, cancellationToken)
            : await _openAiTextClient.GenerateAsync(briefPrompt, cancellationToken);
    }

    public async Task<string> GenerateDraftAsync(AiPipelineRequest request, string brief, CancellationToken cancellationToken = default)
    {
        _accessService.EnsureEnabled();
        var examplesContext = await _promptTemplateService.BuildExamplesContextAsync(request.ExampleFilePath1, request.ExampleFilePath2);
        var formatContext = BuildFormatContext(request);

        var draftPrompt = await _promptTemplateService.RenderAsync("draft.txt", new Dictionary<string, string>
        {
            ["topic"] = request.Topic,
            ["notes"] = request.Notes,
            ["brief"] = brief,
            ["examples_context"] = examplesContext,
            ["format_context"] = formatContext
        });

        return await _claudeTextClient.GenerateAsync(draftPrompt, cancellationToken);
    }

    public async Task<string> EditAsync(AiPipelineRequest request, string brief, string draft, CancellationToken cancellationToken = default)
    {
        _accessService.EnsureEnabled();
        var examplesContext = await _promptTemplateService.BuildExamplesContextAsync(request.ExampleFilePath1, request.ExampleFilePath2);
        var formatContext = BuildFormatContext(request);

        var editPrompt = await _promptTemplateService.RenderAsync("edit.txt", new Dictionary<string, string>
        {
            ["topic"] = request.Topic,
            ["notes"] = request.Notes,
            ["brief"] = brief,
            ["draft"] = draft,
            ["examples_context"] = examplesContext,
            ["format_context"] = formatContext
        });

        return request.UseOpenAiForEditing
            && !request.UseClaudeOnlyMode
            ? await _openAiTextClient.GenerateAsync(editPrompt, cancellationToken)
            : await _claudeTextClient.GenerateAsync(editPrompt, cancellationToken);
    }

    public async Task<AiPipelineResult> SaveAsync(
        AiPipelineRequest request,
        string brief,
        string draft,
        string finalText,
        CancellationToken cancellationToken = default)
    {
        _accessService.EnsureEnabled();

        var result = new AiPipelineResult
        {
            Topic = request.Topic,
            Brief = brief,
            Draft = draft,
            FinalText = finalText,
            FinalEditor = request.UseOpenAiForEditing ? "ChatGPT" : "Claude"
        };

        var savedPaths = await _fileService.SaveAsync(request, result, cancellationToken);
        result.SavedArticlePath = savedPaths.articlePath;
        result.SavedBriefPath = savedPaths.briefPath;
        result.SavedDraftPath = savedPaths.draftPath;
        result.SavedFinalPath = savedPaths.finalPath;

        return result;
    }

    private static string BuildFormatContext(AiPipelineRequest request)
    {
        var extension = Path.GetExtension(request.TargetFilePath ?? string.Empty);
        if (string.Equals(extension, ".yaml", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".yml", StringComparison.OrdinalIgnoreCase))
        {
            return "Return valid YAML only. Do not return markdown headings, horizontal rules, or fenced code blocks. Match the site's YAML-style content structure and include only fields that genuinely fit the page.";
        }

        return string.Empty;
    }

    public async Task<AiPipelineResult> RunAsync(AiPipelineRequest request, CancellationToken cancellationToken = default)
    {
        var brief = request.UseExistingBriefForAuto && !string.IsNullOrWhiteSpace(request.ExistingBrief)
            ? request.ExistingBrief.Trim()
            : await GenerateBriefAsync(request, cancellationToken);
        var draft = await GenerateDraftAsync(request, brief, cancellationToken);
        var finalText = await EditAsync(request, brief, draft, cancellationToken);

        var result = new AiPipelineResult
        {
            Topic = request.Topic,
            Brief = brief,
            Draft = draft,
            FinalText = finalText,
            FinalEditor = request.UseOpenAiForEditing ? "ChatGPT" : "Claude"
        };

        var savedPaths = await _fileService.SaveAsync(request, result, cancellationToken);
        result.SavedArticlePath = savedPaths.articlePath;
        result.SavedBriefPath = savedPaths.briefPath;
        result.SavedDraftPath = savedPaths.draftPath;
        result.SavedFinalPath = savedPaths.finalPath;

        return result;
    }
}
