using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BallouBot.Modules.FactCheck.Models;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.FactCheck.Providers;

/// <summary>
/// AI provider implementation for OpenAI (GPT-4o, GPT-4o-mini, etc.).
/// </summary>
public class OpenAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiProvider> _logger;

    /// <inheritdoc />
    public AiProviderType ProviderType => AiProviderType.OpenAI;

    /// <inheritdoc />
    public string DisplayName => "OpenAI";

    /// <inheritdoc />
    public bool RequiresApiKey => true;

    /// <inheritdoc />
    public bool RequiresEndpoint => false;

    /// <inheritdoc />
    public string DefaultModel => "gpt-4o-mini";

    public OpenAiProvider(HttpClient httpClient, ILogger<OpenAiProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FactCheckResult> AnalyzeAsync(string message, string apiKey, string model, string? endpoint = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("OpenAI API key is not configured.");
            return FactCheckResult.Ignore("No API key configured", DisplayName);
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var body = new
            {
                model = model,
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
                _logger.LogError("OpenAI API returned {StatusCode}: {Response}", response.StatusCode, responseText);
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
            _logger.LogError(ex, "Error calling OpenAI API");
            return FactCheckResult.Ignore($"Error: {ex.Message}", DisplayName);
        }
    }
}
