using BallouBot.Core.Entities;
using BallouBot.Data;
using BallouBot.Modules.RandomRichard;
using Microsoft.EntityFrameworkCore;

namespace BallouBot.Modules.RandomRichard.Tests;

/// <summary>
/// Tests for the RichardCommands class, specifically the GetOrCreateConfigAsync method
/// and entity logic that can be tested without Discord socket mocks.
/// </summary>
public class RichardCommandsTests
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

        var config = await RichardCommands.GetOrCreateConfigAsync(db, guildId);

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

        await RichardCommands.GetOrCreateConfigAsync(db, guildId);

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
        var existing = new RichardConfig
        {
            GuildId = guildId,
            MinIntervalMinutes = 120,
            MaxIntervalMinutes = 240
        };
        db.RichardConfigs.Add(existing);
        await db.SaveChangesAsync();

        var config = await RichardCommands.GetOrCreateConfigAsync(db, guildId);

        await Assert.That(config.MinIntervalMinutes).IsEqualTo(120);
        await Assert.That(config.MaxIntervalMinutes).IsEqualTo(240);
    }

    [Test]
    public async Task GetOrCreateConfigAsync_NewConfigHasCorrectDefaults()
    {
        var options = CreateInMemoryOptions(nameof(GetOrCreateConfigAsync_NewConfigHasCorrectDefaults));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 777888999;

        var config = await RichardCommands.GetOrCreateConfigAsync(db, guildId);

        await Assert.That(config.IsEnabled).IsFalse();
        await Assert.That(config.UseWhitelistMode).IsTrue();
        await Assert.That(config.MinIntervalMinutes).IsEqualTo(480);
        await Assert.That(config.MaxIntervalMinutes).IsEqualTo(480);
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

        await RichardCommands.GetOrCreateConfigAsync(db, guildId);

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

        await RichardCommands.GetOrCreateConfigAsync(db, guildId);

        var fromDb = await db.RichardConfigs.FirstOrDefaultAsync(c => c.GuildId == guildId);
        await Assert.That(fromDb).IsNotNull();
    }
}

/// <summary>
/// Tests for whitelist/blacklist user entry logic.
/// </summary>
public class RichardUserEntryTests
{
    private static DbContextOptions<BotDbContext> CreateInMemoryOptions(string dbName)
    {
        return new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
    }

    [Test]
    public async Task CanAddWhitelistEntry()
    {
        var options = CreateInMemoryOptions(nameof(CanAddWhitelistEntry));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 111;
        var guildSettings = new GuildSettings { GuildId = guildId };
        db.GuildSettings.Add(guildSettings);
        db.RichardConfigs.Add(new RichardConfig { GuildId = guildId });
        await db.SaveChangesAsync();

        db.RichardUserEntries.Add(new RichardUserEntry
        {
            GuildId = guildId,
            UserId = 999,
            ListType = RichardListType.Whitelist
        });
        await db.SaveChangesAsync();

        var entry = await db.RichardUserEntries
            .FirstOrDefaultAsync(e => e.GuildId == guildId && e.UserId == 999);

        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.ListType).IsEqualTo(RichardListType.Whitelist);
    }

    [Test]
    public async Task CanAddBlacklistEntry()
    {
        var options = CreateInMemoryOptions(nameof(CanAddBlacklistEntry));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 222;
        var guildSettings = new GuildSettings { GuildId = guildId };
        db.GuildSettings.Add(guildSettings);
        db.RichardConfigs.Add(new RichardConfig { GuildId = guildId });
        await db.SaveChangesAsync();

        db.RichardUserEntries.Add(new RichardUserEntry
        {
            GuildId = guildId,
            UserId = 888,
            ListType = RichardListType.Blacklist
        });
        await db.SaveChangesAsync();

        var entry = await db.RichardUserEntries
            .FirstOrDefaultAsync(e => e.GuildId == guildId && e.UserId == 888);

        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.ListType).IsEqualTo(RichardListType.Blacklist);
    }

    [Test]
    public async Task CanRemoveUserEntry()
    {
        var options = CreateInMemoryOptions(nameof(CanRemoveUserEntry));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 333;
        var guildSettings = new GuildSettings { GuildId = guildId };
        db.GuildSettings.Add(guildSettings);
        db.RichardConfigs.Add(new RichardConfig { GuildId = guildId });
        var entry = new RichardUserEntry
        {
            GuildId = guildId,
            UserId = 777,
            ListType = RichardListType.Whitelist
        };
        db.RichardUserEntries.Add(entry);
        await db.SaveChangesAsync();

        db.RichardUserEntries.Remove(entry);
        await db.SaveChangesAsync();

        var fromDb = await db.RichardUserEntries
            .FirstOrDefaultAsync(e => e.GuildId == guildId && e.UserId == 777);

        await Assert.That(fromDb).IsNull();
    }

    [Test]
    public async Task CanChangeEntryListType()
    {
        var options = CreateInMemoryOptions(nameof(CanChangeEntryListType));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 444;
        var guildSettings = new GuildSettings { GuildId = guildId };
        db.GuildSettings.Add(guildSettings);
        db.RichardConfigs.Add(new RichardConfig { GuildId = guildId });
        var entry = new RichardUserEntry
        {
            GuildId = guildId,
            UserId = 666,
            ListType = RichardListType.Whitelist
        };
        db.RichardUserEntries.Add(entry);
        await db.SaveChangesAsync();

        entry.ListType = RichardListType.Blacklist;
        await db.SaveChangesAsync();

        var fromDb = await db.RichardUserEntries
            .FirstOrDefaultAsync(e => e.GuildId == guildId && e.UserId == 666);

        await Assert.That(fromDb).IsNotNull();
        await Assert.That(fromDb!.ListType).IsEqualTo(RichardListType.Blacklist);
    }

    [Test]
    public async Task CanQueryWhitelistedUsersForGuild()
    {
        var options = CreateInMemoryOptions(nameof(CanQueryWhitelistedUsersForGuild));
        using var db = new BotDbContext(options);
        await db.Database.EnsureCreatedAsync();

        ulong guildId = 555;
        var guildSettings = new GuildSettings { GuildId = guildId };
        db.GuildSettings.Add(guildSettings);
        db.RichardConfigs.Add(new RichardConfig { GuildId = guildId });
        db.RichardUserEntries.AddRange(
            new RichardUserEntry { GuildId = guildId, UserId = 1, ListType = RichardListType.Whitelist },
            new RichardUserEntry { GuildId = guildId, UserId = 2, ListType = RichardListType.Whitelist },
            new RichardUserEntry { GuildId = guildId, UserId = 3, ListType = RichardListType.Blacklist }
        );
        await db.SaveChangesAsync();

        var whitelisted = await db.RichardUserEntries
            .Where(e => e.GuildId == guildId && e.ListType == RichardListType.Whitelist)
            .ToListAsync();

        await Assert.That(whitelisted.Count).IsEqualTo(2);
    }
}

