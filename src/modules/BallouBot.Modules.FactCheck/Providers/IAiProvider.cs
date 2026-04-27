using BallouBot.Modules.FactCheck.Models;

namespace BallouBot.Modules.FactCheck.Providers;

/// <summary>
/// Interface for AI providers used in fact-checking.
/// </summary>
public interface IAiProvider
{
    /// <summary>
    /// Gets the provider type.
    /// </summary>
    AiProviderType ProviderType { get; }

    /// <summary>
    /// Gets the display name of the provider.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets whether this provider requires an API key.
    /// </summary>
    bool RequiresApiKey { get; }

    /// <summary>
    /// Gets whether this provider requires an endpoint URL (e.g., Azure).
    /// </summary>
    bool RequiresEndpoint { get; }

    /// <summary>
    /// Gets the default model for this provider.
    /// </summary>
    string DefaultModel { get; }

    /// <summary>
    /// Analyzes a message for factual accuracy using the AI provider.
    /// </summary>
    /// <param name="message">The message text to analyze.</param>
    /// <param name="apiKey">The API key for the provider.</param>
    /// <param name="model">The model to use.</param>
    /// <param name="endpoint">Optional endpoint URL (for Azure).</param>
    /// <returns>The fact-check result.</returns>
    Task<FactCheckResult> AnalyzeAsync(string message, string apiKey, string model, string? endpoint = null);
}
