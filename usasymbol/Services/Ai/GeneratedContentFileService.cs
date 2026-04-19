using System.Text;
using Microsoft.Extensions.Options;
using USASymbol.Models.Ai;
using YamlDotNet.Serialization;

namespace USASymbol.Services.Ai;

public sealed class GeneratedContentFileService
{
    private readonly IWebHostEnvironment _environment;
    private readonly AiPipelineOptions _options;

    public GeneratedContentFileService(IWebHostEnvironment environment, IOptions<AiPipelineOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public async Task<(string articlePath, string briefPath, string draftPath, string finalPath)> SaveAsync(
        AiPipelineRequest request,
        AiPipelineResult result,
        CancellationToken cancellationToken = default)
    {
        var slug = BuildSlug(request.FileName, request.Topic);
        var (articlePath, auditDirectory) = ResolveArticlePath(request, slug);
        ValidateFinalContentForTarget(articlePath, result.FinalText);
        Directory.CreateDirectory(Path.GetDirectoryName(articlePath)!);
        Directory.CreateDirectory(auditDirectory);

        var briefPath = Path.Combine(auditDirectory, $"{slug}.brief.md");
        var draftPath = Path.Combine(auditDirectory, $"{slug}.draft.md");
        var finalPath = Path.Combine(auditDirectory, $"{slug}.final.md");

        await File.WriteAllTextAsync(articlePath, result.FinalText, Encoding.UTF8, cancellationToken);
        await File.WriteAllTextAsync(briefPath, result.Brief, Encoding.UTF8, cancellationToken);
        await File.WriteAllTextAsync(draftPath, result.Draft, Encoding.UTF8, cancellationToken);
        await File.WriteAllTextAsync(finalPath, BuildAuditFile(request, result), Encoding.UTF8, cancellationToken);

        return (articlePath, briefPath, draftPath, finalPath);
    }

    private static void ValidateFinalContentForTarget(string articlePath, string content)
    {
        var extension = Path.GetExtension(articlePath);
        if (!string.Equals(extension, ".yaml", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".yml", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var trimmed = content.TrimStart();
        if (trimmed.StartsWith("#", StringComparison.Ordinal) ||
            trimmed.StartsWith("---", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Final output looked like markdown instead of YAML, so it was not saved.");
        }

        try
        {
            var deserializer = new DeserializerBuilder().Build();
            _ = deserializer.Deserialize<object>(content);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Final output was not valid YAML, so it was not saved. {ex.Message}");
        }
    }

    private (string articlePath, string auditDirectory) ResolveArticlePath(AiPipelineRequest request, string slug)
    {
        var projectRoot = Path.GetFullPath(_environment.ContentRootPath);
        var defaultBaseDirectory = Path.Combine(projectRoot, _options.OutputDirectory);

        if (!string.IsNullOrWhiteSpace(request.TargetFilePath))
        {
            var resolvedTargetPath = ResolveUserPath(request.TargetFilePath, projectRoot);
            _ = Path.GetDirectoryName(resolvedTargetPath)
                ?? throw new InvalidOperationException("Target file path must include a directory.");

            var auditDirectory = Path.Combine(defaultBaseDirectory, "_pipeline-audit");
            return (resolvedTargetPath, auditDirectory);
        }

        var safeSubfolder = SanitizePathSegment(request.OutputSubfolder);
        var targetDirectory = string.IsNullOrWhiteSpace(safeSubfolder)
            ? defaultBaseDirectory
            : Path.Combine(defaultBaseDirectory, safeSubfolder);

        var articlePath = Path.Combine(targetDirectory, $"{slug}.md");
        return (articlePath, targetDirectory);
    }

    private static string ResolveUserPath(string userPath, string projectRoot)
    {
        var normalizedInput = userPath.Trim().Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var combinedPath = Path.IsPathRooted(normalizedInput)
            ? normalizedInput
            : Path.Combine(projectRoot, normalizedInput);

        var fullPath = Path.GetFullPath(combinedPath);
        var fullProjectRoot = Path.GetFullPath(projectRoot);

        if (!fullPath.StartsWith(fullProjectRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Target file path must stay inside the current project directory.");
        }

        var extension = Path.GetExtension(fullPath);
        if (!string.Equals(extension, ".yaml", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".yml", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Target file path must end with .yaml, .yml, or .md.");
        }

        return fullPath;
    }

    private static string BuildAuditFile(AiPipelineRequest request, AiPipelineResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {request.Topic}");
        builder.AppendLine();
        builder.AppendLine($"Final editor: {result.FinalEditor}");
        builder.AppendLine();
        builder.AppendLine("## Notes");
        builder.AppendLine();
        builder.AppendLine(string.IsNullOrWhiteSpace(request.Notes) ? "(empty)" : request.Notes);
        builder.AppendLine();
        builder.AppendLine("## Brief");
        builder.AppendLine();
        builder.AppendLine(result.Brief);
        builder.AppendLine();
        builder.AppendLine("## Draft");
        builder.AppendLine();
        builder.AppendLine(result.Draft);
        builder.AppendLine();
        builder.AppendLine("## Final");
        builder.AppendLine();
        builder.AppendLine(result.FinalText);
        return builder.ToString();
    }

    private static string BuildSlug(string requestedFileName, string topic)
    {
        var raw = string.IsNullOrWhiteSpace(requestedFileName) ? topic : requestedFileName;
        var chars = raw
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();

        var compact = new string(chars);
        while (compact.Contains("--", StringComparison.Ordinal))
        {
            compact = compact.Replace("--", "-", StringComparison.Ordinal);
        }

        var slug = compact.Trim('-');
        return string.IsNullOrWhiteSpace(slug)
            ? $"generated-{DateTime.UtcNow:yyyyMMddHHmmss}"
            : slug;
    }

    private static string SanitizePathSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
        {
            return string.Empty;
        }

        var parts = segment
            .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => new string(part.Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_').ToArray()))
            .Where(part => !string.IsNullOrWhiteSpace(part));

        return Path.Combine(parts.ToArray());
    }
}
