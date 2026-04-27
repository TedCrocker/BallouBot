using BallouBot.Modules.Gif.Models;
using BallouBot.Modules.Gif.Providers;
using BallouBot.Modules.Gif.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace BallouBot.Modules.Gif.Tests;

/// <summary>
/// Tests for the GifProviderFactory class.
/// </summary>
public class GifProviderFactoryTests
{
    private GifProviderFactory CreateFactory()
    {
        var httpClient = new HttpClient();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        return new GifProviderFactory(httpClient, loggerFactory.Object);
    }

    [Test]
    public async Task GetProvider_ReturnsTenorProvider()
    {
        var factory = CreateFactory();

        var provider = factory.GetProvider(GifProviderType.Tenor);

        await Assert.That(provider).IsNotNull();
        await Assert.That(provider.ProviderType).IsEqualTo(GifProviderType.Tenor);
        await Assert.That(provider.DisplayName).IsEqualTo("Tenor");
    }

    [Test]
    public async Task GetProvider_ReturnsGiphyProvider()
    {
        var factory = CreateFactory();

        var provider = factory.GetProvider(GifProviderType.Giphy);

        await Assert.That(provider).IsNotNull();
        await Assert.That(provider.ProviderType).IsEqualTo(GifProviderType.Giphy);
        await Assert.That(provider.DisplayName).IsEqualTo("Giphy");
    }

    [Test]
    public async Task GetProvider_ReturnsRedGifsProvider()
    {
        var factory = CreateFactory();

        var provider = factory.GetProvider(GifProviderType.RedGifs);

        await Assert.That(provider).IsNotNull();
        await Assert.That(provider.ProviderType).IsEqualTo(GifProviderType.RedGifs);
        await Assert.That(provider.DisplayName).IsEqualTo("RedGifs");
    }

    [Test]
    public async Task GetProvider_CachesProviderInstances()
    {
        var factory = CreateFactory();

        var first = factory.GetProvider(GifProviderType.Tenor);
        var second = factory.GetProvider(GifProviderType.Tenor);

        await Assert.That(ReferenceEquals(first, second)).IsTrue();
    }

    [Test]
    public async Task GetProvider_DifferentTypesReturnDifferentInstances()
    {
        var factory = CreateFactory();

        var tenor = factory.GetProvider(GifProviderType.Tenor);
        var giphy = factory.GetProvider(GifProviderType.Giphy);

        await Assert.That(ReferenceEquals(tenor, giphy)).IsFalse();
    }

    [Test]
    public async Task GetSupportedProviders_ReturnsAllProviders()
    {
        var providers = GifProviderFactory.GetSupportedProviders();

        await Assert.That(providers.Count).IsEqualTo(3);
    }

    [Test]
    public async Task GetSupportedProviders_ContainsTenor()
    {
        var providers = GifProviderFactory.GetSupportedProviders();
        var tenor = providers.FirstOrDefault(p => p.Type == GifProviderType.Tenor);

        await Assert.That(tenor.Name).IsEqualTo("Tenor");
        await Assert.That(tenor.RequiresKey).IsTrue();
        await Assert.That(tenor.IsNsfw).IsFalse();
    }

    [Test]
    public async Task GetSupportedProviders_ContainsRedGifsAsNsfw()
    {
        var providers = GifProviderFactory.GetSupportedProviders();
        var redGifs = providers.FirstOrDefault(p => p.Type == GifProviderType.RedGifs);

        await Assert.That(redGifs.Name).IsEqualTo("RedGifs");
        await Assert.That(redGifs.RequiresKey).IsFalse();
        await Assert.That(redGifs.IsNsfw).IsTrue();
    }
}

/// <summary>
/// Tests for the TenorGifProvider class.
/// </summary>
public class TenorGifProviderTests
{
    [Test]
    public async Task TenorProvider_HasCorrectMetadata()
    {
        var httpClient = new HttpClient();
        var logger = new Mock<ILogger<TenorGifProvider>>();
        var provider = new TenorGifProvider(httpClient, logger.Object);

        await Assert.That(provider.ProviderType).IsEqualTo(GifProviderType.Tenor);
        await Assert.That(provider.DisplayName).IsEqualTo("Tenor");
        await Assert.That(provider.RequiresApiKey).IsTrue();
        await Assert.That(provider.IsNsfw).IsFalse();
    }

