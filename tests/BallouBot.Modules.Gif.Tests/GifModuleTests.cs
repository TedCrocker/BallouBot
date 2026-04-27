using BallouBot.Modules.Gif;

namespace BallouBot.Modules.Gif.Tests;

/// <summary>
/// Tests for the GifModule class metadata.
/// </summary>
public class GifModuleTests
{
    [Test]
    public async Task GifModule_HasCorrectName()
    {
        var module = new GifModule();

        await Assert.That(module.Name).IsEqualTo("GIF");
    }

    [Test]
    public async Task GifModule_HasVersion()
    {
        var module = new GifModule();

        await Assert.That(module.Version).IsNotNull();
        await Assert.That(module.Version.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task GifModule_HasDescription()
    {
        var module = new GifModule();

        await Assert.That(module.Description).IsNotNull();
        await Assert.That(module.Description.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task GifModule_HasBotModuleAttribute()
    {
        var attr = typeof(GifModule)
            .GetCustomAttributes(typeof(BallouBot.Core.BotModuleAttribute), false)
            .FirstOrDefault() as BallouBot.Core.BotModuleAttribute;

        await Assert.That(attr).IsNotNull();
        await Assert.That(attr!.Id).IsEqualTo("gif");
    }

    [Test]
    public async Task GifModule_ImplementsIModule()
    {
        var module = new GifModule();

        await Assert.That(module is BallouBot.Core.IModule).IsTrue();
    }

    [Test]
    public async Task GifModule_ShutdownAsync_DoesNotThrow()
    {
        var module = new GifModule();

        var exception = null as Exception;
        try
        {
            await module.ShutdownAsync();
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        await Assert.That(exception).IsNull();
    }
}
