namespace BallouBot.Modules.Gif.Providers;

/// <summary>
/// Enumerates the supported GIF provider types.
/// </summary>
public enum GifProviderType
{
    /// <summary>
    /// Google Tenor GIF API.
    /// </summary>
    Tenor,

    /// <summary>
    /// Giphy GIF API.
    /// </summary>
    Giphy,

    /// <summary>
    /// RedGifs API (NSFW content — requires NSFW channel).
    /// </summary>
    RedGifs
}
