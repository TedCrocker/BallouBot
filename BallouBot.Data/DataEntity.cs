using System.Collections.Generic;

namespace BallouBot.Data
{
	public abstract class DataEntity
	{
		public string Id { get; set; }
		public IDictionary<string, object> Data;
	}
}