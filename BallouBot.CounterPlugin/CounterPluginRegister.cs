using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using BallouBot.Interfaces;

namespace BallouBot.CounterPlugin
{
    public class CounterPluginRegister : IPluginRegister
	{
		public IList<AssemblyCatalog> Register(RegistrationBuilder builder)
		{

			return new List<AssemblyCatalog>() { new AssemblyCatalog(GetType().Assembly, builder) };

		}

	}
}
