namespace USASymbol.Models.Ai;

public sealed class AiPipelineRequest
{
    public string Topic { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string OutputSubfolder { get; set; } = string.Empty;
    public string TargetFilePath { get; set; } = string.Empty;
    public string ExampleFilePath1 { get; set; } = string.Empty;
    public string ExampleFilePath2 { get; set; } = string.Empty;
    public string ExistingBrief { get; set; } = string.Empty;
    public bool UseExistingBriefForAuto { get; set; }
    public bool UseClaudeOnlyMode { get; set; }
    public bool UseOpenAiForEditing { get; set; }
}
