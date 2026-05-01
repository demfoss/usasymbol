using Microsoft.Extensions.Options;
using USASymbol.Models.ContentPipeline;

namespace USASymbol.Services.ContentPipeline;

public sealed class ContentPipelineAccessService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ContentPipelineOptions _options;

    public ContentPipelineAccessService(IWebHostEnvironment environment, IOptions<ContentPipelineOptions> options)
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
            throw new InvalidOperationException("Content pipeline is disabled outside development.");
        }
    }
}
