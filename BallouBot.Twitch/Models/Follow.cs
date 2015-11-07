namespace BallouBot.Twitch.Models
{
	public class Follow
	{
		public string created_at { get; set; }
		public bool notifications { get; set; }
		public User user { get; set; }
	}
}