using BallouBot.Core;
using BallouBot.Core.Entities;
using BallouBot.Data;
using BallouBot.Modules.RandomRichard.Models;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.RandomRichard.Services;

/// <summary>
/// Background service that periodically sends Random Richard DMs to eligible users.
/// Runs a loop for each guild with the module enabled, waiting a random interval
/// between sends.
/// </summary>
public class RichardTimerService
{
    private readonly IModuleContext _context;
    private readonly WikipediaService _wikipediaService;
    private readonly ILogger<RichardTimerService> _logger;
    private readonly Random _random = new();
    private CancellationTokenSource? _cts;
    private Task? _timerTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="RichardTimerService"/> class.
    /// </summary>
    /// <param name="context">The module context.</param>
    /// <param name="wikipediaService">The Wikipedia service for fetching Richards.</param>
    public RichardTimerService(IModuleContext context, WikipediaService wikipediaService)
    {
        _context = context;
        _wikipediaService = wikipediaService;
        _logger = context.GetLogger<RichardTimerService>();
    }

    /// <summary>
    /// Starts the background timer loop.
    /// </summary>
    public void Start()
    {
        _cts = new CancellationTokenSource();
        _timerTask = RunTimerLoopAsync(_cts.Token);
        _logger.LogInformation("Random Richard timer service started.");
    }

