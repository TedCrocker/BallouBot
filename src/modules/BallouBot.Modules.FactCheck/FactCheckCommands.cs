using BallouBot.Core;
using BallouBot.Core.Entities;
using BallouBot.Data;
using BallouBot.Modules.FactCheck.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.FactCheck;

/// <summary>
/// Handles slash commands for configuring the Fact Check module.
/// </summary>
public class FactCheckCommands
{
    private readonly IModuleContext _context;
    private readonly ILogger<FactCheckCommands> _logger;

    public FactCheckCommands(IModuleContext context)
    {
        _context = context;
        _logger = context.GetLogger<FactCheckCommands>();
    }

    /// <summary>
    /// Registers the /factcheck slash command with Discord.
    /// </summary>
    public async Task RegisterCommandsAsync()
    {
        try
        {
            var command = new SlashCommandBuilder()
                .WithName("factcheck")
                .WithDescription("Configure the AI Fact Checker module.")
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("toggle")
                    .WithDescription("Enable or disable fact-checking for this server.")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("provider")
                    .WithDescription("Set the AI provider.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("name")
                        .WithDescription("The AI provider to use.")
                        .WithType(ApplicationCommandOptionType.String)
                        .WithRequired(true)
                        .AddChoice("OpenAI", "OpenAI")
                        .AddChoice("Anthropic", "Anthropic")
                        .AddChoice("Azure OpenAI", "AzureOpenAI")
                        .AddChoice("Google Gemini", "Google")))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("apikey")
                    .WithDescription("Set the API key for the AI provider.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("key", ApplicationCommandOptionType.String, "The API key.", isRequired: true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("model")
                    .WithDescription("Set the AI model to use.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("name", ApplicationCommandOptionType.String, "The model name (e.g., gpt-4o-mini, claude-sonnet-4-20250514, gemini-2.5-flash).", isRequired: true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("endpoint")
                    .WithDescription("Set the Azure OpenAI endpoint URL.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("url", ApplicationCommandOptionType.String, "The endpoint URL.", isRequired: true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("watch")
                    .WithDescription("Add a user to the fact-check watch list.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("user", ApplicationCommandOptionType.User, "The user to watch.", isRequired: true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("unwatch")
                    .WithDescription("Remove a user from the fact-check watch list.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("user", ApplicationCommandOptionType.User, "The user to stop watching.", isRequired: true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("list")
                    .WithDescription("Show all watched users.")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("channel")
                    .WithDescription("Restrict fact-checking to a specific channel (empty = all channels).")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("channel", ApplicationCommandOptionType.Channel, "The channel to restrict to (leave empty to clear).", isRequired: false))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("cooldown")
                    .WithDescription("Set the per-user cooldown in seconds.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("seconds", ApplicationCommandOptionType.Integer, "Cooldown in seconds.", isRequired: true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("status")
                    .WithDescription("Show current Fact Check configuration.")
                    .WithType(ApplicationCommandOptionType.SubCommand));

            var builtCommand = command.Build();
            foreach (var guild in _context.Client.Guilds)
            {
                await guild.CreateApplicationCommandAsync(builtCommand);
            }
            _logger.LogInformation("Registered /factcheck slash command on {Count} guild(s).", _context.Client.Guilds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register /factcheck slash command.");
        }
    }

    /// <summary>
    /// Routes incoming /factcheck slash commands to the appropriate handler.
    /// </summary>
    public async Task HandleSlashCommandAsync(SocketSlashCommand command)
    {
        if (command.CommandName != "factcheck") return;

        var subCommand = command.Data.Options.First();

        try
        {
            switch (subCommand.Name)
            {
                case "toggle": await HandleToggleAsync(command); break;
                case "provider": await HandleProviderAsync(command, subCommand); break;
                case "apikey": await HandleApiKeyAsync(command, subCommand); break;
                case "model": await HandleModelAsync(command, subCommand); break;
                case "endpoint": await HandleEndpointAsync(command, subCommand); break;
                case "watch": await HandleWatchAsync(command, subCommand); break;
                case "unwatch": await HandleUnwatchAsync(command, subCommand); break;
                case "list": await HandleListAsync(command); break;
                case "channel": await HandleChannelAsync(command, subCommand); break;
                case "cooldown": await HandleCooldownAsync(command, subCommand); break;
                case "status": await HandleStatusAsync(command); break;
                default:
                    await command.RespondAsync("Unknown subcommand.", ephemeral: true);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling /factcheck {SubCommand}", subCommand.Name);
            if (!command.HasResponded)
                await command.RespondAsync("An error occurred processing your command.", ephemeral: true);
        }
    }

    private async Task HandleToggleAsync(SocketSlashCommand command)
    {
        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var config = await GetOrCreateConfigAsync(db, command.GuildId!.Value);

        config.IsEnabled = !config.IsEnabled;
        config.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var status = config.IsEnabled ? "✅ enabled" : "❌ disabled";
        await command.RespondAsync($"Fact Check is now {status}.", ephemeral: true);
    }

    private async Task HandleProviderAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var providerName = (string)subCommand.Options.First(o => o.Name == "name").Value;

        // Validate provider name
        try { AiProviderFactory.ParseProviderName(providerName); }
        catch
        {
            await command.RespondAsync("❌ Invalid provider. Choose: OpenAI, Anthropic, AzureOpenAI, or Google.", ephemeral: true);
            return;
        }

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var config = await GetOrCreateConfigAsync(db, command.GuildId!.Value);

        config.AiProvider = providerName;
        config.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await command.RespondAsync($"✅ AI provider set to **{providerName}**.", ephemeral: true);
    }

    private async Task HandleApiKeyAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var apiKey = (string)subCommand.Options.First(o => o.Name == "key").Value;

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var config = await GetOrCreateConfigAsync(db, command.GuildId!.Value);

        config.ApiKey = apiKey;
        config.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await command.RespondAsync("✅ API key has been set. (It is stored securely and never displayed.)", ephemeral: true);
    }

    private async Task HandleModelAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var modelName = (string)subCommand.Options.First(o => o.Name == "name").Value;

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var config = await GetOrCreateConfigAsync(db, command.GuildId!.Value);

        config.Model = modelName;
        config.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await command.RespondAsync($"✅ AI model set to **{modelName}**.", ephemeral: true);
    }

    private async Task HandleEndpointAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var endpointUrl = (string)subCommand.Options.First(o => o.Name == "url").Value;

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var config = await GetOrCreateConfigAsync(db, command.GuildId!.Value);

        config.AzureEndpoint = endpointUrl;
        config.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await command.RespondAsync($"✅ Azure endpoint set to `{endpointUrl}`.", ephemeral: true);
    }

    private async Task HandleWatchAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var targetUser = (IUser)subCommand.Options.First(o => o.Name == "user").Value;

        if (targetUser.IsBot)
        {
            await command.RespondAsync("❌ Cannot watch bots.", ephemeral: true);
            return;
        }

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;
        await GetOrCreateConfigAsync(db, guildId);

        var existing = await db.FactCheckUsers
            .FirstOrDefaultAsync(u => u.GuildId == guildId && u.UserId == targetUser.Id);

        if (existing is not null)
        {
            await command.RespondAsync($"{targetUser.Mention} is already on the watch list.", ephemeral: true);
            return;
        }

        db.FactCheckUsers.Add(new FactCheckUser { GuildId = guildId, UserId = targetUser.Id });
        await db.SaveChangesAsync();

        await command.RespondAsync($"✅ {targetUser.Mention} is now being fact-checked.", ephemeral: true);
    }

