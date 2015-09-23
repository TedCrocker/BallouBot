using System.Collections.Generic;

namespace BallouBot.Data
{
	public interface IDataSource
	{
		IList<Channel> Channels { get; set; }
	}
}