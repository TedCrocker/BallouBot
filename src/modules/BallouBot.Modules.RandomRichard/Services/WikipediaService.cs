using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BallouBot.Modules.RandomRichard.Models;
using Microsoft.Extensions.Logging;

namespace BallouBot.Modules.RandomRichard.Services;

/// <summary>
/// Service for fetching information about people named Richard from Wikipedia.
/// Uses the Wikipedia REST API (no API key required).
/// </summary>
public class WikipediaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WikipediaService> _logger;
    private readonly Random _random = new();

    /// <summary>
    /// A curated list of famous people named Richard to pull from Wikipedia.
    /// </summary>
    private static readonly string[] FamousRichards =
    [
        "Richard_Nixon",
        "Richard_Feynman",
        "Richard_Branson",
        "Richard_Pryor",
        "Richard_Simmons",
        "Richard_Wagner",
        "Richard_Dawkins",
        "Richard_Gere",
        "Richard_Harris",
        "Richard_Burton",
        "Richard_Dreyfuss",
        "Richard_Attenborough",
        "Richard_Petty",
        "Richard_Stallman",
        "Richard_the_Lionheart",
        "Richard_III_of_England",
        "Richard_II_of_England",
        "Richard_Arkwright",
        "Richard_Strauss",
        "Richard_Wright_(author)",
        "Richard_Wright_(musician)",
        "Richard_Rodgers",
        "Richard_Roundtree",
        "Richard_Dean_Anderson",
        "Richard_Chamberlain",
        "Richard_Widmark",
        "Richard_Basehart",
        "Richard_Boone",
        "Richard_Crenna",
        "Richard_Dysart",
        "Richard_Farnsworth",
        "Richard_Kiel",
        "Richard_Lewis_(comedian)",
        "Richard_Belzer",
        "Richard_Kind",
        "Richard_Schiff",
        "Richard_Jenkins",
        "Richard_E._Grant",
        "Richard_Griffiths",
        "Richard_Ayoade",
        "Richard_Hammond",
        "Richard_Osman",
        "Richard_Madden",
        "Richard_Armitage_(actor)",
        "Richard_Linklater",
        "Richard_Donner",
        "Richard_Curtis",
        "Richard_Kelly_(filmmaker)",
        "Richard_Matheson",
        "Richard_Adams",
        "Richard_Bach",
        "Richard_Brautigan",
        "Richard_Ford",
        "Richard_Russo",
        "Richard_Price_(writer)",
        "Richard_Yates_(novelist)",
        "Richard_Powers",
        "Richard_Scarry",
        "Richard_Avedon",
        "Richard_Serra",
        "Richard_Meier",
        "Richard_Rogers",
        "Richard_Buckminster_Fuller",
        "Richard_Garriott",
        "Richard_Hendricks",
        "Richard_Cheese",
        "Richard_Thompson_(musician)",
        "Richard_Marx",
        "Richard_Ashcroft",
        "Richard_Patrick",
        "Richard_D._James",
        "Little_Richard",
        "Richard_Ramirez",
        "Richard_Kuklinski",
        "Richard_Childress",
        "Richard_Sherman",
        "Richard_Jefferson",
        "Richard_Hamilton_(basketball)",
        "Richard_Hadlee",
        "Richard_Krajicek",
        "Richard_Gasquet",
        "Richard_Sears_(tennis)",
        "Richard_Leakey",
        "Richard_Thaler",
        "Richard_Hamming",
        "Richard_Bellman",
        "Richard_Karp",
        "Richard_Smalley",
        "Richard_Ernst",
        "Richard_Axel",
        "Richard_Roberts_(biochemist)",
        "Richard_Heck",
        "Richard_Schrock",
        "Richard_Henderson_(biologist)"
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="WikipediaService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for Wikipedia API requests.</param>
    /// <param name="logger">The logger instance.</param>
    public WikipediaService(HttpClient httpClient, ILogger<WikipediaService> logger)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("BallouBot/1.0 (Discord Bot; Random Richard Module)");
        _logger = logger;
    }

    /// <summary>
    /// Fetches a random Richard from Wikipedia.
    /// </summary>
    /// <returns>A <see cref="RichardInfo"/> with the person's details, or null if the fetch failed.</returns>
    public async Task<RichardInfo?> GetRandomRichardAsync()
    {
        // Pick a random Richard from the curated list
        var articleTitle = FamousRichards[_random.Next(FamousRichards.Length)];

        try
        {
            return await FetchRichardFromWikipediaAsync(articleTitle);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Richard '{Article}' from Wikipedia, trying another...", articleTitle);

            // Try a second random pick as fallback
            try
            {
                var fallbackTitle = FamousRichards[_random.Next(FamousRichards.Length)];
                return await FetchRichardFromWikipediaAsync(fallbackTitle);
            }
            catch (Exception ex2)
            {
                _logger.LogError(ex2, "Failed to fetch fallback Richard from Wikipedia.");
                return null;
            }
        }
    }

    /// <summary>
    /// Fetches a specific Richard from Wikipedia by article title.
    /// Exposed for testing and preview functionality.
    /// </summary>
    /// <param name="articleTitle">The Wikipedia article title (e.g., "Richard_Feynman").</param>
    /// <returns>A <see cref="RichardInfo"/> with the person's details, or null if not found.</returns>
    public async Task<RichardInfo?> FetchRichardFromWikipediaAsync(string articleTitle)
    {
        var url = $"https://en.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(articleTitle)}";

        _logger.LogDebug("Fetching Wikipedia summary for '{Article}'", articleTitle);

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Wikipedia API returned {StatusCode} for '{Article}'", response.StatusCode, articleTitle);
            return null;
        }

        var wikiResponse = await response.Content.ReadFromJsonAsync<WikipediaSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (wikiResponse is null)
        {
            _logger.LogWarning("Failed to deserialize Wikipedia response for '{Article}'", articleTitle);
            return null;
        }

        var richard = new RichardInfo
        {
            Name = wikiResponse.Title ?? articleTitle.Replace("_", " "),
            Summary = TruncateSummary(wikiResponse.Extract ?? "No summary available."),
            ImageUrl = wikiResponse.Thumbnail?.Source ?? wikiResponse.OriginalImage?.Source,
            WikipediaUrl = wikiResponse.ContentUrls?.Desktop?.Page
                ?? $"https://en.wikipedia.org/wiki/{articleTitle}"
        };

        _logger.LogDebug("Fetched Richard: {Name} (image: {HasImage})", richard.Name, richard.ImageUrl != null);

        return richard;
    }

    /// <summary>
    /// Gets the list of famous Richards article titles. Useful for testing.
    /// </summary>
    public static IReadOnlyList<string> GetFamousRichardsList() => FamousRichards;

    /// <summary>
    /// Truncates a summary to fit within Discord embed limits (max 2048 chars for description).
    /// </summary>
    private static string TruncateSummary(string summary)
    {
        const int maxLength = 1024;
        if (summary.Length <= maxLength) return summary;
        return summary[..(maxLength - 3)] + "...";
    }

    // Wikipedia REST API response models

    internal class WikipediaSummaryResponse
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("extract")]
        public string? Extract { get; set; }

        [JsonPropertyName("thumbnail")]
        public WikipediaImage? Thumbnail { get; set; }

        [JsonPropertyName("originalimage")]
        public WikipediaImage? OriginalImage { get; set; }

        [JsonPropertyName("content_urls")]
        public WikipediaContentUrls? ContentUrls { get; set; }
    }

    internal class WikipediaImage
    {
        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    internal class WikipediaContentUrls
    {
        [JsonPropertyName("desktop")]
        public WikipediaUrlSet? Desktop { get; set; }
    }

    internal class WikipediaUrlSet
    {
        [JsonPropertyName("page")]
        public string? Page { get; set; }
    }
}
