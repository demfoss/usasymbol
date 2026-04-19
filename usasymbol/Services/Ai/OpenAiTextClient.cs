using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using USASymbol.Models.Ai;

namespace USASymbol.Services.Ai;

public sealed class OpenAiTextClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;

    public OpenAiTextClient(HttpClient httpClient, IOptions<AiPipelineOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.OpenAI;
    }

    public async Task<string> GenerateAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var payload = new
        {
            model = _options.Model,
            input
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI request failed: {(int)response.StatusCode} {responseBody}");
        }

        using var document = JsonDocument.Parse(responseBody);

        if (document.RootElement.TryGetProperty("output_text", out var outputText))
        {
            return outputText.GetString()?.Trim() ?? string.Empty;
        }

        throw new InvalidOperationException("OpenAI response did not contain output_text.");
    }
}
