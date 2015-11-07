using System.Linq;
using BallouBot.Config;
using BallouBot.Twitch;
using Xunit;

namespace BallouBotTests
{
	public class TwitchRestClientTests
	{
		[Fact]
		public async void CanGetFollowersForUser()
		{
			var config = new Config();

			var client = new TwitchRestClient(config);
			var followers = await client.GetFollowers("ballouthebear");

			Assert.True(followers.Any(), "There are no followers!");
		}
	}
}