using System.Collections.Generic;

namespace BallouBot.Data
{
	public class Channel : DataEntity
	{
		public string Name { get; set; }
		public IList<User> Users { get; set; }
	}
}