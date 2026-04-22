using BallouBot.Core.Entities;

namespace BallouBot.Core.Tests;

/// <summary>
/// Tests for the WelcomeConfig entity, particularly the FormatMessage method.
/// </summary>
public class WelcomeConfigTests
{
    [Test]
    public async Task FormatMessage_ReplacesUserPlaceholder()
    {
        var config = new WelcomeConfig { Message = "Hello {user}!" };

        var result = config.FormatMessage("<@123>", "TestUser", "TestServer", 42);

        await Assert.That(result).IsEqualTo("Hello <@123>!");
    }

    [Test]
    public async Task FormatMessage_ReplacesUsernamePlaceholder()
    {
        var config = new WelcomeConfig { Message = "Hello {username}!" };

        var result = config.FormatMessage("<@123>", "TestUser", "TestServer", 42);

        await Assert.That(result).IsEqualTo("Hello TestUser!");
    }

    [Test]
    public async Task FormatMessage_ReplacesServerPlaceholder()
    {
        var config = new WelcomeConfig { Message = "Welcome to {server}!" };

        var result = config.FormatMessage("<@123>", "TestUser", "My Server", 42);

        await Assert.That(result).IsEqualTo("Welcome to My Server!");
    }

    [Test]
    public async Task FormatMessage_ReplacesMemberCountPlaceholder()
    {
        var config = new WelcomeConfig { Message = "You are member #{membercount}." };

        var result = config.FormatMessage("<@123>", "TestUser", "TestServer", 42);

        await Assert.That(result).IsEqualTo("You are member #42.");
    }

    [Test]
    public async Task FormatMessage_ReplacesAllPlaceholders()
    {
        var config = new WelcomeConfig
        {
            Message = "Welcome to {server}, {user}! Hey {username}, you are member #{membercount}."
        };

        var result = config.FormatMessage("<@123>", "TestUser", "My Server", 100);

        await Assert.That(result).IsEqualTo("Welcome to My Server, <@123>! Hey TestUser, you are member #100.");
    }

    [Test]
    public async Task FormatMessage_IsCaseInsensitive()
    {
        var config = new WelcomeConfig { Message = "Hello {USER} in {Server}!" };

        var result = config.FormatMessage("<@123>", "TestUser", "My Server", 42);

        await Assert.That(result).IsEqualTo("Hello <@123> in My Server!");
    }

    [Test]
    public async Task FormatMessage_HandlesNoPlaceholders()
    {
        var config = new WelcomeConfig { Message = "Hello everyone!" };

        var result = config.FormatMessage("<@123>", "TestUser", "TestServer", 42);

        await Assert.That(result).IsEqualTo("Hello everyone!");
    }

    [Test]
    public async Task FormatMessage_HandlesEmptyMessage()
    {
        var config = new WelcomeConfig { Message = "" };

        var result = config.FormatMessage("<@123>", "TestUser", "TestServer", 42);

        await Assert.That(result).IsEqualTo("");
    }
}

/// <summary>
/// Tests for the WelcomeConfig entity default values.
/// </summary>
public class WelcomeConfigDefaultsTests
{
    [Test]
    public async Task NewWelcomeConfig_HasDefaultMessage()
    {
        var config = new WelcomeConfig();

        await Assert.That(config.Message).IsEqualTo("Welcome to {server}, {user}! You are member #{membercount}.");
    }

    [Test]
    public async Task NewWelcomeConfig_IsEnabledByDefault()
    {
        var config = new WelcomeConfig();

        await Assert.That(config.IsEnabled).IsTrue();
    }

    [Test]
    public async Task NewWelcomeConfig_UseEmbedIsFalseByDefault()
    {
        var config = new WelcomeConfig();

        await Assert.That(config.UseEmbed).IsFalse();
    }

    [Test]
    public async Task NewWelcomeConfig_HasDefaultEmbedColor()
    {
        var config = new WelcomeConfig();

        await Assert.That(config.EmbedColor).IsEqualTo("5865F2");
    }

    [Test]
    public async Task NewWelcomeConfig_HasDefaultEmbedTitle()
    {
        var config = new WelcomeConfig();

        await Assert.That(config.EmbedTitle).IsEqualTo("Welcome!");
    }
}

/// <summary>
/// Tests for the GuildSettings entity default values.
/// </summary>
public class GuildSettingsDefaultsTests
{
    [Test]
    public async Task NewGuildSettings_HasDefaultPrefix()
    {
        var settings = new GuildSettings();

        await Assert.That(settings.Prefix).IsEqualTo("!");
    }

    [Test]
    public async Task NewGuildSettings_HasEmptyGuildName()
    {
        var settings = new GuildSettings();

        await Assert.That(settings.GuildName).IsEqualTo(string.Empty);
    }
}

/// <summary>
/// Tests for the BotModuleAttribute.
/// </summary>
public class BotModuleAttributeTests
{
    [Test]
    public async Task BotModuleAttribute_StoresId()
    {
        var attr = new BotModuleAttribute("test-module");

        await Assert.That(attr.Id).IsEqualTo("test-module");
    }

    [Test]
    public async Task BotModuleAttribute_EnabledByDefaultIsTrue()
    {
        var attr = new BotModuleAttribute("test");

        await Assert.That(attr.EnabledByDefault).IsTrue();
    }

    [Test]
    public async Task BotModuleAttribute_EnabledByDefaultCanBeOverridden()
    {
        var attr = new BotModuleAttribute("test") { EnabledByDefault = false };

        await Assert.That(attr.EnabledByDefault).IsFalse();
    }
}
