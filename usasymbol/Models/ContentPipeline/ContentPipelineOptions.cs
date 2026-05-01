namespace USASymbol.Models.ContentPipeline;

public sealed class ContentPipelineOptions
{
    public const string SectionName = "ContentPipeline";

    public bool EnabledOutsideDevelopment { get; set; }
    public string RootDirectory { get; set; } = "Content/content-pipeline";
    public string ConfigsDirectory { get; set; } = "configs";
    public string PromptsDirectory { get; set; } = "prompts";
    public string SchemasDirectory { get; set; } = "schemas";
    public string DataDirectory { get; set; } = "data";
    public string ExamplesDirectory { get; set; } = "examples";
}

public sealed class ContentPipelineClaudeOptions
{
    public const string SectionName = "AiPipeline:Claude";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.anthropic.com";
    public string Model { get; set; } = "claude-sonnet-4-6";
    public string AnthropicVersion { get; set; } = "2023-06-01";
    public int GeneratorMaxTokens { get; set; } = 12000;
    public int FinisherMaxTokens { get; set; } = 12000;
}
