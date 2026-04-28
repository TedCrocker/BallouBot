using BallouBot.Core;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.Help;

/// <summary>
/// The Help module — provides a /balloubot help command that lists all loaded modules
/// and their registered slash commands.
/// </summary>
[BotModule("help")]
public class HelpModule : IModule
{
    /// <inheritdoc />
    public string Name => "Help";

    /// <inheritdoc />
    public string Description => "Provides a help command that lists all loaded modules and their slash commands.";

    /// <inheritdoc />
    public string Version => "1.0.0";

    private IModuleContext? _context;
    private ILogger<HelpModule>? _logger;

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        // No additional services needed
    }

    /// <inheritdoc />
    public async Task InitializeAsync(IModuleContext context)
    {
        _context = context;
        _logger = context.GetLogger<HelpModule>();

        context.Client.SlashCommandExecuted += OnSlashCommandAsync;

        await RegisterCommandsAsync();

        _logger.LogInformation("Help module initialized.");
    }

    /// <inheritdoc />
    public Task ShutdownAsync()
    {
        if (_context is not null)
        {
            _context.Client.SlashCommandExecuted -= OnSlashCommandAsync;
        }
        return Task.CompletedTask;
    }

    private async Task RegisterCommandsAsync()
    {
        try
        {
            // Since multiple modules share the /balloubot command, we must register
            // the full command with ALL known subcommands each time.
            var command = new SlashCommandBuilder()
                .WithName("balloubot")
                .WithDescription("BallouBot commands")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("help")
                    .WithDescription("Show all loaded modules and their commands.")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("errornotify")
                    .WithDescription("Toggle error notification DMs for a user (Administrator only).")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("user", ApplicationCommandOptionType.User,
                        "The user to toggle error notifications for.", isRequired: true));

            var builtCommand = command.Build();
            foreach (var guild in _context!.Client.Guilds)
            {
                await guild.CreateApplicationCommandAsync(builtCommand);
            }
            _logger?.LogInformation("Registered /balloubot help command on {Count} guild(s).", _context.Client.Guilds.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to register /balloubot slash command.");
        }
    }

    private async Task OnSlashCommandAsync(SocketSlashCommand command)
    {
        if (command.CommandName != "balloubot") return;

        var subCommand = command.Data.Options.FirstOrDefault();
        if (subCommand?.Name != "help") return;

        try
        {
            await HandleHelpAsync(command);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling /balloubot help");
            if (!command.HasResponded)
                await command.RespondAsync("An error occurred while loading help information.", ephemeral: true);
        }
    }

    private async Task HandleHelpAsync(SocketSlashCommand command)
    {
        if (_context is null) return;

        // Get all loaded modules from DI
        var modules = _context.Services.GetServices<IModule>().ToList();

        // Get guild slash commands from Discord
        var guild = _context.Client.GetGuild(command.GuildId!.Value);
        var guildCommands = await guild.GetApplicationCommandsAsync();

        // Build the help embed
        var embed = new EmbedBuilder()
            .WithTitle("🤖 BallouBot — Help")
            .WithDescription($"BallouBot has **{modules.Count}** module(s) loaded with **{guildCommands.Count}** command(s) registered.")
            .WithColor(new Color(0x5865F2)) // Discord blurple
            .WithCurrentTimestamp()
            .WithFooter("Use /balloubot help to see this message again");

        // Add a section for each module
        foreach (var module in modules.OrderBy(m => m.Name))
        {
            var attr = module.GetType()
                .GetCustomAttributes(typeof(BotModuleAttribute), false)
                .FirstOrDefault() as BotModuleAttribute;

            var moduleId = attr?.Id ?? "unknown";

            embed.AddField(
                $"📦 {module.Name} v{module.Version}",
                module.Description,
                false);
        }

        // Add a section listing all registered slash commands
        if (guildCommands.Count > 0)
        {
            var commandLines = new List<string>();
            foreach (var cmd in guildCommands.OrderBy(c => c.Name))
            {
                // List subcommands if present
                var subCommands = cmd.Options?
                    .Where(o => o.Type == ApplicationCommandOptionType.SubCommand)
                    .Select(o => o.Name)
                    .ToList();

                if (subCommands is { Count: > 0 })
                {
                    var subList = string.Join(", ", subCommands);
                    commandLines.Add($"`/{cmd.Name}` — {cmd.Description}\n  ↳ Subcommands: `{subList}`");
                }
                else
                {
                    commandLines.Add($"`/{cmd.Name}` — {cmd.Description}");
                }
            }

            // Split into chunks if too long (Discord field limit is 1024)
            var commandText = string.Join("\n", commandLines);
            if (commandText.Length > 1024)
            {
                // Split into multiple fields
                var chunks = ChunkText(commandLines, 1024);
                for (var i = 0; i < chunks.Count; i++)
                {
                    embed.AddField(
                        i == 0 ? "⌨️ Slash Commands" : "⌨️ Slash Commands (cont.)",
                        chunks[i],
                        false);
                }
            }
            else
            {
                embed.AddField("⌨️ Slash Commands", commandText, false);
            }
        }

        await command.RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    /// <summary>
    /// Splits a list of lines into chunks that don't exceed maxLength characters each.
    /// </summary>
    internal static List<string> ChunkText(List<string> lines, int maxLength)
    {
        var chunks = new List<string>();
        var current = new System.Text.StringBuilder();

        foreach (var line in lines)
        {
            if (current.Length + line.Length + 1 > maxLength && current.Length > 0)
            {
                chunks.Add(current.ToString());
                current.Clear();
            }
            if (current.Length > 0) current.AppendLine();
            current.Append(line);
        }

        if (current.Length > 0) chunks.Add(current.ToString());

        return chunks;
    }
}
