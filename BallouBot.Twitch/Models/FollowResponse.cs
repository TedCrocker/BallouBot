using System.Collections.Generic;

namespace BallouBot.Twitch.Models
{
	public class FollowResponse
	{
		public List<Follow> follows { get; set; }
		public int _total { get; set; }
		public Links3 _links { get; set; }
		public string _cursor { get; set; }
	}
}