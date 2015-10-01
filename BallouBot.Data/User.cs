using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BallouBot.Data
{
	public class User : DataEntity
	{
		public string Name { get; set; }
		public IDictionary<string, UserChannel> Channels;
	}

	public class UserChannel
	{
		public string Name { get; set; }
		public bool IsModerator { get; set; }
	}
}
