using System.Collections.Concurrent;
using BallouBot.Core.Entities;
using BallouBot.Modules.FactCheck.Models;
using BallouBot.Modules.FactCheck.Providers;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.FactCheck.Services;

/// <summary>
/// Service that handles rate limiting and coordinates AI fact-checking.
/// </summary>
public class FactCheckService
{
    private readonly AiProviderFactory _providerFactory;
    private readonly ILogger<FactCheckService> _logger;

    // Rate limiting: per-user cooldown tracking (guildId_userId -> last check time)
    private readonly ConcurrentDictionary<string, DateTime> _userCooldowns = new();

    // Rate limiting: per-guild hourly check count (guildId -> list of check timestamps)
    private readonly ConcurrentDictionary<ulong, List<DateTime>> _guildCheckTimes = new();

    public FactCheckService(AiProviderFactory providerFactory, ILogger<FactCheckService> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    /// <summary>
    /// Determines if a message should be checked based on rate limits and basic filters.
    /// </summary>
    public bool ShouldCheck(ulong guildId, ulong userId, string messageContent, FactCheckConfig config)
    {
        // Basic filters
        if (!config.IsEnabled) return false;
        if (string.IsNullOrWhiteSpace(config.ApiKey)) return false;
        if (messageContent.Length < config.MinMessageLength) return false;

        // Skip messages that look like commands
        if (messageContent.StartsWith('/') || messageContent.StartsWith('!') || messageContent.StartsWith('.'))
            return false;

        // Skip messages that are just URLs
        if (Uri.TryCreate(messageContent.Trim(), UriKind.Absolute, out _) && !messageContent.Contains(' '))
            return false;

        // Check per-user cooldown
        var userKey = $"{guildId}_{userId}";
        if (_userCooldowns.TryGetValue(userKey, out var lastCheck))
        {
            if ((DateTime.UtcNow - lastCheck).TotalSeconds < config.CooldownSeconds)
                return false;
        }

        // Check per-guild hourly limit
        if (_guildCheckTimes.TryGetValue(guildId, out var checkTimes))
        {
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            checkTimes.RemoveAll(t => t < oneHourAgo);
            if (checkTimes.Count >= config.MaxChecksPerHour)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Records that a check was performed for rate limiting purposes.
    /// </summary>
    public void RecordCheck(ulong guildId, ulong userId)
    {
        var userKey = $"{guildId}_{userId}";
        _userCooldowns[userKey] = DateTime.UtcNow;

        var checkTimes = _guildCheckTimes.GetOrAdd(guildId, _ => new List<DateTime>());
        lock (checkTimes)
        {
            checkTimes.Add(DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Analyzes a message using the configured AI provider.
    /// </summary>
    public async Task<FactCheckResult> AnalyzeMessageAsync(string message, FactCheckConfig config)
    {
        try
        {
            var providerType = AiProviderFactory.ParseProviderName(config.AiProvider);
            var provider = _providerFactory.GetProvider(providerType);

            _logger.LogDebug("Analyzing message with {Provider} ({Model})", provider.DisplayName, config.Model);

            return await provider.AnalyzeAsync(message, config.ApiKey!, config.Model, config.AzureEndpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing message with provider {Provider}", config.AiProvider);
            return FactCheckResult.Ignore($"Error: {ex.Message}", config.AiProvider);
        }
    }
}
