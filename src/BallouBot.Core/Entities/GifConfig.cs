namespace BallouBot.Core.Entities;

/// <summary>
/// Stores GIF module configuration for a guild.
/// Controls which provider is used, API keys, and preview settings.
/// </summary>
public class GifConfig
{
    /// <summary>
    /// Gets or sets the primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the Discord guild (server) ID.
    /// </summary>
    public ulong GuildId { get; set; }

    /// <summary>
    /// Gets or sets whether the GIF module is enabled for this guild.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the GIF provider to use (e.g., "Tenor", "Giphy", "RedGifs").
    /// </summary>
    public string Provider { get; set; } = "Tenor";

    /// <summary>
    /// Gets or sets the API key for the configured provider.
    /// Some providers (like RedGifs) don't require an API key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets how many GIF results to show in the preview browser.
    /// </summary>
    public int PreviewCount { get; set; } = 5;

    /// <summary>
    /// Gets or sets when this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property: the parent guild settings.
    /// </summary>
    public GuildSettings? GuildSettings { get; set; }
}
