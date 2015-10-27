using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using System.Linq;
using BallouBot.Core;
using BallouBot.Data;
using BallouBotTests.Mocks;
using Xunit;

namespace BallouBotTests
{
	public class MockPluginRegister : IPluginRegister
	{
		public void Register(RegistrationBuilder builder)
		{
			builder.ForType<MockDataSource>()
				.Export<IDataSource>()
				.SetCreationPolicy(CreationPolicy.Shared);
		}
	}


	public class PluginStoreTests
	{
		[Fact]
		public void CanFindPluginRegisters()
		{
            var container =	PluginStore.InitializePluginStoreNew(builder => new AssemblyCatalog(GetType().Assembly, builder));
			var registers = container.GetExports<IPluginRegister>();

			Assert.NotNull(registers);
			Assert.Equal(1, registers.Count());
		}
	}
}