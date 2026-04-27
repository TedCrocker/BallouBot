using BallouBot.Modules.Help;

namespace BallouBot.Modules.Help.Tests;

/// <summary>
/// Tests for the HelpModule class metadata.
/// </summary>
public class HelpModuleTests
{
    [Test]
    public async Task HelpModule_HasCorrectName()
    {
        var module = new HelpModule();
        await Assert.That(module.Name).IsEqualTo("Help");
    }

    [Test]
    public async Task HelpModule_HasVersion()
    {
        var module = new HelpModule();
        await Assert.That(module.Version).IsNotNull();
        await Assert.That(module.Version.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task HelpModule_HasDescription()
    {
        var module = new HelpModule();
        await Assert.That(module.Description).IsNotNull();
        await Assert.That(module.Description.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task HelpModule_HasBotModuleAttribute()
    {
        var attr = typeof(HelpModule)
            .GetCustomAttributes(typeof(BallouBot.Core.BotModuleAttribute), false)
            .FirstOrDefault() as BallouBot.Core.BotModuleAttribute;

        await Assert.That(attr).IsNotNull();
        await Assert.That(attr!.Id).IsEqualTo("help");
    }

    [Test]
    public async Task HelpModule_ImplementsIModule()
    {
        var module = new HelpModule();
        await Assert.That(module is BallouBot.Core.IModule).IsTrue();
    }

    [Test]
    public async Task HelpModule_ShutdownAsync_DoesNotThrow()
    {
        var module = new HelpModule();
        Exception? exception = null;
        try { await module.ShutdownAsync(); }
        catch (Exception ex) { exception = ex; }
        await Assert.That(exception).IsNull();
    }
}

/// <summary>
/// Tests for the ChunkText utility method.
/// </summary>
public class ChunkTextTests
{
    [Test]
    public async Task ChunkText_EmptyList_ReturnsEmpty()
    {
        var result = HelpModule.ChunkText(new List<string>(), 100);
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ChunkText_SingleShortLine_ReturnsSingleChunk()
    {
        var lines = new List<string> { "Hello world" };
        var result = HelpModule.ChunkText(lines, 100);
        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0]).IsEqualTo("Hello world");
    }

    [Test]
    public async Task ChunkText_MultipleShortLines_FitInOneChunk()
    {
        var lines = new List<string> { "Line 1", "Line 2", "Line 3" };
        var result = HelpModule.ChunkText(lines, 100);
        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ChunkText_LongLines_SplitIntoMultipleChunks()
    {
        var lines = new List<string>
        {
            new('A', 40),
            new('B', 40),
            new('C', 40)
        };
        var result = HelpModule.ChunkText(lines, 50);
        await Assert.That(result.Count).IsEqualTo(3);
    }

    [Test]
    public async Task ChunkText_ExactFit_SingleChunk()
    {
        var lines = new List<string> { "12345", "67890" };
        // "12345\r\n67890" = 12 chars on Windows, but the method uses AppendLine + Append
        var result = HelpModule.ChunkText(lines, 20);
        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ChunkText_AllChunksUnderMaxLength()
    {
        var lines = new List<string>();
        for (var i = 0; i < 20; i++)
            lines.Add($"Command {i}: This is a description of command {i}");

        var result = HelpModule.ChunkText(lines, 200);

        foreach (var chunk in result)
        {
            await Assert.That(chunk.Length).IsLessThanOrEqualTo(200);
        }
    }

    [Test]
    public async Task ChunkText_PreservesAllContent()
    {
        var lines = new List<string> { "Alpha", "Beta", "Gamma", "Delta" };
        var result = HelpModule.ChunkText(lines, 15);

        var allContent = string.Join("", result);
        foreach (var line in lines)
        {
            await Assert.That(allContent.Contains(line)).IsTrue();
        }
    }
}
