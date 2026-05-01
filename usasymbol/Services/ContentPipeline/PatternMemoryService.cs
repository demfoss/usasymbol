using System.Text.Json;
using Microsoft.Extensions.Options;
using USASymbol.Models.ContentPipeline;

namespace USASymbol.Services.ContentPipeline;

public sealed class PatternMemoryService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ContentPipelineOptions _options;

    public PatternMemoryService(IWebHostEnvironment environment, IOptions<ContentPipelineOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<PatternMemoryEntryModel>> LoadAsync(
        string categoryKey,
        CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(
            _environment.ContentRootPath,
            _options.RootDirectory,
            _options.DataDirectory,
            "pattern-memory.json");

        if (!File.Exists(filePath))
        {
            return Array.Empty<PatternMemoryEntryModel>();
        }

        await using var stream = File.OpenRead(filePath);
        var entries = await JsonSerializer.DeserializeAsync<List<PatternMemoryEntryModel>>(stream, cancellationToken: cancellationToken)
            ?? new List<PatternMemoryEntryModel>();

        return entries
            .Where(x => string.IsNullOrWhiteSpace(categoryKey) || string.Equals(x.CategoryKey, categoryKey, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
