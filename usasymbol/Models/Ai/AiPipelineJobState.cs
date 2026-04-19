namespace USASymbol.Models.Ai;

public sealed class AiPipelineJobState
{
    public string JobId { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string Status { get; set; } = "queued";
    public string CurrentStep { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string Brief { get; set; } = string.Empty;
    public string Draft { get; set; } = string.Empty;
    public string FinalText { get; set; } = string.Empty;
    public AiPipelineResult? Result { get; set; }
    public AiBatchRunResult? BatchResult { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
