using Microsoft.Extensions.Options;
using USASymbol.Models.Ai;

namespace USASymbol.Services.Ai;

public sealed class PromptTemplateService
{
    private readonly IWebHostEnvironment _environment;
    private readonly AiPipelineOptions _options;

    public PromptTemplateService(IWebHostEnvironment environment, IOptions<AiPipelineOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public async Task<string> RenderAsync(string templateName, IReadOnlyDictionary<string, string> values)
    {
        var promptDirectory = Path.Combine(_environment.ContentRootPath, _options.PromptDirectory);
        var templatePath = Path.Combine(promptDirectory, templateName);

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Prompt template was not found: {templatePath}");
        }

        var content = await File.ReadAllTextAsync(templatePath);

        foreach (var pair in values)
        {
            content = content.Replace($"{{{{{pair.Key}}}}}", pair.Value);
        }

        return content;
    }

    public async Task<string> BuildExamplesContextAsync(string exampleFilePath1, string exampleFilePath2)
    {
        var examples = new List<(string path, string content)>();

        foreach (var rawPath in new[] { exampleFilePath1, exampleFilePath2 })
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                continue;
            }

            var resolvedPath = ResolveProjectPath(rawPath);
            if (!File.Exists(resolvedPath))
            {
                throw new FileNotFoundException($"Example file was not found: {resolvedPath}");
            }

            var content = await File.ReadAllTextAsync(resolvedPath);
            examples.Add((resolvedPath, content.Trim()));
        }

        if (examples.Count == 0)
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("Use these project examples to understand available structure and components. Do not copy one pattern mechanically.");
        builder.AppendLine();

        for (var i = 0; i < examples.Count; i++)
        {
            builder.AppendLine($"Example {i + 1}: {examples[i].path}");
            builder.AppendLine(examples[i].content);
            builder.AppendLine();
        }

        return builder.ToString().Trim();
    }

    private string ResolveProjectPath(string userPath)
    {
        var normalizedInput = userPath.Trim().Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var combinedPath = Path.IsPathRooted(normalizedInput)
            ? normalizedInput
            : Path.Combine(_environment.ContentRootPath, normalizedInput);

        var fullPath = Path.GetFullPath(combinedPath);
        var projectRoot = Path.GetFullPath(_environment.ContentRootPath);

        if (!fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Example file path must stay inside the current project directory.");
        }

        return fullPath;
    }
}
