using System.Collections.Concurrent;
using BallouBot.Core;
using BallouBot.Core.Entities;
using BallouBot.Data;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.ErrorNotify;

/// <summary>
/// Implements <see cref="IErrorNotificationService"/> to forward errors as DMs
/// to subscribed administrators. Includes rate limiting to prevent DM spam
/// during error storms.
/// </summary>
public class ErrorNotificationService : IErrorNotificationService
{
    private readonly ILogger<ErrorNotificationService> _logger;
    private readonly IServiceProvider _services;
    private DiscordSocketClient? _client;

    /// <summary>
    /// Tracks the last DM timestamp per user to enforce rate limiting.
    /// Key is userId, value is the last time a DM was sent.
    /// </summary>
    private readonly ConcurrentDictionary<ulong, DateTime> _lastDmTimestamps = new();

    /// <summary>
    /// Minimum interval between error DMs to a single user (prevents spam).
    /// </summary>
    private static readonly TimeSpan RateLimitInterval = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorNotificationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="services">The application service provider.</param>
    public ErrorNotificationService(ILogger<ErrorNotificationService> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    /// <summary>
    /// Sets the Discord client. Must be called during module initialization.
    /// </summary>
    /// <param name="client">The Discord socket client.</param>
    public void SetClient(DiscordSocketClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async Task NotifyErrorAsync(string source, string message, Exception? exception = null, ulong? guildId = null)
    {
        if (_client is null)
        {
            _logger.LogDebug("Error notification skipped — Discord client not yet initialized.");
            return;
        }

        try
        {
            // Load subscriptions from the database
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            List<ErrorNotifySubscription> subscriptions;
            if (guildId.HasValue)
            {
                subscriptions = await db.ErrorNotifySubscriptions
                    .Where(s => s.GuildId == guildId.Value)
                    .ToListAsync();
            }
            else
            {
                subscriptions = await db.ErrorNotifySubscriptions.ToListAsync();
            }

            if (subscriptions.Count == 0) return;

            // Build the error embed
            var embed = BuildErrorEmbed(source, message, exception, guildId);

            foreach (var subscription in subscriptions)
            {
                await SendErrorDmAsync(subscription.UserId, embed);
            }
        }
        catch (Exception ex)
        {
            // Never let error notification itself crash the bot
            _logger.LogWarning(ex, "Failed to send error notification DMs.");
        }
    }

    /// <summary>
    /// Sends an error DM to a single user, respecting rate limits.
    /// </summary>
    private async Task SendErrorDmAsync(ulong userId, Embed embed)
    {
        try
        {
            // Rate limit check
            if (_lastDmTimestamps.TryGetValue(userId, out var lastSent)
                && DateTime.UtcNow - lastSent < RateLimitInterval)
            {
                _logger.LogDebug("Skipping error DM to {UserId} — rate limited.", userId);
                return;
            }

            var user = _client!.GetUser(userId);
            if (user is null)
            {
                _logger.LogDebug("Could not find user {UserId} for error DM.", userId);
                return;
            }

            var dmChannel = await user.CreateDMChannelAsync();
            await dmChannel.SendMessageAsync(embed: embed);

            _lastDmTimestamps[userId] = DateTime.UtcNow;
            _logger.LogDebug("Sent error notification DM to {Username} ({UserId}).", user.Username, userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send error DM to user {UserId}.", userId);
        }
    }

    /// <summary>
    /// Builds a Discord embed for an error notification.
    /// </summary>
    internal static Embed BuildErrorEmbed(string source, string message, Exception? exception, ulong? guildId)
    {
        var embed = new EmbedBuilder()
            .WithTitle("🚨 BallouBot Error")
            .WithColor(new Color(0xE74C3C)) // Red
            .WithCurrentTimestamp();

        embed.AddField("Source", source, true);

        if (guildId.HasValue)
        {
            embed.AddField("Guild ID", guildId.Value.ToString(), true);
        }

        // Truncate message to fit Discord embed field limits (1024 chars)
        var truncatedMessage = message.Length > 1024 ? message[..1021] + "..." : message;
        embed.AddField("Message", truncatedMessage, false);

        if (exception is not null)
        {
            var exceptionText = exception.ToString();
            // Truncate exception to fit (1024 chars for embed field)
            if (exceptionText.Length > 1024)
            {
                exceptionText = exceptionText[..1021] + "...";
            }
            embed.AddField("Exception", $"```\n{exceptionText}\n```", false);
        }

        embed.WithFooter("BallouBot Error Notify • /balloubot errornotify to manage");

        return embed.Build();
    }
}
