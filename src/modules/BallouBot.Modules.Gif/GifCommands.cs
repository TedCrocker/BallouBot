using System.Collections.Concurrent;
using BallouBot.Core;
using BallouBot.Core.Entities;
using BallouBot.Data;
using BallouBot.Modules.Gif.Models;
using BallouBot.Modules.Gif.Providers;
using BallouBot.Modules.Gif.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.Gif;

/// <summary>
/// Handles slash commands and button interactions for the GIF module.
/// Provides /gif search with interactive preview browsing and /gif config for guild settings.
/// </summary>
public class GifCommands
{
    private readonly IModuleContext _context;
    private readonly ILogger<GifCommands> _logger;
    private readonly GifProviderFactory _providerFactory;

    /// <summary>
    /// Tracks active GIF browsing sessions per user.
    /// Key: "userId-channelId", Value: session state.
    /// Sessions expire after 5 minutes of inactivity.
    /// </summary>
    private readonly ConcurrentDictionary<string, GifBrowseSession> _sessions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="GifCommands"/> class.
    /// </summary>
    public GifCommands(IModuleContext context, GifProviderFactory providerFactory)
    {
        _context = context;
        _logger = context.GetLogger<GifCommands>();
        _providerFactory = providerFactory;
    }

    /// <summary>
    /// Registers the /gif slash command with Discord for all guilds.
    /// </summary>
    public async Task RegisterCommandsAsync()
    {
        try
        {
            var command = new SlashCommandBuilder()
                .WithName("gif")
                .WithDescription("Search for and post GIFs!")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("search")
                    .WithDescription("Search for a GIF to preview and post.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("query", ApplicationCommandOptionType.String, "What to search for.", isRequired: true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("config")
                    .WithDescription("Configure the GIF module for this server.")
                    .WithType(ApplicationCommandOptionType.SubCommandGroup)
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("provider")
                        .WithDescription("Set the GIF provider for this server.")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption(new SlashCommandOptionBuilder()
                            .WithName("source")
                            .WithDescription("The GIF provider to use.")
                            .WithType(ApplicationCommandOptionType.String)
                            .WithRequired(true)
                            .AddChoice("Tenor", "Tenor")
                            .AddChoice("Giphy", "Giphy")
                            .AddChoice("RedGifs (NSFW)", "RedGifs")))
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("apikey")
                        .WithDescription("Set the API key for the current provider.")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption("key", ApplicationCommandOptionType.String, "The API key.", isRequired: true))
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("preview-count")
                        .WithDescription("Set how many GIF results to browse through (1–10).")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption("count", ApplicationCommandOptionType.Integer, "Number of previews (1–10).", isRequired: true))
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("status")
                        .WithDescription("Show current GIF module configuration.")
                        .WithType(ApplicationCommandOptionType.SubCommand)));

            var builtCommand = command.Build();
            foreach (var guild in _context.Client.Guilds)
            {
                await guild.CreateApplicationCommandAsync(builtCommand);
                _logger.LogDebug("Registered /gif command on guild {GuildName} ({GuildId})", guild.Name, guild.Id);
            }
            _logger.LogInformation("Registered /gif slash command on {Count} guild(s).", _context.Client.Guilds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register /gif slash command.");
        }
    }

    /// <summary>
    /// Routes incoming /gif slash commands to the appropriate handler.
    /// </summary>
    public async Task HandleSlashCommandAsync(SocketSlashCommand command)
    {
        if (command.CommandName != "gif") return;

        var subCommand = command.Data.Options.First();

        try
        {
            switch (subCommand.Name)
            {
                case "search":
                    await HandleSearchAsync(command, subCommand);
                    break;
                case "config":
                    await HandleConfigGroupAsync(command, subCommand);
                    break;
                default:
                    await command.RespondAsync("Unknown subcommand.", ephemeral: true);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling /gif {SubCommand}", subCommand.Name);
            if (!command.HasResponded)
            {
                await command.RespondAsync("An error occurred processing your command.", ephemeral: true);
            }
        }
    }

    /// <summary>
    /// Handles button interactions for GIF preview navigation (previous, next, post, cancel).
    /// </summary>
    public async Task HandleButtonAsync(SocketMessageComponent component)
    {
        if (!component.Data.CustomId.StartsWith("gif:")) return;

        var parts = component.Data.CustomId.Split(':');
        if (parts.Length < 3) return;

        var action = parts[1];
        var sessionKey = parts[2];

        if (!_sessions.TryGetValue(sessionKey, out var session))
        {
            await component.RespondAsync("This GIF preview has expired. Use `/gif search` to start a new search.", ephemeral: true);
            return;
        }

        // Only the original user can interact with the buttons
        if (session.UserId != component.User.Id)
        {
            await component.RespondAsync("This isn't your GIF browser!", ephemeral: true);
            return;
        }

        try
        {
            switch (action)
            {
                case "prev":
                    session.CurrentIndex = (session.CurrentIndex - 1 + session.Results.Count) % session.Results.Count;
                    await component.UpdateAsync(msg => UpdatePreviewMessage(msg, session));
                    break;

                case "next":
                    session.CurrentIndex = (session.CurrentIndex + 1) % session.Results.Count;
                    await component.UpdateAsync(msg => UpdatePreviewMessage(msg, session));
                    break;

                case "post":
                    var selectedGif = session.Results[session.CurrentIndex];
                    _sessions.TryRemove(sessionKey, out _);

                    // Dismiss the ephemeral preview
                    await component.UpdateAsync(msg =>
                    {
                        msg.Content = $"✅ Posted GIF: **{selectedGif.Title}**";
                        msg.Embed = null;
                        msg.Components = new ComponentBuilder().Build();
                    });

                    // Post the GIF publicly in the channel
                    var channel = component.Channel;
                    var embed = new EmbedBuilder()
                        .WithImageUrl(selectedGif.Url)
                        .WithColor(new Color(0x00D4AA))
                        .WithFooter($"🎬 via {selectedGif.ProviderName} • searched by {component.User.GlobalName ?? component.User.Username}")
                        .Build();

                    await channel.SendMessageAsync(embed: embed);
                    break;

                case "cancel":
                    _sessions.TryRemove(sessionKey, out _);
                    await component.UpdateAsync(msg =>
                    {
                        msg.Content = "❌ GIF search cancelled.";
                        msg.Embed = null;
                        msg.Components = new ComponentBuilder().Build();
                    });
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling GIF button interaction {Action}", action);
        }
    }

    private async Task HandleSearchAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var query = (string)subCommand.Options.First(o => o.Name == "query").Value;
        var guildId = command.GuildId!.Value;

        await command.DeferAsync(ephemeral: true);

        // Load guild config
        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var config = await db.GifConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.GuildId == guildId);

        if (config is null || !config.IsEnabled)
        {
            await command.FollowupAsync("The GIF module hasn't been configured yet. Ask a server admin to run `/gif config provider` first.", ephemeral: true);
            return;
        }

        // Parse provider type
        if (!Enum.TryParse<GifProviderType>(config.Provider, true, out var providerType))
        {
            await command.FollowupAsync($"Unknown GIF provider: **{config.Provider}**. Use `/gif config provider` to set a valid one.", ephemeral: true);
            return;
        }

        var provider = _providerFactory.GetProvider(providerType);

        // NSFW channel check
        if (provider.IsNsfw)
        {
            var channel = command.Channel as ITextChannel;
            if (channel is null || !channel.IsNsfw)
            {
                await command.FollowupAsync($"🔞 **{provider.DisplayName}** is an NSFW provider. This command can only be used in NSFW-marked channels.", ephemeral: true);
                return;
            }
        }

        // API key check
        if (provider.RequiresApiKey && string.IsNullOrWhiteSpace(config.ApiKey))
        {
            await command.FollowupAsync($"No API key configured for **{provider.DisplayName}**. Ask a server admin to run `/gif config apikey`.", ephemeral: true);
            return;
        }

        // Search for GIFs
        var results = await provider.SearchAsync(query, config.PreviewCount, config.ApiKey);

        if (results.Count == 0)
        {
            await command.FollowupAsync($"No GIFs found for **{query}** on {provider.DisplayName}. Try a different search term.", ephemeral: true);
            return;
        }

        // Create a browsing session
        var sessionKey = $"{command.User.Id}-{command.ChannelId}";
        var session = new GifBrowseSession
        {
            UserId = command.User.Id,
            ChannelId = command.ChannelId!.Value,
            Query = query,
            Results = results,
            CurrentIndex = 0,
            CreatedAt = DateTime.UtcNow
        };

        _sessions[sessionKey] = session;

        // Clean up expired sessions (older than 5 minutes)
        CleanExpiredSessions();

        // Build the preview message
        var embed = BuildPreviewEmbed(session);
        var components = BuildPreviewComponents(sessionKey, session);

        await command.FollowupAsync(
            text: $"🔍 Found **{results.Count}** result(s) for **{query}** via {provider.DisplayName}:",
            embed: embed,
            components: components,
            ephemeral: true);
    }

    private async Task HandleConfigGroupAsync(SocketSlashCommand command, SocketSlashCommandDataOption configGroup)
    {
        var subCommand = configGroup.Options.First();

        switch (subCommand.Name)
        {
            case "provider":
                await HandleConfigProviderAsync(command, subCommand);
                break;
            case "apikey":
                await HandleConfigApiKeyAsync(command, subCommand);
                break;
            case "preview-count":
                await HandleConfigPreviewCountAsync(command, subCommand);
                break;
            case "status":
                await HandleConfigStatusAsync(command);
                break;
            default:
                await command.RespondAsync("Unknown config subcommand.", ephemeral: true);
                break;
        }
    }

    private async Task HandleConfigProviderAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var providerName = (string)subCommand.Options.First(o => o.Name == "source").Value;

        if (!Enum.TryParse<GifProviderType>(providerName, true, out var providerType))
        {
            await command.RespondAsync($"Unknown provider: **{providerName}**.", ephemeral: true);
            return;
        }

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var config = await GetOrCreateConfigAsync(db, guildId);
        config.Provider = providerType.ToString();
        config.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var provider = _providerFactory.GetProvider(providerType);
        var nsfwNote = provider.IsNsfw ? "\n⚠️ This provider serves **NSFW content** and will only work in NSFW-marked channels." : "";
        var keyNote = provider.RequiresApiKey ? $"\n🔑 This provider requires an API key. Use `/gif config apikey` to set it." : "";

        await command.RespondAsync($"✅ GIF provider set to **{provider.DisplayName}**.{nsfwNote}{keyNote}", ephemeral: true);
    }

    private async Task HandleConfigApiKeyAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var apiKey = (string)subCommand.Options.First(o => o.Name == "key").Value;

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var config = await GetOrCreateConfigAsync(db, guildId);
        config.ApiKey = apiKey;
        config.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        // Mask the key for display
        var maskedKey = apiKey.Length > 6
            ? apiKey[..3] + new string('*', apiKey.Length - 6) + apiKey[^3..]
            : new string('*', apiKey.Length);

        await command.RespondAsync($"✅ API key set for **{config.Provider}**: `{maskedKey}`", ephemeral: true);
    }

    private async Task HandleConfigPreviewCountAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        var count = (long)subCommand.Options.First(o => o.Name == "count").Value;

        if (count < 1 || count > 10)
        {
            await command.RespondAsync("Preview count must be between 1 and 10.", ephemeral: true);
            return;
        }

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var config = await GetOrCreateConfigAsync(db, guildId);
        config.PreviewCount = (int)count;
        config.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await command.RespondAsync($"✅ GIF preview count set to **{count}** results.", ephemeral: true);
    }

    private async Task HandleConfigStatusAsync(SocketSlashCommand command)
    {
        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        var guildId = command.GuildId!.Value;

        var config = await db.GifConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.GuildId == guildId);

        if (config is null)
        {
            await command.RespondAsync("The GIF module hasn't been configured yet. Use `/gif config provider` to get started.", ephemeral: true);
            return;
        }

        var hasApiKey = !string.IsNullOrWhiteSpace(config.ApiKey);

        // Look up provider info
        Enum.TryParse<GifProviderType>(config.Provider, true, out var providerType);
        var provider = _providerFactory.GetProvider(providerType);

        var embed = new EmbedBuilder()
            .WithTitle("🎬 GIF Module — Status")
            .WithColor(new Color(0x00D4AA))
            .AddField("Status", config.IsEnabled ? "✅ Enabled" : "❌ Disabled", true)
            .AddField("Provider", config.Provider, true)
            .AddField("NSFW Provider", provider.IsNsfw ? "⚠️ Yes" : "No", true)
            .AddField("API Key", hasApiKey ? "✅ Configured" : (provider.RequiresApiKey ? "❌ Not set" : "Not required"), true)
            .AddField("Preview Count", config.PreviewCount.ToString(), true)
            .WithCurrentTimestamp();

        await command.RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    /// <summary>
    /// Gets an existing GifConfig for the guild or creates a new one.
    /// Also ensures the parent GuildSettings record exists.
    /// </summary>
    public static async Task<GifConfig> GetOrCreateConfigAsync(BotDbContext db, ulong guildId)
    {
        var config = await db.GifConfigs.FirstOrDefaultAsync(c => c.GuildId == guildId);

        if (config is not null) return config;

        // Ensure GuildSettings exists
        var guildSettings = await db.GuildSettings.FirstOrDefaultAsync(g => g.GuildId == guildId);
        if (guildSettings is null)
        {
            guildSettings = new GuildSettings { GuildId = guildId };
            db.GuildSettings.Add(guildSettings);
            await db.SaveChangesAsync();
        }

        config = new GifConfig { GuildId = guildId };
        db.GifConfigs.Add(config);
        await db.SaveChangesAsync();

        return config;
    }

    private static Embed BuildPreviewEmbed(GifBrowseSession session)
    {
        var gif = session.Results[session.CurrentIndex];

        return new EmbedBuilder()
            .WithTitle(string.IsNullOrWhiteSpace(gif.Title) ? $"GIF {session.CurrentIndex + 1}" : gif.Title)
            .WithImageUrl(gif.PreviewUrl)
            .WithColor(new Color(0x00D4AA))
            .WithFooter($"Result {session.CurrentIndex + 1} of {session.Results.Count} • via {gif.ProviderName}")
            .Build();
    }

    private static MessageComponent BuildPreviewComponents(string sessionKey, GifBrowseSession session)
    {
        var builder = new ComponentBuilder();

        builder.WithButton("◀️ Previous", $"gif:prev:{sessionKey}",
            style: ButtonStyle.Secondary,
            disabled: session.Results.Count <= 1);

        builder.WithButton($"{session.CurrentIndex + 1}/{session.Results.Count}", "gif:counter",
            style: ButtonStyle.Secondary,
            disabled: true);

        builder.WithButton("Next ▶️", $"gif:next:{sessionKey}",
            style: ButtonStyle.Secondary,
            disabled: session.Results.Count <= 1);

        builder.WithButton("✅ Post", $"gif:post:{sessionKey}",
            style: ButtonStyle.Success);

        builder.WithButton("❌ Cancel", $"gif:cancel:{sessionKey}",
            style: ButtonStyle.Danger);

        return builder.Build();
    }

    private void UpdatePreviewMessage(MessageProperties msg, GifBrowseSession session)
    {
        var gif = session.Results[session.CurrentIndex];
        var sessionKey = $"{session.UserId}-{session.ChannelId}";

        msg.Embed = BuildPreviewEmbed(session);
        msg.Components = BuildPreviewComponents(sessionKey, session);
    }

    private void CleanExpiredSessions()
    {
        var expiry = DateTime.UtcNow.AddMinutes(-5);
        var expiredKeys = _sessions
            .Where(kvp => kvp.Value.CreatedAt < expiry)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _sessions.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} expired GIF browse sessions.", expiredKeys.Count);
        }
    }

    /// <summary>
    /// Represents an active GIF browsing session for a user.
    /// </summary>
    private class GifBrowseSession
    {
        public ulong UserId { get; set; }
        public ulong ChannelId { get; set; }
        public string Query { get; set; } = string.Empty;
        public List<GifResult> Results { get; set; } = [];
        public int CurrentIndex { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
