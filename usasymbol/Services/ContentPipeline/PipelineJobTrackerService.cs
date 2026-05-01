using System.Collections.Concurrent;
using USASymbol.Models.ContentPipeline;

namespace USASymbol.Services.ContentPipeline;

public sealed class PipelineJobTrackerService
{
    private readonly ConcurrentDictionary<string, PipelineJobStateModel> _jobs = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    public PipelineJobStateModel CreateJob()
    {
        var job = new PipelineJobStateModel
        {
            JobId = Guid.NewGuid().ToString("N"),
            Status = "queued",
            StartedAtUtc = DateTime.UtcNow,
            ProgressEntries = new List<PipelineProgressEntryModel>
            {
                new()
                {
                    TimestampUtc = DateTime.UtcNow,
                    Step = "queued",
                    Message = "Pipeline job queued."
                }
            }
        };

        _jobs[job.JobId] = job;
        return job;
    }

    public PipelineJobStateModel? Get(string jobId)
    {
        return _jobs.TryGetValue(jobId, out var job) ? job : null;
    }

    public void Start(string jobId, string step, string message)
    {
        Update(jobId, "running", step, message);
    }

    public void Report(string jobId, string step, string message)
    {
        Update(jobId, "running", step, message);
    }

    public void Complete(string jobId, PipelineRunResultModel result)
    {
        lock (_sync)
        {
            if (!_jobs.TryGetValue(jobId, out var job))
            {
                return;
            }

            var entries = job.ProgressEntries.ToList();
            entries.Add(new PipelineProgressEntryModel
            {
                TimestampUtc = DateTime.UtcNow,
                Step = "completed",
                Message = result.SavedToDisk
                    ? "Pipeline completed and saved the final YAML."
                    : "Pipeline completed, but final YAML was not saved."
            });

            job.Status = "completed";
            job.CurrentStep = "completed";
            job.CurrentMessage = entries[^1].Message;
            job.CompletedAtUtc = DateTime.UtcNow;
            job.ProgressEntries = entries;
            job.Result = result;
            job.ErrorMessage = string.Empty;
        }
    }

    public void Fail(string jobId, Exception exception)
    {
        lock (_sync)
        {
            if (!_jobs.TryGetValue(jobId, out var job))
            {
                return;
            }

            var entries = job.ProgressEntries.ToList();
            entries.Add(new PipelineProgressEntryModel
            {
                TimestampUtc = DateTime.UtcNow,
                Step = "failed",
                Message = exception.Message
            });

            job.Status = "failed";
            job.CurrentStep = "failed";
            job.CurrentMessage = exception.Message;
            job.CompletedAtUtc = DateTime.UtcNow;
            job.ProgressEntries = entries;
            job.ErrorMessage = exception.Message;
        }
    }

    private void Update(string jobId, string status, string step, string message)
    {
        lock (_sync)
        {
            if (!_jobs.TryGetValue(jobId, out var job))
            {
                return;
            }

            var entries = job.ProgressEntries.ToList();
            entries.Add(new PipelineProgressEntryModel
            {
                TimestampUtc = DateTime.UtcNow,
                Step = step,
                Message = message
            });

            job.Status = status;
            job.CurrentStep = step;
            job.CurrentMessage = message;
            job.ProgressEntries = entries;
        }
    }
}
