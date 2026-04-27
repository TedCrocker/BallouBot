using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BallouBot.Modules.Gif.Models;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.Gif.Providers;

/// <summary>
/// GIF provider implementation for the Giphy API.
/// Requires an API key obtained from the Giphy developer portal.
/// </summary>
public class GiphyGifProvider : IGifProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GiphyGifProvider> _logger;

    private const string BaseUrl = "https://api.giphy.com/v1/gifs/search";

    /// <inheritdoc />
    public GifProviderType ProviderType => GifProviderType.Giphy;

    /// <inheritdoc />
    public string DisplayName => "Giphy";

    /// <inheritdoc />
    public bool RequiresApiKey => true;

    /// <inheritdoc />
    public bool IsNsfw => false;

    /// <summary>
    /// Initializes a new instance of the <see cref="GiphyGifProvider"/> class.
    /// </summary>
    public GiphyGifProvider(HttpClient httpClient, ILogger<GiphyGifProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<GifResult>> SearchAsync(string query, int count, string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Giphy API key is not configured.");
            return [];
        }

        try
        {
            var url = $"{BaseUrl}?q={Uri.EscapeDataString(query)}&api_key={apiKey}&limit={count}&rating=g";
            var response = await _httpClient.GetFromJsonAsync<GiphySearchResponse>(url);

            if (response?.Data is null)
            {
                _logger.LogWarning("Giphy returned no results for query '{Query}'.", query);
                return [];
            }

            return response.Data.Select(d => new GifResult
            {
                Id = d.Id ?? string.Empty,
                Title = d.Title ?? query,
                Url = d.Images?.Original?.Url ?? d.Images?.FixedHeight?.Url ?? string.Empty,
                PreviewUrl = d.Images?.FixedHeightSmall?.Url ?? d.Images?.FixedHeight?.Url ?? string.Empty,
                Width = int.TryParse(d.Images?.Original?.Width, out var w) ? w : 0,
                Height = int.TryParse(d.Images?.Original?.Height, out var h) ? h : 0,
                ProviderName = DisplayName
            }).Where(g => !string.IsNullOrEmpty(g.Url)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search Giphy for '{Query}'.", query);
            return [];
        }
    }

    // Giphy API response models

    internal class GiphySearchResponse
    {
        [JsonPropertyName("data")]
        public List<GiphyGif>? Data { get; set; }
    }

    internal class GiphyGif
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("images")]
        public GiphyImages? Images { get; set; }
    }

    internal class GiphyImages
    {
        [JsonPropertyName("original")]
        public GiphyImageVariant? Original { get; set; }

        [JsonPropertyName("fixed_height")]
        public GiphyImageVariant? FixedHeight { get; set; }

        [JsonPropertyName("fixed_height_small")]
        public GiphyImageVariant? FixedHeightSmall { get; set; }
    }

    internal class GiphyImageVariant
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("width")]
        public string? Width { get; set; }

        [JsonPropertyName("height")]
        public string? Height { get; set; }
    }
}
