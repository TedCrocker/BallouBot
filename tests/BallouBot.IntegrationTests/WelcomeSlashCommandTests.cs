using Discord;
using TUnit.Core.Exceptions;

namespace BallouBot.IntegrationTests;

/// <summary>
/// Integration tests for the /welcome slash command.
/// These tests use the tester bot to verify that BallouBot's slash commands
/// are registered and respond correctly in a real Discord server.
/// </summary>
public class WelcomeSlashCommandTests
{
    private static void SkipIfNotConfigured()
    {
        if (!TestConfiguration.IsConfigured)
            throw new SkipTestException("Integration test environment variables not configured. Skipping.");
    }

    [Test]
    [Category("Integration")]
    public async Task WelcomeCommand_IsRegisteredGlobally()
    {
        SkipIfNotConfigured();

        await using var fixture = new DiscordTestFixture();
        await fixture.ConnectAsync();

        var guild = fixture.TestGuild;
        await Assert.That(guild).IsNotNull();

        var commands = await guild!.GetApplicationCommandsAsync();
        var welcomeCommand = commands.FirstOrDefault(c => c.Name == "welcome");

        await Assert.That(welcomeCommand).IsNotNull();
        await Assert.That(welcomeCommand!.Description).IsNotNull();
    }

    [Test]
    [Category("Integration")]
    public async Task WelcomeCommand_HasExpectedSubcommands()
    {
        SkipIfNotConfigured();

        await using var fixture = new DiscordTestFixture();
        await fixture.ConnectAsync();

        var guild = fixture.TestGuild;
        await Assert.That(guild).IsNotNull();

        var commands = await guild!.GetApplicationCommandsAsync();
        var welcomeCommand = commands.FirstOrDefault(c => c.Name == "welcome");

        await Assert.That(welcomeCommand).IsNotNull();

        var subcommandNames = welcomeCommand!.Options
            .Where(o => o.Type == ApplicationCommandOptionType.SubCommand)
            .Select(o => o.Name)
            .ToList();

        await Assert.That(subcommandNames).Contains("channel");
        await Assert.That(subcommandNames).Contains("message");
        await Assert.That(subcommandNames).Contains("toggle");
        await Assert.That(subcommandNames).Contains("preview");
        await Assert.That(subcommandNames).Contains("embed");
        await Assert.That(subcommandNames).Contains("color");
        await Assert.That(subcommandNames).Contains("title");
    }

    [Test]
    [Category("Integration")]
    public async Task WelcomeCommand_HasPermissionRestriction()
    {
        SkipIfNotConfigured();

        await using var fixture = new DiscordTestFixture();
        await fixture.ConnectAsync();

        var guild = fixture.TestGuild;
        await Assert.That(guild).IsNotNull();

        var commands = await guild!.GetApplicationCommandsAsync();
        var welcomeCommand = commands.FirstOrDefault(c => c.Name == "welcome");

        await Assert.That(welcomeCommand).IsNotNull();

        // Verify the command has some form of permission restriction
        // DefaultMemberPermissions is a GuildPermissions struct — if ManageGuild is not set,
        // the raw value would be 0 (no permissions required)
        var perms = welcomeCommand!.DefaultMemberPermissions;
        await Assert.That(perms.ManageGuild).IsTrue();
    }
}
