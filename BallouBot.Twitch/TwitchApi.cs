using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwixelAPI;

namespace BallouBot.Twitch
{
	public class TwitchApi
	{
		private readonly Twixel _twixel;

		public TwitchApi()
		{
			var config = new Config.Config();
			_twixel = new Twixel(config.TwitchClientID, config.TwitchRedirectUrl);
		}

		public async Task SetUserInfo(Data.User user)
		{
			var twitchUser = await _twixel.RetrieveUser(user.Id, Twixel.APIVersion.v3);
			user.Name = twitchUser.displayName;
		}
	}
}
