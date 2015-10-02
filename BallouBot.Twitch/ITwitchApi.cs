using System;
using System.Threading.Tasks;

namespace BallouBot.Twitch
{
	public interface ITwitchApi
	{
		Task SetUserInfo(Data.User user);
		Task<TimeSpan?> GetUptime(string channel);
	}
}