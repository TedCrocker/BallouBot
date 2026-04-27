using BallouBot.Modules.FactCheck.Providers;
using BallouBot.Modules.FactCheck.Models;

namespace BallouBot.Modules.FactCheck.Tests;

/// <summary>
/// Tests for FactCheckPrompt parsing logic and FactCheckResult model.
/// </summary>
public class FactCheckPromptTests
{
    [Test]
    public async Task ParseResponse_IgnoreResponse_ReturnsIgnoreResult()
    {
        var result = FactCheckPrompt.ParseResponse("IGNORE", "TestProvider");
        await Assert.That(result.ShouldCorrect).IsFalse();
        await Assert.That(result.ProviderName).IsEqualTo("TestProvider");
    }

    [Test]
    public async Task ParseResponse_CorrectResponse_ReturnsCorrectResult()
    {
        var result = FactCheckPrompt.ParseResponse("CORRECT: The Earth orbits the Sun, not the other way around.", "OpenAI");
        await Assert.That(result.ShouldCorrect).IsTrue();
        await Assert.That(result.Correction).IsEqualTo("The Earth orbits the Sun, not the other way around.");
        await Assert.That(result.ProviderName).IsEqualTo("OpenAI");
    }

    [Test]
    public async Task ParseResponse_EmptyResponse_ReturnsIgnore()
    {
        var result = FactCheckPrompt.ParseResponse("", "TestProvider");
        await Assert.That(result.ShouldCorrect).IsFalse();
    }

    [Test]
    public async Task ParseResponse_WhitespaceResponse_ReturnsIgnore()
    {
        var result = FactCheckPrompt.ParseResponse("   ", "TestProvider");
        await Assert.That(result.ShouldCorrect).IsFalse();
    }

    [Test]
    public async Task ParseResponse_CaseInsensitiveCorrect()
    {
        var result = FactCheckPrompt.ParseResponse("correct: Actually, water boils at 100°C.", "Anthropic");
        await Assert.That(result.ShouldCorrect).IsTrue();
        await Assert.That(result.Correction).IsEqualTo("Actually, water boils at 100°C.");
    }

    [Test]
    public async Task ParseResponse_CorrectWithNoText_ReturnsIgnore()
    {
        var result = FactCheckPrompt.ParseResponse("CORRECT:   ", "TestProvider");
        await Assert.That(result.ShouldCorrect).IsFalse();
    }

    [Test]
    public async Task ParseResponse_RandomText_ReturnsIgnore()
    {
        var result = FactCheckPrompt.ParseResponse("This is just random text from the AI", "TestProvider");
        await Assert.That(result.ShouldCorrect).IsFalse();
    }

    [Test]
    public async Task ParseResponse_CorrectWithLeadingWhitespace()
    {
        var result = FactCheckPrompt.ParseResponse("  CORRECT: The capital of France is Paris.", "OpenAI");
        await Assert.That(result.ShouldCorrect).IsTrue();
        await Assert.That(result.Correction).IsEqualTo("The capital of France is Paris.");
    }

    [Test]
    public async Task FactCheckResult_IgnoreFactory()
    {
        var result = FactCheckResult.Ignore("raw response", "TestProvider");
        await Assert.That(result.ShouldCorrect).IsFalse();
        await Assert.That(result.Correction).IsEqualTo(string.Empty);
        await Assert.That(result.RawResponse).IsEqualTo("raw response");
        await Assert.That(result.ProviderName).IsEqualTo("TestProvider");
    }

    [Test]
    public async Task FactCheckResult_CorrectFactory()
    {
        var result = FactCheckResult.Correct("correction text", "raw response", "OpenAI");
        await Assert.That(result.ShouldCorrect).IsTrue();
        await Assert.That(result.Correction).IsEqualTo("correction text");
        await Assert.That(result.RawResponse).IsEqualTo("raw response");
        await Assert.That(result.ProviderName).IsEqualTo("OpenAI");
    }

