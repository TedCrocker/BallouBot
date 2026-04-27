using BallouBot.Core.Entities;
using BallouBot.Data;
using BallouBot.Modules.Gif;
using Microsoft.EntityFrameworkCore;

namespace BallouBot.Modules.Gif.Tests;

/// <summary>
/// Tests for the GifCommands class, specifically the GetOrCreateConfigAsync method
/// and entity logic that can be tested without Discord socket mocks.
/// </summary>
public class GifCommandsTests
{
    private static DbContextOptions<BotDbContext> CreateInMemoryOptions(string dbName)
    {
        return new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(databaseName: $"GifCmd_{dbName}")
            .Options;
    }

    [Test]
    public async Task GetOrCreateConfigAsync_CreatesNewConfigWhenNoneExists()
    {
        var options = CreateInMemoryOptions(nameof(GetOrCreateConfigAsync_CreatesNewConfigWhenNoneExists));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 123456789;

        var config = await GifCommands.GetOrCreateConfigAsync(db, guildId);

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

        await GifCommands.GetOrCreateConfigAsync(db, guildId);

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
        var existing = new GifConfig
        {
            GuildId = guildId,
            Provider = "Giphy",
            PreviewCount = 8
        };
        db.GifConfigs.Add(existing);
        await db.SaveChangesAsync();

        var config = await GifCommands.GetOrCreateConfigAsync(db, guildId);

        await Assert.That(config.Provider).IsEqualTo("Giphy");
        await Assert.That(config.PreviewCount).IsEqualTo(8);
    }

    [Test]
    public async Task GetOrCreateConfigAsync_NewConfigHasCorrectDefaults()
    {
        var options = CreateInMemoryOptions(nameof(GetOrCreateConfigAsync_NewConfigHasCorrectDefaults));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 777888999;

        var config = await GifCommands.GetOrCreateConfigAsync(db, guildId);

        await Assert.That(config.IsEnabled).IsTrue();
        await Assert.That(config.Provider).IsEqualTo("Tenor");
        await Assert.That(config.PreviewCount).IsEqualTo(5);
        await Assert.That(config.ApiKey).IsNull();
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

        await GifCommands.GetOrCreateConfigAsync(db, guildId);

        var count = await db.GuildSettings.CountAsync(g => g.GuildId == guildId);
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task GetOrCreateConfigAsync_ConfigIsPersistedToDatabase()
    {
        var options = CreateInMemoryOptions(nameof(GetOrCreateConfigAsync_ConfigIsPersistedToDatabase));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 101010101;

        await GifCommands.GetOrCreateConfigAsync(db, guildId);

        var fromDb = await db.GifConfigs.FirstOrDefaultAsync(c => c.GuildId == guildId);
        await Assert.That(fromDb).IsNotNull();
    }

    [Test]
    public async Task GifConfig_CanUpdateProvider()
    {
        var options = CreateInMemoryOptions(nameof(GifConfig_CanUpdateProvider));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 202020202;

        var config = await GifCommands.GetOrCreateConfigAsync(db, guildId);
        config.Provider = "RedGifs";
        config.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var fromDb = await db.GifConfigs.FirstOrDefaultAsync(c => c.GuildId == guildId);
        await Assert.That(fromDb!.Provider).IsEqualTo("RedGifs");
    }

    [Test]
    public async Task GifConfig_CanUpdateApiKey()
    {
        var options = CreateInMemoryOptions(nameof(GifConfig_CanUpdateApiKey));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 303030303;

        var config = await GifCommands.GetOrCreateConfigAsync(db, guildId);
        config.ApiKey = "test-api-key-12345";
        await db.SaveChangesAsync();

        var fromDb = await db.GifConfigs.FirstOrDefaultAsync(c => c.GuildId == guildId);
        await Assert.That(fromDb!.ApiKey).IsEqualTo("test-api-key-12345");
    }

    [Test]
    public async Task GifConfig_CanUpdatePreviewCount()
    {
        var options = CreateInMemoryOptions(nameof(GifConfig_CanUpdatePreviewCount));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 404040404;

        var config = await GifCommands.GetOrCreateConfigAsync(db, guildId);
        config.PreviewCount = 10;
        await db.SaveChangesAsync();

        var fromDb = await db.GifConfigs.FirstOrDefaultAsync(c => c.GuildId == guildId);
        await Assert.That(fromDb!.PreviewCount).IsEqualTo(10);
    }
}
