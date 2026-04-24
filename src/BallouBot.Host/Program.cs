using System.Reflection;
using BallouBot.Core;
using BallouBot.Data;
using BallouBot.Host;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/balloubot-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting BallouBot...");

    var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

    // Replace default logging with Serilog
    builder.Services.AddSerilog();

    // Configure Discord socket client
    var discordConfig = new DiscordSocketConfig
    {
        GatewayIntents = GatewayIntents.Guilds
            | GatewayIntents.GuildMembers
            | GatewayIntents.GuildMessages
            | GatewayIntents.MessageContent,
        LogLevel = LogSeverity.Info,
        AlwaysDownloadUsers = true
    };
    builder.Services.AddSingleton(discordConfig);
    builder.Services.AddSingleton<DiscordSocketClient>(sp =>
        new DiscordSocketClient(sp.GetRequiredService<DiscordSocketConfig>()));

    // Configure Entity Framework Core with SQLite
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=balloubot.db";
    builder.Services.AddDbContext<BotDbContext>(options =>
        options.UseSqlite(connectionString));

    // Register module infrastructure
    builder.Services.AddSingleton<ModuleLoader>();

    // Discover and register modules
    // First, scan referenced assemblies (compiled-in modules)
    var moduleLoader = new ModuleLoader(
        LoggerFactory.Create(lb => lb.AddSerilog()).CreateLogger<ModuleLoader>());

    var assemblies = new List<Assembly>();

    // Scan the output directory for compiled-in module assemblies (BallouBot.Modules.*.dll)
    var baseDir = AppContext.BaseDirectory;
    foreach (var dllPath in Directory.GetFiles(baseDir, "BallouBot.Modules.*.dll"))
    {
        try
        {
            var assembly = Assembly.LoadFrom(dllPath);
            assemblies.Add(assembly);
        }
        catch
        {
            // Skip assemblies that fail to load
        }
    }

    // Also scan a "modules" directory for drop-in modules
    var modulesPath = Path.Combine(baseDir, "modules");
    assemblies.AddRange(moduleLoader.LoadAssembliesFromDirectory(modulesPath));

    // Discover module types and register their services
    var moduleTypes = moduleLoader.DiscoverModuleTypes(assemblies);
    moduleLoader.RegisterModuleServices(builder.Services, moduleTypes);

    // Register the hosted service that runs the bot
    builder.Services.AddHostedService<BotHostedService>();

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "BallouBot terminated unexpectedly.");
}
finally
{
    await Log.CloseAndFlushAsync();
}
