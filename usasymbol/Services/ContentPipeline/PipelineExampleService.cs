using Microsoft.Extensions.Options;
using USASymbol.Models.ContentPipeline;
using YamlDotNet.RepresentationModel;

namespace USASymbol.Services.ContentPipeline;

public sealed class PipelineExampleService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ContentPipelineOptions _options;

    public PipelineExampleService(IWebHostEnvironment environment, IOptions<ContentPipelineOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public async Task<string> GetYamlSkeletonAsync(string category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return string.Empty;
        }

        var filePath = Path.Combine(
            _environment.ContentRootPath,
            _options.RootDirectory,
            _options.ExamplesDirectory,
            $"{category.Trim()}.yaml");

        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        var text = await File.ReadAllTextAsync(filePath, cancellationToken);
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        try
        {
            using var reader = new StringReader(text);
            var stream = new YamlStream();
            stream.Load(reader);

            if (stream.Documents.Count > 0 &&
                stream.Documents[0].RootNode is YamlMappingNode root &&
                root.Children.TryGetValue(new YamlScalarNode("yaml_skeleton"), out var skeletonNode) &&
                skeletonNode is YamlScalarNode scalar &&
                !string.IsNullOrWhiteSpace(scalar.Value))
            {
                return NormalizeLineEndings(scalar.Value);
            }
        }
        catch
        {
            // If the example file itself is the skeleton, fall back to raw text.
        }

        return NormalizeLineEndings(text.Trim());
    }

    private static string NormalizeLineEndings(string value)
    {
        return value.Replace("\r\n", "\n").Trim();
    }
}
