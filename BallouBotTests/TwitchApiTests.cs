using System.Xml;
using BallouBot.Config;
using BallouBot.Twitch;
using Xunit;

namespace BallouBotTests
{
	public class TwitchApiTests
	{
		[Fact]
		public async void CanGetUptime()
		{
			var config = new Config();
			var twitchApi = new TwitchApi(config);

			var uptime = await twitchApi.GetUptime("#ballouthebear");
			Assert.Null(uptime);
		}

		[Fact]
		public async void CanSetUsername()
		{
			var config = new Config();
			var twitchApi = new TwitchApi(config);

			var userInfo = new BallouBot.Data.User() {Id = "ballouthebear"};
			await twitchApi.SetUserInfo(userInfo);

			Assert.Equal(userInfo.Name, "BallouTheBear");
		}
	}
}