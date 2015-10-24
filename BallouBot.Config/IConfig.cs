namespace BallouBot.Config
{
	public interface IConfig
	{
		string Nickname { get; set; }
		string Password { get; set; }
		string TwitchClientID { get; set; }
		string TwitchRedirectUrl { get; set; }
		string[] Channels { get; set; }
	}
}