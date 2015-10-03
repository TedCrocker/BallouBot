using System;
using System.Threading.Tasks;
using BallouBot.Data;
using BallouBot.Twitch;

namespace BallouBotTests.Mocks
{
	public class MockTwitchApi : ITwitchApi
	{
		public async Task SetUserInfo(User user)
		{
			user.Name = user.Id.ToUpper();
		}

		public async Task<TimeSpan?> GetUptime(string channel)
		{
			TimeSpan? timeSpan = DateTime.UtcNow - DateTime.UtcNow.AddHours(-2);
			return timeSpan;
		}
	}
}