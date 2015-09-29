using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using BallouBot.Data;
using BallouBot.Interfaces;
using BallouBot.Logging;

namespace BallouBot.Core
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

			builder.ForType<Log>()
				.Export<ILog>()
				.SetCreationPolicy(CreationPolicy.Shared);

			var aggregateCatalog = new AggregateCatalog();

			aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(PluginStore).Assembly, builder));
			aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(IDataSource).Assembly, builder));
			aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(ILog).Assembly, builder));
			
			Container = new CompositionContainer(aggregateCatalog);
		}
	}
}