using Microsoft.Extensions.Options;
using USASymbol.Models.Ai;

namespace USASymbol.Services.Ai;

public sealed class AiPipelineAccessService
{
    private readonly IWebHostEnvironment _environment;
    private readonly AiPipelineOptions _options;

    public AiPipelineAccessService(IWebHostEnvironment environment, IOptions<AiPipelineOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public bool IsEnabled()
    {
        return _environment.IsDevelopment() || _options.EnabledOutsideDevelopment;
    }

    public void EnsureEnabled()
    {
        if (!IsEnabled())
        {
            throw new InvalidOperationException("AI pipeline is disabled outside Development.");
        }
    }
}
