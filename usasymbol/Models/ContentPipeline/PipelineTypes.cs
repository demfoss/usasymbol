using System.Text.Json.Serialization;

namespace USASymbol.Models.ContentPipeline;

public sealed class ManualInputModel
{
    public string Category { get; set; } = string.Empty;
    public string PrimaryKeyword { get; set; } = string.Empty;
    public string TopicOrState { get; set; } = string.Empty;
    public string YamlSkeleton { get; set; } = string.Empty;
    public IReadOnlyList<string> SourceNotes { get; set; } = Array.Empty<string>();
    public string OptionalExtraInstruction { get; set; } = string.Empty;
    public bool ImagesEnabled { get; set; }
}

public sealed class PromptPayloadModel
{
    public string Category { get; set; } = string.Empty;
    public string PrimaryKeyword { get; set; } = string.Empty;
    public string TopicOrState { get; set; } = string.Empty;
    public string YamlSkeleton { get; set; } = string.Empty;
    public IReadOnlyList<string> SourceNotes { get; set; } = Array.Empty<string>();
    public string OptionalExtraInstruction { get; set; } = string.Empty;
    public bool ImagesEnabled { get; set; }
    public IReadOnlyList<string> DefaultSecondaryQueries { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> TitleExamples { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> SeoDescriptionExamples { get; set; } = Array.Empty<string>();
    public IReadOnlyList<InternalLinkCandidateModel> InternalLinks { get; set; } = Array.Empty<InternalLinkCandidateModel>();
    public IReadOnlyList<string> RecentPatternWarnings { get; set; } = Array.Empty<string>();
    public string SuggestedOutputPath { get; set; } = string.Empty;
}

public sealed class CategoryConfigModel
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("output_path_pattern")]
    public string OutputPathPattern { get; set; } = string.Empty;

    [JsonPropertyName("default_secondary_queries")]
    public IReadOnlyList<string> DefaultSecondaryQueries { get; set; } = Array.Empty<string>();

    [JsonPropertyName("title_examples")]
    public IReadOnlyList<string> TitleExamples { get; set; } = Array.Empty<string>();

    [JsonPropertyName("seo_description_examples")]
    public IReadOnlyList<string> SeoDescriptionExamples { get; set; } = Array.Empty<string>();

}

public sealed class PatternMemoryEntryModel
{
    public string Id { get; set; } = string.Empty;
    public string CategoryKey { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
}

public sealed class InternalLinkCandidateModel
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string AnchorHint { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class ContentIndexEntryModel
{
    public string FilePath { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string StateSlug { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string H1 { get; set; } = string.Empty;
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
}

public sealed class SimilarityReportModel
{
    public bool IsWarning { get; set; }
    public double Score { get; set; }
    public string ComparedPath { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

public sealed class PipelineCheckIssueModel
{
    public string Code { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TargetFragment { get; set; } = string.Empty;
}

public sealed class PipelineCheckReportModel
{
    public bool WasRun { get; set; }
    public bool IsSuccess { get; set; }
    public string Summary { get; set; } = string.Empty;
    public IReadOnlyList<PipelineCheckIssueModel> Issues { get; set; } = Array.Empty<PipelineCheckIssueModel>();
}

public sealed class PipelineRunResultModel
{
    public PromptPayloadModel Payload { get; set; } = new();
    public string GeneratorPrompt { get; set; } = string.Empty;
    public string GeneratorRawResponse { get; set; } = string.Empty;
    public string GeneratorYaml { get; set; } = string.Empty;
    public string FinisherPrompt { get; set; } = string.Empty;
    public string FinisherRawResponse { get; set; } = string.Empty;
    public string FinisherYaml { get; set; } = string.Empty;
    public string FinisherNotes { get; set; } = string.Empty;
    public PipelineCheckReportModel Checks { get; set; } = new();
    public SimilarityReportModel Similarity { get; set; } = new();
    public string FinalYaml { get; set; } = string.Empty;
    public bool SavedToDisk { get; set; }
    public string SavePath { get; set; } = string.Empty;
    public bool ImagesPipelineQueued { get; set; }
    public string ExecutionMode { get; set; } = string.Empty;
}

public sealed class PipelineProgressEntryModel
{
    public DateTime TimestampUtc { get; set; }
    public string Step { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class PipelineJobStateModel
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = "queued";
    public string CurrentStep { get; set; } = string.Empty;
    public string CurrentMessage { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public IReadOnlyList<PipelineProgressEntryModel> ProgressEntries { get; set; } = Array.Empty<PipelineProgressEntryModel>();
    public PipelineRunResultModel? Result { get; set; }
}
