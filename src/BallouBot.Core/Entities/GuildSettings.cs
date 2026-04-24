namespace BallouBot.Core.Entities;

/// <summary>
/// Stores per-guild settings and configuration.
/// </summary>
public class GuildSettings
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
    /// Gets or sets the display name of the guild (cached for convenience).
    /// </summary>
    public string GuildName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command prefix for text commands (if used).
    /// </summary>
    public string Prefix { get; set; } = "!";

    /// <summary>
    /// Gets or sets when this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property: welcome configuration for this guild.
    /// </summary>
    public WelcomeConfig? WelcomeConfig { get; set; }

    /// <summary>
    /// Navigation property: Random Richard configuration for this guild.
    /// </summary>
    public RichardConfig? RichardConfig { get; set; }
}
