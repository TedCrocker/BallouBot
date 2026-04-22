using BallouBot.Core;
using BallouBot.Core.Entities;
using BallouBot.Data;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.Welcome.Handlers;

/// <summary>
/// Handles the UserJoined event to send welcome messages.
/// Retrieves the guild's welcome configuration from the database and sends
/// the formatted message to the configured channel.
/// </summary>
public class WelcomeHandler
{
    private readonly IModuleContext _context;
    private readonly ILogger<WelcomeHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WelcomeHandler"/> class.
    /// </summary>
    /// <param name="context">The module context.</param>
    public WelcomeHandler(IModuleContext context)
    {
        _context = context;
        _logger = context.GetLogger<WelcomeHandler>();
    }

    /// <summary>
    /// Handles the UserJoined event by sending a welcome message if configured.
    /// </summary>
    /// <param name="user">The user who joined the guild.</param>
    public async Task HandleUserJoinedAsync(SocketGuildUser user)
    {
        try
        {
            _logger.LogDebug("User {Username} ({UserId}) joined guild {GuildName} ({GuildId})",
                user.Username, user.Id, user.Guild.Name, user.Guild.Id);

            // Use a scoped service provider for the DbContext
            using var scope = _context.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            var config = await db.WelcomeConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.GuildId == user.Guild.Id);

            if (config is null || !config.IsEnabled)
            {
                _logger.LogDebug("Welcome messages not configured or disabled for guild {GuildId}", user.Guild.Id);
                return;
            }

            var channel = user.Guild.GetTextChannel(config.ChannelId);
            if (channel is null)
            {
                _logger.LogWarning("Welcome channel {ChannelId} not found in guild {GuildId}",
                    config.ChannelId, user.Guild.Id);
                return;
            }

            var formattedMessage = config.FormatMessage(
                user.Mention,
                user.DisplayName,
                user.Guild.Name,
                user.Guild.MemberCount);

            if (config.UseEmbed)
            {
                var embed = BuildWelcomeEmbed(config, formattedMessage, user);
                await channel.SendMessageAsync(embed: embed);
            }
            else
            {
                await channel.SendMessageAsync(formattedMessage);
            }

            _logger.LogInformation("Sent welcome message for {Username} in guild {GuildName}",
                user.Username, user.Guild.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UserJoined event for user {UserId} in guild {GuildId}",
                user.Id, user.Guild.Id);
        }
    }

    /// <summary>
    /// Builds a welcome embed from the configuration and formatted message.
    /// </summary>
    public static Embed BuildWelcomeEmbed(WelcomeConfig config, string formattedMessage, SocketGuildUser user)
    {
        var colorValue = Convert.ToUInt32(config.EmbedColor, 16);
        var color = new Color(colorValue);

        var builder = new EmbedBuilder()
            .WithDescription(formattedMessage)
            .WithColor(color)
            .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
            .WithCurrentTimestamp();

        if (!string.IsNullOrWhiteSpace(config.EmbedTitle))
        {
            builder.WithTitle(config.EmbedTitle);
        }

        return builder.Build();
    }
}
