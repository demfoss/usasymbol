using System.Collections.Concurrent;
using USASymbol.Models.Ai;
using USASymbol.Models.ViewModels;

namespace USASymbol.Services.Ai;

public sealed class AiPipelineJobService
{
    private readonly ConcurrentDictionary<string, AiPipelineJobState> _jobs = new();
    private readonly IServiceScopeFactory _scopeFactory;

    public AiPipelineJobService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public AiPipelineJobState Start(AiPipelineViewModel model, string actionType)
    {
        var state = new AiPipelineJobState
        {
            JobId = Guid.NewGuid().ToString("N"),
            ActionType = string.IsNullOrWhiteSpace(actionType) ? "auto" : actionType.Trim().ToLowerInvariant(),
            Status = "queued",
            CurrentStep = "queued",
            StatusMessage = "Job queued."
        };

        _jobs[state.JobId] = state;

        var snapshot = CloneModel(model);
        _ = Task.Run(() => ExecuteAsync(state.JobId, snapshot, state.ActionType));

        return state;
    }

    public bool TryGet(string jobId, out AiPipelineJobState? state)
    {
        return _jobs.TryGetValue(jobId, out state);
    }

    private async Task ExecuteAsync(string jobId, AiPipelineViewModel model, string actionType)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var pipelineService = scope.ServiceProvider.GetRequiredService<AiPipelineService>();
            var batchService = scope.ServiceProvider.GetRequiredService<AiBatchRunnerService>();

            SetRunning(jobId, actionType, "Job started.");
            var request = model.ToRequest();

