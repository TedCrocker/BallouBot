using BallouBot.Modules.FactCheck;

namespace BallouBot.Modules.FactCheck.Tests;

/// <summary>
/// Tests for the FactCheckModule class metadata.
/// </summary>
public class FactCheckModuleTests
{
    [Test]
    public async Task FactCheckModule_HasCorrectName()
    {
        var module = new FactCheckModule();
        await Assert.That(module.Name).IsEqualTo("Fact Check");
    }

    [Test]
    public async Task FactCheckModule_HasVersion()
    {
        var module = new FactCheckModule();
        await Assert.That(module.Version).IsNotNull();
        await Assert.That(module.Version.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task FactCheckModule_HasDescription()
    {
        var module = new FactCheckModule();
        await Assert.That(module.Description).IsNotNull();
        await Assert.That(module.Description.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task FactCheckModule_HasBotModuleAttribute()
    {
        var attr = typeof(FactCheckModule)
            .GetCustomAttributes(typeof(BallouBot.Core.BotModuleAttribute), false)
            .FirstOrDefault() as BallouBot.Core.BotModuleAttribute;

        await Assert.That(attr).IsNotNull();
        await Assert.That(attr!.Id).IsEqualTo("factcheck");
    }

    [Test]
    public async Task FactCheckModule_ImplementsIModule()
    {
        var module = new FactCheckModule();
        await Assert.That(module is BallouBot.Core.IModule).IsTrue();
    }

    [Test]
    public async Task FactCheckModule_ShutdownAsync_DoesNotThrow()
    {
        var module = new FactCheckModule();
        Exception? exception = null;
        try { await module.ShutdownAsync(); }
        catch (Exception ex) { exception = ex; }
        await Assert.That(exception).IsNull();
    }
}
