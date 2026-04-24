using BallouBot.Core;
using BallouBot.Modules.RandomRichard.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.RandomRichard;

/// <summary>
/// Random Richard module — periodically DMs random server members with Wikipedia
/// articles about famous people named Richard. Supports whitelist/blacklist and
/// configurable send intervals.
/// </summary>
[BotModule("random-richard")]
public class RichardModule : IModule
{
    private RichardCommands? _richardCommands;
    private RichardTimerService? _timerService;

    /// <inheritdoc />
    public string Name => "Random Richard";

    /// <inheritdoc />
    public string Description => "Periodically DMs random server members with Wikipedia articles about famous people named Richard.";

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        // WikipediaService and RichardTimerService are created manually in InitializeAsync
        // with the IModuleContext, so no DI registrations are needed here.
    }

    /// <inheritdoc />
    public async Task InitializeAsync(IModuleContext context)
    {
        var logger = context.GetLogger<RichardModule>();

        // Create the Wikipedia service
        var httpClient = new HttpClient();
        var wikiLogger = context.GetLogger<WikipediaService>();
        var wikipediaService = new WikipediaService(httpClient, wikiLogger);

        // Create the timer service
        _timerService = new RichardTimerService(context, wikipediaService);

        // Create and register slash commands
        _richardCommands = new RichardCommands(context, wikipediaService, _timerService);
        context.Client.SlashCommandExecuted += _richardCommands.HandleSlashCommandAsync;
        context.Client.Ready += async () => await _richardCommands.RegisterCommandsAsync();

        // If the client is already connected (Ready already fired), register commands now
        if (context.Client.ConnectionState == Discord.ConnectionState.Connected)
        {
            await _richardCommands.RegisterCommandsAsync();
        }

        // Start the background timer
        _timerService.Start();

        logger.LogInformation("Random Richard module initialized.");
    }

    /// <inheritdoc />
    public async Task ShutdownAsync()
    {
        if (_timerService is not null)
        {
            await _timerService.StopAsync();
            _timerService = null;
        }

        _richardCommands = null;
    }
}
