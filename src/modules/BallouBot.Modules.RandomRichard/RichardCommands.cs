using BallouBot.Core;
using BallouBot.Core.Entities;
using BallouBot.Data;
using BallouBot.Modules.RandomRichard.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.RandomRichard;

/// <summary>
/// Handles slash commands for configuring the Random Richard module.
/// Commands: /richard toggle, frequency, whitelist, blacklist, remove, list, mode, preview, send
/// </summary>
public class RichardCommands
{
    private readonly IModuleContext _context;
    private readonly ILogger<RichardCommands> _logger;
    private readonly WikipediaService _wikipediaService;
    private readonly RichardTimerService _timerService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RichardCommands"/> class.
    /// </summary>
    public RichardCommands(IModuleContext context, WikipediaService wikipediaService, RichardTimerService timerService)
    {
        _context = context;
        _logger = context.GetLogger<RichardCommands>();
        _wikipediaService = wikipediaService;
        _timerService = timerService;
    }

    /// <summary>
    /// Registers the /richard slash command with Discord.
    /// </summary>
    public async Task RegisterCommandsAsync()
    {
        try
        {
            var command = new SlashCommandBuilder()
                .WithName("richard")
                .WithDescription("Configure Random Richard — get DMs about famous Richards!")
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("toggle")
                    .WithDescription("Enable or disable Random Richard for this server.")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("frequency")
                    .WithDescription("Set how often Random Richard sends DMs (in minutes).")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("min", ApplicationCommandOptionType.Integer, "Minimum interval in minutes.", isRequired: true)
                    .AddOption("max", ApplicationCommandOptionType.Integer, "Maximum interval in minutes.", isRequired: true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("whitelist")
                    .WithDescription("Add a user to the Random Richard whitelist.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("user", ApplicationCommandOptionType.User, "The user to whitelist.", isRequired: true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("blacklist")
                    .WithDescription("Add a user to the Random Richard blacklist.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("user", ApplicationCommandOptionType.User, "The user to blacklist.", isRequired: true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("remove")
                    .WithDescription("Remove a user from the whitelist/blacklist.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("user", ApplicationCommandOptionType.User, "The user to remove.", isRequired: true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("list")
                    .WithDescription("Show the current whitelist and blacklist.")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("mode")
                    .WithDescription("Switch between whitelist and blacklist mode.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("type")
                        .WithDescription("The mode to use.")
                        .WithType(ApplicationCommandOptionType.String)
                        .WithRequired(true)
                        .AddChoice("whitelist", "whitelist")
                        .AddChoice("blacklist", "blacklist")))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("preview")
                    .WithDescription("Preview a Random Richard — sends one to you as a DM.")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("send")
                    .WithDescription("Force-send a Random Richard to a random eligible user now.")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("status")
                    .WithDescription("Show current Random Richard configuration.")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("fallback-channel")
                    .WithDescription("Set a fallback channel for users with DMs disabled (uses private threads).")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("channel", ApplicationCommandOptionType.Channel, "The text channel (or leave empty to clear).", isRequired: false));

            var builtCommand = command.Build();
            foreach (var guild in _context.Client.Guilds)
            {
                await guild.CreateApplicationCommandAsync(builtCommand);
                _logger.LogDebug("Registered /richard command on guild {GuildName} ({GuildId})", guild.Name, guild.Id);
            }
            _logger.LogInformation("Registered /richard slash command on {Count} guild(s).", _context.Client.Guilds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register /richard slash command.");
        }
    }

    /// <summary>
    /// Routes incoming /richard slash commands to the appropriate handler.
    /// </summary>
    public async Task HandleSlashCommandAsync(SocketSlashCommand command)
    {
        if (command.CommandName != "richard") return;

        var subCommand = command.Data.Options.First();

        try
        {
            switch (subCommand.Name)
            {
                case "toggle":
                    await HandleToggleAsync(command);
                    break;
                case "frequency":
                    await HandleFrequencyAsync(command, subCommand);
                    break;
                case "whitelist":
                    await HandleWhitelistAsync(command, subCommand);
                    break;
                case "blacklist":
                    await HandleBlacklistAsync(command, subCommand);
                    break;
                case "remove":
                    await HandleRemoveAsync(command, subCommand);
                    break;
                case "list":
                    await HandleListAsync(command);
                    break;
                case "mode":
                    await HandleModeAsync(command, subCommand);
                    break;
                case "preview":
                    await HandlePreviewAsync(command);
                    break;
                case "send":
                    await HandleSendAsync(command);
                    break;
                case "status":
                    await HandleStatusAsync(command);
                    break;
                case "fallback-channel":
                    await HandleFallbackChannelAsync(command, subCommand);
                    break;
                default:
                    await command.RespondAsync("Unknown subcommand.", ephemeral: true);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling /richard {SubCommand}", subCommand.Name);
            if (!command.HasResponded)
            {
                await command.RespondAsync("An error occurred processing your command.", ephemeral: true);
            }
        }
    }

    private async Task HandleToggleAsync(SocketSlashCommand command)
    {
        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var config = await GetOrCreateConfigAsync(db, guildId);
        config.IsEnabled = !config.IsEnabled;
        config.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var status = config.IsEnabled ? "✅ enabled" : "❌ disabled";
        await command.RespondAsync($"Random Richard is now {status}.", ephemeral: true);
    }

    private async Task HandleFrequencyAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var min = (long)subCommand.Options.First(o => o.Name == "min").Value;
        var max = (long)subCommand.Options.First(o => o.Name == "max").Value;

        if (min < 1 || max < 1)
        {
            await command.RespondAsync("Intervals must be at least 1 minute.", ephemeral: true);
            return;
        }

        if (min > max)
        {
            await command.RespondAsync("Minimum interval cannot be greater than maximum.", ephemeral: true);
            return;
        }

        if (max > 10080) // 7 days
        {
            await command.RespondAsync("Maximum interval cannot exceed 10080 minutes (7 days).", ephemeral: true);
            return;
        }

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var config = await GetOrCreateConfigAsync(db, guildId);
        config.MinIntervalMinutes = (int)min;
        config.MaxIntervalMinutes = (int)max;
        config.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var intervalDesc = min == max
            ? $"every {min} minutes"
            : $"every {min}–{max} minutes (random)";

        await command.RespondAsync($"✅ Random Richard will send {intervalDesc}.", ephemeral: true);
    }

    private async Task HandleWhitelistAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var targetUser = (IUser)subCommand.Options.First(o => o.Name == "user").Value;

        if (targetUser.IsBot)
        {
            await command.RespondAsync("Cannot whitelist bots.", ephemeral: true);
            return;
        }

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        await GetOrCreateConfigAsync(db, guildId);

        // Check if already on any list
        var existing = await db.RichardUserEntries
            .FirstOrDefaultAsync(e => e.GuildId == guildId && e.UserId == targetUser.Id);

        if (existing is not null)
        {
            if (existing.ListType == RichardListType.Whitelist)
            {
                await command.RespondAsync($"{targetUser.Mention} is already whitelisted.", ephemeral: true);
                return;
            }
            // Move from blacklist to whitelist
            existing.ListType = RichardListType.Whitelist;
            existing.AddedAt = DateTime.UtcNow;
        }
        else
        {
            db.RichardUserEntries.Add(new RichardUserEntry
            {
                GuildId = guildId,
                UserId = targetUser.Id,
                ListType = RichardListType.Whitelist
            });
        }

        await db.SaveChangesAsync();
        await command.RespondAsync($"✅ {targetUser.Mention} has been added to the Random Richard whitelist.", ephemeral: true);
    }

    private async Task HandleBlacklistAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var targetUser = (IUser)subCommand.Options.First(o => o.Name == "user").Value;

        if (targetUser.IsBot)
        {
            await command.RespondAsync("Cannot blacklist bots.", ephemeral: true);
            return;
        }

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        await GetOrCreateConfigAsync(db, guildId);

        var existing = await db.RichardUserEntries
            .FirstOrDefaultAsync(e => e.GuildId == guildId && e.UserId == targetUser.Id);

        if (existing is not null)
        {
            if (existing.ListType == RichardListType.Blacklist)
            {
                await command.RespondAsync($"{targetUser.Mention} is already blacklisted.", ephemeral: true);
                return;
            }
            // Move from whitelist to blacklist
            existing.ListType = RichardListType.Blacklist;
            existing.AddedAt = DateTime.UtcNow;
        }
        else
        {
            db.RichardUserEntries.Add(new RichardUserEntry
            {
                GuildId = guildId,
                UserId = targetUser.Id,
                ListType = RichardListType.Blacklist
            });
        }

        await db.SaveChangesAsync();
        await command.RespondAsync($"✅ {targetUser.Mention} has been added to the Random Richard blacklist.", ephemeral: true);
    }

    private async Task HandleRemoveAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var targetUser = (IUser)subCommand.Options.First(o => o.Name == "user").Value;

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var existing = await db.RichardUserEntries
            .FirstOrDefaultAsync(e => e.GuildId == guildId && e.UserId == targetUser.Id);

        if (existing is null)
        {
            await command.RespondAsync($"{targetUser.Mention} is not on any list.", ephemeral: true);
            return;
        }

        var listName = existing.ListType.ToString().ToLower();
        db.RichardUserEntries.Remove(existing);
        await db.SaveChangesAsync();

        await command.RespondAsync($"✅ {targetUser.Mention} has been removed from the {listName}.", ephemeral: true);
    }

