using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using System.Runtime.InteropServices.ComTypes;
using BallouBot.Data;

namespace BallouBot
{
	public class PluginStore
	{
		public static CompositionContainer Container;

		public PluginStore()
		{
			var builder = new RegistrationBuilder();
			builder.ForType<CommandQueue>()
				.Export<ICommandQueue>()
				.SetCreationPolicy(CreationPolicy.Shared);

			builder.ForType<DataSource>()
				.Export<IDataSource>()
				.SetCreationPolicy(CreationPolicy.Shared);

			builder.ForTypesDerivedFrom<IChatParser>()
				.Export<IChatParser>()
				.SelectConstructor(cinfo => cinfo[0]);

			var aggregateCatalog = new AggregateCatalog();

			aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(PluginStore).Assembly, builder));
			aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(IDataSource).Assembly, builder));
			
			Container = new CompositionContainer(aggregateCatalog);
		}
	}
}