using BallouBot.Core;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace BallouBot.Host;

/// <summary>
/// Default implementation of <see cref="IModuleContext"/>.
/// Provides modules with access to the Discord client, services, and logging.
/// </summary>
public class ModuleContext : IModuleContext
{
    private readonly ILoggerFactory _loggerFactory;

    /// <inheritdoc />
    public DiscordSocketClient Client { get; }

    /// <inheritdoc />
    public IServiceProvider Services { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleContext"/> class.
    /// </summary>
    /// <param name="client">The Discord socket client.</param>
    /// <param name="services">The application service provider.</param>
    /// <param name="loggerFactory">The logger factory for creating loggers.</param>
    public ModuleContext(DiscordSocketClient client, IServiceProvider services, ILoggerFactory loggerFactory)
    {
        Client = client;
        Services = services;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public ILogger<T> GetLogger<T>()
    {
        return _loggerFactory.CreateLogger<T>();
    }
}
