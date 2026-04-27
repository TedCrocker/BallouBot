using BallouBot.Core;
using BallouBot.Modules.Gif.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.Gif;

/// <summary>
/// GIF module — allows users to search for GIFs from configurable providers
/// (Tenor, Giphy, RedGifs), preview results interactively, and post their
/// selection to the channel. Supports per-guild provider configuration and API keys.
/// </summary>
[BotModule("gif")]
public class GifModule : IModule
{
    private GifCommands? _gifCommands;

    /// <inheritdoc />
    public string Name => "GIF";

    /// <inheritdoc />
    public string Description => "Search for GIFs from multiple providers, preview results, and post them to the channel.";

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        // GifProviderFactory and GifCommands are created manually in InitializeAsync
        // with the IModuleContext, so no DI registrations are needed here.
    }

    /// <inheritdoc />
    public async Task InitializeAsync(IModuleContext context)
    {
        var logger = context.GetLogger<GifModule>();

        // Create the provider factory with a shared HttpClient
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("BallouBot/1.0 (Discord Bot; GIF Module)");

        var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
        var providerFactory = new GifProviderFactory(httpClient, loggerFactory);

        // Create and register slash commands + button handler
        _gifCommands = new GifCommands(context, providerFactory);
        context.Client.SlashCommandExecuted += _gifCommands.HandleSlashCommandAsync;
        context.Client.ButtonExecuted += _gifCommands.HandleButtonAsync;
        context.Client.Ready += async () => await _gifCommands.RegisterCommandsAsync();

        // If the client is already connected (Ready already fired), register commands now
        if (context.Client.ConnectionState == Discord.ConnectionState.Connected)
        {
            await _gifCommands.RegisterCommandsAsync();
        }

        logger.LogInformation("GIF module initialized.");
    }

    /// <inheritdoc />
    public Task ShutdownAsync()
    {
        _gifCommands = null;
        return Task.CompletedTask;
    }
}