            switch (actionType)
            {
                case "brief":
                    SetRunning(jobId, "brief", request.UseClaudeOnlyMode ? "Generating brief with Claude..." : "Generating brief...");
                    Update(jobId, state =>
                    {
                        state.Brief = pipelineService.GenerateBriefAsync(request).GetAwaiter().GetResult();
                        state.Status = "completed";
                        state.CurrentStep = "brief";
                        state.StatusMessage = "Brief generated.";
                    });
                    break;

                case "draft":
                    if (string.IsNullOrWhiteSpace(model.Brief))
                    {
                        throw new InvalidOperationException("Generate or paste a brief first.");
                    }

                    SetRunning(jobId, "draft", "Generating draft...");
                    Update(jobId, state =>
                    {
                        state.Brief = model.Brief;
                        state.Draft = pipelineService.GenerateDraftAsync(request, model.Brief).GetAwaiter().GetResult();
                        state.Status = "completed";
                        state.CurrentStep = "draft";
                        state.StatusMessage = "Draft generated.";
                    });
                    break;

                case "edit":
                    if (string.IsNullOrWhiteSpace(model.Brief))
                    {
                        throw new InvalidOperationException("Generate or paste a brief first.");
                    }

                    if (string.IsNullOrWhiteSpace(model.Draft))
                    {
                        throw new InvalidOperationException("Generate or paste a draft first.");
                    }

                    SetRunning(jobId, "edit", "Generating final edit...");
                    Update(jobId, state =>
                    {
                        state.Brief = model.Brief;
                        state.Draft = model.Draft;
                        state.FinalText = pipelineService.EditAsync(request, model.Brief, model.Draft).GetAwaiter().GetResult();
                        state.Status = "completed";
                        state.CurrentStep = "edit";
                        state.StatusMessage = "Final text generated.";
                    });
                    break;

                case "save":
                    if (string.IsNullOrWhiteSpace(model.FinalText))
                    {
                        throw new InvalidOperationException("Generate or paste final text first.");
                    }

                    SetRunning(jobId, "save", "Saving files...");
                    Update(jobId, state =>
                    {
                        state.Brief = model.Brief;
                        state.Draft = model.Draft;
                        state.FinalText = model.FinalText;
                        state.Result = pipelineService.SaveAsync(request, model.Brief, model.Draft, model.FinalText).GetAwaiter().GetResult();
                        state.Status = "completed";
                        state.CurrentStep = "save";
                        state.StatusMessage = $"Files saved. Article path: {state.Result.SavedArticlePath}";
                    });
                    break;

                case "batch":
                    SetRunning(jobId, "batch", "Running batch jobs...");
                    Update(jobId, state =>
                    {
                        state.BatchResult = batchService.RunAsync(model.BatchFilePath, model.BatchJson).GetAwaiter().GetResult();
                        state.Status = "completed";
                        state.CurrentStep = "batch";
                        state.StatusMessage = $"Batch completed. Done: {state.BatchResult.CompletedJobs}, failed: {state.BatchResult.FailedJobs}.";
                    });
                    break;

                case "auto":
                default:
                    await RunAutoAsync(jobId, pipelineService, request, model);
                    break;
            }
        }
        catch (Exception ex)
        {
            Update(jobId, state =>
            {
                state.Status = "failed";
                state.ErrorMessage = ex.Message;
                state.StatusMessage = "Job failed.";
            });
        }
    }

    private async Task RunAutoAsync(string jobId, AiPipelineService pipelineService, AiPipelineRequest request, AiPipelineViewModel model)
    {
        string brief;

        if (request.UseExistingBriefForAuto && !string.IsNullOrWhiteSpace(request.ExistingBrief))
        {
            brief = request.ExistingBrief.Trim();
            Update(jobId, state =>
            {
                state.Brief = brief;
                state.Status = "running";
                state.CurrentStep = "draft";
                state.StatusMessage = "Using your existing brief. Generating draft...";
            });
        }
        else
        {
            SetRunning(jobId, "brief", request.UseClaudeOnlyMode ? "Generating brief with Claude..." : "Generating brief...");
            brief = await pipelineService.GenerateBriefAsync(request);
            Update(jobId, state => state.Brief = brief);
        }

        SetRunning(jobId, "draft", "Generating draft...");
        var draft = await pipelineService.GenerateDraftAsync(request, brief);
        Update(jobId, state =>
        {
            state.Brief = brief;
            state.Draft = draft;
        });

        SetRunning(jobId, "edit", "Generating final edit...");
        var finalText = await pipelineService.EditAsync(request, brief, draft);
        Update(jobId, state =>
        {
            state.Brief = brief;
            state.Draft = draft;
            state.FinalText = finalText;
        });

        SetRunning(jobId, "save", "Validating and saving files...");
        var result = await pipelineService.SaveAsync(request, brief, draft, finalText);
        Update(jobId, state =>
        {
            state.Brief = brief;
            state.Draft = draft;
            state.FinalText = finalText;
            state.Result = result;
            state.Status = "completed";
            state.CurrentStep = "save";
            state.StatusMessage = request.UseExistingBriefForAuto && !string.IsNullOrWhiteSpace(request.ExistingBrief)
                ? "Automatic pipeline completed from your existing brief and files were saved."
                : "Automatic pipeline completed and files were saved.";
        });
    }

    private void SetRunning(string jobId, string currentStep, string message)
    {
        Update(jobId, state =>
        {
            state.Status = "running";
            state.CurrentStep = currentStep;
            state.StatusMessage = message;
        });
    }

    private void Update(string jobId, Action<AiPipelineJobState> update)
    {
        _jobs.AddOrUpdate(
            jobId,
            _ => throw new InvalidOperationException("Pipeline job not found."),
            (_, state) =>
            {
                update(state);
                state.UpdatedAtUtc = DateTime.UtcNow;
                return state;
            });
    }

    private static AiPipelineViewModel CloneModel(AiPipelineViewModel model)
    {
        return new AiPipelineViewModel
        {
            Topic = model.Topic,
            Notes = model.Notes,
            FileName = model.FileName,
            OutputSubfolder = model.OutputSubfolder,
            TargetFilePath = model.TargetFilePath,
            ExampleFilePath1 = model.ExampleFilePath1,
            ExampleFilePath2 = model.ExampleFilePath2,
            UseOpenAiForEditing = model.UseOpenAiForEditing,
            UseClaudeOnlyMode = model.UseClaudeOnlyMode,
            UseExistingBriefForAuto = model.UseExistingBriefForAuto,
            Brief = model.Brief,
            Draft = model.Draft,
            FinalText = model.FinalText,
            BatchFilePath = model.BatchFilePath,
            BatchJson = model.BatchJson
        };
    }
}
