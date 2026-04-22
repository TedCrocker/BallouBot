using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace BallouBot.Core;

/// <summary>
/// Defines the contract for a BallouBot module.
/// All modules must implement this interface to be discovered and loaded by the host.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Gets the unique name of the module.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a description of the module's functionality.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the version of the module.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Registers services required by this module into the DI container.
    /// Called during application startup before the bot connects.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Initializes the module after all services have been configured.
    /// Use this to register event handlers, slash commands, etc.
    /// </summary>
    /// <param name="context">The module context providing access to the Discord client and services.</param>
    Task InitializeAsync(IModuleContext context);

    /// <summary>
    /// Performs cleanup when the module is being unloaded.
    /// </summary>
    Task ShutdownAsync();
}
