namespace BallouBot.Modules.Gif.Models;

/// <summary>
/// Represents a single GIF result returned from a provider search.
/// </summary>
public class GifResult
{
    /// <summary>
    /// Gets or sets the unique ID of the GIF from the provider.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title or description of the GIF.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the direct URL to the full-size GIF (used when posting).
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL to a smaller preview/thumbnail of the GIF (used in embeds).
    /// </summary>
    public string PreviewUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the width of the GIF in pixels.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the GIF in pixels.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets the source provider name (e.g., "Tenor", "Giphy").
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;
}
