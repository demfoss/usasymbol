namespace USASymbol.Models.Ai;

public sealed class AiBatchJobInput
{
    public string Topic { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string OutputSubfolder { get; set; } = string.Empty;
    public string TargetFilePath { get; set; } = string.Empty;
    public string ExampleFilePath1 { get; set; } = string.Empty;
    public string ExampleFilePath2 { get; set; } = string.Empty;
    public string Brief { get; set; } = string.Empty;
    public bool UseExistingBriefForAuto { get; set; } = true;
    public bool UseClaudeOnlyMode { get; set; }
    public bool UseOpenAiForEditing { get; set; } = true;

    public AiPipelineRequest ToPipelineRequest()
    {
        return new AiPipelineRequest
        {
            Topic = Topic.Trim(),
            Notes = Notes.Trim(),
            FileName = FileName.Trim(),
            OutputSubfolder = OutputSubfolder.Trim(),
            TargetFilePath = TargetFilePath.Trim(),
            ExampleFilePath1 = ExampleFilePath1.Trim(),
            ExampleFilePath2 = ExampleFilePath2.Trim(),
            ExistingBrief = Brief.Trim(),
            UseExistingBriefForAuto = UseExistingBriefForAuto,
            UseClaudeOnlyMode = UseClaudeOnlyMode,
            UseOpenAiForEditing = UseOpenAiForEditing
        };
    }
}