    [Test]
    public async Task SystemPrompt_IsNotEmpty()
    {
        await Assert.That(FactCheckPrompt.SystemPrompt.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task UserPrompt_ContainsPlaceholder()
    {
        await Assert.That(FactCheckPrompt.UserPrompt.Contains("{0}")).IsTrue();
    }
}

/// <summary>
/// Tests for AiProviderFactory static methods.
/// </summary>
public class AiProviderFactoryTests
{
    [Test]
    public async Task ParseProviderName_OpenAI()
    {
        var type = BallouBot.Modules.FactCheck.Services.AiProviderFactory.ParseProviderName("openai");
        await Assert.That(type).IsEqualTo(AiProviderType.OpenAI);
    }

    [Test]
    public async Task ParseProviderName_OpenAI_MixedCase()
    {
        var type = BallouBot.Modules.FactCheck.Services.AiProviderFactory.ParseProviderName("OpenAI");
        await Assert.That(type).IsEqualTo(AiProviderType.OpenAI);
    }

    [Test]
    public async Task ParseProviderName_Anthropic()
    {
        var type = BallouBot.Modules.FactCheck.Services.AiProviderFactory.ParseProviderName("anthropic");
        await Assert.That(type).IsEqualTo(AiProviderType.Anthropic);
    }

    [Test]
    public async Task ParseProviderName_Claude_Alias()
    {
        var type = BallouBot.Modules.FactCheck.Services.AiProviderFactory.ParseProviderName("claude");
        await Assert.That(type).IsEqualTo(AiProviderType.Anthropic);
    }

    [Test]
    public async Task ParseProviderName_AzureOpenAI()
    {
        var type = BallouBot.Modules.FactCheck.Services.AiProviderFactory.ParseProviderName("azureopenai");
        await Assert.That(type).IsEqualTo(AiProviderType.AzureOpenAI);
    }

    [Test]
    public async Task ParseProviderName_Azure_Alias()
    {
        var type = BallouBot.Modules.FactCheck.Services.AiProviderFactory.ParseProviderName("azure");
        await Assert.That(type).IsEqualTo(AiProviderType.AzureOpenAI);
    }

    [Test]
    public async Task ParseProviderName_Invalid_Throws()
    {
        Exception? caught = null;
        try { BallouBot.Modules.FactCheck.Services.AiProviderFactory.ParseProviderName("invalid"); }
        catch (ArgumentException ex) { caught = ex; }
        await Assert.That(caught).IsNotNull();
    }

    [Test]
    public async Task GetSupportedProviders_ReturnsThreeProviders()
    {
        var providers = BallouBot.Modules.FactCheck.Services.AiProviderFactory.GetSupportedProviders();
        await Assert.That(providers.Count).IsEqualTo(3);
    }

    [Test]
    public async Task GetSupportedProviders_ContainsOpenAI()
    {
        var providers = BallouBot.Modules.FactCheck.Services.AiProviderFactory.GetSupportedProviders();
        var hasOpenAi = providers.Any(p => p.Type == AiProviderType.OpenAI);
        await Assert.That(hasOpenAi).IsTrue();
    }

    [Test]
    public async Task GetSupportedProviders_AzureRequiresEndpoint()
    {
        var providers = BallouBot.Modules.FactCheck.Services.AiProviderFactory.GetSupportedProviders();
        var azure = providers.First(p => p.Type == AiProviderType.AzureOpenAI);
        await Assert.That(azure.RequiresEndpoint).IsTrue();
    }
}

/// <summary>
/// Tests for the FactCheckService ShouldCheck logic.
/// </summary>
public class FactCheckServiceTests
{
    [Test]
    public async Task ShouldCheck_DisabledConfig_ReturnsFalse()
    {
        var factory = CreateFactory();
        var service = new BallouBot.Modules.FactCheck.Services.FactCheckService(factory, CreateLogger());
        var config = new BallouBot.Core.Entities.FactCheckConfig { IsEnabled = false, ApiKey = "key" };

        var result = service.ShouldCheck(1, 1, "This is a test message that is long enough", config);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ShouldCheck_NoApiKey_ReturnsFalse()
    {
        var factory = CreateFactory();
        var service = new BallouBot.Modules.FactCheck.Services.FactCheckService(factory, CreateLogger());
        var config = new BallouBot.Core.Entities.FactCheckConfig { IsEnabled = true, ApiKey = null };

        var result = service.ShouldCheck(1, 1, "This is a test message that is long enough", config);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ShouldCheck_ShortMessage_ReturnsFalse()
    {
        var factory = CreateFactory();
        var service = new BallouBot.Modules.FactCheck.Services.FactCheckService(factory, CreateLogger());
        var config = new BallouBot.Core.Entities.FactCheckConfig { IsEnabled = true, ApiKey = "key", MinMessageLength = 20 };

        var result = service.ShouldCheck(1, 1, "short", config);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ShouldCheck_CommandMessage_ReturnsFalse()
    {
        var factory = CreateFactory();
        var service = new BallouBot.Modules.FactCheck.Services.FactCheckService(factory, CreateLogger());
        var config = new BallouBot.Core.Entities.FactCheckConfig { IsEnabled = true, ApiKey = "key", MinMessageLength = 5 };

        var result = service.ShouldCheck(1, 1, "/factcheck status check", config);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ShouldCheck_ValidMessage_ReturnsTrue()
    {
        var factory = CreateFactory();
        var service = new BallouBot.Modules.FactCheck.Services.FactCheckService(factory, CreateLogger());
        var config = new BallouBot.Core.Entities.FactCheckConfig { IsEnabled = true, ApiKey = "key", MinMessageLength = 5 };

        var result = service.ShouldCheck(1, 1, "The earth is flat and water is not wet", config);
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task RecordCheck_EnforcesCooldown()
    {
        var factory = CreateFactory();
        var service = new BallouBot.Modules.FactCheck.Services.FactCheckService(factory, CreateLogger());
        var config = new BallouBot.Core.Entities.FactCheckConfig { IsEnabled = true, ApiKey = "key", MinMessageLength = 5, CooldownSeconds = 60 };

        service.RecordCheck(1, 1);
        var result = service.ShouldCheck(1, 1, "The earth is flat and water is not wet", config);
        await Assert.That(result).IsFalse();
    }

    private static BallouBot.Modules.FactCheck.Services.AiProviderFactory CreateFactory()
    {
        return new BallouBot.Modules.FactCheck.Services.AiProviderFactory(
            new HttpClient(),
            new Moq.Mock<Microsoft.Extensions.Logging.ILoggerFactory>().Object);
    }

    private static Microsoft.Extensions.Logging.ILogger<BallouBot.Modules.FactCheck.Services.FactCheckService> CreateLogger()
    {
        return new Moq.Mock<Microsoft.Extensions.Logging.ILogger<BallouBot.Modules.FactCheck.Services.FactCheckService>>().Object;
    }
}
