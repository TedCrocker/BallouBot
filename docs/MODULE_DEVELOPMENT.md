# BallouBot Module Development Guide

This guide explains how to create new modules for BallouBot.

---

## Module Architecture

BallouBot uses a modular plugin architecture. Each module is a .NET class library that:

1. References `BallouBot.Core`
2. Implements the `IModule` interface
3. Is decorated with the `[BotModule]` attribute
4. Optionally references `BallouBot.Data` for database access

Modules are discovered automatically at startup from:
- **Referenced assemblies** (compiled into the project)
- **A `modules/` directory** (drop-in DLLs)

---

## Quick Start: Creating a New Module

### 1. Create the Project

```bash
cd src/modules
dotnet new classlib -n BallouBot.Modules.MyModule --framework net10.0
cd ../../
dotnet sln add src/modules/BallouBot.Modules.MyModule/BallouBot.Modules.MyModule.csproj
```

### 2. Add Project References

Edit the `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Discord.Net" Version="3.19.1" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\BallouBot.Core\BallouBot.Core.csproj" />
    <ProjectReference Include="..\..\BallouBot.Data\BallouBot.Data.csproj" />
  </ItemGroup>
</Project>
```

### 3. Implement the Module

```csharp
using BallouBot.Core;
using Microsoft.Extensions.DependencyInjection;

namespace BallouBot.Modules.MyModule;

[BotModule("my-module")]
public class MyModule : IModule
{
    public string Name => "My Module";
    public string Description => "A description of what this module does.";
    public string Version => "1.0.0";

    public void ConfigureServices(IServiceCollection services)
    {
        // Register any services your module needs
        // services.AddScoped<MyService>();
    }

    public async Task InitializeAsync(IModuleContext context)
    {
        var logger = context.GetLogger<MyModule>();

        // Register event handlers
        context.Client.MessageReceived += async (message) =>
        {
            // Handle messages
        };

        // Register slash commands
        context.Client.SlashCommandExecuted += async (command) =>
        {
            if (command.CommandName == "mycommand")
            {
                await command.RespondAsync("Hello from MyModule!");
            }
        };

        // Register slash commands with Discord
        // (Best done in a Ready handler or check if already connected)
        context.Client.Ready += async () =>
        {
            var cmd = new Discord.SlashCommandBuilder()
                .WithName("mycommand")
                .WithDescription("My custom command");
            await context.Client.CreateGlobalApplicationCommandAsync(cmd.Build());
        };

        logger.LogInformation("MyModule initialized.");
    }

    public Task ShutdownAsync()
    {
        // Cleanup resources if needed
        return Task.CompletedTask;
    }
}
```

### 4. Reference from Host (Compiled Module)

Add a project reference in `src/BallouBot.Host/BallouBot.Host.csproj`:

```xml
<ProjectReference Include="..\modules\BallouBot.Modules.MyModule\BallouBot.Modules.MyModule.csproj" />
```

### 5. Or Deploy as Drop-in DLL

Build the module and copy the DLL to the `modules/` directory next to the host executable:

```bash
dotnet build src/modules/BallouBot.Modules.MyModule -c Release
cp src/modules/BallouBot.Modules.MyModule/bin/Release/net10.0/BallouBot.Modules.MyModule.dll <host-output>/modules/
```

---

## The IModule Interface

```csharp
public interface IModule
{
    /// The unique name of the module.
    string Name { get; }

    /// A description of the module's functionality.
    string Description { get; }

    /// The version of the module.
    string Version { get; }

    /// Register services into the DI container (called at startup).
    void ConfigureServices(IServiceCollection services);

    /// Initialize the module (called after bot connects to Discord).
    Task InitializeAsync(IModuleContext context);

    /// Cleanup when the module is unloaded.
    Task ShutdownAsync();
}
```

---

## The IModuleContext Interface

The `IModuleContext` gives your module everything it needs:

