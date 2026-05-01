using System.ComponentModel.DataAnnotations;
using USASymbol.Models.ContentPipeline;

namespace USASymbol.Models.ViewModels;

public sealed class ContentPipelineViewModel
{
    [Display(Name = "Category")]
    public string Category { get; set; } = "surnames";

    [Display(Name = "Primary keyword")]
    public string PrimaryKeyword { get; set; } = string.Empty;

    [Display(Name = "Topic or state")]
    public string TopicOrState { get; set; } = string.Empty;

    [Display(Name = "YAML skeleton")]
    public string YamlSkeleton { get; set; } = string.Empty;

    [Display(Name = "Source notes")]
    public string SourceNotesText { get; set; } = string.Empty;

    [Display(Name = "Optional extra instruction")]
    public string OptionalExtraInstruction { get; set; } = string.Empty;

    [Display(Name = "Images enabled")]
    public bool ImagesEnabled { get; set; }

    public IReadOnlyList<CategoryOptionViewModel> CategoryOptions { get; set; } = Array.Empty<CategoryOptionViewModel>();
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public PipelineRunResultModel? Result { get; set; }

    public ManualInputModel ToManualInput()
    {
        return new ManualInputModel
        {
            Category = (Category ?? string.Empty).Trim(),
            PrimaryKeyword = (PrimaryKeyword ?? string.Empty).Trim(),
            TopicOrState = (TopicOrState ?? string.Empty).Trim(),
            YamlSkeleton = (YamlSkeleton ?? string.Empty).Trim(),
            SourceNotes = (SourceNotesText ?? string.Empty)
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.StartsWith("- ") ? x[2..].Trim() : x)
                .ToArray(),
            OptionalExtraInstruction = (OptionalExtraInstruction ?? string.Empty).Trim(),
            ImagesEnabled = ImagesEnabled
        };
    }
}

public sealed class CategoryOptionViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
