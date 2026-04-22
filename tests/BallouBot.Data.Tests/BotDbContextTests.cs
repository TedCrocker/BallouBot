using BallouBot.Core.Entities;
using BallouBot.Data;
using Microsoft.EntityFrameworkCore;

namespace BallouBot.Data.Tests;

/// <summary>
/// Tests for BotDbContext schema configuration and entity relationships.
/// </summary>
public class BotDbContextTests
{
    private static DbContextOptions<BotDbContext> CreateInMemoryOptions(string dbName)
    {
        return new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
    }

    [Test]
    public async Task CanCreateDatabase()
    {
        var options = CreateInMemoryOptions(nameof(CanCreateDatabase));
        using var db = new BotDbContext(options);

        var created = await db.Database.EnsureCreatedAsync();

        await Assert.That(created).IsTrue();
    }

    [Test]
    public async Task CanAddAndRetrieveGuildSettings()
    {
        var options = CreateInMemoryOptions(nameof(CanAddAndRetrieveGuildSettings));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var settings = new GuildSettings
        {
            GuildId = 123456789,
            GuildName = "Test Server",
            Prefix = "?"
        };
        db.GuildSettings.Add(settings);
        await db.SaveChangesAsync();

        var retrieved = await db.GuildSettings.FirstOrDefaultAsync(g => g.GuildId == 123456789);

        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.GuildName).IsEqualTo("Test Server");
        await Assert.That(retrieved.Prefix).IsEqualTo("?");
    }

    [Test]
    public async Task CanAddAndRetrieveWelcomeConfig()
    {
        var options = CreateInMemoryOptions(nameof(CanAddAndRetrieveWelcomeConfig));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var guildSettings = new GuildSettings { GuildId = 111222333 };
        db.GuildSettings.Add(guildSettings);
        await db.SaveChangesAsync();

        var config = new WelcomeConfig
        {
            GuildId = 111222333,
            ChannelId = 999888777,
            Message = "Hello {user}!",
            IsEnabled = true,
            UseEmbed = true,
            EmbedColor = "FF5733",
            EmbedTitle = "Greetings!"
        };
        db.WelcomeConfigs.Add(config);
        await db.SaveChangesAsync();

        var retrieved = await db.WelcomeConfigs.FirstOrDefaultAsync(w => w.GuildId == 111222333);

        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.ChannelId).IsEqualTo((ulong)999888777);
        await Assert.That(retrieved.Message).IsEqualTo("Hello {user}!");
        await Assert.That(retrieved.IsEnabled).IsTrue();
        await Assert.That(retrieved.UseEmbed).IsTrue();
        await Assert.That(retrieved.EmbedColor).IsEqualTo("FF5733");
        await Assert.That(retrieved.EmbedTitle).IsEqualTo("Greetings!");
    }

    [Test]
    public async Task GuildSettings_GuildId_IsUnique()
    {
        var options = CreateInMemoryOptions(nameof(GuildSettings_GuildId_IsUnique));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        // Verify the unique index is configured on the model
        var model = db.Model;
        var guildSettingsEntity = model.FindEntityType(typeof(GuildSettings))!;
        var index = guildSettingsEntity.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(GuildSettings.GuildId)));

        await Assert.That(index).IsNotNull();
        await Assert.That(index!.IsUnique).IsTrue();
    }

    [Test]
    public async Task WelcomeConfig_GuildId_IsUnique()
    {
        var options = CreateInMemoryOptions(nameof(WelcomeConfig_GuildId_IsUnique));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var model = db.Model;
        var welcomeConfigEntity = model.FindEntityType(typeof(WelcomeConfig))!;
        var index = welcomeConfigEntity.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(WelcomeConfig.GuildId)));

        await Assert.That(index).IsNotNull();
        await Assert.That(index!.IsUnique).IsTrue();
    }

    [Test]
    public async Task WelcomeConfig_HasRelationshipToGuildSettings()
    {
        var options = CreateInMemoryOptions(nameof(WelcomeConfig_HasRelationshipToGuildSettings));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var model = db.Model;
        var welcomeConfigEntity = model.FindEntityType(typeof(WelcomeConfig))!;
        var navigation = welcomeConfigEntity.GetNavigations()
            .FirstOrDefault(n => n.Name == nameof(WelcomeConfig.GuildSettings));

        await Assert.That(navigation).IsNotNull();
    }

    [Test]
    public async Task CanUpdateWelcomeConfig()
    {
        var options = CreateInMemoryOptions(nameof(CanUpdateWelcomeConfig));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var guildSettings = new GuildSettings { GuildId = 555666777 };
        db.GuildSettings.Add(guildSettings);

        var config = new WelcomeConfig
        {
            GuildId = 555666777,
            ChannelId = 111,
            Message = "Original message"
        };
        db.WelcomeConfigs.Add(config);
        await db.SaveChangesAsync();

        config.Message = "Updated message";
        config.ChannelId = 222;
        await db.SaveChangesAsync();

        var retrieved = await db.WelcomeConfigs.FirstAsync(w => w.GuildId == 555666777);

        await Assert.That(retrieved.Message).IsEqualTo("Updated message");
        await Assert.That(retrieved.ChannelId).IsEqualTo((ulong)222);
    }

    [Test]
    public async Task CanDeleteWelcomeConfig()
    {
        var options = CreateInMemoryOptions(nameof(CanDeleteWelcomeConfig));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var guildSettings = new GuildSettings { GuildId = 888999000 };
        db.GuildSettings.Add(guildSettings);

        var config = new WelcomeConfig { GuildId = 888999000, ChannelId = 111 };
        db.WelcomeConfigs.Add(config);
        await db.SaveChangesAsync();

        db.WelcomeConfigs.Remove(config);
        await db.SaveChangesAsync();

        var retrieved = await db.WelcomeConfigs.FirstOrDefaultAsync(w => w.GuildId == 888999000);

        await Assert.That(retrieved).IsNull();
    }
}