```csharp
public interface IModuleContext
{
    /// The Discord socket client.
    DiscordSocketClient Client { get; }

    /// The application service provider (for resolving DI services).
    IServiceProvider Services { get; }

    /// Create a logger for your type.
    ILogger<T> GetLogger<T>();
}
```

### Accessing the Database

```csharp
public async Task InitializeAsync(IModuleContext context)
{
    context.Client.SlashCommandExecuted += async (command) =>
    {
        // Always create a scope for DbContext access
        using var scope = context.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

        // Use db as normal...
        var settings = await db.GuildSettings
            .FirstOrDefaultAsync(g => g.GuildId == command.GuildId!.Value);
    };
}
```

> ⚠️ **Important:** Always use `CreateScope()` when accessing `BotDbContext` from event handlers. The DbContext is registered as scoped, so you need a scope for each operation.

---

## Adding Entities to the Database

If your module needs custom database tables:

1. Add your entity class to `BallouBot.Core/Entities/`:

```csharp
namespace BallouBot.Core.Entities;

public class MyModuleData
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public string Setting { get; set; } = string.Empty;
}
```

2. Add a `DbSet` to `BotDbContext`:

```csharp
public DbSet<MyModuleData> MyModuleData => Set<MyModuleData>();
```

3. Configure the entity in `OnModelCreating` if needed.

4. Create a migration:

```bash
cd src/BallouBot.Data
dotnet ef migrations add AddMyModuleData
```

---

## The BotModuleAttribute

Every module class must be decorated with `[BotModule("id")]`:

```csharp
[BotModule("my-module")]           // Simple ID
[BotModule("my-module", EnabledByDefault = false)]  // Disabled by default
```

The `Id` must be unique across all modules and is used for configuration and identification.

---

## Best Practices

1. **Use scoped DbContext** — Always create a scope when accessing the database from event handlers.
2. **Handle exceptions** — Wrap event handler logic in try/catch to prevent one failure from crashing the bot.
3. **Use ephemeral responses** — For configuration commands, use `ephemeral: true` so only the command user sees the response.
4. **Log meaningful messages** — Use the provided logger with structured logging parameters.
5. **Respect rate limits** — Discord.Net handles rate limits automatically, but avoid unnecessary API calls.
6. **Keep modules focused** — Each module should handle one feature or a closely related set of features.
7. **Write tests** — Use TUnit with EF Core InMemory provider and Moq for Discord.Net interfaces.

---

## Testing Your Module

Create a test project:

```bash
cd tests
dotnet new classlib -n BallouBot.Modules.MyModule.Tests --framework net10.0
```

Add references:

```xml
<PackageReference Include="TUnit" Version="1.22.6" />
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />

<ProjectReference Include="..\..\src\BallouBot.Core\BallouBot.Core.csproj" />
<ProjectReference Include="..\..\src\BallouBot.Data\BallouBot.Data.csproj" />
<ProjectReference Include="..\..\src\modules\BallouBot.Modules.MyModule\BallouBot.Modules.MyModule.csproj" />
```

Write tests using TUnit:

```csharp
using BallouBot.Data;
using Microsoft.EntityFrameworkCore;

public class MyModuleTests
{
    [Test]
    public async Task MyFeature_DoesExpectedThing()
    {
        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        // Arrange, Act, Assert...
        await Assert.That(true).IsTrue();
    }
}
```

Run tests:

```bash
dotnet test
```

---

## Module Lifecycle

```
Application Start
    ├── Module Discovery (scan assemblies + modules/ directory)
    ├── ConfigureServices() called for each module
    ├── DI Container built
    ├── Bot connects to Discord
    ├── Client Ready event fires
    │   └── InitializeAsync() called for each module
    │       ├── Register event handlers
    │       ├── Register slash commands
    │       └── Setup complete
    ├── Bot is running, handling events...
    │
Application Shutdown
    └── ShutdownAsync() called for each module
        └── Cleanup resources
```
