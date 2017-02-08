using System;

namespace BallouBot.CounterPlugin
{
	internal class CounterData
	{
		public string Name { get; set; }
		public int Count { get; set; }
		public DateTime LastUpdate { get; set; }
	}
}