/// <summary>
/// Tests for the RichardModule class metadata.
/// </summary>
public class RichardModuleTests
{
    [Test]
    public async Task RichardModule_HasCorrectName()
    {
        var module = new RichardModule();

        await Assert.That(module.Name).IsEqualTo("Random Richard");
    }

    [Test]
    public async Task RichardModule_HasVersion()
    {
        var module = new RichardModule();

        await Assert.That(module.Version).IsNotNull();
        await Assert.That(module.Version.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task RichardModule_HasDescription()
    {
        var module = new RichardModule();

        await Assert.That(module.Description).IsNotNull();
        await Assert.That(module.Description.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task RichardModule_HasBotModuleAttribute()
    {
        var attr = typeof(RichardModule)
            .GetCustomAttributes(typeof(BallouBot.Core.BotModuleAttribute), false)
            .FirstOrDefault() as BallouBot.Core.BotModuleAttribute;

        await Assert.That(attr).IsNotNull();
        await Assert.That(attr!.Id).IsEqualTo("random-richard");
    }

    [Test]
    public async Task RichardModule_ImplementsIModule()
    {
        var module = new RichardModule();

        await Assert.That(module is BallouBot.Core.IModule).IsTrue();
    }

    [Test]
    public async Task RichardModule_ShutdownAsync_DoesNotThrow()
    {
        var module = new RichardModule();

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

/// <summary>
/// Tests for the WikipediaService class (static methods and list).
/// </summary>
public class WikipediaServiceTests
{
    [Test]
    public async Task FamousRichardsList_IsNotEmpty()
    {
        var richards = Services.WikipediaService.GetFamousRichardsList();

        await Assert.That(richards.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task FamousRichardsList_ContainsRichardFeynman()
    {
        var richards = Services.WikipediaService.GetFamousRichardsList();

        await Assert.That(richards.Contains("Richard_Feynman")).IsTrue();
    }

    [Test]
    public async Task FamousRichardsList_ContainsRichardNixon()
    {
        var richards = Services.WikipediaService.GetFamousRichardsList();

        await Assert.That(richards.Contains("Richard_Nixon")).IsTrue();
    }

    [Test]
    public async Task FamousRichardsList_AllEntriesContainRichard()
    {
        var richards = Services.WikipediaService.GetFamousRichardsList();

        foreach (var richard in richards)
        {
            // All entries should contain "Richard" or "richard" (case-insensitive)
            // Exception: Little_Richard
            var containsRichard = richard.Contains("Richard", StringComparison.OrdinalIgnoreCase) ||
                                  richard.Contains("Little_Richard", StringComparison.OrdinalIgnoreCase);
            await Assert.That(containsRichard).IsTrue();
        }
    }

    [Test]
    public async Task FamousRichardsList_HasNoDuplicates()
    {
        var richards = Services.WikipediaService.GetFamousRichardsList();
        var distinct = richards.Distinct().ToList();

        await Assert.That(richards.Count).IsEqualTo(distinct.Count);
    }
}

/// <summary>
/// Tests for RichardInfo model.
/// </summary>
public class RichardInfoTests
{
    [Test]
    public async Task RichardInfo_DefaultValues()
    {
        var info = new Models.RichardInfo();

        await Assert.That(info.Name).IsEqualTo(string.Empty);
        await Assert.That(info.Summary).IsEqualTo(string.Empty);
        await Assert.That(info.ImageUrl).IsNull();
        await Assert.That(info.WikipediaUrl).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task RichardInfo_CanSetProperties()
    {
        var info = new Models.RichardInfo
        {
            Name = "Richard Feynman",
            Summary = "American theoretical physicist.",
            ImageUrl = "https://example.com/feynman.jpg",
            WikipediaUrl = "https://en.wikipedia.org/wiki/Richard_Feynman"
        };

        await Assert.That(info.Name).IsEqualTo("Richard Feynman");
        await Assert.That(info.Summary).IsEqualTo("American theoretical physicist.");
        await Assert.That(info.ImageUrl).IsEqualTo("https://example.com/feynman.jpg");
        await Assert.That(info.WikipediaUrl).IsEqualTo("https://en.wikipedia.org/wiki/Richard_Feynman");
    }
}
