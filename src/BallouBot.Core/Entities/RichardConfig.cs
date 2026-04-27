namespace BallouBot.Core.Entities;

/// <summary>
/// Stores Random Richard configuration for a guild.
/// Controls whether the module is enabled, the send interval, and the list mode.
/// </summary>
public class RichardConfig
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
    /// Gets or sets whether the Random Richard module is enabled for this guild.
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the minimum interval in minutes between Random Richard DMs.
    /// </summary>
    public int MinIntervalMinutes { get; set; } = 480;

    /// <summary>
    /// Gets or sets the maximum interval in minutes between Random Richard DMs.
    /// </summary>
    public int MaxIntervalMinutes { get; set; } = 480;

    /// <summary>
    /// Gets or sets whether whitelist mode is active.
    /// When true, only whitelisted users receive DMs.
    /// When false, all users except blacklisted ones receive DMs.
    /// </summary>
    public bool UseWhitelistMode { get; set; } = true;

    /// <summary>
    /// Gets or sets the fallback channel ID for users who have DMs disabled.
    /// When set, the bot creates a private thread in this channel to deliver the Richard.
    /// </summary>
    public ulong? FallbackChannelId { get; set; }

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

    /// <summary>
    /// Navigation property: the user entries (whitelist/blacklist) for this guild.
    /// </summary>
    public List<RichardUserEntry> UserEntries { get; set; } = new();
}