    [Test]
    public async Task TenorProvider_ReturnsEmptyWhenNoApiKey()
    {
        var httpClient = new HttpClient();
        var logger = new Mock<ILogger<TenorGifProvider>>();
        var provider = new TenorGifProvider(httpClient, logger.Object);

        var results = await provider.SearchAsync("cats", 5, null);

        await Assert.That(results).IsNotNull();
        await Assert.That(results.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TenorProvider_ReturnsEmptyWhenEmptyApiKey()
    {
        var httpClient = new HttpClient();
        var logger = new Mock<ILogger<TenorGifProvider>>();
        var provider = new TenorGifProvider(httpClient, logger.Object);

        var results = await provider.SearchAsync("cats", 5, "");

        await Assert.That(results).IsNotNull();
        await Assert.That(results.Count).IsEqualTo(0);
    }
}

/// <summary>
/// Tests for the GiphyGifProvider class.
/// </summary>
public class GiphyGifProviderTests
{
    [Test]
    public async Task GiphyProvider_HasCorrectMetadata()
    {
        var httpClient = new HttpClient();
        var logger = new Mock<ILogger<GiphyGifProvider>>();
        var provider = new GiphyGifProvider(httpClient, logger.Object);

        await Assert.That(provider.ProviderType).IsEqualTo(GifProviderType.Giphy);
        await Assert.That(provider.DisplayName).IsEqualTo("Giphy");
        await Assert.That(provider.RequiresApiKey).IsTrue();
        await Assert.That(provider.IsNsfw).IsFalse();
    }

    [Test]
    public async Task GiphyProvider_ReturnsEmptyWhenNoApiKey()
    {
        var httpClient = new HttpClient();
        var logger = new Mock<ILogger<GiphyGifProvider>>();
        var provider = new GiphyGifProvider(httpClient, logger.Object);

        var results = await provider.SearchAsync("cats", 5, null);

        await Assert.That(results).IsNotNull();
        await Assert.That(results.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GiphyProvider_ReturnsEmptyWhenEmptyApiKey()
    {
        var httpClient = new HttpClient();
        var logger = new Mock<ILogger<GiphyGifProvider>>();
        var provider = new GiphyGifProvider(httpClient, logger.Object);

        var results = await provider.SearchAsync("cats", 5, "  ");

        await Assert.That(results).IsNotNull();
        await Assert.That(results.Count).IsEqualTo(0);
    }
}

/// <summary>
/// Tests for the RedGifsGifProvider class.
/// </summary>
public class RedGifsGifProviderTests
{
    [Test]
    public async Task RedGifsProvider_HasCorrectMetadata()
    {
        var httpClient = new HttpClient();
        var logger = new Mock<ILogger<RedGifsGifProvider>>();
        var provider = new RedGifsGifProvider(httpClient, logger.Object);

        await Assert.That(provider.ProviderType).IsEqualTo(GifProviderType.RedGifs);
        await Assert.That(provider.DisplayName).IsEqualTo("RedGifs");
        await Assert.That(provider.RequiresApiKey).IsFalse();
        await Assert.That(provider.IsNsfw).IsTrue();
    }
}

/// <summary>
/// Tests for the GifResult model.
/// </summary>
public class GifResultTests
{
    [Test]
    public async Task GifResult_DefaultValues()
    {
        var result = new GifResult();

        await Assert.That(result.Id).IsEqualTo(string.Empty);
        await Assert.That(result.Title).IsEqualTo(string.Empty);
        await Assert.That(result.Url).IsEqualTo(string.Empty);
        await Assert.That(result.PreviewUrl).IsEqualTo(string.Empty);
        await Assert.That(result.Width).IsEqualTo(0);
        await Assert.That(result.Height).IsEqualTo(0);
        await Assert.That(result.ProviderName).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task GifResult_CanSetProperties()
    {
        var result = new GifResult
        {
            Id = "abc123",
            Title = "Funny Cat",
            Url = "https://example.com/cat.gif",
            PreviewUrl = "https://example.com/cat_thumb.gif",
            Width = 480,
            Height = 360,
            ProviderName = "Tenor"
        };

        await Assert.That(result.Id).IsEqualTo("abc123");
        await Assert.That(result.Title).IsEqualTo("Funny Cat");
        await Assert.That(result.Url).IsEqualTo("https://example.com/cat.gif");
        await Assert.That(result.PreviewUrl).IsEqualTo("https://example.com/cat_thumb.gif");
        await Assert.That(result.Width).IsEqualTo(480);
        await Assert.That(result.Height).IsEqualTo(360);
        await Assert.That(result.ProviderName).IsEqualTo("Tenor");
    }
}
