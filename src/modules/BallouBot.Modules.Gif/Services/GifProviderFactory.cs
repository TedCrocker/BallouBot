using BallouBot.Modules.Gif.Providers;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.Gif.Services;

/// <summary>
/// Factory for creating GIF provider instances based on the configured provider type.
/// </summary>
public class GifProviderFactory
{
    private readonly HttpClient _httpClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<GifProviderType, IGifProvider> _providers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="GifProviderFactory"/> class.
    /// </summary>
    public GifProviderFactory(HttpClient httpClient, ILoggerFactory loggerFactory)
    {
        _httpClient = httpClient;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Gets a GIF provider instance for the specified type.
    /// Provider instances are cached for reuse.
    /// </summary>
    /// <param name="providerType">The type of provider to retrieve.</param>
    /// <returns>The GIF provider instance.</returns>
    public IGifProvider GetProvider(GifProviderType providerType)
    {
        if (_providers.TryGetValue(providerType, out var cached))
        {
            return cached;
        }

        IGifProvider provider = providerType switch
        {
            GifProviderType.Tenor => new TenorGifProvider(_httpClient, _loggerFactory.CreateLogger<TenorGifProvider>()),
            GifProviderType.Giphy => new GiphyGifProvider(_httpClient, _loggerFactory.CreateLogger<GiphyGifProvider>()),
            GifProviderType.RedGifs => new RedGifsGifProvider(_httpClient, _loggerFactory.CreateLogger<RedGifsGifProvider>()),
            _ => throw new ArgumentOutOfRangeException(nameof(providerType), $"Unsupported GIF provider: {providerType}")
        };

        _providers[providerType] = provider;
        return provider;
    }

    /// <summary>
    /// Gets all registered provider types and their display info.
    /// </summary>
    /// <returns>A list of all supported providers with their metadata.</returns>
    public static IReadOnlyList<(GifProviderType Type, string Name, bool RequiresKey, bool IsNsfw)> GetSupportedProviders()
    {
        return
        [
            (GifProviderType.Tenor, "Tenor", true, false),
            (GifProviderType.Giphy, "Giphy", true, false),
            (GifProviderType.RedGifs, "RedGifs", false, true),
        ];
    }
}
