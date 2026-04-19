using System.ComponentModel.DataAnnotations;
using USASymbol.Models.Ai;

namespace USASymbol.Models.ViewModels;

public sealed class AiPipelineViewModel
{
    private string _topic = string.Empty;
    private string _notes = string.Empty;
    private string _fileName = string.Empty;
    private string _outputSubfolder = string.Empty;
    private string _targetFilePath = string.Empty;
    private string _exampleFilePath1 = string.Empty;
    private string _exampleFilePath2 = string.Empty;
    private string _brief = string.Empty;
    private string _draft = string.Empty;
    private string _finalText = string.Empty;
    private string _batchFilePath = string.Empty;
    private string _batchJson = string.Empty;
    private string _lastAction = string.Empty;

    [Display(Name = "Topic")]
    public string Topic
    {
        get => _topic;
        set => _topic = value ?? string.Empty;
    }

    [Display(Name = "Facts, jokes, links, notes")]
    public string Notes
    {
        get => _notes;
        set => _notes = value ?? string.Empty;
    }

    [Display(Name = "Output file name")]
    public string FileName
    {
        get => _fileName;
        set => _fileName = value ?? string.Empty;
    }

    [Display(Name = "Subfolder inside Content/generated")]
    public string OutputSubfolder
    {
        get => _outputSubfolder;
        set => _outputSubfolder = value ?? string.Empty;
    }

    [Display(Name = "Exact target file path")]
    public string TargetFilePath
    {
        get => _targetFilePath;
        set => _targetFilePath = value ?? string.Empty;
    }

    [Display(Name = "Example file path 1")]
    public string ExampleFilePath1
    {
        get => _exampleFilePath1;
        set => _exampleFilePath1 = value ?? string.Empty;
    }

    [Display(Name = "Example file path 2")]
    public string ExampleFilePath2
    {
        get => _exampleFilePath2;
        set => _exampleFilePath2 = value ?? string.Empty;
    }

    [Display(Name = "Use ChatGPT for the final edit")]
    public bool UseOpenAiForEditing { get; set; } = true;

    [Display(Name = "Run everything with Claude")]
    public bool UseClaudeOnlyMode { get; set; }

    [Display(Name = "Use my brief for auto mode")]
    public bool UseExistingBriefForAuto { get; set; }

    [Display(Name = "Generated brief")]
    public string Brief
    {
        get => _brief;
        set => _brief = value ?? string.Empty;
    }

    [Display(Name = "Generated draft")]
    public string Draft
    {
        get => _draft;
        set => _draft = value ?? string.Empty;
    }

    [Display(Name = "Final text")]
    public string FinalText
    {
        get => _finalText;
        set => _finalText = value ?? string.Empty;
    }

    [Display(Name = "Batch file path")]
    public string BatchFilePath
    {
        get => _batchFilePath;
        set => _batchFilePath = value ?? string.Empty;
    }

    [Display(Name = "Batch JSON")]
    public string BatchJson
    {
        get => _batchJson;
        set => _batchJson = value ?? string.Empty;
    }

    public string LastAction
    {
        get => _lastAction;
        set => _lastAction = value ?? string.Empty;
    }
    public string? ErrorMessage { get; set; }
    public string? StatusMessage { get; set; }
    public AiPipelineResult? Result { get; set; }
    public AiBatchRunResult? BatchResult { get; set; }

    public AiPipelineRequest ToRequest()
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
