using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BallouBot.Modules.Gif.Models;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.Gif.Providers;

/// <summary>
/// GIF provider implementation for the Google Tenor API (v2).
/// Requires an API key obtained from the Google Cloud Console.
/// </summary>
public class TenorGifProvider : IGifProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TenorGifProvider> _logger;

    private const string BaseUrl = "https://tenor.googleapis.com/v2/search";

    /// <inheritdoc />
    public GifProviderType ProviderType => GifProviderType.Tenor;

    /// <inheritdoc />
    public string DisplayName => "Tenor";

    /// <inheritdoc />
    public bool RequiresApiKey => true;

    /// <inheritdoc />
    public bool IsNsfw => false;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenorGifProvider"/> class.
    /// </summary>
    public TenorGifProvider(HttpClient httpClient, ILogger<TenorGifProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<GifResult>> SearchAsync(string query, int count, string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Tenor API key is not configured.");
            return [];
        }

        try
        {
            var url = $"{BaseUrl}?q={Uri.EscapeDataString(query)}&key={apiKey}&limit={count}&media_filter=gif,tinygif";
            var response = await _httpClient.GetFromJsonAsync<TenorSearchResponse>(url);

            if (response?.Results is null)
            {
                _logger.LogWarning("Tenor returned no results for query '{Query}'.", query);
                return [];
            }

            return response.Results.Select(r =>
            {
                var gif = r.MediaFormats?.GetValueOrDefault("gif");
                var tinyGif = r.MediaFormats?.GetValueOrDefault("tinygif");

                return new GifResult
                {
                    Id = r.Id ?? string.Empty,
                    Title = r.Title ?? query,
                    Url = gif?.Url ?? tinyGif?.Url ?? string.Empty,
                    PreviewUrl = tinyGif?.Url ?? gif?.Url ?? string.Empty,
                    Width = gif?.Dims?.Length >= 2 ? gif.Dims[0] : 0,
                    Height = gif?.Dims?.Length >= 2 ? gif.Dims[1] : 0,
                    ProviderName = DisplayName
                };
            }).Where(g => !string.IsNullOrEmpty(g.Url)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search Tenor for '{Query}'.", query);
            return [];
        }
    }

    // Tenor API v2 response models

    internal class TenorSearchResponse
    {
        [JsonPropertyName("results")]
        public List<TenorResult>? Results { get; set; }
    }

    internal class TenorResult
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("media_formats")]
        public Dictionary<string, TenorMediaFormat>? MediaFormats { get; set; }
    }

    internal class TenorMediaFormat
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("dims")]
        public int[]? Dims { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }
    }
}
