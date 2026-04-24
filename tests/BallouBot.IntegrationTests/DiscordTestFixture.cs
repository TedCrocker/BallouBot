using Discord;
using Discord.WebSocket;

namespace BallouBot.IntegrationTests;

/// <summary>
/// Manages a Discord client connection for integration testing.
/// The tester bot connects to Discord and can interact with the test server
/// to verify BallouBot's behavior.
/// </summary>
public class DiscordTestFixture : IAsyncDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly TaskCompletionSource<bool> _readyTcs = new();

    /// <summary>
    /// Gets the connected Discord socket client.
    /// </summary>
    public DiscordSocketClient Client => _client;

    /// <summary>
    /// Gets the test guild.
    /// </summary>
    public SocketGuild? TestGuild => _client.GetGuild(TestConfiguration.TestGuildId);

    /// <summary>
    /// Gets the test channel.
    /// </summary>
    public SocketTextChannel? TestChannel => TestGuild?.GetTextChannel(TestConfiguration.TestChannelId);

    private readonly string _token;

    /// <summary>
    /// Creates a fixture that connects using the tester bot token by default.
    /// </summary>
    /// <param name="token">The bot token to use. Defaults to the tester bot token.</param>
    public DiscordTestFixture(string? token = null)
    {
        _token = token ?? TestConfiguration.TesterBotToken;
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds
                | GatewayIntents.GuildMessages
                | GatewayIntents.MessageContent
                | GatewayIntents.GuildMembers,
            LogLevel = LogSeverity.Warning
        });

        _client.Ready += () =>
        {
            _readyTcs.TrySetResult(true);
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Connects the bot to Discord and waits until ready.
    /// </summary>
    /// <param name="timeoutSeconds">Maximum seconds to wait for connection.</param>
    public async Task ConnectAsync(int timeoutSeconds = 30)
    {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();

        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
        var completedTask = await Task.WhenAny(_readyTcs.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            throw new TimeoutException($"Tester bot failed to connect within {timeoutSeconds} seconds.");
        }
    }

    /// <summary>
    /// Waits for a message in the test channel that matches a predicate.
    /// Useful for verifying bot responses.
    /// </summary>
    /// <param name="predicate">Condition to match against received messages.</param>
    /// <param name="timeoutSeconds">Maximum seconds to wait for the message.</param>
    /// <returns>The matching message, or null if timed out.</returns>
    public async Task<IMessage?> WaitForMessageAsync(
        Func<IMessage, bool> predicate,
        int timeoutSeconds = 10)
    {
        var tcs = new TaskCompletionSource<IMessage>();

        Task HandleMessage(SocketMessage message)
        {
            if (predicate(message))
            {
                tcs.TrySetResult(message);
            }
            return Task.CompletedTask;
        }

        _client.MessageReceived += HandleMessage;

        try
        {
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            return completedTask == tcs.Task ? await tcs.Task : null;
        }
        finally
        {
            _client.MessageReceived -= HandleMessage;
        }
    }

    /// <summary>
    /// Disconnects the tester bot from Discord.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
        _client.Dispose();
    }
}
