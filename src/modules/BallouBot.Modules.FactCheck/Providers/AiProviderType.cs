namespace BallouBot.Modules.FactCheck.Providers;

/// <summary>
/// Supported AI provider types for fact-checking.
/// </summary>
public enum AiProviderType
{
    /// <summary>
    /// OpenAI (GPT-4o, GPT-4o-mini, etc.)
    /// </summary>
    OpenAI,

    /// <summary>
    /// Anthropic (Claude models)
    /// </summary>
    Anthropic,

    /// <summary>
    /// Azure OpenAI (hosted OpenAI models on Azure)
    /// </summary>
    AzureOpenAI,

    /// <summary>
    /// Google Gemini (Gemini 2.0 Flash, etc.)
    /// </summary>
    Google
}
