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

            var user = await PickRandomEligibleUserAsync(guild, config);
            if (user is null)
            {
                return "❌ No eligible users found. Make sure users are whitelisted (or switch to blacklist mode).";
            }

            var richard = await _wikipediaService.GetRandomRichardAsync();
            if (richard is null)
            {
                return "❌ Failed to fetch a Richard from Wikipedia. Try again later.";
            }

            var sent = await SendRichardDmAsync(user, richard);
            return sent
                ? $"✅ Sent a Random Richard ({richard.Name}) to {user.DisplayName}!"
                : $"⚠️ Fetched {richard.Name} but couldn't DM {user.DisplayName} (they may have DMs disabled).";
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

        var user = await PickRandomEligibleUserAsync(guild, config);
        if (user is null)
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

        var sent = await SendRichardDmAsync(user, richard);
        if (sent)
        {
            _logger.LogInformation("Sent Random Richard ({Name}) to {User} in guild {Guild}",
                richard.Name, user.DisplayName, guild.Name);
        }
        else
        {
            _logger.LogWarning("Could not DM {User} in guild {Guild} (DMs may be disabled)",
                user.DisplayName, guild.Name);
        }

        // Reset timer with new random interval
        _lastSendTimes[guildId] = DateTime.UtcNow;
        _nextIntervalMinutes[guildId] = GetRandomInterval(config);

        _logger.LogDebug("Next Random Richard for guild {GuildId} in {Minutes} minutes",
            guildId, _nextIntervalMinutes[guildId]);
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
