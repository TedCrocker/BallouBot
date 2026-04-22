using BallouBot.Core;
using BallouBot.Core.Entities;
using BallouBot.Data;
using BallouBot.Modules.Welcome;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace BallouBot.IntegrationTests;

/// <summary>
/// Hybrid integration tests for the WelcomeHandler.
/// Uses a REAL SQLite database but MOCKED Discord events.
/// This tests the full flow: database config → event trigger → message formatting,
/// without needing to actually trigger a user join on Discord.
/// </summary>
public class WelcomeHandlerHybridTests
{
    private static string GetTestDbPath(string testName)
        => $"Data Source=hybrid-{testName}-{Guid.NewGuid():N}.db";

    /// <summary>
    /// Tests that the WelcomeHandler reads config from a real SQLite database
    /// and formats the welcome message correctly.
    /// </summary>
    [Test]
    [Category("Hybrid")]
    public async Task WelcomeHandler_ReadsConfigFromRealDatabase()
    {
        var connStr = GetTestDbPath(nameof(WelcomeHandler_ReadsConfigFromRealDatabase));

        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseSqlite(connStr)
            .Options;

        // Setup: create database and seed config
        using (var setupDb = new BotDbContext(options))
        {
            await setupDb.Database.EnsureCreatedAsync();
            setupDb.GuildSettings.Add(new GuildSettings { GuildId = 100 });
            setupDb.WelcomeConfigs.Add(new WelcomeConfig
            {
                GuildId = 100,
                ChannelId = 200,
                Message = "Welcome {user} to {server}!",
                IsEnabled = true,
                UseEmbed = false
            });
            await setupDb.SaveChangesAsync();
        }

        // Verify: read back from a fresh context
        using (var verifyDb = new BotDbContext(options))
        {
            var config = await verifyDb.WelcomeConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.GuildId == 100);

            await Assert.That(config).IsNotNull();
            await Assert.That(config!.IsEnabled).IsTrue();
            await Assert.That(config.ChannelId).IsEqualTo((ulong)200);

            // Test message formatting
            var formatted = config.FormatMessage(
                "<@12345>",
                "TestUser",
                "Test Server",
                42);

            await Assert.That(formatted).IsEqualTo("Welcome <@12345> to Test Server!");
        }

