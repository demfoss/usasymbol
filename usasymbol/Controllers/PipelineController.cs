using Microsoft.AspNetCore.Mvc;
using USASymbol.Models.ContentPipeline;
using USASymbol.Models.ViewModels;
using USASymbol.Services.ContentPipeline;
using USASymbol.Services.ContentPipeline.Runners;

namespace USASymbol.Controllers;

public sealed class PipelineController : Controller
{
    private static readonly CategoryOptionViewModel[] DefaultCategories =
    {
        new() { Key = "surnames", Label = "Surnames" },
        new() { Key = "state-seals", Label = "State Seals" },
        new() { Key = "ranking", Label = "Ranking" }
    };

    private readonly ContentPipelineAccessService _accessService;
    private readonly PipelineRunner _pipelineRunner;
    private readonly PipelineJobTrackerService _pipelineJobTrackerService;
    private readonly PipelineExampleService _pipelineExampleService;
    private readonly PipelinePreflightService _pipelinePreflightService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public PipelineController(
        ContentPipelineAccessService accessService,
        PipelineRunner pipelineRunner,
        PipelineJobTrackerService pipelineJobTrackerService,
        PipelineExampleService pipelineExampleService,
        PipelinePreflightService pipelinePreflightService,
        IServiceScopeFactory serviceScopeFactory)
    {
        _accessService = accessService;
        _pipelineRunner = pipelineRunner;
        _pipelineJobTrackerService = pipelineJobTrackerService;
        _pipelineExampleService = pipelineExampleService;
        _pipelinePreflightService = pipelinePreflightService;
        _serviceScopeFactory = serviceScopeFactory;
    }

    [HttpGet("/pipeline")]
    public IActionResult Index()
    {
        if (!_accessService.IsEnabled())
        {
            return NotFound();
        }

        ViewData["Title"] = "Content Pipeline";
        ViewData["BodyClass"] = "pipeline-page";

        return View(Enrich(new ContentPipelineViewModel()));
    }

    [HttpPost("/pipeline")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ContentPipelineViewModel model, CancellationToken cancellationToken)
    {
        if (!_accessService.IsEnabled())
        {
            return NotFound();
        }

        ViewData["Title"] = "Content Pipeline";
        ViewData["BodyClass"] = "pipeline-page";

        if (string.IsNullOrWhiteSpace(model.Category))
        {
            model.ErrorMessage = "Category is required.";
            return View(Enrich(model));
        }

        if (string.IsNullOrWhiteSpace(model.PrimaryKeyword))
        {
            model.ErrorMessage = "Primary keyword is required.";
            return View(Enrich(model));
        }

        if (string.IsNullOrWhiteSpace(model.TopicOrState))
        {
            model.ErrorMessage = "Topic or state is required.";
            return View(Enrich(model));
        }

        if (string.IsNullOrWhiteSpace(model.YamlSkeleton))
        {
            model.ErrorMessage = "YAML skeleton is required.";
            return View(Enrich(model));
        }

        if (string.IsNullOrWhiteSpace(model.SourceNotesText))
        {
            model.ErrorMessage = "Source notes are required.";
            return View(Enrich(model));
        }

        var preflightIssues = _pipelinePreflightService.Validate(model.ToManualInput());
        if (preflightIssues.Count > 0)
        {
            model.ErrorMessage = $"Preflight blocked the live run: {string.Join(" ", preflightIssues)}";
            return View(Enrich(model));
        }

        try
        {
            model.Result = await _pipelineRunner.RunAsync(model.ToManualInput(), null, cancellationToken);
            model.StatusMessage = model.Result.SavedToDisk
                ? $"Pipeline completed. Final YAML was generated and saved to {model.Result.SavePath}."
                : "Pipeline ran, but the final YAML was not saved because checks still found blocking issues.";
        }
        catch (Exception ex)
        {
            model.ErrorMessage = ex.Message;
        }

        return View(Enrich(model));
    }

    [HttpPost("/pipeline/start")]
    [ValidateAntiForgeryToken]
    public IActionResult Start(ContentPipelineViewModel model)
    {
        if (!_accessService.IsEnabled())
        {
            return NotFound();
        }

        var validationError = ValidateModel(model, _pipelinePreflightService);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        var input = model.ToManualInput();
        var job = _pipelineJobTrackerService.CreateJob();
        _pipelineJobTrackerService.Start(job.JobId, "starting", "Pipeline job started.");

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<PipelineRunner>();
                var tracker = scope.ServiceProvider.GetRequiredService<PipelineJobTrackerService>();

                var progress = new Progress<PipelineProgressEntryModel>(entry =>
                {
                    tracker.Report(job.JobId, entry.Step, entry.Message);
                });

                var result = await runner.RunAsync(input, progress, CancellationToken.None);
                tracker.Complete(job.JobId, result);
            }
            catch (Exception ex)
            {
                _pipelineJobTrackerService.Fail(job.JobId, ex);
            }
        });

        return Json(new { jobId = job.JobId });
    }

    [HttpGet("/pipeline/status/{jobId}")]
    public IActionResult Status(string jobId)
    {
        if (!_accessService.IsEnabled())
        {
            return NotFound();
        }

        var job = _pipelineJobTrackerService.Get(jobId);
        if (job is null)
        {
            return NotFound(new { error = "Pipeline job not found." });
        }

        return Json(job);
    }

    [HttpGet("/pipeline/template/{category}")]
    public async Task<IActionResult> Template(string category, CancellationToken cancellationToken)
    {
        if (!_accessService.IsEnabled())
        {
            return NotFound();
        }

        var yamlSkeleton = await _pipelineExampleService.GetYamlSkeletonAsync(category, cancellationToken);
        return Json(new
        {
            category,
            yamlSkeleton
        });
    }

    private static ContentPipelineViewModel Enrich(ContentPipelineViewModel model)
    {
        model.CategoryOptions = DefaultCategories;

        if (string.IsNullOrWhiteSpace(model.Category))
        {
            model.Category = DefaultCategories[0].Key;
        }

        return model;
    }

    private static string? ValidateModel(ContentPipelineViewModel model, PipelinePreflightService preflightService)
    {
        if (string.IsNullOrWhiteSpace(model.Category))
        {
            return "Category is required.";
        }

        if (string.IsNullOrWhiteSpace(model.PrimaryKeyword))
        {
            return "Primary keyword is required.";
        }

        if (string.IsNullOrWhiteSpace(model.TopicOrState))
        {
            return "Topic or state is required.";
        }

        if (string.IsNullOrWhiteSpace(model.YamlSkeleton))
        {
            return "YAML skeleton is required.";
        }

        if (string.IsNullOrWhiteSpace(model.SourceNotesText))
        {
            return "Source notes are required.";
        }

        var issues = preflightService.Validate(model.ToManualInput());
        if (issues.Count > 0)
        {
            return $"Preflight blocked the live run: {string.Join(" ", issues)}";
        }

        return null;
    }
}
