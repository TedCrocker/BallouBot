using BallouBot.Modules.FactCheck.Models;

namespace BallouBot.Modules.FactCheck.Providers;

/// <summary>
/// Shared prompt templates and response parsing for fact-check AI providers.
/// </summary>
public static class FactCheckPrompt
{
    /// <summary>
    /// The system prompt sent to all AI providers.
    /// </summary>
    public const string SystemPrompt = """
        You are a fact-checker. Your job is to analyze messages from a Discord chat and determine if they contain incorrect factual claims.

        Rules:
        1. If the message is an opinion, subjective statement, joke, sarcasm, question, greeting, or emotional expression → respond with exactly: IGNORE
        2. If the message contains a factual claim that is CORRECT or mostly correct → respond with exactly: IGNORE
        3. If the message contains a factual claim that is clearly, objectively INCORRECT → respond with:
           CORRECT: [brief, friendly correction with the accurate information in 1-2 sentences]

        Important guidelines:
        - Only flag things that are clearly and objectively wrong. When in doubt, respond with IGNORE.
        - Do NOT correct grammar, spelling, or stylistic choices.
        - Do NOT correct hyperbole, casual speech, or rough approximations.
        - Do NOT be pedantic about minor details.
        - Be concise and friendly in corrections.
        - Your response must start with either "IGNORE" or "CORRECT:" — nothing else.
        """;

    /// <summary>
    /// The user prompt template. {0} is replaced with the message text.
    /// </summary>
    public const string UserPrompt = "Message: \"{0}\"";

    /// <summary>
    /// Parses the AI response into a FactCheckResult.
    /// </summary>
    /// <param name="response">The raw AI response text.</param>
    /// <param name="providerName">The name of the AI provider.</param>
    /// <returns>A parsed FactCheckResult.</returns>
    public static FactCheckResult ParseResponse(string response, string providerName)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return FactCheckResult.Ignore(response, providerName);
        }

        var trimmed = response.Trim();

        if (trimmed.StartsWith("CORRECT:", StringComparison.OrdinalIgnoreCase))
        {
            var correction = trimmed["CORRECT:".Length..].Trim();
            if (!string.IsNullOrWhiteSpace(correction))
            {
                return FactCheckResult.Correct(correction, response, providerName);
            }
        }

        return FactCheckResult.Ignore(response, providerName);
    }
}
