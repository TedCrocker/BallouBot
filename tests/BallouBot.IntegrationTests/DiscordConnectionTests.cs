using Discord;
using TUnit.Core.Exceptions;

namespace BallouBot.IntegrationTests;

/// <summary>
/// Tests that verify basic Discord connectivity and server setup.
/// These are the first tests to run — if they fail, all other integration tests are invalid.
/// </summary>
public class DiscordConnectionTests
{
    private static void SkipIfNotConfigured()
    {
        if (!TestConfiguration.IsConfigured)
            throw new SkipTestException("Integration test environment variables not configured. Skipping.");
    }

    [Test]
    [Category("Integration")]
    public async Task TesterBot_CanConnectToDiscord()
    {
        SkipIfNotConfigured();

        await using var fixture = new DiscordTestFixture();
        await fixture.ConnectAsync();

        await Assert.That(fixture.Client.ConnectionState).IsEqualTo(ConnectionState.Connected);
    }

    [Test]
    [Category("Integration")]
    public async Task TesterBot_CanSeeTestGuild()
    {
        SkipIfNotConfigured();

        await using var fixture = new DiscordTestFixture();
        await fixture.ConnectAsync();

        var guild = fixture.TestGuild;

        await Assert.That(guild).IsNotNull();
        await Assert.That(guild!.Name).IsNotNull();
    }

    [Test]
    [Category("Integration")]
    public async Task TesterBot_CanSeeTestChannel()
    {
        SkipIfNotConfigured();

        await using var fixture = new DiscordTestFixture();
        await fixture.ConnectAsync();

        var channel = fixture.TestChannel;

        await Assert.That(channel).IsNotNull();
        await Assert.That(channel!.Name).IsNotNull();
    }

    [Test]
    [Category("Integration")]
    public async Task TesterBot_CanSeeBallouBotInGuild()
    {
        SkipIfNotConfigured();

        await using var fixture = new DiscordTestFixture();
        await fixture.ConnectAsync();

        var guild = fixture.TestGuild;
        await Assert.That(guild).IsNotNull();

        // Download users to make sure we can see all members
        await guild!.DownloadUsersAsync();

        // Find at least one bot user (BallouBot should be there)
        var bots = guild.Users.Where(u => u.IsBot && u.Id != fixture.Client.CurrentUser.Id).ToList();

        await Assert.That(bots.Count).IsGreaterThanOrEqualTo(1);
    }
}
