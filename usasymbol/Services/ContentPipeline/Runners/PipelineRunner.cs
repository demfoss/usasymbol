using USASymbol.Models.ContentPipeline;

namespace USASymbol.Services.ContentPipeline.Runners;

public sealed class PipelineRunner
{
    private readonly BuildPacketService _buildPacketService;
    private readonly WriterRunner _writerRunner;
    private readonly FinisherRunner _finisherRunner;
    private readonly AnthropicContentClient _anthropicContentClient;
    private readonly PipelineResponseParserService _pipelineResponseParserService;
    private readonly PipelineOutputService _pipelineOutputService;
    private readonly SimilarityService _similarityService;
    private readonly YamlValidatorService _yamlValidatorService;

    public PipelineRunner(
        BuildPacketService buildPacketService,
        WriterRunner writerRunner,
        FinisherRunner finisherRunner,
        AnthropicContentClient anthropicContentClient,
        PipelineResponseParserService pipelineResponseParserService,
        PipelineOutputService pipelineOutputService,
        SimilarityService similarityService,
        YamlValidatorService yamlValidatorService)
    {
        _buildPacketService = buildPacketService;
        _writerRunner = writerRunner;
        _finisherRunner = finisherRunner;
        _anthropicContentClient = anthropicContentClient;
        _pipelineResponseParserService = pipelineResponseParserService;
        _pipelineOutputService = pipelineOutputService;
        _similarityService = similarityService;
        _yamlValidatorService = yamlValidatorService;
    }

    public async Task<PipelineRunResultModel> RunAsync(
        ManualInputModel input,
        IProgress<PipelineProgressEntryModel>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Report(progress, "input", "Building prompt payload.");
        var payload = await _buildPacketService.BuildPromptPayloadAsync(input, cancellationToken);
        Report(progress, "payload", "Prompt payload is ready.");
        var generatorPrompt = await _writerRunner.RunAsync(payload, cancellationToken);
        Report(progress, "generator", "Calling Claude generator.");
        var generatorRawResponse = await _anthropicContentClient.GenerateDraftAsync(generatorPrompt, cancellationToken);
        var generatorYaml = _pipelineResponseParserService.ExtractYaml(generatorRawResponse);

        var finisherPrompt = await _finisherRunner.RunAsync(payload, generatorYaml, cancellationToken);
        Report(progress, "finisher", "Calling Claude finisher.");
        var finisherRawResponse = await _anthropicContentClient.FinishDraftAsync(finisherPrompt, cancellationToken);
        var finisherYaml = _pipelineResponseParserService.ExtractYaml(finisherRawResponse);
        var finisherNotes = _pipelineResponseParserService.ExtractNotes(finisherRawResponse);

        var finalYaml = finisherYaml;
        Report(progress, "checks", "Running post-generation checks.");
        var checks = await _yamlValidatorService.RunChecksAsync(payload, finalYaml, cancellationToken);
        Report(progress, "similarity", "Running similarity warning check.");
        var similarity = await _similarityService.CheckAsync(input.Category, finalYaml, cancellationToken);

        var savedToDisk = false;
        if (checks.IsSuccess && !string.IsNullOrWhiteSpace(finalYaml))
        {
            Report(progress, "save", "Saving final YAML to disk.");
            await _pipelineOutputService.SaveAsync(payload.SuggestedOutputPath, finalYaml, cancellationToken);
            savedToDisk = true;
        }
        else
        {
            Report(progress, "save-skipped", "Skipping save because blocking issues remain.");
        }

        return new PipelineRunResultModel
        {
            Payload = payload,
            GeneratorPrompt = generatorPrompt,
            GeneratorRawResponse = generatorRawResponse,
            GeneratorYaml = generatorYaml,
            FinisherPrompt = finisherPrompt,
            FinisherRawResponse = finisherRawResponse,
            FinisherYaml = finisherYaml,
            FinisherNotes = finisherNotes,
            Checks = checks,
            Similarity = similarity,
            FinalYaml = finalYaml,
            SavedToDisk = savedToDisk,
            SavePath = payload.SuggestedOutputPath,
            ImagesPipelineQueued = input.ImagesEnabled,
            ExecutionMode = "AnthropicLive"
        };
    }

    private static void Report(IProgress<PipelineProgressEntryModel>? progress, string step, string message)
    {
        progress?.Report(new PipelineProgressEntryModel
        {
            TimestampUtc = DateTime.UtcNow,
            Step = step,
            Message = message
        });
    }
}
