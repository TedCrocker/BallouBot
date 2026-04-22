using BallouBot.Core.Entities;
using BallouBot.Data;
using BallouBot.Modules.Welcome;
using Microsoft.EntityFrameworkCore;

namespace BallouBot.Modules.Welcome.Tests;

/// <summary>
/// Tests for the WelcomeCommands class, specifically the GetOrCreateConfigAsync method
/// and command logic that can be tested without Discord socket mocks.
/// </summary>
public class WelcomeCommandsTests
{
    private static DbContextOptions<BotDbContext> CreateInMemoryOptions(string dbName)
    {
        return new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
    }

    [Test]
    public async Task GetOrCreateConfigAsync_CreatesNewConfigWhenNoneExists()
    {
        var options = CreateInMemoryOptions(nameof(GetOrCreateConfigAsync_CreatesNewConfigWhenNoneExists));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 123456789;

        var config = await WelcomeCommands.GetOrCreateConfigAsync(db, guildId);

        await Assert.That(config).IsNotNull();
        await Assert.That(config.GuildId).IsEqualTo(guildId);
    }

    [Test]
    public async Task GetOrCreateConfigAsync_CreatesGuildSettingsWhenNoneExists()
    {
        var options = CreateInMemoryOptions(nameof(GetOrCreateConfigAsync_CreatesGuildSettingsWhenNoneExists));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 987654321;

        await WelcomeCommands.GetOrCreateConfigAsync(db, guildId);

        var guildSettings = await db.GuildSettings.FirstOrDefaultAsync(g => g.GuildId == guildId);
        await Assert.That(guildSettings).IsNotNull();
    }

    [Test]
    public async Task GetOrCreateConfigAsync_ReturnsExistingConfig()
    {
        var options = CreateInMemoryOptions(nameof(GetOrCreateConfigAsync_ReturnsExistingConfig));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 111222333;
        var guildSettings = new GuildSettings { GuildId = guildId };
        db.GuildSettings.Add(guildSettings);
        var existing = new WelcomeConfig
        {
            GuildId = guildId,
            ChannelId = 999,
            Message = "Custom message"
        };
        db.WelcomeConfigs.Add(existing);
        await db.SaveChangesAsync();

        var config = await WelcomeCommands.GetOrCreateConfigAsync(db, guildId);

        await Assert.That(config.Message).IsEqualTo("Custom message");
        await Assert.That(config.ChannelId).IsEqualTo((ulong)999);
    }

    [Test]
    public async Task GetOrCreateConfigAsync_DoesNotDuplicateGuildSettings()
    {
        var options = CreateInMemoryOptions(nameof(GetOrCreateConfigAsync_DoesNotDuplicateGuildSettings));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 444555666;
        var guildSettings = new GuildSettings { GuildId = guildId, GuildName = "Existing" };
        db.GuildSettings.Add(guildSettings);
        await db.SaveChangesAsync();

        await WelcomeCommands.GetOrCreateConfigAsync(db, guildId);

        var count = await db.GuildSettings.CountAsync(g => g.GuildId == guildId);
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task GetOrCreateConfigAsync_NewConfigHasDefaultValues()
    {
        var options = CreateInMemoryOptions(nameof(GetOrCreateConfigAsync_NewConfigHasDefaultValues));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 777888999;

        var config = await WelcomeCommands.GetOrCreateConfigAsync(db, guildId);

        await Assert.That(config.IsEnabled).IsTrue();
        await Assert.That(config.UseEmbed).IsFalse();
        await Assert.That(config.EmbedColor).IsEqualTo("5865F2");
    }

    [Test]
    public async Task GetOrCreateConfigAsync_ConfigIsPersistedToDatabase()
    {
        var options = CreateInMemoryOptions(nameof(GetOrCreateConfigAsync_ConfigIsPersistedToDatabase));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 101010101;

        await WelcomeCommands.GetOrCreateConfigAsync(db, guildId);

        // Verify it's actually in the database
        var fromDb = await db.WelcomeConfigs.FirstOrDefaultAsync(w => w.GuildId == guildId);
        await Assert.That(fromDb).IsNotNull();
    }
}

/// <summary>
/// Tests for the WelcomeModule class metadata.
/// </summary>
public class WelcomeModuleTests
{
    [Test]
    public async Task WelcomeModule_HasCorrectName()
    {
        var module = new WelcomeModule();

        await Assert.That(module.Name).IsEqualTo("Welcome Messages");
    }

    [Test]
    public async Task WelcomeModule_HasVersion()
    {
        var module = new WelcomeModule();

        await Assert.That(module.Version).IsNotNull();
        await Assert.That(module.Version.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task WelcomeModule_HasDescription()
    {
        var module = new WelcomeModule();

        await Assert.That(module.Description).IsNotNull();
        await Assert.That(module.Description.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task WelcomeModule_HasBotModuleAttribute()
    {
        var attr = typeof(WelcomeModule)
            .GetCustomAttributes(typeof(BallouBot.Core.BotModuleAttribute), false)
            .FirstOrDefault() as BallouBot.Core.BotModuleAttribute;

        await Assert.That(attr).IsNotNull();
        await Assert.That(attr!.Id).IsEqualTo("welcome");
    }

    [Test]
    public async Task WelcomeModule_ImplementsIModule()
    {
        var module = new WelcomeModule();

        await Assert.That(module is BallouBot.Core.IModule).IsTrue();
    }

    [Test]
    public async Task WelcomeModule_ShutdownAsync_DoesNotThrow()
    {
        var module = new WelcomeModule();

        // Should not throw even if not initialized
        var exception = null as Exception;
        try
        {
            await module.ShutdownAsync();
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        await Assert.That(exception).IsNull();
    }
}
