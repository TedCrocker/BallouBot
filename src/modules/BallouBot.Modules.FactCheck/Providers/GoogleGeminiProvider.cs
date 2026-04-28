using System.Text;
using System.Text.Json;
using BallouBot.Modules.FactCheck.Models;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.FactCheck.Providers;

/// <summary>
/// AI provider implementation for Google Gemini (Gemini 2.0 Flash, etc.).
/// Free tier: 15 RPM, 1M tokens/day at https://aistudio.google.com/
/// </summary>
public class GoogleGeminiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleGeminiProvider> _logger;

    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    /// <inheritdoc />
    public AiProviderType ProviderType => AiProviderType.Google;

    /// <inheritdoc />
    public string DisplayName => "Google Gemini";

    /// <inheritdoc />
    public bool RequiresApiKey => true;

    /// <inheritdoc />
    public bool RequiresEndpoint => false;

    /// <inheritdoc />
    public string DefaultModel => "gemini-2.0-flash";

    public GoogleGeminiProvider(HttpClient httpClient, ILogger<GoogleGeminiProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FactCheckResult> AnalyzeAsync(string message, string apiKey, string model, string? endpoint = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Google Gemini API key is not configured.");
            return FactCheckResult.Ignore("No API key configured", DisplayName);
        }

        try
        {
            var url = $"{BaseUrl}/{model}:generateContent?key={apiKey}";
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            var body = new
            {
                system_instruction = new
                {
                    parts = new[]
                    {
                        new { text = FactCheckPrompt.SystemPrompt }
                    }
                },
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = string.Format(FactCheckPrompt.UserPrompt, message) }
                        }
                    }
                },
                generationConfig = new
                {
                    maxOutputTokens = 300,
                    temperature = 0.1
                }
            };

            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Google Gemini API returned {StatusCode}: {Response}", response.StatusCode, responseText);
                return FactCheckResult.Ignore($"API error: {response.StatusCode}", DisplayName);
            }

            var json = JsonDocument.Parse(responseText);
            var content = json.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            return FactCheckPrompt.ParseResponse(content.Trim(), DisplayName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Google Gemini API");
            return FactCheckResult.Ignore($"Error: {ex.Message}", DisplayName);
        }
    }
}