    private async Task HandleUnwatchAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var targetUser = (IUser)subCommand.Options.First(o => o.Name == "user").Value;

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var existing = await db.FactCheckUsers
            .FirstOrDefaultAsync(u => u.GuildId == guildId && u.UserId == targetUser.Id);

        if (existing is null)
        {
            await command.RespondAsync($"{targetUser.Mention} is not on the watch list.", ephemeral: true);
            return;
        }

        db.FactCheckUsers.Remove(existing);
        await db.SaveChangesAsync();

        await command.RespondAsync($"✅ {targetUser.Mention} has been removed from the watch list.", ephemeral: true);
    }

    private async Task HandleListAsync(SocketSlashCommand command)
    {
        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var users = await db.FactCheckUsers
            .Where(u => u.GuildId == guildId)
            .OrderBy(u => u.AddedAt)
            .ToListAsync();

        if (users.Count == 0)
        {
            await command.RespondAsync("No users are being watched. Use `/factcheck watch` to add users.", ephemeral: true);
            return;
        }

        var userList = string.Join("\n", users.Select(u => $"• <@{u.UserId}>"));
        var embed = new EmbedBuilder()
            .WithTitle("🔍 Fact Check — Watch List")
            .WithDescription(userList)
            .WithColor(new Color(0x3498DB))
            .WithFooter($"{users.Count} user(s) being watched")
            .WithCurrentTimestamp();

        await command.RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    private async Task HandleChannelAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var config = await GetOrCreateConfigAsync(db, command.GuildId!.Value);

        var channelOption = subCommand.Options.FirstOrDefault(o => o.Name == "channel");

        if (channelOption is null)
        {
            config.ChannelId = null;
            config.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await command.RespondAsync("✅ Fact-checking will monitor all channels.", ephemeral: true);
            return;
        }

        if (channelOption.Value is not ITextChannel textChannel)
        {
            await command.RespondAsync("❌ Please select a text channel.", ephemeral: true);
            return;
        }

        config.ChannelId = textChannel.Id;
        config.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await command.RespondAsync($"✅ Fact-checking restricted to {textChannel.Mention}.", ephemeral: true);
    }

    private async Task HandleCooldownAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var seconds = (long)subCommand.Options.First(o => o.Name == "seconds").Value;

        if (seconds < 0 || seconds > 3600)
        {
            await command.RespondAsync("❌ Cooldown must be between 0 and 3600 seconds.", ephemeral: true);
            return;
        }

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var config = await GetOrCreateConfigAsync(db, command.GuildId!.Value);

        config.CooldownSeconds = (int)seconds;
        config.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await command.RespondAsync($"✅ Per-user cooldown set to **{seconds} seconds**.", ephemeral: true);
    }

    private async Task HandleStatusAsync(SocketSlashCommand command)
    {
        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var config = await db.FactCheckConfigs
            .AsNoTracking()
            .Include(c => c.WatchedUsers)
            .FirstOrDefaultAsync(c => c.GuildId == guildId);

        if (config is null)
        {
            await command.RespondAsync("Fact Check hasn't been configured yet. Use `/factcheck toggle` to get started.", ephemeral: true);
            return;
        }

        var statusEmoji = config.IsEnabled ? "✅" : "❌";
        var hasKey = !string.IsNullOrWhiteSpace(config.ApiKey) ? "✅ Set" : "❌ Not set";
        var channelDesc = config.ChannelId.HasValue ? $"<#{config.ChannelId.Value}>" : "All channels";

        var embed = new EmbedBuilder()
            .WithTitle("🔍 Fact Check — Status")
            .WithColor(new Color(0x3498DB))
            .AddField("Status", $"{statusEmoji} {(config.IsEnabled ? "Enabled" : "Disabled")}", true)
            .AddField("Provider", config.AiProvider, true)
            .AddField("Model", config.Model, true)
            .AddField("API Key", hasKey, true)
            .AddField("Channel", channelDesc, true)
            .AddField("Watched Users", config.WatchedUsers.Count.ToString(), true)
            .AddField("Cooldown", $"{config.CooldownSeconds}s", true)
            .AddField("Max Checks/Hour", config.MaxChecksPerHour.ToString(), true)
            .AddField("Min Message Length", $"{config.MinMessageLength} chars", true)
            .WithCurrentTimestamp();

        await command.RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    /// <summary>
    /// Gets an existing FactCheckConfig for the guild or creates a new one.
    /// </summary>
    public static async Task<FactCheckConfig> GetOrCreateConfigAsync(BotDbContext db, ulong guildId)
    {
        var config = await db.FactCheckConfigs.FirstOrDefaultAsync(c => c.GuildId == guildId);
        if (config is not null) return config;

        var guildSettings = await db.GuildSettings.FirstOrDefaultAsync(g => g.GuildId == guildId);
        if (guildSettings is null)
        {
            guildSettings = new GuildSettings { GuildId = guildId };
            db.GuildSettings.Add(guildSettings);
            await db.SaveChangesAsync();
        }

        config = new FactCheckConfig { GuildId = guildId };
        db.FactCheckConfigs.Add(config);
        await db.SaveChangesAsync();

        return config;
    }
}
