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
/// The Error Notify module — allows server administrators to subscribe to error
/// notification DMs from BallouBot. Captures Discord.Net errors, unhandled task
/// exceptions, and provides <see cref="IErrorNotificationService"/> for other
/// modules to report errors.
/// </summary>
[BotModule("errornotify")]
public class ErrorNotifyModule : IModule
{
    /// <inheritdoc />
    public string Name => "Error Notify";

    /// <inheritdoc />
    public string Description => "Allows administrators to subscribe to error notification DMs. Use /balloubot errornotify @user to toggle.";

    /// <inheritdoc />
    public string Version => "1.0.0";

    private IModuleContext? _context;
    private ILogger<ErrorNotifyModule>? _logger;
    private ErrorNotificationService? _notificationService;

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ErrorNotificationService>();
        services.AddSingleton<IErrorNotificationService>(sp => sp.GetRequiredService<ErrorNotificationService>());
    }

    /// <inheritdoc />
    public async Task InitializeAsync(IModuleContext context)
    {
        _context = context;
        _logger = context.GetLogger<ErrorNotifyModule>();

        // Resolve and initialize the notification service with the Discord client
        _notificationService = context.Services.GetRequiredService<ErrorNotificationService>();
        _notificationService.SetClient(context.Client);

        // Subscribe to Discord.Net log events for error capture
        context.Client.Log += OnDiscordLogAsync;

        // Subscribe to slash commands
        context.Client.SlashCommandExecuted += OnSlashCommandAsync;

        // Subscribe to unhandled task exceptions
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        await RegisterCommandsAsync();

        _logger.LogInformation("Error Notify module initialized.");
    }

    /// <inheritdoc />
    public Task ShutdownAsync()
    {
        if (_context is not null)
        {
            _context.Client.Log -= OnDiscordLogAsync;
            _context.Client.SlashCommandExecuted -= OnSlashCommandAsync;
        }

        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

        return Task.CompletedTask;
    }

    private async Task RegisterCommandsAsync()
    {
        try
        {
            // We need to update the existing /balloubot command to add our subcommand.
            // Since multiple modules may add subcommands to /balloubot, we build the
            // full command with all known subcommands. The Help module registers "help",
            // and we add "errornotify".
            var command = new SlashCommandBuilder()
                .WithName("balloubot")
                .WithDescription("BallouBot commands")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("help")
                    .WithDescription("Show all loaded modules and their commands.")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("errornotify")
                    .WithDescription("Toggle error notification DMs for a user (Administrator only).")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("user", ApplicationCommandOptionType.User,
                        "The user to toggle error notifications for.", isRequired: true));

            var builtCommand = command.Build();
            foreach (var guild in _context!.Client.Guilds)
            {
                await guild.CreateApplicationCommandAsync(builtCommand);
            }

            _logger?.LogInformation("Registered /balloubot errornotify command on {Count} guild(s).",
                _context.Client.Guilds.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to register /balloubot errornotify slash command.");
        }
    }

    private async Task OnSlashCommandAsync(SocketSlashCommand command)
    {
        if (command.CommandName != "balloubot") return;

        var subCommand = command.Data.Options.FirstOrDefault();
        if (subCommand?.Name != "errornotify") return;

        try
        {
            await HandleErrorNotifyAsync(command, subCommand);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling /balloubot errornotify");
            if (!command.HasResponded)
                await command.RespondAsync("An error occurred processing your command.", ephemeral: true);
        }
    }

    private async Task HandleErrorNotifyAsync(SocketSlashCommand command, SocketSlashCommandDataOption subCommand)
    {
        if (_context is null) return;

        // Check that the invoking user has Administrator permission
        var guildUser = command.User as SocketGuildUser;
        if (guildUser is null || !guildUser.GuildPermissions.Administrator)
        {
            await command.RespondAsync("❌ You need the **Administrator** permission to use this command.",
                ephemeral: true);
            return;
        }

        var targetUser = (IUser)subCommand.Options.First(o => o.Name == "user").Value;

        if (targetUser.IsBot)
        {
            await command.RespondAsync("❌ Cannot subscribe bots to error notifications.", ephemeral: true);
            return;
        }

        var guildId = command.GuildId!.Value;

        using var scope = _context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

        var existing = await db.ErrorNotifySubscriptions
            .FirstOrDefaultAsync(s => s.GuildId == guildId && s.UserId == targetUser.Id);

        if (existing is not null)
        {
            // Unsubscribe
            db.ErrorNotifySubscriptions.Remove(existing);
            await db.SaveChangesAsync();

            await command.RespondAsync(
                $"🔕 {targetUser.Mention} will no longer receive error notification DMs for this server.",
                ephemeral: true);

            _logger?.LogInformation("User {Username} ({UserId}) unsubscribed from error notifications in guild {GuildId}.",
                targetUser.Username, targetUser.Id, guildId);
        }
        else
        {
            // Subscribe
            db.ErrorNotifySubscriptions.Add(new ErrorNotifySubscription
            {
                GuildId = guildId,
                UserId = targetUser.Id
            });
            await db.SaveChangesAsync();

            await command.RespondAsync(
                $"🔔 {targetUser.Mention} will now receive DMs when BallouBot encounters errors in this server.",
                ephemeral: true);

            _logger?.LogInformation("User {Username} ({UserId}) subscribed to error notifications in guild {GuildId}.",
                targetUser.Username, targetUser.Id, guildId);
        }
    }

    /// <summary>
    /// Handles Discord.Net log messages and forwards Error/Critical-level messages
    /// to subscribed users.
    /// </summary>
    private async Task OnDiscordLogAsync(LogMessage logMessage)
    {
        if (logMessage.Severity is not (LogSeverity.Error or LogSeverity.Critical))
            return;

        if (_notificationService is null) return;

        var message = !string.IsNullOrWhiteSpace(logMessage.Message)
            ? logMessage.Message
            : logMessage.Exception?.Message ?? "Unknown error";

        await _notificationService.NotifyErrorAsync(
            $"Discord.Net / {logMessage.Source}",
            message,
            logMessage.Exception);
    }

    /// <summary>
    /// Handles unobserved task exceptions and forwards them to subscribed users.
    /// </summary>
    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (_notificationService is null) return;

        // Fire and forget — we can't await in this event handler
        _ = Task.Run(async () =>
        {
            try
            {
                await _notificationService.NotifyErrorAsync(
                    "Unobserved Task Exception",
                    e.Exception.Message,
                    e.Exception);
            }
            catch
            {
                // Swallow — never let notification errors propagate
            }
        });
    }
}
