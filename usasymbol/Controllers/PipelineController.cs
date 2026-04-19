using Microsoft.AspNetCore.Mvc;
using USASymbol.Models.ViewModels;
using USASymbol.Services.Ai;

namespace USASymbol.Controllers;

public sealed class PipelineController : Controller
{
    private readonly AiPipelineAccessService _accessService;
    private readonly AiPipelineService _aiPipelineService;
    private readonly AiBatchRunnerService _batchRunnerService;
    private readonly AiPipelineJobService _jobService;

    public PipelineController(
        AiPipelineAccessService accessService,
        AiPipelineService aiPipelineService,
        AiBatchRunnerService batchRunnerService,
        AiPipelineJobService jobService)
    {
        _accessService = accessService;
        _aiPipelineService = aiPipelineService;
        _batchRunnerService = batchRunnerService;
        _jobService = jobService;
    }

    [HttpGet("/pipeline")]
    public IActionResult Index()
    {
        if (!_accessService.IsEnabled())
        {
            return NotFound();
        }

        ViewData["Title"] = "AI Pipeline";
        ViewData["BodyClass"] = "pipeline-page";
        return View(new AiPipelineViewModel());
    }

    [HttpPost("/pipeline")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AiPipelineViewModel model, string actionType, CancellationToken cancellationToken)
    {
        if (!_accessService.IsEnabled())
        {
            return NotFound();
        }

        ViewData["Title"] = "AI Pipeline";
        ViewData["BodyClass"] = "pipeline-page";
        model.LastAction = actionType ?? string.Empty;

        if (!string.Equals(actionType, "batch", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(model.Topic))
        {
            model.ErrorMessage = "Topic is required for single-page pipeline runs.";
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var request = model.ToRequest();

            switch ((actionType ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "brief":
                    model.Brief = await _aiPipelineService.GenerateBriefAsync(request, cancellationToken);
                    model.StatusMessage = request.UseClaudeOnlyMode
                        ? "Brief generated with Claude. You can edit it manually or move to draft."
                        : "Brief generated. You can edit it manually or move to draft.";
                    break;

                case "draft":
                    if (string.IsNullOrWhiteSpace(model.Brief))
                    {
                        model.ErrorMessage = "Generate or paste a brief first.";
                        break;
                    }

                    model.Draft = await _aiPipelineService.GenerateDraftAsync(request, model.Brief, cancellationToken);
                    model.StatusMessage = "Draft generated. You can edit it manually or run the final edit.";
                    break;

                case "edit":
                    if (string.IsNullOrWhiteSpace(model.Brief))
                    {
                        model.ErrorMessage = "Generate or paste a brief first.";
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(model.Draft))
                    {
                        model.ErrorMessage = "Generate or paste a draft first.";
                        break;
                    }

                    model.FinalText = await _aiPipelineService.EditAsync(request, model.Brief, model.Draft, cancellationToken);
                    model.StatusMessage = "Final edit generated. Review it and save when ready.";
                    break;

                case "save":
                    if (string.IsNullOrWhiteSpace(model.FinalText))
                    {
                        model.ErrorMessage = "Generate or paste final text first.";
                        break;
                    }

                    model.Result = await _aiPipelineService.SaveAsync(
                        request,
                        model.Brief,
                        model.Draft,
                        model.FinalText,
                        cancellationToken);
                    model.StatusMessage = $"Files saved. Article path: {model.Result.SavedArticlePath}";
                    break;

                case "batch":
                    model.BatchResult = await _batchRunnerService.RunAsync(model.BatchFilePath, model.BatchJson, cancellationToken);
                    model.StatusMessage = $"Batch completed. Done: {model.BatchResult.CompletedJobs}, failed: {model.BatchResult.FailedJobs}.";
                    break;

                case "auto":
                default:
                    model.Result = await _aiPipelineService.RunAsync(request, cancellationToken);
                    model.Brief = model.Result.Brief;
                    model.Draft = model.Result.Draft;
                    model.FinalText = model.Result.FinalText;
                    model.StatusMessage = request.UseExistingBriefForAuto && !string.IsNullOrWhiteSpace(request.ExistingBrief)
                        ? "Automatic pipeline completed from your existing brief and files were saved."
                        : "Automatic pipeline completed and files were saved.";
                    break;
            }
        }
        catch (Exception ex)
        {
            model.ErrorMessage = ex.Message;
        }

        return View(model);
    }

    [HttpPost("/pipeline/start")]
    [ValidateAntiForgeryToken]
    public IActionResult Start(AiPipelineViewModel model, string actionType)
    {
        if (!_accessService.IsEnabled())
        {
            return NotFound();
        }

        if (!string.Equals(actionType, "batch", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(model.Topic))
        {
            return BadRequest(new { error = "Topic is required for single-page pipeline runs." });
        }

        var job = _jobService.Start(model, actionType);
        return Json(new { jobId = job.JobId, status = job.Status, currentStep = job.CurrentStep, message = job.StatusMessage });
    }

    [HttpGet("/pipeline/status/{jobId}")]
    public IActionResult Status(string jobId)
    {
        if (!_accessService.IsEnabled())
        {
            return NotFound();
        }

        if (!_jobService.TryGet(jobId, out var state) || state is null)
        {
            return NotFound(new { error = "Job not found." });
        }

        return Json(state);
    }
}
