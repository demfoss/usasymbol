using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using USASymbol.Models.Ai;

namespace USASymbol.Services.Ai;

public sealed class ClaudeTextClient
{
    private readonly HttpClient _httpClient;
    private readonly ClaudeOptions _options;

    public ClaudeTextClient(HttpClient httpClient, IOptions<AiPipelineOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.Claude;
    }

    public async Task<string> GenerateAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Claude API key is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", _options.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var payload = new
        {
            model = _options.Model,
            max_tokens = 4096,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = input
                }
            }
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Claude request failed: {(int)response.StatusCode} {responseBody}");
        }

        using var document = JsonDocument.Parse(responseBody);
        if (!document.RootElement.TryGetProperty("content", out var contentArray) || contentArray.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Claude response did not contain a content array.");
        }

        var builder = new StringBuilder();

        foreach (var item in contentArray.EnumerateArray())
        {
            if (item.TryGetProperty("type", out var type) &&
                type.GetString() == "text" &&
                item.TryGetProperty("text", out var text))
            {
                builder.AppendLine(text.GetString());
            }
        }

        var result = builder.ToString().Trim();
        if (string.IsNullOrWhiteSpace(result))
        {
            throw new InvalidOperationException("Claude response contained no text blocks.");
        }

        return result;
    }
}
