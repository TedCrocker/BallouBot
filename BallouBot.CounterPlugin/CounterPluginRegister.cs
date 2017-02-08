using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using System.IO;
using BallouBot.Data;
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


	


	internal class CounterChannelData : DataEntity
	{
		public IList<CounterData> Counters { get; set; }
	}

	internal class CounterData
	{
		public string Name { get; set; }
		public int Count { get; set; }
	}
	
}
