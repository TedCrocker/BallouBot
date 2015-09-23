using System.Collections.Generic;
using System.Linq;

namespace BallouBot
{
	public class Message
	{
		public string RawMessage { get; set; }
		public string Prefix { get; set; }
		public string Suffix { get; set; }
		public IEnumerable<string> Parameters { get; set; }
		public string Command { get; set; }

		private string _userName;
		public string User
		{
			get
			{
				if (string.IsNullOrWhiteSpace(_userName))
				{
					var split = Prefix.Split('!').First();
					_userName = split.Substring(0);
				}

				return _userName;
			}
		}

		public string Channel
		{
			get { return Parameters.First(); }
		}
	}
}