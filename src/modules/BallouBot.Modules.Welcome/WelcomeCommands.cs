using BallouBot.Core;
using BallouBot.Core.Entities;
using BallouBot.Data;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.Welcome;

/// <summary>
/// Handles slash commands for configuring welcome messages.
/// Commands: /welcome channel, /welcome message, /welcome toggle, /welcome preview, /welcome embed
/// </summary>
public class WelcomeCommands
{
    private readonly IModuleContext _context;
    private readonly ILogger<WelcomeCommands> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WelcomeCommands"/> class.
    /// </summary>
    /// <param name="context">The module context.</param>
    public WelcomeCommands(IModuleContext context)
    {
        _context = context;
        _logger = context.GetLogger<WelcomeCommands>();
    }

    /// <summary>
    /// Registers the /welcome slash command with Discord.
    /// </summary>
    public async Task RegisterCommandsAsync()
    {
        try
        {
            var command = new SlashCommandBuilder()
                .WithName("welcome")
                .WithDescription("Configure welcome messages for your server.")
                .WithDefaultMemberPermissions(GuildPermission.ManageGuild)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("channel")
                    .WithDescription("Set the channel for welcome messages.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("target", ApplicationCommandOptionType.Channel, "The channel to send welcome messages in.", isRequired: true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("message")
                    .WithDescription("Set the welcome message. Use {user}, {username}, {server}, {membercount}.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("text", ApplicationCommandOptionType.String, "The welcome message template.", isRequired: true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("toggle")
                    .WithDescription("Enable or disable welcome messages.")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("preview")
                    .WithDescription("Preview the current welcome message.")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("embed")
                    .WithDescription("Toggle between plain text and embed mode.")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("color")
                    .WithDescription("Set the embed color (hex code without #).")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("hex", ApplicationCommandOptionType.String, "Hex color code (e.g., 5865F2).", isRequired: true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("title")
                    .WithDescription("Set the embed title.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("text", ApplicationCommandOptionType.String, "The embed title text.", isRequired: true));

            // Register as guild commands (instant) on all connected guilds
            var builtCommand = command.Build();
            foreach (var guild in _context.Client.Guilds)
            {
                await guild.CreateApplicationCommandAsync(builtCommand);
                _logger.LogDebug("Registered /welcome command on guild {GuildName} ({GuildId})", guild.Name, guild.Id);
            }
            _logger.LogInformation("Registered /welcome slash command on {Count} guild(s).", _context.Client.Guilds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register /welcome slash command.");
        }
    }

    /// <summary>
    /// Routes incoming /welcome slash commands to the appropriate handler.
    /// </summary>
    /// <param name="command">The slash command interaction.</param>
    public async Task HandleSlashCommandAsync(SocketSlashCommand command)
    {
        if (command.CommandName != "welcome") return;

        var subCommand = command.Data.Options.First();

        try
        {
            switch (subCommand.Name)
            {
                case "channel":
                    await HandleChannelAsync(command, subCommand);
                    break;
                case "message":
                    await HandleMessageAsync(command, subCommand);
                    break;
                case "toggle":
                    await HandleToggleAsync(command);
                    break;
                case "preview":
                    await HandlePreviewAsync(command);
                    break;
                case "embed":
                    await HandleEmbedToggleAsync(command);
                    break;
                case "color":
                    await HandleColorAsync(command, subCommand);
                    break;
                case "title":
                    await HandleTitleAsync(command, subCommand);
                    break;
                default:
                    await command.RespondAsync("Unknown subcommand.", ephemeral: true);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling /welcome {SubCommand}", subCommand.Name);
            await command.RespondAsync("An error occurred processing your command.", ephemeral: true);
        }
    }

    private async Task HandleChannelAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var channel = (IChannel)subCommand.Options.First().Value;

        if (channel is not ITextChannel textChannel)
        {
            await command.RespondAsync("Please select a text channel.", ephemeral: true);
            return;
        }

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var config = await GetOrCreateConfigAsync(db, guildId);
        config.ChannelId = textChannel.Id;
        config.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await command.RespondAsync($"✅ Welcome messages will be sent to {textChannel.Mention}.", ephemeral: true);
    }

    private async Task HandleMessageAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var messageText = (string)subCommand.Options.First().Value;

        if (messageText.Length > 2000)
        {
            await command.RespondAsync("Message must be 2000 characters or less.", ephemeral: true);
            return;
        }

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var config = await GetOrCreateConfigAsync(db, guildId);
        config.Message = messageText;
        config.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await command.RespondAsync($"✅ Welcome message updated.\n\n**Preview:**\n{config.FormatMessage(command.User.Mention, command.User.Username, "Server Name", 100)}", ephemeral: true);
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
        await command.RespondAsync($"Welcome messages are now {status}.", ephemeral: true);
    }

    private async Task HandlePreviewAsync(SocketSlashCommand command)
    {
        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var config = await db.WelcomeConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.GuildId == guildId);

        if (config is null)
        {
            await command.RespondAsync("Welcome messages haven't been configured yet. Use `/welcome channel` to get started.", ephemeral: true);
            return;
        }

        var guild = _context.Client.GetGuild(guildId);
        var formattedMessage = config.FormatMessage(
            command.User.Mention,
            command.User.Username,
            guild?.Name ?? "Server",
            guild?.MemberCount ?? 0);

        var statusEmoji = config.IsEnabled ? "✅" : "❌";
        var channelMention = config.ChannelId != 0 ? $"<#{config.ChannelId}>" : "Not set";

        if (config.UseEmbed)
        {
            var colorValue = Convert.ToUInt32(config.EmbedColor, 16);
            var embedBuilder = new EmbedBuilder()
                .WithDescription(formattedMessage)
                .WithColor(new Color(colorValue))
                .WithThumbnailUrl(command.User.GetAvatarUrl() ?? command.User.GetDefaultAvatarUrl())
                .WithCurrentTimestamp()
                .WithFooter($"Status: {statusEmoji} | Channel: {channelMention} | Mode: Embed");

            if (!string.IsNullOrWhiteSpace(config.EmbedTitle))
            {
                embedBuilder.WithTitle(config.EmbedTitle);
            }

            await command.RespondAsync("**Welcome Message Preview:**", embed: embedBuilder.Build(), ephemeral: true);
        }
        else
        {
            await command.RespondAsync(
                $"**Welcome Message Preview:**\n{formattedMessage}\n\n" +
                $"*Status: {statusEmoji} | Channel: {channelMention} | Mode: Plain text*",
                ephemeral: true);
        }
    }

    private async Task HandleEmbedToggleAsync(SocketSlashCommand command)
    {
        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var config = await GetOrCreateConfigAsync(db, guildId);
        config.UseEmbed = !config.UseEmbed;
        config.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var mode = config.UseEmbed ? "embed" : "plain text";
        await command.RespondAsync($"✅ Welcome messages will now use **{mode}** mode.", ephemeral: true);
    }

    private async Task HandleColorAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var hex = ((string)subCommand.Options.First().Value).TrimStart('#');

        if (hex.Length != 6 || !uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out _))
        {
            await command.RespondAsync("Invalid hex color. Use a 6-character hex code like `5865F2`.", ephemeral: true);
            return;
        }

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var config = await GetOrCreateConfigAsync(db, guildId);
        config.EmbedColor = hex.ToUpperInvariant();
        config.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await command.RespondAsync($"✅ Embed color set to `#{hex.ToUpperInvariant()}`.", ephemeral: true);
    }

    private async Task HandleTitleAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var titleText = (string)subCommand.Options.First().Value;

        if (titleText.Length > 256)
        {
            await command.RespondAsync("Title must be 256 characters or less.", ephemeral: true);
            return;
        }

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var config = await GetOrCreateConfigAsync(db, guildId);
        config.EmbedTitle = titleText;
        config.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await command.RespondAsync($"✅ Embed title set to **{titleText}**.", ephemeral: true);
    }

    /// <summary>
    /// Gets an existing WelcomeConfig for the guild or creates a new one.
    /// Also ensures the parent GuildSettings record exists.
    /// </summary>
    public static async Task<WelcomeConfig> GetOrCreateConfigAsync(BotDbContext db, ulong guildId)
    {
        var config = await db.WelcomeConfigs.FirstOrDefaultAsync(w => w.GuildId == guildId);

        if (config is not null) return config;

        // Ensure GuildSettings exists
        var guildSettings = await db.GuildSettings.FirstOrDefaultAsync(g => g.GuildId == guildId);
        if (guildSettings is null)
        {
            guildSettings = new GuildSettings { GuildId = guildId };
            db.GuildSettings.Add(guildSettings);
            await db.SaveChangesAsync();
        }

        config = new WelcomeConfig { GuildId = guildId };
        db.WelcomeConfigs.Add(config);
        await db.SaveChangesAsync();

        return config;
    }
}
