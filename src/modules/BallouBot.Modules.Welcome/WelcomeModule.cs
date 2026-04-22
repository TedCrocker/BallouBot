using BallouBot.Core;
using BallouBot.Modules.Welcome.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.Welcome;

/// <summary>
/// Welcome module — sends customizable welcome messages when users join a guild.
/// Provides slash commands for configuration and listens to the UserJoined gateway event.
/// </summary>
[BotModule("welcome")]
public class WelcomeModule : IModule
{
    private WelcomeHandler? _welcomeHandler;
    private WelcomeCommands? _welcomeCommands;

    /// <inheritdoc />
    public string Name => "Welcome Messages";

    /// <inheritdoc />
    public string Description => "Sends customizable welcome messages when users join a server.";

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<WelcomeHandler>();
        services.AddScoped<WelcomeCommands>();
    }

    /// <inheritdoc />
    public async Task InitializeAsync(IModuleContext context)
    {
        var logger = context.GetLogger<WelcomeModule>();

        // Create and register the welcome handler for UserJoined events
        _welcomeHandler = new WelcomeHandler(context);
        context.Client.UserJoined += _welcomeHandler.HandleUserJoinedAsync;

        // Create and register slash commands
        _welcomeCommands = new WelcomeCommands(context);
        context.Client.SlashCommandExecuted += _welcomeCommands.HandleSlashCommandAsync;
        context.Client.Ready += async () => await _welcomeCommands.RegisterCommandsAsync();

        // If the client is already connected (Ready already fired), register commands now
        if (context.Client.ConnectionState == Discord.ConnectionState.Connected)
        {
            await _welcomeCommands.RegisterCommandsAsync();
        }

        logger.LogInformation("Welcome module initialized.");
    }

    /// <inheritdoc />
    public Task ShutdownAsync()
    {
        // Event handlers will be cleaned up when the client disconnects
        _welcomeHandler = null;
        _welcomeCommands = null;
        return Task.CompletedTask;
    }
}
