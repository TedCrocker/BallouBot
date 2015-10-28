using System.Collections.Generic;

namespace BallouBot.PollPlugin
{
	internal class StrawPollPostModel
	{
		public string title { get; set; }
		public IList<string> options { get; set; }
		public bool multi = false;
	}
}