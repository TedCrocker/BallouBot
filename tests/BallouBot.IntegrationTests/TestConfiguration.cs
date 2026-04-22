namespace BallouBot.IntegrationTests;

/// <summary>
/// Reads integration test configuration from environment variables.
/// These must be set before running integration tests (via CI secrets or local env).
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    /// The tester bot token (second bot that sends commands and verifies responses).
    /// </summary>
    public static string TesterBotToken =>
        Environment.GetEnvironmentVariable("TESTER_BOT_TOKEN")
        ?? throw new InvalidOperationException(
            "TESTER_BOT_TOKEN environment variable is not set. See docs/INTEGRATION_TESTING.md for setup.");

    /// <summary>
    /// The test guild (server) ID.
    /// </summary>
    public static ulong TestGuildId =>
        ulong.TryParse(Environment.GetEnvironmentVariable("TEST_GUILD_ID"), out var id)
            ? id
            : throw new InvalidOperationException(
                "TEST_GUILD_ID environment variable is not set or invalid. See docs/INTEGRATION_TESTING.md for setup.");

    /// <summary>
    /// The test channel ID for welcome message testing.
    /// </summary>
    public static ulong TestChannelId =>
        ulong.TryParse(Environment.GetEnvironmentVariable("TEST_CHANNEL_ID"), out var id)
            ? id
            : throw new InvalidOperationException(
                "TEST_CHANNEL_ID environment variable is not set or invalid. See docs/INTEGRATION_TESTING.md for setup.");

    /// <summary>
    /// The SQLite connection string for the test database.
    /// Defaults to an in-memory or file-based test DB.
    /// </summary>
    public static string TestConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? "Data Source=balloubot-integration-test.db";

    /// <summary>
    /// Checks whether all required environment variables are present.
    /// Returns true if integration tests can run, false otherwise.
    /// </summary>
    public static bool IsConfigured =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TESTER_BOT_TOKEN"))
        && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEST_GUILD_ID"))
        && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEST_CHANNEL_ID"));
}
