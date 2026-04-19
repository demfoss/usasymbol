using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using USASymbol.Models.Ai;

namespace USASymbol.Services.Ai;

public sealed class AiBatchRunnerService
{
    private readonly IWebHostEnvironment _environment;
    private readonly AiPipelineService _pipelineService;
    private readonly AiPipelineAccessService _accessService;
    private readonly AiPipelineOptions _options;

    public AiBatchRunnerService(
        IWebHostEnvironment environment,
        AiPipelineService pipelineService,
        AiPipelineAccessService accessService,
        IOptions<AiPipelineOptions> options)
    {
        _environment = environment;
        _pipelineService = pipelineService;
        _accessService = accessService;
        _options = options.Value;
    }

    public async Task<AiBatchRunResult> RunAsync(
        string batchFilePath,
        string batchJson,
        CancellationToken cancellationToken = default)
    {
        _accessService.EnsureEnabled();

        var jobs = await LoadJobsAsync(batchFilePath, batchJson, cancellationToken);
        var result = new AiBatchRunResult
        {
            TotalJobs = jobs.Count
        };

        for (var i = 0; i < jobs.Count; i++)
        {
            var job = jobs[i];
            var item = new AiBatchRunItemResult
            {
                Index = i + 1,
                Topic = job.Topic
            };

            try
            {
                var pipelineResult = await _pipelineService.RunAsync(job.ToPipelineRequest(), cancellationToken);
                item.Status = "done";
                item.Message = "Generated and saved.";
                item.ArticlePath = pipelineResult.SavedArticlePath;
                result.CompletedJobs++;
            }
            catch (Exception ex)
            {
                item.Status = "failed";
                item.Message = ex.Message;
                result.FailedJobs++;
            }

            result.Items.Add(item);
        }

        result.ReportPath = await SaveReportAsync(result, cancellationToken);
        return result;
    }

    private async Task<List<AiBatchJobInput>> LoadJobsAsync(string batchFilePath, string batchJson, CancellationToken cancellationToken)
    {
        string rawJson;

        if (!string.IsNullOrWhiteSpace(batchJson))
        {
            rawJson = batchJson;
        }
        else if (!string.IsNullOrWhiteSpace(batchFilePath))
        {
            var path = ResolveProjectPath(batchFilePath);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Batch file was not found: {path}");
            }

            rawJson = await File.ReadAllTextAsync(path, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("Provide batch JSON or a batch file path.");
        }

        var jobs = JsonSerializer.Deserialize<List<AiBatchJobInput>>(rawJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (jobs is null || jobs.Count == 0)
        {
            throw new InvalidOperationException("Batch input is empty or could not be parsed.");
        }

        if (jobs.Any(job => string.IsNullOrWhiteSpace(job.Topic)))
        {
            throw new InvalidOperationException("Every batch job must include a topic.");
        }

        return jobs;
    }

    private async Task<string> SaveReportAsync(AiBatchRunResult result, CancellationToken cancellationToken)
    {
        var outputDirectory = Path.Combine(_environment.ContentRootPath, _options.OutputDirectory, "_pipeline-audit");
        Directory.CreateDirectory(outputDirectory);

        var path = Path.Combine(outputDirectory, $"batch-{DateTime.UtcNow:yyyyMMddHHmmss}.md");
        var builder = new StringBuilder();
        builder.AppendLine("# Batch Pipeline Report");
        builder.AppendLine();
        builder.AppendLine($"Total: {result.TotalJobs}");
        builder.AppendLine($"Completed: {result.CompletedJobs}");
        builder.AppendLine($"Failed: {result.FailedJobs}");
        builder.AppendLine();

        foreach (var item in result.Items)
        {
            builder.AppendLine($"## {item.Index}. {item.Topic}");
            builder.AppendLine();
            builder.AppendLine($"Status: {item.Status}");
            builder.AppendLine($"Message: {item.Message}");

            if (!string.IsNullOrWhiteSpace(item.ArticlePath))
            {
                builder.AppendLine($"Article: {item.ArticlePath}");
            }

            builder.AppendLine();
        }

        await File.WriteAllTextAsync(path, builder.ToString(), cancellationToken);
        return path;
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
            throw new InvalidOperationException("Batch file path must stay inside the current project directory.");
        }

        return fullPath;
    }
}