    private async Task HandleListAsync(SocketSlashCommand command)
    {
        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var entries = await db.RichardUserEntries
            .Where(e => e.GuildId == guildId)
            .OrderBy(e => e.ListType)
            .ThenBy(e => e.AddedAt)
            .ToListAsync();

        if (entries.Count == 0)
        {
            await command.RespondAsync("No users on any list yet. Use `/richard whitelist` or `/richard blacklist` to add users.", ephemeral: true);
            return;
        }

        var whitelisted = entries.Where(e => e.ListType == RichardListType.Whitelist).ToList();
        var blacklisted = entries.Where(e => e.ListType == RichardListType.Blacklist).ToList();

        var embed = new EmbedBuilder()
            .WithTitle("🎩 Random Richard — User Lists")
            .WithColor(new Color(0x9B59B6));

        if (whitelisted.Count > 0)
        {
            var whitelistText = string.Join("\n", whitelisted.Select(e => $"<@{e.UserId}>"));
            embed.AddField("✅ Whitelist", whitelistText, true);
        }
        else
        {
            embed.AddField("✅ Whitelist", "*Empty*", true);
        }

        if (blacklisted.Count > 0)
        {
            var blacklistText = string.Join("\n", blacklisted.Select(e => $"<@{e.UserId}>"));
            embed.AddField("❌ Blacklist", blacklistText, true);
        }
        else
        {
            embed.AddField("❌ Blacklist", "*Empty*", true);
        }

        await command.RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    private async Task HandleModeAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var modeValue = (string)subCommand.Options.First(o => o.Name == "type").Value;
        var useWhitelist = modeValue == "whitelist";

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var config = await GetOrCreateConfigAsync(db, guildId);
        config.UseWhitelistMode = useWhitelist;
        config.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var modeDesc = useWhitelist
            ? "**whitelist** mode — only whitelisted users will receive DMs"
            : "**blacklist** mode — all users except blacklisted ones will receive DMs";

        await command.RespondAsync($"✅ Switched to {modeDesc}.", ephemeral: true);
    }

