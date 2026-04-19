namespace USASymbol.Models.Ai;

public sealed class AiBatchRunResult
{
    public int TotalJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int FailedJobs { get; set; }
    public string ReportPath { get; set; } = string.Empty;
    public List<AiBatchRunItemResult> Items { get; set; } = new();
}

public sealed class AiBatchRunItemResult
{
    public int Index { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ArticlePath { get; set; } = string.Empty;
}
