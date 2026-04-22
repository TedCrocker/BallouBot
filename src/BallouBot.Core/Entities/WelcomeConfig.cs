namespace BallouBot.Core.Entities;

/// <summary>
/// Stores welcome message configuration for a guild.
/// Supports placeholders: {user}, {username}, {server}, {membercount}
/// </summary>
public class WelcomeConfig
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
    /// Gets or sets the channel ID where welcome messages are sent.
    /// </summary>
    public ulong ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the welcome message template.
    /// Supports placeholders: {user}, {username}, {server}, {membercount}
    /// </summary>
    public string Message { get; set; } = "Welcome to {server}, {user}! You are member #{membercount}.";

    /// <summary>
    /// Gets or sets whether welcome messages are enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use an embed instead of plain text.
    /// </summary>
    public bool UseEmbed { get; set; } = false;

    /// <summary>
    /// Gets or sets the embed color (hex without #, e.g., "5865F2") when UseEmbed is true.
    /// </summary>
    public string EmbedColor { get; set; } = "5865F2";

    /// <summary>
    /// Gets or sets the embed title when UseEmbed is true.
    /// </summary>
    public string? EmbedTitle { get; set; } = "Welcome!";

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
    /// Formats the message template with actual values.
    /// </summary>
    /// <param name="userMention">The user mention string (e.g., &lt;@123456&gt;).</param>
    /// <param name="username">The user's display name.</param>
    /// <param name="serverName">The server name.</param>
    /// <param name="memberCount">The current member count.</param>
    /// <returns>The formatted welcome message.</returns>
    public string FormatMessage(string userMention, string username, string serverName, int memberCount)
    {
        return Message
            .Replace("{user}", userMention, StringComparison.OrdinalIgnoreCase)
            .Replace("{username}", username, StringComparison.OrdinalIgnoreCase)
            .Replace("{server}", serverName, StringComparison.OrdinalIgnoreCase)
            .Replace("{membercount}", memberCount.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