    /// <summary>
    /// Stops the background timer loop.
    /// </summary>
    public async Task StopAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            if (_timerTask is not null)
            {
                try
                {
                    await _timerTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected on shutdown
                }
            }
            _cts.Dispose();
            _cts = null;
        }
        _logger.LogInformation("Random Richard timer service stopped.");
    }

    /// <summary>
    /// Forces an immediate send of a Random Richard to a random eligible user in the specified guild.
    /// If a user has DMs disabled, skips them and tries another eligible user.
    /// </summary>
    /// <param name="guildId">The guild ID to send in.</param>
    /// <returns>A message describing the result.</returns>
    public async Task<string> ForceSendAsync(ulong guildId)
    {
        try
        {
            using var scope = _context.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            var config = await db.RichardConfigs
                .Include(c => c.UserEntries)
                .FirstOrDefaultAsync(c => c.GuildId == guildId);

            if (config is null)
            {
                return "❌ Random Richard is not configured for this server.";
            }

            var guild = _context.Client.GetGuild(guildId);
            if (guild is null)
            {
                return "❌ Could not find the server.";
            }

            var eligibleUsers = await GetShuffledEligibleUsersAsync(guild, config);
            if (eligibleUsers.Count == 0)
            {
                return "❌ No eligible users found. Make sure users are whitelisted (or switch to blacklist mode).";
            }

            var richard = await _wikipediaService.GetRandomRichardAsync();
            if (richard is null)
            {
                return "❌ Failed to fetch a Richard from Wikipedia. Try again later.";
            }

            var (sent, recipient, usedFallback) = await TrySendToEligibleUsersAsync(eligibleUsers, richard, config);
            if (sent && usedFallback)
                return $"✅ Sent a Random Richard ({richard.Name}) to {recipient!.DisplayName} via private thread (DMs blocked).";
            if (sent)
                return $"✅ Sent a Random Richard ({richard.Name}) to {recipient!.DisplayName}!";
            return $"⚠️ Fetched {richard.Name} but couldn't reach any eligible user (all {eligibleUsers.Count} user(s) may have DMs disabled and no fallback channel is set).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ForceSendAsync for guild {GuildId}", guildId);
            return "❌ An error occurred while sending.";
        }
    }

    private async Task RunTimerLoopAsync(CancellationToken cancellationToken)
    {
        // Wait a bit for the bot to fully connect
        await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAllGuildsAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Random Richard timer loop.");
            }

            // Check every 5 minutes whether any guild needs a send
            await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
        }
    }

    // Track when each guild was last sent to
    private readonly Dictionary<ulong, DateTime> _lastSendTimes = new();
    private readonly Dictionary<ulong, int> _nextIntervalMinutes = new();

    private async Task ProcessAllGuildsAsync(CancellationToken cancellationToken)
    {
        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

        var enabledConfigs = await db.RichardConfigs
            .Include(c => c.UserEntries)
            .Where(c => c.IsEnabled)
            .ToListAsync(cancellationToken);

        foreach (var config in enabledConfigs)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                await ProcessGuildAsync(config, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing guild {GuildId} for Random Richard", config.GuildId);
            }
        }
    }

    private async Task ProcessGuildAsync(RichardConfig config, CancellationToken cancellationToken)
    {
        var guildId = config.GuildId;

        // Initialize tracking for this guild if needed
        if (!_lastSendTimes.ContainsKey(guildId))
        {
            _lastSendTimes[guildId] = DateTime.UtcNow;
            _nextIntervalMinutes[guildId] = GetRandomInterval(config);
            _logger.LogDebug("Initialized timer for guild {GuildId}: next send in {Minutes} minutes",
                guildId, _nextIntervalMinutes[guildId]);
            return; // Don't send immediately on first startup
        }

        var minutesSinceLastSend = (DateTime.UtcNow - _lastSendTimes[guildId]).TotalMinutes;
        var targetInterval = _nextIntervalMinutes[guildId];

        if (minutesSinceLastSend < targetInterval)
        {
            return; // Not time yet
        }

        _logger.LogInformation("Time to send a Random Richard to guild {GuildId}!", guildId);

        var guild = _context.Client.GetGuild(guildId);
        if (guild is null)
        {
            _logger.LogWarning("Guild {GuildId} not found, skipping.", guildId);
            return;
        }

        var eligibleUsers = await GetShuffledEligibleUsersAsync(guild, config);
        if (eligibleUsers.Count == 0)
        {
            _logger.LogWarning("No eligible users in guild {GuildId}, skipping.", guildId);
            _lastSendTimes[guildId] = DateTime.UtcNow;
            _nextIntervalMinutes[guildId] = GetRandomInterval(config);
            return;
        }

        var richard = await _wikipediaService.GetRandomRichardAsync();
        if (richard is null)
        {
            _logger.LogWarning("Failed to fetch Richard for guild {GuildId}, will retry next cycle.", guildId);
            return;
        }

        var (sent, recipient, usedFallback) = await TrySendToEligibleUsersAsync(eligibleUsers, richard, config);
        if (sent && usedFallback)
        {
            _logger.LogInformation("Sent Random Richard ({Name}) to {User} via private thread in guild {Guild}",
                richard.Name, recipient!.DisplayName, guild.Name);
        }
        else if (sent)
        {
            _logger.LogInformation("Sent Random Richard ({Name}) to {User} in guild {Guild}",
                richard.Name, recipient!.DisplayName, guild.Name);
        }
        else
        {
            _logger.LogWarning("Could not reach any of {Count} eligible user(s) in guild {Guild}",
                eligibleUsers.Count, guild.Name);
        }

        // Reset timer with new random interval
        _lastSendTimes[guildId] = DateTime.UtcNow;
        _nextIntervalMinutes[guildId] = GetRandomInterval(config);

        _logger.LogDebug("Next Random Richard for guild {GuildId} in {Minutes} minutes",
            guildId, _nextIntervalMinutes[guildId]);
    }

    /// <summary>
    /// Gets all eligible users for the guild in a randomized order.
    /// Used to iterate through users when a DM fails (e.g., DMs disabled) so the next user can be tried.
    /// </summary>
    /// <param name="guild">The guild to get users from.</param>
    /// <param name="config">The Richard configuration with whitelist/blacklist settings.</param>
    /// <returns>A shuffled list of eligible users.</returns>
    internal async Task<List<SocketGuildUser>> GetShuffledEligibleUsersAsync(SocketGuild guild, RichardConfig config)
    {
        // Ensure members are downloaded
        await guild.DownloadUsersAsync();

        var allUsers = guild.Users
            .Where(u => !u.IsBot)
            .ToList();

        if (allUsers.Count == 0) return [];

        List<SocketGuildUser> eligibleUsers;

        if (config.UseWhitelistMode)
        {
            var whitelistedIds = config.UserEntries
                .Where(e => e.ListType == RichardListType.Whitelist)
                .Select(e => e.UserId)
                .ToHashSet();

            eligibleUsers = allUsers.Where(u => whitelistedIds.Contains(u.Id)).ToList();
        }
        else
        {
            var blacklistedIds = config.UserEntries
                .Where(e => e.ListType == RichardListType.Blacklist)
                .Select(e => e.UserId)
                .ToHashSet();

            eligibleUsers = allUsers.Where(u => !blacklistedIds.Contains(u.Id)).ToList();
        }

        // Shuffle using Fisher-Yates
        for (var i = eligibleUsers.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (eligibleUsers[i], eligibleUsers[j]) = (eligibleUsers[j], eligibleUsers[i]);
        }

        return eligibleUsers;
    }

    /// <summary>
    /// Attempts to send a Richard DM to each user in the list, in order, until one succeeds.
    /// If all DMs fail and a fallback channel is configured, creates a private thread for the first user.
    /// </summary>
    /// <param name="users">The shuffled list of eligible users to try.</param>
    /// <param name="richard">The Richard info to send.</param>
    /// <param name="config">The Richard config with fallback channel settings.</param>
    /// <returns>A tuple indicating success, the recipient, and whether the fallback was used.</returns>
    internal async Task<(bool Sent, SocketGuildUser? Recipient, bool UsedFallback)> TrySendToEligibleUsersAsync(
        List<SocketGuildUser> users, RichardInfo richard, RichardConfig config)
    {
        var failedUsers = new List<SocketGuildUser>();

        foreach (var user in users)
        {
            var sent = await SendRichardDmAsync(user, richard);
            if (sent)
            {
                return (true, user, false);
            }

            failedUsers.Add(user);
            _logger.LogInformation("User {Username} ({UserId}) — DM failed, trying next eligible user.",
                user.DisplayName, user.Id);
        }

        // All DMs failed — try fallback channel with private thread
        if (config.FallbackChannelId.HasValue && failedUsers.Count > 0)
        {
            var targetUser = failedUsers[0]; // Send to the first user we tried
            var sent = await SendRichardViaPrivateThreadAsync(targetUser, richard, config.FallbackChannelId.Value);
            if (sent)
            {
                return (true, targetUser, true);
            }
        }

        _logger.LogWarning("Exhausted all {Count} eligible user(s) — none could be reached.", users.Count);
        return (false, null, false);
    }

    /// <summary>
    /// Attempts to send a Richard DM to each user in the list, in order, until one succeeds.
    /// Users who have DMs disabled are logged and skipped. (Legacy overload without fallback.)
    /// </summary>
    internal async Task<(bool Sent, SocketGuildUser? Recipient)> TrySendToEligibleUsersAsync(
        List<SocketGuildUser> users, RichardInfo richard)
    {
        foreach (var user in users)
        {
            var sent = await SendRichardDmAsync(user, richard);
            if (sent)
            {
                return (true, user);
            }

            _logger.LogInformation("Skipping user {Username} ({UserId}) — DM failed, trying next eligible user.",
                user.DisplayName, user.Id);
        }

        _logger.LogWarning("Exhausted all {Count} eligible user(s) — none could be DM'd.", users.Count);
        return (false, null);
    }

    /// <summary>
    /// Creates a private thread in the fallback channel and sends the Richard embed to it,
    /// adding only the target user. This is used when the user has DMs disabled.
    /// </summary>
    internal async Task<bool> SendRichardViaPrivateThreadAsync(SocketGuildUser user, RichardInfo richard, ulong fallbackChannelId)
    {
        try
        {
            var guild = user.Guild;
            var channel = guild.GetTextChannel(fallbackChannelId);
            if (channel is null)
            {
                _logger.LogWarning("Fallback channel {ChannelId} not found in guild {GuildId}.", fallbackChannelId, guild.Id);
                return false;
            }

            // Create a private thread
            var threadName = $"🎩 Richard for {user.DisplayName}";
            // Truncate to Discord's 100-char thread name limit
            if (threadName.Length > 100)
                threadName = threadName[..100];

            var thread = await channel.CreateThreadAsync(
                name: threadName,
                type: ThreadType.PrivateThread,
                autoArchiveDuration: ThreadArchiveDuration.OneHour);

            // Add the target user to the thread
            await thread.AddUserAsync(user);

            // Build and send the embed
            var embed = new EmbedBuilder()
                .WithTitle($"🎩 Random Richard: {richard.Name}")
                .WithDescription(richard.Summary)
                .WithColor(new Color(0x9B59B6))
                .WithUrl(richard.WikipediaUrl)
                .WithFooter("Brought to you by Random Richard™ | Powered by Wikipedia\n💡 This was sent here because your DMs are disabled.")
                .WithCurrentTimestamp();

            if (!string.IsNullOrEmpty(richard.ImageUrl))
            {
                embed.WithImageUrl(richard.ImageUrl);
            }

            await thread.SendMessageAsync(
                $"Hey {user.Mention}! You've been chosen for a Random Richard, but your DMs are closed so here it is:",
                embed: embed.Build());

            _logger.LogInformation("Sent Random Richard to {User} via private thread in fallback channel {Channel}.",
                user.DisplayName, channel.Name);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Richard via private thread to user {UserId}", user.Id);
            return false;
        }
    }

    /// <summary>
    /// Picks a random eligible user from the guild based on the whitelist/blacklist configuration.
    /// </summary>
    internal async Task<SocketGuildUser?> PickRandomEligibleUserAsync(SocketGuild guild, RichardConfig config)
    {
        // Ensure members are downloaded
        await guild.DownloadUsersAsync();

        var allUsers = guild.Users
            .Where(u => !u.IsBot)
            .ToList();

        if (allUsers.Count == 0) return null;

        List<SocketGuildUser> eligibleUsers;

        if (config.UseWhitelistMode)
        {
            // Only whitelisted users
            var whitelistedIds = config.UserEntries
                .Where(e => e.ListType == RichardListType.Whitelist)
                .Select(e => e.UserId)
                .ToHashSet();

            eligibleUsers = allUsers.Where(u => whitelistedIds.Contains(u.Id)).ToList();
        }
        else
        {
            // Everyone except blacklisted users
            var blacklistedIds = config.UserEntries
                .Where(e => e.ListType == RichardListType.Blacklist)
                .Select(e => e.UserId)
                .ToHashSet();

            eligibleUsers = allUsers.Where(u => !blacklistedIds.Contains(u.Id)).ToList();
        }

        if (eligibleUsers.Count == 0) return null;

        return eligibleUsers[_random.Next(eligibleUsers.Count)];
    }

    /// <summary>
    /// Sends a Richard DM to a user with a Discord embed.
    /// </summary>
    internal async Task<bool> SendRichardDmAsync(SocketGuildUser user, RichardInfo richard)
    {
        try
        {
            var dmChannel = await user.CreateDMChannelAsync();

            var embed = new EmbedBuilder()
                .WithTitle($"🎩 Random Richard: {richard.Name}")
                .WithDescription(richard.Summary)
                .WithColor(new Color(0x9B59B6)) // Purple
                .WithUrl(richard.WikipediaUrl)
                .WithFooter("Brought to you by Random Richard™ | Powered by Wikipedia")
                .WithCurrentTimestamp();

            if (!string.IsNullOrEmpty(richard.ImageUrl))
            {
                embed.WithImageUrl(richard.ImageUrl);
            }

            await dmChannel.SendMessageAsync(embed: embed.Build());
            return true;
        }
        catch (Discord.Net.HttpException ex) when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
        {
            _logger.LogDebug("User {UserId} has DMs disabled.", user.Id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to DM user {UserId}", user.Id);
            return false;
        }
    }

    private int GetRandomInterval(RichardConfig config)
    {
        if (config.MinIntervalMinutes >= config.MaxIntervalMinutes)
            return config.MinIntervalMinutes;

        return _random.Next(config.MinIntervalMinutes, config.MaxIntervalMinutes + 1);
    }
}
