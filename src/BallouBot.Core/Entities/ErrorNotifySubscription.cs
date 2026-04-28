namespace BallouBot.Core.Entities;

/// <summary>
/// Represents a user subscription to receive error notification DMs from BallouBot.
/// </summary>
public class ErrorNotifySubscription
{
    /// <summary>
    /// Gets or sets the primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the Discord guild (server) ID where the subscription was created.
    /// </summary>
    public ulong GuildId { get; set; }

    /// <summary>
    /// Gets or sets the Discord user ID of the subscribed administrator.
    /// </summary>
    public ulong UserId { get; set; }

    /// <summary>
    /// Gets or sets when this subscription was created.
    /// </summary>
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
}
