using System.Collections.Generic;

namespace BallouBot.Data
{
	public class DataSource : IDataSource
	{
		public IList<Channel> Channels { get; set; } 
	}
}