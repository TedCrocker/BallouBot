namespace BallouBot.Modules.RandomRichard.Models;

/// <summary>
/// Represents information about a person named Richard fetched from Wikipedia.
/// </summary>
public class RichardInfo
{
    /// <summary>
    /// Gets or sets the full name of the Richard.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the summary/description from Wikipedia.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL to the Wikipedia thumbnail image.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL to the full Wikipedia article.
    /// </summary>
    public string WikipediaUrl { get; set; } = string.Empty;
}
