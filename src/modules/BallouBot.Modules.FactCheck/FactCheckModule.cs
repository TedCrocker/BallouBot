using BallouBot.Core;
using BallouBot.Data;
using BallouBot.Modules.FactCheck.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.FactCheck;

/// <summary>
/// The Fact Check module — monitors watched users and uses AI to correct factual inaccuracies.
/// </summary>
[BotModule("factcheck")]
public class FactCheckModule : IModule
{
    /// <inheritdoc />
    public string Name => "Fact Check";

    /// <inheritdoc />
    public string Description => "Monitors watched users and uses AI to fact-check their messages, correcting inaccuracies with a reply.";

    /// <inheritdoc />
    public string Version => "1.0.0";

    private FactCheckCommands? _commands;
    private FactCheckService? _factCheckService;
    private IModuleContext? _context;
    private ILogger<FactCheckModule>? _logger;

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<HttpClient>();
        services.AddSingleton<AiProviderFactory>();
        services.AddSingleton<FactCheckService>();
    }

    /// <inheritdoc />
    public async Task InitializeAsync(IModuleContext context)
    {
        _context = context;
        _logger = context.GetLogger<FactCheckModule>();

        var httpClient = context.Services.GetRequiredService<HttpClient>();
        var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
        var providerFactory = new AiProviderFactory(httpClient, loggerFactory);
        _factCheckService = new FactCheckService(providerFactory, context.GetLogger<FactCheckService>());

        _commands = new FactCheckCommands(context);

        context.Client.SlashCommandExecuted += OnSlashCommandAsync;
        context.Client.MessageReceived += OnMessageReceivedAsync;

        await _commands.RegisterCommandsAsync();

        _logger.LogInformation("Fact Check module initialized.");
    }

    /// <inheritdoc />
    public Task ShutdownAsync()
    {
        if (_context is not null)
        {
            _context.Client.SlashCommandExecuted -= OnSlashCommandAsync;
            _context.Client.MessageReceived -= OnMessageReceivedAsync;
        }
        return Task.CompletedTask;
    }

    private async Task OnSlashCommandAsync(SocketSlashCommand command)
    {
        if (_commands is not null && command.CommandName == "factcheck")
        {
            await _commands.HandleSlashCommandAsync(command);
        }
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        if (_factCheckService is null || _context is null) return;
        if (message is not SocketUserMessage userMessage) return;
        if (message.Author.IsBot) return;
        if (message.Channel is not SocketTextChannel textChannel) return;

        var guildId = textChannel.Guild.Id;
        var userId = message.Author.Id;

        try
        {
            using var scope = _context.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            // Load config with watched users
            var config = await db.FactCheckConfigs
                .Include(c => c.WatchedUsers)
                .FirstOrDefaultAsync(c => c.GuildId == guildId);

            if (config is null || !config.IsEnabled) return;

            // Check if user is on the watch list
            var isWatched = config.WatchedUsers.Any(u => u.UserId == userId);
            if (!isWatched) return;

            // Check channel restriction
            if (config.ChannelId.HasValue && textChannel.Id != config.ChannelId.Value) return;

            // Check rate limits and basic filters
            if (!_factCheckService.ShouldCheck(guildId, userId, userMessage.Content, config)) return;

            // Record the check for rate limiting
            _factCheckService.RecordCheck(guildId, userId);

            _logger.LogDebug("Fact-checking message from {User} in {Guild}: {Preview}",
                message.Author.Username, textChannel.Guild.Name,
                userMessage.Content.Length > 50 ? userMessage.Content[..50] + "..." : userMessage.Content);

            // Analyze the message
            var result = await _factCheckService.AnalyzeMessageAsync(userMessage.Content, config);

            if (result.ShouldCorrect)
            {
                _logger.LogInformation("Correction found for {User} in {Guild}: {Correction}",
                    message.Author.Username, textChannel.Guild.Name, result.Correction);

                var embed = new EmbedBuilder()
                    .WithTitle("🔍 Fact Check")
                    .WithDescription(result.Correction)
                    .WithColor(new Color(0xE74C3C)) // Red for corrections
                    .WithCurrentTimestamp();

                await userMessage.ReplyAsync(embed: embed.Build());
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing message for fact-check in guild {GuildId}", guildId);
        }
    }
}
