using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace BallouBot.Core;

/// <summary>
/// Provides the runtime context for a module, including access to the Discord client,
/// service provider, and logging.
/// </summary>
public interface IModuleContext
{
    /// <summary>
    /// Gets the Discord socket client instance.
    /// </summary>
    DiscordSocketClient Client { get; }

    /// <summary>
    /// Gets the application's service provider for resolving dependencies.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Creates a logger for the specified type.
    /// </summary>
    ILogger<T> GetLogger<T>();
}
