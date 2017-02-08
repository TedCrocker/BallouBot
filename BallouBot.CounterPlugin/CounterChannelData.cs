using System.Collections.Generic;
using BallouBot.Data;

namespace BallouBot.CounterPlugin
{
	internal class CounterChannelData : DataEntity
	{
		public IList<CounterData> Counters { get; set; }
	}
}