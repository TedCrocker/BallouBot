namespace BallouBot.Core.Entities;

/// <summary>
/// Stores Fact Check module configuration for a guild.
/// Controls AI provider, rate limiting, and channel restrictions.
/// </summary>
public class FactCheckConfig
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
    /// Gets or sets whether the Fact Check module is enabled for this guild.
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the AI provider name (e.g., "OpenAI", "Anthropic", "AzureOpenAI").
    /// </summary>
    public string AiProvider { get; set; } = "OpenAI";

    /// <summary>
    /// Gets or sets the API key for the configured AI provider.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the AI model to use (e.g., "gpt-4o-mini", "claude-sonnet-4-20250514").
    /// </summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Gets or sets the Azure OpenAI endpoint URL (only used for AzureOpenAI provider).
    /// </summary>
    public string? AzureEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the cooldown in seconds between checks for the same user.
    /// </summary>
    public int CooldownSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the maximum number of AI checks per hour per guild.
    /// </summary>
    public int MaxChecksPerHour { get; set; } = 30;

    /// <summary>
    /// Gets or sets the minimum message length to consider for fact-checking.
    /// </summary>
    public int MinMessageLength { get; set; } = 20;

    /// <summary>
    /// Gets or sets the optional channel ID to restrict fact-checking to.
    /// When null, all channels are monitored.
    /// </summary>
    public ulong? ChannelId { get; set; }

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
    /// Navigation property: the watched users for this guild.
    /// </summary>
    public List<FactCheckUser> WatchedUsers { get; set; } = new();
}