    private async Task HandlePreviewAsync(SocketSlashCommand command)
    {
        await command.DeferAsync(ephemeral: true);

        var richard = await _wikipediaService.GetRandomRichardAsync();
        if (richard is null)
        {
            await command.FollowupAsync("❌ Failed to fetch a Richard from Wikipedia. Try again later.", ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"🎩 Random Richard: {richard.Name}")
            .WithDescription(richard.Summary)
            .WithColor(new Color(0x9B59B6))
            .WithUrl(richard.WikipediaUrl)
            .WithFooter("Brought to you by Random Richard™ | Powered by Wikipedia")
            .WithCurrentTimestamp();

        if (!string.IsNullOrEmpty(richard.ImageUrl))
        {
            embed.WithImageUrl(richard.ImageUrl);
        }

        await command.FollowupAsync("Here's a preview of what a Random Richard DM looks like:", embed: embed.Build(), ephemeral: true);
    }

    private async Task HandleSendAsync(SocketSlashCommand command)
    {
        await command.DeferAsync(ephemeral: true);

        var guildId = command.GuildId!.Value;
        var result = await _timerService.ForceSendAsync(guildId);

        await command.FollowupAsync(result, ephemeral: true);
    }

    private async Task HandleStatusAsync(SocketSlashCommand command)
    {
        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var config = await db.RichardConfigs
            .AsNoTracking()
            .Include(c => c.UserEntries)
            .FirstOrDefaultAsync(c => c.GuildId == guildId);

        if (config is null)
        {
            await command.RespondAsync("Random Richard hasn't been configured yet. Use `/richard toggle` to get started.", ephemeral: true);
            return;
        }

        var statusEmoji = config.IsEnabled ? "✅" : "❌";
        var mode = config.UseWhitelistMode ? "Whitelist" : "Blacklist";
        var whitelistCount = config.UserEntries.Count(e => e.ListType == RichardListType.Whitelist);
        var blacklistCount = config.UserEntries.Count(e => e.ListType == RichardListType.Blacklist);

        var intervalDesc = config.MinIntervalMinutes == config.MaxIntervalMinutes
            ? $"{config.MinIntervalMinutes} minutes"
            : $"{config.MinIntervalMinutes}–{config.MaxIntervalMinutes} minutes";

        var fallbackDesc = config.FallbackChannelId.HasValue
            ? $"<#{config.FallbackChannelId.Value}>"
            : "*Not set*";

        var embed = new EmbedBuilder()
            .WithTitle("🎩 Random Richard — Status")
            .WithColor(new Color(0x9B59B6))
            .AddField("Status", $"{statusEmoji} {(config.IsEnabled ? "Enabled" : "Disabled")}", true)
            .AddField("Mode", mode, true)
            .AddField("Interval", intervalDesc, true)
            .AddField("Whitelisted", whitelistCount.ToString(), true)
            .AddField("Blacklisted", blacklistCount.ToString(), true)
            .AddField("Fallback Channel", fallbackDesc, true)
            .WithCurrentTimestamp();

        await command.RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    private async Task HandleFallbackChannelAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var config = await GetOrCreateConfigAsync(db, guildId);

        var channelOption = subCommand.Options.FirstOrDefault(o => o.Name == "channel");

        if (channelOption is null)
        {
            // Clear the fallback channel
            config.FallbackChannelId = null;
            config.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await command.RespondAsync("✅ Fallback channel cleared. Users with DMs disabled will be skipped.", ephemeral: true);
            return;
        }

        var channel = channelOption.Value as IChannel;
        if (channel is not ITextChannel textChannel)
        {
            await command.RespondAsync("❌ Please select a text channel.", ephemeral: true);
            return;
        }

        config.FallbackChannelId = textChannel.Id;
        config.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await command.RespondAsync(
            $"✅ Fallback channel set to {textChannel.Mention}.\n" +
            "When a user has DMs disabled, a **private thread** will be created in that channel to deliver the Richard.",
            ephemeral: true);
    }

    /// <summary>
    /// Gets an existing RichardConfig for the guild or creates a new one.
    /// Also ensures the parent GuildSettings record exists.
    /// </summary>
    public static async Task<RichardConfig> GetOrCreateConfigAsync(BotDbContext db, ulong guildId)
    {
        var config = await db.RichardConfigs.FirstOrDefaultAsync(c => c.GuildId == guildId);

        if (config is not null) return config;

        // Ensure GuildSettings exists
        var guildSettings = await db.GuildSettings.FirstOrDefaultAsync(g => g.GuildId == guildId);
        if (guildSettings is null)
        {
            guildSettings = new GuildSettings { GuildId = guildId };
            db.GuildSettings.Add(guildSettings);
            await db.SaveChangesAsync();
        }

        config = new RichardConfig { GuildId = guildId };
        db.RichardConfigs.Add(config);
        await db.SaveChangesAsync();

        return config;
    }
}
