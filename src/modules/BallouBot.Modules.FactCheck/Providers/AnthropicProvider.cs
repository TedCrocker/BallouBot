using System.Text;
using System.Text.Json;
using BallouBot.Modules.FactCheck.Models;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.FactCheck.Providers;

/// <summary>
/// AI provider implementation for Anthropic (Claude models).
/// </summary>
public class AnthropicProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AnthropicProvider> _logger;

    /// <inheritdoc />
    public AiProviderType ProviderType => AiProviderType.Anthropic;

    /// <inheritdoc />
    public string DisplayName => "Anthropic";

    /// <inheritdoc />
    public bool RequiresApiKey => true;

    /// <inheritdoc />
    public bool RequiresEndpoint => false;

    /// <inheritdoc />
    public string DefaultModel => "claude-sonnet-4-20250514";

    public AnthropicProvider(HttpClient httpClient, ILogger<AnthropicProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FactCheckResult> AnalyzeAsync(string message, string apiKey, string model, string? endpoint = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Anthropic API key is not configured.");
            return FactCheckResult.Ignore("No API key configured", DisplayName);
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            var body = new
            {
                model = model,
                max_tokens = 300,
                system = FactCheckPrompt.SystemPrompt,
                messages = new[]
                {
                    new { role = "user", content = string.Format(FactCheckPrompt.UserPrompt, message) }
                }
            };

            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Anthropic API returned {StatusCode}: {Response}", response.StatusCode, responseText);
                return FactCheckResult.Ignore($"API error: {response.StatusCode}", DisplayName);
            }

            var json = JsonDocument.Parse(responseText);
            var content = json.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            return FactCheckPrompt.ParseResponse(content.Trim(), DisplayName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Anthropic API");
            return FactCheckResult.Ignore($"Error: {ex.Message}", DisplayName);
        }
    }
}
