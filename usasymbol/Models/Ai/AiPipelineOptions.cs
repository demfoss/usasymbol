namespace USASymbol.Models.Ai;

public sealed class AiPipelineOptions
{
    public bool EnabledOutsideDevelopment { get; set; }
    public string PromptDirectory { get; set; } = "Content/ai-prompts";
    public string OutputDirectory { get; set; } = "Content/generated";
    public OpenAiOptions OpenAI { get; set; } = new();
    public ClaudeOptions Claude { get; set; } = new();
}

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-5-mini";
}

public sealed class ClaudeOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-4-5";
}
