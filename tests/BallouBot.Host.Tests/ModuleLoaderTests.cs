using System.Reflection;
using BallouBot.Core;
using BallouBot.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace BallouBot.Host.Tests;

/// <summary>
/// Tests for the ModuleLoader class.
/// </summary>
public class ModuleLoaderTests
{
    private ModuleLoader CreateModuleLoader()
    {
        var mockLogger = new Mock<ILogger<ModuleLoader>>();
        return new ModuleLoader(mockLogger.Object);
    }

    [Test]
    public async Task DiscoverModuleTypes_FindsModulesInAssembly()
    {
        var loader = CreateModuleLoader();

        // The Welcome module assembly should be referenced
        var assemblies = new[] { typeof(BallouBot.Modules.Welcome.WelcomeModule).Assembly };

        var types = loader.DiscoverModuleTypes(assemblies);

        await Assert.That(types.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(types.Any(t => t.Name == "WelcomeModule")).IsTrue();
    }

    [Test]
    public async Task DiscoverModuleTypes_IgnoresClassesWithoutBotModuleAttribute()
    {
        var loader = CreateModuleLoader();
        var assemblies = new[] { typeof(ModuleLoaderTests).Assembly };

        var types = loader.DiscoverModuleTypes(assemblies);

        // This test assembly has no modules
        await Assert.That(types.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DiscoverModuleTypes_IgnoresAbstractClasses()
    {
        var loader = CreateModuleLoader();
        // Even with our own assembly, abstract classes should be skipped
        var assemblies = new[] { typeof(ModuleLoaderTests).Assembly };

        var types = loader.DiscoverModuleTypes(assemblies);

        await Assert.That(types.All(t => !t.IsAbstract)).IsTrue();
    }

    [Test]
    public async Task DiscoverModuleTypes_HandlesEmptyAssemblyList()
    {
        var loader = CreateModuleLoader();

        var types = loader.DiscoverModuleTypes(Array.Empty<Assembly>());

        await Assert.That(types.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RegisterModuleServices_RegistersModuleAsSingleton()
    {
        var loader = CreateModuleLoader();
        var services = new ServiceCollection();
        var moduleTypes = new[] { typeof(BallouBot.Modules.Welcome.WelcomeModule) };

        loader.RegisterModuleServices(services, moduleTypes);

        // Check that IModule is registered
        var descriptor = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(IModule));

        await Assert.That(descriptor).IsNotNull();
        await Assert.That(descriptor!.Lifetime).IsEqualTo(ServiceLifetime.Singleton);
    }

    [Test]
    public async Task LoadAssembliesFromDirectory_ReturnsEmptyForNonExistentDir()
    {
        var loader = CreateModuleLoader();

        var assemblies = loader.LoadAssembliesFromDirectory(@"C:\NonExistent\Path\12345");

        await Assert.That(assemblies.Count).IsEqualTo(0);
    }

    [Test]
    public async Task InitializeModulesAsync_InitializesModules()
    {
        var loader = CreateModuleLoader();
        var mockContext = new Mock<IModuleContext>();
        var mockModule = new Mock<IModule>();
        mockModule.Setup(m => m.Name).Returns("TestModule");
        mockModule.Setup(m => m.Version).Returns("1.0.0");
        mockModule.Setup(m => m.InitializeAsync(It.IsAny<IModuleContext>())).Returns(Task.CompletedTask);

        await loader.InitializeModulesAsync(mockContext.Object, new[] { mockModule.Object });

        await Assert.That(loader.Modules.Count).IsEqualTo(1);
        mockModule.Verify(m => m.InitializeAsync(mockContext.Object), Times.Once);
    }

    [Test]
    public async Task ShutdownModulesAsync_ShutsDownAllModules()
    {
        var loader = CreateModuleLoader();
        var mockContext = new Mock<IModuleContext>();
        var mockModule = new Mock<IModule>();
        mockModule.Setup(m => m.Name).Returns("TestModule");
        mockModule.Setup(m => m.Version).Returns("1.0.0");
        mockModule.Setup(m => m.InitializeAsync(It.IsAny<IModuleContext>())).Returns(Task.CompletedTask);
        mockModule.Setup(m => m.ShutdownAsync()).Returns(Task.CompletedTask);

        await loader.InitializeModulesAsync(mockContext.Object, new[] { mockModule.Object });
        await loader.ShutdownModulesAsync();

        await Assert.That(loader.Modules.Count).IsEqualTo(0);
        mockModule.Verify(m => m.ShutdownAsync(), Times.Once);
    }

    [Test]
    public async Task InitializeModulesAsync_ContinuesAfterModuleFailure()
    {
        var loader = CreateModuleLoader();
        var mockContext = new Mock<IModuleContext>();

        var failingModule = new Mock<IModule>();
        failingModule.Setup(m => m.Name).Returns("FailingModule");
        failingModule.Setup(m => m.InitializeAsync(It.IsAny<IModuleContext>()))
            .ThrowsAsync(new Exception("Module init failed"));

        var successModule = new Mock<IModule>();
        successModule.Setup(m => m.Name).Returns("SuccessModule");
        successModule.Setup(m => m.Version).Returns("1.0.0");
        successModule.Setup(m => m.InitializeAsync(It.IsAny<IModuleContext>())).Returns(Task.CompletedTask);

        await loader.InitializeModulesAsync(mockContext.Object, new[] { failingModule.Object, successModule.Object });

        // Only the successful module should be in the loaded list
        await Assert.That(loader.Modules.Count).IsEqualTo(1);
        await Assert.That(loader.Modules[0].Name).IsEqualTo("SuccessModule");
    }
}
