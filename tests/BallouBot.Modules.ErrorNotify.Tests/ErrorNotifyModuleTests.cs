using BallouBot.Core.Entities;
using BallouBot.Data;
using BallouBot.Modules.ErrorNotify;
using Discord;
using Microsoft.EntityFrameworkCore;

namespace BallouBot.Modules.ErrorNotify.Tests;

/// <summary>
/// Tests for the ErrorNotifyModule class metadata.
/// </summary>
public class ErrorNotifyModuleTests
{
    [Test]
    public async Task ErrorNotifyModule_HasCorrectName()
    {
        var module = new ErrorNotifyModule();
        await Assert.That(module.Name).IsEqualTo("Error Notify");
    }

    [Test]
    public async Task ErrorNotifyModule_HasVersion()
    {
        var module = new ErrorNotifyModule();
        await Assert.That(module.Version).IsNotNull();
        await Assert.That(module.Version.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task ErrorNotifyModule_HasDescription()
    {
        var module = new ErrorNotifyModule();
        await Assert.That(module.Description).IsNotNull();
        await Assert.That(module.Description.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task ErrorNotifyModule_HasBotModuleAttribute()
    {
        var attr = typeof(ErrorNotifyModule)
            .GetCustomAttributes(typeof(BallouBot.Core.BotModuleAttribute), false)
            .FirstOrDefault() as BallouBot.Core.BotModuleAttribute;

        await Assert.That(attr).IsNotNull();
        await Assert.That(attr!.Id).IsEqualTo("errornotify");
    }

    [Test]
    public async Task ErrorNotifyModule_ImplementsIModule()
    {
        var module = new ErrorNotifyModule();
        await Assert.That(module is BallouBot.Core.IModule).IsTrue();
    }

    [Test]
    public async Task ErrorNotifyModule_ShutdownAsync_DoesNotThrow()
    {
        var module = new ErrorNotifyModule();
        Exception? exception = null;
        try { await module.ShutdownAsync(); }
        catch (Exception ex) { exception = ex; }
        await Assert.That(exception).IsNull();
    }
}

/// <summary>
/// Tests for the ErrorNotificationService.BuildErrorEmbed method.
/// </summary>
public class ErrorNotificationServiceTests
{
    [Test]
    public async Task BuildErrorEmbed_BasicError_HasCorrectTitle()
    {
        var embed = ErrorNotificationService.BuildErrorEmbed("TestSource", "Test error message", null, null);

        await Assert.That(embed.Title).IsEqualTo("🚨 BallouBot Error");
    }

    [Test]
    public async Task BuildErrorEmbed_BasicError_HasSourceField()
    {
        var embed = ErrorNotificationService.BuildErrorEmbed("TestSource", "Test error message", null, null);

        var sourceField = embed.Fields.FirstOrDefault(f => f.Name == "Source");
        await Assert.That(sourceField.Value).IsEqualTo("TestSource");
    }

    [Test]
    public async Task BuildErrorEmbed_BasicError_HasMessageField()
    {
        var embed = ErrorNotificationService.BuildErrorEmbed("TestSource", "Test error message", null, null);

        var messageField = embed.Fields.FirstOrDefault(f => f.Name == "Message");
        await Assert.That(messageField.Value).IsEqualTo("Test error message");
    }

    [Test]
    public async Task BuildErrorEmbed_WithGuildId_IncludesGuildField()
    {
        var embed = ErrorNotificationService.BuildErrorEmbed("TestSource", "Test error", null, 123456789UL);

        var guildField = embed.Fields.FirstOrDefault(f => f.Name == "Guild ID");
        await Assert.That(guildField.Value).IsEqualTo("123456789");
    }

    [Test]
    public async Task BuildErrorEmbed_WithoutGuildId_DoesNotIncludeGuildField()
    {
        var embed = ErrorNotificationService.BuildErrorEmbed("TestSource", "Test error", null, null);

        var guildField = embed.Fields.FirstOrDefault(f => f.Name == "Guild ID");
        await Assert.That(guildField.Value).IsNull();
    }

    [Test]
    public async Task BuildErrorEmbed_WithException_IncludesExceptionField()
    {
        var ex = new InvalidOperationException("Something went wrong");
        var embed = ErrorNotificationService.BuildErrorEmbed("TestSource", "Test error", ex, null);

        var exceptionField = embed.Fields.FirstOrDefault(f => f.Name == "Exception");
        await Assert.That(exceptionField.Value).IsNotNull();
        await Assert.That(exceptionField.Value!.Contains("Something went wrong")).IsTrue();
    }

    [Test]
    public async Task BuildErrorEmbed_WithoutException_DoesNotIncludeExceptionField()
    {
        var embed = ErrorNotificationService.BuildErrorEmbed("TestSource", "Test error", null, null);

        var exceptionField = embed.Fields.FirstOrDefault(f => f.Name == "Exception");
        await Assert.That(exceptionField.Value).IsNull();
    }

    [Test]
    public async Task BuildErrorEmbed_LongMessage_IsTruncated()
    {
        var longMessage = new string('A', 2000);
        var embed = ErrorNotificationService.BuildErrorEmbed("TestSource", longMessage, null, null);

        var messageField = embed.Fields.FirstOrDefault(f => f.Name == "Message");
        await Assert.That(messageField.Value!.Length).IsLessThanOrEqualTo(1024);
        await Assert.That(messageField.Value!.EndsWith("...")).IsTrue();
    }

    [Test]
    public async Task BuildErrorEmbed_HasRedColor()
    {
        var embed = ErrorNotificationService.BuildErrorEmbed("TestSource", "Test error", null, null);

        await Assert.That(embed.Color).IsEqualTo(new Color(0xE74C3C));
    }

    [Test]
    public async Task BuildErrorEmbed_HasFooter()
    {
        var embed = ErrorNotificationService.BuildErrorEmbed("TestSource", "Test error", null, null);

        await Assert.That(embed.Footer.HasValue).IsTrue();
        await Assert.That(embed.Footer!.Value.Text.Contains("errornotify")).IsTrue();
    }
}

/// <summary>
/// Tests for ErrorNotifySubscription database operations.
/// </summary>
public class ErrorNotifySubscriptionDbTests
{
    private BotDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var db = new BotDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Test]
    public async Task CanAddSubscription()
    {
        using var db = CreateInMemoryDb(nameof(CanAddSubscription));

        db.ErrorNotifySubscriptions.Add(new ErrorNotifySubscription
        {
            GuildId = 111,
            UserId = 222
        });
        await db.SaveChangesAsync();

        var count = await db.ErrorNotifySubscriptions.CountAsync();
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task CanRemoveSubscription()
    {
        using var db = CreateInMemoryDb(nameof(CanRemoveSubscription));

        var sub = new ErrorNotifySubscription { GuildId = 111, UserId = 222 };
        db.ErrorNotifySubscriptions.Add(sub);
        await db.SaveChangesAsync();

        db.ErrorNotifySubscriptions.Remove(sub);
        await db.SaveChangesAsync();

        var count = await db.ErrorNotifySubscriptions.CountAsync();
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task CanQuerySubscriptionsByGuild()
    {
        using var db = CreateInMemoryDb(nameof(CanQuerySubscriptionsByGuild));

        db.ErrorNotifySubscriptions.Add(new ErrorNotifySubscription { GuildId = 111, UserId = 222 });
        db.ErrorNotifySubscriptions.Add(new ErrorNotifySubscription { GuildId = 111, UserId = 333 });
        db.ErrorNotifySubscriptions.Add(new ErrorNotifySubscription { GuildId = 999, UserId = 444 });
        await db.SaveChangesAsync();

        var guild111Subs = await db.ErrorNotifySubscriptions
            .Where(s => s.GuildId == 111)
            .ToListAsync();

        await Assert.That(guild111Subs.Count).IsEqualTo(2);
    }

    [Test]
    public async Task CanFindSpecificSubscription()
    {
        using var db = CreateInMemoryDb(nameof(CanFindSpecificSubscription));

        db.ErrorNotifySubscriptions.Add(new ErrorNotifySubscription { GuildId = 111, UserId = 222 });
        db.ErrorNotifySubscriptions.Add(new ErrorNotifySubscription { GuildId = 111, UserId = 333 });
        await db.SaveChangesAsync();

        var existing = await db.ErrorNotifySubscriptions
            .FirstOrDefaultAsync(s => s.GuildId == 111 && s.UserId == 222);

        await Assert.That(existing).IsNotNull();
        await Assert.That(existing!.UserId).IsEqualTo(222UL);
    }

    [Test]
    public async Task SubscriptionNotFound_ReturnsNull()
    {
        using var db = CreateInMemoryDb(nameof(SubscriptionNotFound_ReturnsNull));

        var existing = await db.ErrorNotifySubscriptions
            .FirstOrDefaultAsync(s => s.GuildId == 111 && s.UserId == 999);

        await Assert.That(existing).IsNull();
    }

    [Test]
    public async Task SubscriptionHasDefaultTimestamp()
    {
        using var db = CreateInMemoryDb(nameof(SubscriptionHasDefaultTimestamp));

        var sub = new ErrorNotifySubscription { GuildId = 111, UserId = 222 };
        db.ErrorNotifySubscriptions.Add(sub);
        await db.SaveChangesAsync();

        var loaded = await db.ErrorNotifySubscriptions.FirstAsync();
        await Assert.That(loaded.SubscribedAt).IsNotEqualTo(default(DateTime));
    }
}
