using Microsoft.Extensions.Configuration;

namespace BallouBot.IntegrationTests;

/// <summary>
/// Reads integration test configuration from user secrets and environment variables.
/// User secrets are used for local development; environment variables are used in CI.
/// Environment variables take precedence over user secrets.
///
/// To set secrets locally:
///   dotnet user-secrets set "TESTER_BOT_TOKEN" "your-token" --project tests/BallouBot.IntegrationTests
///   dotnet user-secrets set "TEST_GUILD_ID" "123456789" --project tests/BallouBot.IntegrationTests
///   dotnet user-secrets set "TEST_CHANNEL_ID" "987654321" --project tests/BallouBot.IntegrationTests
/// </summary>
public static class TestConfiguration
{
    private static readonly IConfiguration Configuration = BuildConfiguration();

    private static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder();

        // Add user secrets (for local development)
        builder.AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly(), optional: true);

        // Add environment variables (for CI, and as override for local)
        builder.AddEnvironmentVariables();

        return builder.Build();
    }

    /// <summary>
    /// The BallouBot token (the bot under test, used to verify its own command registrations).
    /// </summary>
    public static string BotToken =>
        Configuration["TEST_BOT_TOKEN"]
        ?? throw new InvalidOperationException(
            "TEST_BOT_TOKEN is not set. Use 'dotnet user-secrets' or environment variables. See docs/INTEGRATION_TESTING.md for setup.");

    /// <summary>
    /// The tester bot token (second bot that sends commands and verifies responses).
    /// </summary>
    public static string TesterBotToken =>
        Configuration["TESTER_BOT_TOKEN"]
        ?? throw new InvalidOperationException(
            "TESTER_BOT_TOKEN is not set. Use 'dotnet user-secrets' or environment variables. See docs/INTEGRATION_TESTING.md for setup.");

    /// <summary>
    /// The test guild (server) ID.
    /// </summary>
    public static ulong TestGuildId =>
        ulong.TryParse(Configuration["TEST_GUILD_ID"], out var id)
            ? id
            : throw new InvalidOperationException(
                "TEST_GUILD_ID is not set or invalid. Use 'dotnet user-secrets' or environment variables. See docs/INTEGRATION_TESTING.md for setup.");

    /// <summary>
    /// The test channel ID for welcome message testing.
    /// </summary>
    public static ulong TestChannelId =>
        ulong.TryParse(Configuration["TEST_CHANNEL_ID"], out var id)
            ? id
            : throw new InvalidOperationException(
                "TEST_CHANNEL_ID is not set or invalid. Use 'dotnet user-secrets' or environment variables. See docs/INTEGRATION_TESTING.md for setup.");

    /// <summary>
    /// The SQLite connection string for the test database.
    /// Defaults to an in-memory or file-based test DB.
    /// </summary>
    public static string TestConnectionString =>
        Configuration["ConnectionStrings:DefaultConnection"]
        ?? "Data Source=balloubot-integration-test.db";

    /// <summary>
    /// Checks whether all required configuration values are present.
    /// Returns true if integration tests can run, false otherwise.
    /// </summary>
    public static bool IsConfigured =>
        !string.IsNullOrEmpty(Configuration["TEST_BOT_TOKEN"])
        && !string.IsNullOrEmpty(Configuration["TESTER_BOT_TOKEN"])
        && !string.IsNullOrEmpty(Configuration["TEST_GUILD_ID"])
        && !string.IsNullOrEmpty(Configuration["TEST_CHANNEL_ID"]);
}
