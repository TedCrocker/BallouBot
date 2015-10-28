using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace BallouBot.PollPlugin
{
	internal class StrawPollPostResultModel
	{
		public string title { get; set; }
		public IList<string> options { get; set; }
		public bool multi = false;
		public string id { get; set; }
	}
}
