namespace BallouBot
{
	public static class Constants
	{
		public const string TwitchUser = "tmi.twitch.tv";
		public const int TimeBetweenUptimeRequestsInMinutes = 5;
		public const string PrivateMessageCommand = "PRIVMSG";
		public const string UserStateCommand = "USERSTATE";
		public const int CommandDequeueTimeLimit = 31;
		public const int CommandDequeueCommandLimit = 20;
	}
}