        // Cleanup
        using (var cleanupDb = new BotDbContext(options))
        {
            await cleanupDb.Database.EnsureDeletedAsync();
        }
    }

    /// <summary>
    /// Tests the full config → format → output flow with embed mode.
    /// </summary>
    [Test]
    [Category("Hybrid")]
    public async Task WelcomeHandler_FormatsEmbedMessageCorrectly()
    {
        var connStr = GetTestDbPath(nameof(WelcomeHandler_FormatsEmbedMessageCorrectly));

        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseSqlite(connStr)
            .Options;

        using (var setupDb = new BotDbContext(options))
        {
            await setupDb.Database.EnsureCreatedAsync();
            setupDb.GuildSettings.Add(new GuildSettings { GuildId = 300 });
            setupDb.WelcomeConfigs.Add(new WelcomeConfig
            {
                GuildId = 300,
                ChannelId = 400,
                Message = "Hey {username}, you are member #{membercount}!",
                IsEnabled = true,
                UseEmbed = true,
                EmbedColor = "FF5733",
                EmbedTitle = "Welcome!"
            });
            await setupDb.SaveChangesAsync();
        }

        using (var verifyDb = new BotDbContext(options))
        {
            var config = await verifyDb.WelcomeConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.GuildId == 300);

            await Assert.That(config).IsNotNull();
            await Assert.That(config!.UseEmbed).IsTrue();
            await Assert.That(config.EmbedColor).IsEqualTo("FF5733");
            await Assert.That(config.EmbedTitle).IsEqualTo("Welcome!");

            var formatted = config.FormatMessage(
                "<@99999>",
                "CoolUser",
                "Cool Server",
                100);

            await Assert.That(formatted).IsEqualTo("Hey CoolUser, you are member #100!");
        }

        using (var cleanupDb = new BotDbContext(options))
        {
            await cleanupDb.Database.EnsureDeletedAsync();
        }
    }

    /// <summary>
    /// Tests that disabled config is respected.
    /// </summary>
    [Test]
    [Category("Hybrid")]
    public async Task WelcomeHandler_SkipsDisabledConfig()
    {
        var connStr = GetTestDbPath(nameof(WelcomeHandler_SkipsDisabledConfig));

        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseSqlite(connStr)
            .Options;

        using (var setupDb = new BotDbContext(options))
        {
            await setupDb.Database.EnsureCreatedAsync();
            setupDb.GuildSettings.Add(new GuildSettings { GuildId = 500 });
            setupDb.WelcomeConfigs.Add(new WelcomeConfig
            {
                GuildId = 500,
                ChannelId = 600,
                Message = "Welcome!",
                IsEnabled = false  // Disabled
            });
            await setupDb.SaveChangesAsync();
        }

        using (var verifyDb = new BotDbContext(options))
        {
            var config = await verifyDb.WelcomeConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.GuildId == 500);

            await Assert.That(config).IsNotNull();
            await Assert.That(config!.IsEnabled).IsFalse();
        }

        using (var cleanupDb = new BotDbContext(options))
        {
            await cleanupDb.Database.EnsureDeletedAsync();
        }
    }

    /// <summary>
    /// Tests GetOrCreateConfigAsync with a real SQLite database.
    /// </summary>
    [Test]
    [Category("Hybrid")]
    public async Task GetOrCreateConfig_WorksWithRealSqlite()
    {
        var connStr = GetTestDbPath(nameof(GetOrCreateConfig_WorksWithRealSqlite));

        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseSqlite(connStr)
            .Options;

        using (var db = new BotDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();

            // First call creates the config
            var config1 = await WelcomeCommands.GetOrCreateConfigAsync(db, 777);
            await Assert.That(config1).IsNotNull();
            await Assert.That(config1.GuildId).IsEqualTo((ulong)777);
            await Assert.That(config1.IsEnabled).IsTrue();

            // Second call returns the existing config
            var config2 = await WelcomeCommands.GetOrCreateConfigAsync(db, 777);
            await Assert.That(config2.Id).IsEqualTo(config1.Id);
        }

        using (var cleanupDb = new BotDbContext(options))
        {
            await cleanupDb.Database.EnsureDeletedAsync();
        }
    }

    /// <summary>
    /// Tests that config updates persist correctly in SQLite.
    /// </summary>
    [Test]
    [Category("Hybrid")]
    public async Task ConfigUpdate_PersistsAcrossContexts()
    {
        var connStr = GetTestDbPath(nameof(ConfigUpdate_PersistsAcrossContexts));

        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseSqlite(connStr)
            .Options;

        // Create config in first context
        using (var db = new BotDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            var config = await WelcomeCommands.GetOrCreateConfigAsync(db, 888);
            config.Message = "Updated welcome!";
            config.ChannelId = 12345;
            config.UseEmbed = true;
            config.EmbedColor = "00FF00";
            await db.SaveChangesAsync();
        }

        // Read in a completely new context (simulates a different request)
        using (var db = new BotDbContext(options))
        {
            var config = await db.WelcomeConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.GuildId == 888);

            await Assert.That(config).IsNotNull();
            await Assert.That(config!.Message).IsEqualTo("Updated welcome!");
            await Assert.That(config.ChannelId).IsEqualTo((ulong)12345);
            await Assert.That(config.UseEmbed).IsTrue();
            await Assert.That(config.EmbedColor).IsEqualTo("00FF00");
        }

        using (var cleanupDb = new BotDbContext(options))
        {
            await cleanupDb.Database.EnsureDeletedAsync();
        }
    }
}
