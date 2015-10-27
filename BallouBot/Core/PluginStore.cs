﻿using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.Registration;
using BallouBot.Config;
using BallouBot.Data;
using BallouBot.Interfaces;
using BallouBot.Logging;
using BallouBot.Twitch;

namespace BallouBot.Core
{
	public static class PluginStore
	{
		public static CompositionContainer Container;

		public static void InitializePluginStore()
		{
			InitializePluginStore((builder, aggregateCatalog) =>
			{
				builder.ForType<Config.Config>()
					.Export<IConfig>()
					.SetCreationPolicy(CreationPolicy.Shared);

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

				builder.ForType<TwitchApi>()
					.Export<ITwitchApi>()
					.SetCreationPolicy(CreationPolicy.Shared);

				aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof (PluginStore).Assembly, builder));
				aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof (IDataSource).Assembly, builder));
				aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof (ILog).Assembly, builder));
				aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof (ITwitchApi).Assembly, builder));
				aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof (IConfig).Assembly, builder));
			});
		}

		public static void InitializePluginStore(Action<RegistrationBuilder, AggregateCatalog> buildCatalog)
		{
			var builder = new RegistrationBuilder();
			var aggregateCatalog = new AggregateCatalog();
			buildCatalog(builder, aggregateCatalog);
			Container = new CompositionContainer(aggregateCatalog);
		}

		public static CompositionContainer InitializePluginStoreNew(Func<RegistrationBuilder, ComposablePartCatalog> addAssemblies = null)
		{
			var builder = new RegistrationBuilder();
			ComposablePartCatalog catalog = null;
			if (addAssemblies == null)
			{
				catalog = new DirectoryCatalog(".", builder);
			}
			else
			{
				catalog = addAssemblies(builder);
			}
			
			builder.ForTypesDerivedFrom<IPluginRegister>().Export<IPluginRegister>().SelectConstructor(cinfo => cinfo[0]);
			
			var container = new CompositionContainer(catalog);
			return container;
		}
}
}