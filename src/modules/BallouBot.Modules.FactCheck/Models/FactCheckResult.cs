namespace BallouBot.Modules.FactCheck.Models;

/// <summary>
/// Represents the result of an AI fact-check analysis.
/// </summary>
public class FactCheckResult
{
    /// <summary>
    /// Gets or sets whether the statement should be corrected.
    /// </summary>
    public bool ShouldCorrect { get; set; }

    /// <summary>
    /// Gets or sets the correction text (only populated when ShouldCorrect is true).
    /// </summary>
    public string Correction { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the raw response from the AI provider.
    /// </summary>
    public string RawResponse { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AI provider that produced this result.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Creates an IGNORE result (no correction needed).
    /// </summary>
    public static FactCheckResult Ignore(string rawResponse, string providerName) => new()
    {
        ShouldCorrect = false,
        RawResponse = rawResponse,
        ProviderName = providerName
    };

    /// <summary>
    /// Creates a CORRECT result with the correction text.
    /// </summary>
    public static FactCheckResult Correct(string correction, string rawResponse, string providerName) => new()
    {
        ShouldCorrect = true,
        Correction = correction,
        RawResponse = rawResponse,
        ProviderName = providerName
    };
}
