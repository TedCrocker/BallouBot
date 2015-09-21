using System.Collections.Generic;

namespace BallouBot
{
	public class Message
	{
		public string RawMessage { get; set; }
		public string Prefix { get; set; }
		public string Suffix { get; set; }
		public IEnumerable<string> Parameters { get; set; }
		public int Command { get; set; }
	}
}