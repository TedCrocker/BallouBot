using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using BallouBot.Core;
using BallouBot.Interfaces;

namespace BallouBot.PollPlugin
{
	public class PollPluginRegister : IPluginRegister
	{
		public IList<AssemblyCatalog> Register(RegistrationBuilder builder)
		{
			builder
				.ForType<StrawPoll>()
				.Export<IPoll>();
			return new List<AssemblyCatalog>() { new AssemblyCatalog(GetType().Assembly, builder)};
		}
	}
}