using BallouBot.Core;
using BallouBot.Data;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BallouBot.Host;

/// <summary>
/// Background hosted service that manages the Discord bot lifecycle.
/// Handles connecting, disconnecting, and coordinating module initialization.
/// </summary>
public class BotHostedService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BotHostedService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ModuleLoader _moduleLoader;

    /// <summary>
    /// Initializes a new instance of the <see cref="BotHostedService"/> class.
    /// </summary>
    public BotHostedService(
        DiscordSocketClient client,
        IServiceProvider services,
        IConfiguration configuration,
        ILogger<BotHostedService> logger,
        ILoggerFactory loggerFactory,
        ModuleLoader moduleLoader)
    {
        _client = client;
        _services = services;
        _configuration = configuration;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _moduleLoader = moduleLoader;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BallouBot is starting...");

        // Ensure database is created and migrations are applied
        using (var scope = _services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
            await db.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Database migrations applied.");
        }

        // Wire up Discord.Net logging to Serilog/Microsoft.Extensions.Logging
        _client.Log += LogDiscordMessage;
        _client.Ready += OnClientReady;

        // Get bot token from configuration
        var token = _configuration["Discord:Token"];
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogCritical("Discord bot token is not configured. Set 'Discord:Token' in appsettings.json or environment variables.");
            throw new InvalidOperationException("Discord bot token is not configured.");
        }

        // Connect to Discord
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        _logger.LogInformation("BallouBot connected to Discord.");
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BallouBot is shutting down...");

        await _moduleLoader.ShutdownModulesAsync();
        await _client.StopAsync();
        await _client.LogoutAsync();

        _logger.LogInformation("BallouBot has shut down.");
    }

    private async Task OnClientReady()
    {
        _logger.LogInformation("Discord client is ready. Bot user: {BotUser} ({BotId})",
            _client.CurrentUser.Username, _client.CurrentUser.Id);

        // Initialize all modules
        var context = new ModuleContext(_client, _services, _loggerFactory);
        var modules = _services.GetServices<IModule>();
        await _moduleLoader.InitializeModulesAsync(context, modules);

        _logger.LogInformation("All modules initialized. {Count} module(s) loaded.", _moduleLoader.Modules.Count);
    }

    private Task LogDiscordMessage(LogMessage message)
    {
        var logLevel = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, message.Exception, "[Discord.Net] {Source}: {Message}", message.Source, message.Message);
        return Task.CompletedTask;
    }
}
