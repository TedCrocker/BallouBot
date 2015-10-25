using System.Collections.Generic;

namespace BallouBot.Poll
{
	internal class StrawPollPostModel
	{
		public string title { get; set; }
		public IList<string> options { get; set; }
		public bool multi = false;
	}
}