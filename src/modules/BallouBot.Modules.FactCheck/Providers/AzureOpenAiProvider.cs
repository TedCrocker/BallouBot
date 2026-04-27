using System.Text;
using System.Text.Json;
using BallouBot.Modules.FactCheck.Models;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.FactCheck.Providers;

/// <summary>
/// AI provider implementation for Azure OpenAI.
/// </summary>
public class AzureOpenAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureOpenAiProvider> _logger;

    /// <inheritdoc />
    public AiProviderType ProviderType => AiProviderType.AzureOpenAI;

    /// <inheritdoc />
    public string DisplayName => "Azure OpenAI";

    /// <inheritdoc />
    public bool RequiresApiKey => true;

    /// <inheritdoc />
    public bool RequiresEndpoint => true;

    /// <inheritdoc />
    public string DefaultModel => "gpt-4o-mini";

    public AzureOpenAiProvider(HttpClient httpClient, ILogger<AzureOpenAiProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FactCheckResult> AnalyzeAsync(string message, string apiKey, string model, string? endpoint = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Azure OpenAI API key is not configured.");
            return FactCheckResult.Ignore("No API key configured", DisplayName);
        }

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _logger.LogWarning("Azure OpenAI endpoint is not configured.");
            return FactCheckResult.Ignore("No endpoint configured", DisplayName);
        }

        try
        {
            var url = $"{endpoint.TrimEnd('/')}/openai/deployments/{model}/chat/completions?api-version=2024-02-01";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("api-key", apiKey);

            var body = new
            {
                messages = new[]
                {
                    new { role = "system", content = FactCheckPrompt.SystemPrompt },
                    new { role = "user", content = string.Format(FactCheckPrompt.UserPrompt, message) }
                },
                max_tokens = 300,
                temperature = 0.1
            };

            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Azure OpenAI API returned {StatusCode}: {Response}", response.StatusCode, responseText);
                return FactCheckResult.Ignore($"API error: {response.StatusCode}", DisplayName);
            }

            var json = JsonDocument.Parse(responseText);
            var content = json.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            return FactCheckPrompt.ParseResponse(content.Trim(), DisplayName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Azure OpenAI API");
            return FactCheckResult.Ignore($"Error: {ex.Message}", DisplayName);
        }
    }
}
