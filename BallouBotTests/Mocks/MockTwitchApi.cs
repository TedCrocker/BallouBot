using System;
using System.Threading.Tasks;
using BallouBot.Data;
using BallouBot.Twitch;

namespace BallouBotTests.Mocks
{
	public class MockTwitchApi : ITwitchApi
	{
		public Task SetUserInfo(User user)
		{
			throw new NotImplementedException();
		}

		public Task<TimeSpan?> GetUptime(string channel)
		{
			throw new NotImplementedException();
		}
	}
}