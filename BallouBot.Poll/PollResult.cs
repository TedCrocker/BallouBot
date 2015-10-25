using Newtonsoft.Json;

namespace BallouBot.Poll
{
	public class PollResult
	{
		[JsonProperty("title")]
		public string Title { get; set; }
		[JsonProperty("options")]
		public string[] Options { get; set; }
		[JsonProperty("votes")]
		public string[] Votes { get; set; }
	}
}