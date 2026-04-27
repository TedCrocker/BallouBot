using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BallouBot.Modules.Gif.Models;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.Gif.Providers;

/// <summary>
/// GIF provider implementation for the RedGifs API.
/// This provider serves NSFW content and should only be used in NSFW-marked Discord channels.
/// Uses temporary auth tokens obtained from the RedGifs API.
/// </summary>
public class RedGifsGifProvider : IGifProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RedGifsGifProvider> _logger;

    private const string TokenUrl = "https://api.redgifs.com/v2/auth/temporary";
    private const string SearchUrl = "https://api.redgifs.com/v2/gifs/search";

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    /// <inheritdoc />
    public GifProviderType ProviderType => GifProviderType.RedGifs;

    /// <inheritdoc />
    public string DisplayName => "RedGifs";

    /// <inheritdoc />
    public bool RequiresApiKey => false;

    /// <inheritdoc />
    public bool IsNsfw => true;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedGifsGifProvider"/> class.
    /// </summary>
    public RedGifsGifProvider(HttpClient httpClient, ILogger<RedGifsGifProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<GifResult>> SearchAsync(string query, int count, string? apiKey)
    {
        try
        {
            var token = await GetTemporaryTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Failed to obtain RedGifs temporary token.");
                return [];
            }

            var url = $"{SearchUrl}?search_text={Uri.EscapeDataString(query)}&count={count}&order=trending";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("RedGifs API returned {StatusCode} for query '{Query}'.", response.StatusCode, query);
                return [];
            }

            var searchResponse = await response.Content.ReadFromJsonAsync<RedGifsSearchResponse>();

            if (searchResponse?.Gifs is null)
            {
                _logger.LogWarning("RedGifs returned no results for query '{Query}'.", query);
                return [];
            }

            return searchResponse.Gifs.Select(g => new GifResult
            {
                Id = g.Id ?? string.Empty,
                Title = g.Tags is { Count: > 0 } ? string.Join(", ", g.Tags.Take(3)) : query,
                Url = g.Urls?.Hd ?? g.Urls?.Sd ?? string.Empty,
                PreviewUrl = g.Urls?.Thumbnail ?? g.Urls?.Poster ?? g.Urls?.Sd ?? string.Empty,
                Width = g.Width,
                Height = g.Height,
                ProviderName = DisplayName
            }).Where(g => !string.IsNullOrEmpty(g.Url)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search RedGifs for '{Query}'.", query);
            return [];
        }
    }

    /// <summary>
    /// Obtains a temporary authentication token from RedGifs.
    /// Caches the token until it expires.
    /// </summary>
    private async Task<string?> GetTemporaryTokenAsync()
    {
        if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry)
        {
            return _cachedToken;
        }

        try
        {
            var response = await _httpClient.GetFromJsonAsync<RedGifsTokenResponse>(TokenUrl);
            if (response?.Token is not null)
            {
                _cachedToken = response.Token;
                // RedGifs temp tokens last ~24h, but refresh conservatively at 1h
                _tokenExpiry = DateTime.UtcNow.AddHours(1);
                _logger.LogDebug("Obtained new RedGifs temporary token.");
                return _cachedToken;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain RedGifs temporary token.");
        }

        return null;
    }

    // RedGifs API response models

    internal class RedGifsTokenResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }

    internal class RedGifsSearchResponse
    {
        [JsonPropertyName("gifs")]
        public List<RedGifsGif>? Gifs { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    internal class RedGifsGif
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("urls")]
        public RedGifsUrls? Urls { get; set; }
    }

    internal class RedGifsUrls
    {
        [JsonPropertyName("hd")]
        public string? Hd { get; set; }

        [JsonPropertyName("sd")]
        public string? Sd { get; set; }

        [JsonPropertyName("poster")]
        public string? Poster { get; set; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
    }
}
