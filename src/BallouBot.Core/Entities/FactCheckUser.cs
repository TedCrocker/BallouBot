namespace BallouBot.Core.Entities;

/// <summary>
/// Represents a user who is being watched for fact-checking in a guild.
/// </summary>
public class FactCheckUser
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
    /// Gets or sets the Discord user ID to watch.
    /// </summary>
    public ulong UserId { get; set; }

    /// <summary>
    /// Gets or sets when this user was added to the watch list.
    /// </summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property: the parent fact check configuration.
    /// </summary>
    public FactCheckConfig? FactCheckConfig { get; set; }
}
