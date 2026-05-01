namespace USASymbol.Services.ContentPipeline;

public sealed class PipelineOutputService
{
    public async Task SaveAsync(string path, string yamlText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("Output path is required.");
        }

        if (string.IsNullOrWhiteSpace(yamlText))
        {
            throw new InvalidOperationException("Cannot save empty YAML.");
        }

        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Output directory could not be resolved.");
        }

        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(path, yamlText, cancellationToken);
    }
}
