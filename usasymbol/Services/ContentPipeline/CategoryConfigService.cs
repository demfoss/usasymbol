using System.Text.Json;
using Microsoft.Extensions.Options;
using USASymbol.Models.ContentPipeline;

namespace USASymbol.Services.ContentPipeline;

public sealed class CategoryConfigService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ContentPipelineOptions _options;

    public CategoryConfigService(IWebHostEnvironment environment, IOptions<ContentPipelineOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public async Task<CategoryConfigModel?> LoadAsync(string categoryKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryKey))
        {
            return null;
        }

        var configsPath = Path.Combine(_environment.ContentRootPath, _options.RootDirectory, _options.ConfigsDirectory);
        var filePath = Path.Combine(configsPath, $"categories.{categoryKey}.json");

        if (!File.Exists(filePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<CategoryConfigModel>(
            stream,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            },
            cancellationToken);
    }
}
