using System.ComponentModel.Composition.Hosting;
using System.Linq;
using BallouBot.Core;
using BallouBot.Data;
using BallouBot.Interfaces;
using Xunit;

namespace BallouBotTests
{
	public class PluginStoreTests
	{
		[Fact]
		public void CanFindPluginRegister()
		{
            var container =	PluginStore.InitializePluginStoreNew(builder => new AssemblyCatalog(GetType().Assembly, builder));
			var registers = container.GetExports<IPluginRegister>();

			Assert.NotNull(registers);
			Assert.Equal(2, registers.Count());
		}

		[Fact]
		public void CanExecutePluginRegister()
		{
			PluginStore.InitializePluginStoreNew(builder => new AssemblyCatalog(GetType().Assembly, builder));
			var dataSource = PluginStore.Container.GetExport<IDataSource>();
			Assert.NotNull(dataSource);
			Assert.NotNull(dataSource.Value);
		}
	}
}