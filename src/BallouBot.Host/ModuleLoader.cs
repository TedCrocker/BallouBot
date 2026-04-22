using System.Reflection;
using BallouBot.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BallouBot.Host;

/// <summary>
/// Discovers and loads BallouBot modules from referenced assemblies and a modules directory.
/// Modules are classes that implement <see cref="IModule"/> and are decorated with <see cref="BotModuleAttribute"/>.
/// </summary>
public class ModuleLoader
{
    private readonly ILogger<ModuleLoader> _logger;
    private readonly List<IModule> _modules = [];

    /// <summary>
    /// Gets the list of loaded modules.
    /// </summary>
    public IReadOnlyList<IModule> Modules => _modules.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleLoader"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ModuleLoader(ILogger<ModuleLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Discovers all module types from the given assemblies.
    /// A valid module must implement <see cref="IModule"/> and be decorated with <see cref="BotModuleAttribute"/>.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for modules.</param>
    /// <returns>A list of discovered module types.</returns>
    public List<Type> DiscoverModuleTypes(IEnumerable<Assembly> assemblies)
    {
        var moduleTypes = new List<Type>();

        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes()
                    .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(IModule).IsAssignableFrom(t)
                        && t.GetCustomAttribute<BotModuleAttribute>() != null);

                foreach (var type in types)
                {
                    var attr = type.GetCustomAttribute<BotModuleAttribute>()!;
                    _logger.LogInformation("Discovered module: {ModuleId} ({TypeName}) from {Assembly}",
                        attr.Id, type.FullName, assembly.GetName().Name);
                    moduleTypes.Add(type);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger.LogWarning(ex, "Failed to load types from assembly {Assembly}", assembly.GetName().Name);
            }
        }

        return moduleTypes;
    }

    /// <summary>
    /// Discovers module assemblies from a directory path.
    /// Loads all .dll files from the specified directory.
    /// </summary>
    /// <param name="modulesPath">The path to the modules directory.</param>
    /// <returns>A list of loaded assemblies.</returns>
    public List<Assembly> LoadAssembliesFromDirectory(string modulesPath)
    {
        var assemblies = new List<Assembly>();

        if (!Directory.Exists(modulesPath))
        {
            _logger.LogInformation("Modules directory does not exist: {Path}. Skipping external module loading.", modulesPath);
            return assemblies;
        }

        foreach (var dllPath in Directory.GetFiles(modulesPath, "*.dll"))
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllPath);
                assemblies.Add(assembly);
                _logger.LogDebug("Loaded assembly from modules directory: {Path}", dllPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load assembly: {Path}", dllPath);
            }
        }

        return assemblies;
    }

    /// <summary>
    /// Registers module services into the DI container.
    /// Each module's ConfigureServices method is called.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="moduleTypes">The module types to register.</param>
    public void RegisterModuleServices(IServiceCollection services, IEnumerable<Type> moduleTypes)
    {
        foreach (var moduleType in moduleTypes)
        {
            // Register the module type itself as a singleton
            services.AddSingleton(typeof(IModule), moduleType);

            // Create a temporary instance to call ConfigureServices
            var module = (IModule)Activator.CreateInstance(moduleType)!;
            module.ConfigureServices(services);

            _logger.LogInformation("Registered services for module: {ModuleName}", module.Name);
        }
    }

    /// <summary>
    /// Initializes all loaded modules with the provided context.
    /// </summary>
    /// <param name="context">The module context.</param>
    /// <param name="modules">The module instances resolved from DI.</param>
    public async Task InitializeModulesAsync(IModuleContext context, IEnumerable<IModule> modules)
    {
        foreach (var module in modules)
        {
            try
            {
                await module.InitializeAsync(context);
                _modules.Add(module);
                _logger.LogInformation("Initialized module: {ModuleName} v{Version}", module.Name, module.Version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize module: {ModuleName}", module.Name);
            }
        }
    }

    /// <summary>
    /// Shuts down all loaded modules gracefully.
    /// </summary>
    public async Task ShutdownModulesAsync()
    {
        foreach (var module in _modules)
        {
            try
            {
                await module.ShutdownAsync();
                _logger.LogInformation("Shut down module: {ModuleName}", module.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shutting down module: {ModuleName}", module.Name);
            }
        }

        _modules.Clear();
    }
}
