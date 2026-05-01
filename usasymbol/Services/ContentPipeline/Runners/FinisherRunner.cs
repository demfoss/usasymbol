using USASymbol.Models.ContentPipeline;

namespace USASymbol.Services.ContentPipeline.Runners;

public sealed class FinisherRunner
{
    private readonly BuildPacketService _buildPacketService;
    private readonly PromptTemplateRendererService _promptTemplateRendererService;

    public FinisherRunner(
        BuildPacketService buildPacketService,
        PromptTemplateRendererService promptTemplateRendererService)
    {
        _buildPacketService = buildPacketService;
        _promptTemplateRendererService = promptTemplateRendererService;
    }

    public async Task<string> RunAsync(
        PromptPayloadModel payload,
        string generatedYaml,
        CancellationToken cancellationToken = default)
    {
        var template = await _buildPacketService.ReadPromptTemplateAsync("finisher-prompt.txt", cancellationToken);
        return _promptTemplateRendererService.RenderFinisher(template.Trim(), WriterRunner.NormalizePayload(payload), generatedYaml);
    }
}
