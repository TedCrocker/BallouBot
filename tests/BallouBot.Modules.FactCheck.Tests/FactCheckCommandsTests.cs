using BallouBot.Core.Entities;
using BallouBot.Data;
using BallouBot.Modules.FactCheck;
using Microsoft.EntityFrameworkCore;

namespace BallouBot.Modules.FactCheck.Tests;

/// <summary>
/// Tests for the FactCheckCommands class config management.
/// </summary>
public class FactCheckCommandsTests
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
        var config = await FactCheckCommands.GetOrCreateConfigAsync(db, guildId);

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
        await FactCheckCommands.GetOrCreateConfigAsync(db, guildId);

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
        var existing = new FactCheckConfig
        {
            GuildId = guildId,
            AiProvider = "Anthropic",
            Model = "claude-sonnet-4-20250514"
        };
        db.FactCheckConfigs.Add(existing);
        await db.SaveChangesAsync();

        var config = await FactCheckCommands.GetOrCreateConfigAsync(db, guildId);

        await Assert.That(config.AiProvider).IsEqualTo("Anthropic");
        await Assert.That(config.Model).IsEqualTo("claude-sonnet-4-20250514");
    }

    [Test]
    public async Task GetOrCreateConfigAsync_NewConfigHasCorrectDefaults()
    {
        var options = CreateInMemoryOptions(nameof(GetOrCreateConfigAsync_NewConfigHasCorrectDefaults));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 777888999;
        var config = await FactCheckCommands.GetOrCreateConfigAsync(db, guildId);

        await Assert.That(config.IsEnabled).IsFalse();
        await Assert.That(config.AiProvider).IsEqualTo("OpenAI");
        await Assert.That(config.Model).IsEqualTo("gpt-4o-mini");
        await Assert.That(config.CooldownSeconds).IsEqualTo(60);
        await Assert.That(config.MaxChecksPerHour).IsEqualTo(30);
        await Assert.That(config.MinMessageLength).IsEqualTo(20);
        await Assert.That(config.ApiKey).IsNull();
        await Assert.That(config.ChannelId).IsNull();
    }

    [Test]
    public async Task GetOrCreateConfigAsync_ConfigIsPersistedToDatabase()
    {
        var options = CreateInMemoryOptions(nameof(GetOrCreateConfigAsync_ConfigIsPersistedToDatabase));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 101010101;
        await FactCheckCommands.GetOrCreateConfigAsync(db, guildId);

        var fromDb = await db.FactCheckConfigs.FirstOrDefaultAsync(c => c.GuildId == guildId);
        await Assert.That(fromDb).IsNotNull();
    }

    [Test]
    public async Task FactCheckConfig_CanSetApiKey()
    {
        var options = CreateInMemoryOptions(nameof(FactCheckConfig_CanSetApiKey));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 202020202;
        var config = await FactCheckCommands.GetOrCreateConfigAsync(db, guildId);
        config.ApiKey = "sk-test-key-12345";
        await db.SaveChangesAsync();

        var fromDb = await db.FactCheckConfigs.FirstOrDefaultAsync(c => c.GuildId == guildId);
        await Assert.That(fromDb!.ApiKey).IsEqualTo("sk-test-key-12345");
    }

    [Test]
    public async Task FactCheckConfig_CanToggleEnabled()
    {
        var options = CreateInMemoryOptions(nameof(FactCheckConfig_CanToggleEnabled));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 303030303;
        var config = await FactCheckCommands.GetOrCreateConfigAsync(db, guildId);
        await Assert.That(config.IsEnabled).IsFalse();

        config.IsEnabled = true;
        await db.SaveChangesAsync();

        var fromDb = await db.FactCheckConfigs.FirstOrDefaultAsync(c => c.GuildId == guildId);
        await Assert.That(fromDb!.IsEnabled).IsTrue();
    }
}

/// <summary>
/// Tests for FactCheckUser (watched users) CRUD.
/// </summary>
public class FactCheckUserTests
{
    private static DbContextOptions<BotDbContext> CreateInMemoryOptions(string dbName)
    {
        return new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
    }

    [Test]
    public async Task CanAddWatchedUser()
    {
        var options = CreateInMemoryOptions(nameof(CanAddWatchedUser));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 111;
        db.GuildSettings.Add(new GuildSettings { GuildId = guildId });
        db.FactCheckConfigs.Add(new FactCheckConfig { GuildId = guildId });
        await db.SaveChangesAsync();

        db.FactCheckUsers.Add(new FactCheckUser { GuildId = guildId, UserId = 999 });
        await db.SaveChangesAsync();

        var user = await db.FactCheckUsers.FirstOrDefaultAsync(u => u.GuildId == guildId && u.UserId == 999);
        await Assert.That(user).IsNotNull();
    }

    [Test]
    public async Task CanRemoveWatchedUser()
    {
        var options = CreateInMemoryOptions(nameof(CanRemoveWatchedUser));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 222;
        db.GuildSettings.Add(new GuildSettings { GuildId = guildId });
        db.FactCheckConfigs.Add(new FactCheckConfig { GuildId = guildId });
        var entry = new FactCheckUser { GuildId = guildId, UserId = 888 };
        db.FactCheckUsers.Add(entry);
        await db.SaveChangesAsync();

        db.FactCheckUsers.Remove(entry);
        await db.SaveChangesAsync();

        var fromDb = await db.FactCheckUsers.FirstOrDefaultAsync(u => u.GuildId == guildId && u.UserId == 888);
        await Assert.That(fromDb).IsNull();
    }

    [Test]
    public async Task CanQueryWatchedUsersForGuild()
    {
        var options = CreateInMemoryOptions(nameof(CanQueryWatchedUsersForGuild));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 333;
        db.GuildSettings.Add(new GuildSettings { GuildId = guildId });
        db.FactCheckConfigs.Add(new FactCheckConfig { GuildId = guildId });
        db.FactCheckUsers.AddRange(
            new FactCheckUser { GuildId = guildId, UserId = 1 },
            new FactCheckUser { GuildId = guildId, UserId = 2 },
            new FactCheckUser { GuildId = guildId, UserId = 3 }
        );
        await db.SaveChangesAsync();

        var users = await db.FactCheckUsers.Where(u => u.GuildId == guildId).ToListAsync();
        await Assert.That(users.Count).IsEqualTo(3);
    }
}
