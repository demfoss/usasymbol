using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using USASymbol.Models.ContentPipeline;

namespace USASymbol.Services.ContentPipeline;

public sealed class AnthropicContentClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ContentPipelineClaudeOptions _options;

    public AnthropicContentClient(HttpClient httpClient, IOptions<ContentPipelineClaudeOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public Task<string> GenerateDraftAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return SendAsync(prompt, _options.GeneratorMaxTokens, 0.5, cancellationToken);
    }

    public Task<string> FinishDraftAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return SendAsync(prompt, _options.FinisherMaxTokens, 0.2, cancellationToken);
    }

    private async Task<string> SendAsync(string prompt, int maxTokens, double temperature, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("AiPipeline:Claude:ApiKey is required for live content generation.");
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages");
        request.Headers.Add("x-api-key", _options.ApiKey);
        request.Headers.Add("anthropic-version", _options.AnthropicVersion);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var body = new
        {
            model = _options.Model,
            max_tokens = maxTokens,
            temperature,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Anthropic request failed with {(int)response.StatusCode}: {responseText}");
        }

        var payload = JsonSerializer.Deserialize<AnthropicMessageResponse>(responseText, JsonOptions)
            ?? throw new InvalidOperationException("Anthropic response was empty.");

        var text = string.Concat(payload.Content?
            .Where(x => string.Equals(x.Type, "text", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Text) ?? Array.Empty<string>());

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Anthropic returned no text content.");
        }

        return text.Trim();
    }

    private sealed class AnthropicMessageResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public List<AnthropicContentBlock> Content { get; set; } = new();
    }

    private sealed class AnthropicContentBlock
    {
        public string Type { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
