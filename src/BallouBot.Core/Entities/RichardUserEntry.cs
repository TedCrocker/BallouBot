namespace BallouBot.Core.Entities;

/// <summary>
/// Represents a whitelist or blacklist entry for the Random Richard module.
/// </summary>
public class RichardUserEntry
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
    /// Gets or sets the Discord user ID.
    /// </summary>
    public ulong UserId { get; set; }

    /// <summary>
    /// Gets or sets the list type (Whitelist or Blacklist).
    /// </summary>
    public RichardListType ListType { get; set; }

    /// <summary>
    /// Gets or sets when this entry was added.
    /// </summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property: the parent Richard config.
    /// </summary>
    public RichardConfig? RichardConfig { get; set; }
}
