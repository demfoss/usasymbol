namespace USASymbol.Models.Ai;

public sealed class AiPipelineResult
{
    public string Topic { get; set; } = string.Empty;
    public string Brief { get; set; } = string.Empty;
    public string Draft { get; set; } = string.Empty;
    public string FinalText { get; set; } = string.Empty;
    public string FinalEditor { get; set; } = string.Empty;
    public string SavedArticlePath { get; set; } = string.Empty;
    public string SavedBriefPath { get; set; } = string.Empty;
    public string SavedDraftPath { get; set; } = string.Empty;
    public string SavedFinalPath { get; set; } = string.Empty;
}
