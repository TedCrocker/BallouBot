using System.Collections.Concurrent;
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
		public IDictionary<string, string> Tags { get; set; }
		private string _userName;

		public Message()
		{
			Tags = new ConcurrentDictionary<string,string>();
		}

		public string User
		{
			get
			{
				if (string.IsNullOrWhiteSpace(_userName))
				{
					if (Tags.ContainsKey("display-name"))
					{
						_userName = Tags["display-name"].ToLower();
					}
					else
					{
						var split = Prefix.Split('!').First();
						_userName = split.Substring(0);
					}

					var index = _userName.IndexOf("." + Constants.TwitchUser);
					if (index > -1)
					{
						_userName = _userName.Substring(0, index);
					}
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