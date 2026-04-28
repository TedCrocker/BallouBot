using BallouBot.Modules.FactCheck.Providers;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.FactCheck.Services;

/// <summary>
/// Factory for creating and caching AI provider instances.
/// </summary>
public class AiProviderFactory
{
    private readonly HttpClient _httpClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<AiProviderType, IAiProvider> _cache = new();

    public AiProviderFactory(HttpClient httpClient, ILoggerFactory loggerFactory)
    {
        _httpClient = httpClient;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Gets the AI provider for the specified type.
    /// </summary>
    public IAiProvider GetProvider(AiProviderType type)
    {
        if (_cache.TryGetValue(type, out var cached))
            return cached;

        IAiProvider provider = type switch
        {
            AiProviderType.OpenAI => new OpenAiProvider(_httpClient, _loggerFactory.CreateLogger<OpenAiProvider>()),
            AiProviderType.Anthropic => new AnthropicProvider(_httpClient, _loggerFactory.CreateLogger<AnthropicProvider>()),
            AiProviderType.AzureOpenAI => new AzureOpenAiProvider(_httpClient, _loggerFactory.CreateLogger<AzureOpenAiProvider>()),
            AiProviderType.Google => new GoogleGeminiProvider(_httpClient, _loggerFactory.CreateLogger<GoogleGeminiProvider>()),
            _ => throw new ArgumentException($"Unknown AI provider type: {type}")
        };

        _cache[type] = provider;
        return provider;
    }

    /// <summary>
    /// Parses a provider name string to an AiProviderType.
    /// </summary>
    public static AiProviderType ParseProviderName(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "openai" => AiProviderType.OpenAI,
            "anthropic" or "claude" => AiProviderType.Anthropic,
            "azureopenai" or "azure" => AiProviderType.AzureOpenAI,
            "google" or "gemini" => AiProviderType.Google,
            _ => throw new ArgumentException($"Unknown provider: {name}. Supported: OpenAI, Anthropic, AzureOpenAI, Google")
        };
    }

    /// <summary>
    /// Gets info about all supported providers.
    /// </summary>
    public static List<(AiProviderType Type, string Name, string DefaultModel, bool RequiresEndpoint)> GetSupportedProviders()
    {
        return
        [
            (AiProviderType.OpenAI, "OpenAI", "gpt-4o-mini", false),
            (AiProviderType.Anthropic, "Anthropic", "claude-sonnet-4-20250514", false),
            (AiProviderType.AzureOpenAI, "Azure OpenAI", "gpt-4o-mini", true),
            (AiProviderType.Google, "Google Gemini", "gemini-2.0-flash", false)
        ];
    }
}
