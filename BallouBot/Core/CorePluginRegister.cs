using System.Collections.Generic;
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
	public class CorePluginRegister : IPluginRegister
	{
		public IList<AssemblyCatalog> Register(RegistrationBuilder builder)
		{
			builder.ForType<CommandQueue>()
				.Export<ICommandQueue>()
				.SetCreationPolicy(CreationPolicy.Shared);

			builder.ForTypesDerivedFrom<IChatParser>()
				.Export<IChatParser>()
				.SelectConstructor(cinfo => cinfo[0]);

			builder.ForType<Config.Config>()
				.Export<IConfig>()
				.SetCreationPolicy(CreationPolicy.Shared);

			builder.ForType<DataSource>()
				.Export<IDataSource>()
				.SetCreationPolicy(CreationPolicy.Shared);

			builder.ForType<Log>()
				.Export<ILog>()
				.SetCreationPolicy(CreationPolicy.Shared);

			builder.ForType<TwitchApi>()
				.Export<ITwitchApi>()
				.SetCreationPolicy(CreationPolicy.Shared);

			var catalogs = new List<AssemblyCatalog>();

			catalogs.Add(new AssemblyCatalog(typeof(PluginStore).Assembly, builder));
			catalogs.Add(new AssemblyCatalog(typeof(IDataSource).Assembly, builder));
			catalogs.Add(new AssemblyCatalog(typeof(ILog).Assembly, builder));
			catalogs.Add(new AssemblyCatalog(typeof(ITwitchApi).Assembly, builder));
			catalogs.Add(new AssemblyCatalog(typeof(IConfig).Assembly, builder));
			return catalogs;
		}
	}
}