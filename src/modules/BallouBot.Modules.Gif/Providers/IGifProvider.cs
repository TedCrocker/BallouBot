using BallouBot.Modules.Gif.Models;

namespace BallouBot.Modules.Gif.Providers;

/// <summary>
/// Defines the contract for a GIF search provider.
/// Implementations wrap external GIF APIs (Tenor, Giphy, RedGifs, etc.).
/// </summary>
public interface IGifProvider
{
    /// <summary>
    /// Gets the provider type identifier.
    /// </summary>
    GifProviderType ProviderType { get; }

    /// <summary>
    /// Gets the human-readable display name for this provider.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets whether this provider requires an API key to function.
    /// </summary>
    bool RequiresApiKey { get; }

    /// <summary>
    /// Gets whether this provider serves NSFW content and should only be used in NSFW channels.
    /// </summary>
    bool IsNsfw { get; }

    /// <summary>
    /// Searches for GIFs matching the given query.
    /// </summary>
    /// <param name="query">The search term.</param>
    /// <param name="count">The maximum number of results to return.</param>
    /// <param name="apiKey">The API key for authentication (may be null if not required).</param>
    /// <returns>A list of GIF results.</returns>
    Task<List<GifResult>> SearchAsync(string query, int count, string? apiKey);
}
