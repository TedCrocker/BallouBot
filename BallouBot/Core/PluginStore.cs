using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using BallouBot.Data;
using BallouBot.Interfaces;
using BallouBot.Logging;

namespace BallouBot.Core
{
	public static class PluginStore
	{
		public static CompositionContainer Container;

		public static void InitializePluginStore()
		{
			InitializePluginStore((builder, aggregateCatalog) =>
			{
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

				aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(PluginStore).Assembly, builder));
				aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(IDataSource).Assembly, builder));
				aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(ILog).Assembly, builder));
			});
		}

		public static void InitializePluginStore(Action<RegistrationBuilder, AggregateCatalog> buildCatalog)
		{
			var builder = new RegistrationBuilder();
			var aggregateCatalog = new AggregateCatalog();
			buildCatalog(builder, aggregateCatalog);
			Container = new CompositionContainer(aggregateCatalog);
		}
	}
}